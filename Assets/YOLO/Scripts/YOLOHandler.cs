using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Sentis;
using UnityEngine;
using YOLOQuestUnity.Inference;
using YOLOQuestUnity.ObjectDetection;
using YOLOQuestUnity.Utilities;
using YOLOQuestUnity.Display;
using UnityEngine.SceneManagement;

namespace YOLOQuestUnity.YOLO
{
    public class YOLOHandler : MonoBehaviour
    {

        #region Inputs

        [Tooltip("The YOLO model to run.")]
        [SerializeField] private ModelAsset _model;
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

        #region Evaluation


        private long _YOLOInferenceStartTime;
        private long _CaptureTime;
        private long _DetectedTime;
        private readonly List<long> _YOLOInferenceTimes = new();
        private readonly List<long> _CaptureTimes = new();
        private readonly List<long> _DetectedTimes = new();

        private long GetMillisecondsElapsed(long start)
        {
            return ((ProfilerUnsafeUtility.Timestamp - start) * ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio.Numerator / ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio.Denominator) / 1000000;
        }

        void OnApplicationQuit()
        {
            if (_YOLOInferenceTimes.Count == 0) return;
            using (StreamWriter file = File.AppendText(Path.Combine(Application.persistentDataPath, $"CustomProfileData_{((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds()}.csv")))
            {
                for (int i = 0; i < _YOLOInferenceTimes.Count; i++)
                {
                    file.WriteLine($"{_YOLOInferenceTimes[i]},{_CaptureTimes[i]},{_DetectedTimes[i]}");
                }
            }
            _YOLOInferenceTimes.Clear();
            _CaptureTimes.Clear();
            _DetectedTimes.Clear();
        }

        private void OnApplicationPause()
        {
            OnApplicationQuit();
        }

        #endregion

        void Start()
        {
            _classes = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, string>>>(_classJson.text)["class"];
            _inferenceHandler = new YOLOInferenceHandler(_model, ref InputSize);
            if (_layersPerFrame <= 0) _layersPerFrame = 1;
            _analysisCamera = GetComponent<Camera>();

            _YOLOCamera = GameObject.FindGameObjectWithTag("VideoFeedManager").GetComponent<VideoFeedManager>();
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
                    #region Evaluation

                    int sceneToLoad;
                    if ((sceneToLoad = SceneController.NextScene(1)) != -1)
                    {
                        Debug.Log("Loading scene " + sceneToLoad);
                        _inferenceHandler.OnDestroy();
                        SceneManager.LoadScene(sceneToLoad);
                        return;
                    }
                    
                    #endregion

                    _CaptureTime = ProfilerUnsafeUtility.Timestamp;
                    if ((_inputTexture = _YOLOCamera.GetTexture()) == null)
                    {
                        Debug.LogWarning("No texture available");
                        return;
                    }
                    _YOLOInferenceStartTime = ProfilerUnsafeUtility.Timestamp;
                    splitInferenceEnumerator = _inferenceHandler.RunWithLayerControl(_inputTexture);
                    inferencePending = true;
                    _analysisCamera.CopyFrom(ReferenceCamera);
                }
                if (inferencePending)
                {
                    int it = 0;
                    while (splitInferenceEnumerator.MoveNext()) if (++it % _layersPerFrame == 0) return;

                    _DetectedTime = ProfilerUnsafeUtility.Timestamp;
                    long yoloInferenceTime = GetMillisecondsElapsed(_YOLOInferenceStartTime);


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
                        analysisResultTensor = null;

                        _displayManager.DisplayModels(detectedObjects, _analysisCamera);

                        long detectedToDisplayTime = GetMillisecondsElapsed(_DetectedTime);
                        long captureToDisplayTime = GetMillisecondsElapsed(_CaptureTime);

                        Debug.Log("Time to run YOLO inference: " + yoloInferenceTime + "ms");
                        Debug.Log("Time from detection to display: " + detectedToDisplayTime + "ms");
                        Debug.Log("Time from capture to display: " + captureToDisplayTime + "ms");

                        if (_YOLOInferenceTimes.Count == 100000)
                        {
                            _YOLOInferenceTimes.RemoveAt(0);
                            _DetectedTimes.RemoveAt(0);
                            _CaptureTimes.RemoveAt(0);
                        }

                        _YOLOInferenceTimes.Add(yoloInferenceTime);
                        _DetectedTimes.Add(detectedToDisplayTime);
                        _CaptureTimes.Add(captureToDisplayTime);
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