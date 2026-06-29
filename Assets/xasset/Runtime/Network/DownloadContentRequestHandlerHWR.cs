using System;
using System.IO;
using System.Net;
using System.Threading;

namespace xasset
{
    public struct DownloadContentRequestHandlerHWR : DownloadContentRequestHandler
    {
        private readonly byte[] _readBuffer;
        private readonly DownloadContentRequest _request;
        private Thread _thread;

        // Fix 1: 用 volatile bool 替代 Thread.Abort()
        private volatile bool _cancelled;

        // Fix 2: 用 volatile 信号将结果从后台线程传递到主线程，
        //        避免直接在后台线程调用 SetResult/VerifyContent（跨线程写 _request 状态）
        private volatile bool _threadDone;
        private DownloadRequest.Result _threadResult; // 写在 _threadDone=true 之前，volatile 保证可见
        private string _threadError;

        // Fix 5: 缓冲区从 10KB 提升到 512KB，减少系统调用次数
        public static int bufferSize { get; set; } = 512 * 1024;

        public static string Username { get; set; } = string.Empty;
        public static string Password { get; set; } = string.Empty;

        public DownloadContentRequestHandlerHWR(DownloadContentRequest request)
        {
            _readBuffer = new byte[bufferSize];
            _request = request;
            _thread = null;
            _cancelled = false;
            _threadDone = false;
            _threadResult = DownloadRequest.Result.Default;
            _threadError = null;
        }

        public void OnStart()
        {
            if (_request.status == DownloadRequest.Status.Progressing) return;
            _cancelled = false;
            _threadDone = false;
            _threadResult = DownloadRequest.Result.Default;
            _threadError = null;
            _thread = new Thread(Downloading) { IsBackground = true };
            _thread.Start();
        }

        public void OnPause(bool paused)
        {
            // HWR 通过 _request.status 检查，OnReceiveData 循环内会感知暂停状态
        }

        public bool Update()
        {
            if (_request.status == DownloadRequest.Status.Paused) return true;
            if (_request.status != DownloadRequest.Status.Progressing) return false;

            // Fix 2: 在主线程检查后台完成信号，再调用 VerifyContent/SetResult
            if (!_threadDone) return true;

            if (_threadResult == DownloadRequest.Result.Default)
                _request.VerifyContent(); // 文件校验留在主线程执行
            else
                _request.SetResult(_threadResult, _threadError);
            return false;
        }

        public void OnCancel()
        {
            // Fix 1: 用标志位取代 Thread.Abort()
            _cancelled = true;
        }

        private void OnReceiveData(Stream reader, Stream writer)
        {
            if (_request.downloadedBytes > 0) writer.Seek(0, SeekOrigin.End);
            _request.BeganSample();
            while (_request.downloadedBytes < _request.downloadSize)
            {
                if (_cancelled || _request.result == DownloadRequest.Result.Cancelled) break;
                if (_request.status == DownloadRequest.Status.Paused)
                {
                    _request.bandwidth = 0;
                    Thread.Sleep(100); // 暂停时避免忙等
                    continue;
                }
                var len = reader.Read(_readBuffer, 0, _readBuffer.Length);
                if (len <= 0) break;
                writer.Write(_readBuffer, 0, len);
                _request.OnReceiveBytes((ulong)len);
            }
            writer.Flush(); // Fix 1 附带：确保数据完整刷入磁盘
        }

        private void Downloading()
        {
            try
            {
                if (Downloader.SimulationMode)
                    DownloadingSimulation();
                else if (_request.url.StartsWith("http"))
                    DownloadingHttp();
                else if (_request.url.StartsWith("ftp"))
                    DownloadingFtp();

                // Default 表示下载流程正常结束，由主线程执行 VerifyContent
                if (!_cancelled)
                    _threadResult = DownloadRequest.Result.Default;
            }
            catch (Exception e)
            {
                if (_cancelled) return;

                if (e is WebException webException &&
                    webException.Response is HttpWebResponse response &&
                    response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    // 416: 请求范围超出文件末尾，说明文件已完整
                    if (_request.downloadSize == 0)
                    {
                        var file = new FileInfo(_request.savePath);
                        if (file.Exists) _request.OnGetDownloadSize((ulong)file.Length);
                    }
                    _threadResult = DownloadRequest.Result.Success;
                }
                else
                {
                    _threadResult = DownloadRequest.Result.Failed;
                    _threadError = e.Message;
                }
            }
            finally
            {
                if (!_cancelled)
                    _threadDone = true; // volatile 写，保证 _threadResult/_threadError 对主线程可见
            }
        }

        private void DownloadingSimulation()
        {
            var path = _request.url.Replace(Assets.Protocol, string.Empty);
            if (!File.Exists(path))
                throw new FileNotFoundException(DownloadErrors.FileNotExist, path);

            using (var writer = _request.downloadedBytes > 0
                       ? File.OpenWrite(_request.savePath)
                       : File.Create(_request.savePath))
            using (var reader = File.OpenRead(path))
            {
                _request.OnGetDownloadSize((ulong)reader.Length);
                OnReceiveData(reader, writer);
            }
        }

        private void DownloadingHttp()
        {
            var httpWebRequest = WebRequest.CreateHttp(_request.url);
            if (_request.downloadedBytes > 0)
                httpWebRequest.AddRange((long)_request.downloadedBytes);

            using (var writer = _request.downloadedBytes > 0
                       ? File.OpenWrite(_request.savePath)
                       : File.Create(_request.savePath))
            using (var response = httpWebRequest.GetResponse())
            {
                // Fix 4: ContentLength=-1（chunked 编码）时 (ulong)(-1L)=ulong.MaxValue，导致溢出死循环
                var serverLen = response.ContentLength;
                if (serverLen >= 0)
                    _request.OnGetDownloadSize((ulong)serverLen + _request.downloadedBytes);
                else
                    _request.OnGetDownloadSize(_request.content.size); // 回退为 manifest 中记录的大小

                using (var reader = response.GetResponseStream())
                    OnReceiveData(reader, writer);
            }
        }

        private void DownloadingFtp()
        {
            var ftpWebRequest = (FtpWebRequest)WebRequest.Create(_request.url);
            ftpWebRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            if (!string.IsNullOrEmpty(Username))
                ftpWebRequest.Credentials = new NetworkCredential(Username, Password);
            if (_request.downloadedBytes > 0)
                ftpWebRequest.ContentOffset = (long)_request.downloadedBytes;

            using (var writer = _request.downloadedBytes > 0
                       ? File.OpenWrite(_request.savePath)
                       : File.Create(_request.savePath))
            using (var response = ftpWebRequest.GetResponse())
            {
                // Fix 4: 同 HTTP，防止 ContentLength=-1 溢出
                var serverLen = response.ContentLength;
                if (serverLen >= 0)
                    _request.OnGetDownloadSize((ulong)serverLen + _request.downloadedBytes);
                else
                    _request.OnGetDownloadSize(_request.content.size);

                using (var reader = response.GetResponseStream())
                    OnReceiveData(reader, writer);
            }
        }
    }
}
