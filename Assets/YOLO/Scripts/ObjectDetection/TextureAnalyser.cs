using UnityEngine;
using Unity.Sentis;
using System.Collections;

namespace YOLOQuestUnity.ObjectDetection
{
    public class TextureAnalyser
    {
        private Worker _worker;
        private Tensor<float> _input;

        public TextureAnalyser(Worker worker)
        {
            _worker = worker;
        }

        public Awaitable<Tensor<float>> AnalyseTexture(Texture2D texture)
        {

            Texture2D inputTexture = new(texture.width, texture.height, texture.format, false);
            Graphics.CopyTexture(texture, inputTexture);

            TextureTransform textureTransform = new TextureTransform().SetChannelSwizzle().SetDimensions(-1, -1, 3);
            _input = TextureConverter.ToTensor(inputTexture, textureTransform);
        
            _worker.Schedule(_input);

            _input.Dispose();

            var tensor = _worker.PeekOutput() as Tensor<float>;
            var output = tensor.ReadbackAndCloneAsync();
            return output;
        }

        public IEnumerator AnalyseTextureWithLayerControl(Texture2D texture)
        {
            Texture2D inputTexture = new(texture.width, texture.height, texture.format, false);
            Graphics.CopyTexture(texture, inputTexture);

            TextureTransform textureTransform = new TextureTransform().SetChannelSwizzle().SetDimensions(-1, -1, 3);
            _input = TextureConverter.ToTensor(inputTexture, textureTransform);

            var output =_worker.ScheduleIterable(_input);

            _input.Dispose();

            return output;
        }





    }
}