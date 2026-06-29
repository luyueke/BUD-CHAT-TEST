
using System.Collections.Generic;
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(TrapSpawnBehaviour))]
	public class TrapSpawnManager : BaseNodeManager
	{
		private Dictionary<uint, NodeBaseBehaviour> spawnPoints = new Dictionary<uint, NodeBaseBehaviour>();
		
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{
			var spawnComp = nodeBehaviour.entity.AddComp<TrapSpawnComponent>();
			AddTrapSpawnBehaviour(nodeBehaviour);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyCreateInBuild(nodeBehaviour);
			AddTrapSpawnBehaviour(nodeBehaviour);
			
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
			AddTrapSpawnBehaviour(newBehaviour);
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

		public override void OnEdit()
		{
			base.OnEdit();
			EnterEditMode();
		}

		private void EnterPlayMode()
		{
			foreach (var nodeBehv in spawnPoints.Values)
			{
				TrapSpawnBehaviour spawnBehaviour = nodeBehv as TrapSpawnBehaviour;
				if (spawnBehaviour != null)
				{
					spawnBehaviour.SetRenderEnable(false);
				}
			}
		}

		private void EnterEditMode()
		{
			foreach (var nodeBehv in spawnPoints.Values)
			{
				TrapSpawnBehaviour spawnBehaviour = nodeBehv as TrapSpawnBehaviour;
				if(spawnBehaviour == null) continue;
				spawnBehaviour.SetRenderEnable(true);
				
				//刷新一下id
				if ( spawnBehaviour.Index == 0)
				{
					var trapSpawnComp = spawnBehaviour.entity.GetComp<TrapSpawnComponent>();
					var boxBehav = GlobalNodeManager.Inst.Get<TrapBoxManager>().GetBoxByUid(trapSpawnComp.BoxId);
					if (boxBehav != null && boxBehav.entity.HasComp<TrapBoxComponent>())
					{
						spawnBehaviour.SetIndex(boxBehav.entity.GetComp<TrapBoxComponent>().BoxIndex);
					}
				}
				
			}
		}

		private void AddTrapSpawnBehaviour(NodeBaseBehaviour nodeBehaviour)
		{
			uint uid = nodeBehaviour.entity.GetComp<GameObjectComponent>().Uid;
			AddTrapSpawn(uid,nodeBehaviour);
		}

		private void RemoveTrapSpawnBehaviour(NodeBaseBehaviour nodeBehaviour)
		{
			uint uid = nodeBehaviour.entity.GetComp<GameObjectComponent>().Uid;
			RemoveTrapSpawn(uid);
		}

		public void AddTrapSpawn(uint uid,NodeBaseBehaviour spawnBehav)
		{
			if (!spawnPoints.ContainsKey(uid))
			{
				spawnPoints.Add(uid,spawnBehav);
			}
		}

		public void RemoveTrapSpawn(uint uid)
		{
			if (spawnPoints.ContainsKey(uid))
			{
				spawnPoints.Remove(uid);
			}
		}
		
		public NodeBaseBehaviour GetSpawnByUid(uint uid)
		{
			if (spawnPoints.ContainsKey(uid))
			{
				return spawnPoints[uid];
			}
			return null;
		}


		public void CheckRemoveTrapBox(NodeBaseBehaviour boxBehaviour)
		{
			if (boxBehaviour == null || !boxBehaviour.entity.HasComp<TrapBoxComponent>())
			{
				return;
			}
			
			TrapBoxComponent trapBoxComponent = boxBehaviour.entity.GetComp<TrapBoxComponent>();

			if (trapBoxComponent.PointId == 0)
			{
				return;
			}

			var spawnBehav = GetSpawnByUid(trapBoxComponent.PointId);
			if (spawnBehav != null)
			{
				spawnBehav.transform.SetParent(boxBehaviour.transform);
				RemoveTrapSpawnBehaviour(spawnBehav);
			}

		}


		public void CheckRevertTrapBox(NodeBaseBehaviour boxBehaviour)
		{
			if (boxBehaviour == null || !boxBehaviour.entity.HasComp<TrapBoxComponent>())
			{
				return;
			}
			
			var spawnBehav = boxBehaviour.GetComponentInChildren<TrapSpawnBehaviour>(true);
			if (spawnBehav != null)
			{
				spawnBehav.transform.SetParent(boxBehaviour.transform.parent);
				AddTrapSpawnBehaviour(spawnBehav);
			}
		}
	}
}
        
