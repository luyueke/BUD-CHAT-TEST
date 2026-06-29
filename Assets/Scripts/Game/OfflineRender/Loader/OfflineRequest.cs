using System.Collections.Generic;
using Game.OfflineRender;
using GameData.OfflineRender;
using UnityEngine;
using xasset;

namespace GameData.OfflineRender {
    public class OfflineRequest : LoadRequest {
        public OfflineRenderData renderData;
        public ModelLODType modelLODType;
        public OfflineRequestHandler handler;

        public UGCCombineData combineData;
        public GameObject asset;
        public Dictionary<string, Texture> textures;


        private static readonly Queue<OfflineRequest> Unused = new Queue<OfflineRequest>();
        public static readonly Dictionary<string, OfflineRequest> Loaded = new Dictionary<string, OfflineRequest>();


        protected override void OnStart() {
            handler.OnStart();
        }

        protected override void OnUpdated() {
            handler.OnUpdate();
        }

        protected override void OnCompleted() {
            base.OnCompleted();
        }

        protected override void OnWaitForCompletion() {
            handler.WaitForCompletion();
        }


        public override void RecycleAsync() {
            base.RecycleAsync();
            asset = null;
            combineData = null;
            textures = null;
            handler.Dispose();
            Loaded.Remove(renderData.GetKey(modelLODType));
        }

        protected override void OnDispose() {
        }

        public static OfflineRequest Load(OfflineRenderData renderData, ModelLODType modelLODType) {
            if (renderData == null || renderData.abConfig == null || string.IsNullOrEmpty(renderData.id)) {
                return null;
            }

            if (!Loaded.TryGetValue(renderData.GetKey(modelLODType), out var request)) {
                if (!Unused.TryDequeue(out request)) {
                    request = new OfflineRequest();
                }

                request.Reset();
                request.modelLODType = modelLODType;
                request.renderData = renderData;
                request.path = renderData.GetPath(modelLODType);
                request.handler = new OfflineRequestHandler(request);
                Loaded[renderData.GetKey(modelLODType)] = request;
            }

            request.LoadAsync();

            return request;
        }
    }
}
