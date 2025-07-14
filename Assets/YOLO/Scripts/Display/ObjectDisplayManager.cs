using MyBox;
using AYellowpaper.SerializedCollections;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using YOLOQuestUnity.ObjectDetection;
using YOLOQuestUnity.PassthroughCamera;
using YOLOQuestUnity.Utilities;

namespace YOLOQuestUnity.Display
{
    public class ObjectDisplayManager : MonoBehaviour
    {
        #region Model Management

        private Dictionary<int, Dictionary<int, GameObject>> _activeModels;

        private int _modelCount;
        [Tooltip("The maximum number of models which can spawn at once.")]
        [PositiveValueOnly] [SerializeField] private int _maxModelCount = 10;
        [Tooltip("The minimum distance from an existing model at which a model of the same class can spawn.")]
        [PositiveValueOnly] [SerializeField] private float _distanceThreshold = 1f;

        [Tooltip("The names of the COCO classes to detect and their associated models.")]
        [SerializeField, SerializedDictionary("Coco Class", "3D Model")]
        private SerializedDictionary<string, GameObject> _cocoModels;

        [Tooltip("Use existing models when a new object is detected.")]
        [SerializeField] private bool _movingObjects;
        public bool MovingObjects { get => _movingObjects; set => _movingObjects = value; }

        public int ModelCount { get { return _modelCount; } private set { _modelCount = value; } }
        public int MaxModelCount { get { return _maxModelCount; } private set { _maxModelCount = value; } }
        public float DistanceThreshold { get { return _distanceThreshold; } private set { _distanceThreshold = value; } }

        [Tooltip("The scaling method to use:\nMIN: Use the minimum of the x and y scale change.\nMAX: Use the maximum of the x and y scale change.\nAVERAGE: Use the average of both the x and y scale change.\nWIDTH: Use the x scale change.\nHEIGHT: Use the y scale change.")]
        [SerializeField] private ScaleType _scaleType = ScaleType.AVERAGE;

        private const float ScaleDampener = 0.3f;
        
        
        #endregion

        #region External Data Management

        [Tooltip("The VideoFeedManager used to capture input frames.")]
        [MustBeAssigned] [SerializeField] private VideoFeedManager _videoFeedManager;

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
            _activeModels = new Dictionary<int, Dictionary<int, GameObject>>();
            SceneManager = FindAnyObjectByType<MRUK>();
            SceneManager.SceneLoadedEvent.AddListener(OnSceneLoad);
            SceneManager.RoomUpdatedEvent.AddListener(OnSceneUpdated);
            _environmentRaycastManager = GetComponent<EnvironmentRaycastManager>();
            Unity.XR.Oculus.Utils.SetupEnvironmentDepth(new Unity.XR.Oculus.Utils.EnvironmentDepthCreateParams());
        }

        public void DisplayModels(List<DetectedObject> objects, Camera referenceCamera)
        {
            Profiler.BeginSample("ObjectDisplayManager.DisplayModels");

            _camera = referenceCamera;

            Dictionary<int, int> objectCounts = new();

            foreach (var obj in objects)
            {
                if (objectCounts.GetValueOrDefault(obj.CocoClass) == 3) continue;

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
                
                if ((!MovingObjects || objectCounts[obj.CocoClass] > modelList.Count) && ModelCount != MaxModelCount)
                {
                    var model = Instantiate(_cocoModels[obj.CocoName]);
                    modelList.Add(modelList.Count, model);
                    UpdateModel(obj, objectCounts[obj.CocoClass], spawnPosition, spawnRotation, model, _environmentRaycastManager != null && _environmentRaycastManager.isActiveAndEnabled && hitConfidence >= 0.5f);
                    ModelCount++;
                }
                else if (objectCounts[obj.CocoClass] <= modelList.Count)
                {
                    if (MovingObjects)
                    {
                        var model = modelList[objectCounts[obj.CocoClass] - 1];
                        UpdateModel(obj, objectCounts[obj.CocoClass], spawnPosition, spawnRotation, model, _environmentRaycastManager != null && _environmentRaycastManager.isActiveAndEnabled && hitConfidence >= 0.5f);
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

            Profiler.EndSample();
        }

        #region Model Methods

        private void RescaleObject(DetectedObject obj, GameObject model)
        {

            Vector3 p3 = obj.BoundingBox.max;
            Vector3 p1 = obj.BoundingBox.min;

            Vector3 sP3 = ImageToScreenCoordinates(p3);
            Vector3 sP1 = ImageToScreenCoordinates(p1);

            float newHeight = Math.Abs(sP3.y - sP1.y);
            float newWidth = Math.Abs(sP3.x - sP1.x);

            (Vector2 minPoint, Vector2 maxPoint) = GetModel2DBounds(GetModel3DBounds(model));

            float currentWidth = Math.Abs(maxPoint.x - minPoint.x);
            float currentHeight = Math.Abs(maxPoint.y - minPoint.y);
            float scaleFactor = _scaleType switch
            {
                ScaleType.WIDTH => newWidth / currentWidth,
                ScaleType.HEIGHT => newHeight / currentHeight,
                ScaleType.AVERAGE => ((newWidth / currentWidth) + (newHeight / currentHeight)) / 2,
                ScaleType.MIN => Math.Min(newWidth / currentWidth, newHeight / currentHeight),
                ScaleType.MAX => Math.Max(newWidth / currentWidth, newHeight / currentHeight),
                _ => 1f
            };
            scaleFactor *= 1f-ScaleDampener;
            if (float.IsInfinity(scaleFactor)) scaleFactor = 1f;
            Debug.Log($"Scale Factor for {obj.CocoName}: {scaleFactor}");
            Vector3 scaleVector = new(scaleFactor, scaleFactor, scaleFactor);
            model.transform.localScale = Vector3.Scale(model.transform.localScale, scaleVector);
        }

        private void UpdateModel(DetectedObject obj, int id, Vector3 newPosition, Quaternion newRotation, GameObject model, bool useRaycastNormal)
        {
            model.transform.SetPositionAndRotation(newPosition, newRotation);

            if (!useRaycastNormal) model.transform.LookAt(_camera.transform);

            model.name = $"{obj.CocoName} {id}";
            RescaleObject(obj, model);
            model.SetActive(true);
        }

        private bool IsDuplicate(Vector3 spawnPosition, Dictionary<int, GameObject> modelList)
        {
            foreach (var (id, model) in modelList)
            {
                // if "close enough" to new model, don't add
                // Euclidian distance?

                var distance = Vector3.Distance(spawnPosition, model.transform.position);
                Debug.Log("Distance: " + distance);
                Debug.Log("Distance id: " + id);
                var boundingBoxR = Vector3.Distance(model.GetComponentInChildren<MeshRenderer>().bounds.max, model.GetComponentInChildren<MeshRenderer>().bounds.center);
                if (distance < DistanceThreshold * boundingBoxR)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Helper Methods

        private (Vector3, Quaternion, float) GetObjectWorldCoordinates(DetectedObject obj)
        {
            const int SpreadWidth = 3;
            const int SpreadHeight = 3;

            Vector3 position;
            Quaternion rotation;
            float hitConfidence = 1;
            
            var xDistanceFromCentre = obj.BoundingBox.center.x - _videoFeedManager.GetFeedDimensions().Width/2f;
            var yDistanceFromCentre = obj.BoundingBox.center.y - _videoFeedManager.GetFeedDimensions().Height/2f;

            var normalisedXDistanceFromCentre = xDistanceFromCentre / _videoFeedManager.GetFeedDimensions().Width / 2f;
            var normalisedYDistanceFromCentre = yDistanceFromCentre / _videoFeedManager.GetFeedDimensions().Height / 2f;

            var yDistanceMultiplier = 1.5f;
            var xDistanceMultiplier = 0.95f;

            if (normalisedYDistanceFromCentre < 0)
            {
                yDistanceMultiplier *= 4f;
            }

            if (normalisedYDistanceFromCentre < 0)
            {
                xDistanceMultiplier *= 3f;
            }
            
            var unwarpedPoint = new Vector2((1f-xDistanceMultiplier*normalisedXDistanceFromCentre)*obj.BoundingBox.center.x, (1f-yDistanceMultiplier*normalisedYDistanceFromCentre)*obj.BoundingBox.center.y);
            
            if (_environmentRaycastManager != null && _environmentRaycastManager.isActiveAndEnabled && EnvironmentRaycastManager.IsSupported)
            {
                var screenPoint = ImageToScreenCoordinates(unwarpedPoint).ToVector2Int();
                if (_environmentRaycastManager.Raycast(
                            PassthroughCameraUtils.ScreenPointToRayInWorld(PassthroughCameraEye.Left,
                                screenPoint), out var hit))
                {
                    position = hit.point;
                    rotation = Quaternion.LookRotation(hit.normal);
                    hitConfidence = hit.normalConfidence;
                }
                else
                {
                    (position, rotation) = ImageToWorldCoordinates(unwarpedPoint);
                }
                // return AverageRaycastHits(FireRaycastSpread(obj, SpreadWidth, SpreadHeight));
                // var ray = _camera.ScreenPointToRay(obj.BoundingBox.center, Camera.MonoOrStereoscopicEye.Left);
                // if (_environmentRaycastManager.Raycast(ray, out var hit))
                // {
                //     Debug.DrawRay(ray.origin, ray.direction, Color.yellow);
                //     (position, rotation, hitConfidence) = (hit.point, Quaternion.LookRotation(hit.normal), hit.normalConfidence);
                // }
                // else
                // {
                //     (position, rotation) = ImageToWorldCoordinates(obj.BoundingBox.center);
                // }
            }
            else (position, rotation) = ImageToWorldCoordinates(unwarpedPoint);

            return (position, rotation, hitConfidence);
        }

        private (Vector3, Quaternion, float) AverageRaycastHits(EnvironmentRaycastHit[] hits)
        {
            Vector3 pointSum = Vector3.zero;
            Vector3 normalSum = Vector3.zero;
            float confidenceSum = 0;
            int normalCount = 0;

            foreach (EnvironmentRaycastHit hit in hits)
            {
                pointSum += hit.point;
                if (hit.normalConfidence > 0.5f)
                {
                    normalSum += hit.normal;
                    confidenceSum += hit.normalConfidence;
                    normalCount++;
                }
            }

            Vector3 averagePosition = pointSum / hits.Length;
            Quaternion averageRotation = Quaternion.LookRotation(normalSum / hits.Length);
            float averageHitConfidence = confidenceSum / normalCount;

            return (averagePosition, averageRotation, averageHitConfidence);
        }

        private (Vector2, Vector2) GetModel2DBounds(Vector3[] bounds3D)
        {
            Vector2[] screenPoints = bounds3D.Select(boundPoint => (Vector2)_camera.WorldToScreenPoint(boundPoint)).ToArray();

            float maxX = screenPoints[0].x;
            float minX = screenPoints[0].x;
            float maxY = screenPoints[0].y;
            float minY = screenPoints[0].y;

            foreach (Vector3 screenPoint in screenPoints)
            {
                if (screenPoint.x > maxX) maxX = screenPoint.x;
                if (screenPoint.x < minX) minX = screenPoint.x;
                if (screenPoint.y > maxY) maxY = screenPoint.y;
                if (screenPoint.y < minY) minY = screenPoint.y;
            }

            return (new Vector2(minX, minY), new Vector2(maxX, maxY));
        }

        private Vector3[] GetModel3DBounds(GameObject model)
        {
            Vector3[] boundPoints = new Vector3[8];

            boundPoints[0] = model.GetComponentInChildren<MeshRenderer>().bounds.min;
            boundPoints[1] = model.GetComponentInChildren<MeshRenderer>().bounds.max;
            boundPoints[2] = new Vector3(boundPoints[0].x, boundPoints[0].y, boundPoints[1].z);
            boundPoints[3] = new Vector3(boundPoints[0].x, boundPoints[1].y, boundPoints[0].z);
            boundPoints[4] = new Vector3(boundPoints[1].x, boundPoints[0].y, boundPoints[0].z);
            boundPoints[5] = new Vector3(boundPoints[0].x, boundPoints[1].y, boundPoints[1].z);
            boundPoints[6] = new Vector3(boundPoints[1].x, boundPoints[0].y, boundPoints[1].z);
            boundPoints[7] = new Vector3(boundPoints[1].x, boundPoints[1].y, boundPoints[0].z);

            return boundPoints;
        }

        private EnvironmentRaycastHit[] FireRaycastSpread(DetectedObject obj, int spreadWidth, int spreadHeight)
        {
            if (spreadWidth <= 0 || spreadHeight <= 0) throw new Exception("Spread width and spread height must both be greater than 0");

            if (spreadWidth % 2 == 0) spreadWidth += 1;
            if (spreadHeight % 2 == 0) spreadHeight += 1;

            Vector2[,] rayPoints = new Vector2[spreadHeight, spreadWidth];
            rayPoints[spreadHeight / 2, spreadWidth / 2] = ImageToScreenCoordinates(obj.BoundingBox.center);

            float yDist = 0.01f * _videoFeedManager.GetFeedDimensions().Height;
            float xDist = 0.01f * _videoFeedManager.GetFeedDimensions().Width;

            float currentY = rayPoints[spreadHeight / 2, spreadWidth / 2].y - yDist;
            float currentX = rayPoints[spreadHeight / 2, spreadWidth / 2].x - xDist;

            for (int i = 0; i < spreadHeight; i++)
            {
                for (int j = 0; j < spreadWidth; j++)
                {
                    if (i == spreadHeight / 2 && j == spreadWidth / 2) continue;
                    rayPoints[i, j] = ImageToScreenCoordinates(new Vector2(currentX, currentY));
                    currentX += xDist;
                }

                currentY += yDist;
                currentX = rayPoints[spreadHeight / 2, spreadWidth / 2].x - xDist;
            }

            // Replace _camera.ScreenPointToRay with PassthroughCameraUtils.ScreenPointToRayWorld (unclear whether this is ImageToScreenCoordinates converted or not)

            // Very unhappy with this. Will return to it.
            Ray[] rays = null;
            //if (_videoFeedManager.GetType() == typeof(WebCamTextureManager)) rays = rayPoints.Cast<Vector2Int>().Select(point => PassthroughCameraUtils.ScreenPointToRayInWorld(((WebCamTextureManager)_videoFeedManager).Eye, point)).ToArray();
            //else rays = rayPoints.Cast<Vector2>().Select(point => _camera.ScreenPointToRay(point)).ToArray();

            rays = rayPoints.Cast<Vector2>().Select(point => _camera.ScreenPointToRay(point)).ToArray();

            EnvironmentRaycastHit[] hits = rays.Select(ray =>
            {
                _environmentRaycastManager.Raycast(ray, out EnvironmentRaycastHit hit);
                return hit;
            }).Where(hit => hit.status == EnvironmentRaycastHitStatus.Hit).ToArray();

            return hits;
        }

        private (Vector3, Quaternion) ImageToWorldCoordinates(Vector2 coordinates)
        {

            Vector3 screenPoint = ImageToScreenCoordinates(coordinates);

            float newX = screenPoint.x;
            float newY = screenPoint.y;

            float spawnDepth = 1.5f;
            if (_sceneLoaded && currentRoom != null)
            {
                Ray ray = _camera.ScreenPointToRay(new Vector2(newX, newY), Camera.MonoOrStereoscopicEye.Left);
                if (currentRoom.Raycast(ray, 500, out RaycastHit hit, out MRUKAnchor anchor))
                {
                    return (hit.point, Quaternion.LookRotation(hit.normal));
                }
            }

            return (_camera.ScreenToWorldPoint(new Vector3(newX, newY, spawnDepth)), Quaternion.identity);
        }

        private Vector2 ImageToScreenCoordinates(Vector2 coordinates)
        {
            FeedDimensions feedDimensions = _videoFeedManager.GetFeedDimensions();

            float cameraWidthScale = (float)_camera.scaledPixelWidth / feedDimensions.Width;
            float cameraHeightScale = (float)_camera.scaledPixelHeight / feedDimensions.Height;

            float newX = coordinates.x * cameraWidthScale;
            float newY = _camera.pixelHeight - coordinates.y * cameraHeightScale;

            return new Vector2(newX, newY);
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

        #endregion

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