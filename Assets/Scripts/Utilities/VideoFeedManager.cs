using UnityEngine;
using UnityEngine.Events;

namespace YOLOQuestUnity.Utilities
{

    public abstract class VideoFeedManager : MonoBehaviour
    {
        public UnityEvent NewFrame { get; } = new UnityEvent();

        public abstract Texture2D GetTexture();

    }

}