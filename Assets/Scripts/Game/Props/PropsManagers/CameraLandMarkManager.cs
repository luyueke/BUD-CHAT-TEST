
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData.Manager;
using System.Collections.Generic;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(CameraLandMarkBehaviour))]
	public class CameraLandMarkManager : BaseNodeManager
	{
		private readonly Dictionary<uint, CameraLandMarkBehaviour> _behaviourDict = new Dictionary<uint, CameraLandMarkBehaviour>();
		private readonly List<uint> _enteredUidList = new List<uint>();

		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<CameraLandMarkComponent>();
			RegisterBehaviour(nodeBehaviour as CameraLandMarkBehaviour);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			RegisterBehaviour(nodeBehaviour as CameraLandMarkBehaviour);
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
			RegisterBehaviour(newBehaviour as CameraLandMarkBehaviour);
		}

		protected override void OnNotifyRevert(NodeBaseBehaviour nodeBehaviour)
		{
			RegisterBehaviour(nodeBehaviour as CameraLandMarkBehaviour);
		}

		protected override void OnNotifyRemove(NodeBaseBehaviour nodeBehaviour)
		{
			UnregisterBehaviour(nodeBehaviour as CameraLandMarkBehaviour);
		}

		protected override void OnNotifyRelease()
		{
			base.OnNotifyRelease();
			_behaviourDict.Clear();
			_enteredUidList.Clear();
		}

		public override void OnEdit()
		{
			base.OnEdit();
			_enteredUidList.Clear();
			EnterEditMode();
		}

		public override void OnPlay()
		{
			base.OnPlay();
			_enteredUidList.Clear();
			EnterPlayMode();
		}

		public override void OnGuest()
		{
			base.OnGuest();
			_enteredUidList.Clear();
			EnterPlayMode();
		}

		public void OnLandMarkTrigEnter(CameraLandMarkBehaviour behaviour)
		{
			if (behaviour == null || behaviour.entity == null) return;
			RegisterBehaviour(behaviour);
			var uid = GetUid(behaviour);
			if (uid == 0) return;

			_enteredUidList.Remove(uid);
			_enteredUidList.Add(uid);
		}

		public void OnLandMarkTrigExit(CameraLandMarkBehaviour behaviour)
		{
			if (behaviour == null) return;
			var uid = GetUid(behaviour);
			if (uid == 0) return;
			_enteredUidList.Remove(uid);
		}

		public bool TryGetCurrentLocation(out string locationName, out string mapId, out bool isLandMark)
		{
			locationName = null;
			mapId = GetCurrentMapId();
			if (_enteredUidList.Count <= 0)
			{
				locationName = GetCurrentMapName();
				isLandMark = false;
				return !string.IsNullOrEmpty(locationName) || !string.IsNullOrEmpty(mapId);
			}

			var uid = _enteredUidList[_enteredUidList.Count - 1];
			if (_behaviourDict.TryGetValue(uid, out var behaviour) && behaviour != null && behaviour.entity != null)
			{
				var comp = behaviour.entity.GetComp<CameraLandMarkComponent>();
				locationName = comp?.ShowText;
			}

			if (string.IsNullOrEmpty(locationName))
			{
				locationName = GetCurrentMapName();
			}

			isLandMark = true;
			return !string.IsNullOrEmpty(locationName) || !string.IsNullOrEmpty(mapId);
		}

		private void RegisterBehaviour(CameraLandMarkBehaviour behaviour)
		{
			if (behaviour == null || behaviour.entity == null) return;
			var uid = GetUid(behaviour);
			if (uid == 0) return;
			_behaviourDict[uid] = behaviour;
		}

		private void UnregisterBehaviour(CameraLandMarkBehaviour behaviour)
		{
			if (behaviour == null) return;
			var uid = GetUid(behaviour);
			if (uid == 0) return;
			_behaviourDict.Remove(uid);
			_enteredUidList.Remove(uid);
		}

		private static uint GetUid(CameraLandMarkBehaviour behaviour)
		{
			return behaviour?.entity?.GetComp<GameObjectComponent>()?.Uid ?? 0;
		}

		private static string GetCurrentMapId()
		{
			return GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.id;
		}

		private static string GetCurrentMapName()
		{
			return GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.name;
		}

		private void EnterPlayMode()
		{
			foreach (var behaviour in _behaviourDict.Values)
			{
				behaviour.SetBoxVisiable(false);
			}
		}

		private void EnterEditMode()
		{
			foreach (var behaviour in _behaviourDict.Values)
			{
				behaviour.SetBoxVisiable(true);
			}
		}
	}
}
        
