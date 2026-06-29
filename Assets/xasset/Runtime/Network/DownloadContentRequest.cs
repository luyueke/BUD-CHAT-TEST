using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace xasset
{
    public class DownloadContentRequest : DownloadRequest
    {
        public DownloadContentRequestHandler handler { get; set; }
        public Action<DownloadContentRequest> completed { get; set; }
        public DownloadContent content { get; set; }
        public string savePath => content.savePath;
        public string url => content.url;

        public byte[] bytes;

        public Texture texture;

        public AssetBundle assetBundle;

        internal void OnGetDownloadSize(ulong size)
        {
            downloadSize = content.size;
            // content.size = size;
        }

        public override void Reset()
        {
            base.Reset();
            bytes = null;
            texture = null;
            assetBundle = null;
        }

        public void Start()
        {
            if (status == Status.Progressing) return;

            downloadedBytes = content.GetDownloadedBytes();
            if (downloadedBytes > 0 && downloadedBytes == content.size)
            {
                // Fix 7: 快速退出路径也需标记 Downloaded，避免下次 DownloadAsync 重复入队
                content.status = DownloadContent.Status.Downloaded;
                SetResult(Result.Success, DownloadErrors.NothingToDownload);
                return;
            }

            OnStart();
            status = Status.Progressing;
        }

        protected override void OnPause(bool paused)
        {
            handler.OnPause(paused);
        }

        protected override void OnCancel()
        {
            handler.OnCancel();
        }

        public void SendRequest()
        {
            Downloader.Queue.Enqueue(this);
        }

        public void WaitForCompletion()
        {
            if (isDone) return;
            if (Assets.IsWebGLPlatform)
            {
                SetResult(Result.Failed, "WaitForCompletion is not supported on WebGL.");
                return;
            }

            if (status == Status.Wait) Start();

            while (!isDone) Update();
        }

        public override void Retry()
        {
            Reset();
            SendRequest();
        }

        internal void VerifyImage(DownloadHandler handler)
        {
            if (result == Result.Failed || result == Result.Cancelled) return;

            if (!string.IsNullOrEmpty(error))
            {
                SetResult(Result.Failed, error);
                return;
            }

          
            if (handler is DownloadHandlerTexture)
            {
                var tex2D = ((DownloadHandlerTexture)handler).texture;
                // if (tex2D != null && tex2D.width % 4 == 0 && tex2D.height % 4 == 0)
                // {
                //     tex2D.Compress(true);
                // }
                texture = tex2D;
            }
            bytes = handler.data;
            content.status = DownloadContent.Status.Downloaded;
            SetResult(Result.Success);
        }
        
        internal void VerifyBundle(UnityWebRequest webRequest)
        {
            if (result == Result.Failed || result == Result.Cancelled) return;

            if (!string.IsNullOrEmpty(error))
            {
                SetResult(Result.Failed, error);
                return;
            }
            assetBundle = DownloadHandlerAssetBundle.GetContent(webRequest);
            content.status = DownloadContent.Status.Downloaded;
            SetResult(Result.Success);
        }

        internal void VerifyContent()
        {
            if (result == Result.Failed || result == Result.Cancelled) return;

            if (!string.IsNullOrEmpty(error))
            {
                SetResult(Result.Failed, error);
                return;
            }

            var file = new FileInfo(savePath);
            if (!file.Exists)
            {
                SetResult(Result.Failed, DownloadErrors.FileNotExist);
                return;
            }

            // Fix 3: 恢复文件大小校验，防止截断文件在 FastVerifyMode 下被误判为成功
            if ((long)content.size > 0 && file.Length != (long)content.size)
            {
                if (File.Exists(savePath)) File.Delete(savePath);
                content.downloadedBytes = 0;
                SetResult(Result.Failed, string.Format(DownloadErrors.DownloadSizeMismatch, file.Length, content.size));
                return;
            }

            if (string.IsNullOrEmpty(content.hash) || Assets.FastVerifyMode)
            {
                content.status = DownloadContent.Status.Downloaded;
                SetResult(Result.Success);
                return;
            }

            var computeHash = Utility.ComputeHash(savePath);
            if (content.hash.Equals(computeHash))
            {
                content.status = DownloadContent.Status.Downloaded;
                SetResult(Result.Success);
                return;
            }

            // Hash 不符说明文件损坏，删除以确保下次重试从头全量下载
            Debug.LogError($"[xasset] Hash校验失败，删除损坏文件: {savePath} 期望={content.hash} 实际={computeHash}");
            if (File.Exists(savePath))
                File.Delete(savePath);
            content.downloadedBytes = 0;
            SetResult(Result.Failed,
                string.Format(DownloadErrors.DownloadHashMismatch, computeHash, content.hash));
        }

        private void OnStart()
        {
            content.status = DownloadContent.Status.Downloading;
            handler.OnStart();
        }

        public void Update()
        {
            handler.Update();
        }

        public void Complete()
        {
            content.downloadedBytes = downloadedBytes;
            Logger.D($"Download {url} {result} {error}");
            var saved = completed;
            completed?.Invoke(this);
            completed -= saved;
        }

        public void Clear()
        {
            Cancel();
            downloadedBytes = 0;
            progress = 0;
            bandwidth = 0;
            content.Clear();
        }
    }
}