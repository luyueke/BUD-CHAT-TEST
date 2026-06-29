
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Scene.ModeController;
using GameData.Manager;
using GameData.BaseInfo;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(SpawnPointBehaviour))]
	public class SpawnPointManager : BaseNodeManager,INodeEdit,IModeManager
	{

		private int playerSpawnId = -1;

		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{
			nodeBehaviour.entity.AddComp<SpawnPointComponent>();
			SetMaxPlayer();
		}

		protected override void OnNotifyRemove(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyRemove(nodeBehaviour);
			var behv = nodeBehaviour as SpawnPointBehaviour;
			behv.SetEffectActive(false);
			UpdateSpawnPointOrder(behv.Index,behv.IsDefault);
			SetMaxPlayer();
		}

        public Vector3 GetDefaultSpawnPoint()
		{
			for (var i = 0; i < entities.Count; i++)
			{
				var behv = entities[i] as SpawnPointBehaviour;
				if (behv.IsDefault)
				{
					return behv.transform.position;
				}
			}

			if (entities.Count > 0)
			{
				var behv = entities[0] as SpawnPointBehaviour;
				return behv.transform.position;
			}

			Debug.LogError("GetDefaultSpawnPoint  Fail");
			return Vector3.zero;
		}

        public Quaternion GetDefaultSpawnRotation()
        {
            for (var i = 0; i < entities.Count; i++)
            {
                var behv = entities[i] as SpawnPointBehaviour;
                if (behv.IsDefault)
                {
                    return behv.transform.rotation;
                }
            }

            if (entities.Count > 0)
            {
	            var behv = entities[0] as SpawnPointBehaviour;
	            return behv.transform.rotation;
            }

            Debug.LogError("GetDefaultSpawnPoint  Fail");
            return Quaternion.identity;
        }

        /// <summary>
        /// 随机获取一个出生点
        /// </summary>
        /// <returns></returns>
        public SpawnPointBehaviour GetRandomSpawnPoint()
        {
	        if (playerSpawnId == -1)
	        {
		        playerSpawnId = Random.Range(0, entities.Count - 1);
	        }
	        return entities[playerSpawnId] as SpawnPointBehaviour;
        }

        public void SetSpawnPoint(int spawnId)
        {
	        playerSpawnId = spawnId;
        }



        public void UpdateSpawnPointDefaultState()
		{
			for (var i = 0; i < entities.Count; i++)
			{
				var behv = entities[i] as SpawnPointBehaviour;
				var comp = behv.entity.GetComp<SpawnPointComponent>();
				comp.SpawnDefault = 0;
				behv.SetDefault(comp.SpawnDefault);
			}
		}

		public int GetLastIndex()
		{
			return entities.Count;
		}

		public List<GameObject> GetSpawnPointByLast(int count)
		{
			entities.Sort((x, y) =>
			{
				var xbehv = x as SpawnPointBehaviour;
				var ybehv = y as SpawnPointBehaviour;
				return xbehv.Index.CompareTo(ybehv.Index);
			});

			var removeEntitys = entities.GetRange(entities.Count - count, count);
			return removeEntitys.Select(x => x.gameObject).ToList();
		}

		public void UpdateSpawnPointOrder(int index,bool changeDefault)
		{
			for (var i = 0; i < entities.Count; i++)
			{
				var behv = entities[i] as SpawnPointBehaviour;
				var comp = behv.entity.GetComp<SpawnPointComponent>();
				if (comp.SpawnIndex >= index)
				{
					comp.SpawnIndex--;
					behv.SetIndex(comp.SpawnIndex);
				}

				if (changeDefault && comp.SpawnIndex == 1)
				{
					comp.SpawnDefault = 1;
					behv.SetDefault(comp.SpawnDefault);
				}
			}
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			var behv = newBehaviour as SpawnPointBehaviour;
			var comp = behv.entity.GetComp<SpawnPointComponent>();
			comp.SpawnIndex = entities.Count + 1;
			comp.SpawnDefault = 0;
			behv.SetIndex(comp.SpawnIndex);
			behv.SetDefault(comp.SpawnDefault);
			SetMaxPlayer();
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			if (nodeBehaviour.entity.HasComp<SpawnPointComponent>())
			{
				var comp = nodeBehaviour.entity.GetComp<SpawnPointComponent>();
				var behv = nodeBehaviour as SpawnPointBehaviour;
				behv.SetIndex(comp.SpawnIndex);
				behv.SetDefault(comp.SpawnDefault);
			}
		}

		public void OnSelectProp(SceneEntity entity)
		{
			var behv = entity.GetNodeBaseBehaviour() as SpawnPointBehaviour;
			behv.SetEffectActive(true);
		}

		public void OnUnSelectProp(SceneEntity entity)
		{
			var behv = entity.GetNodeBaseBehaviour() as SpawnPointBehaviour;
			behv.SetEffectActive(false);
		}


		public override void OnEdit()
		{
            if (this.IsPropEdit()) {
                return;
            }
			base.OnEdit();
			entities.ForEach(x=>x.gameObject.SetActive(true));
		}

		public override void OnPlay()
		{
			base.OnEdit();
			entities.ForEach(x=>x.gameObject.SetActive(false));
		}

		public override void OnGuest()
		{
			base.OnEdit();
			entities.ForEach(x=>x.gameObject.SetActive(false));
		}

		public void SetMaxPlayer()
		{
			var mapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
			if (mapInfo != null)
			{
				mapInfo.gameSetting.maxPlayer = GetLastIndex();
			}
		}

		protected override void OnNotifyRelease()
		{
			base.OnNotifyRelease();
			playerSpawnId = -1;
		}

        public void OnDragMoveUpdate(GameObject target)
		{
            // 出生点不允许放入地下
			var spawnPointBehaviour = target.GetComponentInChildren<SpawnPointBehaviour>();
            if (spawnPointBehaviour != null)
            {
                var pos = spawnPointBehaviour.transform.position;
                if (pos.y < 0)
                {
                    pos.y = 0;
                    spawnPointBehaviour.transform.position = pos;
                }
            }

		}




	}
}

