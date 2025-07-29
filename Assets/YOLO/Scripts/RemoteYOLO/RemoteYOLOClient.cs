using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace YOLOQuestUnity.YOLO.RemoteYOLO
{
    public class RemoteYOLOClient
    {
        public string BaseAddress { get; set; }

        private HttpClient _client;

        private static string _customModelEndpoint = "/api/custom-model";
        private static string _analyseEndpoint = "/api/analyse";
        
        public RemoteYOLOClient(string baseAddress)
        {
            BaseAddress = baseAddress;
            _client = new HttpClient();
        }

        /// <summary>
        /// Sends an asynchronous request to the /api/custom-model endpoint. 
        /// </summary>
        /// <param name="customModel">The custom model file to be uploaded.</param>
        /// <returns>An <see cref="Awaitable"/>&lt;<see cref="CustomModelResponse"/>&gt; conforming to the response schema.</returns>
        /// <exception cref="HttpRequestException">Throws an HttpRequestException on failure status code. The message field contains the error message from the server, if one exists.</exception>
        public async Awaitable<CustomModelResponse> UploadCustomModelAsync(byte[] customModel)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, $"http://{BaseAddress}{_customModelEndpoint}");
            
            MultipartFormDataContent content = new();
            
            content.Add(new ByteArrayContent(customModel), "model", "model.pt");
            request.Content = content;
            
            using HttpResponseMessage response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new HttpRequestException(
                    JsonConvert.DeserializeObject<CustomModelFailureResponse>(await response.Content.ReadAsStringAsync()).error);
            }

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                throw new HttpRequestException(
                    JsonConvert.DeserializeObject<CustomModelFailureResponse>(await response.Content.ReadAsStringAsync()).error);
            }
            
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Request failed: {response.StatusCode} {response.Content.ReadAsStringAsync().Result}");

            var customModelResponse = JsonConvert.DeserializeObject<CustomModelResponse>(await response.Content.ReadAsStringAsync());
            
            return customModelResponse;
        }

        /// <summary>
        /// Sends a request to the /api/custom-model endpoint. 
        /// </summary>
        /// <param name="customModel">The custom model file to be uploaded.</param>
        /// <returns>A <see cref="CustomModelResponse"/> conforming to the response schema.</returns>
        /// <exception cref="HttpRequestException">Throws an HttpRequestException on failure status code. The message field contains the error message from the server, if one exists.</exception>
        public CustomModelResponse UploadCustomModel(byte[] customModel)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, $"http://{BaseAddress}{_customModelEndpoint}");
            
            MultipartFormDataContent content = new();
            
            content.Add(new ByteArrayContent(customModel), "model", "model.pt");
            request.Content = content;
            
            using HttpResponseMessage response = _client.SendAsync(request).Result;
                
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new HttpRequestException(
                    JsonConvert.DeserializeObject<CustomModelFailureResponse>(response.Content.ReadAsStringAsync().Result).error);
            }

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                throw new HttpRequestException(
                    JsonConvert.DeserializeObject<CustomModelFailureResponse>(response.Content.ReadAsStringAsync().Result).error);
            }
            
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Request failed: {response.StatusCode} {response.Content.ReadAsStringAsync().Result}");

            var customModelResponse = JsonConvert.DeserializeObject<CustomModelResponse>(response.Content.ReadAsStringAsync().Result);
            
            return customModelResponse;
        }

        /// <summary>
        /// Sends an asynchronous request to the /api/analyse endpoint.
        /// </summary>
        /// <param name="yoloModel">The YOLO Model to use. For options, see <see cref="YOLOModel"/></param>
        /// <param name="yoloFormat">The YOLO Format to use. For options, see <see cref="YOLOFormat"/></param>
        /// <param name="imageData">The JPG encoded image data to analyse.</param>
        /// <returns>An <see cref="Awaitable"/>&lt;<see cref="RemoteYOLOAnalyseResponse"/>&gt; confroming to the response schema.</returns>
        /// <exception cref="HttpRequestException">Throws an HttpRequestException on failure status code. The message field contains the error message from the server, if one exists.</exception>
        public async Awaitable<RemoteYOLOAnalyseResponse> AnalyseAsync(YOLOModel yoloModel, YOLOFormat yoloFormat, byte[] imageData)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, $"http://{BaseAddress}{_analyseEndpoint}");
            
            MultipartFormDataContent content = new();
            
            content.Add(new StringContent(yoloFormat.ToString().ToLower()), "format");
            content.Add(new StringContent(yoloModel.ToString().ToLower()), "model");
            content.Add(new ByteArrayContent(imageData), "image", "image.jpg");
            request.Content = content;
            
            using HttpResponseMessage response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new HttpRequestException(JsonConvert
                    .DeserializeObject<AnalyseFailureResponse>(await response.Content.ReadAsStringAsync()).error);
            }
            
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Request failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
            
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<RemoteYOLOAnalyseResponse>(responseString);
        }
        
        /// <summary>
        /// Sends a request to the /api/analyse endpoint.
        /// </summary>
        /// <param name="yoloModel">The YOLO Model to use. For options, see <see cref="YOLOModel"/></param>
        /// <param name="yoloFormat">The YOLO Format to use. For options, see <see cref="YOLOFormat"/></param>
        /// <param name="imageData">The JPG encoded image data to analyse.</param>
        /// <returns>A <see cref="RemoteYOLOAnalyseResponse"/> conforming to the response schema.</returns>
        /// <exception cref="HttpRequestException">Throws an HttpRequestException on failure status code. The message field contains the error message from the server, if one exists.</exception>
        public RemoteYOLOAnalyseResponse Analyse(YOLOModel yoloModel, YOLOFormat yoloFormat, byte[] imageData)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, $"http://{BaseAddress}{_analyseEndpoint}");
            
            MultipartFormDataContent content = new();
            
            content.Add(new StringContent(yoloFormat.ToString().ToLower()), "format");
            content.Add(new StringContent(yoloModel.ToString().ToLower()), "model");
            content.Add(new ByteArrayContent(imageData), "image", "image.jpg");
            request.Content = content;
            
            using HttpResponseMessage response = _client.SendAsync(request).Result;
                
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new HttpRequestException(JsonConvert
                    .DeserializeObject<AnalyseFailureResponse>(response.Content.ReadAsStringAsync().Result).error);
            }
            
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Request failed: {response.StatusCode} {response.Content.ReadAsStringAsync().Result}");
            
            var responseString = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<RemoteYOLOAnalyseResponse>(responseString);
        }
        
        private struct CustomModelFailureResponse
        {
            public bool success;
            public string error;
        }

        private struct AnalyseFailureResponse
        {
            public bool success;
            public string error;
        }
    }

    
    
    public class CustomModelResponse
    {
        public bool success;
        public string result;
        public CustomModelResponseMetadata metadata;
    }

    public class CustomModelResponseMetadata
    {
        public CustomModelResponseRequestMetadata request;
    }

    public class CustomModelResponseRequestMetadata
    {
        public float time_ms;
    }

    public class RemoteYOLOAnalyseResponse
    {
        public bool success;
        public RemoteYOLOAnalyseMetadata metadata;
        public RemoteYOLOAnalysePredictionResult[] result;
    }

    public class RemoteYOLOAnalyseMetadata
    {
        public Dictionary<int, string> names;
        public RemoteYOLOAnalyseSpeedMetadata speed;
        public RemoteYOLOAnalyseRequestMetadata request;
    }

    public class RemoteYOLOAnalyseRequestMetadata
    {
        public float time_ms;
    }

    public class RemoteYOLOAnalyseSpeedMetadata
    {
        public float preprocess;
        public float inference;
        public float postprocess;
    }

    public class RemoteYOLOAnalysePredictionResult
    {
        public string name;
        public int class_id;
        public float confidence;
        public RemoteYOLOResultBox box;
    }

    public struct RemoteYOLOResultBox
    {
        public float x1;
        public float y1;
        public float x2;
        public float y2;
    }

    public enum YOLOFormat
    {
        NCNN,
        ONNX,
        PYTORCH
    }

    public enum YOLOModel
    {
        YOLO11N,
        YOLO11S,
        YOLO11M,
        YOLO11L
    }
}