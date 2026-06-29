using System;
using System.Collections.Generic;
using UnityEngine;

namespace xasset
{
    public sealed class BundleRequest : LoadRequest
    {
        internal BundleRequestHandler handler;
        internal AssetBundle assetBundle { get; set; }
        public ManifestBundle info { get; set; }

        public static Func<BundleRequest, BundleRequestHandler> CreateHandler { get; set; }

        protected override void OnStart()
        {
            handler.OnStart();
        }

        protected override void OnUpdated()
        {
            handler.Update();
        }

        protected override void OnWaitForCompletion()
        {
            handler.WaitForCompletion();
        }

        internal ulong GetDefaultOffset()
        {
            var offset = 0UL;
            if (Assets.Versions.encryption)
                offset = !Assets.TryGetPlayerAsset(info.hash, out var value)
                    ? Assets.WriteOffset
                    : value.offset;
            return offset;
        }

        public void LoadAssetBundle(string filename, ulong offset)
        {
            Logger.D($"Load {info.nameWithAppendHash} from {filename} with offset {offset}");
            ReloadAssetBundle(info.name);
            assetBundle = AssetBundle.LoadFromFile(filename, 0, offset);

            progress = 1;
            if (assetBundle == null)
            {
                SetResult(Result.Failed, $"assetBundle == null, {info.nameWithAppendHash}");
                return;
            }

            SetResult(Result.Success);
            AddAssetBundle(info.name, assetBundle);
        }

        protected override void OnDispose()
        {
            Reuse(this);
            handler.Dispose();
            if (assetBundle != null)
            {
                assetBundle.Unload(true);
                RemoveAssetBundle(info.name);
                assetBundle = null;
            }

            Reset();
        }

        #region Hotreload

        private static readonly Dictionary<string, AssetBundle> AssetBundles = new Dictionary<string, AssetBundle>();

        private static void AddAssetBundle(string name, AssetBundle assetBundle)
        {
            AssetBundles[name] = assetBundle;
        }

        private static void ReloadAssetBundle(string name)
        {
            if (!AssetBundles.TryGetValue(name, out var assetBundle)) return;
            if (assetBundle != null) assetBundle.Unload(false);
            AssetBundles.Remove(name);
        }

        private static void RemoveAssetBundle(string name)
        {
            AssetBundles.Remove(name);
        }

        #endregion

        #region Internal

        private static readonly Queue<BundleRequest> Unused = new Queue<BundleRequest>();
        public static readonly Dictionary<string, BundleRequest> Loaded = new Dictionary<string, BundleRequest>();

        private static BundleRequestHandler GetHandler(BundleRequest request)
        {
            var bundle = request.info;
            var handler = CreateHandler?.Invoke(request);
            if (handler != null) return handler;

            if (Assets.TryGetAssetPack(bundle, out var pack) && Assets.IsDownloaded(pack))
                return new BundleRequestHandlerAssetPack {pack = pack, request = request};

            if (Assets.IsPlayerAsset(bundle.nameWithAppendHash))
                if (Assets.IsWebGLPlatform)
                    return new BundleRequestHandlerWebFile {request = request};
                else
                    return new BundleRequestHandlerFile {path = Assets.GetBundlePath(bundle.nameWithAppendHash), request = request};

            if (Assets.IsDownloaded(bundle))
                return new BundleRequestHandlerFile {path = Assets.GetDownloadDataPath(bundle.nameWithAppendHash), request = request};

            return new BundleRequestHandlerWebFile {request = request};
        }

        private static void Reuse(BundleRequest request)
        {
            Unused.Enqueue(request);
        }

        // 回收开始的时候移除已加载列表，防止
        public override void RecycleAsync()
        {
            Loaded.Remove(info.nameWithAppendHash);
        }

        internal static BundleRequest Load(ManifestBundle bundle)
        {
            if (!Loaded.TryGetValue(bundle.nameWithAppendHash, out var request))
            {
                request = Unused.Count > 0 ? Unused.Dequeue() : new BundleRequest();
                request.Reset();
                request.info = bundle;
                request.path = bundle.nameWithAppendHash;
                request.handler = GetHandler(request);
                Loaded[bundle.nameWithAppendHash] = request;
            }

            request.LoadAsync();
            return request;
        }

        #endregion
    }
}