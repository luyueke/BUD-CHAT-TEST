using System.Collections.Generic;

namespace xasset
{
    public class AssetPackRequest : LoadRequest
    {
        private static readonly Queue<AssetPackRequest> Unused = new Queue<AssetPackRequest>();
        private static readonly Dictionary<string, AssetPackRequest> Loaded = new Dictionary<string, AssetPackRequest>();

        private DownloadContentRequest _request;

        private int _retryTimes;
        private AssetPack pack;

        protected override void OnStart()
        {
            var url = Assets.GetDownloadURL(pack.nameWithAppendHash);
            var savePath = Assets.GetDownloadDataPath(pack.nameWithAppendHash);
            _request = Downloader.DownloadAsync(DownloadContent.Get(url, savePath, pack.hash, pack.size));
            _retryTimes = 0;
        }

        protected override void OnWaitForCompletion()
        {
            _request.WaitForCompletion();
            while (!isDone) Update();
        }

        protected override void OnUpdated()
        {
            progress = _request.progress;
            if (!_request.isDone)
                return;

            if (_request.result == DownloadRequest.Result.Success)
            {
                SetResult(Result.Success);
                return;
            }

            if (_retryTimes < Downloader.MaxRetryTimes)
            {
                _request.Retry();
                return;
            }

            SetResult(Result.Failed, _request.error);
        }

        protected override void OnDispose()
        {
            Reuse(this);
        }

        private static void Reuse(AssetPackRequest request)
        {
            //Loaded.Remove(request.pack.nameWithAppendHash);
            Unused.Enqueue(request);
        }

        // 回收开始的时候移除已加载列表，防止
        public override void RecycleAsync()
        {
            Loaded.Remove(pack.nameWithAppendHash);
        }

        internal static AssetPackRequest Load(AssetPack pack)
        {
            if (!Loaded.TryGetValue(pack.nameWithAppendHash, out var request))
            {
                request = Unused.Count > 0 ? Unused.Dequeue() : new AssetPackRequest {pack = pack};
                request.Reset();
                request.pack = pack;
                request.path = pack.nameWithAppendHash;
                Loaded[pack.nameWithAppendHash] = request;
            }

            request.LoadAsync();
            return request;
        }
    }
}