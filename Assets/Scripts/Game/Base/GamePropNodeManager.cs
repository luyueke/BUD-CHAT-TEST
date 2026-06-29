using System.Collections.Generic;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Utils;
using GameData.Config;
using Google.Protobuf.Collections;
using Pb.Map;
using UnityEngine;
using Game.Config;
using System.Linq;
using System;
using Basic.Extensions;
using Basic.Utils;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using GameData.BaseInfo;
using Object = UnityEngine.Object;

namespace Game.Base
{
    public class GamePropNodeManager : GameInstance<GamePropNodeManager>
    {
        private List<NodeBaseBehaviour> AllNodeBaseBehaviours = new List<NodeBaseBehaviour>();
        private EcsSceneWorld sceneWorld;
        private NodeFlowSystem nodeFlowSystem;
        private SecondCachePool secondCachePool;
        Vector3 cloneOffset = new Vector3(-0.2f, 0.0001f, 0.0001f);


        public void Init(EcsSceneWorld wrold)
        {
            sceneWorld = wrold;
            nodeFlowSystem = new NodeFlowSystem(sceneWorld);
            secondCachePool = new SecondCachePool();
            secondCachePool.AddOverMaxListener(OnSecondCacheOverMax);
        }

        public NodeFlowSystem GetNodeFlowSystem() {
            return nodeFlowSystem;
        }

        public List<T> GetBehaviours<T>() where T : NodeBaseBehaviour {
           return AllNodeBaseBehaviours.Where(tmp => tmp != null && tmp is T).Cast<T>().ToList();
        }


        public T GetBehaviour<T>() where T : NodeBaseBehaviour {
            return AllNodeBaseBehaviours.FirstOrDefault(tmp => tmp is T) as T;
        }



        public void AddBehaviour(NodeBaseBehaviour behaviour)
        {
            if (behaviour != null && !AllNodeBaseBehaviours.Contains(behaviour))
            {
                AllNodeBaseBehaviours.Add(behaviour);
            }
        }

        public void RemoveBehaviour(NodeBaseBehaviour behaviour)
        {
            //TODO 标记移除Entity
            nodeFlowSystem?.Remove(behaviour);
            AllNodeBaseBehaviours?.Remove(behaviour);
            InterfaceNotifyUtil.OnRemoveNode(behaviour);
        }

        public void RevertBehaviour(NodeBaseBehaviour behaviour)
        {
            AddBehaviour(behaviour);
            //通知需要监听道具回滚的管理器：如自定义碰撞体属性、发光属性、开关控制等
            InterfaceNotifyUtil.OnRevertNode(behaviour);
        }

        public NodeBaseBehaviour GetBehaviourById(uint findUid)
        {
            foreach(var nBehaviour in AllNodeBaseBehaviours){
                if(nBehaviour.entity.GetComp<GameObjectComponent>().Uid == findUid){
                    return nBehaviour;
                }
            }
            return null;
        }

        /// <summary>
        /// 只在逻辑层合并一系列的节点，不生成表现层实体（主要用于素材数据的生成）
        /// </summary>
        public Tuple<PVector3, PNodeData> CombineNodeInLogic()
        {
            var parent = SceneBuilder.Inst.StageParent;
            var entitys = GetAllNodeInFirstLayer(parent);
            var exModelType = new List<NodeModelType>() { NodeModelType.Terrain, NodeModelType.SpawnPoint ,NodeModelType.PreviewModel}; // 需要排除的模型
            entitys = entitys.Where(e => !exModelType.Contains(e.GetGameObjectComponent().ModelType)).ToList();

            List<SceneEntity> packEntitys = new List<SceneEntity>();
            for (var i = 0; i < entitys.Count; i++)
            {
                var gComp = entitys[i].GetComp<GameObjectComponent>();
                switch (gComp.ModelType)
                {
                    case NodeModelType.Combine:
                        var combineTrans = entitys[i].GetComp<GameObjectComponent>().BindGo.transform;
                        for (int j = 0; j < combineTrans.childCount; j++)
                        {
                            var nodeBehav = combineTrans.GetChild(j).GetComponent<NodeBaseBehaviour>();
                            if (nodeBehav != null)
                            {
                                packEntitys.Add(nodeBehav.entity);
                            }
                        }
                        break;
                    default:
                        packEntitys.Add(entitys[i]);
                        break;
                }
            }

            var centerPos = GamePropUtils.GetCenterPoint(packEntitys);
            var conEntity = nodeFlowSystem.CreateLogicEntity(GameConsts.CombinePropId, centerPos, parent);
            var conVirtualGo = conEntity.GetViewGo();
            for (int i = 0; i < entitys.Count; i++)
            {
                // 调整节点位置
                entitys[i].GetViewGo().transform.SetParent(conVirtualGo.transform);
            }

            PNodeData nodeData = null;
            try {
                nodeData = nodeFlowSystem.SerializeEntity(conEntity);
                nodeData.Prims.AddRange(GetChildrenData(conVirtualGo.transform, null, true));
                nodeData.Pos = Vector3.zero.ToPB();
                nodeData.Rotation = Vector3.zero.ToPB();
                nodeData.Scale = Vector3.one.ToPB();
                nodeData.Uid = 0;
            } catch (Exception e) {
                LoggerUtils.LogError("CombineNodeInLogic Error:" + e.Message);
            }

            var bounds = conEntity.GetBounds();

            for (int i = 0; i < entitys.Count; i++)
            {
                // 重置节点位置
                entitys[i].GetViewGo().transform.SetParent(parent);
            }

            Object.Destroy(conVirtualGo);
            sceneWorld.DestroyEntity(conEntity);
            return new Tuple<PVector3, PNodeData>(bounds.size.ToPB(), nodeData);
        }

        /// <summary>
        /// 用来计算合一后坐标，获取偏移量
        /// </summary>
        public Vector3 CombineNodePosLogic()
        {
            var parent = SceneBuilder.Inst.StageParent;
            var entitys = GetAllNodeInFirstLayer(parent);
            var exModelType = new List<NodeModelType>() { NodeModelType.Terrain, NodeModelType.SpawnPoint, NodeModelType.PreviewModel }; // 需要排除的模型
            entitys = entitys.Where(e => !exModelType.Contains(e.GetGameObjectComponent().ModelType)).ToList();

            List<SceneEntity> packEntitys = new List<SceneEntity>();
            for (var i = 0; i < entitys.Count; i++)
            {
                var gComp = entitys[i].GetComp<GameObjectComponent>();
                switch (gComp.ModelType)
                {
                    case NodeModelType.Combine:
                        var combineTrans = entitys[i].GetComp<GameObjectComponent>().BindGo.transform;
                        for (int j = 0; j < combineTrans.childCount; j++)
                        {
                            var nodeBehav = combineTrans.GetChild(j).GetComponent<NodeBaseBehaviour>();
                            if (nodeBehav != null)
                            {
                                packEntitys.Add(nodeBehav.entity);
                            }
                        }
                        break;
                    default:
                        packEntitys.Add(entitys[i]);
                        break;
                }
            }

            return GamePropUtils.GetCenterPoint(packEntitys);
        }

        public SceneEntity CombineNode(List<SceneEntity> entitys,Transform par = null)
        {
            List<SceneEntity> packEntitys = new List<SceneEntity>();
            List<SceneEntity> destoryEntitys = new List<SceneEntity>();

            for (var i = 0; i < entitys.Count; i++)
            {
                // entitys[i].RemoveComp<RPAnimComponent>();
                // entitys[i].RemoveComp<MovementComponent>();
                // entitys[i].RemoveComp<CollectControlComponent>();
                // entitys[i].RemoveComp<FollowableComponent>();
                // FollowModeManager.Inst.OnCombineNode(entitys[i]);
                // ShowHideManager.Inst.OnCombineNode(entitys[i]);
                // SwitchControlManager.Inst.OnCombineNode(entitys[i]);
                // SwitchManager.Inst.OnCombineNode(entitys[i]);
                // SensorBoxManager.Inst.OnCombineNode(entitys[i]);
                // LockHideManager.Inst.RefreshLockList(entitys[i].GetComp<GameObjectComponent>().Uid, false);
                // PickabilityManager.Inst.OnCombineNode(entitys[i]);
                // EdibilityManager.Inst.OnCombineNode(entitys[i]);
                // FishingManager.Inst.OnCombineNode(entitys[i]);
                //以上需要监听OnCombine事件的，只需要在Manager里实现ICombine接口
                GlobalNodeManager.Inst.OnNodeCombine(entitys[i]);
                InterfaceNotifyUtil.OnCombine(entitys[i]);

                var gComp = entitys[i].GetComp<GameObjectComponent>();
                switch (gComp.ModelType)
                {
                    case NodeModelType.Combine:
                        var combineTrans = entitys[i].GetComp<GameObjectComponent>().BindGo.transform;
                        for (int j = 0; j < combineTrans.childCount; j++)
                        {
                            var nodeBehav = combineTrans.GetChild(j).GetComponent<NodeBaseBehaviour>();
                            if (nodeBehav != null)
                            {
                                packEntitys.Add(nodeBehav.entity);
                            }
                        }
                        destoryEntitys.Add(entitys[i]);
                        break;
                    default:
                        packEntitys.Add(entitys[i]);
                        break;
                }
            }

            var centerPos = GamePropUtils.GetCenterPoint(packEntitys);
            NodeBaseBehaviour conBehav;
            TryCreateInEdit(GameConsts.CombinePropId, out conBehav, centerPos);
            if (par != null)
            {
                conBehav.transform.SetParent(par);
            }

            AddBehaviour(conBehav);
            var comParent = conBehav.transform;
            packEntitys.ForEach(x =>
            {
                var entityGo = x.GetComp<GameObjectComponent>().BindGo;
                entityGo.transform.SetParent(comParent);
                entityGo.transform.localScale = entityGo.transform.localScale.LimitVector3();
            });
            destoryEntitys.ForEach(x =>
            {
                var comp = x.GetComp<GameObjectComponent>();
                DestroyNodeToSecondCache(comp.BindGo);
            });
            //TODO:方向盘需要对组合后的节点特殊处理
            // SteeringWheelManager.Inst.CombineCar(conBehav.entity);
            return conBehav.entity;
        }


        private void DestroyOneNode(NodeBaseBehaviour nBehav,bool noCache = false)
        {

            SceneEntity entity = nBehav.entity;
            GameObjectComponent comp = entity.GetComp<GameObjectComponent>();
            var assetId = comp.PropId;
            ActorNodeBehaviour actorBehaviour = null;
            if (nBehav is ActorNodeBehaviour)
            {
                actorBehaviour = nBehav as ActorNodeBehaviour;
                assetId = actorBehaviour.GetAssetId();
            }


            RemoveBehaviour(nBehav);
            secondCachePool?.RemoveItem(nBehav.gameObject);
            sceneWorld?.DestroyEntity(entity);

            nBehav.OnReset();

            if(actorBehaviour != null)
            {
                if(actorBehaviour.assetObj != null)
                {
                    ModelCachePool.Inst.Release(assetId,actorBehaviour.assetObj);
                }
                GameObject.Destroy(nBehav.gameObject);
            }
            else
            {
                if (noCache)
                {
                    GameObject.Destroy(nBehav.gameObject);
                }
                else
                {
                    ModelCachePool.Inst.Release(assetId, nBehav.gameObject);
                }


            }

        }

        /// <summary>
        /// 原DestroyEntity
        /// 删除节点和其子节点，并回收至ModelCachePool
        /// </summary>
        /// <param name="targetGo"></param>
        public void DestroyNode(GameObject targetGo,bool noCache = false)
        {
            var nodeBehaviours = targetGo.GetComponentsInChildren<NodeBaseBehaviour>(true);
            List<NodeBaseBehaviour> delayDestroyBehvs = new List<NodeBaseBehaviour>();
            for (int i = 0; i < nodeBehaviours.Length; i++)
            {
                var nBehav = nodeBehaviours[i];
                if (IsDelayDestroy(nBehav))
                {
                    delayDestroyBehvs.Add(nBehav);
                    continue;
                }

                DestroyOneNode(nBehav,noCache);
            }

            for (int i = 0; i < delayDestroyBehvs.Count; i++)
            {
                DestroyOneNode(delayDestroyBehvs[i],noCache);
            }

            if (nodeBehaviours == null || nodeBehaviours.Length == 0)
            {
                GameObject.Destroy(targetGo);
            }
        }




        private bool IsDelayDestroy(NodeBaseBehaviour behaviour)
        {
            //如CombineBehaviour等有多个子节点的behaviour
            if (behaviour is MultiChildBehaviour)
            {
                return true;
            }
            return false;
        }


        #region 包装SecondCachePool，后续外部不允许直接调用SeconeCachePool
        public GameGlobalEnum.NodeOpReason TryDeleteInEdit(GameObject gameObject)
        {
            var baseBehaivours = gameObject.GetComponentsInChildren<NodeBaseBehaviour>(true);
            for (int i = 0; i < baseBehaivours.Length; i++)
            {
                var nBehav = baseBehaivours[i];
                var gameObjComponent = nBehav.entity.GetComp<GameObjectComponent>();
                var propId = gameObjComponent.PropId;
                var nodeModelType = GamePropDataHelper.GetNodeModelTypeByID(propId);
                var iNodeManager = GlobalNodeManager.Inst.Get(nodeModelType);
                var checkReason = iNodeManager.CheckRemoveCondition(propId);

                if (checkReason != GameGlobalEnum.NodeOpReason.RemovePrepare)
                {
                    // 子节点有一个不能删除就整个父节点也不能删除
                    return checkReason;
                }
            }

            DestroyNodeToSecondCache(gameObject);
            return GameGlobalEnum.NodeOpReason.RemoveSuccess;
        }


        /// <summary>
        /// 删除节点，并缓存到包装SecondCachePool，供UndoRedo使用
        /// </summary>
        /// <param name="gameObject"></param>
        public void DestroyNodeToSecondCache(GameObject gameObject)
        {
            if (gameObject == null) { return; }

            secondCachePool?.DestroyNode(gameObject);
            var baseBehaivours = gameObject.GetComponentsInChildren<NodeBaseBehaviour>(true);
            if (baseBehaivours == null) { return; }

            for (int i = 0; i < baseBehaivours.Length; i++)
            {
                var nBehav = baseBehaivours[i];
                RemoveBehaviour(nBehav);
            }
        }

        /// <summary>
        /// 从缓存池中移除一个节点
        /// </summary>
        /// <param name="go"></param>
        public void RemoveItemFromSecondCache(GameObject go)
        {
            secondCachePool?.RemoveItem(go);
        }


        /// <summary>
        /// 从缓存池中回滚一个节点，并执行RevertBehaviour
        /// </summary>
        /// <param name="gameObject"></param>
        public void RevertNodeFromSecondCache(GameObject gameObject)
        {
            secondCachePool?.RevertItem(gameObject);
            var baseBehaivours = gameObject.GetComponentsInChildren<NodeBaseBehaviour>(true);
            for (int i = 0; i < baseBehaivours.Length; i++)
            {
                var nBehav = baseBehaivours[i];
                RevertBehaviour(nBehav);
                nodeFlowSystem.Revert(nBehav);
            }
        }

        /// <summary>
        /// 从缓存池弹出指定节点，不执行RevertBehaviour
        /// </summary>
        /// <param name="gameObject"></param>
        public void PopItemFromSecondCache(GameObject gameObject)
        {
            secondCachePool?.RevertItem(gameObject);
        }

        /// <summary>
        /// 通过道具uid，从缓存池中获取一个节点
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public GameObject GetNodeFromSecondCache(uint uid)
        {
            return secondCachePool?.GetGameObjectByUid(uid);
        }



        public bool IsContainsSecondCache(GameObject go)
        {
            return secondCachePool.IsContains(go);
        }


        public void ClearSecondCache()
        {
            List<GameObject> toDestroyList = secondCachePool.ClearPool();
            if (toDestroyList != null)
            {
                foreach (GameObject gameObject in toDestroyList)
                {
                    if (gameObject != null)
                    {
                        DestroyNode(gameObject);
                    }
                }
            }
        }


        private void OnSecondCacheOverMax(GameObject target)
        {
            DestroyNode(target);
        }

        #endregion



        #region 创建逻辑
        public NodeBaseBehaviour CreateSceneNodeByData(PNodeData data,Transform parent = null)
        {
            if (data == null )
            {
                return null;
            }
            if (nodeFlowSystem == null)//大厅发布载具时，会为空
            {
                nodeFlowSystem = new NodeFlowSystem(new EcsSceneWorld());
            }
            var entity = nodeFlowSystem.Create(data, parent);
            NodeBaseBehaviour nBehaviour = entity?.GetViewGo()?.GetComponent<NodeBaseBehaviour>();
            AddBehaviour(nBehaviour);
            
            return nBehaviour;
        }

        public void CreateSceneNodes(RepeatedField<PNodeData>nodes, Transform parent = null)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                CreateSceneNodeByData(nodes[i], parent);
            }
        }

        /// <summary>
        /// 由UI创建调用, 会检测创建条件
        /// </summary>
        public GameGlobalEnum.NodeOpReason TryCreateInEdit(
            string propId,
            out NodeBaseBehaviour behaviour,
            Vector3 pos = default,
            Transform parent = null)
        {
            behaviour = null;
            var nodeModelType = GamePropDataHelper.GetNodeModelTypeByID(propId);
            var iNodeManager = GlobalNodeManager.Inst.Get(nodeModelType);
            var checkReason = iNodeManager.CheckCreateCondition(propId);
            if (checkReason != GameGlobalEnum.NodeOpReason.CreatePrepare)
            {
                return checkReason;
            }

            if (pos == default)
            {
                pos = GameCameraUtils.Inst.GetCreatePosition();
            }
            var entity = nodeFlowSystem.Create(propId, parent, pos);
            GameObject nodeGo = entity.GetViewGo();
            behaviour = nodeGo.GetComponent<NodeBaseBehaviour>();
            AddBehaviour(behaviour);
            return GameGlobalEnum.NodeOpReason.CreateSuccess;
        }

        public GameGlobalEnum.NodeOpReason TryCreateInEdit(PNodeData pNodeData, out NodeBaseBehaviour behaviour, Transform parent = null)
        {
            behaviour = null;
            var nodeModelType = GamePropDataHelper.GetNodeModelTypeByID(pNodeData.PropId);
            var iNodeManager = GlobalNodeManager.Inst.Get(nodeModelType);
            var checkReason = iNodeManager.CheckCreateCondition(pNodeData.PropId);
            if (checkReason != GameGlobalEnum.NodeOpReason.CreatePrepare)
            {
                return checkReason;
            }

            if (pNodeData.Pos.ToVector3() == default)
            {
                pNodeData.Pos = GameCameraUtils.Inst.GetCreatePosition().ToPB();
            }
            var entity = nodeFlowSystem.RealCreate(pNodeData, parent, NodeCreateType.Edit);;
            var nodeGo = entity.GetViewGo();
            behaviour = nodeGo.GetComponent<NodeBaseBehaviour>();
            AddBehaviour(behaviour);
            return GameGlobalEnum.NodeOpReason.CreateSuccess;
        }

        /// <summary>
        /// 克隆一个节点
        /// </summary>
        public GameGlobalEnum.NodeOpReason TryCloneInEidt(GameObject oldGo, out NodeBaseBehaviour newBehv)
        {
            newBehv = null;
            var oldBehv = oldGo.GetComponent<NodeBaseBehaviour>();
            var oldChildBehv = oldBehv.GetComponentsInChildren<NodeBaseBehaviour>(true);
            // 判断是否创建阻断
            for (int i = 0; i < oldChildBehv.Length; i++)
            {
                var nBehav = oldChildBehv[i];
                var gameObjComponent = nBehav.entity.GetComp<GameObjectComponent>();
                var propId = gameObjComponent.PropId;
                var nodeModelType = GamePropDataHelper.GetNodeModelTypeByID(propId);
                var iNodeManager = GlobalNodeManager.Inst.Get(nodeModelType);
                var checkReason = iNodeManager.CheckCreateCondition(propId);

                if (checkReason != GameGlobalEnum.NodeOpReason.CreatePrepare)
                {
                    // 子节点有一个不能删除就整个父节点也不能删除
                    return checkReason;
                }
            }

            // 复制UI对象
            newBehv = GameObject.Instantiate(oldBehv, oldBehv.transform.parent);
            newBehv.name.Replace("(Clone)", "");
            // 处理子节点
            var newChildBehv = newBehv.GetComponentsInChildren<NodeBaseBehaviour>(true);
            for  (int i = oldChildBehv.Length - 1; i >= 0; i--)
            {
                // 克隆逻辑对象
                nodeFlowSystem.Clone(oldChildBehv[i], newChildBehv[i]);
                // LoggerUtils.Log($"TryCloneInEidt => {oldChildBehv[i].gameObject.name}");
            }

            newBehv.transform.position += cloneOffset;
            AddBehaviour(newBehv);

            return GameGlobalEnum.NodeOpReason.CreateSuccess;
        }
#endregion

#region 序列化
        /// <summary>
        /// 获取所有道具数据
        /// </summary>
        /// <returns></returns>
        public PGamePropData SavePropData()
        {
            var propData = new PGamePropData();
            var parent = SceneBuilder.Inst.StageParent;
            var nodeDatas = GetChildrenData(parent, null);
            propData.Pref.AddRange(nodeDatas);
            return propData;
        }

        //isPropMode:如果是保存素材，uid清0
        //如果是UGC 素材且使用离线渲染
        private RepeatedField<PNodeData> GetChildrenData(Transform node, List<uint> ignorekeys, bool isPropMode = false)
        {
            RepeatedField<PNodeData> nodeDatas = new RepeatedField<PNodeData>();
            var entitys = GetAllNodeInFirstLayer(node);
            for (int i = 0; i < entitys.Count; i++)
            {
                var nodeData = nodeFlowSystem.SerializeEntity(entitys[i], ignorekeys);
                var trans = entitys[i].GetViewGo().transform;
                nodeData.Uid = isPropMode ? 0: nodeData.Uid;
                nodeDatas.Add(nodeData);

                RepeatedField<PNodeData> primsDatas = GetChildrenData(trans, ignorekeys, isPropMode);
                nodeData.Prims.AddRange(primsDatas);
            }
            return nodeDatas;
        }

        private List<SceneEntity> GetAllNodeInFirstLayer(Transform node)
        {
            List<SceneEntity> behaviours = new List<SceneEntity>();
            for (int i = 0; i < node.childCount; i++)
            {
                var nBehaviour = node.GetChild(i).GetComponent<NodeBaseBehaviour>();
                if (nBehaviour != null)
                {
                    behaviours.Add(nBehaviour.entity);
                }
            }
            return behaviours;
        }
#endregion

#region 素材数据的导出
        public PUGCItemData SaveUgcItemData()
        {
            var tuple = CombineNodeInLogic();
            var ugcItemData = new PUGCItemData
            {
                NodeData = tuple.Item2,
                UgcmatData = GameUgcMatManager.Inst.SaveUGCMatData(),
                Size = tuple.Item1
            };
            return ugcItemData;
        }

        public DetailInfo GetUGCItemDetailInfo()
        {
            var detailInfo = new DetailInfo();
            detailInfo.vertexs = GameProfilerManager.Inst.mapStatisticInfo.vertices;
            detailInfo.triangles = GameProfilerManager.Inst.mapStatisticInfo.triangles;
            detailInfo.materials = GameProfilerManager.Inst.mapStatisticInfo.materials;
            detailInfo.dTexts = GameProfilerManager.Inst.mapStatisticInfo.dTexts;
            return detailInfo;
        }

        /// <summary>
        /// 地图编辑器中的发布
        /// </summary>
        public PNodeData SaveUgcItemDataInGame(SceneEntity entity) {

            var parent = entity.GetViewGo().transform.parent;
            var centerPos = entity.GetViewGo().transform.localPosition;
            var conEntity = nodeFlowSystem.CreateLogicEntity(GameConsts.CombinePropId, centerPos, parent);
            var conVirtualGo = conEntity.GetViewGo();
            entity.GetViewGo().transform.SetParent(conVirtualGo.transform);
            PNodeData nodeData = null;
            try {
                nodeData = nodeFlowSystem.SerializeEntity(conEntity);
                nodeData.Prims.AddRange(GetChildrenData(conVirtualGo.transform, null, true));
                nodeData.Pos = Vector3.zero.ToPB();
                nodeData.Rotation = Vector3.zero.ToPB();
                nodeData.Scale = Vector3.one.ToPB();
                nodeData.Uid = 0;
            } catch (Exception e) {
                LoggerUtils.LogError("SaveUgcItemDataInGame Error:" + e.Message);
            }

            entity.GetViewGo().transform.SetParent(parent);
            Object.Destroy(conVirtualGo);
            sceneWorld.DestroyEntity(conEntity);
            return nodeData;
        }

        public PUGCItemData SaveUGCItemData(SceneEntity entity)
        {
            var nodeData = SaveUgcItemDataInGame(entity);
            var uMatId = new HashSet<string>();
            foreach (var tmpNodeData in nodeData.Prims)
            {
                if (tmpNodeData.TryGetComponent<MaterialComponent>(out var comp))
                {
                    if (comp.matId.IsUGC)
                    {
                        uMatId.Add(comp.matId.UGCId);
                    }
                }
            }
            var ugcItemData = new PUGCItemData
            {
                NodeData = nodeData,
                UgcmatData = GameUgcMatManager.Inst.SaveUGCMatData(uMatId.ToList()),
                Size = DataUtil.CalculateBoundingBox(entity.GetViewGo().transform).size.ToPB()
            };
            return ugcItemData;
        }

        public DetailInfo GetUGCItemDetailInfo(SceneEntity entity)
        {
            var detailInfo = new DetailInfo();
            var nodeData = SaveUgcItemDataInGame(entity);
            foreach (var tmpNodeData in nodeData.Prims)
            {
                var tmpMesh = MeshCombineManager.Inst.GetOriginMeshInfo(tmpNodeData.PropId);
                detailInfo.vertexs += tmpMesh.vertices.Length;
                detailInfo.triangles += tmpMesh.triangles.Length;
            }
            return detailInfo;
        }




        #endregion

#region 其他接口
        public List<NodeBaseBehaviour> GetAllNodeInFirstLayer(Transform node, List<Type> excludeNodeBehav = null)
        {
            List<NodeBaseBehaviour> behaviours = new List<NodeBaseBehaviour>();
            for (int i = 0; i < node.childCount; i++)
            {
                var nBehaviour = node.GetChild(i).GetComponent<NodeBaseBehaviour>();
                if (nBehaviour != null)
                {
                    if (excludeNodeBehav != null)
                    {
                        if (!excludeNodeBehav.Contains(nBehaviour.GetType()))
                        {
                            behaviours.Add(nBehaviour);
                        }
                    } else {
                        behaviours.Add(nBehaviour);
                    }
                }
            }
            return behaviours;
        }
#endregion

        public override void Release()
        {
            base.Release();
            secondCachePool?.Release();
        }

        private int airlineId = 0;

        public void OnCombineStart()
        {
            if (AllNodeBaseBehaviours is not { Count: > 0 })
                return;

            foreach (var bev in AllNodeBaseBehaviours)
            {
                if (bev is CombineBehaviour)
                {
                    continue;
                }

                var propEditCfg = bev.entity.GetEditOperationConfig();
                if (propEditCfg == null) continue;
                var isCanCombine = propEditCfg.CanCombine == 1;
                if (!isCanCombine && bev.gameObject.activeSelf)
                {
                    airlineId = FlyToTheMoonUtils.Inst.FlyAwayInNewAirline(airlineId, bev.gameObject);
                }
            }
        }

        public void OnCombineEnd()
        {
            FlyToTheMoonUtils.Inst.ReturnAllShipsInAirline(airlineId);
            airlineId = 0;
        }
    }
}

