
using System.Collections.Generic;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsController;
using GameData.Config;
using GameData.GameSync;
using Google.Protobuf;
using Pb.Game;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(SwitchBtnBehaviour))]
	public class SwitchBtnManager : BaseNodeManager
	{
		private const string TAG = "SwitchBtnManager";
		private int CurrentNum;
		private Dictionary<uint, SwitchBtnBehaviour> switchDict = new Dictionary<uint, SwitchBtnBehaviour>();

		//当前控制的类型
		private readonly List<GameGlobalEnum.PropControlType> controlTypes = new List<GameGlobalEnum.PropControlType>
		{
			GameGlobalEnum.PropControlType.Visible,
			GameGlobalEnum.PropControlType.Movement,
			GameGlobalEnum.PropControlType.Anim,
			// GameGlobalEnum.PropControlType.Sound,
			// GameGlobalEnum.PropControlType.Firework
		};


		public int GetNewIndex()
	    {
	        return ++CurrentNum;
	    }

		public void UpdateMaxIndex(int index)
		{
			if (index > CurrentNum)
			{
				CurrentNum = index;
			}
		}

		public uint GetUidByIndex(int index)
		{
			foreach (var uid in switchDict.Keys)
			{
				var b = switchDict[uid];
				if (index == b.entity.GetComp<SwitchBtnComponent>().SwitchIndex)
				{
					return uid;
				}
			}
			return 0;
		}

		public int GetIndexByUid(uint uid)
		{
			if (switchDict.ContainsKey(uid))
			{
				var b = switchDict[uid];
				int index = b.entity.GetComp<SwitchBtnComponent>().SwitchIndex;
				return index;
			}
			return 0;
		}


		public List<int> GetIndexList()
		{
			List<int> tempList = new List<int>();
			foreach (var behaviour in switchDict.Values)
			{
				int index = behaviour.entity.GetComp<SwitchBtnComponent>().SwitchIndex;
				if (!tempList.Contains(index))
				{
					tempList.Add(index);
				}
			}
			tempList.Sort();
			return tempList;
		}

		/// <summary>
		/// 根据controlType从SwitchBtnComponent获取被控制道具id
		/// </summary>
		/// <param name="switchComp"></param>
		/// <param name="controlType"></param>
		/// <returns></returns>
		private List<uint> GetCtrListFromSwitchComp(SwitchBtnComponent switchComp,GameGlobalEnum.PropControlType controlType)
		{
			if(switchComp.CtrTypeDicts.ContainsKey(controlType))
			{
				return switchComp.CtrTypeDicts[controlType];
			}
			return null;
		}

		public void AddSwitch(NodeBaseBehaviour behaviour)
		{
			uint uid = behaviour.entity.GetComp<GameObjectComponent>().Uid;
			if (!switchDict.ContainsKey(uid))
			{
				SwitchBtnBehaviour b = behaviour as SwitchBtnBehaviour;
				switchDict.Add(uid, b);
			}
		}

		#region  ================绑定与解除绑定==================
		/// <summary>
		/// 从开关的 各个属性列 表移除控制体的uid
		/// </summary>
		/// <param name="entity"></param>
		public void RemoveControlledId(uint ctrId)
		{
			foreach (var sid in switchDict.Keys)
			{
				var behav = switchDict[sid];
				var switchBtnComp = behav.entity.GetComp<SwitchBtnComponent>();
				switchBtnComp.RemoveAllCtrId(ctrId);
			}
		}

		/// <summary>
		/// 从指定开关的指定 属性列表 将控制物品id
		/// </summary>
		/// <param name="ctrId"></param>
		/// <param name="switchId"></param>
		/// <param name="controlType"></param>
		public void RemoveCtrIdFromSwitchComp(uint ctrId,uint switchId,GameGlobalEnum.PropControlType controlType)
		{
			if(!switchDict.ContainsKey(switchId)) return;
			SwitchBtnComponent switchBtnComp = switchDict[switchId].entity.GetComp<SwitchBtnComponent>();
			var ctrUids = GetCtrListFromSwitchComp(switchBtnComp, controlType);
			ctrUids?.Remove(ctrId);
		}


		/// <summary>
		/// 将被控制物品id关联到开关上
		/// </summary>
		/// <param name="ctrId"></param>
		/// <param name="switchId"></param>
		/// <param name="controlType"></param>
		public void AddCtrIdToSwitchComp(uint ctrId,uint switchId,GameGlobalEnum.PropControlType controlType)
		{
			if(!switchDict.ContainsKey(switchId)) return;
			SwitchBtnComponent switchBtnComp = switchDict[switchId].entity.GetComp<SwitchBtnComponent>();
			switchBtnComp.AddCtrId(ctrId,controlType);
		}
		#endregion ================绑定与解除绑定==================



		#region  点击开关的处理
		public void HandleSwitchClick(SwitchBtnBehaviour behaviour)
		{
			var comp = behaviour.entity.GetComp<SwitchBtnComponent>();

			foreach (var controlType in controlTypes)
			{
				var ctrUids = GetCtrListFromSwitchComp(comp, controlType);
				if (ctrUids != null && ctrUids.Count > 0)
				{
					foreach (var uid in ctrUids)
					{
						SwitchCtrController.Inst.OnHandleControl(uid,controlType);
					}
				}
			}
		}


		public void SendRequest(SwitchBtnBehaviour behaviour)
		{
			SwitchBtnNetData netData = new SwitchBtnNetData();
            netData.Uid = behaviour.entity.GetComp<GameObjectComponent>().Uid;
			LoggerUtils.Log("控制开关："+netData.Uid);
            netData.Status = behaviour.isWork ? 1 : 0;
            NetSyncManager.Inst.Send(SubCmdType.SwitchBtn,netData);
		}


		#endregion


		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
			SwitchBtnBehaviour switchBehav = newBehaviour as SwitchBtnBehaviour;
			switchBehav.RefreshIndex();
			AddSwitch(newBehaviour);
		}

		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{
			SwitchBtnComponent switchBtnComp =nodeBehaviour.entity.AddComp<SwitchBtnComponent>();
			switchBtnComp.SwitchIndex = GetNewIndex();
			SwitchBtnBehaviour switchBehav = nodeBehaviour as SwitchBtnBehaviour;
			switchBehav.RefreshIndex();
			AddSwitch(nodeBehaviour);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			SwitchBtnBehaviour switchBehav = nodeBehaviour as SwitchBtnBehaviour;
			SwitchBtnComponent switchBtnComp =nodeBehaviour.entity.GetComp<SwitchBtnComponent>();
			UpdateMaxIndex(switchBtnComp.SwitchIndex);
			switchBehav.RefreshIndex();
			AddSwitch(nodeBehaviour);
		}

		//从地图删除开关
		protected override void OnNotifyRemove(NodeBaseBehaviour nodeBehaviour)
		{
			GameObjectComponent goComp = nodeBehaviour.entity.GetComp<GameObjectComponent>();
			SwitchBtnComponent sComp = nodeBehaviour.entity.GetComp<SwitchBtnComponent>();
			uint curSwitchId = goComp.Uid;

			switchDict.Remove(goComp.Uid);
			//从开关锁控制的物体移除开关本身
			foreach (var controlType in controlTypes)
			{
				var ctrUids = GetCtrListFromSwitchComp(sComp, controlType);
				if (ctrUids == null || ctrUids.Count == 0)
				{
					continue;
				}
				foreach (var uid in ctrUids)
				{
					SwitchCtrController.Inst.UnbindEntityByUid(uid, curSwitchId, controlType);
				}
			}

		}

		protected override void OnNotifyRevert(NodeBaseBehaviour nodeBehaviour)
		{
			AddSwitch(nodeBehaviour);
			SwitchBtnBehaviour sBehav = nodeBehaviour as SwitchBtnBehaviour;
			sBehav.RefreshIndex();

			GameObjectComponent goComp = nodeBehaviour.entity.GetComp<GameObjectComponent>();
			SwitchBtnComponent sComp = nodeBehaviour.entity.GetComp<SwitchBtnComponent>();
			uint curSwitchId = goComp.Uid;

			//开关锁控制的物体关联开关本身
			foreach (var controlType in controlTypes)
			{
				var ctrUids = GetCtrListFromSwitchComp(sComp, controlType);
				if (ctrUids == null || ctrUids.Count == 0)
				{
					continue;
				}
				foreach (var uid in ctrUids)
				{
					var ctrProNodeBehav = GamePropNodeManager.Inst.GetBehaviourById(uid);
					if (ctrProNodeBehav != null && ctrProNodeBehav.entity != null)
					{
						SwitchCtrController.Inst.BindEntity(ctrProNodeBehav.entity, curSwitchId, controlType);
					}
				}
			}
		}



        private void EnterPlayMode()
		{
			foreach (var switchBehav in switchDict.Values)
			{
				switchBehav.SetTextVisible(false);
			}
		}

		private void EnterEditMode()
		{
			foreach (var switchBehav in switchDict.Values)
			{
				switchBehav.SetTextVisible(true);
			}
		}

		public override void OnEdit()
		{
			base.OnEdit();
			EnterEditMode();
		}

		public override void OnGuest()
		{
			base.OnGuest();
			EnterPlayMode();
			NetSyncManager.Inst.AddBroadcastListener(SubCmdType.SwitchBtn,OnRecvServer);
			NetSyncManager.Inst.AddMapInfoListener(SubCmdType.SwitchBtn,OnMapInfoSync);
		}

		public override void OnPlay()
		{
			base.OnPlay();
			EnterPlayMode();
		}

		protected override void OnNotifyRelease()
        {
            base.OnNotifyRelease();
			NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.SwitchBtn,OnRecvServer);
			NetSyncManager.Inst.RemoveMapInfoListener(SubCmdType.SwitchBtn,OnMapInfoSync);
        }


		private void OnRecvServer(CommonSyncClientData netData)
		{
			SwitchBtnNetData switchNetData = (SwitchBtnNetData)netData.Body;
			LoggerUtils.Log($"{TAG} OnReceiveServer==> Uid:{switchNetData.Uid} , Status:{switchNetData.Status}");
			if(!switchDict.ContainsKey(switchNetData.Uid))
			{
				LoggerUtils.Log($"{TAG} OnReceiveServer 找不到该开关：{switchNetData.Uid}");
				return;
			}
			SwitchBtnBehaviour btnBehaviour = switchDict[switchNetData.Uid];
			bool serverIsWork = switchNetData.Status == 1;
			if(serverIsWork == btnBehaviour.isWork)
			{
				LoggerUtils.Log("SwitchButton OnReceiveServer 出现两次一样的值");
				return;
			}
			btnBehaviour.isWork = serverIsWork;
            HandleSwitchClick(btnBehaviour);
		}

		private void OnMapInfoSync(string mapId,List<IMessage> propDatas)
		{
			for (int i = 0; i < propDatas.Count; i++)
			{
				SwitchBtnNetData switchNetData = (SwitchBtnNetData)propDatas[i];
				if(!switchDict.ContainsKey(switchNetData.Uid)) continue;
				SwitchBtnBehaviour btnBehaviour = switchDict[switchNetData.Uid];
				if(btnBehaviour.isWork != (switchNetData.Status == 1))
				{
					HandleSwitchClick(btnBehaviour);
					btnBehaviour.isWork = true;
				}
			}
		}

	}
}

