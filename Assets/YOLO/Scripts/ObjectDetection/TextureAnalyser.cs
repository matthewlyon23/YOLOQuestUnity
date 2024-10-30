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
            TextureTransform textureTransform = new TextureTransform().SetChannelSwizzle().SetDimensions(640, 640, 3);
            _input = TextureConverter.ToTensor(texture, textureTransform);
        
            _worker.Schedule(_input);

            var tensor = _worker.PeekOutput() as Tensor<float>;
            var output = tensor.ReadbackAndCloneAsync();
            return output;
        }

        public IEnumerator AnalyseTextureWithLayerControl(Texture2D texture)
        {
            TextureTransform textureTransform = new TextureTransform().SetChannelSwizzle().SetDimensions(640, 640, 3);
            _input = TextureConverter.ToTensor(texture, textureTransform);

            var output =_worker.ScheduleIterable(_input);

            return output;
        }

        public void OnDestroy()
        {
            _input.Dispose();
        }





    }
}