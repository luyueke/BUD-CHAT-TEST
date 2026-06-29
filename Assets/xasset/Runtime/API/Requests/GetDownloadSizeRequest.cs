using System;
using System.Collections.Generic;

namespace xasset
{
    public class GetDownloadSizeRequest : Request
    {
        private readonly List<ManifestBundle> _bundles = new List<ManifestBundle>();
        private readonly List<ManifestBundle> _bundlesPacked = new List<ManifestBundle>();
        private readonly List<DownloadContent> _contents = new List<DownloadContent>();
        private int _max;

        private Step _step = Step.CheckPack;
        public List<Versions> versionsList { get; set; }
        public ulong downloadSize { get; private set; }
        public ulong downloadedSize { get; private set; }
        public string[] assetPaths { get; set; }

        public DownloadRequest DownloadAsync()
        {
            var request = DownloadContentRequestBatch.Create();
            foreach (var content in _contents)
                if (content.status != DownloadContent.Status.Downloaded)
                    request.AddContent(content);
            request.SendRequest();
            return request;
        }

        protected override void OnStart()
        {
            if (Assets.SimulationMode || Assets.OfflineMode)
            {
                SetResult(Result.Success);
                return;
            }

            _step = Step.CheckBundle;
            var set = new HashSet<ManifestBundle>();
            if (assetPaths.Length == 0)
                foreach (var versions in versionsList)
                {
                    foreach (var version in versions.data)
                    {
                        var bundles = version.manifest.bundles;
                        foreach (var bundle in bundles)
                            set.Add(bundle);
                    }
                }
            else
            {
                foreach (var versions in versionsList)
                {
                    foreach (var path in assetPaths)
                    {
                        if (versions.TryGetAssetPacks(path, out var packs))
                        {
                            foreach (var pack in packs)
                                foreach (var asset in pack.assets)
                                {
                                    set.Add(pack.manifest.bundles[asset.id]);
                                    if (pack.packed)
                                        _step = Step.CheckPack;
                                }
                        }
                        else
                        {
                            if (!versions.TryGetAssets(path, out var assets)) continue;
                            foreach (var asset in assets)
                            {
                                var bundles = asset.manifest.bundles;
                                var bundle = bundles[asset.bundle];
                                set.Add(bundle);
                                foreach (var dep in bundle.deps)
                                    set.Add(bundles[dep]);
                            }
                        }
                    }
                }
            }

            _bundles.AddRange(set);
            foreach (var bundle in _bundles)
                _bundlesPacked.Add(bundle);
            _max = _bundles.Count + _bundlesPacked.Count;
        }

        protected override void OnUpdated()
        {
            progress = (_max - _bundles.Count - _bundlesPacked.Count) * 1f / _max;
            switch (_step)
            {
                case Step.CheckPack:
                    while (_bundlesPacked.Count > 0)
                    {
                        var bundle = _bundlesPacked[0];
                        _bundlesPacked.RemoveAt(0);
                        if (!Assets.TryGetAssetPack(bundle, out var pack))
                            continue;
                        var bundles = pack.manifest.bundles;
                        var assets = pack.assets;
                        var exists = assets.Exists(input => Assets.IsDownloaded(bundles[input.id]));
                        if (exists)
                            continue;
                        AddContent(pack);
                        foreach (var asset in assets)
                        {
                            var item = bundles[asset.id];
                            _bundles.Remove(item);
                            _bundlesPacked.Remove(item);
                        }

                        if (Scheduler.Busy) return;
                    }

                    _step = Step.CheckBundle;
                    break;

                case Step.CheckBundle:
                    while (_bundles.Count > 0)
                    {
                        AddContent(_bundles[0]);
                        _bundles.RemoveAt(0);
                        if (Scheduler.Busy) return;
                    }

                    SetResult(Result.Success);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddContent(ManifestBundle bundle)
        {
            var url = Assets.GetDownloadURL(bundle.nameWithAppendHash);
            var savePath = Assets.GetDownloadDataPath(bundle.nameWithAppendHash);
            var content = DownloadContent.Get(url, savePath, bundle.hash, bundle.size, false);
            content.status = DownloadContent.Status.Default;
            _contents.Add(content);
            Logger.I($"AddBundle {bundle.nameWithAppendHash} {Assets.IsDownloaded(bundle)}");
            if (!Assets.IsDownloaded(bundle))
                downloadSize += content.downloadSize;
            else
            {
                content.status = DownloadContent.Status.Downloaded;
                downloadedSize += content.downloadSize;
            }
        }

        private void AddContent(AssetPack file)
        {
            var url = Assets.GetDownloadURL(file.nameWithAppendHash);
            var savePath = Assets.GetDownloadDataPath(file.nameWithAppendHash);
            var content = DownloadContent.Get(url, savePath, file.hash, file.size, false);
            content.assets = file.assets.Count;
            _contents.Add(content);
            Logger.I($"AddPack {file.nameWithAppendHash} {Assets.IsDownloaded(file)}");
            if (!Assets.IsDownloaded(file))
                downloadSize += content.downloadSize;
            else
            {
                content.status = DownloadContent.Status.Downloaded;
                downloadedSize += content.downloadSize;
            }
        }

        private enum Step
        {
            CheckPack,
            CheckBundle
        }
    }
}