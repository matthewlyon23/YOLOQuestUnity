using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MyBox;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Experimental.Rendering;
using YOLOQuestUnity.Display;
using YOLOQuestUnity.ObjectDetection;
using YOLOQuestUnity.Utilities;

namespace YOLOQuestUnity.YOLO
{
    public class RemoteYOLOHandler : MonoBehaviour
    {

        [MustBeAssigned] [SerializeField] private string m_remoteYOLOProcessorAddress;
        [SerializeField] private YOLOFormat m_YOLOFormat;
        [SerializeField] private YOLOModel m_YOLOModel;
        [Space(30f)]
        [SerializeField] [Range(0f,1f)] private float m_confidenceThreshold = 0.5f;
        [Space(30f)]
        [MustBeAssigned]
        [SerializeField] [DisplayInspector] private ObjectDisplayManager m_objectDisplayManager;
        [MustBeAssigned] public VideoFeedManager YOLOCamera;
        [MustBeAssigned]
        [SerializeField] private Camera m_referenceCamera;

        private Texture2D m_inputTexture;
        private bool m_inferencePending = false;
        private bool m_inferenceDone = false;
        private RemoteYOLOResponse m_remoteYOLOResponse;
        private Camera m_analysisCamera;

        private static readonly HttpClient Client = new();
        
        private byte[] m_imageData;
        
        private void Start()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                Permission.RequestUserPermission("internet");
            }

            m_analysisCamera = GetComponent<Camera>();
            File.Delete(Path.Join(Application.persistentDataPath, "metrics.txt"));
            File.Create(Path.Join (Application.persistentDataPath, "metrics.txt")).Close();
        }

        private void Update()
        {
            if (m_inferencePending) return;
            
            try
            {
                if (!m_inferenceDone)
                {
                    if (YOLOCamera == null) return;
                    if ((m_inputTexture = YOLOCamera.GetTexture()) == null) return;
                    _ = AnalyseImage(m_inputTexture);
                    m_inferencePending = true;
                    m_analysisCamera.CopyFrom(m_referenceCamera);
                }
                else
                {
                    m_inferencePending = false;
                    m_inferenceDone = false;
                    m_objectDisplayManager.DisplayModels(Postprocess(m_remoteYOLOResponse), m_analysisCamera);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                m_inferencePending = false;
                m_inferenceDone = false;
            }
        }

        private void EncodeImageJPG(object paras)
        {
            var p = (ImageConversionThreadParams)paras;
            m_imageData = ImageConversion.EncodeArrayToJPG(p.imageBuffer, p.graphicsFormat, p.width, p.height, quality: p.quality);
        }

        private async Awaitable AnalyseImage(Texture2D texture)
        {
            var jpegEncodeStart = DateTime.Now;

            var imageConversionThreadParams = new ImageConversionThreadParams
            {
                imageBuffer = texture.GetRawTextureData(),
                graphicsFormat = texture.graphicsFormat,
                height = (uint)texture.height,
                width = (uint)texture.width,
                quality = 75
            };
            
            await Task.Run(() => EncodeImageJPG(imageConversionThreadParams));
            
            var jpegEncodeEnd = DateTime.Now;

            var start = DateTime.Now;
            var res = await SendRemoteRequest();
            var end = DateTime.Now;

            await using (StreamWriter sw = new StreamWriter(Path.Join(Application.persistentDataPath, "metrics.txt"), true))
            {
                await sw.WriteLineAsync("Encode time: " + (jpegEncodeEnd - jpegEncodeStart).TotalMilliseconds + "ms");
                await sw.WriteLineAsync("Network Time: " + (end - start).TotalMilliseconds + "ms");
                await sw.WriteLineAsync("Inference Time: " + res.metadata.speed.inference + "ms");
            }

            m_remoteYOLOResponse = res;
            m_inferenceDone = true;
            m_inferencePending = false;
        }

        private async Awaitable<RemoteYOLOResponse> SendRemoteRequest()
        {
            using HttpRequestMessage request = new(HttpMethod.Post, m_remoteYOLOProcessorAddress) ;
            
            MultipartFormDataContent content = new();
            
            content.Add(new StringContent(m_YOLOFormat.ToString().ToLower()), "format");
            content.Add(new StringContent(m_YOLOModel.ToString().ToLower()), "model");
            content.Add(new ByteArrayContent(m_imageData), "image", "image.jpg");
            request.Content = content;
            
            using HttpResponseMessage response = await Client.SendAsync(request);
                
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Request failed: {response.StatusCode} {response.Content.ReadAsStringAsync().Result}");
            
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<RemoteYOLOResponse>(responseString);
        }

        private List<DetectedObject> Postprocess(RemoteYOLOResponse response)
        {
            List<DetectedObject> results = new();

            foreach (RemoteYOLOPredictionResult obj in response.result)
            {
                if (obj.confidence < m_confidenceThreshold) continue;
                var cx = (obj.box.x1 + obj.box.x2) / 2;
                var cy = (obj.box.y1 + obj.box.y2) / 2;
                var width = (obj.box.x2 - obj.box.x1);
                var height = (obj.box.y2 - obj.box.y1);
                results.Add(new DetectedObject(cx, cy, width, height, obj.class_id, obj.name, obj.confidence));
            }

            return results;
        }

        private class ImageConversionThreadParams
        {
            public byte[] imageBuffer;
            public GraphicsFormat graphicsFormat;
            public uint width;
            public uint height;
            public int quality;
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
            public RemoteYOLORequestMetadata request;
        }

        private class RemoteYOLORequestMetadata
        {
            public float time_ms;
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