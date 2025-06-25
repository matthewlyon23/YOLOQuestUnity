using UnityEngine;
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
        private Awaitable<RemoteYOLOResponse> m_pendingDetectedObjects;
        private Camera m_analysisCamera;

        static HttpClient client = new();


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
            if (YOLOCamera == null) return;

            if ((m_inputTexture = YOLOCamera.GetTexture()) == null) return;

            if (!m_inferencePending)
            {
                try
                {
                    m_pendingDetectedObjects = AnalyseImage(m_inputTexture);
                    m_pendingDetectedObjects.GetAwaiter().OnCompleted(async () =>
                    {
                        m_inferencePending = false;
                        var response = await m_pendingDetectedObjects;
                        Debug.Log("Inference Time: " + response.metadata.speed.inference);
                        m_objectDisplayManager.DisplayModels(Postprocess(response), m_analysisCamera);
                    });

                    m_inferencePending = true;
                    m_analysisCamera.CopyFrom(m_referenceCamera);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    m_inferencePending = false;
                }
                
            }
        }

        private async Awaitable<RemoteYOLOResponse> AnalyseImage(Texture2D texture)
        {
            var start = DateTime.Now;

            var imageData = texture.EncodeToJPG();

            using HttpRequestMessage request = new(HttpMethod.Post, m_remoteYOLOProcessorAddress);
            MultipartFormDataContent content = new();

            content.Add(new StringContent(m_YOLOFormat.ToString().ToLower()), "format");
            content.Add(new StringContent(m_YOLOModel.ToString().ToLower()), "model");
            content.Add(new ByteArrayContent(imageData), "image", "image.jpg");
            request.Content = content;
            
            using HttpResponseMessage response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode) throw new HttpRequestException();

            var responseString = await response.Content.ReadAsStringAsync();

            var res = JsonConvert.DeserializeObject<RemoteYOLOResponse>(responseString);

            var end = DateTime.Now;

            Debug.Log("Time for network: " + (end - start).TotalMilliseconds + "ms");

            return res;
        }

        private List<DetectedObject> Postprocess(RemoteYOLOResponse response)
        {
            List<DetectedObject> results = new();

            foreach (RemoteYOLOPredictionResult obj in response.result)
            {
                var cx = (obj.box.x1 + obj.box.x2) / 2;
                var cy = (obj.box.y1 + obj.box.y2) / 2;
                var width = (obj.box.x2 - obj.box.x1);
                var height = (obj.box.y2 - obj.box.y1);
                results.Add(new DetectedObject(cx, cy, width, height, obj.class_id, obj.name, obj.confidence));
            }

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