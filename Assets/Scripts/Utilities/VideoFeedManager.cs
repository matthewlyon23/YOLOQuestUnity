using UnityEngine;
using UnityEngine.Events;

namespace YOLOQuestUnity.Utilities
{

    public abstract class VideoFeedManager : MonoBehaviour
    {
        public UnityEvent NewFrame { get; } = new UnityEvent();

        public abstract Texture2D GetTexture();

        public abstract FeedDimensions GetFeedDimensions();

    }

    public class FeedDimensions
    {
        private int _width;
        private int _height;

        public int Width { get => _width; }
        public int Height { get => _height; }

        public FeedDimensions(int width, int height)
        {
            _width = width;
            _height = height;
        }

    }

}