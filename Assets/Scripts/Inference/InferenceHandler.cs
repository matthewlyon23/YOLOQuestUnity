using Unity.Sentis;
using UnityEngine;

namespace YOLOQuestUnity.Inference
{
    public abstract class InferenceHandler<T>
    {
        protected Model _model;
        protected Worker _worker;

        public abstract Awaitable<Tensor<float>> Run(T input);
    }
}