using System.IO;
using UnityEngine.Networking;

namespace xasset
{
    public struct DownloadImageRequestHandlerUWR : DownloadContentRequestHandler
    {
        private UnityWebRequest _content;
        private UnityWebRequest _header;
        private ulong _lastRequestDownloadedBytes;
        private Step _step;
        private readonly DownloadContentRequest _request;

        public DownloadImageRequestHandlerUWR(DownloadContentRequest request)
        {
            _header = null;
            _content = null;
            _lastRequestDownloadedBytes = 0;
            _step = Step.GetHeader;
            _request = request;
        }

        public void OnStart()
        {
            _step = Step.GetHeader;
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
            if (_step == Step.GetHeader) return UpdateHeaderRequest();
            return _step == Step.GetContent && UpdateContentRequest();
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
            else
            {
                if (!Downloader.Resumable)
                    GetContentRequest();
                else
                    GetHeadRequest();
            }

            _request.status = DownloadRequest.Status.Progressing;
        }

        private void GetHeadRequest()
        {
            _header = UnityWebRequest.Head(_request.url);
            _header.SendWebRequest();
            _step = Step.GetHeader;
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

            _request.SetResult(DownloadRequest.Result.Success, DownloadErrors.NothingToDownload);
            return false;
        }

        private bool UpdateContentRequest()
        {
            var deltaBytes = _content.downloadedBytes - _lastRequestDownloadedBytes;
            _request.OnReceiveBytes(deltaBytes);
            _lastRequestDownloadedBytes = _content.downloadedBytes;
            if (!_content.isDone) return true;
            _request.downloadedBytes = _request.content.GetDownloadedBytes();
            _request.error = _content.error;
            _request.VerifyImage(_content.downloadHandler);
            _step = Step.Ended;
            Dispose();
            return false;
        }

        private void GetContentRequest()
        {
            _content = UnityWebRequest.Get(_request.url);

            _request.downloadedBytes = 0;
            if (_content.url.EndsWith(".jpg") || _content.url.EndsWith(".jpeg") || _content.url.EndsWith(".png"))
            {
                _content.downloadHandler = new DownloadHandlerTexture(true);
            }

            _content.disposeDownloadHandlerOnDispose = true;
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
            Ended
        }
    }
}