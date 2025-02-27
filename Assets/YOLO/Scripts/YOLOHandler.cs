using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using YOLOQuestUnity.Inference;
using YOLOQuestUnity.ObjectDetection;
using YOLOQuestUnity.Utilities;
using YOLOQuestUnity.Display;

namespace YOLOQuestUnity.YOLO
{
    public class YOLOHandler : MonoBehaviour
    {

        #region Inputs

        [Tooltip("The YOLO model to run.")]
        [SerializeField] private ModelAsset _model;
        [Tooltip("The VideoFeedManager to analyse frames from.")]
        public VideoFeedManager _YOLOCamera;
        [Tooltip("The number of model layers to run per frame. Increasing this value will decrease performance.")]
        [SerializeField] private uint _layersPerFrame = 10;
        [Tooltip("The size of the input image to the model. This will be overwritten if the model has a fixed input size.")]
        [SerializeField] private int InputSize = 640;
        [Tooltip("The number of model layers to run per frame. Increasing this value will decrease performance.")]
        [SerializeField] private uint _layersPerFrame = 10;
        [Tooltip("The threshold at which a detection is accepted.")]
        [SerializeField] private float _confidenceThreshold = 0.5f;
        [Tooltip("A JSON containing a mapping of class numbers to class names")]
        [SerializeField] private TextAsset _classJson;
        [Tooltip("The VideoFeedManager to analyse frames from.")]
        public VideoFeedManager _YOLOCamera;
        [Tooltip("The base camera for scene analysis")]
        [SerializeField] private Camera _referenceCamera;
        [Tooltip("The ObjectDisplayManager that will handle the spawning of digital double models.")]
        [SerializeField] private ObjectDisplayManager _displayManager;
        [Tooltip("The base camera for scene analysis")]
        [SerializeField] private Camera _referenceCamera;
        [Tooltip("The threshold at which a detection is accepted.")]
        [SerializeField] private float _confidenceThreshold = 0.5f;
        
        public Camera ReferenceCamera { get => _referenceCamera; private set => _referenceCamera = value; }

        #endregion

        #region InstanceFields

        private int Size = 640;
        private InferenceHandler<Texture2D> _inferenceHandler;
        private int _frameCount;

        private int FrameCount { get => _frameCount; set => _frameCount = value % 30; }
        private bool inferencePending = false;
        private bool readingBack = false;
        private Tensor<float> analysisResultTensor;
        private Texture2D _inputTexture;
        private IEnumerator splitInferenceEnumerator;

        private Camera _analysisCamera;

        #endregion

        #region Data

        private Dictionary<int, string> _classes = new();

        #endregion

        void Start()
        {
            _classes = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, string>>>(_classJson.text)["class"];
            _inferenceHandler = new YOLOInferenceHandler(_model, ref InputSize);
            if (_layersPerFrame <= 0) _layersPerFrame = 1;
            _analysisCamera = GetComponent<Camera>();
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
                    _analysisCamera.CopyFrom(ReferenceCamera);
                }
                if (inferencePending)
                {
                    int it = 0;
                    while (splitInferenceEnumerator.MoveNext()) if (++it % _layersPerFrame == 0) return;

                    readingBack = true;
                    analysisResultTensor = _inferenceHandler.PeekOutput() as Tensor<float>;
                    var analysisResult = analysisResultTensor.ReadbackAndCloneAsync().GetAwaiter();
                    analysisResult.OnCompleted(() =>
                    {
                        analysisResultTensor = analysisResult.GetResult();
                        readingBack = false;

                        var detectedObjects = YOLOPostProcessor.PostProcess(analysisResultTensor, _inputTexture, InputSize, _classes, _confidenceThreshold);
                        analysisResultTensor.Dispose();
                        _inferenceHandler.DisposeTensors();
                        inferencePending = false;

                        _displayManager.DisplayModels(detectedObjects, _analysisCamera);
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                analysisResultTensor.Dispose();
                analysisResultTensor = null;
                _inferenceHandler.DisposeTensors();
                inferencePending = false;
            }

            objects.Sort((x, y) => y.Confidence.CompareTo(x.Confidence));

            return objects;
        }
    }
}