using System;
using System.Collections;
using Unity.Sentis;
using UnityEngine;
using YOLOQuestUnity.Inference;
using YOLOQuestUnity.Utilities;

namespace YOLOQuestUnity.ObjectDetection
{
    public class YOLOInferenceHandler : InferenceHandler<Texture2D>
    {
        private TextureAnalyser _textureAnalyser;
        private int _size;

        public YOLOInferenceHandler(ModelAsset modelAsset, out int size)
        {
            _model = ModelLoader.Load(modelAsset);

            size = _model.inputs[0].shape.Get(2);
            _size = size;

            if (modelAsset.name.Contains("yolo11"))
            {
                var graph = new FunctionalGraph();

                var inputs = graph.AddInputs(_model);
                var outputs = Functional.Forward(_model, inputs);
                
                var slicedClasses = outputs[0][.., 4..84, ..];
                var argMaxClasses = Functional.ArgMax(slicedClasses, 1, true);
                var confidences = Functional.Gather(slicedClasses, 1, argMaxClasses);
                var slicedPositions = outputs[0][.., 0..4, ..];
                var concatenated = Functional.Concat(new FunctionalTensor[] { slicedPositions, argMaxClasses.Float(), confidences }, 1);

                _model = graph.Compile(concatenated);
            }

            _worker = new Worker(_model, BackendType.GPUCompute);
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