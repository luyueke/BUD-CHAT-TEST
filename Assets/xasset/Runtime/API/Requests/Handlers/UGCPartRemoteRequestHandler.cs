// @Author: YangJie
// @Description:
// @Date:  2023/12/26
// @Modify:

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace xasset
{
    public class UGCPartRemoteRequestHandler : RemoteImageRequestHandlerRuntime
    {


        public static int SmallSize
        {
            get
            {
                if (Assets.Quality == "High")
                {
                    return 256;
                }
                else
                {
                    return 128;
                }
            }
        }

        protected override void OnDownloadCompleted(DownloadContentRequest request)
        {
            if (_request == null)
            {
                Debug.LogError("OnDownloadCompleted _request is Null");
                return;
            }

            if (_downloadAsync.result == DownloadRequest.Result.Success)
            {
                bytes = request.bytes;
                // CDN 冷缓存/imageMogr2 异步未完成时会返回 200 空 body，HTTP 层判成功但内容是空的。
                // 这里把"空包/解码失败"也当成失败走退避重试，且不写入沙盒缓存（避免缓存坏数据）。
                bool emptyBytes = bytes == null || bytes.Length == 0;
                bool emptyTexture = !_request.isZip && request.texture == null;
                if (emptyBytes || emptyTexture)
                {
                    if (_retryTimes < Downloader.MaxRetryTimes)
                    {
                        Debug.LogWarning($"UgcPart Image Download empty retry {_retryTimes + 1}/{Downloader.MaxRetryTimes} {request.url} emptyBytes:{emptyBytes} emptyTexture:{emptyTexture}");
                        ScheduleRetry();
                        return;
                    }
                    Debug.LogError($"Load UgcPart Image Download Fail(empty) {_request.path} emptyBytes:{emptyBytes} emptyTexture:{emptyTexture}");
                    _request.SetResult(Request.Result.Failed, emptyBytes ? "bytes null" : "texture null");
                    _downloadAsync = null;
                    return;
                }

                _downloadAsync = null;
                // 建立写沙盒的请求
                SandboxStorage.DownLoadFinish(request.downloadedBytes, request.savePath, bytes);

                // 如果不是zip包，不用进行下一步
                if (!_request.isZip)
                {
                    _request.asset = request.texture;
                    _step = Step.Unzip;
                    return;
                }
                _step = Step.Unzip;
                return;
            }
            else if (request.result == DownloadRequest.Result.Failed)
            {
                if (_downloadAsync == null)
                {
                    Debug.LogError("_downloadAsync is Null " + request.url);
                    _request.SetResult(Request.Result.Failed, "_downloadAsync is Null");
                    return;
                }
                // 失败才重试，取消下载不重试
                if (_retryTimes < Downloader.MaxRetryTimes)
                {
                    Debug.LogWarning($"UgcPart Image Download retry {_retryTimes + 1}/{Downloader.MaxRetryTimes} {request.url} err:{request.error}");
                    ScheduleRetry();
                    return;
                }
            }

            Debug.LogError($"Load UgcPart Image Download Fail {_request.path} err:{_downloadAsync?.error}");
            _request.SetResult(Request.Result.Failed, _downloadAsync?.error);
            _downloadAsync = null;
        }


        protected override void UpdateUnzip()
        {
            if (_request.isZip == false)
            {
                SplitTextures();
            }
            else
            {
                _request.assets = new Dictionary<string, Texture>();
                try
                {
                    Stream stream = new MemoryStream(bytes);
                    var zipStream = new ZipInputStream(stream);
                    ZipEntry entry = null;
                    while ((entry = zipStream.GetNextEntry()) != null)
                    {
                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            using (MemoryStream fs = new MemoryStream())
                            {
                                int size = 2048;
                                byte[] data = new byte[size];
                                while (true)
                                {
                                    size = zipStream.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        fs.Write(data, 0, size); //解决读取不完整情况 
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                var imageBytes = fs.GetBuffer();
                                var texture = NewTexture();
                                texture.name = entry.Name;
                                texture.LoadImage(imageBytes);
                                texture = ScaleTexture(texture, SmallSize, SmallSize);
                                // if (texture.width % 4 == 0 && texture.height % 4 == 0)
                                // {
                                //     texture.Compress(true);
                                // }
                                _request.assets.Add(entry.Name, texture);
                            }
                        }
                    }
                    _request.SetResult(Request.Result.Success);
                }
                catch (Exception msg)
                {
                    _request.SetResult(Request.Result.Failed, msg.Message);
                }
            }
        }

        private void SplitTextures()
        {
            try
            {
                Texture2D texture;
                if (_request.asset != null) texture = (Texture2D)_request.asset;
                else
                {
                    // 本地文件才去用bytes，下载是直接有texture的
                    texture = new Texture2D(SmallSize, SmallSize);
                    if (!texture.LoadImage(bytes))
                    {
                        UnityEngine.Object.Destroy(texture);
                        Debug.LogError("本地缓存文件损坏:" + _request.path);
                        OnLocalLoadFail("本地缓存图片损坏");
                        return;
                    }
                    else
                    {
                        bytes = null;
                        _request.asset = texture;
                        return;
                    }
                }

                Stopwatch watch = new Stopwatch();
                watch.Start();
                _request.assets = SplitTextures(texture, SmallSize, ((UGCPartRemoteRequest)_request).textureCount);
                watch.Stop();
                if (watch.ElapsedMilliseconds >= 30)
                {
                    Debug.Log("分割UgcPart图片耗时===" + _request.path + " - " + watch.ElapsedMilliseconds);
                }

                if (_request.assets == null)
                {
                    Debug.LogError("图片分割失败:" + _request.path);
                    _request.SetResult(Request.Result.Failed, "SplitTextures Error");
                }
                else
                {
                    _request.asset = null;
                    _request.SetResult(Request.Result.Success);
                }
            }
            catch (Exception msg)
            {
                Debug.LogError("图片分割异常:" + _request.path + "," + msg.Message);
                _request.SetResult(Request.Result.Failed, msg.Message);
            }
        }

        public static Dictionary<string, Texture> SplitTextures(Texture2D texture, int smallSize, int texCount)
        {
            try
            {
                var textures = new Dictionary<string, Texture>();
                int numRows = texture.height / smallSize;
                int numCols = texture.width / smallSize;
                int index = 0;
                bool isBreak = false;
                for (int i = 0; i < numRows; i++)
                {
                    for (int j = 0; j < numCols; j++)
                    {
                        var smallTexture = new Texture2D(smallSize, smallSize , TextureFormat.RGBA32 ,false);
                        smallTexture.SetPixels(texture.GetPixels(j * smallSize, i * smallSize, smallSize, smallSize));
                        smallTexture.Apply();
                        // if (smallTexture.width % 4 == 0 && smallTexture.height % 4 == 0)
                        // {
                        //     smallTexture.Compress(true);
                        // }
                        smallTexture.name = $"{index / 2 + 1}{(index % 2 == 0 ? "" : "_alpha")}";
                        textures.Add($"{index / 2 + 1}{(index % 2 == 0 ? "" : "_alpha")}", smallTexture);
                        index++;
                        if (index >= texCount)
                        {
                            isBreak = true;
                            break;
                        }
                    }
                    if (isBreak)
                    {
                        break;
                    }
                }
                UnityEngine.Object.Destroy(texture);
                return textures;
            }
            catch (Exception msg)
            {
                Debug.LogError("SplitTextures:" + msg.Message);
                return null;
            }
        }

        private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            if (source.width == targetWidth && source.height == targetHeight)
            {
                return source;
            }
            var scaledTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32 ,false);
            scaledTexture.name = source.name;
            Color[] texturePixels = source.GetPixels();
            Color[] scaledPixels = new Color[targetWidth * targetHeight];

            float ratioX = 1.0f / ((float)targetWidth / (source.width - 1));
            float ratioY = 1.0f / ((float)targetHeight / (source.height - 1));

            for (int y = 0; y < targetHeight; y++)
            {
                int yFloor = (int)Mathf.Floor(y * ratioY);
                int yCeiling = (int)Mathf.Ceil(y * ratioY);
                float yBlend = y * ratioY - yFloor;

                for (int x = 0; x < targetWidth; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    int xCeiling = (int)Mathf.Ceil(x * ratioX);
                    float xBlend = x * ratioX - xFloor;

                    Color pixel1 = texturePixels[yFloor * source.width + xFloor];
                    Color pixel2 = texturePixels[yFloor * source.width + xCeiling];
                    Color pixel3 = texturePixels[yCeiling * source.width + xFloor];
                    Color pixel4 = texturePixels[yCeiling * source.width + xCeiling];

                    Color blendedPixel = Color.Lerp(Color.Lerp(pixel1, pixel2, xBlend), Color.Lerp(pixel3, pixel4, xBlend), yBlend);
                    scaledPixels[y * targetWidth + x] = blendedPixel;
                }
            }

            scaledTexture.SetPixels(scaledPixels);
            scaledTexture.Apply();
            UnityEngine.Object.Destroy(source);
            return scaledTexture;
        }


        public static UGCPartRemoteRequestHandler CreateInstance(UGCPartRemoteRequest assetRequest)
        {
            return new UGCPartRemoteRequestHandler {_request = assetRequest};
        }

    }
}