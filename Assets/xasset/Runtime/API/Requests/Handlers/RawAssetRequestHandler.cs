using System.IO;

namespace xasset
{
    public interface RawAssetRequestHandler
    {
        void OnStart();
        void Update();
    }

    public struct RawAssetRequestHandlerRuntime : RawAssetRequestHandler
    {
        public RawAssetRequest request { get; set; }
        private DownloadContentRequest _downloadContent;

        public void OnStart()
        {
            if (File.Exists(request.info.path))
            {
                request.path = request.info.path;
                request.SetResult(Request.Result.Success);
                return;
            }

            var bundle = request.info.manifest.bundles[request.info.bundle];
            var nameWithAppendHash = bundle.nameWithAppendHash;
            var path = Assets.GetPlayerDataPath(nameWithAppendHash);
            var file = new FileInfo(path);

            if (file.Exists && file.Length == (long) bundle.size)
            {
                request.path = path;
                request.SetResult(Request.Result.Success);
                return;
            }

            path = Assets.GetDownloadDataPath(nameWithAppendHash);
            file = new FileInfo(path);

            if (file.Exists && file.Length == (long) bundle.size)
            {
                request.path = path;
                request.SetResult(Request.Result.Success);
                return;
            }

            var url = Assets.IsPlayerAsset(bundle.nameWithAppendHash)
                ? Assets.GetPlayerDataURl(nameWithAppendHash)
                : Assets.GetDownloadURL(nameWithAppendHash);
            request.path = path;
            _downloadContent =
                Downloader.DownloadAsync(DownloadContent.Get(url, request.path, bundle.hash, bundle.size));
        }

        public void Update()
        {
            request.progress = _downloadContent.progress;
            if (!_downloadContent.isDone) return;
            Finished();
        }

        public static RawAssetRequestHandler CreateInstance(RawAssetRequest rawAssetRequest)
        {
            return new RawAssetRequestHandlerRuntime {request = rawAssetRequest};
        }

        private void Finished()
        {
            if (!string.IsNullOrEmpty(_downloadContent.error))
            {
                request.SetResult(Request.Result.Failed, _downloadContent.error);
                return;
            }

            request.SetResult(Request.Result.Success);
        }
    }
}