
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UIAgent;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(AttentionButtonBehaviour))]
	public class AttentionButtonManager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{
			nodeBehaviour.entity.AddComp<AttentionButtonComponent>();
		}

		public override void OnEdit()
		{
			base.OnEdit();
			EnterEditMode();
		}

		public override void OnGuest()
		{
			base.OnGuest();
			EnterGuestMode();
		}

		private void EnterGuestMode()
		{
			int selectState = 0;
			var creator = GameDataManager.Inst.mapGlobalData.Creator;
			if (creator != null && creator.relationShipInfo != null && (
			    creator.relationShipInfo.relationShip == (int)RelationShipType.Follow
			    && creator.relationShipInfo.relationStatus == (int)RelationStatusType.Posi ||
			    creator.relationShipInfo.relationStatus == (int)RelationStatusType.Each))
			{
				selectState = 1;
			}

			SetAllBtnState(selectState,false);
		}

		private void EnterEditMode()
		{
			SetAllBtnState(0,false);
		}

		public void SetAllBtnState(int state,bool playAnim)
		{
			foreach (var nodeBehav in entities)
			{
				AttentionButtonBehaviour btnBehav = nodeBehav as AttentionButtonBehaviour;
				if (btnBehav != null)
				{
					btnBehav.SetSelectState(state,playAnim);
				}
			}
		}


		public void SendRequestAttention()
		{
			var creator = GameDataManager.Inst.mapGlobalData.Creator;
			if (creator == null || string.IsNullOrEmpty(creator.uid))
			{
				UIAgentManager.Inst.ShowToast("获取用户信息失败，请重进试试");
				return;
			}

			if (creator.uid == AccountDataManager.Inst.Uid)
			{
				UIAgentManager.Inst.ShowToast("不能关注你自己");
				return;
			}


			SetRealtionParams setRelationReq = new SetRealtionParams();
			setRelationReq.setType = 1;
			setRelationReq.relationship = 1;
			setRelationReq.targetUid = creator.uid;
			NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setRelation, HttpMethod.POST,
				JsonConvert.SerializeObject(setRelationReq),
				OnFollowSuccess, OnFollowFail);
		}

		private void OnFollowSuccess(string message)
		{
			UIAgentManager.Inst.ShowToast("已关注");
			SetAllBtnState(1,true);
		}

		private void OnFollowFail(string message)
		{
			LoggerUtils.LogError("AtttionButtonManager OnFollowFail:",message);
		}
	}
}

