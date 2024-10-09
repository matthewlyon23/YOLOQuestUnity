using UnityEngine;
using Unity.Sentis;

namespace YOLOQuestUnity.ObjectDetection
{
    public class TextureAnalyser
    {
        private Texture2D _texture;
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

            _input = TextureConverter.ToTensor(inputTexture);
            _input = _input.ReadbackAndClone();
        
            _worker.Schedule(_input);

            _input.Dispose();

            var tensor = _worker.PeekOutput() as Tensor<float>;
            var output = tensor.ReadbackAndCloneAsync();
            return output;
        }





    }
}