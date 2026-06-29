
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Utils;
using Game.ECS;
using GameData.Config;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(PortalButtonBehaviour))]
	public class PortalButtonManager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<PortalButtonComponent>();

			var buttonBehv = nodeBehaviour as PortalButtonBehaviour;
			RefreshNode(buttonBehv);
			CreatePointNode(buttonBehv);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyCreateInBuild(nodeBehaviour);

			RefreshNode(nodeBehaviour as PortalButtonBehaviour);
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);

			var buttonBehv = newBehaviour as PortalButtonBehaviour;
			RefreshNode(buttonBehv);
			CreatePointNode(buttonBehv);
		}

		protected override void OnNotifyRemove(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyRemove(nodeBehaviour);
			RemovePointNode(nodeBehaviour as PortalButtonBehaviour);
		}

		protected override void OnNotifyRevert(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyRevert(nodeBehaviour);

			var buttonBehv = nodeBehaviour as PortalButtonBehaviour;
			CreatePointNode(buttonBehv);
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

		/// <summary>
		/// 删除一个辅助传送光柱
		/// </summary>
		void RemovePointNode(PortalButtonBehaviour buttonBehv)
		{
			// 找到这个按钮对应的光柱
			var buttonComponent = buttonBehv.entity.GetComp<PortalButtonComponent>();
			var pointManager = GlobalNodeManager.Inst.Get<PortalPointManager>();
			var pointBehv = pointManager.GetBehaviour(buttonComponent.PointUid);
			if (pointBehv != null)
			{
				buttonComponent.PointUid = 0;
				buttonComponent.TmpOriginPostion = pointBehv.transform.position; // 记录一下位置
				GamePropNodeManager.Inst.DestroyNodeToSecondCache(pointBehv.gameObject);
				GamePropNodeManager.Inst.DestroyNode(pointBehv.gameObject);
			}
		}

		/// <summary>
		/// 创建一个辅助传送光柱
		/// </summary>
		void CreatePointNode(PortalButtonBehaviour buttonBehv)
		{
			var pointData = GamePropDataHelper.GetPropIdByNodeModelType(NodeModelType.PortalPoint);
			NodeBaseBehaviour pointBehv;
			GamePropNodeManager.Inst.TryCreateInEdit(pointData.Id, out pointBehv);

			if (pointBehv != null)
			{
				var buttonComponent = buttonBehv.entity.GetComp<PortalButtonComponent>();
				var goComponent = pointBehv.entity.GetGameObjectComponent();
				buttonComponent.PointUid = goComponent.Uid;
				if (buttonComponent.TmpOriginPostion != null)
				{
					pointBehv.transform.position = buttonComponent.TmpOriginPostion.Value;
					buttonComponent.TmpOriginPostion = null;
				} else {
					pointBehv.transform.position = buttonBehv.transform.position + new Vector3(2, 0, 0);
				}
			}
		}

		void ActiveNodes(bool active)
		{
			for (int i = 0; i < entities.Count; i++)
			{
				var buttonBehv = entities[i] as PortalButtonBehaviour;
				buttonBehv.ActiveText(active);
			}
		}

		void RefreshNode(PortalButtonBehaviour behaviour)
		{
			behaviour.SetNum(entities.Count);
		}
	}
}
        
