using System;
using System.Collections.Generic;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.OfflineRender;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Utils;
using GameData.Config;
using GameData.MapData;
using GameData.BaseInfo;
using GameData.OfflineRender;
using HLOD;
using Pb.Map;
using UGCAsset;
using UnityEngine;
using xasset;

namespace Game.Props.PropsManagers {
    [NodeBehaviourAttribute(typeof(PropBehaviour))]
    public class PropManager : BaseNodeManager {
        private readonly Dictionary<string, PNodeData> propDic = new Dictionary<string, PNodeData>();
        private readonly Dictionary<string, DetailInfo> propDetailInfoDic = new Dictionary<string, DetailInfo>();


        // 注册节点创建时的回调

        protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) {
            nodeBehaviour.entity.AddComp<PropComponent>();
            nodeBehaviour.entity.AddComp<TransactionComponent>();
        }


        protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour) {
            base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
            var nBehaviours = newBehaviour.GetComponentsInChildren<MeshCombineBehaviour>();
            var oBehaviours = oldBehaviour.GetComponentsInChildren<MeshCombineBehaviour>();
            for (var i = 0; i < nBehaviours.Length; i++) {
                nBehaviours[i].SetMat(oBehaviours[i].GetMat());
            }

            var nTextureBehaviours = newBehaviour.GetComponentsInChildren<TextureCombineBehaviour>();
            var oTextureBehaviours = oldBehaviour.GetComponentsInChildren<TextureCombineBehaviour>();
            for (var i = 0; i < nTextureBehaviours.Length; i++) {
                var tmpData = oTextureBehaviours[i].GetData();
                nTextureBehaviours[i].SetData(tmpData.Item1, tmpData.Item2, tmpData.Item3);
            }


            var nSimpleBehaviours = newBehaviour.GetComponentsInChildren<MeshSimpleBehaviour>();
            var oSimpleBehaviours = oldBehaviour.GetComponentsInChildren<MeshSimpleBehaviour>();
            for (var i = 0; i < nSimpleBehaviours.Length; i++) {
                var tmpData = oSimpleBehaviours[i].GetMat();
                nSimpleBehaviours[i].SetMat(oSimpleBehaviours[i].GetMat());
            }


        }



        public void Create(PropInfo propInfo, Action<PropBehaviour> callback = null) {
            if (string.IsNullOrEmpty(propInfo.id)) {
                callback?.Invoke(null);
                return;
            }

            GameOfflineRenderManager.Inst.AddOfflineRenderData(propInfo.id, new OfflineRenderData() {
                id = propInfo.id,
                metaDataUrl = propInfo.metaDataUrl,
                abConfig = propInfo.abConfig,
            });
            if (propDic.ContainsKey(propInfo.id)) {
                var pNodeData = GetEmptyNodeData(propInfo.id);
                GamePropNodeManager.Inst.TryCreateInEdit(pNodeData, out var nBehav);
                callback?.Invoke(nBehav as PropBehaviour);
            } else {
                var bytes = Asset.LoadRemoteAssetSync(propInfo.metaDataUrl);
                if (bytes == null) {
                    LoggerUtils.LogError("下载素材信息失败:" + propInfo.metaDataUrl);
                    callback?.Invoke(null);
                }

                var pPropData = MapPbDataTool.ParsePropPb(bytes);
                GameUgcMatManager.Inst.AddUGCMatData(pPropData.UgcmatData);
                propDic.TryAdd(propInfo.id, pPropData.NodeData);
                var pNodeData = GetEmptyNodeData(propInfo.id);
                GamePropNodeManager.Inst.TryCreateInEdit(pNodeData, out var nBehav);
                PropBehaviour propBehaviour = nBehav as PropBehaviour;
                if (propInfo.detailInfo != null && propBehaviour != null) {
                    propBehaviour.SetScaleAndAnchor(propInfo.detailInfo.scale, propInfo.detailInfo.anchor);
                }

                callback?.Invoke(propBehaviour);
            }

            if(propInfo.detailInfo != null)
            {
                propDetailInfoDic.TryAdd(propInfo.id, propInfo.detailInfo);
            }
        }

        protected override void OnNotifyRelease()
        {
            base.OnNotifyRelease();
            propDic?.Clear();
            propDetailInfoDic?.Clear();
        }

        public void InitPropData(PGameUGCItemMapData ugcItemData) {
            if (ugcItemData?.ItemDataList == null) {
                return;
            }

            foreach (var itemData in ugcItemData.ItemDataList) {
                propDic.TryAdd(itemData.UItemId, itemData.UItemNode);
            }
        }

        public void AddPropData(string id, PNodeData itemNode) {
            if (string.IsNullOrEmpty(id) || id  == GameConsts.ScenePropDraftId) {
                return;
            }
            propDic[id] = itemNode;
        }

        public void RemovePropData(string id) {
            propDic.Remove(id);
            propDetailInfoDic.Remove(id);
        }

        public bool TryGetPropData(string id, out PNodeData itemNode) {
            return propDic.TryGetValue(id, out itemNode);
        }

        public PGameUGCItemMapData SavePropData(PGamePropData propData) {
            var ids = new HashSet<string>();
            foreach (var tmpNodeData in propData.Pref) {
                GetPropIds(tmpNodeData, ids);
            }

            var itemDataList = new List<PGameUGCItemData>();
            foreach (var itemKeyValue in propDic) {
                if (ids.Contains(itemKeyValue.Key)) {
                    itemDataList.Add(new PGameUGCItemData() {
                        UItemId = itemKeyValue.Key,
                        UItemNode = itemKeyValue.Value
                    });
                }
            }

            var pGameUGCItemMapData = new PGameUGCItemMapData() {
                ItemDataList = { itemDataList }
            };
            return pGameUGCItemMapData;
        }


        public void GetPropIds(PNodeData pNodeData, HashSet<string> ids) {
            if (pNodeData.TryGetComponent<PropComponent>(out var compData)) {
                ids.Add(compData.uItemId);
            } else if (pNodeData.Prims != null && pNodeData.Prims.Count > 0) {
                foreach (var tmpPrim in pNodeData.Prims) {
                    GetPropIds(tmpPrim, ids);
                }
            }
        }

        public bool IsPropDetailInfoExist(string id) {
            return propDetailInfoDic.ContainsKey(id);
        }

        public void RemovePropDetailInfo(string id) {
            if(propDetailInfoDic.ContainsKey(id)){
                propDetailInfoDic.Remove(id);
            }
        }

        public void AddPropDetailInfo(string id, DetailInfo detailInfo) {
            propDetailInfoDic.TryAdd(id, detailInfo);
        }

        public DetailInfo GetPropDetailInfo(string id) {
            if (propDetailInfoDic.TryGetValue(id, out var detailInfo)) {
                return detailInfo;
            }
            return null;
        }

        public static PNodeData GetEmptyNodeData(string id) {
            var comp = new PropComponent {
                uItemId = id,
                scale = Vector3.one,
                anchor = Vector3.zero
            };
            var componentData = comp.Write();
            var pNodeData = new PNodeData {
                PropId = GamePropDataHelper.GetPropIdByNodeModelType(NodeModelType.Prop).Id,
                Pos = Vector3.zero.ToPB(),
                Scale = Vector3.one.ToPB(),
                Rotation = Vector3.zero.ToPB(),
                Attrs = { componentData }
            };
            return pNodeData;
        }
    }
}
