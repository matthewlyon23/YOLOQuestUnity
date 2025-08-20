using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEngine;
using YOLOTools.YOLO.ObjectDetection;
using YOLOTools.YOLO.ObjectDetection.Utilities;
using YOLOTools.YOLO;
using YOLOTools.YOLO.RemoteYOLO;
using YOLOModel = YOLOTools.YOLO.RemoteYOLO.YOLOModel;

namespace YOLOTools
{
    public class YOLOTools
    {
        public static async Task<List<DetectedObject>> YOLOAnalyseAsync(Texture2D texture, ModelAsset modelAsset, YOLOAnalysisParameters yoloAnalysisParameters, YOLOCustomizationParameters customizationParameters)
        {
            YOLOCustomizer.CustomizeModel(modelAsset, customizationParameters, out var yoloModel);
            var size = yoloAnalysisParameters.Size;
            var inferenceHandler = new YOLOInferenceHandler(yoloModel, ref size, yoloAnalysisParameters.BackendType);

            var result = await inferenceHandler.Run(texture);

            return YOLOPostProcessor.PostProcess(result, texture, size, yoloAnalysisParameters.Classes,
                yoloAnalysisParameters.ConfidenceThreshold);
        }

        public static List<DetectedObject> YOLOAnalyse(Texture2D texture, ModelAsset modelAsset,
            YOLOAnalysisParameters yoloAnalysisParameters, YOLOCustomizationParameters customizationParameters)
        {
            YOLOCustomizer.CustomizeModel(modelAsset, customizationParameters, out var yoloModel);
            var size = yoloAnalysisParameters.Size;
            var inferenceHandler = new YOLOInferenceHandler(yoloModel, ref size, yoloAnalysisParameters.BackendType);

            var result = inferenceHandler.Run(texture).GetAwaiter().GetResult();

            return YOLOPostProcessor.PostProcess(result, texture, size, yoloAnalysisParameters.Classes,
                yoloAnalysisParameters.ConfidenceThreshold);
        }

        public static IEnumerator YOLOAnalyseWithLayerControl(Texture2D texture, ModelAsset modelAsset, YOLOAnalysisParameters yoloAnalysisParameters, YOLOCustomizationParameters customizationParameters)
        {
            YOLOCustomizer.CustomizeModel(modelAsset, customizationParameters, out var yoloModel);
            var size = yoloAnalysisParameters.Size;
            var inferenceHandler = new YOLOInferenceHandler(yoloModel, ref size, yoloAnalysisParameters.BackendType);

            return inferenceHandler.RunWithLayerControl(texture);
        }
        
        public static async Task<List<DetectedObject>> YOLOAnalyseWithLayerControlAsync(Texture2D texture, ModelAsset modelAsset,
            YOLOAnalysisParameters yoloAnalysisParameters, YOLOCustomizationParameters customizationParameters, uint layersPerFrame = 10)
        {
            YOLOCustomizer.CustomizeModel(modelAsset, customizationParameters, out var yoloModel);
            var size = yoloAnalysisParameters.Size;
            var inferenceHandler = new YOLOInferenceHandler(yoloModel, ref size, yoloAnalysisParameters.BackendType);

            var splitInferenceEnumerator = inferenceHandler.RunWithLayerControl(texture);

            int it = 0;
            while (splitInferenceEnumerator.MoveNext())
                if (++it % layersPerFrame == 0)
                {
                    await Awaitable.NextFrameAsync();
                    it = 0;
                }

            var result = await inferenceHandler.PeekOutput().ReadbackAndCloneAsync() as Tensor<float>;
            
            return YOLOPostProcessor.PostProcess(result, texture, size, yoloAnalysisParameters.Classes,
                yoloAnalysisParameters.ConfidenceThreshold);
        }     

        public static async Task<List<DetectedObject>> RemoteYOLOAnalyseAsync(Texture2D texture, string remoteYOLOAddress, float confidenceThreshold = 0.5f, YOLOModel yoloModel = YOLOModel.YOLO11N, YOLOFormat yoloFormat = YOLOFormat.NCNN, RemoteYOLOImageFormat imageFormat = RemoteYOLOImageFormat.JPG)
        {
            var client = new RemoteYOLOClient(remoteYOLOAddress);

            var imageData = imageFormat switch
            {
                RemoteYOLOImageFormat.JPG => Texture2DToJPGAsync(texture),
                RemoteYOLOImageFormat.PNG => Texture2DToPNGAsync(texture),
                _ => throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, null)
            };
            var result = client.AnalyseAsync(await imageData, yoloModel, yoloFormat);

            return YOLOPostProcessor.RemoteYOLOPostprocess(await result, confidenceThreshold);
        }
        
        public static async Task<List<DetectedObject>> RemoteYOLOAnalyseAsync(Texture2D texture, byte[] customModel, string remoteYOLOAddress, float confidenceThreshold = 0.5f, YOLOModel yoloModel = YOLOModel.YOLO11N, YOLOFormat yoloFormat = YOLOFormat.NCNN, RemoteYOLOImageFormat imageFormat = RemoteYOLOImageFormat.JPG)
        {
            var client = new RemoteYOLOClient(remoteYOLOAddress);

            var customModelResponse = await client.UploadCustomModelAsync(customModel);
            if (!customModelResponse.success)
            {
                throw new HttpRequestException(customModelResponse.error);
            }
            
            var imageData = imageFormat switch
            {
                RemoteYOLOImageFormat.JPG => Texture2DToJPGAsync(texture),
                RemoteYOLOImageFormat.PNG => Texture2DToPNGAsync(texture),
                _ => throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, null)
            };
            var result = client.AnalyseAsync(await imageData, yoloModel, yoloFormat);

            return YOLOPostProcessor.RemoteYOLOPostprocess(await result, confidenceThreshold);
        }
        
        public static List<DetectedObject> RemoteYOLOAnalyse(Texture2D texture, string remoteYOLOAddress, float confidenceThreshold = 0.5f, YOLOModel yoloModel = YOLOModel.YOLO11N, YOLOFormat yoloFormat = YOLOFormat.NCNN, RemoteYOLOImageFormat imageFormat = RemoteYOLOImageFormat.JPG)
        {
            var client = new RemoteYOLOClient(remoteYOLOAddress);

            var imageData = imageFormat switch
            {
                RemoteYOLOImageFormat.JPG => Texture2DToJPGAsync(texture),
                RemoteYOLOImageFormat.PNG => Texture2DToPNGAsync(texture),
                _ => throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, null)
            };
            var result = client.AnalyseAsync(imageData.Result, yoloModel, yoloFormat).GetAwaiter().GetResult();

            return YOLOPostProcessor.RemoteYOLOPostprocess(result, confidenceThreshold);
        }
        
        public static List<DetectedObject> RemoteYOLOAnalyse(Texture2D texture, byte[] customModel, string remoteYOLOAddress, float confidenceThreshold = 0.5f, YOLOModel yoloModel = YOLOModel.YOLO11N, YOLOFormat yoloFormat = YOLOFormat.NCNN, RemoteYOLOImageFormat imageFormat = RemoteYOLOImageFormat.JPG)
        {
            var client = new RemoteYOLOClient(remoteYOLOAddress);

            var customModelResponse = client.UploadCustomModel(customModel);
            if (!customModelResponse.success)
            {
                throw new HttpRequestException(customModelResponse.error);
            }
            
            var imageData = imageFormat switch
            {
                RemoteYOLOImageFormat.JPG => Texture2DToJPGAsync(texture),
                RemoteYOLOImageFormat.PNG => Texture2DToPNGAsync(texture),
                _ => throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, null)
            };
            var result = client.AnalyseAsync(imageData.Result, yoloModel, yoloFormat).GetAwaiter().GetResult();

            return YOLOPostProcessor.RemoteYOLOPostprocess(result, confidenceThreshold);
        }

        public static async Task<bool> UploadCustomModelAsync(byte[] customModel, string remoteYOLOAddress)
        {
            var client = new RemoteYOLOClient(remoteYOLOAddress);
            
            var customModelResponse = await client.UploadCustomModelAsync(customModel);

            return customModelResponse.success;
        }

        public static bool UploadCustomModel(byte[] customModel, string remoteYOLOAddress)
        {
            var client = new RemoteYOLOClient(remoteYOLOAddress);
            
            var customModelResponse = client.UploadCustomModel(customModel);

            return customModelResponse.success;
        }
        
        public static async Task<byte[]> Texture2DToJPGAsync(Texture2D texture, int quality = 75)
        {
            var rawTextureData = texture.GetRawTextureData();
            var graphicsFormat = texture.graphicsFormat;
            var width = texture.width;
            var height = texture.height;
            
            return await Task.Run(() => ImageConversion.EncodeArrayToJPG(rawTextureData, graphicsFormat, (uint)width, (uint)height, quality: quality));
        }

        public static async Task<byte[]> Texture2DToPNGAsync(Texture2D texture)
        {
            var rawTextureData = texture.GetRawTextureData();
            var graphicsFormat = texture.graphicsFormat;
            var width = texture.width;
            var height = texture.height;
            
            return await Task.Run(() => ImageConversion.EncodeArrayToPNG(rawTextureData, graphicsFormat, (uint)width, (uint)height));
        }

        public static byte[] Texture2DToJPG(Texture2D texture, int quality = 75)
        {
            return texture.EncodeToJPG(quality: quality);
        }

        public static byte[] Texture2DToPNG(Texture2D texture)
        {
            return texture.EncodeToPNG();
        }
    }

    public class YOLOAnalysisParameters
    {
        public readonly int Size;
        public readonly BackendType BackendType;
        public readonly Dictionary<int, string> Classes;
        public readonly float ConfidenceThreshold;

        public YOLOAnalysisParameters(BackendType backendType, int size, Dictionary<int, string> classes, float confidenceThreshold)
        {
            BackendType = backendType;
            Size = size;
            Classes = classes;
            ConfidenceThreshold = confidenceThreshold;
        }
    }
    
    public enum RemoteYOLOImageFormat
    {
        JPG,
        PNG
    }
}
