/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-17 14:58:19
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-04 13:29:03
 * @ Description: 基础道具管理器基类
 */

using System.Collections.Generic;
using Game.Config;
using Game.ECS;
using Game.Utils;
using UnityEngine;

namespace Game.Base
{
	public abstract class BaseNodeManager : INodeManager, IModeManager
	{
        protected List<NodeBaseBehaviour> entities = new List<NodeBaseBehaviour>();
        protected virtual void OnNotifyRemove(NodeBaseBehaviour nodeBehaviour){}
        protected virtual void OnNotifyRevert(NodeBaseBehaviour nodeBehaviour){}
        protected virtual void OnNotifyRelease() { }
        // 检查创建条件
        protected virtual bool OnCheckCreateCondition() { return true; } 
        // 检查删除条件
        protected virtual bool OnCheckRemoveCondition() { return true; }
        // 通过场景还原创建
        protected virtual void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour) { }
        // 通过编辑模式创建
        protected virtual void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) { }
        // 通过克隆创建
        protected virtual void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour) { }

        public GameGlobalEnum.NodeOpReason CheckCreateCondition(string propId)
        {
            var propData = GamePropDataHelper.GetPropDataByID(propId);
            var canCreate = OnCheckCreateCondition();
            if (propData.MaxNum >= 0 && entities.Count >= propData.MaxNum)
            {
                // 超出配置就不能创建
                return GameGlobalEnum.NodeOpReason.CreateFail_MaxNum;
            }
            if (!canCreate)
            {
                return GameGlobalEnum.NodeOpReason.Unknown;
            }
            return GameGlobalEnum.NodeOpReason.CreatePrepare;
        }

        public GameGlobalEnum.NodeOpReason CheckRemoveCondition(string propId)
        {
            var propData = GamePropDataHelper.GetPropDataByID(propId);
            var canRemove = OnCheckRemoveCondition();
            if (propData.MinNum >= 0 && entities.Count <= propData.MinNum)
            {
                // 小于配置就不能删除
                return GameGlobalEnum.NodeOpReason.RemoveFail_MinNum;
            }
            if (!canRemove)
            {
                return GameGlobalEnum.NodeOpReason.Unknown;
            }
            return GameGlobalEnum.NodeOpReason.RemovePrepare;
        }

        /// <summary>
        /// 预留方法
        /// </summary>
        public void OnPrepareData(SceneEntity entity)
        {
            
        }

        public void OnCloneNode(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
        {
            if (!entities.Contains(newBehaviour))
            {
                entities.Add(newBehaviour);
                OnNotifyCreateInClone(oldBehaviour, newBehaviour);
            }
        }

        /// <summary>
        /// 创建道具实例的回调
        /// </summary>
		public void OnCreateNode(NodeBaseBehaviour nodeBehaviour, NodeCreateType createType)
		{
			if (!entities.Contains(nodeBehaviour))
            {
                entities.Add(nodeBehaviour);
                switch (createType)
                {
                    case NodeCreateType.Edit:
                        OnNotifyCreateInEdit(nodeBehaviour);
                        break;
                    case NodeCreateType.SceneBuild:
                        OnNotifyCreateInBuild(nodeBehaviour);
                        break;
                }
            }
		}

        /// <summary>
        /// 删除道具实例的回调
        /// </summary>
		public void OnRemoveNode(NodeBaseBehaviour nodeBehaviour)
		{
			if (entities.Contains(nodeBehaviour))
            {
                entities.Remove(nodeBehaviour);
                OnNotifyRemove(nodeBehaviour);
            }
		}

        public void OnRevertNode(NodeBaseBehaviour nodeBehaviour)
        {
            if (!entities.Contains(nodeBehaviour))
            {
                entities.Add(nodeBehaviour);
                OnNotifyRevert(nodeBehaviour);
            }
        }

        /// <summary>
        /// 切换到编辑模式时调用
        /// </summary>
        public virtual void OnEdit()
        {
	        
        }
        
        /// <summary>
        /// 切换到Play模式时调用
        /// </summary>
        public virtual void OnPlay()
        {
	        
        }
        
        /// <summary>
        /// 切换到Guest模式时调用
        /// </summary>
        public virtual void OnGuest()
        {
	        
        }

        /// <summary>
        /// 释放所有的道具实例 
        /// </summary>
        public void Release()
        {
            entities.Clear();
            OnNotifyRelease();
        }
	}
}