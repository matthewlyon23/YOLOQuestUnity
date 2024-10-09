using Unity.Sentis;

namespace YOLOQuestUnity.Inference
{
    public abstract class InferenceHandler<T>
    {
        protected Model _model;
        protected Worker _worker;

        protected abstract Tensor<float> Run(T input);
    }
}