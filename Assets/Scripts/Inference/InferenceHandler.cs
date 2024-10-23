using System.Collections;
using Unity.Sentis;
using UnityEngine;

namespace YOLOQuestUnity.Inference
{
    public abstract class InferenceHandler<T>
    {
        protected Model _model;
        protected Worker _worker;

        public abstract Awaitable<Tensor<float>> Run(T input);

        public abstract IEnumerator RunWithLayerControl(T input);

        public abstract Worker GetWorker();

        public abstract void DisposeTensors();

        public abstract void OnDestroy();
    }
}