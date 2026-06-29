using System.IO;
using UnityEngine.Networking;

namespace xasset
{
    public class DownloadCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    public struct DownloadContentRequestHandlerUWR : DownloadContentRequestHandler
    {
        private UnityWebRequest _content;
        private UnityWebRequest _header;
        private ulong _lastRequestDownloadedBytes;
        private Step _step;
        private readonly DownloadContentRequest _request;
        private float _stallTimer;
        private int _retryCount;
        private float _retryWaitUntil;
        private const float StallTimeout = 30f;
        private const int MaxRetries = 5;

        public DownloadContentRequestHandlerUWR(DownloadContentRequest request)
        {
            _header = null;
            _content = null;
            _lastRequestDownloadedBytes = 0;
            _step = Step.GetHeader;
            _request = request;
            _retryCount = 0;
            _retryWaitUntil = 0f;
            _stallTimer = 0f;
        }

        public void OnStart()
        {
            _step = Step.GetHeader;
            _retryCount = 0; // 每次 OnStart 重置，支持外层重试时内层重新计数
            StartDownload();
        }

        public void OnPause(bool paused)
        {
            if (paused)
            {
                _content?.Abort();
                _header?.Abort();
                Dispose();
            }
            else
            {
                StartDownload();
            }
        }

        public bool Update()
        {
            if (_request.isDone) return false;
            if (_request.status == DownloadRequest.Status.Paused) return true;
            if (_request.status != DownloadRequest.Status.Progressing) return false;
            if (_step == Step.RetryWait) return UpdateRetryWait();
            if (_step == Step.GetHeader) return UpdateHeaderRequest();
            return _step == Step.GetContent && UpdateContentRequest();
        }

        private bool UpdateRetryWait()
        {
            if (UnityEngine.Time.realtimeSinceStartup < _retryWaitUntil) return true;
            // 退避时间到，重新计算断点续传偏移，发起新一次 GET
            _request.downloadedBytes = _request.content.GetDownloadedBytes();
            _lastRequestDownloadedBytes = 0;
            GetContentRequest();
            return true;
        }

        public void OnCancel()
        {
            Dispose();
        }

        private void StartDownload()
        {
            if (Downloader.SimulationMode)
            {
                // 本地仿真的时候不支持断点续传。
                _request.downloadedBytes = 0;
                var path = _request.url.Replace(Assets.Protocol, string.Empty);
                var file = new FileInfo(path);
                if (file.Exists) _request.OnGetDownloadSize((ulong) file.Length);
                GetContentRequest();
            }
            else if (!Downloader.Resumable)
            {
                GetContentRequest();
            }
            else if (_request.downloadedBytes > 0)
            {
                // 断点续传：发 HEAD 确认服务端文件未变化，防止续传到脏数据
                GetHeadRequest();
            }
            else
            {
                // 全新下载：manifest 中已有文件大小，无需 HEAD，直接 GET
                // 跳过 HEAD 可节省每个文件一次 RTT，大量小文件时效果显著
                _request.OnGetDownloadSize(_request.content.size);
                GetContentRequest();
            }

            _request.status = DownloadRequest.Status.Progressing;
        }

        private void GetHeadRequest()
        {
            _header = UnityWebRequest.Head(_request.url);
            _header.timeout = 15;
            _header.SendWebRequest();
            _step = Step.GetHeader;
            UnityEngine.Debug.Log($"[xasset] HEAD {_request.url}");
        }

        private bool UpdateHeaderRequest()
        {
            if (!_header.isDone) return true;
            if (!string.IsNullOrEmpty(_header.error))
            {
                _request.SetResult(DownloadRequest.Result.Failed, _header.error);
                return false;
            }

            const string key = "Content-Length";
            var value = _header.GetResponseHeader(key);
            if (ulong.TryParse(value, out var size))
            {
                _request.OnGetDownloadSize(size);
                if (size == _request.downloadedBytes)
                {
                    _request.SetResult(DownloadRequest.Result.Success, DownloadErrors.NothingToDownload);
                    return false;
                }

                GetContentRequest();
                return true;
            }

            // CDN 未返回 Content-Length（gzip/chunked 常见），不能假设"无需下载"
            // 强制全量 GET，放弃断点续传；调用 OnGetDownloadSize 确保 downloadSize 有效值
            UnityEngine.Debug.LogWarning($"[xasset] HEAD 未返回 Content-Length，强制全量下载: {_request.url}");
            _request.downloadedBytes = 0;
            _request.OnGetDownloadSize(0); // 内部会取 content.size，避免 downloadSize=0 导致进度除零
            GetContentRequest();
            return true;
        }

        private bool UpdateContentRequest()
        {
            var deltaBytes = _content.downloadedBytes - _lastRequestDownloadedBytes;
            _request.OnReceiveBytes(deltaBytes);
            _lastRequestDownloadedBytes = _content.downloadedBytes;

            if (!_content.isDone)
            {
                if (deltaBytes > 0)
                    _stallTimer = UnityEngine.Time.realtimeSinceStartup;
                else if (UnityEngine.Time.realtimeSinceStartup - _stallTimer > StallTimeout)
                {
                    UnityEngine.Debug.LogWarning($"[xasset] 下载停滞超过{StallTimeout}s: {_request.url}");
                    _content.Abort();
                    return TryRetry($"stall >{StallTimeout}s");
                }
                return true;
            }

            UnityEngine.Debug.Log($"[xasset] 下载完成: {_request.url} error={_content.error}");

            if (!string.IsNullOrEmpty(_content.error))
                return TryRetry(_content.error);

            // 下载成功，走校验流程
            _request.downloadedBytes = _request.content.GetDownloadedBytes();
            _request.error = _content.error;
            _request.VerifyContent();
            _step = Step.Ended;
            Dispose();
            return false;
        }

        private bool TryRetry(string error)
        {
            Dispose(); // 释放失败的 UWR，清空 _content/_header
            if (_retryCount < MaxRetries)
            {
                _retryCount++;
                int delaySec = 1;// _retryCount <= 5 ? (1 << (_retryCount - 1)) : 30; // 1,2,4,8,16,30s
                UnityEngine.Debug.LogWarning(
                    $"[xasset] 下载失败(第{_retryCount}/{MaxRetries}次重试)，{delaySec}s后重试: " +
                    $"{_request.url} err={error}");
                Downloader.OnDownloadRetry?.Invoke(_request.url, _retryCount);
                _retryWaitUntil = UnityEngine.Time.realtimeSinceStartup + delaySec;
                _step = Step.RetryWait;
                return true; // 继续 Update 循环，isDone 保持 false
            }
            UnityEngine.Debug.LogError(
                $"[xasset] 下载失败，已达最大重试次数({MaxRetries}): {_request.url} err={error}");
            _request.SetResult(DownloadRequest.Result.Failed, error);
            _step = Step.Ended;
            return false;
        }

        private void GetContentRequest()
        {
            _content = UnityWebRequest.Get(_request.url);
            _stallTimer = UnityEngine.Time.realtimeSinceStartup;
            UnityEngine.Debug.Log($"[xasset] GET {_request.url} resumeFrom={_request.downloadedBytes}");
            if (_request.downloadedBytes > 0 && _request.downloadedBytes < _request.downloadSize)
            {
#if UNITY_2019_1_OR_NEWER
                _content.SetRequestHeader("Range", $"bytes={_request.downloadedBytes}-");
                _content.downloadHandler = new DownloadHandlerFile(_request.savePath, true);
#else
                _request.downloadedBytes = 0;
                _content.downloadHandler = new DownloadHandlerFile(_request.savePath);
#endif
            }
            else
            {
                _request.downloadedBytes = 0;
                _content.downloadHandler = new DownloadHandlerFile(_request.savePath);
            }

            _content.certificateHandler = new DownloadCertificateHandler();
            _content.disposeDownloadHandlerOnDispose = true;
            _content.disposeCertificateHandlerOnDispose = true;
            _content.disposeUploadHandlerOnDispose = true;
            _content.SendWebRequest();
            _lastRequestDownloadedBytes = 0;
            _step = Step.GetContent;
            _request.BeganSample();
        }

        private void Dispose()
        {
            if (_header != null)
            {
                _header.Dispose();
                _header = null;
            }

            if (_content == null) return;
            _content.Dispose();
            _content = null;
        }

        private enum Step
        {
            GetHeader,
            GetContent,
            RetryWait,
            Ended
        }
    }
}