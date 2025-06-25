using UnityEngine;
using UnityEngine.Networking;
using MyBox;
using System.Collections.Generic;
using Newtonsoft.Json;
using YOLOQuestUnity.ObjectDetection;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using YOLOQuestUnity.Utilities;
using YOLOQuestUnity.Display;
using System;
using System.Net.Http;
using UnityEngine.Android;

namespace YOLOQuestUnity.YOLO
{
    public class RemoteYOLOHandler : MonoBehaviour
    {

        [MustBeAssigned] [SerializeField] private string m_remoteYOLOProcessorAddress;
        [SerializeField] private YOLOFormat m_YOLOFormat;
        [SerializeField] private YOLOModel m_YOLOModel;
        [Space(40)]
        [MustBeAssigned]
        [SerializeField] private ObjectDisplayManager m_objectDisplayManager;
        [MustBeAssigned] public VideoFeedManager YOLOCamera;
        [MustBeAssigned]
        [SerializeField] private Camera m_referenceCamera; 


        private Texture2D m_inputTexture;
        private bool m_inferencePending = false;
        private Awaitable<List<DetectedObject>> m_pendingDetectedObjects;
        private Camera m_analysisCamera;


        private void Start()
        {

            if (Application.platform == RuntimePlatform.Android)
            {
                Permission.RequestUserPermission("internet");
            }

            m_analysisCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            if (YOLOCamera is null) return;

            if ((m_inputTexture = YOLOCamera.GetTexture()) is null) return;

            if (!m_inferencePending)
            {
                try
                {
                    AnalyseImage(m_inputTexture).GetAwaiter().OnCompleted(() =>
                    {
                        m_inferencePending = false;
                        m_objectDisplayManager.DisplayModels(m_pendingDetectedObjects.GetAwaiter().GetResult(), m_analysisCamera);
                    });
                    m_inferencePending = true;
                    m_analysisCamera.CopyFrom(m_referenceCamera);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                
            }
        }

        public async Awaitable<List<DetectedObject>> AnalyseImage(Texture2D texture)
        {
            var start = DateTime.Now;

            var imageData = texture.EncodeToJPG();

            HttpClient client = new();
            HttpRequestMessage request = new();
            using MultipartFormDataContent content = new();

            content.Add(new StringContent("format"), m_YOLOFormat.ToString().ToLower());
            content.Add(new StringContent("model"), m_YOLOModel.ToString().ToLower());
            content.Add(new ByteArrayContent(imageData), "image", "image.jpg");
            request.Content = content;
            
            HttpResponseMessage response = await client.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                Debug.Log($"{response.StatusCode}, {response.Content.ReadAsStringAsync().Result}");
                return new List<DetectedObject>();
            }

            var responseString = await response.Content.ReadAsStringAsync();

            var res = JsonConvert.DeserializeObject<RemoteYOLOResponse>(responseString);


            List<DetectedObject> results = new();
            foreach(RemoteYOLOPredictionResult obj in res.result)
            {
                var cx = (obj.box.x1 + obj.box.x2) / 2;
                var cy = (obj.box.y1 + obj.box.y2) / 2;
                var width = (obj.box.x2 - obj.box.x1);
                var height = (obj.box.y2 - obj.box.y1);
                results.Add(new DetectedObject(cx, cy, width, height, obj.class_id, obj.name, obj.confidence));
            }


            //MultipartFormDataSection format = new("format", m_YOLOFormat.ToString().ToLower());
            //MultipartFormDataSection model = new("model", m_YOLOModel.ToString().ToLower());
            //MultipartFormFileSection image = new("image", imageData);

            //DownloadHandler downloadHandler = new DownloadHandlerBuffer();
            //UploadHandler uploadHandler = new UploadHandlerRaw(UnityWebRequest.SerializeFormSections(new List<IMultipartFormSection>() { format, model, image }, UnityWebRequest.GenerateBoundary()));

            //UnityWebRequest request = new UnityWebRequest(m_remoteYOLOProcessorAddress, UnityWebRequest.kHttpVerbPOST, downloadHandler, uploadHandler);
            //await request.SendWebRequest();

            //List<DetectedObject> results = new();

            //Debug.Log($"RemoteYOLO: {request.result}, {request.responseCode}, {request.downloadHandler.text}");

            //if (request.result == UnityWebRequest.Result.Success)
            //{
            //    var response = ((DownloadHandlerBuffer)request.downloadHandler).text;
            //    JsonSerializer serializer = new JsonSerializer();
            //    var result = serializer.Deserialize<RemoteYOLOResponse>(new JsonTextReader(new System.IO.StringReader(response)));
            //    foreach (RemoteYOLOPredictionResult obj in result.result)
            //    {
            //        var cx = (obj.box.x1 + obj.box.x2) / 2;
            //        var cy = (obj.box.y1 + obj.box.y2) / 2;
            //        var width = (obj.box.x2 - obj.box.x1);
            //        var height = (obj.box.y2 - obj.box.y1);
            //        results.Add(new DetectedObject(cx, cy, width, height, obj.class_id, obj.name, obj.confidence));
            //    }
            //}

            var end = DateTime.Now;

            Debug.Log("Time for network: " + (end - start).TotalMilliseconds + "ms");

            return results;
        }

        private enum YOLOFormat
        {
            NCNN,
            ONNX,
            PYTORCH
        }

        private enum YOLOModel
        {
            YOLO11N,
            YOLO11S,
            YOLO11M,
            YOLO11L
        }

        private class RemoteYOLOResponse
        {
            public bool success;
            public RemoteYOLOMetadata metadata;
            public RemoteYOLOPredictionResult[] result;
        }

        private class RemoteYOLOMetadata
        {
            public Dictionary<int, string> names;
            public RemoteYOLOSpeedMetadata speed;
        }

        private class RemoteYOLOSpeedMetadata
        {
            public float preprocess;
            public float inference;
            public float postprocess;
        }

        private class RemoteYOLOPredictionResult
        {
            public string name;
            public int class_id;
            public float confidence;
            public RemoteYOLOResultBox box;

            [JsonExtensionData]
            private IDictionary<string, JToken> m_additionalData;

            [OnDeserialized]
            private void OnDeserialized(StreamingContext context)
            {
                class_id = (int)m_additionalData["class"];
            }

        }

        private struct RemoteYOLOResultBox
        {
            public float x1;
            public float y1;
            public float x2;
            public float y2;
        }

    }

       
}