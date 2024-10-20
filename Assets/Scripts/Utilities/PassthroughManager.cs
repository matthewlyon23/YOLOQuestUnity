using Trev3d.Quest.ScreenCapture;
using UnityEngine;
using UnityEngine.Events;

namespace YOLOQuestUnity.Utilities
{
    public class PassthroughManager : VideoFeedManager
    {
        [SerializeField] private QuestScreenCaptureTextureManager _screenCaptureTextureManager;
        private Texture2D _currentTexture;

        public UnityEvent ScreenCaptureStart = new();

        private void Start()
        {
            _screenCaptureTextureManager.OnNewFrame.AddListener(OnNewFrame);
            _screenCaptureTextureManager.OnScreenCaptureStarted.AddListener(OnScreenCaptureStart);
        }

        public override Texture2D GetTexture()
        {
            return _currentTexture;
        }

        private void OnNewFrame()
        {
            _currentTexture = _screenCaptureTextureManager.ScreenCaptureTexture;
            NewFrame.Invoke();
        }
        private void OnScreenCaptureStart() => ScreenCaptureStart.Invoke();
    }
}

