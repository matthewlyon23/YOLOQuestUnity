using System;
using System.Collections;
using Unity.Sentis;
using UnityEngine;
using YOLOQuestUnity.Inference;

namespace YOLOQuestUnity.ObjectDetection
{
    public class YOLOInferenceHandler : InferenceHandler<Texture2D>
    {
        private readonly TextureAnalyser _textureAnalyser;
        private readonly int _size;

        private readonly float _iouThreshold = 0.5f;
        private readonly float _scoreThreshold = 0.5f;


        [Obsolete("This constructor will be removed in future versions as it is missing new features and limits control. Please use YOLOInferenceHandler(ModelAsset, ref int, YOLOInferenceHandlerParameters) instead.")]
        public YOLOInferenceHandler(YOLOModelAsset modelAsset, ref int size, bool addClassificationHead)
        {
            _model = ModelLoader.Load(modelAsset.ModelAsset);

            if (_model.inputs[0].shape.Get(2) != -1) size = _model.inputs[0].shape.Get(2);
            _size = size;

            if (addClassificationHead) AddClassificationHead();

            _worker = new Worker(_model, BackendType.GPUCompute);
            _textureAnalyser = new TextureAnalyser(_worker);
        }

        public YOLOInferenceHandler(YOLOModelAsset modelAsset, ref int size, YOLOInferenceHandlerParameters parameters)
        {
            _model = ModelLoader.Load(modelAsset.ModelAsset);

            if (_model.inputs[0].shape.Get(2) != -1) size = _model.inputs[0].shape.Get(2);
            _size = size;

            _iouThreshold = parameters.IoUThreshold;
            _scoreThreshold = parameters.ScoreThreshold;

            if (parameters.AddClassificationHead) AddClassificationHead();

            if (parameters.QuantizeModel) ModelQuantizer.QuantizeWeights(parameters.QuantizationType, ref _model);

            _worker = new Worker(_model, parameters.BackendType);
            _textureAnalyser = new TextureAnalyser(_worker);
        }

        public override Awaitable<Tensor<float>> Run(Texture2D input)
        {
            //Texture2D inputTexture = new(input.width, input.height, input.format, false);
            //Graphics.CopyTexture(input, inputTexture);

            //ResizeTool.Resize(input, _size, _size, false, input.filterMode);

            return _textureAnalyser.AnalyseTexture(input, _size);
        }

        public override IEnumerator RunWithLayerControl(Texture2D input)
        {
            //Texture2D inputTexture = new(input.width, input.height, input.format, false);
            //Graphics.CopyTexture(input, inputTexture);

            //ResizeTool.Resize(input, _size, _size, false, input.filterMode);

            return _textureAnalyser.AnalyseTextureWithLayerControl(input, _size);
        }

        public override void DisposeTensors()
        {
            _textureAnalyser.OnDestroy();
        }

        public override void OnDestroy()
        {
            _worker.Dispose();
            _textureAnalyser.OnDestroy();
        }

        public override Tensor PeekOutput()
        {
            return _worker.PeekOutput();
        }

        private void AddClassificationHead()
        {
            var graph = new FunctionalGraph();

            var inputs = graph.AddInputs(_model);
            var output = Functional.Forward(_model, inputs)[0];

            /*
            Output Format

            1x84xN tensor

            Each of the N columns are a prediction. The first 0..4 rows of each column are the position and scale. The last 4..84 rows are the confidences of each of the 80 classes.

            */

            var centersToCornersData = new[]
            {
                1,      0,      1,      0,
                0,      1,      0,      1,
                -0.5f,  0,      0.5f,   0,
                0,      -0.5f,  0,      0.5f
            };
            var centersToCorners = Functional.Constant(new TensorShape(4, 4), centersToCornersData); // (4,4)
            var slicedClasses = output[.., 4..84, ..]; // (1,80,N)

            var argMaxClasses = Functional.ArgMax(slicedClasses, 1, false); // (1,N)
            var confidences = Functional.ReduceMax(slicedClasses, 1); // (1,N)
            var slicedPositions = output[.., 0..4, ..]; // (1,4,N)

            var boxCorners = Functional.MatMul(slicedPositions[0, .., ..].Transpose(0,1), centersToCorners); // (N,4)
            var indices = Functional.NMS(boxCorners, confidences[0,..], _iouThreshold, _scoreThreshold); // (N)

            var classIds = Functional.Gather(argMaxClasses, 1, indices.Unsqueeze(0)).Unsqueeze(1); // (1,1,N)
            var scores = Functional.Gather(confidences, 1, indices.Unsqueeze(0)).Unsqueeze(1); // (1,1,N)
            var coords = Functional.IndexSelect(slicedPositions, 2, indices); // (1,4,N)

            var concatenated = Functional.Concat(new FunctionalTensor[] { coords, classIds.Float(), scores }, 1); // (1,6,N)

            _model = graph.Compile(concatenated);
        }
    }

    public class YOLOInferenceHandlerParameters
    {
        public bool AddClassificationHead { get; private set; }

        public bool QuantizeModel { get; private set; }
        public QuantizationType QuantizationType { get; private set; } = QuantizationType.Float16;
        
        public float IoUThreshold;
        public float ScoreThreshold;
        
        public BackendType BackendType;

        public YOLOInferenceHandlerParameters(YOLOCustomization yoloCustomization = YOLOCustomization.AddClassificationHead, YOLOQuantizationType yoloQuantizationType = YOLOQuantizationType.None, float iouThreshold = 0.5f, float scoreThreshold = 0.5f, BackendType backendType = BackendType.GPUCompute)
        {
            AddClassificationHead = yoloCustomization == YOLOCustomization.AddClassificationHead;
            IoUThreshold = iouThreshold;
            ScoreThreshold = scoreThreshold;
            BackendType = backendType;

            switch (yoloQuantizationType)
            {
                case YOLOQuantizationType.Float16:
                    QuantizeModel = true;
                    QuantizationType = QuantizationType.Float16;
                    break;
                case YOLOQuantizationType.Uint8:
                    QuantizeModel = true;
                    QuantizationType = QuantizationType.Uint8;
                    break;
                default:
                    QuantizeModel = false;
                    break;
            }

        }
    }

    public class YOLOModelAsset
    {
        public ModelAsset ModelAsset { get; private set; }

        public YOLOModelAsset(ModelAsset modelAsset)
        {
            ModelAsset = modelAsset;
        }
    }

    public enum YOLOQuantizationType
    {
        None,
        Float16,
        Uint8
    }

    public enum YOLOCustomization
    { 
        AddClassificationHead,
        None
    }

}