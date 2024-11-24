using UnityEngine;
using Unity.Sentis;
using System.Collections;

namespace YOLOQuestUnity.ObjectDetection
{
    public class TextureAnalyser
    {
        private Worker _worker;
        private Tensor<float> _input;


        /**
         * <param name="worker">Sentis NN worker to use to analyse textures</param>
        */
        public TextureAnalyser(Worker worker)
        {
            _worker = worker;
        }


        /**
         * <summary>
         *  Analyses the given <paramref name="texture"/> and returns an <see cref="Awaitable"/> <see cref="Tensor"/> containing the potential result.
         * </summary>
         * <param name="texture">The texture to be analysed</param>
         * <returns><see cref="Awaitable"/> <see cref="Tensor"/> representing the potential result of running the NN on <paramref name="texture"/></returns>
         */
        public Awaitable<Tensor<float>> AnalyseTexture(Texture2D texture)
        { 
            TextureTransform textureTransform = new TextureTransform().SetChannelSwizzle().SetDimensions(640, 640, 3);
            _input = TextureConverter.ToTensor(texture, textureTransform);
        
            _worker.Schedule(_input);

            var tensor = _worker.PeekOutput() as Tensor<float>;
            var output = tensor.ReadbackAndCloneAsync();
            return output;
        }

        /**
         * <summary>
         *  Sets up an <see cref="IEnumerator"/> which represents the layers of execution of the NN on <paramref name="texture"/>.
         * </summary>
         * <param name="texture">The texture to be analysed</param>
         * <returns><see cref="IEnumerator"/> representing the layers of execution of the NN on <paramref name="texture"/></returns>
         */
        public IEnumerator AnalyseTextureWithLayerControl(Texture2D texture)
        {
            TextureTransform textureTransform = new TextureTransform().SetChannelSwizzle().SetDimensions(640, 640, 3);
            _input = TextureConverter.ToTensor(texture, textureTransform);

            var output =_worker.ScheduleIterable(_input);

            return output;
        }

        /**
         * <summary>Disposes of the internal copy of the last texture passed to either <see cref="AnalyseTextureWithLayerControl(Texture2D)"/> or <see cref="AnalyseTexture(Texture2D)"/></summary>
         */
        public void OnDestroy()
        {
            _input.Dispose();
        }





    }
}