using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset {
    public class RemoteAssetRequest : LoadRequest {
        private static readonly Queue<RemoteAssetRequest> Unused = new Queue<RemoteAssetRequest>();

        private static readonly Dictionary<string, RemoteAssetRequest> Loaded =
            new Dictionary<string, RemoteAssetRequest>();

        protected RemoteAssetRequestHandler handler {
            get;
            set;
        }

        public bool isLocalUrl {
            get;
            set;
        }

        public bool isZip {
            get;
            set;
        }

        public byte[] asset {
            get;
            set;
        }

        public static Func<RemoteAssetRequest, RemoteAssetRequestHandler> CreateHandler {
            get;
            set;
        } = RemoteAssetRequestHandler.CreateInstance;

        protected override void OnStart() {
            handler.OnStart();
        }

        protected override void OnWaitForCompletion() {
            handler.WaitForCompletion();
        }

        protected override void OnUpdated() {
            handler.Update();
        }

        protected override void OnDispose() {
            handler.Dispose();
            asset = null;
            isZip = false;
            isLocalUrl = false;
            status = Status.Wait;
            OnRemove();
        }

        public override void Release() {
            if (_refCount > 0) References.Release(path);
            base.Release();
        }

        protected override void LoadAsync() {
            References.Retain(path);
            base.LoadAsync();
        }

        protected override void OnCompleted() {
            handler.Dispose();
        }

        protected virtual void OnRemove() {
            Reuse(this);
        }


        // 回收开始的时候移除已加载列表，防止
        public override void RecycleAsync() {
            Loaded.Remove($"{path}");
        }

        private static void Reuse(RemoteAssetRequest request) {
            Unused.Enqueue(request);
        }

        internal static RemoteAssetRequest Load(string path) {
            if (string.IsNullOrEmpty(path)) {
                Debug.LogError("RemoteImageRequest Error Url == null");
                return null;
            }

            try {
                _ = new Uri(path);
            } catch (Exception e) {
                Debug.LogError("RemoteImageRequest Error Url:" + path + "," + e.Message);
                return null;
            }


            var key = $"{path}";
            if (!Loaded.TryGetValue(key, out var request)) {
                request = Unused.Count > 0 ? Unused.Dequeue() : new RemoteAssetRequest();
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
