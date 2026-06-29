using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace xasset {
    public class RemoteAssetRequestHandler {
        protected enum Step {
            CheckResSize,
            Download,
            LoadLocal
        }

        protected RemoteAssetRequest _request {
            get;
            set;
        }

        private static bool NeedCheckResSize = false;
        protected Step _step;

        private UnityWebRequest LocalContentWebRequest {
            get;
            set;
        }

        private UnityWebRequest HeadWebRequest {
            get;
            set;
        }

        private UnityWebRequest RemoteContentWebRequest {
            get;
            set;
        }

        protected string _savePath;
        protected int _retryTimes;
        protected byte[] bytes;
        protected ulong localFileSize;

        public void OnStart() {
            Debug.Log("OnStart");
            _retryTimes = 0;
            var path = _request.path;
            var url = path;

            if (_request.isLocalUrl) {
                _savePath = new System.Uri(_request.path).AbsoluteUri;
                _step = Step.LoadLocal;
                return;
            }
            _savePath = SandboxStorage.GetSandboxPath(GetMd5(url));
            var file = new FileInfo(_savePath);
            if (file.Exists) {
                localFileSize = (ulong)file.Length;
                _step = Step.CheckResSize;
            } else {
                StartDownload(url);
            }
        }

        public void Update() {
            switch (_step) {
                case Step.Download:
                    UpdateDownload();
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

        private void UpdateDownload() {
            if (RemoteContentWebRequest == null) {
                Debug.LogError("RemoteContentWebRequest is Null");
                _request.SetResult(Request.Result.Failed, "_downloadAsync is Null");
                return;
            }

            if (_request == null) {
                Debug.LogError("UpdateDownload _request is Null");
                return;
            }

            _request.progress = RemoteContentWebRequest.downloadProgress;
            if (!RemoteContentWebRequest.isDone)
                return;

            if (_request == null) {
                Debug.LogError("OnDownloadCompleted _request is Null");
                return;
            }

            if (RemoteContentWebRequest.result == UnityWebRequest.Result.Success) {
                bytes = RemoteContentWebRequest.downloadHandler.data;
                if (bytes == null || bytes.Length == 0) {
                    _request.SetResult(Request.Result.Failed, "bytes null");
                    return;
                }
                // 建立写沙盒的请求
                SandboxStorage.DownLoadFinish(RemoteContentWebRequest.downloadedBytes, _savePath, bytes);
                _request.asset = bytes;
                _request.SetResult(Request.Result.Success);
                return;
            } else {
                if (RemoteContentWebRequest == null) {
                    Debug.LogError("RemoteContentWebRequest is Null " + _request.path);
                    _request.SetResult(Request.Result.Failed, "RemoteContentWebRequest is Null");
                    return;
                }
                // 失败才重试，取消下载不重试
                if (_retryTimes < Downloader.MaxRetryTimes) {
                    StartDownload(_request.path);
                    _retryTimes++;
                    return;
                }
            }
            Debug.LogError("Load Asset Download Fail " + _request.path);
            _request.SetResult(Request.Result.Failed, RemoteContentWebRequest?.error);

        }

        private void UpdateLoadLocal() {
            if (!_request.isLocalUrl) {
                try {
                    bytes = File.ReadAllBytes(_savePath);
                    _request.asset = bytes;
                    _request.SetResult(Request.Result.Success);
                } catch (Exception msg) {
                    OnLocalLoadFail(msg.Message);
                }
                return;
            }

            if (LocalContentWebRequest == null) {
                LocalContentWebRequest = UnityWebRequest.Get(_savePath);
                LocalContentWebRequest.SendWebRequest();
            }

            if (!LocalContentWebRequest.isDone) return;
            if (!string.IsNullOrEmpty(LocalContentWebRequest.error)) {
                OnLocalLoadFail(LocalContentWebRequest.error);
                return;
            }

            bytes = LocalContentWebRequest.downloadHandler.data;
            _request.asset = bytes;
            _request.SetResult(Request.Result.Success);
        }

        private void UpdateCheckResSize() {
            if (NeedCheckResSize == false) {
                // 不需要校验本地文件的话 直接跳过这个阶段
                _step = Step.LoadLocal;
                return;
            }

            if (HeadWebRequest == null) {
                HeadWebRequest = UnityWebRequest.Head(_request.path);
                HeadWebRequest.SendWebRequest();
            }

            if (!HeadWebRequest.isDone) return;
            if (!string.IsNullOrEmpty(HeadWebRequest.error)) {
                _request.SetResult(Request.Result.Failed, HeadWebRequest.error);
                return;
            }

            const string key = "Content-Length";
            var value = HeadWebRequest.GetResponseHeader(key);
            if (ulong.TryParse(value, out var size)) {
                if (size == localFileSize) {
                    _step = Step.LoadLocal;
                    return;
                }
            }
            StartDownload(_request.path);
        }

        public void Dispose() {
            bytes = null;
            _retryTimes = 0;

            LocalContentWebRequest?.Dispose();
            LocalContentWebRequest = null;

            RemoteContentWebRequest?.Dispose();
            RemoteContentWebRequest = null;

            HeadWebRequest?.Dispose();
            HeadWebRequest = null;
        }

        public void WaitForCompletion() {
            while (!_request.isDone) Update();
        }

        protected virtual void OnLocalLoadFail(string msg) {
            // 本地加载不存在远端地址
            if (_request.isLocalUrl) {
                _request.SetResult(Request.Result.Failed, msg);
                return;
            }

            // 删除本地损坏的文件
            SandboxStorage.DeleteFile(_savePath);

            // 没有下载过远端
            if (RemoteContentWebRequest == null) {
                StartDownload(_request.path);
            } else {
                _request.SetResult(Request.Result.Failed, msg);
            }
        }

        private string GetMd5(string url) {
            var bytes = Encoding.UTF8.GetBytes(url);
            var md5 = Utility.ComputeHash(bytes);
            return md5;
        }

        private void StartDownload(string url) {
            RemoteContentWebRequest?.Dispose();
            RemoteContentWebRequest = UnityWebRequest.Get(url);
            RemoteContentWebRequest.SendWebRequest();
            _step = Step.Download;
        }
        public static RemoteAssetRequestHandler CreateInstance(RemoteAssetRequest assetRequest) {
            return new RemoteAssetRequestHandler { _request = assetRequest };
        }
    }
}
