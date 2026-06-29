using System;
using System.Collections.Generic;

namespace xasset
{
    public class VersionsRequest : Request
    {
        private static readonly List<VersionsRequest> AllRequests = new List<VersionsRequest>();
        private readonly Queue<Version> queue = new Queue<Version>();
        private DownloadContentRequestBatch _contents;
        private DownloadContentRequest _downloadContent;
        private int _retryTimes;
        private Step _step = Step.DownloadHeader;
        public Versions versions { get; private set; }
        public string url { get; set; }
        public string hash { get; set; }
        public ulong size { get; set; }

        protected override void OnStart()
        {
            AllRequests.Add(this);

            if (Assets.SimulationMode || Assets.OfflineMode)
            {
                SetResult(Result.Success);
                return;
            }

            if (string.IsNullOrEmpty(url)) url = Assets.GetDownloadURL($"{Versions.Filename}");
            UnityEngine.Debug.Log($"[xasset] GetVersionsAsync 开始下载版本头: {url} 队列中第{AllRequests.Count}个");
            var savePath = Assets.GetTemporaryCachePath(Versions.Filename);
            var content = DownloadContent.Get(url, savePath, hash, size);
            content.Clear();
            _downloadContent = Downloader.DownloadAsync(content);
            _step = Step.DownloadHeader;
        }

        protected override void OnUpdated()
        {
            // 防止并行执行多个请求。
            if (AllRequests.Count > 1 && AllRequests[0] != this) return;

            switch (_step)
            {
                case Step.DownloadHeader:
                    UpdateDownloadHeader();
                    break;
                case Step.DownloadContents:
                    UpdateDownloadContents();
                    break;
                case Step.LoadContents:
                    UpdateLoadVersions();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateLoadVersions()
        {
            while (queue.Count > 0)
            {
                var version = queue.Dequeue();
                if (Assets.Versions.TryGetVersion(version.build, out var value) && value.hash.Equals(version.hash))
                {
                    version.manifest = value.manifest;
                }
                else
                {
                    var path = Assets.GetDownloadDataPath($"{version.file}");
                    var manifest = Utility.LoadFromFile<Manifest>(path);
                    manifest.build = version.build;
                    manifest.name = version.file;
                    version.manifest = manifest;
                }

                if (Scheduler.Busy) return;
            }

            UnityEngine.Debug.Log("[xasset] GetVersionsAsync 完成");
            SetResult(Result.Success);
        }

        private void UpdateDownloadContents()
        {
            progress = 0.3f + _contents.progress * 0.7f;
            if (!_contents.isDone) return;
            if (_contents.result == DownloadRequest.Result.Failed)
            {
                Logger.W($"Failed to download versions with error {_contents.error}.");
                if (_retryTimes > Downloader.MaxRetryTimes)
                {
                    UnityEngine.Debug.LogError($"[xasset] GetVersionsAsync 下载版本内容失败(重试{_retryTimes}次): {_contents.error}");
                    SetResult(Result.Failed, _contents.error);
                    return;
                }

                UnityEngine.Debug.LogWarning($"[xasset] GetVersionsAsync 下载版本内容失败，重试第{_retryTimes + 1}次");
                _contents.Retry();
                _retryTimes++;
                return;
            }

            UnityEngine.Debug.Log("[xasset] GetVersionsAsync 版本内容下载成功，开始加载Manifest");
            foreach (var version in versions.data) queue.Enqueue(version);

            _step = Step.LoadContents;
        }

        private void UpdateDownloadHeader()
        {
            progress = _downloadContent.progress * 0.3f;
            if (!_downloadContent.isDone) return;

            if (!string.IsNullOrEmpty(_downloadContent.error))
            {
                UnityEngine.Debug.LogError($"[xasset] GetVersionsAsync 下载版本头失败: {_downloadContent.error}");
                SetResult(Result.Failed, _downloadContent.error);
                return;
            }
            if (!string.IsNullOrEmpty(hash))
            {
                var _hash = Utility.ComputeHash(_downloadContent.savePath);
                if (_hash != hash)
                {
                    UnityEngine.Debug.LogError($"[xasset] GetVersionsAsync 版本头Hash校验失败: 期望={hash} 实际={_hash}");
                    SetResult(Result.Failed, $"download hash {_hash} mismatch {hash} ");
                    return;
                }
            }

            versions = Utility.LoadFromFile<Versions>(_downloadContent.savePath);
            var changes = new List<Version>();
            foreach (var item in versions.data)
            {
                if (Assets.IsDownloaded(item)) continue;
                changes.Add(item);
            }

            UnityEngine.Debug.Log($"[xasset] GetVersionsAsync 版本头下载成功，需下载版本文件数={changes.Count}");
            if (changes.Count > 0)
            {
                _contents = DownloadContentRequestBatch.Create();
                foreach (var item in changes)
                {
                    var downloadURL = Assets.GetDownloadURL($"{item.file}");
                    var savePath = Assets.GetDownloadDataPath($"{item.file}");
                    var content = DownloadContent.Get(downloadURL, savePath, item.hash, item.size);
                    _contents.AddContent(content); // Fix 8: AddContent 内部已累加 downloadSize，删除重复赋值
                    UnityEngine.Debug.Log($"[xasset] GetVersionsAsync 待下载版本文件: {downloadURL}");
                }

                _contents.SendRequest();
                _step = Step.DownloadContents;
            }
            else
            {
                foreach (var version in versions.data) queue.Enqueue(version);
                _step = Step.LoadContents;
            }
        }

        protected override void OnCompleted()
        {
            AllRequests.Remove(this);
        }

        private enum Step
        {
            DownloadHeader,
            DownloadContents,
            LoadContents
        }
    }
}