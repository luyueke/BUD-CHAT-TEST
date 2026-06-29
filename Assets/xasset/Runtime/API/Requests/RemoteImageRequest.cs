using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset
{
    [RequestPriority(999)]
    public class RemoteImageRequest : LoadRequest
    {
        private static readonly Queue<RemoteImageRequest> Unused = new Queue<RemoteImageRequest>();
        private static readonly Dictionary<string, RemoteImageRequest> Loaded = new Dictionary<string, RemoteImageRequest>();
        protected RemoteImageRequestHandler handler { get; set; }

        public bool isLocalUrl { get; set; }

        public bool isZip { get; set; }
        public Texture asset { get; set; }
        public Dictionary<string, Texture> assets { get; set; }

        public static Func<RemoteImageRequest, RemoteImageRequestHandler> CreateHandler { get; set; } = RemoteImageRequestHandlerRuntime.CreateInstance;

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
            OnRemove();
            handler.Dispose();

            if (asset != null) Object.Destroy(asset);
            if (assets != null)
            {
                foreach (var kv in assets)
                {
                    if (kv.Value) Object.Destroy(kv.Value);
                }
            }
            asset = null;
            assets = null;
            isZip = false;
            isLocalUrl = false;
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

        protected override void OnCompleted()
        {
            handler.Dispose();
        }

        protected virtual void OnRemove()
        {
            Reuse(this);
        }


        // 回收开始的时候移除已加载列表，防止
        public override void RecycleAsync()
        {
            Loaded.Remove($"{path}");
        }

        private static void Reuse(RemoteImageRequest request)
        {
            Unused.Enqueue(request);
        }

        internal static RemoteImageRequest Load(string path)
        {


            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("RemoteImageRequest Error Url == null");
                return null;
            }

            if (path.StartsWith("http"))
            {
                // 仅仅 http 链接做校验
                try
                {
                    _ = new Uri(path);
                }
                catch (Exception e)
                {
                    Debug.LogError("RemoteImageRequest Error Url:" + path + "," + e.Message);
                    return null;
                }
            }


            var key = $"{path}";
            if (!Loaded.TryGetValue(key, out var request))
            {
                request = Unused.Count > 0 ? Unused.Dequeue() : new RemoteImageRequest();
                request.Reset();
                request.isLocalUrl = !path.StartsWith("https://");
                request.isZip = path.EndsWith(".zip");
                request.path = path;
                request.handler = CreateHandler(request);
                Loaded[key] = request;
            }

            request.LoadAsync();
            return request;
        }
    }
}