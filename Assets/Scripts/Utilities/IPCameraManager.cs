using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

namespace YOLOQuestUnity.Utilities
{
    public class IPCameraManager : VideoFeedManager
    {

        [SerializeField] private string _imageUrl = "http://ip:port/shot.jpg";
        [SerializeField] private bool _downloadAsImage = true;
        [SerializeField] private string username;
        [SerializeField] private string password;

        private UnityWebRequest _webRequest;
        private Texture2D _currentTexture;

        private void Update()
        {
            if (_webRequest == null) StartCoroutine(GetLatestImageFrame());
        }

        private IEnumerator GetLatestImageFrame()
        {
            _webRequest = UnityWebRequestTexture.GetTexture(_imageUrl);

            if (_imageUrl.StartsWith("https"))
            {
                var auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                _webRequest.SetRequestHeader("Authorization", auth);
                _webRequest.certificateHandler = new ForceCertificate();
            }
            
            yield return _webRequest.SendWebRequest();

            try
            {
                if (_webRequest.result != UnityWebRequest.Result.Success) throw new Exception("Web request failed");
                if (!_webRequest.GetResponseHeaders()["Content-Type"].StartsWith("image/")) throw new IPCameraException("Invalid URL: The resource is not an image");
                _currentTexture = DownloadHandlerTexture.GetContent(_webRequest);
                
                _webRequest = null;
            }
            catch (Exception e)
            {
                Debug.Log($"Response from: {_imageUrl} was {_webRequest.error}");
                Debug.Log(e);
            }
            finally
            {
                _webRequest = null;
            }
        }

        public override Texture2D GetTexture()
        {
            return _currentTexture;
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
