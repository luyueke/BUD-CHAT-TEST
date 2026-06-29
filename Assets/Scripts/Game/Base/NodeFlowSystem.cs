/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-18 11:19:28
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-31 15:54:59
 * @ Description: 节点流程控制系统
 */
using GameData.Config;
using Game.ECS;
using Pb.Map;
using Game.Utils;
using UnityEngine;
using Google.Protobuf.Collections;
using System.Collections.Generic;
using Game.Props.PropsBehaviours;

namespace Game.Base
{
    public enum NodeCreateType
    {
        SceneBuild, // 通过场景还原创建
        Edit, // 通过编辑创建
    }

    public class NodeFlowSystem
    {
        SceneNodeFactory nodeFactory;
        EcsSceneWorld sceneWorld;

        public NodeFlowSystem(EcsSceneWorld world)
        {
            sceneWorld = world;
            nodeFactory = new SceneNodeFactory(world);
        }

        /// <summary>
        /// 创建一个临时的Entity(没有实际的表现层逻辑)
        /// </summary>
        public SceneEntity CreateLogicEntity(string propId, Vector3 postion, Transform parent = null)
        {
            PNodeData data = new PNodeData();
            data.PropId = propId;
            data.Pos = postion.ToPB();
            data.Scale = Vector3.one.ToPB();
            data.Rotation = Vector3.zero.ToPB();
            var creater = nodeFactory.GetCreater<GenericNodeCreater, PNodeData>(data);
            var entity = creater.CreateEntity();
            // 这里空节点主要为了序列化的操作
            var virtualGo = new GameObject();
            virtualGo.name = $"VirtualCombine";
            creater.RefreshAssetGo(virtualGo, parent.gameObject);
            entity.SetViewGo(virtualGo);
            return entity;
        }

        /// <summary>
        /// 克隆一个临时的Entity(没有实际的表现层逻辑)
        /// </summary>
        public SceneEntity CloneLogicEntity(SceneEntity entity, Transform parent = null)
        {
            var nEntity = sceneWorld.CloneEntity(entity);
            // 这里空节点主要为了序列化的操作
            var oGo = entity.GetViewGo();
            if (oGo != null)
            {
                var virtualGo = new GameObject();
                virtualGo.name = oGo.name;
                virtualGo.transform.SetParent(oGo.transform.parent);
                virtualGo.transform.position = oGo.transform.position;
                virtualGo.transform.eulerAngles = oGo.transform.eulerAngles;
                virtualGo.transform.localScale = oGo.transform.localScale;
                virtualGo.transform.SetParent(parent);
                nEntity.SetViewGo(virtualGo);
            }
            return nEntity;
        }

        /// <summary>
        /// 通过道具ID创建默认道具
        /// </summary>
        public SceneEntity Create(string propId, Transform parent = null, Vector3 pos = default)
        {
            PNodeData data = new PNodeData();
            data.PropId = propId;
            data.Pos = pos.ToPB();
            data.Scale = Vector3.one.ToPB();
            data.Rotation = Vector3.zero.ToPB();
            return RealCreate(data, parent, NodeCreateType.Edit);
        }


        /// <summary>
        /// 场景还原创建
        /// </summary>
        public SceneEntity Create(PNodeData data, Transform parent = null)
        {
            return RealCreate(data, parent, NodeCreateType.SceneBuild);
        }

        /// <summary>
        /// 通过数据进行道具创建
        /// </summary>
        /// <param name="propId">节点ID</param>
        /// <param name="data">节点数据</param>
        public SceneEntity RealCreate(PNodeData data, Transform parent, NodeCreateType createType)
        {
            var gamePropConfig = GamePropDataHelper.GetPropDataByID(data.PropId);
            if (gamePropConfig == null) {
                LoggerUtils.LogError("GetPropDataByID null:" + data.PropId);
                return null;
            }
            // 创建
            var creater = nodeFactory.GetCreater<GenericNodeCreater, PNodeData>(data);
            var managerInst = GlobalNodeManager.Inst.Get((NodeModelType)gamePropConfig.ModelType);

            // 创建Entity
            var entity = creater.CreateEntity();
            managerInst?.OnPrepareData(entity);

            // 绑定Behaviour
            NodeBaseBehaviour nodeBehaviour = null;
            if (managerInst!=null)
            {
                var attrs = managerInst.GetType().GetCustomAttributes(false);
                if (attrs.Length > 0)
                {
                    NodeBehaviourAttribute nodeBehaviourAttr = attrs[0] as NodeBehaviourAttribute;

                    nodeBehaviour = creater.CreateAssetBehaviour(nodeBehaviourAttr.BehaviourType);
                    nodeBehaviour.entity = entity;
                }

                if (nodeBehaviour != null)
                {
                    if (parent == null && SceneBuilder.HasInstance ) {
                        parent = SceneBuilder.Inst.StageParent;
                    }
                    creater.RefreshAssetGo(nodeBehaviour.gameObject, parent != null ? parent.gameObject : null);
                    entity.SetViewGo(nodeBehaviour.gameObject);
                    nodeBehaviour.OnInitByCreate();
                }
            }

            if (nodeBehaviour is MultiChildBehaviour multiChildBehaviour)
            {
                multiChildBehaviour.CreateChildNodes(data.Prims);
            }
            // 创建完成通知
            managerInst?.OnCreateNode(nodeBehaviour, createType);
            InterfaceNotifyUtil.OnCreateNode(nodeBehaviour, createType);

            return entity;
        }

        /// <summary>
        /// 删除一个节点
        /// </summary>
        public void Remove(NodeBaseBehaviour behaviour)
        {
            var entity = behaviour.entity;
            var gameObjComponent = entity.GetComp<GameObjectComponent>();
            var managerInst = GlobalNodeManager.Inst.Get(gameObjComponent.ModelType);
            managerInst?.OnRemoveNode(behaviour);
        }

        /// <summary>
        /// 从Undo池子里回滚一个节点
        /// </summary>
        public void Revert(NodeBaseBehaviour behaviour)
        {
            var entity = behaviour.entity;
            var gameObjComponent = entity.GetComp<GameObjectComponent>();
            var managerInst = GlobalNodeManager.Inst.Get(gameObjComponent.ModelType);
            managerInst?.OnRevertNode(behaviour);
        }

        /// <summary>
        /// Clone 一个节点
        /// </summary>
        public void Clone(NodeBaseBehaviour oldBehv, NodeBaseBehaviour newBehv)
        {
            var newEntity = sceneWorld.CloneEntity(oldBehv.entity);
            newEntity.SetViewGo(newBehv.gameObject);
            newBehv.entity = newEntity;
            newBehv.OnInitByCreate();

            var gameObjComponent = newEntity.GetComp<GameObjectComponent>();
            var iNodeManager = GlobalNodeManager.Inst.Get(gameObjComponent.ModelType);
            iNodeManager.OnCloneNode(oldBehv, newBehv);
            InterfaceNotifyUtil.OnCloneNode(oldBehv, newBehv);
        }

        public PNodeData SerializeEntity(SceneEntity entity, List<uint> ignorekeys = null)
        {
            var nodeData = new PNodeData();
            var gComp = entity.GetComp<GameObjectComponent>();
            Transform nodeTrans = gComp.BindGo.transform;
            nodeData.Uid = gComp.Uid;
            nodeData.PropId = gComp.PropId;
            nodeData.Pos = nodeTrans.localPosition.ToPB();
            nodeData.Rotation = nodeTrans.localEulerAngles.ToPB();
            nodeData.Scale = nodeTrans.localScale.LimitVector3().ToPB();
            RepeatedField < PComponentData > compDatas = GetComponentsAttrByEntity(entity, ignorekeys);
            nodeData.Attrs.AddRange(compDatas);
            return nodeData;
        }

        private RepeatedField<PComponentData> GetComponentsAttrByEntity(SceneEntity entity, List<uint> ignorekeys)
        {
            RepeatedField < PComponentData > compDatas = new RepeatedField<PComponentData>();
            foreach (var comp in entity.Components.Values)
            {
                if (comp is IComponentSerializer)
                {
                    comp.SetId((uint)GameTypeRegister.Inst.GetComponentId(comp.GetType()));
                    var serComp = comp as IComponentSerializer;
                    var componentData = serComp.Write();
                    if (componentData != null && (ignorekeys == null || !ignorekeys.Contains(comp.CmpId)))
                    {
                        componentData.CmpId = comp.CmpId;
                        compDatas.Add(componentData);
                    }
                }
            }
            return compDatas;
        }
    }
}
