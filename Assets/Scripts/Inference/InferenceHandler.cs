using System.Collections;
using Unity.Sentis;
using UnityEngine;

namespace YOLOQuestUnity.Inference
{
    public abstract class InferenceHandler<T>
    {
        protected Model _model;
        protected Worker _worker;

        /**
         * <summary>Analyses the provided <see cref="T"/> <paramref name="input"/> using the <see cref="Model"/> represented by the <see cref="ModelAsset"/> provided.</summary>
         * <param name="input">The texture to run the inference on.</param>
         * <returns>An <see cref="Awaitable"/> <see cref="Tensor"/> representing the potential output of running the NN model on the <paramref name="input"/></returns>
         */
        public abstract Awaitable<Tensor<float>> Run(T input);

        /**
         * <summary>Sets up the analysis on the <see cref="T"/> <paramref name="input"/> and returns the <see cref="IEnumerator"/> representing the execution of the layers of the NN <see cref="Model"/> for that input.</summary>
         * <param name="input">The texture to set-up the inference on.</param>
         * <returns>An <see cref="IEnumerator"/> representing the layers of the NN model run on the <paramref name="input"/></returns>
         */
        public abstract IEnumerator RunWithLayerControl(T input);

        /**
         * <summary>Returns a reference to the result of the internal <see cref="Worker"/> on the GPU.</summary>
         * <returns>The <see cref="Tensor"/> result of the NN on the GPU.</returns>
         */
        public abstract Tensor PeekOutput();

        /**
         * <summary>Dispose of any internal <see cref="Tensor"/>s as defined or used by implementing classes.</summary>
         */
        public abstract void DisposeTensors();

        /**
         * <summary>Defines actions to perform when an object of the class is destroyed.</summary>
         */
        public abstract void OnDestroy();
    }
}