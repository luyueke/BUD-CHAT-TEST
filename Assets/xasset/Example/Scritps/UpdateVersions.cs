using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace xasset.example
{
    public enum AppUpdateState
    {
        GetVersion,
        DownloadingResource,
        CleanData,
        Complete
    }
    
    [Serializable]
    public class ABUpdateInfo
    {
        public string downloadURL;
        public int quality;  // 0:high 1:low
        public string file;
        public string hash;
        public ulong size;
        public long timestamp;
        public string hotUpdateVersion;
        public int forceUpdate; //是否强更 0 否 1 是
        public string panelURL;
        //是否box白名单用户
        public bool isBoxWhiteUser;
    }
    
    public class UpdateVersions : MonoBehaviour
    {
        public MessageBox tempMessageBox;
        public MessageBox exitMessageBox;
        [NonSerialized] public Action<AppUpdateState, float, ulong, ulong, float> onUpdateChanged;
        [SerializeField] private bool fastVerifyMode = true;
        [SerializeField] private bool simulationMode = true;
        [SerializeField] private bool offlineMode;
        [SerializeField] private string baseUpdateURL = "http://113.90.166.128:8080";
        private DownloadRequest _downloadAsync;
        private Versions versions;
        private const string updateVersionMsg = "更新版本信息";
        private ABUpdateInfo info;
        
        public void OnStart()
        {
            StartCoroutine(Updating());
        }

        public void InitUpdateSetting()
        {
#if UNITY_EDITOR
            Assets.SimulationMode = simulationMode;
            Assets.FastVerifyMode = fastVerifyMode;
            if (Assets.SimulationMode)
            {
                // 模拟模式：AssetDatabase 直读，视所有资源为本地已有
                Assets.OfflineMode = true;
                AssetRequest.CreateHandler = AssetRequestHandlerEdit.CreateInstance;
                SceneRequest.CreateHandler = SceneRequestHandlerEdit.CreateInstance;
            }
            else
            {
                // AB 模式：与手机设备行为完全一致，走真实 HTTP 下载
                Assets.OfflineMode = false;
                Downloader.SimulationMode = false;
                Assets.UpdateURL = $"{baseUpdateURL}/{Assets.Platform}/{UpdateInfo.Filename}";
                Debug.Log("BUD UpdateURL " + Assets.UpdateURL);
            }
#else
            Assets.SimulationMode = false;
            Assets.OfflineMode = false;
            Downloader.SimulationMode = false;
            Assets.UpdateURL = $"{baseUpdateURL}/{Assets.Platform}/{UpdateInfo.Filename}";
#endif
        }

        public static void ClearAsync()
        {
            Debug.Log("Clear BundleCache");
            var dir = Assets.DownloadDataPath;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            // Fix 6: 同步清理内存字典，防止旧条目干扰续传偏移
            DownloadContent.ClearAll();
        }
        

        private IEnumerator Updating()
        {
            if (info != null)
            {
                Debug.Log($"[热更] 开始GetVersionsAsync: file={info.file} size={info.size} downloadURL={Assets.DownloadURL}");
                var t0 = Time.realtimeSinceStartup;
                var getVersionsAsync = Assets.GetVersionsAsync(info.file,info.hash,info.size);
                while (!getVersionsAsync.isDone)
                {
                    onUpdateChanged?.Invoke(AppUpdateState.GetVersion, getVersionsAsync.progress, 0, 0, 0);
                    yield return null;
                }
                Debug.Log($"[热更] GetVersionsAsync 耗时={(Time.realtimeSinceStartup - t0):f1}s result={getVersionsAsync.result} error={getVersionsAsync.error}");
                versions = getVersionsAsync.versions;

                if (versions)
                {
                    Debug.Log($"[热更] 远端Versions.timestamp={versions.timestamp} 本地AssetsVersions.timestamp={Assets.Versions.timestamp}");
                }
                else
                {
                    Debug.LogError($"[热更] GetVersionsAsync失败: versions为null，跳过资源更新");
                }

                onUpdateChanged?.Invoke(AppUpdateState.GetVersion, 1, 0, 0, 0);

                if (versions != null &&  Assets.Versions.timestamp < versions.timestamp)
                {
                    Debug.Log("[热更] 检测到新版本，计算需下载大小...");
                    var getDownloadSizeAsync = Assets.GetDownloadSizeAsync(versions,"BuildInPack","wwise");
                    yield return getDownloadSizeAsync;
                    Debug.Log($"[热更] 需下载大小={Utility.FormatBytes(getDownloadSizeAsync.downloadSize)} 已下载={Utility.FormatBytes(getDownloadSizeAsync.downloadedSize)}");
                    if (getDownloadSizeAsync.downloadSize > 0)
                    {
                        onUpdateChanged?.Invoke(AppUpdateState.DownloadingResource, 0, 0, 0, 0);
                        Debug.Log($"[热更] 开始下载资源，总大小={Utility.FormatBytes(getDownloadSizeAsync.downloadSize)}");
                        _downloadAsync = getDownloadSizeAsync.DownloadAsync();
                        yield return Downloading();
                        onUpdateChanged?.Invoke(AppUpdateState.DownloadingResource, 1, 0, 0, 0);
                        if (_downloadAsync.result == DownloadRequest.Result.Success)
                        {
                            Debug.Log("[热更] 资源下载成功，开始清理旧文件");
                            onUpdateChanged?.Invoke(AppUpdateState.CleanData, 0, 0, 0, 0);
                            yield return Clearing();
                            onUpdateChanged?.Invoke(AppUpdateState.CleanData, 1, 0, 0, 0);
                            Assets.Versions = versions;
                            versions.Save(Assets.GetDownloadDataPath(Versions.Filename));
                            Debug.Log("[热更] 版本文件保存完成");
                        }
                        else
                        {
                            Debug.LogError($"[热更] 资源下载失败: {_downloadAsync.error}，退出应用");
                            Application.Quit();
                        }
                    }
                    else
                    {
                        Debug.Log("[热更] 无需下载，直接保存版本文件");
                        versions.Save(Assets.GetDownloadDataPath(Versions.Filename));
                    }
                }
                else
                {
                    Debug.Log("[热更] 版本无更新，跳过下载");
                }
            }
            else
            {
                Debug.LogError("[热更] UpdateVersions.cs info is Null，跳过热更");
            }
            Debug.Log("[热更] 热更流程结束，触发Complete");
            onUpdateChanged?.Invoke(AppUpdateState.Complete,1,0,0,0);
        }

        private IEnumerator Downloading()
        {
            float startTime = Time.time;
            ulong lastBytes = 0;
            float lastBytesTime = Time.realtimeSinceStartup;
            const float stallWarnInterval = 10f;
            float lastStallLog = 0f;
            int autoRetryCount = 0;

            while (_downloadAsync.result != DownloadRequest.Result.Success)
            {
                var downloadedBytes = Utility.FormatBytes(_downloadAsync.downloadedBytes);
                var downloadSize = Utility.FormatBytes(_downloadAsync.downloadSize);
                var bandwidth = Utility.FormatBytes(_downloadAsync.bandwidth);
                float progress = _downloadAsync.downloadSize > 0 ? _downloadAsync.downloadedBytes / (float) _downloadAsync.downloadSize : 0f;
                onUpdateChanged?.Invoke(AppUpdateState.DownloadingResource,progress,_downloadAsync.downloadedBytes, _downloadAsync.downloadSize, startTime);
                yield return null;

                // 停滞检测：记录进度、打印警告、超时强制重启批次
                if (_downloadAsync.downloadedBytes > lastBytes)
                {
                    lastBytes = _downloadAsync.downloadedBytes;
                    lastBytesTime = Time.realtimeSinceStartup;
                    autoRetryCount = 0;
                }
                else
                {
                    float stalledFor = Time.realtimeSinceStartup - lastBytesTime;
                    if (stalledFor > stallWarnInterval && Time.realtimeSinceStartup - lastStallLog > stallWarnInterval)
                    {
                        Debug.LogWarning($"[热更] 下载停滞 {stalledFor:f0}s 无新数据，已下载={downloadedBytes}/{downloadSize} isDone={_downloadAsync.isDone} error={_downloadAsync.error}");
                        lastStallLog = Time.realtimeSinceStartup;
                    }
                    // 批次内部死锁保护：isDone=False 但 90s 没有任何字节，强制重启整个批次
                    if (!_downloadAsync.isDone && stalledFor > 10f)
                    {
                        autoRetryCount++;
                        float waitSeconds = 1;// Mathf.Min(3f * autoRetryCount, 30f);
                        Debug.LogWarning($"[热更] 批次死锁超过10s，{waitSeconds:f0}s后强制重启批次 (第{autoRetryCount}次)");
                        yield return new WaitForSeconds(waitSeconds);
                        lastBytes = 0;
                        lastBytesTime = Time.realtimeSinceStartup;
                        // 等待期间 batch 可能已自行恢复（isDone=True），只在仍死锁时才强制重启
                        if (!_downloadAsync.isDone)
                            _downloadAsync.Retry();
                        continue;
                    }
                }

                if (!_downloadAsync.isDone || string.IsNullOrEmpty(_downloadAsync.error)) continue;
                if(_downloadAsync.result == DownloadRequest.Result.Success)
                    break;

                Debug.LogError($"[热更] 下载出错: {_downloadAsync.error} 已下载={downloadedBytes}/{downloadSize}");
                if (_downloadAsync.error != null && _downloadAsync.error.Contains("System out of memory"))
                {
                    string tipsStr = "手机内存已不足，无法完成更新。 请及时前往设置清理缓存，确保您在游戏中的正常体验。";
                #if PACKAGE_TYPE_US
                    tipsStr = "The phone memory is insufficient to complete the update. Please go to Settings and clear the cache in time to ensure your normal experience in the game. ";
                #endif
                    var exit = MessageBox.Show(exitMessageBox, tipsStr);
                    yield return exit;
                    Application.Quit();
                }
                else
                {
                    if(autoRetryCount > 3)
                    {
                        autoRetryCount = 0;
                        string failTips = "下载失败，是否重试？";
#if PACKAGE_TYPE_US
                    failTips = "Download failed. Do you want to try again? ";
#endif
                        var retry = MessageBox.Show(tempMessageBox, failTips);
                        yield return retry;
                        if (retry.result == Request.Result.Success)
                        {
                            Debug.Log("[热更] 用户选择重试下载");
                            lastBytes = 0;
                            lastBytesTime = Time.realtimeSinceStartup;
                            _downloadAsync.Retry();
                          
                        }
                        else
                        {
                            break;
                        }
                    }else
                    {
                        autoRetryCount++;
                        float waitSeconds = 2;// Mathf.Min(3f * autoRetryCount, 30f);
                        Debug.LogWarning($"[热更] 网络错误，{waitSeconds:f0}s后自动重试 (第{autoRetryCount}次): {_downloadAsync.error}");
                        yield return new WaitForSeconds(waitSeconds);
                        lastBytes = 0;
                        lastBytesTime = Time.realtimeSinceStartup;
                        _downloadAsync.Retry();
                    }
          
                }
            }
            Debug.Log($"[热更] Downloading 结束 result={_downloadAsync.result} 总耗时={(Time.realtimeSinceStartup - startTime):f1}s");
        }

        private IEnumerator Clearing()
        {
            var bundles = new HashSet<string>();
            foreach (var item in versions.data)
            {
                bundles.Add(item.file);
                foreach (var bundle in item.manifest.bundles)
                    bundles.Add(bundle.nameWithAppendHash);
            }

            var files = new List<string>();
            foreach (var item in Assets.Versions.data)
            {
                if (!bundles.Contains(item.file))
                    files.Add(item.file);
                foreach (var bundle in item.manifest.bundles)
                    if (!bundles.Contains(bundle.nameWithAppendHash))
                        files.Add(item.file);
            }

            var removeAsync = new RemoveRequest();
            foreach (var file in files)
            {
                var path = Assets.GetDownloadDataPath(file);
                removeAsync.files.Add(path);
            }

            removeAsync.SendRequest();
            while (!removeAsync.isDone)
            {
                var msg = $"清理历史文件 {removeAsync.current}/{removeAsync.max}";
                var progress = (float)removeAsync.current / (float)removeAsync.max;
                onUpdateChanged?.Invoke(AppUpdateState.CleanData, progress, 0, 0, 0);
                yield return null;
            }
        }

        public void SetInfo(ABUpdateInfo _info)
        {
            if (_info != null)
            {
                info = _info;
                // Assets.DownloadURL = $"{info.downloadURL}{Assets.Bundles}/{Assets.Platform}";
                Assets.DownloadURL = info.downloadURL;
                Assets.HotUpdateVersion = info.hotUpdateVersion;
            }
        }
        
        private void OnApplicationQuit()
        {
#if UNITY_EDITOR
            xasset.Assets.Dispose();
#endif
        }
    }
}