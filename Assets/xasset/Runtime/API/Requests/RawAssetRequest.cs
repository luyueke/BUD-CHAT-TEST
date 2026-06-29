using System;
using System.Collections.Generic;

namespace xasset
{
    public class RawAssetRequest : LoadRequest
    {
        private static readonly Queue<RawAssetRequest> Unused = new Queue<RawAssetRequest>();
        private static readonly Dictionary<string, RawAssetRequest> Loaded = new Dictionary<string, RawAssetRequest>();
        private RawAssetRequestHandler handler;

        public ManifestAsset info;
        public ulong offset { get; set; }

        public static Func<RawAssetRequest, RawAssetRequestHandler> CreateHandler { get; set; } = RawAssetRequestHandlerRuntime.CreateInstance;

        protected override void OnStart()
        {
            handler.OnStart();
        }

        protected override void OnUpdated()
        {
            handler.Update();
        }

        protected override void OnDispose()
        {
            Reuse(this);
        }

        private static void Reuse(RawAssetRequest rawAssetRequest)
        {
            Unused.Enqueue(rawAssetRequest);
        }

        // 回收开始的时候移除已加载列表，防止
        public override void RecycleAsync()
        {
            Loaded.Remove(info.name);
        }

        internal static RawAssetRequest GetAsync(string path)
        {
            if (!Assets.Versions.TryGetAsset(path, out var info)) return null;
            if (!Loaded.TryGetValue(info.path, out var request))
            {
                request = Unused.Count > 0 ? Unused.Dequeue() : new RawAssetRequest();
                request.Reset();
                request.info = info;
                request.path = info.path;
                request.handler = CreateHandler(request);
                Loaded[info.path] = request;
            }

            request.LoadAsync();
            return request;
        }
    }
}