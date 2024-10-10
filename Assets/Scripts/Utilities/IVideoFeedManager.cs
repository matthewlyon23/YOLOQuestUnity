using UnityEngine;
using UnityEngine.Events;

namespace YOLOQuestUnity.Utilities
{
    public interface IVideoFeedManager
    {
        public UnityEvent NewFrame { get; }

        public Texture2D GetTexture();
    }

    public abstract class VideoFeedManager : MonoBehaviour, IVideoFeedManager
    {
        public UnityEvent NewFrame { get; }

        public abstract Texture2D GetTexture();

    }

}