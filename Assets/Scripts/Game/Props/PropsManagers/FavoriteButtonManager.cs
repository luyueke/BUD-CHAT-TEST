
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UIAgent;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(FavoriteButtonBehaviour))]
	public class FavoriteButtonManager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<FavoriteButtonComponent>();
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
				selectState = InteractInfo.collected;
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
				FavoriteButtonBehaviour btnBehav = nodeBehav as FavoriteButtonBehaviour;
				if (btnBehav != null)
				{
					btnBehav.SetSelectState(state,playAnim);
				}
			}
		}


		public void SendRequestFavorite()
		{
			var mapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
			if (mapInfo == null) return;
			
			JObject req = new JObject()
			{
				["id"] = mapInfo.id,
				["setType"] =  (int)CollectType.Collect,
			};
			NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCCollect, HttpMethod.POST, JsonConvert.SerializeObject(req), (content)=>
			{
				OnFavoriteCallBack(true);
			}, (error) =>
			{
				LoggerUtils.LogError("收藏失败："+error);
				OnFavoriteCallBack(false);
			});
		}
		
		private void OnFavoriteCallBack(bool isSuccess)
		{
			if (isSuccess)
			{
				UIAgentManager.Inst.ShowToast("已收藏");
				SetAllBtnState(1,true);
			}
		}

	}
}
        
