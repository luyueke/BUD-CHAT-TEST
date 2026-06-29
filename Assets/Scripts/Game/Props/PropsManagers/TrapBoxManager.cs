
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Utils;
using GameData.Config;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(TrapBoxBehaviour))]
	public class TrapBoxManager : BaseNodeManager
	{
		private int CurrentNum = 0;
		
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			var boxComp = nodeBehaviour.entity.AddComp<TrapBoxComponent>();
			boxComp.BoxIndex = GetNewIndex();
			TrapBoxBehaviour boxBehav = nodeBehaviour as TrapBoxBehaviour;
			boxBehav.RefreshShowId();
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyCreateInBuild(nodeBehaviour);
			TrapBoxComponent boxComp = nodeBehaviour.entity.GetComp<TrapBoxComponent>();
			UpdateMaxIndex(boxComp.BoxIndex);
			TrapBoxBehaviour boxBehav = nodeBehaviour as TrapBoxBehaviour;
			boxBehav.RefreshShowId();
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
			TrapBoxBehaviour boxBehav = newBehaviour as TrapBoxBehaviour;
			boxBehav.RefreshShowId();
			
			var oldComp = oldBehaviour.entity.GetComp<TrapBoxComponent>();
			if (oldComp != null && oldComp.PointId != 0)
			{
				var oPoint = GlobalNodeManager.Inst.Get<TrapSpawnManager>().GetSpawnByUid(oldComp.PointId);
				if (oPoint != null)
				{
					CreateTrapSpawn(newBehaviour as TrapBoxBehaviour);
				}

			}

		}

		protected override void OnNotifyRemove(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyRemove(nodeBehaviour);
			GlobalNodeManager.Inst.Get<TrapSpawnManager>().CheckRemoveTrapBox(nodeBehaviour);
		}


		protected override void OnNotifyRevert(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyRevert(nodeBehaviour);
			GlobalNodeManager.Inst.Get<TrapSpawnManager>().CheckRevertTrapBox(nodeBehaviour);
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
		}

		public override void OnPlay()
		{
			base.OnPlay();
			EnterPlayMode();
		}

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

		public TrapBoxBehaviour GetBoxByUid(uint uid)
		{
			foreach (var nodeBaseBehaviour in entities)
			{
				var gameComp = nodeBaseBehaviour.entity.GetComp<GameObjectComponent>();
				if (gameComp.Uid == uid)
				{
					return nodeBaseBehaviour as TrapBoxBehaviour;
				}
			}

			return null;
		}


		/// <summary>
		/// 创建一个自定义复活点
		/// </summary>
		public NodeBaseBehaviour CreateTrapSpawn(TrapBoxBehaviour trapBoxBehav)
		{
			if (trapBoxBehav == null) return null;
			var pointData = GamePropDataHelper.GetPropIdByNodeModelType(NodeModelType.TrapSpawn);
			NodeBaseBehaviour pointBehv;
			GamePropNodeManager.Inst.TryCreateInEdit(pointData.Id, out pointBehv);
			TrapSpawnBehaviour spawnBehav = pointBehv as TrapSpawnBehaviour;
			if (spawnBehav != null)
			{
				var trapBoxComp = trapBoxBehav.entity.GetComp<TrapBoxComponent>();
				var goComponent = pointBehv.entity.GetGameObjectComponent();
				trapBoxComp.PointId = goComponent.Uid;
				pointBehv.transform.position = trapBoxBehav.transform.position + new Vector3(2, 0, 0);
				spawnBehav.SetIndex(trapBoxComp.BoxIndex);

				TrapSpawnComponent spawnComponent = spawnBehav.entity.GetOrAddComp<TrapSpawnComponent>();
				spawnComponent.BoxId = trapBoxBehav.entity.GetComp<GameObjectComponent>().Uid;
			}

			return pointBehv;
		}

		public void DestroyTrapSpawn(TrapBoxBehaviour trapBoxBehav)
		{
			var boxComp = trapBoxBehav.entity.GetComp<TrapBoxComponent>();
			var spawnBehav = GlobalNodeManager.Inst.Get<TrapSpawnManager>().GetSpawnByUid(boxComp.PointId);
			if (spawnBehav != null)
			{
				var pointTarget = spawnBehav.entity.GetComp<GameObjectComponent>().BindGo;
				GlobalNodeManager.Inst.Get<TrapSpawnManager>().RemoveTrapSpawn(boxComp.PointId);
				// GamePropNodeManager.Inst.DestroyNodeToSecondCache(pointTarget);
				GamePropNodeManager.Inst.DestroyNode(pointTarget);
				boxComp.PointId = 0;
			}
		}


		public void DoHitTrap(TrapBoxBehaviour trapBoxBehaviour)
		{
			var trapBoxComp = trapBoxBehaviour.entity.GetComp<TrapBoxComponent>();
			if (trapBoxComp.HitState == 0)
			{
				return;
			}
			
			//TODO:伤害逻辑
			
		}
		
		private void EnterPlayMode()
		{
			foreach (var nodeBaseBehaviour in entities)
			{
				var boxBehaviour = nodeBaseBehaviour as TrapBoxBehaviour;
				if (boxBehaviour != null)
				{
					boxBehaviour.SetBoxVisiable(false);
					boxBehaviour.SetTextVisiable(false);
				}
			}
		}

		private void EnterEditMode()
		{
			foreach (var nodeBaseBehaviour in entities)
			{
				var boxBehaviour = nodeBaseBehaviour as TrapBoxBehaviour;
				if (boxBehaviour != null)
				{
					boxBehaviour.SetBoxVisiable(true);
					boxBehaviour.SetTextVisiable(true);
				}
			}
		}
	}
}
        
