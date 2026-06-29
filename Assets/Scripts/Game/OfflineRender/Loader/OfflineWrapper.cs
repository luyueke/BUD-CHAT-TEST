using System;
using UnityEngine;
using xasset;

namespace GameData.OfflineRender {
    public class OfflineWrapper {
        public OfflineRequest request;
        public Action<bool> completed;
        private bool IsUsed = false;

        public OfflineWrapper(OfflineRequest request)
        {
            this.request = request;
            request.completed += OnCompleted;
        }

        public GameObject Instantiate(Transform parent = null, bool instantiateInWorldSpace = false)
        {
            if (request.asset == null) return default;
            var clone = Clone(request);
            var obj = UnityEngine.Object.Instantiate(clone.asset, parent, instantiateInWorldSpace);
            AutoreleaseCache.Get(obj).Add(clone);
            return obj;
        }

        /// <summary>
        /// 取用资源
        /// </summary>
        /// <param name="user"> 资源的使用者 </param>
        /// <returns></returns>
        public GameObject RetainAsset(GameObject user)
        {
            if (user == null) return null;
            if (request.asset == null) return null;
            var clone = Clone(request);
            AutoreleaseCache.Get(user).Add(clone);
            return request.asset;
        }

        public Texture RetainTexture(string name, GameObject user) {
            if (user == null || string.IsNullOrEmpty(name)) return null;
            if (request.textures == null || !request.textures.ContainsKey(name)) return null;
            var clone = Clone(request);
            AutoreleaseCache.Get(user).Add(clone);
            return request.textures[name];
        }



        private void OnCompleted(Request req)
        {
            completed?.Invoke(req.result == Request.Result.Success);
            completed = null;
        }

        ~OfflineWrapper()
        {
            if (request != null) request.Release();
        }

        private OfflineRequest Clone(OfflineRequest req) {
            return OfflineRequest.Load(req.renderData, req.modelLODType);
        }

    }
}
