using System;
using System.Collections.Generic;
using MyBox;
using Niantic.Lightship.AR.ObjectDetection;
using UnityEngine;

namespace YOLOTools.YOLO.ObjectDetection
{
    public class NianticObjectDetectionHandler : MonoBehaviour
    {
        private ARObjectDetectionManager _arObjectDetectionManager;
        [MustBeAssigned] [SerializeField] private Camera referenceCamera;

        public event Action<List<DetectedObject>> OnDetectedObjectsUpdated = delegate { };

        [Range(0, 1)] public float confidenceThreshold = 0.5f;
        
        private void Start()
        {
            _arObjectDetectionManager = GetComponent<ARObjectDetectionManager>();
            _arObjectDetectionManager.ObjectDetectionsUpdated += OnObjectDetectionsUpdated;
        }

        private void OnObjectDetectionsUpdated(ARObjectDetectionsUpdatedEventArgs args)
        {
            var objects = args.Results;

            if (objects.IsNullOrEmpty()) return;
            
            var detectedObjects = new List<DetectedObject>();
            
            foreach (var obj in objects)
            {
                var rect = obj.CalculateRect(referenceCamera.scaledPixelWidth, referenceCamera.scaledPixelHeight, ScreenOrientation.AutoRotation);
                var confidences = obj.GetConfidentCategorizations(confidenceThreshold);

                if (confidences.IsNullOrEmpty()) return;
                
                float finalConfidence = confidences[0].Confidence;
                int finalIndex = 0;
                
                for (int i = 1; i < confidences.Count; i++)
                {
                    if (finalConfidence < confidences[i].Confidence)
                    {
                        finalConfidence = confidences[i].Confidence;
                        finalIndex = i;
                    }
                }
                
                Debug.Log($"Detected Object: {rect.center.x} {rect.center.y} {rect.width} {rect.height} {finalIndex} {confidences[finalIndex].CategoryName} {finalConfidence}");
                
                detectedObjects.Add(new DetectedObject(rect.center.x, rect.center.y, rect.width, rect.height, finalIndex, confidences[finalIndex].CategoryName, finalConfidence));
                
            }
            
            OnDetectedObjectsUpdated.Invoke(detectedObjects);
            
        }
        
    }
}
