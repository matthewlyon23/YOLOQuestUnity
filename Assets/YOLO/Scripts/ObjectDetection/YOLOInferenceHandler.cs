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

        public YOLOInferenceHandler(ModelAsset modelAsset, int size)
        {
            _size = size;
            _model = ModelLoader.Load(modelAsset);
            _worker = new Worker(_model, BackendType.GPUCompute);
            _textureAnalyser = new TextureAnalyser(_worker);
        }

        public override Worker GetWorker()
        {
            return _worker;
        }

        public override Awaitable<Tensor<float>> Run(Texture2D input)
        {
            Texture2D inputTexture = new(input.width, input.height, input.format, false);
            Graphics.CopyTexture(input, inputTexture);

            ResizeTool.Resize(inputTexture, _size, _size, false, input.filterMode);

            return _textureAnalyser.AnalyseTexture(inputTexture);

        }

        public override IEnumerator RunWithLayerControl(Texture2D input)
        {
            Texture2D inputTexture = new(input.width, input.height, input.format, false);
            Graphics.CopyTexture(input, inputTexture);

            ResizeTool.Resize(inputTexture, _size, _size, false, input.filterMode);

            return _textureAnalyser.AnalyseTextureWithLayerControl(inputTexture);
        }
    }
}