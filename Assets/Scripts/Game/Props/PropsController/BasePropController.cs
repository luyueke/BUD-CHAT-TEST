/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-02 17:55:57
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-08 14:14:36
 * @ Description: 道具全局管理的控制器
 */

using Game.Base;
using Game.ECS;
using System.Collections.Generic;

namespace Game.Props.PropsController
{
	public abstract class BasePropController<T> : GameInstance<T>, INodeGlobalSelect, INodeLife, IModeManager, ICombine where T : BaseInstance, new()
	{
        protected List<NodeBaseBehaviour> nodeBaseBehaviours = new List<NodeBaseBehaviour>();

        protected abstract bool Filter(NodeBaseBehaviour nodeBehaviour);

        public void AddNode(NodeBaseBehaviour nodeBehaviour)
        {
			if (Filter(nodeBehaviour) && !nodeBaseBehaviours.Contains(nodeBehaviour))
            {
                nodeBaseBehaviours.Add(nodeBehaviour);
            }
        }

        public void RemoveNode(NodeBaseBehaviour nodeBehaviour)
        {
            if (nodeBaseBehaviours.Contains(nodeBehaviour))
                nodeBaseBehaviours.Remove(nodeBehaviour);
        }

		public virtual void OnCloneNode(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
            AddNode(newBehaviour);
		}

		public virtual void OnCreateNode(NodeBaseBehaviour nodeBehaviour, NodeCreateType createType)
		{
            AddNode(nodeBehaviour);
		}

		public virtual void OnRemoveNode(NodeBaseBehaviour nodeBehaviour)
		{
            RemoveNode(nodeBehaviour);
		}

		public virtual void OnRevertNode(NodeBaseBehaviour nodeBehaviour)
		{
            AddNode(nodeBehaviour);
		}

        public virtual void OnSelectNode(NodeBaseBehaviour nodeBehaviour)
        {
        }

        public virtual void OnUnSelectNode(NodeBaseBehaviour nodeBehaviour)
        {
        }

        public virtual void OnUnSelectAll()
        {
        }


        public virtual void OnEdit()
        {
        }

        public virtual void OnPlay()
        {
        }
        
        public virtual void OnGuest()
        {
        }

        public virtual void OnCombine(SceneEntity entity)
        {

        }
	}
}