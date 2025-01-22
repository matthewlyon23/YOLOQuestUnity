using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using YOLOQuestUnity.ObjectDetection;
using YOLOQuestUnity.Utilities;
using Meta.XR.MRUtilityKit;
using UnityEngine.EventSystems;
using Meta.XR;
using static Meta.XR.MRUtilityKit.FindSpawnPositions;

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
                        Vector3 spawnPosition = Vector3.zero;
                        Quaternion spawnRotation = Quaternion.identity;

                        if (_environmentRaycastManager != null && _environmentRaycastManager.enabled)
                        {
                            Debug.Log("Using environment raycast manager");

                            Ray ray = _camera.ScreenPointToRay(ImageToScreenCoordinates(obj.BoundingBox.center));
                            if (_environmentRaycastManager.Raycast(ray, out EnvironmentRaycastHit hit))
                            {
                                Debug.Log($"Hit {hit.status}");
                                spawnPosition = hit.point;
                                spawnRotation = Quaternion.LookRotation(hit.normal);
                                Debug.Log($"Depth: Spawning new object at point ({spawnPosition.x}, {spawnPosition.y}, {spawnPosition.z}) distance {(_camera.transform.position - spawnPosition).magnitude}");
                                Debug.Log($"Depth: Previous method would have places at {ImageToWorldCoordinates(obj.BoundingBox.center)}");
                            }
                        }
                        else
                        {
                            spawnPosition = ImageToWorldCoordinates(obj.BoundingBox.center);
                        }

                        Debug.Log("Spawning new object");
                        var model = Instantiate(_cocoModels[obj.CocoName], spawnPosition, spawnRotation);
                        if (_environmentRaycastManager == null)
                        {
                            model.transform.LookAt(_camera.transform);
                        }
                    model.name = $"{obj.CocoName} {objectCounts[obj.CocoClass]}";
                        modelList.Add(objectCounts[obj.CocoClass], model);

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
                        model.transform.SetPositionAndRotation(ImageToWorldCoordinates(obj.BoundingBox.center), Quaternion.identity);
                        model.transform.LookAt(_camera.transform);
                        model.name = obj.CocoName;
                        model.SetActive(true);
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

            var screenPoint = ImageToScreenCoordinates(coordinates);

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

            var newWorldPoint = _camera.ScreenToWorldPoint(new Vector3(newX, newY, spawnDepth));
            //newWorldPoint.x -= 0.3f;
            //newWorldPoint.y += 0.5f;

            return newWorldPoint;
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