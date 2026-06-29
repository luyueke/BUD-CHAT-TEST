
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
    [NodeBehaviourAttribute(typeof(SensorBoxBehaviour))]
	public class SensorBoxManager : BaseNodeManager
	{
		private const string TAG = "SensorBoxManager";
		private int CurrentNum;
		private Dictionary<uint, SensorBoxBehaviour> sensorBoxDict = new Dictionary<uint, SensorBoxBehaviour>();

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
			foreach (var uid in sensorBoxDict.Keys)
			{
				var b = sensorBoxDict[uid];
				if (index == b.entity.GetComp<SensorBoxComponent>().BoxIndex)
				{
					return uid;
				}
			}
			return 0;
		}

		public int GetIndexByUid(uint uid)
		{
			if (sensorBoxDict.ContainsKey(uid))
			{
				var b = sensorBoxDict[uid];
				int index = b.entity.GetComp<SensorBoxComponent>().BoxIndex;
				return index;
			}
			return 0;
		}


		public List<int> GetIndexList()
		{
			List<int> tempList = new List<int>();
			foreach (var behaviour in sensorBoxDict.Values)
			{
				int index = behaviour.entity.GetComp<SensorBoxComponent>().BoxIndex;
				if (!tempList.Contains(index))
				{
					tempList.Add(index);
				}
			}
			tempList.Sort();
			return tempList;
		}

		/// <summary>
		/// 根据controlType从SensorBoxComponent获取被控制道具id
		/// </summary>
		/// <param name="sComp"></param>
		/// <param name="controlType"></param>
		/// <returns></returns>
		private List<uint> GetCtrListFromSensorComp(SensorBoxComponent sComp,GameGlobalEnum.PropControlType controlType)
		{
			if(sComp.CtrTypeDicts.ContainsKey(controlType))
			{
				return sComp.CtrTypeDicts[controlType];
			}
			return null;
		}

		public void AddSensorBox(NodeBaseBehaviour behaviour)
		{
			uint uid = behaviour.entity.GetComp<GameObjectComponent>().Uid;
			if (!sensorBoxDict.ContainsKey(uid))
			{
				SensorBoxBehaviour b = behaviour as SensorBoxBehaviour;
				sensorBoxDict.Add(uid, b);
			}
		}

		#region  ================绑定与解除绑定==================
		/// <summary>
		/// 从感应盒的 各个属性列 表移除控制体的uid
		/// </summary>
		/// <param name="entity"></param>
		public void RemoveControlledId(uint ctrId)
		{
			foreach (var sid in sensorBoxDict.Keys)
			{
				var behav = sensorBoxDict[sid];
				var sensorBoxComp = behav.entity.GetComp<SensorBoxComponent>();
				sensorBoxComp.RemoveAllCtrId(ctrId);
			}
		}

		/// <summary>
		/// 从指定感应盒的指定 属性列表 将控制物品id
		/// </summary>
		/// <param name="ctrId"></param>
		/// <param name="sId"></param>
		/// <param name="controlType"></param>
		public void RemoveCtrIdFromSensorComp(uint ctrId,uint sId,GameGlobalEnum.PropControlType controlType)
		{
			if(!sensorBoxDict.ContainsKey(sId)) return;
			SensorBoxComponent sensorBoxComp = sensorBoxDict[sId].entity.GetComp<SensorBoxComponent>();
			var ctrUids = GetCtrListFromSensorComp(sensorBoxComp, controlType);
			ctrUids?.Remove(ctrId);
		}


		/// <summary>
		/// 将被控制物品id关联到感应盒上
		/// </summary>
		/// <param name="ctrId"></param>
		/// <param name="sId"></param>
		/// <param name="controlType"></param>
		public void AddCtrIdToSensorComp(uint ctrId,uint sId,GameGlobalEnum.PropControlType controlType)
		{
			if(!sensorBoxDict.ContainsKey(sId)) return;
			SensorBoxComponent sensorBoxComp = sensorBoxDict[sId].entity.GetComp<SensorBoxComponent>();
			sensorBoxComp.AddCtrId(ctrId,controlType);
		}
		#endregion ================绑定与解除绑定==================



		#region  点击感应盒的处理
		public void HandleSensorBoxTouch(SensorBoxBehaviour behaviour)
		{
			var comp = behaviour.entity.GetComp<SensorBoxComponent>();

			foreach (var controlType in controlTypes)
			{
				var ctrUids = GetCtrListFromSensorComp(comp, controlType);
				if (ctrUids != null && ctrUids.Count > 0)
				{
					foreach (var uid in ctrUids)
					{
						SensorCtrController.Inst.OnHandleControl(uid,controlType);
					}
				}
			}
		}
		#endregion


		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
			SensorBoxBehaviour sensorBoxBehav = newBehaviour as SensorBoxBehaviour;
			sensorBoxBehav.RefreshIndex();
			AddSensorBox(newBehaviour);
		}

		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{
			SensorBoxComponent sensorBoxComp =nodeBehaviour.entity.AddComp<SensorBoxComponent>();
			sensorBoxComp.BoxIndex = GetNewIndex();
			SensorBoxBehaviour sensorBoxBehav = nodeBehaviour as SensorBoxBehaviour;
			sensorBoxBehav.RefreshIndex();
			AddSensorBox(nodeBehaviour);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			SensorBoxBehaviour sensorBoxBehav = nodeBehaviour as SensorBoxBehaviour;
			SensorBoxComponent sensorBoxComp =nodeBehaviour.entity.GetComp<SensorBoxComponent>();
			UpdateMaxIndex(sensorBoxComp.BoxIndex);
			sensorBoxBehav.RefreshIndex();
			AddSensorBox(nodeBehaviour);
		}

		//从地图删除感应盒
		protected override void OnNotifyRemove(NodeBaseBehaviour nodeBehaviour)
		{
			GameObjectComponent goComp = nodeBehaviour.entity.GetComp<GameObjectComponent>();
			SensorBoxComponent sComp = nodeBehaviour.entity.GetComp<SensorBoxComponent>();
			uint curSensorBoxId = goComp.Uid;

			sensorBoxDict.Remove(goComp.Uid);
			//从感应盒锁控制的物体移除感应盒本身
			foreach (var controlType in controlTypes)
			{
				var ctrUids = GetCtrListFromSensorComp(sComp, controlType);
				if (ctrUids == null || ctrUids.Count == 0)
				{
					continue;
				}
				foreach (var uid in ctrUids)
				{
					SensorCtrController.Inst.UnbindEntityByUid(uid, curSensorBoxId, controlType);
				}
			}

		}

		protected override void OnNotifyRevert(NodeBaseBehaviour nodeBehaviour)
		{
			AddSensorBox(nodeBehaviour);
			SensorBoxBehaviour sBehav = nodeBehaviour as SensorBoxBehaviour;
			sBehav.RefreshIndex();

			GameObjectComponent goComp = nodeBehaviour.entity.GetComp<GameObjectComponent>();
			SensorBoxComponent sComp = nodeBehaviour.entity.GetComp<SensorBoxComponent>();
			uint curSensorBoxId = goComp.Uid;

			//感应盒锁控制的物体关联感应盒本身
			foreach (var controlType in controlTypes)
			{
				var ctrUids = GetCtrListFromSensorComp(sComp, controlType);
				if (ctrUids == null || ctrUids.Count == 0)
				{
					continue;
				}
				foreach (var uid in ctrUids)
				{
					var ctrProNodeBehav = GamePropNodeManager.Inst.GetBehaviourById(uid);
					if (ctrProNodeBehav != null && ctrProNodeBehav.entity != null)
					{
						SensorCtrController.Inst.BindEntity(ctrProNodeBehav.entity, curSensorBoxId, controlType);
					}
				}
			}
		}

		private void EnterPlayMode()
		{
			foreach (var sensorBox in sensorBoxDict.Values)
			{
				sensorBox.SetBoxVisiable(false);
			}
		}

		private void EnterEditMode()
		{
			foreach (var sensorBox in sensorBoxDict.Values)
			{
				sensorBox.SetBoxVisiable(true);
				sensorBox.SensorStatus = 0;
				sensorBox.UsedTimes = 0;
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
			NetSyncManager.Inst.AddBroadcastListener(SubCmdType.SensorBox,OnRecvServer);
			NetSyncManager.Inst.AddMapInfoListener(SubCmdType.SensorBox,OnMapInfoSync);
		}

		public override void OnPlay()
		{
			base.OnPlay();
			EnterPlayMode();
		}

        protected override void OnNotifyRelease()
        {
            base.OnNotifyRelease();
			NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.SensorBox,OnRecvServer);
			NetSyncManager.Inst.RemoveMapInfoListener(SubCmdType.SensorBox,OnMapInfoSync);

        }

        public void SendRequest(SensorBoxBehaviour behaviour)
		{
			SensorBoxNetData netData = new SensorBoxNetData();
			SensorBoxComponent sensorBoxComp= behaviour.entity.GetComp<SensorBoxComponent>();
            netData.Uid = behaviour.entity.GetComp<GameObjectComponent>().Uid;
            netData.Status = behaviour.SensorStatus;
			netData.Times = sensorBoxComp.BoxTimes;
            NetSyncManager.Inst.Send(SubCmdType.SensorBox,netData);
		}

		private void OnRecvServer(CommonSyncClientData netData)
		{
			SensorBoxNetData sensorNetData = (SensorBoxNetData)netData.Body;
			LoggerUtils.Log($"{TAG} OnReceiveServer==> Uid:{sensorNetData.Uid} , Status:{sensorNetData.Status}");
			if(!sensorBoxDict.ContainsKey(sensorNetData.Uid))
			{
				LoggerUtils.Log($"{TAG} OnReceiveServer 找不到该感应盒：{sensorNetData.Uid}");
				return;
			}
			SensorBoxBehaviour sensorBehaviour = sensorBoxDict[sensorNetData.Uid];
			if( sensorNetData.Status == sensorBehaviour.SensorStatus)
			{
				LoggerUtils.Log("感应盒 OnReceiveServer 出现两次一样的值");
				return;
			}
			sensorBehaviour.SensorStatus = sensorNetData.Status;
            HandleSensorBoxTouch(sensorBehaviour);
		}

		private void OnMapInfoSync(string mapId,List<IMessage> propDatas)
		{
			for (int i = 0; i < propDatas.Count; i++)
			{
				SensorBoxNetData sensorNetData = (SensorBoxNetData)propDatas[i];
				if(!sensorBoxDict.ContainsKey(sensorNetData.Uid)) continue;
				SensorBoxBehaviour sensorBehaviour = sensorBoxDict[sensorNetData.Uid];
				if(sensorBehaviour.SensorStatus != sensorNetData.Status)
				{
					HandleSensorBoxTouch(sensorBehaviour);
					sensorBehaviour.SensorStatus = sensorNetData.Status;
					sensorBehaviour.UsedTimes = sensorNetData.UseTimes;
				}
			}
		}

	}
}

