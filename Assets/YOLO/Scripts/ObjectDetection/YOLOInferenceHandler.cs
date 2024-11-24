using System.Collections;
using Unity.Sentis;
using UnityEngine;
using YOLOQuestUnity.Inference;

namespace YOLOQuestUnity.ObjectDetection
{
    public class YOLOInferenceHandler : InferenceHandler<Texture2D>
    {
        private TextureAnalyser _textureAnalyser;
        private int _size;

        /**
         * <summary>Creates a <see cref="YOLOInferenceHandler"/>, takes a YOLO11/8 style YOLO model and appends concatenation and max finding to the model.</summary>
         * <param name="modelAsset">The <see cref="ModelAsset"/> of the YOLO model that will be used to perform inference.</param>
         */
        public YOLOInferenceHandler(ModelAsset modelAsset, int size)
        {
            _size = size;
            _model = ModelLoader.Load(modelAsset);
            
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

        /**
         * <summary>Analyses the provided texture <paramref name="input"/> using the <see cref="Model"/> represented by the <see cref="ModelAsset"/> provided.</summary>
         * <param name="input">The texture to run the inference on.</param>
         * <returns>An <see cref="Awaitable"/> <see cref="Tensor"/> representing the potential output of running the YOLO model on the <paramref name="input"/></returns>
         */
        public override Awaitable<Tensor<float>> Run(Texture2D input)
        {
            //Texture2D inputTexture = new(input.width, input.height, input.format, false);
            //Graphics.CopyTexture(input, inputTexture);

            //ResizeTool.Resize(input, _size, _size, false, input.filterMode);

            return _textureAnalyser.AnalyseTexture(input);
        }

        /**
         * <summary>Analyses the provided texture <paramref name="input"/> using the <see cref="Model"/> represented by the <see cref="ModelAsset"/> provided.</summary>
         * <param name="input">The texture to run the inference on.</param>
         * <returns>An <see cref="Awaitable"/> <see cref="Tensor"/> representing the potential output of running the YOLO model on the <paramref name="input"/></returns>
         */
        public override IEnumerator RunWithLayerControl(Texture2D input)
        {
            //Texture2D inputTexture = new(input.width, input.height, input.format, false);
            //Graphics.CopyTexture(input, inputTexture);

            //ResizeTool.Resize(input, _size, _size, false, input.filterMode);

            return _textureAnalyser.AnalyseTextureWithLayerControl(input);
        }

        /**
         * <summary>Disposes of the <see cref="Tensor"/>s stored in the internal <see cref="TextureAnalyser"/></summary>
         */
        public override void DisposeTensors()
        {
            _textureAnalyser.OnDestroy();
        }

        /**
         * <summary>Disposes of the internal worker and calls <see cref="TextureAnalyser.OnDestroy"/> on the internal <see cref="TextureAnalyser"/>.</summary>
         */
        public override void OnDestroy()
        {
            _worker.Dispose();
            _textureAnalyser.OnDestroy();
        }

        /**
         * <summary>Returns the result of <see cref="Worker.PeekOutput()"/> on the internal worker.</summary>
         * <returns>The <see cref="Tensor"/> result of the internal <see cref="Worker"/> on the GPU.</returns>
         */
        public override Tensor PeekOutput()
        {
            return _worker.PeekOutput();
        }
    }
}