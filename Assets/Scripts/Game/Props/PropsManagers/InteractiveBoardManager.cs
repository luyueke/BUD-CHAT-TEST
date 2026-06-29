
using System.Collections.Generic;
using Game.Avatar;
using Game.Base;
using Game.ECS;
using Game.KinematicCharacter;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Scene.ModeController;
using GameData.GameSync;
using GameData.PgcData;
using Google.Protobuf;
using Message;
using NetEngine;
using Pb.Base;
using Pb.Game;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(InteractiveBoardBehaviour))]
	public class InteractiveBoardManager : BaseNodeManager
	{

		private string TAG = "InteractiveBoardManager";
		private Dictionary<Transform, Transform> curBoardDict = new Dictionary<Transform, Transform>();//当前与玩家绑定的板子
		private Dictionary<string, uint> dataDict = new Dictionary<string, uint>();//服务器下发与玩家绑定板子



		KinematicCharacterController player
		{
			get
			{
				if (AvatarController.Inst != null)
				{
					return AvatarController.Inst.SelfController;
				}

				return null;
			}
		}

		public InteractiveBoardManager()
		{
			MessageHelper.AddListener<string>(MessageName.PlayerEnter, OnPlayerEnter);
			MessageHelper.AddListener<string>(MessageName.PlayerLeave, OnPlayerLeave);
			MessageHelper.AddListener<uint,Transform>(MessageName.InteractiveOnBoard, OnBroadcastOnBoard);
			MessageHelper.AddListener<Transform>(MessageName.InteracriveDownBoard, OnBroadcastDownBoard);
			MessageHelper.AddListener<bool>(MessageName.InteracriveInterrupt, OnBroadcasInterrupt);

		}

		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{
			nodeBehaviour.entity.AddComp<InteractiveBoardComponent>();
		}

		protected override void OnNotifyRelease()
		{
			base.OnNotifyRelease();
			MessageHelper.RemoveListener<string>(MessageName.PlayerEnter, OnPlayerEnter);
			MessageHelper.RemoveListener<string>(MessageName.PlayerLeave, OnPlayerLeave);
			MessageHelper.RemoveListener<uint,Transform>(MessageName.InteractiveOnBoard, OnBroadcastOnBoard);
			MessageHelper.RemoveListener<Transform>(MessageName.InteracriveDownBoard, OnBroadcastDownBoard);
			MessageHelper.RemoveListener<bool>(MessageName.InteracriveInterrupt, OnBroadcasInterrupt);
		}

		public InteractiveBoardBehaviour GetBoardById(uint boardId)
		{
			InteractiveBoardBehaviour result = null;
			foreach (var nodeBaseBehaviour in entities)
			{
				if (nodeBaseBehaviour.entity.GetComp<GameObjectComponent>().Uid == boardId)
				{
					result = nodeBaseBehaviour as InteractiveBoardBehaviour;
					break;
				}
			}
			return result;
		}

		public InteractiveBoardBehaviour OnBoard(uint boardId, Transform transform)
		{
			if (transform != null)
			{
				InteractiveBoardBehaviour bv = GetBoardById(boardId);
				if (bv != null && !curBoardDict.ContainsValue(bv.carryTran))
				{
					curBoardDict.Add(transform, bv.carryTran);
					bv.OnBoardSuccess();
				}
				return bv;
			}
			return null;
		}

		public void DownBoard(Transform transform)
		{
			if (transform != null && curBoardDict.ContainsKey(transform))
			{
				InteractiveBoardBehaviour boardBehaviour = curBoardDict[transform].GetComponentInParent<InteractiveBoardBehaviour>();
				if (boardBehaviour != null)
				{
					boardBehaviour.DownBoardSuccess();
				}
				curBoardDict.Remove(transform);
			}
		}

		public void AddDownBoard()
		{
			foreach (var transform in curBoardDict.Keys)
			{
				InteractiveBoardBehaviour boardBehaviour = curBoardDict[transform].GetComponentInParent<InteractiveBoardBehaviour>();
				if (boardBehaviour != null)
				{
					boardBehaviour.DownBoardSuccess();
				}
			}
			curBoardDict.Clear();
		}

		public void PlayerDownBoardState(string playerId,bool isPlayExitAni = true)
		{
			var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(playerId);
			playerStateCtrl.ExitState(PlayerState.InteractiveBoard,isPlayExitAni);
		}

		public void PlayerOnBoardState(string playerId, uint boardId)
		{
			var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(playerId);
			if (playerStateCtrl == null)
			{
				LoggerUtils.LogError("InteractiveBoardManager PlayerOnBoardState:  playerStateCtrl == null");
				return;
			}

			InteractiveBoardBehaviour board = GetBoardById(boardId);
			var boardComp = board.entity.GetComp<InteractiveBoardComponent>();
            playerStateCtrl.EnterState(PlayerState.InteractiveBoard, boardId,board.transform, boardComp.EmoteId);
        }

		public override void OnEdit()
		{
			base.OnEdit();
			EnterEditMode();
		}

		public override void OnGuest()
		{
			base.OnGuest();
			NetSyncManager.Inst.AddBroadcastListener(SubCmdType.InteractPanel,OnRecvServer);
			NetSyncManager.Inst.AddMapInfoListener(SubCmdType.InteractPanel,OnMapInfoSync);
			EnterPlayMode();
		}

		public override void OnPlay()
		{
			base.OnPlay();
			EnterPlayMode();
		}

		private void EnterPlayMode()
		{
			foreach (var bev in entities)
			{
				var boardBehav = bev as InteractiveBoardBehaviour;
				if (boardBehav != null)
				{
					boardBehav.SetRenderEnable(false);
				}
			}
		}


		private void EnterEditMode()
		{
			foreach (var bev in entities)
			{
				var boardBehav = bev as InteractiveBoardBehaviour;
				if (boardBehav != null)
				{
					boardBehav.SetRenderEnable(true);
				}
			}
		}


		public void OnPlayerForceDownBoard(bool isTrap = false)
		{
			if (player == null)
			{
				return;
			}
			if (curBoardDict.ContainsKey(player.transform))
			{
				InteractiveBoardBehaviour bv = curBoardDict[player.transform].GetComponentInParent<InteractiveBoardBehaviour>();
				uint uid = bv.entity.GetComp<GameObjectComponent>().Uid;
				PlayerDownBoardState(Player.Id,false);
				if (this.IsGuest())
				{
					SendUnBindBoard(uid);
				}
			}
		}
		private void OnPlayerLeaveRoom(string id)
		{
			var otherCtr = AvatarController.Inst.GetPlayerStateCtrl(id);
			if (otherCtr != null)
			{
				DownBoard(otherCtr.transform);
				if (dataDict.ContainsKey(id))
				{
					dataDict.Remove(id);
				}
			}
		}


		public void OnBoardDisable(Transform carry, uint uid)
		{
			if (curBoardDict != null && player != null)
			{
				if (curBoardDict.ContainsKey(player.transform) && curBoardDict[player.transform] == carry)
				{
					PlayerDownBoardState(Player.Id);
					if (this.IsGuest())
					{
						SendUnBindBoard(uid);
					}
				}
			}
		}


		#region  联机部分

		private void OnPlayerEnter(string playerId)
		{
			if (dataDict.ContainsKey(playerId))
			{
				PlayerOnBoardState(playerId, dataDict[playerId]);
			}
			else
			{
				PlayerDownBoardState(playerId);
			}
		}


		private void OnPlayerLeave(string playerId)
		{
			var otherCtr = AvatarController.Inst.GetPlayerStateCtrl(playerId);
			if (otherCtr != null)
			{
				DownBoard(otherCtr.transform);
				if (dataDict.ContainsKey(playerId))
				{
					dataDict.Remove(playerId);
				}
			}
		}

		private void OnBroadcastOnBoard(uint boardId,Transform playerTransform)
		{
			DownBoard(playerTransform);
			OnBoard(boardId,playerTransform);
		}

		private void OnBroadcastDownBoard(Transform playerTransform)
		{
			DownBoard(playerTransform);
		}

		/// <summary>
		/// 是否只有Guest模式下
		/// </summary>
		/// <param name="isOnlyGuest"></param>
		private void OnBroadcasInterrupt(bool isOnlyGuest)
		{
			if (isOnlyGuest && !this.IsGuest())
			{
				return;
			}

			PlayerSendDownBoard();
		}


		public void PlayerSendOnBoard(InteractiveBoardBehaviour boardBehaviour)
		{
			if (this.IsGuest())
			{
				if (entities.Contains(boardBehaviour))
				{
					SendBindBoard(boardBehaviour);
				}
			}
			else
			{
				uint boardId = boardBehaviour.entity.GetComp<GameObjectComponent>().Uid;
				PlayerOnBoardState(Player.Id, boardId);
			}
		}

		public void PlayerSendDownBoard()
		{
			if (this.IsGuest())
			{
				if (curBoardDict.ContainsKey(player.transform))
				{
					SendUnBindBoard(curBoardDict[player.transform].GetComponentInParent<InteractiveBoardBehaviour>());
				}
			}
			else
			{
				PlayerDownBoardState(Player.Id);
			}
		}
		private void SendBindBoard(InteractiveBoardBehaviour bv)
		{
			SendBoard(bv.entity.GetComp<GameObjectComponent>().Uid, 1);
		}
		private void SendUnBindBoard(InteractiveBoardBehaviour bv)
		{
			SendBoard(bv.entity.GetComp<GameObjectComponent>().Uid, 0);
		}
		private void SendUnBindBoard(uint uid)
		{
			SendBoard(uid, 0);
		}

		private void SetBoard(InteractPanelNetData syncData,bool isReconnect = false)
		{
			uint boardId = syncData.Uid;
			InteractiveBoardBehaviour bv = GetBoardById(boardId);
			if (bv != null)
			{
				bool isOnBoard = syncData.Status == 1;
				SetDataBoard(syncData, boardId);
				if (isOnBoard && !curBoardDict.ContainsValue(bv.carryTran))//上板子
				{
					if(isReconnect)
					{
						var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(syncData.PlayerId);
						playerStateCtrl.ReconnectIntoState(PlayerState.InteractiveBoard, boardId);
					}
					else
					{
						PlayerOnBoardState(syncData.PlayerId, boardId);
					}

				}
				else if (!isOnBoard && curBoardDict.ContainsValue(bv.carryTran))//下板子
				{
					PlayerDownBoardState(syncData.PlayerId);
				}
			}
		}

		public void SetDataBoard(InteractPanelNetData data, uint boardId)
		{
			if (data.Status == 1 && !dataDict.ContainsKey(data.PlayerId))//上板子
			{
				dataDict.Add(data.PlayerId, boardId);
			}
			else if (data.Status == 0 && dataDict.ContainsKey(data.PlayerId))//下板子
			{
				dataDict.Remove(data.PlayerId);
			}
		}

		private void SendBoard(uint uid, int isOnBoard)
		{
			InteractPanelNetData netData = new InteractPanelNetData
			{
				Status = isOnBoard,
				PlayerId = Player.Id,
				Uid = uid
			};
			NetSyncManager.Inst.Send(SubCmdType.InteractPanel,netData);
		}

		private void OnRecvServer(CommonSyncClientData netData)
		{
			InteractPanelNetData boardNetData = (InteractPanelNetData)netData.Body;
			LoggerUtils.Log($"{TAG} OnReceiveServer==> Uid:{boardNetData.Uid} , Status:{boardNetData.Status}");
			SetBoard(boardNetData);
		}

		private void OnMapInfoSync(string mapId,List<IMessage> propDatas)
		{
			LoggerUtils.Log($"{TAG} OnMapInfoSync==> propDatas.Count:{propDatas.Count}");
			for (int i = 0; i < propDatas.Count; i++)
			{
				InteractPanelNetData boardNetData = (InteractPanelNetData)propDatas[i];
				SetBoard(boardNetData);
			}
		}

		#endregion
	}
}

