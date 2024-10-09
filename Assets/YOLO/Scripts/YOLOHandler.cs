using System;
using System.Collections.Generic;
using TMPro;
using Unity.Sentis;
using UnityEngine;
using YOLOQuestUnity.Inference;
using YOLOQuestUnity.ObjectDetection;

namespace YOLOQuestUnity.YOLO
{
    public class YOLOHandler : MonoBehaviour
    {

        [SerializeField] private int Size = 640;
        [SerializeField] private ModelAsset _model;
        private InferenceHandler<Texture2D> _inferenceHandler;
        private Dictionary<int, string> classes = new()
        {
            { 0, "person" },
            { 1, "bicycle" },
            { 2, "car" },
            { 3, "motorcycle" },
            { 4, "airplane" },
            { 5, "bus" },
            { 6, "train" },
            { 7, "truck" },
            { 8, "boat" },
            { 9, "traffic light" },
            { 10, "fire hydrant" },
            { 11, "stop sign" },
            { 12, "parking meter" },
            { 13, "bench" },
            { 14, "bird" },
            { 15, "cat" },
            { 16, "dog" },
            { 17, "horse" },
            { 18, "sheep" },
            { 19, "cow" },
            { 20, "elephant" },
            { 21, "bear" },
            { 22, "zebra" },
            { 23, "giraffe" }, {24, "backpack"}, {25, "umbrella"}, {26, "handbag"}, {27, "tie"}, {28, "suitcase"}, {29, "frisbee"}, {30, "skis"}, {31, "snowboard"}, {32, "sports ball"}, {33, "kite"}, {34, "baseball bat"}, {35, "baseball glove"},{ 36, "skateboard"}, {37, "surfboard"}, {38, "tennis racket"}, {39, "bottle"}, {40, "wine glass"}, {41, "cup"}, {42, "fork"}, {43, "knife"},{ 44, "spoon"}, {45, "bowl"},{ 46, "banana"},{ 47, "apple"}, {48, "sandwich"}, {49, "orange"}, {50, "broccoli"}, {51, "carrot"}, {52, "hot dog"}, {53, "pizza"}, {54, "donut"}, {55, "cake"}, {56, "chair"}, {57, "couch"}, {58, "potted plant"}, {59, "bed"}, {60, "dining table"}, {61, "toilet"}, {62, "tv"}, {63, "laptop"}, {64, "mouse"},{ 65, "remote"}, {66, "keyboard"},{ 67, "cell phone"}, {68, "microwave"}, {69, "oven"}, {70, "toaster"}, {71, "sink"}, {72, "refrigerator"}, {73, "book"}, {74, "clock"}, {75, "vase"}, {76, "scissors"}, {77, "teddy bear"},{ 78, "hair drier"}, {79, "toothbrush"}
        };
        Awaitable<Tensor<float>> analysisResult;
        private bool inferencePending = false;
        [SerializeField] private Texture2D _inputTexture;

        private int _frameCount;
        private int FrameCount { get => _frameCount; set => _frameCount = value % 30; }

        #region DebugInfo

        [SerializeField] private TextMeshProUGUI T1;
        [SerializeField] private TextMeshProUGUI T2;
        [SerializeField] private TextMeshProUGUI T3;

        #endregion

        void Start()
        {
            _inferenceHandler = new YOLOInferenceHandler(_model, 640);
        }

        void Update()
        {
            FrameCount++;

            if (FrameCount != 0) return;
            
            if (_inferenceHandler == null) return;

            try
            {
                if (analysisResult == null || !inferencePending)
                {
                    Debug.Log("Getting texture");
                    if (_inputTexture == null) return;
                    Debug.Log("Got texture");
                    Debug.Log("Starting YOLO analysis");
                    analysisResult = _inferenceHandler.Run(_inputTexture);
                    Debug.Log("YOLO Analysis started");
                    inferencePending = true;
                }
                else if (inferencePending && analysisResult.GetAwaiter().IsCompleted)
                {
                    Debug.Log("Got YOLO result");
                    var detectedObjects = PostProcess(analysisResult.GetAwaiter().GetResult());
                    inferencePending = false;
                    Debug.Log("Collected YOLO results");
                    T1.text = $"{detectedObjects[0].CocoName} detected with confidence {detectedObjects[0].Confidence}";
                    T2.text = $"{detectedObjects[1].CocoName} detected with confidence {detectedObjects[1].Confidence}";
                    T3.text = $"{detectedObjects[2].CocoName} detected with confidence {detectedObjects[2].Confidence}";
                }
            }
            catch (NullReferenceException e)
            {
                Debug.Log(e.StackTrace);
            }
            finally
            {
                //if (!inferencePending && analysisResult.GetAwaiter().IsCompleted && analysisResult != null) analysisResult.GetAwaiter().GetResult().Dispose();
            }

        }

        private List<DetectedObject> PostProcess(Tensor<float> result)
        {
            float widthScale = Size / _inputTexture.width;
            float heightScale = widthScale;

            Debug.Log($"Output Shape: {result.shape}");

            List<DetectedObject> objects = new();

            for (int i = 0; i < result.shape[1]; i++)
            {
                int cocoClass = (int)result[0, i, 5];
                objects.Add(new DetectedObject(result[0, i, 0] * widthScale, result[0, i, 2] * heightScale, result[0, i, 1] * widthScale, result[0, i, 3] * heightScale, cocoClass, classes[cocoClass], result[0, i, 4]));
            }

            return objects;
        }
    }
}