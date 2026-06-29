using System;
using System.Collections.Generic;
using Game.Base;
using Game.OfflineRender;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Utils;
using Google.Protobuf.Collections;
using HLOD;
using Pb.Map;
using UGCAsset;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsBehaviours {
    public class PropBehaviour : MultiChildBehaviour, IHLODNode {
        private PropComponent itemComp;
        public Transform anchorRoot;

        public override void OnInitByCreate() {
            base.OnInitByCreate();
            itemComp = entity.GetOrAddComp<PropComponent>();
            hlodGroup = gameObject.GetOrAddComponent<HLODGroup>();
            hlodGroup.Init(this);
            anchorRoot = transform.Find("AnchorRoot");
            if (anchorRoot == null) {
                anchorRoot = new GameObject("AnchorRoot").transform;
                anchorRoot.SetParent(transform);
                anchorRoot.gameObject.Reset();
            }
        }

        public override void CreateChildNodes(RepeatedField<PNodeData> nodes) {
            // ChangeToHigh();
            // 若是在大厅，则直接切换，不依赖于 HLODManager
            if (GameController.IsInHallScene()) {
                hlodGroup.ChangeLODStatus(ModelLODType.HIGH);
            }
        }

        public override void OnReset() {
            base.OnReset();

        }

        public override void OnTouchClick() {
            base.OnTouchClick();
        }


        public void SetScaleAndAnchor(Vector3 scale, Vector3 anchor) {
            anchorRoot.localScale = scale;
            itemComp.scale = scale;
            itemComp.anchor = anchor;
            if (assetObj != null) {
                assetObj.transform.localPosition = anchor;
            }
        }

        public GameObject assetObj {
            get;
            set;
        }

        public HLODGroup hlodGroup {
            get;
            set;
        }

        public Action onAssetFirstLoaded {
            get;
            set;
        }

        public void ChangeToLow() {
            if (!GameOfflineRenderManager.Inst.TryGetOfflineRenderData(itemComp.uItemId, out var renderData)) {
                RestoreByData();
                return;
            }

            if (hlodGroup.CurLODType == ModelLODType.HIGH && renderData.IsSample()) {
                hlodGroup.CurLODType = hlodGroup.targetLODType;
                return;
            }

            GameOfflineRenderManager.Inst.LoadLowGameObject(itemComp.uItemId, anchorRoot, tmpObj =>
            {
                if (this == null || hlodGroup.CurLODType == hlodGroup.targetLODType)
                {
                    if (tmpObj != null) {
                        Destroy(tmpObj);
                    }
                    return;
                }

                if (tmpObj == null) {
                    RestoreByData();
                    return;
                }
                var propManager = GlobalNodeManager.Inst.Get<PropManager>();
                if (propManager != null && propManager.TryGetPropData(itemComp.uItemId, out var pNodeData)) {
                    MeshCombineManager.Inst.FilterCombineNode(pNodeData, out _, out var unCombineList);
                    if (unCombineList != null && unCombineList.Count > 0) {
                        MeshCombineManager.Inst.CreateUnCombineMesh(tmpObj, unCombineList);
                    }
                }

                RefreshAsset(tmpObj);
                hlodGroup.CurLODType = hlodGroup.targetLODType;
            });

        }

        public void ChangeToHigh() {
            if (!GameOfflineRenderManager.Inst.TryGetOfflineRenderData(itemComp.uItemId, out var renderData)) {
                RestoreByData();
                return;
            }

            if (hlodGroup.CurLODType == ModelLODType.LOW && renderData.IsSample()) {
                hlodGroup.CurLODType = hlodGroup.targetLODType;
                return;
            }

            GameOfflineRenderManager.Inst.LoadHighGameObject(itemComp.uItemId, anchorRoot, tmpObj =>
            {
                if (this == null || hlodGroup.CurLODType == hlodGroup.targetLODType)
                {
                    if (tmpObj != null) {
                        Destroy(tmpObj);
                    }
                    return;
                }

                if (tmpObj == null) {
                    RestoreByData();
                    return;
                }

                var propManager = GlobalNodeManager.Inst.Get<PropManager>();
                if (propManager != null && propManager.TryGetPropData(itemComp.uItemId, out var pNodeData)) {
                    MeshCombineManager.Inst.FilterCombineNode(pNodeData, out _, out var unCombineList);
                    if (unCombineList != null && unCombineList.Count > 0) {
                        MeshCombineManager.Inst.CreateUnCombineMesh(tmpObj, unCombineList);
                    }
                }

                RefreshAsset(tmpObj);
                hlodGroup.CurLODType = hlodGroup.targetLODType;
            });
        }

        public void ChangeToCull() {
            RefreshAsset(null);
            hlodGroup.CurLODType = hlodGroup.targetLODType;
        }

        public void RefreshAsset(GameObject asset) {
            if (assetObj == asset)
            {
                return;
            }
            if (assetObj != null)
            {
                Destroy(assetObj);
            }
            if (asset == null)
            {
                return;
            }

            assetObj = asset;
            assetObj.Reset();
            assetObj.transform.parent = anchorRoot;
            assetObj.transform.localPosition = itemComp.anchor;
        }



        private void RestoreByData() {
            if (hlodGroup == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(itemComp.uItemId))
            {
                LoggerUtils.LogError("rid 为空");
                hlodGroup.CurLODType = hlodGroup.targetLODType;
                return;
            }

            if (assetObj != null)
            {
                hlodGroup.CurLODType = hlodGroup.targetLODType;
                return;
            }

            var propManager = GlobalNodeManager.Inst.Get<PropManager>();
            if (propManager.TryGetPropData(itemComp.uItemId, out var pNodeData)) {
                RefreshAsset(MeshCombineManager.Inst.GetCombineObj(itemComp.uItemId, pNodeData, anchorRoot));
            } else {
                RefreshAsset(new GameObject("Empty GameObject"));
            }
            hlodGroup.CurLODType = hlodGroup.targetLODType;
        }

        public override void HighLight(bool isHigh) {
            base.HighLight(isHigh);
            var combineBehaviours = GetComponentsInChildren<TextureCombineBehaviour>();
            foreach (var tmpCombineBehaviour in combineBehaviours)
            {
                tmpCombineBehaviour.HighLight(isHigh);
            }
        }
    }
}
