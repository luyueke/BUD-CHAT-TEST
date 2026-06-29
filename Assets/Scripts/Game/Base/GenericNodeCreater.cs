/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-18 11:08:28
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-07-28 16:52:58
 * @ Description: 通用模型的构建器
 */
using Game.ECS;
using GameData.Config;
using Pb.Map;
using UnityEngine;
using Es;
using Game.Utils;
using System;
using Game.Props.PropsBehaviours;

namespace Game.Base
{
    public class GenericNodeCreater : INodeCreater<PNodeData>
    {
        protected GamePropData gamePropConfig; // 配置信息

        public override void BindData(PNodeData data)
        {
            base.BindData(data);
            gamePropConfig = GamePropDataHelper.GetPropDataByID(data.PropId);
        }

        /// <summary>
        /// 创建模型行为层
        /// </summary>
		public override NodeBaseBehaviour CreateAssetBehaviour(Type behvT)
		{
			GameObject assetGo = null;
            if (typeof(MultiChildBehaviour).IsAssignableFrom(behvT))
            {
                assetGo = new GameObject("MultiChild");
            }
            else if (typeof(ActorNodeBehaviour).IsAssignableFrom(behvT))
            {
                assetGo = new GameObject("ActorNode");
            }
            else
            {
                assetGo = ModelCachePool.Inst.Get(gamePropConfig.Id);
            }

            NodeBaseBehaviour nodeBehaviour;
            if (assetGo.TryGetComponent(behvT, out var component))
            {
                nodeBehaviour = component as NodeBaseBehaviour;
            } else {
                nodeBehaviour = assetGo.AddComponent(behvT) as NodeBaseBehaviour;
            }

            return nodeBehaviour;
		}

		/// <summary>
		/// 生成Entity实体
		/// </summary>
		public override SceneEntity CreateEntity()
		{
			var entity = sceneWorld.CreateEntity();
            LoadComponents(entity);
            return entity;
		}

		public override void RefreshAssetGo(GameObject assetGo, GameObject parentGo)
		{
            if (parentGo != null) {
                assetGo.transform.SetParent(parentGo.transform);
            }
            
            if (FixNaN(createData.Pos, 0))
            {
                LoggerUtils.LogError("RefreshAssetGo Pos 出现非法数值");
            }

            assetGo.transform.localPosition = createData.Pos.ToVector3();
            assetGo.transform.localEulerAngles = createData.Rotation.ToVector3();
            assetGo.transform.localScale = createData.Scale.ToVector3();
		}

        
        public bool FixNaN(PVector3 vector,float newValue)
        {
            bool isError = false;
            if(float.IsNaN(vector.X))
            {
                vector.X = newValue;
                isError = true;
            }
            
            if(float.IsNaN(vector.Y))
            {
                vector.Y = newValue;
                isError = true;
            }
            
            if(float.IsNaN(vector.Z))
            {
                vector.Z = newValue;
                isError = true;
            }
            
            return isError;
        }

        public void LoadComponents(SceneEntity entity)
        {
            // 创建GameObjectComponent
            var gameObjectComponent = entity.AddComp<GameObjectComponent>();
            gameObjectComponent.Uid = UidManager.Inst.GetUid(createData);
            gameObjectComponent.PropId = createData.PropId;
            gameObjectComponent.ModelType = (NodeModelType)gamePropConfig.ModelType;
            gameObjectComponent.SetId((uint)NodeComponentId.GameObjectComponent);

            // 解析Components
            if (createData.Attrs != null)
            {
                for (int i = 0; i < createData.Attrs.Count; i++)
                {
                    var componentData = createData.Attrs[i];
                    NodeComponentId componentId = (NodeComponentId)componentData.CmpId;
                    var componentType = GameTypeRegister.Inst.GetComponentType(componentId);
                    if (typeof(IComponentSerializer).IsAssignableFrom(componentType)) // 是否是序列化类型的
                    {
                        var component = entity.AddComp(componentType);
                        component.SetId((uint)componentData.CmpId);
                        (component as IComponentSerializer).Read(componentData);
                    }
                }
            }
        }
    }
}
