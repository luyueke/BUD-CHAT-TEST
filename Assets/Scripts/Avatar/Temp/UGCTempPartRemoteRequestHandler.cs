// @Author: YangJie
// @Description:
// @Date:  2023/12/26
// @Modify:

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GameData.Manager;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace xasset
{
    //由于XAsset中相关代码裁剪、临时复制代码
    public class UGCTempPartRemoteRequestHandler : RemoteImageRequestHandlerRuntime {


        private static readonly Color DefaultColor = DataUtil.DeSerializeColor("CDCDCDCD");

        protected override void OnDownloadCompleted(DownloadContentRequest request)
        {
            if (_request == null)
            {
                Debug.LogError("OnDownloadCompleted _request is Null");
                return;
            }

            if (_downloadAsync.result == DownloadRequest.Result.Success)
            {
                _downloadAsync = null;
                bytes = request.bytes;
                if (bytes == null || bytes.Length == 0) _request.SetResult(Request.Result.Failed, "bytes null");
                // 建立写沙盒的请求
                SandboxStorage.DownLoadFinish(request.downloadedBytes, request.savePath, bytes);

                if (!_request.isZip)
                {
                    if (request.texture != null)
                    {
                        bytes = null;
                        _request.asset = request.texture;
                        _step = Step.Unzip;
                    }
                    else
                    {
                        _request.SetResult(Request.Result.Failed, "texture null");
                    }
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
                    request.Retry();
                    _downloadAsync.completed += OnDownloadCompleted;
                    _retryTimes++;
                    return;
                }
            }

            Debug.LogError("Load UgcPart Image Download Fail " + _request.path);
            _request.SetResult(Request.Result.Failed, _downloadAsync.error);
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
                                var ugcRequest = (UGCTempPartRemoteRequest) _request;
                                texture = ScaleTexture(texture, ugcRequest.texSize, ugcRequest.texSize);
                                // if (texture.width % 4 == 0 && texture.height % 4 == 0)
                                // {
                                //     texture.Compress(true);
                                // }
                                _request.assets.Add(entry.Name, texture);
                            }
                        }
                    }
                    bytes = null;
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
                var ugcRequest = (UGCTempPartRemoteRequest) _request;
                if (_request.asset != null) texture = (Texture2D)_request.asset;
                else
                {
                    // 本地文件才去用bytes，下载是直接有texture的
                    texture = new Texture2D(ugcRequest.texSize, ugcRequest.texSize);
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
                _request.assets = SplitTextures(texture, ugcRequest.texSize, ugcRequest.textureCount );
                watch.Stop();
                if (watch.ElapsedMilliseconds >= 30)
                {
                    Debug.Log("deltaTime===" + _request.path + " - " + watch.ElapsedMilliseconds);
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

        public Dictionary<string, Texture> SplitTextures(Texture2D texture, int smallSize, int texCount)
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
                        var pixels = texture.GetPixels(j * smallSize, i * smallSize, smallSize, smallSize);
                        if (index % 2 != 0) {
                            // 海外缺失的图片，用白色填充
                            bool isAllDefault= pixels.All(tmp => tmp == DefaultColor);
                            if (isAllDefault)
                            {
                                for (int k = 0; k < pixels.Length; k++) {
                                    pixels[k] = Color.white;
                                }
                            }
                        }

                        smallTexture.SetPixels(pixels);
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


        public static UGCTempPartRemoteRequestHandler CreateInstance(UGCTempPartRemoteRequest assetRequest)
        {
            return new UGCTempPartRemoteRequestHandler {_request = assetRequest};
        }

    }
}
