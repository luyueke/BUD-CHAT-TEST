using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset
{
    public class AudioRequest : LoadRequest
    {
        private static readonly Queue<AudioRequest> Unused = new Queue<AudioRequest>();
        private static readonly Dictionary<string, AudioRequest> Loaded = new Dictionary<string, AudioRequest>();
        protected AudioRequestHandler handler { get; set; }
        public bool isLocalUrl { get; set; }
        public AudioClip asset { get; set; }
        public Dictionary<string, AudioClip> assets { get; set; }

        public static Func<AudioRequest, AudioRequestHandler> CreateHandler { get; set; } = AudioRequestHandlerRuntime.CreateInstance;

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
            // OnRemove();
            // handler.Dispose();
            //
            // if (asset != null) Object.Destroy(asset);
            // if (assets != null)
            // {
            //     foreach (var kv in assets)
            //     {
            //         if (kv.Value) Object.Destroy(kv.Value);
            //     }
            // }
            // asset = null;
            // assets = null;
            // isLocalUrl = false;
        }

        public override void Release()
        {
            if (_refCount > 0) References.Release(path);
            base.Release();
        }

        // protected override void LoadAsync()
        // {
        //     References.Retain(path);
        //     base.LoadAsync();
        // }

        protected override void OnCompleted()
        {
            base.OnCompleted();
        }

        // protected virtual void OnRemove()
        // {
        //     Reuse(this);
        // }
        //
        //
        // 回收开始的时候移除已加载列表，防止
        public override void RecycleAsync()
        {
            base.RecycleAsync();
            asset = null;
            handler.Dispose();
            Loaded.Remove($"{path}");
        }

        // private static void Reuse(AudioRequest request)
        // {
        //     Unused.Enqueue(request);
        // }

        internal static AudioRequest Load(string path)
        {


            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("AudioRequest Error Url == null");
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
                    Debug.LogError("AudioRequest Error Url:" + path + "," + e.Message);
                    return null;
                }
            }


            var key = $"{path}";
            if (!Loaded.TryGetValue(key, out var request))
            {
                request = Unused.Count > 0 ? Unused.Dequeue() : new AudioRequest();
                request.Reset();
                request.isLocalUrl = !path.StartsWith("https://");
                request.path = path;
                request.handler = CreateHandler(request);
                Loaded[key] = request;
            }

            request.LoadAsync();
            return request;
        }
    }
}