using Oculus.Interaction.DebugTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Profiling;
using YOLOQuestUnity.Inference;
using YOLOQuestUnity.ObjectDetection;
using YOLOQuestUnity.Utilities;

namespace YOLOQuestUnity.YOLO
{
    public class YOLOHandler : MonoBehaviour
    {

        #region Inputs

        [SerializeField] private int Size = 640;
        [SerializeField] private ModelAsset _model;
        [SerializeField] private VideoFeedManager _YOLOCamera;
        [SerializeField] private uint _layersPerFrame = 10;

        #endregion

        #region InstanceFields

        private InferenceHandler<Texture2D> _inferenceHandler;
        private int _frameCount;
        private int FrameCount { get => _frameCount; set => _frameCount = value % 30; }
        private bool inferencePending = false;
        private bool readingBack = false;
        Awaitable<Tensor<float>> analysisResult;
        Tensor<float> analysisResultTensor;
        private Texture2D _inputTexture;
        IEnumerator splitInferenceEnumerator;

        #endregion

        #region Data

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

        #endregion

        #region Debugging

        [SerializeField] private TextMeshProUGUI T1;
        [SerializeField] private TextMeshProUGUI T2;
        [SerializeField] private TextMeshProUGUI T3;

        #endregion

        void Start()
        {
            _inferenceHandler = new YOLOInferenceHandler(_model, 640);
            if (_layersPerFrame == 0) _layersPerFrame = 1;
        }

        void Update()
        {
            if (_inferenceHandler == null) return;

            if (_YOLOCamera == null) return;

            if (readingBack) return;

            try
            {
                if (!inferencePending)
                {
                    if ((_inputTexture = _YOLOCamera.GetTexture()) == null) return;
                    splitInferenceEnumerator = _inferenceHandler.RunWithLayerControl(_inputTexture);
                    inferencePending = true;
                }
                if (inferencePending)
                {
                    int it = 0;
                    while (splitInferenceEnumerator.MoveNext()) if (++it % _layersPerFrame == 0) return;
                    
                    Debug.Log("Got YOLO result");
                    analysisResult = ((Tensor<float>)_inferenceHandler.GetWorker().PeekOutput()).ReadbackAndCloneAsync();
                    readingBack = true;
                    analysisResult.GetAwaiter().OnCompleted(() => {
                        analysisResultTensor = analysisResult.GetAwaiter().GetResult();
                        readingBack = false;

                        var detectedObjects = PostProcess(analysisResultTensor);
                        analysisResultTensor.Dispose();
                        inferencePending = false;
                        T1.text = $"{detectedObjects[0].CocoName} detected with confidence {detectedObjects[0].Confidence}";
                        T2.text = $"{detectedObjects[1].CocoName} detected with confidence {detectedObjects[1].Confidence}";
                        T3.text = $"{detectedObjects[2].CocoName} detected with confidence {detectedObjects[2].Confidence}";
                    });
                }
            }
            finally
            {

                if (!inferencePending && analysisResultTensor != null)
                {
                    Debug.Log("Disposing of tensor");
                    analysisResultTensor.Dispose();
                    analysisResultTensor = null;
                    _inferenceHandler.DisposeTensors();
                }
            }

        }

        private List<DetectedObject> PostProcess(Tensor<float> result)
        {
            List<DetectedObject> objects = new();
            float widthScale = Size / _inputTexture.width;
            float heightScale = widthScale;

            if (_model.name.Contains("yolov10"))
            {
                for (int i = 0; i < result.shape[1]; i++)
                {
                    int cocoClass = (int)result[0, i, 5];
                    objects.Add(new DetectedObject(result[0, i, 0] * widthScale, result[0, i, 2] * heightScale, result[0, i, 1] * widthScale, result[0, i, 3] * heightScale, cocoClass, classes[cocoClass], result[0, i, 4]));
                }
            }
            else if (_model.name.Contains("yolo11"))
            {
                for (int i = 0; i < result.shape[2]; i++)
                {
                    (int cocoClass, float confidence) = FindMaxConfidence(result, i);
                    if (confidence < 0.3f) continue;
                    int centreX = (int)result[0, 0, i];
                    int centreY = (int)result[0, 1, i];
                    int width = (int)result[0, 2, i];
                    int height = (int)result[0, 3, i];

                    objects.Add(new DetectedObject(centreX - width / 2f, centreY - height / 2f, centreX + width / 2f, centreY + height / 2f, cocoClass, classes[cocoClass], confidence));
                }

                objects.OrderBy(x => -x.Confidence);
            }
            
            return objects;
        }

        private (int, float) FindMaxConfidence(Tensor<float> tensor, int cell)
        {
            const int classOffset = 4;
            int maxIndex = 0;
            float maxConfidence = float.MinValue;
            
            for (int i = classOffset; i < tensor.shape[1]; i++)
            {
                if (tensor[0, i, cell] > maxConfidence)
                {
                    maxConfidence = tensor[0, i, cell];
                    maxIndex = i;
                }
            }

            return (maxIndex-classOffset, maxConfidence);
        }
    }
}