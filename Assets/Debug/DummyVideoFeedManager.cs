using UnityEngine;
using YOLOQuestUnity.Utilities;
using MyBox;

namespace YOLOQuestUnity.Debug
{
    public class DummyVideoFeedManager : VideoFeedManager
    {
        [MustBeAssigned][SerializeField] private Texture2D dummyImage;

        public override FeedDimensions GetFeedDimensions()
        {
            return new FeedDimensions(dummyImage.width, dummyImage.height);
        }

        public override Texture2D GetTexture()
        {
            return dummyImage;
        }
    }
}
