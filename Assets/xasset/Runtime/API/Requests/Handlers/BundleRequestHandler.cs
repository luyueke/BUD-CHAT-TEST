namespace xasset
{
    internal struct BundleRequestHandlerFile : BundleRequestHandler
    {
        public BundleRequest request { get; set; }
        public string path;

        public void OnStart()
        {
            request = request;
            request.LoadAssetBundle(path, request.GetDefaultOffset());
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public void WaitForCompletion()
        {
        }
    }

    internal struct BundleRequestHandlerAssetPack : BundleRequestHandler
    {
        public BundleRequest request { get; set; }
        public AssetPack pack { get; set; }
        private AssetPackRequest _packRequest;

        public void OnStart()
        {
            _packRequest = AssetPackRequest.Load(pack);
        }

        public void Update()
        {
            request.progress = _packRequest.progress;
            if (!_packRequest.isDone)
                return;

            if (_packRequest.result == Request.Result.Success)
            {
                if (!pack.TryGetAsset(request.info.name, out var result))
                {
                    request.SetResult(Request.Result.Failed, "result == null.");
                    return;
                }

                var path = Assets.GetDownloadDataPath(pack.nameWithAppendHash);
                var offset = result.offset + request.GetDefaultOffset();
                request.LoadAssetBundle(path, offset);
                return;
            }

            request.SetResult(Request.Result.Failed, _packRequest.error);
        }

        public void Dispose()
        {
        }

        public void WaitForCompletion()
        {
            _packRequest.WaitForCompletion();
        }
    }

    internal struct BundleRequestHandlerWebFile : BundleRequestHandler
    {
        public BundleRequest request { get; set; }
        private DownloadContentRequest _downloadAsync;
        private string _savePath;
        private int _retryTimes;
        private float _retryAfterTime;
        private bool _waitingRetry;

        // 连续失败达到此次数时触发 OnNetworkError 通知（让游戏层显示"网络不稳定"提示）
        private const int NotifyThreshold = 3;
        // 指数退避封顶（秒）
        private const int MaxBackoffSec = 30;

        /// <summary>
        /// 动态 bundle 下载持续失败时的通知回调，参数为 bundleName 和已重试次数。
        /// 游戏层订阅此事件可显示"网络不稳定，正在重试"等 UI 提示。
        /// </summary>
        public static System.Action<string, int> OnNetworkError;

        public void OnStart()
        {
            _retryTimes = 0;
            _waitingRetry = false;
            _retryAfterTime = 0f;
            var bundle = request.info;
            var url = Assets.GetDownloadURL(bundle.nameWithAppendHash);
            _savePath = Assets.GetDownloadDataPath(bundle.nameWithAppendHash);
            _downloadAsync = Downloader.DownloadAsync(DownloadContent.Get(url, _savePath, bundle.hash, bundle.size));
        }

        public void Update()
        {
            if (_waitingRetry)
            {
                if (UnityEngine.Time.realtimeSinceStartup < _retryAfterTime) return;
                _waitingRetry = false;
                _downloadAsync.Retry();
                return;
            }

            request.progress = _downloadAsync.progress;
            if (!_downloadAsync.isDone) return;

            if (_downloadAsync.result == DownloadRequest.Result.Success)
            {
                if (_retryTimes >= NotifyThreshold)
                    UnityEngine.Debug.Log($"[xasset] 动态bundle重试成功(第{_retryTimes}次后): {request.info.nameWithAppendHash}");
                request.LoadAssetBundle(_savePath, request.GetDefaultOffset());
                return;
            }

            // 指数退避：1s, 2s, 4s, 8s, 16s, 之后恒为 MaxBackoffSec
            int delaySec = _retryTimes < 5 ? (1 << _retryTimes) : MaxBackoffSec;
            _retryTimes++;

            UnityEngine.Debug.LogWarning(
                $"[xasset] 动态bundle下载失败(第{_retryTimes}次)，{delaySec}s后重试: " +
                $"{request.info.nameWithAppendHash} err={_downloadAsync.error}");

            // 达到阈值后通知游戏层（可显示"检查网络"提示）
            if (_retryTimes == NotifyThreshold)
                OnNetworkError?.Invoke(request.info.nameWithAppendHash, _retryTimes);

            _retryAfterTime = UnityEngine.Time.realtimeSinceStartup + delaySec;
            _waitingRetry = true;
            // 注意：永不调用 request.SetResult(Failed)，确保游戏不会因单次网络故障卡死
        }

        public void Dispose()
        {
        }

        public void WaitForCompletion()
        {
            // UWR handler 内部处理重试（_request.isDone 全程 false），
            // WaitForCompletion 只需驱动下载直到最终完成或失败，不再有死锁问题。
            _downloadAsync.WaitForCompletion();
            while (!request.isDone)
                Update();
        }
    }

    public interface BundleRequestHandler
    {
        void OnStart();
        void Update();
        void Dispose();
        void WaitForCompletion();
    }
}