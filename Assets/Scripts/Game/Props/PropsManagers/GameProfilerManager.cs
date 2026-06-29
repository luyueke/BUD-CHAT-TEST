// @Author: YangJie
// @Description:
// @Date:  2023/10/08
// @Modify:

using System.Collections.Generic;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Utils;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using UIAgent;

namespace Game.Props.PropsManagers
{
    public class GameProfilerManager : GameInstance<GameProfilerManager>, INodeLife, IAutoInit
    {
        public MapStatisticInfo mapStatisticInfo;
        private Dictionary<uint, string> sceneNodeUidDic = new Dictionary<uint, string>();
        private Dictionary<string, List<uint>> materialCountDic = new Dictionary<string, List<uint>>(); //materials使用情况
        public bool IsInit = false;

        public GameProfilerManager()
        {
            mapStatisticInfo = new MapStatisticInfo();
            materialCountDic = new Dictionary<string, List<uint>>();
            sceneNodeUidDic = new Dictionary<uint, string>();
            IsInit = false;
            MessageHelper.AddListener<EnterGameModel, object>(MessageName.StartBuildMap, OnStartBuildMap);
            MessageHelper.AddListener<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, OnOverBuildMap);
        }

        private void OnStartBuildMap(EnterGameModel enterGameModel, object obj)
        {
            ResetData();
        }

        private void OnOverBuildMap(EnterGameModel enterGameModel, UgcBaseInfo info)
        {
            IsInit = true;
        }

        public void ResetData()
        {
            mapStatisticInfo.Clear();
            materialCountDic.Clear();
            sceneNodeUidDic.Clear();
        }

        public override void Release()
        {
            base.Release();
            MessageHelper.RemoveListener<EnterGameModel, object>(MessageName.StartBuildMap, OnStartBuildMap);
            MessageHelper.RemoveListener<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, OnOverBuildMap);
            ResetData();
        }

        public void OnCloneNode(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
        {
            if (CheckCurNodeIsInScene(newBehaviour))
            {
                return;
            }
            else
            {
                RecordSceneNodeAdd(newBehaviour);
            }
            
            if (newBehaviour is SimpleShapeBehaviour)
            {
                var gameObjectComponent = newBehaviour.entity.GetComp<GameObjectComponent>();
                var tmpMesh = MeshCombineManager.Inst.GetOriginMeshInfo(gameObjectComponent.PropId);
                mapStatisticInfo.vertices += tmpMesh.vertices.Length;
                mapStatisticInfo.triangles += tmpMesh.triangles.Length;
            }
            else if (newBehaviour is DTextBehaviour)
            {
                mapStatisticInfo.dTexts += 1;
            }
            else if(newBehaviour is PropBehaviour && newBehaviour.entity != null && newBehaviour.entity.HasComp<PropComponent>())
            {
                if(UIAgentManager.Inst.FindPanel(WindowId.UGCItemEditWindow, PanelId.UGCVehicleEditPanel)){
                    var propComponent = newBehaviour.entity.GetComp<PropComponent>();
                    GlobalNodeManager.Inst.Get<PropManager>().TryGetPropData(propComponent.uItemId, out var propData);
                    if(propData != null)
                    {
                        mapStatisticInfo.vertices += GlobalNodeManager.Inst.Get<PropManager>().GetPropDetailInfo(propComponent.uItemId).vertexs;
                        mapStatisticInfo.triangles += GlobalNodeManager.Inst.Get<PropManager>().GetPropDetailInfo(propComponent.uItemId).triangles;
                    }
                }
            }

            MessageHelper.Broadcast(MessageName.UpgradeProfilerInfo, true);
        }

        public void OnCreateNode(NodeBaseBehaviour nodeBehaviour, NodeCreateType createType)
        {
            if (CheckCurNodeIsInScene(nodeBehaviour))
            {
                return;
            }
            else
            {
                RecordSceneNodeAdd(nodeBehaviour);
            }
            
            if (nodeBehaviour is SimpleShapeBehaviour)
            {
                var gameObjectComponent = nodeBehaviour.entity.GetComp<GameObjectComponent>();
                var tmpMesh = MeshCombineManager.Inst.GetOriginMeshInfo(gameObjectComponent.PropId);
                mapStatisticInfo.vertices += tmpMesh.vertices.Length;
                mapStatisticInfo.triangles += tmpMesh.triangles.Length;
            }
            else if (nodeBehaviour is DTextBehaviour)
            {
                mapStatisticInfo.dTexts += 1;
            }
            else if(nodeBehaviour is PropBehaviour && nodeBehaviour.entity != null && nodeBehaviour.entity.HasComp<PropComponent>())
            {
                if(UIAgentManager.Inst.FindPanel(WindowId.UGCItemEditWindow, PanelId.UGCVehicleEditPanel)){
                    var propComponent = nodeBehaviour.entity.GetComp<PropComponent>();
                    GlobalNodeManager.Inst.Get<PropManager>().TryGetPropData(propComponent.uItemId, out var propData);
                    if(propData != null)
                    {
                        mapStatisticInfo.vertices += GlobalNodeManager.Inst.Get<PropManager>().GetPropDetailInfo(propComponent.uItemId).vertexs;
                        mapStatisticInfo.triangles += GlobalNodeManager.Inst.Get<PropManager>().GetPropDetailInfo(propComponent.uItemId).triangles;
                    }
                }
            }
            
            MessageHelper.Broadcast(MessageName.UpgradeProfilerInfo, true);
        }

        public void OnRemoveNode(NodeBaseBehaviour nodeBehaviour)
        {
            if (!CheckCurNodeIsInScene(nodeBehaviour))
            {
                return;
            }
            else
            {
                RecordSceneNodeRemove(nodeBehaviour);
            }
            
            if (nodeBehaviour is SimpleShapeBehaviour && nodeBehaviour.entity != null && nodeBehaviour.entity.HasComp<GameObjectComponent>())
            {
                var gameObjectComponent = nodeBehaviour.entity.GetComp<GameObjectComponent>();
                var tmpMesh = MeshCombineManager.Inst.GetOriginMeshInfo(gameObjectComponent.PropId);
                mapStatisticInfo.vertices -= tmpMesh.vertices.Length;
                mapStatisticInfo.triangles -= tmpMesh.triangles.Length;
            }
            else if (nodeBehaviour is DTextBehaviour)
            {
                mapStatisticInfo.dTexts -= 1;
            }else if(nodeBehaviour is PropBehaviour && nodeBehaviour.entity != null && nodeBehaviour.entity.HasComp<PropComponent>())
            {
                if(UIAgentManager.Inst.FindPanel(WindowId.UGCItemEditWindow, PanelId.UGCVehicleEditPanel)){
                    var propComponent = nodeBehaviour.entity.GetComp<PropComponent>();
                    GlobalNodeManager.Inst.Get<PropManager>().TryGetPropData(propComponent.uItemId, out var propData);
                    if(propData != null)
                    {
                        mapStatisticInfo.vertices -= GlobalNodeManager.Inst.Get<PropManager>().GetPropDetailInfo(propComponent.uItemId).vertexs;
                        mapStatisticInfo.triangles -= GlobalNodeManager.Inst.Get<PropManager>().GetPropDetailInfo(propComponent.uItemId).triangles;
                    }
                }
            }
            
            //删除材质信息
            if (nodeBehaviour != null && nodeBehaviour.entity != null && nodeBehaviour.entity.HasComp<GameObjectComponent>())
            {
                var gComp = nodeBehaviour.entity.GetComp<GameObjectComponent>();
                var nodeUid = gComp.Uid;     
                
                var combineBevList = nodeBehaviour.GetComponentsInChildren<MeshCombineBehaviour>();
                if (combineBevList != null && combineBevList.Length > 0)
                {
                    foreach (var meshCombineBev in combineBevList)
                    {
                        var matComp = meshCombineBev.GetMat();
                        var matUnionID = matComp.matId;
                        RecordMatUnUsed(matUnionID.UGCId, nodeUid); 
                    }
                }
                
                if (nodeBehaviour.entity.TryGetComp(out MaterialComponent materialComponent))
                {
                    var matUnionID = materialComponent.matId;
                    RecordMatUnUsed(matUnionID.UGCId, nodeUid); 
                }
            }

            MessageHelper.Broadcast(MessageName.UpgradeProfilerInfo, false);
        }

        public void OnRevertNode(NodeBaseBehaviour nodeBehaviour)
        {
            if (CheckCurNodeIsInScene(nodeBehaviour))
            {
                return;
            }
            else
            {
                RecordSceneNodeAdd(nodeBehaviour);
            }
            
            if (nodeBehaviour is SimpleShapeBehaviour && nodeBehaviour.entity != null && nodeBehaviour.entity.HasComp<GameObjectComponent>())
            {
                var gameObjectComponent = nodeBehaviour.entity.GetComp<GameObjectComponent>();
                var tmpMesh = MeshCombineManager.Inst.GetOriginMeshInfo(gameObjectComponent.PropId);
                mapStatisticInfo.vertices += tmpMesh.vertices.Length;
                mapStatisticInfo.triangles += tmpMesh.triangles.Length;
            }
            else if (nodeBehaviour is DTextBehaviour)
            {
                mapStatisticInfo.dTexts += 1;
            }
            else if(nodeBehaviour is PropBehaviour && nodeBehaviour.entity != null && nodeBehaviour.entity.HasComp<PropComponent>())
            {
                if(UIAgentManager.Inst.FindPanel(WindowId.UGCItemEditWindow, PanelId.UGCVehicleEditPanel)){
                    var propComponent = nodeBehaviour.entity.GetComp<PropComponent>();
                    GlobalNodeManager.Inst.Get<PropManager>().TryGetPropData(propComponent.uItemId, out var propData);
                    if(propData != null)
                    {
                        mapStatisticInfo.vertices += GlobalNodeManager.Inst.Get<PropManager>().GetPropDetailInfo(propComponent.uItemId).vertexs;
                        mapStatisticInfo.triangles += GlobalNodeManager.Inst.Get<PropManager>().GetPropDetailInfo(propComponent.uItemId).triangles;
                    }
                }
            }

            MessageHelper.Broadcast(MessageName.UpgradeProfilerInfo, true);
        }

        public void Init()
        {

        }


        public static int GetLimitVerticesCount(LimitType limitType)
        {
            switch (limitType)
            {
                case LimitType.Map:
                    return MapLimitSetting.MAX_VERTEX_COUNT;
                case LimitType.Prop:
                    return PropLimitSetting.MAX_VERTEX_COUNT;
                case LimitType.Cloth:
                    return PropClothLimitSetting.MAX_VERTEX_COUNT;
                case LimitType.Bag:
                    return PropBagLimitSetting.MAX_VERTEX_COUNT;
                default:
                    return MapLimitSetting.MAX_VERTEX_COUNT;
            }
        }


        public static int GetLimitTextCount(LimitType limitType)
        {
            switch (limitType)
            {
                case LimitType.Map:
                    return MapLimitSetting.MAX_DTEXT_COUNT;
                case LimitType.Prop:
                    return PropLimitSetting.MAX_DTEXT_COUNT;
                case LimitType.Cloth:
                    return PropClothLimitSetting.MAX_DTEXT_COUNT;
                case LimitType.Bag:
                    return PropBagLimitSetting.MAX_DTEXT_COUNT;
                default:
                    return MapLimitSetting.MAX_DTEXT_COUNT;
            }
        }

        public static int GetLimitMaterialsCount(LimitType limitType)
        {
            switch (limitType)
            {
                case LimitType.Map:
                    return MapLimitSetting.MAX_MATERIAL_COUNT;
                case LimitType.Prop:
                    return PropLimitSetting.MAX_MATERIAL_COUNT;
                case LimitType.Cloth:
                    return PropClothLimitSetting.MAX_MATERIAL_COUNT;
                case LimitType.Bag:
                    return PropBagLimitSetting.MAX_MATERIAL_COUNT;
                default:
                    return MapLimitSetting.MAX_MATERIAL_COUNT;
            }
        }

        #region 记录UGC材质使用情况
        public void RecordMatUsed(string matId, uint nodeUid)
        {
            if (string.IsNullOrEmpty(matId))
                return;

            if (!materialCountDic.ContainsKey(matId))
            {
                materialCountDic.Add(matId, new List<uint>());
            }
            
            materialCountDic[matId].Add(nodeUid);

            mapStatisticInfo.materials = materialCountDic.Count;
            MessageHelper.Broadcast(MessageName.UpgradeProfilerInfo, IsInit);
        }

        public void RecordMatUnUsed(string matId, uint nodeUid)
        {
            if (string.IsNullOrEmpty(matId))
                return;

            if (materialCountDic.ContainsKey(matId))
            {
                if (materialCountDic[matId].Contains(nodeUid))
                {
                    materialCountDic[matId].Remove(nodeUid);
                }

                if (materialCountDic[matId].Count == 0)
                    materialCountDic.Remove(matId);
            }
            
            mapStatisticInfo.materials = materialCountDic.Count;
            MessageHelper.Broadcast(MessageName.UpgradeProfilerInfo, false);
        }

        #endregion

        #region 发布校验

        public bool CheckStatisticInfoIsPass(LimitType type, DetailInfo detailInfo)
        {
            bool isPass = true;
            if (detailInfo == null)
                return true;
            
            MapStatisticInfo info = new MapStatisticInfo()
            {
                materials =  detailInfo.materials,
                vertices = detailInfo.vertexs,
                dTexts =  detailInfo.dTexts
            };
            
            var limitVerticesCount = GameProfilerManager.GetLimitVerticesCount(type);
            var limitMaterialsCount = GameProfilerManager.GetLimitMaterialsCount(type);
            var limitTextCount = GameProfilerManager.GetLimitTextCount(type);
            
            if (info.materials > limitMaterialsCount)
            {
                isPass = false;
            }
            if (info.vertices > limitVerticesCount)
            {
                isPass = false;
            }
            if (info.dTexts > limitTextCount)
            {
                isPass = false;
            }

            return isPass;
        }
        

        #endregion

        #region 校验当前NodeBaseBehaviour是否在场景中
        private bool CheckCurNodeIsInScene(NodeBaseBehaviour bev)
        {
            var isInScene = false;
            if (bev != null && bev.entity != null && bev.entity.HasComp<GameObjectComponent>())
            {
                var gComp = bev.entity.GetComp<GameObjectComponent>();
                var uid = gComp.Uid;
                if (sceneNodeUidDic.ContainsKey(uid))
                {
                    isInScene = true;
                }
            }
            return isInScene;
        }

        //记录当前节点
        private void RecordSceneNodeAdd(NodeBaseBehaviour bev)
        {
            if (bev != null && bev.entity != null && bev.entity.HasComp<GameObjectComponent>())
            {
                var gComp = bev.entity.GetComp<GameObjectComponent>();
                var uid = gComp.Uid;
                if (!sceneNodeUidDic.ContainsKey(uid))
                {
                    sceneNodeUidDic.Add(uid, "");
                }
            }
        }
        
        //移除记录
        private void RecordSceneNodeRemove(NodeBaseBehaviour bev)
        {
            if (bev != null && bev.entity != null && bev.entity.HasComp<GameObjectComponent>())
            {
                var gComp = bev.entity.GetComp<GameObjectComponent>();
                var uid = gComp.Uid;
                if (sceneNodeUidDic.ContainsKey(uid))
                {
                    sceneNodeUidDic.Remove(uid);
                }
            }
        }
        #endregion
    }

    // 地图统计信息
    public class MapStatisticInfo
    {
        public long memory = 0;
        public int triangles = 0;
        public int vertices = 0;
        public int materials = 0;
        public int dTexts = 0;

        public void Clear()
        {
            this.memory = 0;
            this.triangles = 0;
            this.vertices = 0;
            this.materials = 0;
            this.dTexts = 0;
        }
    }
    
    public enum LimitType
    {
        Map,
        Prop,
        Cloth,
        Bag,
    }
}