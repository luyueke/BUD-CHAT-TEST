// @Author: YangJie
// @Description:
// @Date:  2023/12/26
// @Modify:

using System;
using System.Collections.Generic;
using UnityEngine;

namespace xasset
{
    //由于XAsset中相关代码裁剪、临时复制代码
    public class UGCTempPartRemoteRequest : RemoteImageRequest
    {
        public int textureCount { get; set; }
        
        public int texSize { get; set; }
        public string originPath { get; set; }


        private static readonly Queue<UGCTempPartRemoteRequest> Unused = new Queue<UGCTempPartRemoteRequest>();
        private static readonly Dictionary<string, UGCTempPartRemoteRequest> Loaded = new Dictionary<string, UGCTempPartRemoteRequest>();

        
        protected override void OnRemove()
        {
            Unused.Enqueue(this);
        }

        public override void RecycleAsync()
        {
            Loaded.Remove($"{path}");
        }

        internal static UGCTempPartRemoteRequest Load(int textureCount,int texSize, string downloadPath, string originPath)
        {
            try
            {
                _ = new Uri(downloadPath);
            }
            catch (Exception e)
            {
                Debug.LogError("UGCTempPartRemoteRequest Error Url:" + downloadPath + "," + e.Message);
                return null;
            }
            
            if (string.IsNullOrEmpty(downloadPath))
            {
                return null;
            }

            var key = $"{downloadPath}";
            if (!Loaded.TryGetValue(key, out var request))
            {
                request = Unused.Count > 0 ? Unused.Dequeue() : new UGCTempPartRemoteRequest();
                request.Reset();
                request.isLocalUrl = !downloadPath.StartsWith("https://");
                request.isZip = downloadPath.EndsWith(".zip");
                request.path = downloadPath;
                request.textureCount = textureCount;
                request.texSize = texSize;
                request.originPath = originPath;
                request.handler = UGCTempPartRemoteRequestHandler.CreateInstance(request);
                Loaded[key] = request;
            }

            request.LoadAsync();
            return request;
        }
    }
}