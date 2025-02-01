using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using YOLOQuestUnity.ObjectDetection;
using YOLOQuestUnity.Utilities;
using Meta.XR.MRUtilityKit;
using UnityEngine.EventSystems;
using Meta.XR;
using static Meta.XR.MRUtilityKit.FindSpawnPositions;
using NUnit.Framework.Interfaces;

namespace YOLOQuestUnity.YOLO.Display
{
    public class ObjectDisplayManager : MonoBehaviour
    {

        #region Model Management


        private Dictionary<int, Dictionary<int, GameObject>> _activeModels;

        private int _modelCount;
        [SerializeField] private int _maxModelCount;

        [SerializeField, SerializedDictionary("Coco Class", "3D Model")]
        private SerializedDictionary<string, GameObject> _cocoModels;

        [SerializeField] private bool _movingObjects;
        public bool MovingObjects { get => _movingObjects; set => _movingObjects = value; }

        public int ModelCount { get { return _modelCount; } private set { _modelCount = value; } }
        public int MaxModelCount { get { return _maxModelCount; } private set { _maxModelCount = value; } }

        #endregion

        #region External Data Management

        [SerializeField] private VideoFeedManager _videoFeedManager;

        private Camera _camera;

        #endregion

        #region Depth

        public bool UseSceneModel { get; set; }
        public bool UseEnvironmentDepth { get; set; }

        [SerializeField] private MRUK _mruk;
        public MRUK SceneManager { get => _mruk; set => _mruk = value; }
        private MRUKRoom currentRoom = null;

        private EnvironmentRaycastManager _environmentRaycastManager;

        private bool _sceneLoaded = false;

        #endregion

        private void Start()
        {
            _activeModels = new();
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

                Dictionary<int, GameObject> modelList;
                if (_activeModels.ContainsKey(obj.CocoClass)) modelList = _activeModels[obj.CocoClass];
                else
                {
                    modelList = new Dictionary<int, GameObject>();
                    _activeModels.Add(obj.CocoClass, modelList);
                }

                if (!objectCounts.TryAdd(obj.CocoClass, 1))
                {
                    objectCounts[obj.CocoClass]++;
                }

                if (objectCounts[obj.CocoClass] > modelList.Count && ModelCount != MaxModelCount)
                {
                    if (_cocoModels.ContainsKey(obj.CocoName) && _cocoModels[obj.CocoName] != null)
                    {
                        (Vector3 spawnPosition, Quaternion spawnRotation, float hitConfidence) = GetObjectWorldCoordinates(obj);

                        Debug.Log("Spawning new object");
                        var model = Instantiate(_cocoModels[obj.CocoName]);
                        modelList.Add(objectCounts[obj.CocoClass], model);
                        UpdateModel(obj, objectCounts[obj.CocoClass], spawnPosition, spawnRotation, model, _environmentRaycastManager != null && _environmentRaycastManager.isActiveAndEnabled && hitConfidence >= 0.5);
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
                        (Vector3 newPosition, Quaternion newRotation, float hitConfidence) = GetObjectWorldCoordinates(obj);

                        Debug.Log("Using existing object");
                        var model = modelList[objectCounts[obj.CocoClass]];
                        UpdateModel(obj, objectCounts[obj.CocoClass], newPosition, newRotation, model, _environmentRaycastManager != null && _environmentRaycastManager.isActiveAndEnabled && hitConfidence >= 0.5);
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
                    Debug.Log($"Hit {hit.status}");
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

        private void UpdateModel(DetectedObject obj, int id, Vector2 newPosition, Quaternion newRotation, GameObject model, bool useRaycastNormal)
        {
            model.transform.SetPositionAndRotation(newPosition, newRotation);

            if (!useRaycastNormal) model.transform.LookAt(_camera.transform);

            model.name = $"{obj.CocoName} {id}";
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
    }
}