using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Profiling;
using YOLOQuestUnity.ObjectDetection;

public class YOLOPostProcessor
{
    public static List<DetectedObject> PostProcess(Tensor<float> result, Texture2D inputTexture, int inputSize, Dictionary<int, string> classes, float confidenceThreshold)
    {
        Profiler.BeginSample("YOLO.Postprocess");

        List<DetectedObject> objects = new();
        float widthScale = inputTexture.width / (float)inputSize;
        float heightScale = inputTexture.height / (float)inputSize;

        for (int i = 0; i < result.shape[2]; i++)
        {
            float confidence = result[0, 5, i];
            if (confidence < confidenceThreshold) continue;
            int cocoClass = (int)result[0, 4, i];
            float centerX = result[0, 0, i] * widthScale;
            float centerY = result[0, 1, i] * heightScale;
            float width = result[0, 2, i] * widthScale;
            float height = result[0, 3, i] * heightScale;

            objects.Add(new DetectedObject(centerX, centerY, width, height, cocoClass, classes[cocoClass], confidence));
        }

        objects.Sort((x, y) => y.Confidence.CompareTo(x.Confidence));

        Profiler.EndSample();

        return objects;
    }
}
