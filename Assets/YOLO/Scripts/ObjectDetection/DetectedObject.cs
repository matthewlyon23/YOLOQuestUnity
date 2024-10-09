using UnityEngine;

namespace YOLOQuestUnity.ObjectDetection
{
    public class DetectedObject
    {
        public Rect BoundingBox { get; private set; }
        public int CocoClass { get; private set; }
        public string CocoName { get; private set; }
        public float Confidence { get; private set; }

        public DetectedObject(float minX, float minY, float maxX, float maxY, int cocoClass, string cocoName, float confidence)
        {
            CocoClass = cocoClass;
            CocoName = cocoName;
            Confidence = confidence;
            BoundingBox = new Rect(minX, minY, maxX - minX, maxY - minX);
        }

        
    }
}
