using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using YOLOQuestUnity.ObjectDetection;
using YOLOQuestUnity.Utilities;

namespace YOLOQuestUnity.YOLO.Display
{
    public class ObjectDisplayManager : MonoBehaviour
    {

        private Dictionary<int, Dictionary<int, GameObject>> _activeModels;

        private int _modelCount;
        [SerializeField] private int _maxModelCount;

        [SerializeField, SerializedDictionary("Coco Class", "3D Model")]
        private SerializedDictionary<string, GameObject> _cocoModels;

        [SerializeField] private VideoFeedManager _videoFeedManager;

        [SerializeField] private Camera _camera;

        [SerializeField] private bool _movingObjects;
        public bool MovingObjects { get => _movingObjects; set => _movingObjects = value; }

        public int ModelCount { get { return _modelCount; } private set { _modelCount = value; } }
        public int MaxModelCount { get { return _maxModelCount; } private set { _maxModelCount = value; } }

        private void Start()
        {
            _activeModels = new();
        }

        public void DisplayModels(List<DetectedObject> objects)
        {
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
                        Debug.Log("Spawning new object");
                        var model = Instantiate(_cocoModels[obj.CocoName], ImageToWorldCoordinates(obj.BoundingBox.center), Quaternion.identity);
                        model.transform.LookAt(_camera.transform);
                        model.name = obj.CocoName;
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

            var feedDimensions = _videoFeedManager.GetFeedDimensions();

            var cameraWidthScale = _camera.pixelWidth / feedDimensions.Width;
            var cameraHeightScale = _camera.pixelHeight / feedDimensions.Height;

            var newX = coordinates.x * cameraWidthScale;
            var newY = _camera.pixelHeight - coordinates.y * cameraHeightScale;

            var newWorldPoint = _camera.ScreenToWorldPoint(new Vector3(newX, newY, 1.5f));
            newWorldPoint.x -= 0.5f;
            //newWorldPoint.y += 0.5f;

            return newWorldPoint;
        }
    }
}