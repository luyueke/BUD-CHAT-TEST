using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace xasset
{
    public class PreloadRequest : LoadRequest
    {
        private static readonly Queue<PreloadRequest> Unused = new Queue<PreloadRequest>();
        private static readonly Dictionary<string, PreloadRequest> Loaded = new Dictionary<string, PreloadRequest>();
        private PreloadRequestHandler handler { get; set; }

        public ManifestAsset info { get; private set; }

        public static Func<PreloadRequest, PreloadRequestHandler> CreateHandler { get; set; } = PreloadRequestHandlerRuntime.CreateInstance;

        protected override void OnStart()
        {
            handler.OnStart();
        }

        protected override void OnWaitForCompletion()
        {
            handler.WaitForCompletion();
        }

        protected override void OnUpdated()
        {
            handler.Update();
        }

        protected override void OnDispose()
        {
            Reuse(this);
            handler.Dispose();
        }

        public override void Release()
        {
            if (_refCount > 0) References.Release(path);
            base.Release();
        }

        protected override void LoadAsync()
        {
            References.Retain(path);
            base.LoadAsync();
        }

        private static void Reuse(PreloadRequest request)
        {
            Loaded.Remove($"{request.info.path}");
            Unused.Enqueue(request);
        }

        // 回收开始的时候移除已加载列表，防止
        public override void RecycleAsync()
        {
            Loaded.Remove($"{info.path}");
        }

        internal static PreloadRequest Load(string path)
        {
            if (!Assets.Versions.TryGetAsset(path, out var info))
            {
                Logger.D($"File not found:{path}");
                return null;
            }

            var key = $"{info.path}";
            if (!Loaded.TryGetValue(key, out var request))
            {
                request = Unused.Count > 0 ? Unused.Dequeue() : new PreloadRequest();
                request.Reset();
                request.info = info;
                request.path = info.path;
                request.handler = CreateHandler(request);
                Loaded[key] = request;
            }

            request.LoadAsync();
            return request;
        }
    }
}