using System.Collections;
using Unity.InferenceEngine;
using UnityEngine;

namespace YOLOTools.Inference
{
    public abstract class InferenceHandler<T>
    {
        protected Unity.InferenceEngine.Model _model;
        protected Unity.InferenceEngine.Worker _worker;

        public abstract Awaitable<Unity.InferenceEngine.Tensor<float>> Run(T input);

        public abstract IEnumerator RunWithLayerControl(T input);

        public abstract Tensor PeekOutput();

        public abstract void DisposeTensors();

        public abstract void OnDestroy();
    }
}