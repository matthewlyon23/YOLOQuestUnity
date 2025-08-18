using System.Collections.Generic;
using MyBox;
using Niantic.Lightship.AR.ObjectDetection;
using Niantic.Lightship.AR.XRSubsystems;
using UnityEngine;
using YOLOQuestUnity.ObjectDetection;

namespace YOLOQuestUnity.Display
{
    public class ARObjectDetectionHandler : MonoBehaviour
    {
        
        private ARObjectDetectionManager _arObjectDetectionManager;
        [SerializeField] private Camera referenceCamera;
        [SerializeField] private ObjectDisplayManager objectDisplayManager;

        private Camera analysisCamera;
        
        private void Start()
        {
            _arObjectDetectionManager = GetComponent<ARObjectDetectionManager>();
            analysisCamera = GetComponent<Camera>();
            if (_arObjectDetectionManager == null) return;
            _arObjectDetectionManager.ObjectDetectionsUpdated += OnArObjectDetectionsUpdated;
        }

        private void OnArObjectDetectionsUpdated(ARObjectDetectionsUpdatedEventArgs args)
        {
            if (args.Results.IsNullOrEmpty()) return;
            List<DetectedObject> detectedObjects = new();
            
            foreach (var arObjectDetection in args.Results)
            {
                var rect = arObjectDetection.CalculateRect(referenceCamera.scaledPixelWidth, referenceCamera.scaledPixelHeight, ScreenOrientation.AutoRotation);
                var cats = arObjectDetection.GetConfidentCategorizations();
                if (cats.IsNullOrEmpty()) return;
                var result = cats[0];
                foreach (var cat in cats)
                {
                    if (cat.Confidence > result.Confidence)
                    {
                        result = cat;
                    }
                }

                var detectedObject = new DetectedObject(rect.center.x, rect.center.y, rect.width, rect.height,
                    result.CategoryIndex, result.CategoryName, result.Confidence);
                Debug.Log($"Detected Object: {detectedObject.CocoClass} {detectedObject.Confidence} {detectedObject.Confidence}");
                detectedObjects.Add(detectedObject);
            }
            
            analysisCamera.CopyFrom(referenceCamera);
            objectDisplayManager.DisplayModels(detectedObjects, analysisCamera);
            
        }
    }
}
