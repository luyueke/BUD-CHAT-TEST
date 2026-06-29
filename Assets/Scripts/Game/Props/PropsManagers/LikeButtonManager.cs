
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData.BaseInfo;
using GameData.Manager;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UIAgent;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(LikeButtonBehaviour))]
	public class LikeButtonManager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<LikeButtonComponent>();
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
			var InteractInfo = GameDataManager.Inst.mapGlobalData.InteractInfo;
			if (InteractInfo != null)
			{
				selectState = InteractInfo.liked;
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
				LikeButtonBehaviour btnBehav = nodeBehav as LikeButtonBehaviour;
				if (btnBehav != null)
				{
					btnBehav.SetSelectState(state,playAnim);
				}
			}
		}


		public void SendRequestLike()
		{
			var mapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
			if (mapInfo == null || mapInfo.id == null) return;
			UGCCommonReq.Inst.UGCLikeReq(mapInfo.id,UGCCommonReq.LikeType.Like,OnLikeCallBack);
		}
		
		private void OnLikeCallBack(bool isSuccess)
		{
			if (isSuccess)
			{
				UIAgentManager.Inst.ShowToast("已点赞");
				SetAllBtnState(1,true);
			}
		}
		
	}
}
        
