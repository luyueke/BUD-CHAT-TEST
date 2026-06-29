// @Author: YangJie
// @Description:
// @Date:  2023/12/26
// @Modify:

using System;
using System.Collections.Generic;
using UnityEngine;

namespace xasset
{
    public class UGCPartRemoteRequest : RemoteImageRequest
    {
        public int textureCount { get; set; }
        public string originPath { get; set; }


        private static readonly Queue<UGCPartRemoteRequest> Unused = new Queue<UGCPartRemoteRequest>();
        private static readonly Dictionary<string, UGCPartRemoteRequest> Loaded = new Dictionary<string, UGCPartRemoteRequest>();

        
        protected override void OnRemove()
        {
            Unused.Enqueue(this);
        }

        public override void RecycleAsync()
        {
            Loaded.Remove($"{path}");
        }

        internal static UGCPartRemoteRequest Load(int textureCount, string downloadPath, string originPath)
        {
            try
            {
                _ = new Uri(downloadPath);
            }
            catch (Exception e)
            {
                Debug.LogError("UGCPartRemoteRequest Error Url:" + downloadPath + "," + e.Message);
                return null;
            }
            
            if (string.IsNullOrEmpty(downloadPath))
            {
                return null;
            }

            var key = $"{downloadPath}";
            if (!Loaded.TryGetValue(key, out var request))
            {
                request = Unused.Count > 0 ? Unused.Dequeue() : new UGCPartRemoteRequest();
                request.Reset();
                request.isLocalUrl = !downloadPath.StartsWith("https://");
                request.isZip = downloadPath.EndsWith(".zip");
                request.path = downloadPath;
                request.textureCount = textureCount;
                request.originPath = originPath;
                request.handler = UGCPartRemoteRequestHandler.CreateInstance(request);
                Loaded[key] = request;
            }

            request.LoadAsync();
            return request;
        }
    }
}