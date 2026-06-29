using System;
using System.Collections.Generic;
using System.IO;
using Game.OfflineRender;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using xasset;

namespace GameData.OfflineRender {
    public class OfflineRequestHandler {
        private enum Step {
            None,
            LoadAssetBundle,
            Download,
            LoadAsset,
        }

        private Step step;

        private readonly OfflineRequest request;

        private UnityWebRequest remoteContentRequest;
        private AssetBundleCreateRequest assetBundleRequest;
        private AssetBundleRequest assetRequest;
        private AssetBundle assetBundle;

        private string savePath;
        private string remotePath;
        private int retryTimes;
        private const int MaxRetryTimes = 3;
        private bool isLoadFromCache = false;

        public OfflineRequestHandler(OfflineRequest request) {
            this.request = request;
        }

        public void OnStart() {
            remotePath = request.renderData.GetPath(request.modelLODType);
            Uri uri = null;
            try {
                uri = new Uri(remotePath);
            } catch (Exception e) {
                request.SetResult(Request.Result.Failed, "Url Error:" + e.Message);
                return;
            }
            var fileName = Path.GetFileName(uri.LocalPath);
            savePath = OfflineSandboxStorage.GetSandboxPath(fileName);
            if (File.Exists(savePath)) {
                isLoadFromCache = true;
                assetBundleRequest = AssetBundle.LoadFromFileAsync(savePath);
                step = Step.LoadAssetBundle;
            } else {
                remoteContentRequest = UnityWebRequest.Get(remotePath);
                remoteContentRequest.downloadHandler = new DownloadHandlerBuffer();
                remoteContentRequest.SendWebRequest();
                step = Step.Download;
            }
        }

        public void OnUpdate() {
            switch (step) {
                case Step.LoadAssetBundle:
                    LoadAssetBundle();
                    break;
                case Step.Download:
                    DownloadAssetBundle();
                    break;
                case Step.LoadAsset:
                    LoadAsset();
                    break;
            }
        }

        private void LoadAssetBundle() {
            if (assetBundleRequest == null) {
                request.SetResult(Request.Result.Failed, "assetBundleCreateRequest == null");
                return;
            }

            if (request == null) {
                Debug.LogError("LoadLocalAssetBundle _request is Null");
                return;
            }
            request.progress = assetBundleRequest.progress;

            if (!assetBundleRequest.isDone) {
                return;
            }

            if (request == null) {
                Debug.LogError("LoadLocalAssetBundle _request is Null");
                return;
            }

            if (assetBundleRequest.assetBundle != null) {
                assetBundle = assetBundleRequest.assetBundle;
                step = Step.LoadAsset;
                return;
            }

            Debug.LogError("LoadLocalAssetBundle Fail " + request.path);
            if (isLoadFromCache && File.Exists(savePath)) {
                OfflineSandboxStorage.DeleteFile(savePath);
            }
            request.SetResult(Request.Result.Failed, "assetBundle == null");
        }

        private void DownloadAssetBundle() {
            if (remoteContentRequest == null) {
                Debug.LogError("assetBundleRequest is Null");
                request.SetResult(Request.Result.Failed, "assetBundleRequest is Null");
                return;
            }

            if (request == null) {
                Debug.LogError("UpdateDownload _request is Null");
                return;
            }

            request.progress = remoteContentRequest.downloadProgress;
            if (!remoteContentRequest.isDone)
                return;

            if (request == null) {
                Debug.LogError("OnDownloadCompleted _request is Null");
                return;
            }

            if (remoteContentRequest.result == UnityWebRequest.Result.Success) {
                assetBundleRequest = AssetBundle.LoadFromMemoryAsync(remoteContentRequest.downloadHandler.data);
                OfflineSandboxStorage.DownLoadFinish(remoteContentRequest.downloadedBytes, savePath, remoteContentRequest.downloadHandler.data);
                step = Step.LoadAssetBundle;
                return;
            }

            // 失败才重试，取消下载不重试
            if (retryTimes < MaxRetryTimes) {
                remoteContentRequest?.Dispose();
                remoteContentRequest = UnityWebRequest.Get(remotePath);
                remoteContentRequest.downloadHandler = new DownloadHandlerBuffer();
                remoteContentRequest.SendWebRequest();
                step = Step.Download;
                retryTimes++;
                return;
            }
            Debug.LogError("Load Asset Download Fail " + request.path);
            request.SetResult(Request.Result.Failed, remoteContentRequest?.error);
        }

        private void LoadAsset() {
            if (assetBundle == null) {
                request.SetResult(Request.Result.Failed, "Asset bundle  == null");
                return;
            }

            if (assetRequest == null) {
                var ugcItemConfigData = assetBundle.LoadAsset<TextAsset>("ugcItems");
                if (ugcItemConfigData == null)
                {
                    request.SetResult(Request.Result.Failed, "ugcItems null");
                    return;
                }
                request.combineData = JsonConvert.DeserializeObject<UGCCombineData>(ugcItemConfigData.text);
                request.textures = new Dictionary<string, Texture>();
                foreach (var matData in request.combineData.matDatas) {
                    // 使用 TryAdd 避免多个节点共用同一贴图路径时 Add 抛出 ArgumentException
                    if (!string.IsNullOrEmpty(matData.mainTex))
                    {
                        request.textures.TryAdd(matData.mainTex, assetBundle.LoadAsset<Texture>(matData.mainTex));
                    }

                    if (!string.IsNullOrEmpty(matData.norTex))
                    {
                        request.textures.TryAdd(matData.norTex, assetBundle.LoadAsset<Texture>(matData.norTex));
                    }
                }


                assetRequest = assetBundle.LoadAssetAsync<GameObject>(request.combineData.parentName);

            }

            if (!assetRequest.isDone) {
                return;
            }

            if (assetRequest.asset == null) {
                request.SetResult(Request.Result.Failed, "assetBundleRequest.asset == null");
                return;
            }

            request.asset = assetRequest.asset as GameObject;
            request.SetResult(Request.Result.Success);
        }

        public void WaitForCompletion() {
            if (!request.isDone) {
                OnUpdate();
            }
        }

        public void Dispose() {
            step = Step.None;
            remoteContentRequest?.Dispose();
            remoteContentRequest = null;
            assetBundleRequest = null;
            assetRequest = null;
            savePath = null;
            remotePath = null;
            retryTimes = 0;
            if (assetBundle != null) {
                assetBundle.Unload(true);
            }
            assetBundle = null;
            isLoadFromCache = false;
        }

    }
}
