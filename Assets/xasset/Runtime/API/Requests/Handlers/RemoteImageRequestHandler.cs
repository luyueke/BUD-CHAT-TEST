using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace xasset
{
    public interface RemoteImageRequestHandler
    {
        void OnStart();
        void Update();
        void Dispose();
        void WaitForCompletion();
    }

    public class RemoteImageRequestHandlerRuntime : RemoteImageRequestHandler
    {
        protected enum Step
        {
            CheckResSize,
            Download,
            LoadLocal,

            LoadBundle,
            Unzip
        }
        private static bool NeedCheckResSize = false;
        protected Step _step;
        protected RemoteImageRequest _request { get; set; }
        private UnityWebRequest www { get; set; }
        private UnityWebRequest wwwHead { get; set; }
        protected DownloadContentRequest _downloadAsync;
        protected string _savePath;
        protected int _retryTimes;
        protected byte[] bytes;
        protected ulong localFileSize;

        // 重试退避：CDN 冷缓存/imageMogr2 异步处理未完成时会瞬时返回空 body，
        // 立即重试容易在处理完成前把次数耗尽，这里加一个递增延迟。
        private bool _pendingRetry;
        private float _retryAtTime;

        /// <summary>
        /// 安排一次延迟重试（递增退避：0.3s、0.6s、0.9s …… 最多 2s）。
        /// 实际的 Retry 在 UpdateDownload 中等延迟到达后执行。
        /// </summary>
        protected void ScheduleRetry()
        {
            _retryTimes++;
            var delay = Mathf.Min(0.3f * _retryTimes, 2f);
            _retryAtTime = Time.realtimeSinceStartup + delay;
            _pendingRetry = true;
        }

        public void OnStart()
        {
            _retryTimes = 0;
            var path = _request.path;
            var url = path;

            // TODO 从本地文件加载
            if (_request.isLocalUrl)
            {
                _savePath = new System.Uri(_request.path).AbsoluteUri;
                _step = Step.LoadLocal;
                return;
            }

            _savePath = SandboxStorage.GetSandboxPath(GetMd5(url));
            var file = new FileInfo(_savePath);
            if (file.Exists)
            {
                localFileSize = (ulong)file.Length;
                _step = Step.CheckResSize;
            }
            else
            {
                StartDownload(url, _savePath);
            }
        }

        private string GetMd5(string url)
        {
            var bytes = Encoding.UTF8.GetBytes(url);
            var md5 = Utility.ComputeHash(bytes);
            return md5;
        }

        public virtual void Update()
        {
            switch (_step)
            {
                case Step.Download:
                    UpdateDownload();
                    break;
                case Step.Unzip:
                    UpdateUnzip();
                    break;
                case Step.LoadLocal:
                    UpdateLoadLocal();
                    break;
                case Step.CheckResSize:
                    UpdateCheckResSize();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual void UpdateDownload()
        {
            if (_downloadAsync == null)
            {
                Debug.LogError("UpdateDownload _downloadAsync is Null");
                _request.SetResult(Request.Result.Failed, "_downloadAsync is Null");
                return;
            }
            
            if (_request == null)
            {
                Debug.LogError("UpdateDownload _request is Null");
                return;
            }

            // 延迟到达后再真正发起重试
            if (_pendingRetry)
            {
                if (Time.realtimeSinceStartup < _retryAtTime)
                    return;
                _pendingRetry = false;
                _downloadAsync.Retry();
                _downloadAsync.completed += OnDownloadCompleted;
                return;
            }

            _request.progress = _downloadAsync.progress;
            if (!_downloadAsync.isDone)
                return;
        }

        protected Texture2D NewTexture(int width = 128, int height = 128)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
// #if UNITY_ANDROID
//             tex = new Texture2D(width, height, TextureFormat.ETC2_RGBA8, false);
// #elif UNITY_IOS
//             tex = new Texture2D(width, height, TextureFormat.PVRTC_RGBA4, false);
// #endif
            return tex;
        }

        private void UpdateLoadLocal()
        {
            if (Assets.Quality == "High" && !_request.isLocalUrl)
            {
                try
                {
                    Debug.LogFormat("RemoteImageRequest load loacl image : {0}", _request.isLocalUrl);
                    bytes = File.ReadAllBytes(_savePath);
                    _step = Step.Unzip;
                }
                catch (Exception msg)
                {
                    OnLocalLoadFail(msg.Message);
                }

                return;
            }

            if (www == null)
            {
                www = UnityWebRequest.Get(_savePath);
                www.SendWebRequest();
            }
            if (!www.isDone) return;
            if (!string.IsNullOrEmpty(www.error))
            {
                OnLocalLoadFail(www.error);
                return;
            }
            bytes = www.downloadHandler.data;
            _step = Step.Unzip;
        }

        private void UpdateCheckResSize()
        {
            if (NeedCheckResSize == false)
            {
                // 不需要校验本地文件的话 直接跳过这个阶段
                _step = Step.LoadLocal;
                return;
            }

            if (wwwHead == null)
            {
                wwwHead = UnityWebRequest.Head(_request.path);
                wwwHead.SendWebRequest();
            }
            if (!wwwHead.isDone) return;
            if (!string.IsNullOrEmpty(wwwHead.error))
            {
                _request.SetResult(Request.Result.Failed, wwwHead.error);
                return;
            }

            const string key = "Content-Length";
            var value = wwwHead.GetResponseHeader(key);
            if (ulong.TryParse(value, out var size))
            {
                if (size == localFileSize)
                {
                    _step = Step.LoadLocal;
                    return;
                }
            }

            StartDownload(_request.path, _savePath);
        }

        protected virtual void OnLocalLoadFail(string msg)
        {
            // 本地加载不存在远端地址
            if (_request.isLocalUrl)
            {
                _request.SetResult(Request.Result.Failed, msg);
                return;
            }

            // 删除本地损坏的文件
            SandboxStorage.DeleteFile(_savePath);

            // 没有下载过远端
            if (_downloadAsync == null)
            {
                StartDownload(_request.path, _savePath);
            }
            else
            {
                _request.SetResult(Request.Result.Failed, msg);
            }
        }

        private void StartDownload(string url, string savePath)
        {
            _downloadAsync = Downloader.DownloadImageAsync(DownloadContent.Get(url, savePath));
            _downloadAsync.completed = OnDownloadCompleted;
            _step = Step.Download;
        }

        protected virtual void OnDownloadCompleted(DownloadContentRequest request)
        {
            if (_request == null)
            {
                Debug.LogError("OnDownloadCompleted _request is Null");
                return;
            }
            
            if (request.result == DownloadRequest.Result.Success)
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
                        Debug.LogWarning($"Load Image Download empty retry {_retryTimes + 1}/{Downloader.MaxRetryTimes} {request.url} emptyBytes:{emptyBytes} emptyTexture:{emptyTexture}");
                        ScheduleRetry();
                        return;
                    }
                    Debug.LogError($"Load Image Download Fail(empty) {_request.path} emptyBytes:{emptyBytes} emptyTexture:{emptyTexture}");
                    _request.SetResult(Request.Result.Failed, emptyBytes ? "bytes null" : "texture null");
                    bytes = null;
                    _downloadAsync = null;
                    return;
                }

                _downloadAsync = null;
                // 建立写沙盒的请求
                SandboxStorage.DownLoadFinish((ulong)bytes.Length - request.downloadedBytes, request.savePath, bytes);

                // 如果不是zip包，不用进行下一步
                if (!_request.isZip)
                {
                    _request.asset = request.texture;
                    _request.SetResult(Request.Result.Success);
                    bytes = null;
                    return;
                }
                _step = Step.Unzip;
                return;
            }
            else if (request.result == DownloadRequest.Result.Failed)
            {
                if (_downloadAsync == null)
                {
                    Debug.LogError("_downloadAsync is Null " +request.url);
                    _request.SetResult(Request.Result.Failed, "_downloadAsync is Null");
                    return;
                }
                // 失败才重试，取消下载不重试
                if (_retryTimes < Downloader.MaxRetryTimes)
                {
                    Debug.LogWarning($"Load Image Download retry {_retryTimes + 1}/{Downloader.MaxRetryTimes} {request.url} err:{request.error}");
                    ScheduleRetry();
                    return;
                }
            }

            Debug.LogError($"Load Image Download Fail {_request.path} err:{_downloadAsync?.error}");
            _request.SetResult(Request.Result.Failed, _downloadAsync?.error);
            _downloadAsync = null;
        }

        protected virtual void UpdateUnzip()
        {
            if (_request.isZip == false)
            {
                try
                {
                    var texture = NewTexture();
                    if (!texture.LoadImage(bytes))
                    {
                        UnityEngine.Object.Destroy(texture);
                        OnLocalLoadFail("本地缓存图片损坏");
                        Debug.LogError("Load Image Local File Error " + _request.path);
                        return;
                    }

                    // if (texture.width % 4 == 0 && texture.height % 4 == 0)
                    // {
                    //     texture.Compress(true);
                    // }
                    bytes = null;
                    _request.asset = texture;
                    _request.SetResult(Request.Result.Success);
                }
                catch (Exception msg)
                {
                    _request.SetResult(Request.Result.Failed, msg.Message);
                }
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
                                texture.LoadImage(imageBytes);

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
                    OnLocalLoadFail("本地缓存Zip文件损坏 - " + msg.Message);
                }
            }
        }

        public void Dispose()
        {
            bytes = null;

            if(_downloadAsync != null)
            {
                _downloadAsync.completed = null;
                _downloadAsync?.Cancel();
                _downloadAsync = null;
            }

            www?.Dispose();
            www = null;

            wwwHead?.Dispose();
            wwwHead = null;
        }

        public void WaitForCompletion()
        {
            if (_step == Step.Download)
            {
                _downloadAsync.WaitForCompletion();
            }
            while (!_request.isDone) Update();
        }

        public static RemoteImageRequestHandler CreateInstance(RemoteImageRequest assetRequest)
        {
            return new RemoteImageRequestHandlerRuntime { _request = assetRequest };
        }
    }
}
