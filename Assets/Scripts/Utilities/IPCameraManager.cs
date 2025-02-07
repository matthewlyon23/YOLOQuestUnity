using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

namespace YOLOQuestUnity.Utilities
{
    public class IPCameraManager : VideoFeedManager
    {

        [SerializeField] protected string _imageUrl = "http://ip:port/shot.jpg";
        [SerializeField] protected bool _downloadAsImage = true;
        [SerializeField] protected string username;
        [SerializeField] protected string password;

        protected UnityWebRequest _webRequest;
        protected Texture2D _currentTexture;


        protected bool _gettingFrame = false;

        protected void Update()
        {
            if (!_gettingFrame)
            {
                _gettingFrame = true;
                StartCoroutine(GetLatestImageFrame());
            }
        }

        protected IEnumerator GetLatestImageFrame()
        {
            _webRequest = UnityWebRequestTexture.GetTexture(_imageUrl);

            var auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _webRequest.SetRequestHeader("Authorization", auth);
            _webRequest.certificateHandler = new ForceCertificate();


            yield return _webRequest.SendWebRequest();

            Texture2D tempTexture = null;

            try
            {
                if (_webRequest.result != UnityWebRequest.Result.Success) throw new Exception("Web request failed");
                if (!_webRequest.GetResponseHeaders()["Content-Type"].StartsWith("image/")) throw new IPCameraException("Invalid URL: The resource is not an image");
                tempTexture = DownloadHandlerTexture.GetContent(_webRequest);
                if (_currentTexture != null) Destroy(_currentTexture);
                _currentTexture = tempTexture;
            }
            catch (Exception e)
            {
                Debug.Log($"Response from: {_imageUrl} was {_webRequest.error}");
                Debug.Log(e);
            }
            finally
            {
                _webRequest = null;
                _gettingFrame = false;
            }
        }

        public override Texture2D GetTexture()
        {
            return _currentTexture;
        }

        public override FeedDimensions GetFeedDimensions()
        {
            return _currentTexture == null ? new FeedDimensions(1024, 1024) : new FeedDimensions(_currentTexture.width, _currentTexture.height);
        }
    }

    class IPCameraException : Exception
    {
        public IPCameraException() : base() { }
        public IPCameraException(string message) : base(message) { }
        public IPCameraException(string message, Exception inner) : base(message, inner) { }
    }

    class ForceCertificate : CertificateHandler
    {
        public ForceCertificate() { }

        protected override bool ValidateCertificate(byte[] input)
        {
            return true;
        }
    }
}
