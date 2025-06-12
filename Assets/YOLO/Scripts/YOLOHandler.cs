using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using YOLOQuestUnity.Inference;
using YOLOQuestUnity.ObjectDetection;
using YOLOQuestUnity.Utilities;
using YOLOQuestUnity.Display;
using System.Text;
using System.Linq;

namespace YOLOQuestUnity.YOLO
{
    public class YOLOHandler : MonoBehaviour
    {

        #region Inputs

        [Tooltip("The YOLO model to run.")]
        [SerializeField] private ModelAsset _model;
        [Tooltip("Add a classification head to the model to select the most likely class for each detection.")]
        [SerializeField] private bool _addClassificationHead = false;
        [Tooltip("The size of the input image to the model. This will be overwritten if the model has a fixed input size.")]
        [SerializeField] private int InputSize = 640;
        [Tooltip("The number of model layers to run per frame. Increasing this value will decrease performance.")]
        [SerializeField] private uint _layersPerFrame = 10;
        [Tooltip("The threshold at which a detection is accepted.")]
        [SerializeField] private float _confidenceThreshold = 0.5f;
        [Tooltip("A JSON containing a mapping of class numbers to class names")]
        [SerializeField] private TextAsset _classJson;
        [Tooltip("The VideoFeedManager to analyse frames from.")]
        public VideoFeedManager YOLOCamera;
        [Tooltip("The base camera for scene analysis")]
        [SerializeField] private Camera _referenceCamera;
        [Tooltip("The ObjectDisplayManager that will handle the spawning of digital double models.")]
        [SerializeField] private ObjectDisplayManager _displayManager;
        
        public Camera ReferenceCamera { get => _referenceCamera; private set => _referenceCamera = value; }

        #endregion

        #region InstanceFields

        private InferenceHandler<Texture2D> _inferenceHandler;

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
            _inferenceHandler = new YOLOInferenceHandler(_model, ref InputSize, _addClassificationHead);
            if (_layersPerFrame <= 0) _layersPerFrame = 1;
            _analysisCamera = GetComponent<Camera>();
        }

        void Update()
        {            
            if (_inferenceHandler is null) return;

            if (YOLOCamera is null) return;

            if (readingBack) return;

            try
            {
                if (!inferencePending)
                {
                    if ((_inputTexture = YOLOCamera.GetTexture()) == null) return;
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
                        try
                        {

                        analysisResultTensor = analysisResult.GetResult();
                        readingBack = false;

                        var detectedObjects = YOLOPostProcessor.PostProcess(analysisResultTensor, _inputTexture, InputSize, _classes, _confidenceThreshold);
                        analysisResultTensor.Dispose();
                        _inferenceHandler.DisposeTensors();
                        inferencePending = false;
                        analysisResultTensor = null;

                        _displayManager.DisplayModels(detectedObjects, _analysisCamera);
                        }
                        catch
                        {
                            analysisResultTensor?.Dispose();
                            analysisResultTensor = null;
                            inferencePending = false;
                            _inferenceHandler.DisposeTensors();
                        }
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
        }
    }
}