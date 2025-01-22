using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using YOLOQuestUnity.Inference;
using YOLOQuestUnity.ObjectDetection;
using YOLOQuestUnity.Utilities;
using YOLOQuestUnity.YOLO.Display;

namespace YOLOQuestUnity.YOLO
{
    public class YOLOHandler : MonoBehaviour
    {

        #region Inputs

        [Tooltip("The size the input image will be converted to before running the model. Here for future use. Currently has no functionality.")]
        [SerializeField] private int Size = 640;
        [Tooltip("The YOLO model to run.")]
        [SerializeField] private ModelAsset _model;
        [Tooltip("The VideoFeedManager to analyse frames from.")]
        [SerializeField] private VideoFeedManager _YOLOCamera;
        [Tooltip("The number of model layers to run per frame. Increases this value will decrease performance.")]
        [SerializeField] private uint _layersPerFrame = 10;
        [Tooltip("A JSON containing a mapping of class numbers to class names")]
        [SerializeField] private TextAsset _classJson;
        [Tooltip("The ObjectDisplayManager that will handle the spawning of digital double models.")]
        [SerializeField] private ObjectDisplayManager _displayManager;
        [Tooltip("The base camera for scene analysis")]
        [SerializeField] private Camera _referenceCamera;
        
        public Camera ReferenceCamera { get => _referenceCamera; set => _referenceCamera = value; }

        #endregion

        #region InstanceFields

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

        #region Debugging

        [SerializeField] private TextMeshProUGUI T1;
        [SerializeField] private TextMeshProUGUI T2;
        [SerializeField] private TextMeshProUGUI T3;

        [SerializeField] private Slider slider;

        private void SetLayersPerFrame(float i)
        {
            _layersPerFrame = (uint)i;
        }

        [SerializeField] Texture2D _tempTexture;
        [SerializeField] RawImage _cameraDisplay;

        #endregion


        void Start()
        {
            var classJsonString = _classJson.text;
            _classes = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, string>>>(classJsonString)["class"];
            _inferenceHandler = new YOLOInferenceHandler(_model, 640);
            if (_layersPerFrame == 0) _layersPerFrame = 1;
            slider.onValueChanged.AddListener(SetLayersPerFrame);
            _analysisCamera = GetComponent<Camera>();
        }

        void Update()
        {
            if (_YOLOCamera.GetTexture() != null && _cameraDisplay != null) _cameraDisplay.texture = _YOLOCamera.GetTexture();
            
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
                    Debug.Log($"Analysis camera position: {_analysisCamera.transform.position}");
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

                        var detectedObjects = PostProcess(analysisResultTensor);
                        analysisResultTensor.Dispose();
                        _inferenceHandler.DisposeTensors();
                        inferencePending = false;

                        _displayManager.DisplayModels(detectedObjects, _analysisCamera);

                        //if (detectedObjects.Count > 2)
                        //{
                        //    T1.text = $"{detectedObjects[0].CocoName} detected with confidence {detectedObjects[0].Confidence}";
                        //    T2.text = $"{detectedObjects[1].CocoName} detected with confidence {detectedObjects[1].Confidence}";
                        //    T3.text = $"{detectedObjects[2].CocoName} detected with confidence {detectedObjects[2].Confidence}";
                        //}

                        //foreach (var detectedObject in detectedObjects)
                        //{
                        //    var boundingBox = detectedObject.BoundingBox;
                        //    for (int i =0; i < boundingBox.)
                        //} 
                    });

                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.Log("Disposing of tensor");
                analysisResultTensor.Dispose();
                analysisResultTensor = null;
                _inferenceHandler.DisposeTensors();
            }

        }

        private List<DetectedObject> PostProcess(Tensor<float> result)
        {
            Profiler.BeginSample("Postprocessing");

            List<DetectedObject> objects = new();
            float widthScale = _inputTexture.width / (float)Size;
            float heightScale = _inputTexture.height / (float)Size;

            for (int i = 0; i < result.shape[2]; i++)
            {
                float confidence = result[0, 5, i];
                if (confidence < 0.7f) continue;
                int cocoClass = (int)result[0, 4, i];
                float centreX = result[0, 0, i] * widthScale;
                float centreY = result[0, 1, i] * heightScale;
                float width = result[0, 2, i] * widthScale;
                float height = result[0, 3, i] * heightScale;

                objects.Add(new DetectedObject(centreX, centreY, width, height, cocoClass, _classes[cocoClass], confidence));
            }

            objects.Sort((x, y) => y.Confidence.CompareTo(x.Confidence));


            Profiler.EndSample();

            return objects;
        }
    }
}