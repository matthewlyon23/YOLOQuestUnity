using AYellowpaper.SerializedCollections;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YOLOQuestUnity.ObjectDetection;
using YOLOQuestUnity.Utilities;

namespace YOLOQuestUnity.YOLO.Display
{
    public class ObjectDisplayManager : MonoBehaviour
    {

        #region Model Management

        private Dictionary<int, Dictionary<int, GameObject>> _activeModels;

        private int _modelCount;
        [SerializeField] private int _maxModelCount;
        [SerializeField] private float _distanceThreshold = 0.5f;

        [SerializeField, SerializedDictionary("Coco Class", "3D Model")]
        private SerializedDictionary<string, GameObject> _cocoModels;

        [SerializeField] private bool _movingObjects;
        public bool MovingObjects { get => _movingObjects; set => _movingObjects = value; }

        public int ModelCount { get { return _modelCount; } private set { _modelCount = value; } }
        public int MaxModelCount { get { return _maxModelCount; } private set { _maxModelCount = value; } }
        public float DistanceThreshold { get { return _distanceThreshold; } private set { _distanceThreshold = value; } }

        [SerializeField] private ScaleType _scaleType = ScaleType.AVERAGE;


        #endregion

        #region External Data Management

        [SerializeField] private VideoFeedManager _videoFeedManager;

        private Camera _camera;

        #endregion

        #region Depth

        private MRUK _mruk;
        private MRUK SceneManager { get => _mruk; set => _mruk = value; }
        private MRUKRoom currentRoom = null;

        private EnvironmentRaycastManager _environmentRaycastManager;

        private bool _sceneLoaded = false;

        #endregion

        private void Start()
        {
            _activeModels = new();
            SceneManager = FindAnyObjectByType<MRUK>();
            SceneManager.SceneLoadedEvent.AddListener(OnSceneLoad);
            SceneManager.RoomUpdatedEvent.AddListener(OnSceneUpdated);
            _environmentRaycastManager = GetComponent<EnvironmentRaycastManager>();
        }

        public void DisplayModels(List<DetectedObject> objects, Camera referenceCamera)
        {
            _camera = referenceCamera;

            Dictionary<int, int> objectCounts = new();

            foreach (var obj in objects)
            {
                if (objectCounts.GetValueOrDefault(obj.CocoClass) == 2) continue;

                if (!_cocoModels.ContainsKey(obj.CocoName) || _cocoModels[obj.CocoName] == null)
                {
                    Debug.Log("Error: No model provided for the detected class.");

                    continue;
                }

                Dictionary<int, GameObject> modelList;
                if (_activeModels.ContainsKey(obj.CocoClass)) modelList = _activeModels[obj.CocoClass];
                else
                {
                    modelList = new Dictionary<int, GameObject>();
                    _activeModels.Add(obj.CocoClass, modelList);
                }

                (Vector3 spawnPosition, Quaternion spawnRotation, float hitConfidence) = GetObjectWorldCoordinates(obj);

                if (IsDuplicate(spawnPosition, modelList)) continue;

                if (!objectCounts.TryAdd(obj.CocoClass, 1))
                {
                    objectCounts[obj.CocoClass]++;
                }

                if (objectCounts[obj.CocoClass] > modelList.Count && ModelCount != MaxModelCount)
                {
                    if (_cocoModels.ContainsKey(obj.CocoName) && _cocoModels[obj.CocoName] != null)
                    {

                        Debug.Log("Spawning new object");
                        var model = Instantiate(_cocoModels[obj.CocoName]);
                        modelList.Add(objectCounts[obj.CocoClass], model);
                        Debug.Log($"Hit Confidence: {hitConfidence}");
                        UpdateModel(obj, objectCounts[obj.CocoClass], spawnPosition, spawnRotation, model, _environmentRaycastManager != null && _environmentRaycastManager.isActiveAndEnabled && hitConfidence >= 0.2f);
                        ModelCount++;
                    }
                    else
                    {
                        Debug.Log("Error: No model provided for the detected class.");
                    }
                }
                else if (objectCounts[obj.CocoClass] <= modelList.Count)
                {
                    if (MovingObjects)
                    {
                        Debug.Log("Using existing object");
                        var model = modelList[objectCounts[obj.CocoClass]];
                        UpdateModel(obj, objectCounts[obj.CocoClass], spawnPosition, spawnRotation, model, _environmentRaycastManager != null && _environmentRaycastManager.isActiveAndEnabled && hitConfidence >= 0.2f);
                    }
                }
            }

            //foreach (var kv in _activeModels)
            //{
            //    if (!objectCounts.ContainsKey(kv.Key))
            //    {
            //        foreach (var obj in kv.Value)
            //        {
            //            Destroy(obj.Value);
            //        }
            //        ModelCount -= kv.Value.Count;
            //        _activeModels[kv.Key] = new Dictionary<int, GameObject>();
            //        continue;
            //    }

            //    var modelList = kv.Value;
            //    var cocoClass = kv.Key;

            //    for (int i = objectCounts[cocoClass]; i < modelList.Count; i++)
            //    {
            //        var model = modelList[i];
            //        model.SetActive(false);
            //    }
            //}
        }

        private bool IsDuplicate(Vector3 spawnPosition, Dictionary<int, GameObject> modelList)
        {
            foreach (var (id, model) in modelList)
            {
                // if "close enough" to new model, don't add
                // Euclidian distance?

                var distance = Vector3.Distance(spawnPosition, model.transform.position);
                if (distance < DistanceThreshold)
                {
                    return true;
                }
            }

            return false;
        }
        private Vector3 ImageToWorldCoordinates(Vector2 coordinates)
        {

            Vector3 screenPoint = ImageToScreenCoordinates(coordinates);

            var newX = screenPoint.x;
            var newY = screenPoint.y;

            var spawnDepth = 1.5f;
            if (_sceneLoaded && currentRoom != null)
            {
                Debug.Log("Testing Depth");

                Ray ray = _camera.ScreenPointToRay(new Vector2(newX, newY));
                if (currentRoom.Raycast(ray, 500, out RaycastHit hit, out MRUKAnchor anchor))
                {
                    Debug.Log($"Hit {anchor.Label}");
                    spawnDepth = hit.distance;
                }
            }

            Vector3 newWorldPoint = _camera.ScreenToWorldPoint(new Vector3(newX, newY, spawnDepth));

            return newWorldPoint;
        }

        public (Vector3, Quaternion, float) GetObjectWorldCoordinates(DetectedObject obj)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            float hitConfidence = 0;

            if (_environmentRaycastManager != null && _environmentRaycastManager.isActiveAndEnabled && EnvironmentRaycastManager.IsSupported)
            {
                Ray ray = _camera.ScreenPointToRay(ImageToScreenCoordinates(obj.BoundingBox.center));
                if (_environmentRaycastManager.Raycast(ray, out EnvironmentRaycastHit hit))
                {
                    position = hit.point;
                    rotation = Quaternion.LookRotation(hit.normal);
                    hitConfidence = hit.normalConfidence;
                }

                Debug.Log($"Hit {hit.status}");
            }
            else position = ImageToWorldCoordinates(obj.BoundingBox.center);

            return (position, rotation, hitConfidence);
        }

        private Vector2 ImageToScreenCoordinates(Vector2 coordinates)
        {
            var feedDimensions = _videoFeedManager.GetFeedDimensions();

            var cameraWidthScale = _camera.pixelWidth / feedDimensions.Width;
            var cameraHeightScale = _camera.pixelHeight / feedDimensions.Height;

            var newX = coordinates.x * cameraWidthScale;
            var newY = _camera.pixelHeight - coordinates.y * cameraHeightScale;

            //newX -= _camera.pixelWidth * 0.08f;

            return new Vector2(newX, newY);
        }

        private void RescaleObject(DetectedObject obj, GameObject model)
        {
            Vector3 p3 = obj.BoundingBox.max;
            Vector3 p1 = obj.BoundingBox.min;

            Vector3 sP3 = ImageToScreenCoordinates(p3);
            Vector3 sP1 = ImageToScreenCoordinates(p1);

            var newHeight = Math.Abs(sP3.y - sP1.y);
            var newWidth = Math.Abs(sP3.x - sP1.x);

            Vector3[] boundPoints = new Vector3[8];

            boundPoints[0] = model.GetComponentInChildren<MeshRenderer>().bounds.min;
            boundPoints[1] = model.GetComponentInChildren<MeshRenderer>().bounds.max;
            boundPoints[2] = new Vector3(boundPoints[0].x, boundPoints[0].y, boundPoints[1].z);
            boundPoints[3] = new Vector3(boundPoints[0].x, boundPoints[1].y, boundPoints[0].z);
            boundPoints[4] = new Vector3(boundPoints[1].x, boundPoints[0].y, boundPoints[0].z);
            boundPoints[5] = new Vector3(boundPoints[0].x, boundPoints[1].y, boundPoints[1].z);
            boundPoints[6] = new Vector3(boundPoints[1].x, boundPoints[0].y, boundPoints[1].z);
            boundPoints[7] = new Vector3(boundPoints[1].x, boundPoints[1].y, boundPoints[0].z);

            Debug.Log("Max Current: " + boundPoints[1]);
            Debug.Log("Min Current: " + boundPoints[0]);

            Vector2[] screenPoints = boundPoints.Select(boundPoint => (Vector2)_camera.WorldToScreenPoint(boundPoint)).ToArray();

            Vector2 maxPoint = screenPoints[0];
            Vector2 minPoint = screenPoints[0];

            foreach (Vector3 screenPoint in screenPoints)
            {
                if (screenPoint.x >= maxPoint.x && screenPoint.y >= maxPoint.y) maxPoint = screenPoint;
                if (screenPoint.x <= minPoint.x && screenPoint.y <= minPoint.y) minPoint = screenPoint;
            }

            var currentWidth = Math.Abs(maxPoint.x - minPoint.x);
            var currentHeight = Math.Abs(maxPoint.y - minPoint.y);
            var scaleFactor = _scaleType switch
            {
                ScaleType.WIDTH => newWidth / currentWidth,
                ScaleType.HEIGHT => newHeight / currentHeight,
                ScaleType.AVERAGE => ((newWidth / currentWidth) + (newHeight / currentHeight)) / 2,
                ScaleType.MIN => Math.Min(newWidth / currentWidth, newHeight / currentHeight),
                ScaleType.MAX => Math.Max(newWidth / currentWidth, newHeight / currentHeight),
                _ => 1f,
            };
            
            Debug.Log($"Scaling {model.name} by {scaleFactor}");
            Vector3 scaleVector = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            
            Debug.Log("Old Scale: " + model.transform.localScale);
            Vector3 oldScale = model.transform.localScale;
            oldScale.Scale(scaleVector);
            
            model.transform.localScale = oldScale;
            Debug.Log("New Scale: " + model.transform.localScale);
        }

        private void UpdateModel(DetectedObject obj, int id, Vector3 newPosition, Quaternion newRotation, GameObject model, bool useRaycastNormal)
        {
            model.transform.SetPositionAndRotation(newPosition, newRotation);

            if (!useRaycastNormal) model.transform.LookAt(_camera.transform);

            model.name = $"{obj.CocoName} {id}";
            RescaleObject(obj, model);
            model.SetActive(true);
        }


        private void OnSceneLoad()
        {
            _sceneLoaded = true;
            currentRoom = SceneManager.GetCurrentRoom();
        }

        private void OnSceneUpdated(MRUKRoom room)
        {
            currentRoom = room;
        }

        private enum ScaleType
        {
            WIDTH,
            HEIGHT,
            AVERAGE,
            MIN,
            MAX
        }
    }
}