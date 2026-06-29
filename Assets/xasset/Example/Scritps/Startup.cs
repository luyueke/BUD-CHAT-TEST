using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace xasset.example
{
    [RequireComponent(typeof(Downloader))]
    [RequireComponent(typeof(Scheduler))]
    [RequireComponent(typeof(Recycler))]
    [DisallowMultipleComponent]
    public class Startup : MonoBehaviour
    {
        // public ExampleScene startWithScene;
        // [SerializeField] private bool loggable = true;
        // [SerializeField] private bool autoUpdate = true;
        // [SerializeField] private bool fastVerifyMode = true;
        // [SerializeField] private bool simulationMode = true;
        // [SerializeField] private bool offlineMode;
        // [SerializeField] private string baseUpdateURL = $"http://127.0.0.1/{Assets.Bundles}";
        //
        // [Conditional("UNITY_EDITOR")]
        // private void Awake()
        // {
        //     Assets.SimulationMode = simulationMode;
        // }
        //
        // private IEnumerator Start()
        // {
        //     DontDestroyOnLoad(gameObject);
        //     Assets.OfflineMode = offlineMode;
        //     Assets.FastVerifyMode = fastVerifyMode; 
        //     if (!Assets.SimulationMode && !Downloader.SimulationMode)
        //         Assets.UpdateURL = $"{baseUpdateURL}/{Assets.Platform}/{UpdateInfo.Filename}";
        //
        //     var initializeAsync = Assets.InitializeAsync();
        //     yield return initializeAsync;
        //     if (autoUpdate && !offlineMode)
        //     {
        //         // 获取服务器的更新信息。 
        //         var getUpdateInfoFromServerAsync = GetUpdateInfoFromServerAsync(false ? 1 : 0);//1:Low 0:High
        //         yield return getUpdateInfoFromServerAsync;
        //         if (getUpdateInfoFromServerAsync.result == Request.Result.Success)
        //         {
        //             var info = getUpdateInfoFromServerAsync.info;
        //             // 安装包完全不带资源的时候，可以从服务器下载版本文件和资源。
        //             var getVersionsAsync = Assets.GetVersionsAsync(info.file, info.hash, info.size);
        //             yield return getVersionsAsync;
        //             if (getVersionsAsync.versions != null)
        //             {
        //                 getVersionsAsync.versions.Save(Assets.GetDownloadDataPath(Versions.Filename));
        //                 Assets.Versions = getVersionsAsync.versions;
        //             }
        //         }
        //     }
        //
        //     yield return Asset.LoadAsync(MessageBox.Filename, typeof(GameObject));
        //     yield return Asset.InstantiateAsync(LoadingScreen.Filename);
        //     LoadingScreen.Instance.SetVisible(false);
        //     Scene.LoadAsync(startWithScene.ToString());
        // }
        //
        // public static GetUpdateInfoFromServerRequest GetUpdateInfoFromServerAsync(int quality)
        // {
        //     var request = new GetUpdateInfoFromServerRequest();
        //     request.Quality = quality;
        //     request.SendRequest();
        //     request.Start();
        //     return request;
        // }
        //
        //
        // private void Update()
        // {
        //     Logger.Enabled = loggable;
        // }
    }
}