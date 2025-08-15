using System;
using System.Collections;
using System.IO;
using Unity.Sentis;
using UnityEngine;
using YOLOQuestUnity.Inference;
using YOLOQuestUnity.ObjectDetection.Utilities;

namespace YOLOQuestUnity.ObjectDetection
{
    public class YOLOInferenceHandler : InferenceHandler<Texture2D>
    {
        private readonly TextureAnalyser _textureAnalyser;
        private readonly int _size;

        private readonly float _iouThreshold = 0.5f;
        private readonly float _scoreThreshold = 0.5f;


        public YOLOInferenceHandler(YOLOModel model, ref int size, BackendType backendType = BackendType.GPUCompute)
        {
            _model = model.Model;

            if (_model.inputs[0].shape.Get(2) != -1) size = _model.inputs[0].shape.Get(2);
            _size = size;

            _worker = new Worker(_model, backendType);
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

    }
}