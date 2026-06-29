
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using System.Collections.Generic;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(PortalPointBehaviour))]
	public class PortalPointManager : BaseNodeManager
	{
		Dictionary<uint, NodeBaseBehaviour> nodeDict = new Dictionary<uint, NodeBaseBehaviour>();

		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{
			AddNodeDict(nodeBehaviour);
			RefreshNode(nodeBehaviour as PortalPointBehaviour);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyCreateInBuild(nodeBehaviour);
			AddNodeDict(nodeBehaviour);
			RefreshNode(nodeBehaviour as PortalPointBehaviour);
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
			AddNodeDict(newBehaviour);
			RefreshNode(newBehaviour as PortalPointBehaviour);
		}

		protected override void OnNotifyRemove(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyRemove(nodeBehaviour);
			RmNodeDict(nodeBehaviour);
		}

		protected override void OnNotifyRevert(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyRevert(nodeBehaviour);
			AddNodeDict(nodeBehaviour);
		}

		public NodeBaseBehaviour GetBehaviour(uint uid)
		{
			if (nodeDict.ContainsKey(uid))
				return nodeDict[uid];

			return null;
		}

		void AddNodeDict(NodeBaseBehaviour nodeBehaviour)
		{
			var uid = nodeBehaviour.entity.GetComp<GameObjectComponent>().Uid;
			if (!nodeDict.ContainsKey(uid))
			{
				nodeDict.Add(uid, nodeBehaviour);
			}
		}

		void RmNodeDict(NodeBaseBehaviour nodeBehaviour)
		{
			var uid = nodeBehaviour.entity.GetComp<GameObjectComponent>().Uid;
			if (nodeDict.ContainsKey(uid))
			{
				nodeDict.Remove(uid);
			}
		}

		public override void OnEdit()
		{
			base.OnEdit();
			ActiveNodes(true);
		}

		public override void OnPlay()
		{
			base.OnPlay();
			ActiveNodes(false);
		}

		public override void OnGuest()
		{
			base.OnGuest();
			ActiveNodes(false);
		}

		void ActiveNodes(bool active)
		{
			for (int i = 0; i < entities.Count; i++)
			{
				entities[i].gameObject.SetActive(active);
			}
		}

		void RefreshNode(PortalPointBehaviour behaviour)
		{
			behaviour.SetNum(entities.Count);
		}
	}
}
        
