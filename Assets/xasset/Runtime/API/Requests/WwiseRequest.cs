using System;
using System.Collections.Generic;
using UnityEngine;

namespace xasset
{
    public class WwiseRequest : Request
    {
        private readonly List<ManifestBundle> _bundles = new List<ManifestBundle>();
        private readonly List<DownloadContent> _contents = new List<DownloadContent>();
        private int _max;

        public List<string> wwisePaths { get; set; }
        public ulong downloadSize { get; private set; }
        public ulong downloadedSize { get; private set; }
        public string[] assetPaths { get; set; }

        public DownloadRequest DownloadAsync()
        {
            var request = DownloadContentRequestBatch.Create();
            foreach (var content in _contents)
                if (content.status != DownloadContent.Status.Downloaded)
                {
                    Debug.LogError("WwiseRequest-url = " + content.url);
                    request.AddContent(content);
                }
            request.SendRequest();
            return request;
        }

        public DownloadRequest Download()
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

            var set = new HashSet<ManifestBundle>();

            foreach (var path in wwisePaths)
            {
                if (!Assets.Versions.TryGetAssets(path, out var assets)) continue;
                foreach (var asset in assets)
                {
                    var bundles = asset.manifest.bundles;
                    var bundle = bundles[asset.bundle];
                    set.Add(bundle);
                    foreach (var dep in bundle.deps)
                        set.Add(bundles[dep]);
                }
            }
            
            _bundles.AddRange(set);
            _max = _bundles.Count;
        }

        protected override void OnUpdated()
        {
            progress = (_max - _bundles.Count) * 1f / _max;
            while (_bundles.Count > 0)
            {
                AddContent(_bundles[0]);
                _bundles.RemoveAt(0);
                if (Scheduler.Busy) return;
            }
            SetResult(Result.Success);
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
        
    }
}