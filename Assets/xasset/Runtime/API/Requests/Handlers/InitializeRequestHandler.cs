using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace xasset
{
    public class InitializeRequestHandlerRuntime : InitializeRequestHandler
    {
        private readonly Queue<Version> _queue = new Queue<Version>();
        private readonly List<UnityWebRequest> _requests = new List<UnityWebRequest>();
        private Versions _downloadVersions;
        private Step _step = Step.LoadPlayerVersions;
        private UnityWebRequest _unityWebRequest;
        private InitializeRequest request { get; set; }

        public void OnStart()
        {
            if (Application.isEditor && Assets.OfflineMode)
            {
                LoadVersionsHeader(Assets.GetPlayerDataURl(Versions.Filename).Replace("/Bundles/", "/BundlesCache/"));
                return;
            }

            var url = Assets.GetTopPlayerDataURl(PlayerAssets.Filename);
            Debug.Log($"[xasset] InitializeAsync Step=LoadPlayerAssets url={url}");
            _unityWebRequest = UnityWebRequest.Get(url);
            _unityWebRequest.timeout = 15;
            _unityWebRequest.SendWebRequest();
            _step = Step.LoadPlayerAssets;
        }


        public void OnUpdated()
        {
            switch (_step)
            {
                case Step.LoadPlayerAssets:
                    UpdateLoadingPlayerAssets();
                    break;
                case Step.LoadVersionsHeader:
                    UpdateLoadVersionsHeader();
                    break;
                case Step.LoadPlayerVersions:
                    UpdateLoadPlayerVersions();
                    break;
                case Step.LoadVersionsContent:
                    UpdateLoadVersions();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LoadVersionsHeader(string url)
        {
            Debug.Log($"[xasset] InitializeAsync Step=LoadVersionsHeader url={url}");
            _unityWebRequest = UnityWebRequest.Get(url);
            _unityWebRequest.timeout = 15;
            _unityWebRequest.SendWebRequest();
            _step = Step.LoadVersionsHeader;
        }

        private void UpdateLoadVersions()
        {
            while (_queue.Count > 0)
            {
                var version = _queue.Dequeue();
                var path = Assets.GetDownloadDataPath($"{version.file}");
                var manifest = Utility.LoadFromFile<Manifest>(path);
                manifest.build = version.build;
                manifest.name = version.file;
                version.manifest = manifest;
                if (Scheduler.Busy) return;
            }

            Debug.Log("[xasset] InitializeAsync 完成");
            request.SetResult(Request.Result.Success);
        }


        internal static InitializeRequestHandler CreateInstance(InitializeRequest initializeRequest)
        {
            return new InitializeRequestHandlerRuntime { request = initializeRequest };
        }

        private void UpdateLoadPlayerVersions()
        {
            for (var index = 0; index < _requests.Count; index++)
            {
                var unityWebRequest = _requests[index];
                if (!unityWebRequest.isDone) return;
                _requests.RemoveAt(index);
                index--;
                if (!string.IsNullOrEmpty(unityWebRequest.error))
                {
                    Debug.LogError($"[xasset] InitializeAsync 下载版本文件失败: {unityWebRequest.url} {unityWebRequest.error}");
                    request.SetResult(Request.Result.Failed, unityWebRequest.url + unityWebRequest.error);
                }
                unityWebRequest.Dispose();
            }

            Debug.Log("[xasset] InitializeAsync Step=LoadVersionsContent 开始加载Manifest");
            // 不用远端版本文件
            var path = Assets.GetDownloadDataPath(Versions.Filename);
            _downloadVersions = Utility.LoadFromFile<Versions>(path);
            if (_downloadVersions != null && _downloadVersions.timestamp > Assets.Versions.timestamp)
            {
                Debug.Log($"[xasset] 使用本地缓存Versions timestamp={_downloadVersions.timestamp}");
                Assets.Versions = _downloadVersions;
            }
            foreach (var version in Assets.Versions.data)
                _queue.Enqueue(version);
            Debug.Log($"[xasset] InitializeAsync 需加载Manifest数量={_queue.Count}");
            _step = Step.LoadVersionsContent;
        }

        private void UpdateLoadingPlayerAssets()
        {
            if (!_unityWebRequest.isDone) return;
            if (!string.IsNullOrEmpty(_unityWebRequest.error))
            {
                Debug.LogError($"[xasset] InitializeAsync 加载PlayerAssets失败: {_unityWebRequest.url} {_unityWebRequest.error}");
                request.SetResult(Request.Result.Failed, _unityWebRequest.url + _unityWebRequest.error);
                return;
            }

            Debug.Log($"[xasset] InitializeAsync PlayerAssets加载成功，进入LoadVersionsHeader");
            Assets.PlayerAssets = Utility.LoadFromJson<PlayerAssets>(_unityWebRequest.downloadHandler.text);

            foreach (var asset in Assets.PlayerAssets.data)
            {
                var content = DownloadContent.Get(asset.key, null);
                content.status = DownloadContent.Status.Downloaded;
            }

            _unityWebRequest.Dispose();
            LoadVersionsHeader(Assets.GetPlayerDataURl(Versions.Filename));
        }

        private void UpdateLoadVersionsHeader()
        {
            if (!_unityWebRequest.isDone) return;
            if (!string.IsNullOrEmpty(_unityWebRequest.error))
            {
                Debug.LogError($"[xasset] InitializeAsync 加载VersionsHeader失败: {_unityWebRequest.url} {_unityWebRequest.error}");
                request.SetResult(Request.Result.Failed, _unityWebRequest.url + _unityWebRequest.error);
                return;
            }

            var json = _unityWebRequest.downloadHandler.text;
            Logger.D($"LoadVersionsHeader {json}");
            Debug.Log($"[xasset] InitializeAsync VersionsHeader加载成功");
            Assets.Versions = Utility.LoadFromJson<Versions>(json);
            _unityWebRequest.Dispose();
            var downloadCount = 0;
            foreach (var version in Assets.Versions.data)
            {
                if (Assets.IsDownloaded(version)) continue;
                var url = Assets.GetTopPlayerDataURl($"{version.file}");
                var savePath = Assets.GetDownloadDataPath($"{version.file}");
                var unityWebRequest = UnityWebRequest.Get(url);
                unityWebRequest.timeout = 15;
                unityWebRequest.downloadHandler = new DownloadHandlerFile(savePath);
                unityWebRequest.SendWebRequest();
                _requests.Add(unityWebRequest);
                downloadCount++;
                Debug.Log($"[xasset] InitializeAsync 下载版本文件: {url}");
            }
            Debug.Log($"[xasset] InitializeAsync 需下载版本文件数={downloadCount}，进入LoadPlayerVersions");
            _step = Step.LoadPlayerVersions;
        }

        private enum Step
        {
            LoadPlayerVersions,
            LoadPlayerAssets,
            LoadVersionsHeader,
            LoadVersionsContent
        }
    }

    public class InitializeRequestHandlerMultVersions : InitializeRequestHandler
    {
        public static string Quality = "High";
        private readonly Queue<Version> _queue = new Queue<Version>();
        private readonly List<UnityWebRequest> _requests = new List<UnityWebRequest>();
        private Versions _downloadVersions;
        private Step _step = Step.LoadPlayerVersions;
        private UnityWebRequest _unityWebRequest;
        private InitializeRequest request { get; set; }

        public void OnStart()
        {
            if (Application.isEditor && Assets.OfflineMode)
            {
                LoadVersionsHeader(Assets.GetPlayerDataURl(Versions.Filename).Replace("/Bundles/", "/BundlesCache/"));
                return;
            }

            var url = Assets.GetTopPlayerDataURl(PlayerAssets.Filename);
            Debug.Log($"[xasset] InitializeAsync(Mult) Step=LoadPlayerAssets url={url}");
            _unityWebRequest = UnityWebRequest.Get(url);
            _unityWebRequest.timeout = 15;
            _unityWebRequest.SendWebRequest();
            _step = Step.LoadPlayerAssets;
        }

        public Versions GetCurVersions()
        {
            return Assets.Versions;
        }

        public void SetCurVersions(Versions versions)
        {
            Assets.Versions = versions;
        }

        public void OnUpdated()
        {
            switch (_step)
            {
                case Step.LoadPlayerAssets:
                    UpdateLoadingPlayerAssets();
                    break;
                case Step.LoadVersionsHeader:
                    UpdateLoadVersionsHeader();
                    break;
                case Step.LoadPlayerVersions:
                    UpdateLoadPlayerVersions();
                    break;
                case Step.LoadVersionsContent:
                    UpdateLoadVersions();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LoadVersionsHeader(string url)
        {
            Debug.Log($"[xasset] InitializeAsync(Mult) Step=LoadVersionsHeader url={url}");
            _unityWebRequest = UnityWebRequest.Get(url);
            _unityWebRequest.timeout = 15;
            _unityWebRequest.SendWebRequest();
            _step = Step.LoadVersionsHeader;
        }

        private void UpdateLoadVersions()
        {
            while (_queue.Count > 0)
            {
                var version = _queue.Dequeue();
                var path = Assets.GetDownloadDataPath($"{version.file}");
                var manifest = Utility.LoadFromFile<Manifest>(path);
                manifest.build = version.build;
                manifest.name = version.file;
                version.manifest = manifest;
                if (Scheduler.Busy) return;
            }
            Debug.Log("[xasset] InitializeAsync(Mult) 完成");
            request.SetResult(Request.Result.Success);
        }


        internal static InitializeRequestHandler CreateInstance(InitializeRequest initializeRequest)
        {
            return new InitializeRequestHandlerMultVersions { request = initializeRequest };
        }

        private void UpdateLoadPlayerVersions()
        {
            for (var index = 0; index < _requests.Count; index++)
            {
                var unityWebRequest = _requests[index];
                if (!unityWebRequest.isDone) return;
                _requests.RemoveAt(index);
                index--;
                if (!string.IsNullOrEmpty(unityWebRequest.error))
                {
                    Debug.LogError($"[xasset] InitializeAsync(Mult) 下载版本文件失败: {unityWebRequest.url} {unityWebRequest.error}");
                    request.SetResult(Request.Result.Failed, unityWebRequest.url + unityWebRequest.error);
                }
                unityWebRequest.Dispose();
            }

            var versions = GetCurVersions();
            #region 目前以包内Version为主，只更新部分build，所以这里先屏蔽
            var path = Assets.GetDownloadDataPath(Versions.Filename);
            _downloadVersions = Utility.LoadFromFile<Versions>(path);
            if (_downloadVersions != null && _downloadVersions.timestamp > versions.timestamp)
            {
                Debug.Log($"[xasset] InitializeAsync(Mult) 使用本地缓存Versions timestamp={_downloadVersions.timestamp}");
                SetCurVersions(_downloadVersions);
                versions = GetCurVersions();
            }
            #endregion
            foreach (var version in versions.data)
                _queue.Enqueue(version);
            Debug.Log($"[xasset] InitializeAsync(Mult) Step=LoadVersionsContent manifest数={_queue.Count}");
            _step = Step.LoadVersionsContent;
        }

        private void UpdateLoadingPlayerAssets()
        {
            if (!_unityWebRequest.isDone) return;
            if (!string.IsNullOrEmpty(_unityWebRequest.error))
            {
                Debug.LogError($"[xasset] InitializeAsync(Mult) 加载PlayerAssets失败: {_unityWebRequest.url} {_unityWebRequest.error}");
                request.SetResult(Request.Result.Failed, _unityWebRequest.url + _unityWebRequest.error);
                return;
            }

            Debug.Log("[xasset] InitializeAsync(Mult) PlayerAssets加载成功");
            Assets.PlayerAssets = Utility.LoadFromJson<PlayerAssets>(_unityWebRequest.downloadHandler.text);

            foreach (var asset in Assets.PlayerAssets.data)
            {
                var content = DownloadContent.Get(asset.key, null);
                content.status = DownloadContent.Status.Downloaded;
            }

            _unityWebRequest.Dispose();
            LoadVersionsHeader(Assets.GetPlayerDataURl(Versions.Filename));
        }

        private void UpdateLoadVersionsHeader()
        {
            if (!_unityWebRequest.isDone) return;
            if (!string.IsNullOrEmpty(_unityWebRequest.error))
            {
                Debug.LogError($"[xasset] InitializeAsync(Mult) 加载VersionsHeader失败: {_unityWebRequest.url} {_unityWebRequest.error}");
                request.SetResult(Request.Result.Failed, _unityWebRequest.url + _unityWebRequest.error);
                return;
            }

            var json = _unityWebRequest.downloadHandler.text;
            Logger.D($"LoadVersionsHeader {json}");
            Debug.Log("[xasset] InitializeAsync(Mult) VersionsHeader加载成功");
            var versions = Utility.LoadFromJson<Versions>(json);
            SetCurVersions(versions);
            _unityWebRequest.Dispose();
            var downloadCount = 0;
            foreach (var version in versions.data)
            {
                if (Assets.IsDownloaded(version)) continue;
                var url = Assets.GetTopPlayerDataURl($"{version.file}");
                var savePath = Assets.GetDownloadDataPath($"{version.file}");
                var unityWebRequest = UnityWebRequest.Get(url);
                unityWebRequest.timeout = 15;
                unityWebRequest.downloadHandler = new DownloadHandlerFile(savePath);
                unityWebRequest.SendWebRequest();
                _requests.Add(unityWebRequest);
                downloadCount++;
                Debug.Log($"[xasset] InitializeAsync(Mult) 下载版本文件: {url}");
            }
            Debug.Log($"[xasset] InitializeAsync(Mult) 需下载版本文件数={downloadCount}，进入LoadPlayerVersions");
            _step = Step.LoadPlayerVersions;
        }

        private enum Step
        {
            LoadPlayerVersions,
            LoadPlayerAssets,
            LoadVersionsHeader,
            LoadVersionsContent,
        }
    }

    public interface InitializeRequestHandler
    {
        void OnStart();
        void OnUpdated();
    }
}