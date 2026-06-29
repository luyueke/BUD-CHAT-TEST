
using Es;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(TerrainBehaviour))]
	public class TerrainManager : BaseNodeManager
	{
		public NodeBaseBehaviour GetTerrain()
		{
			// 默认一定有一个地板
			return entities[0];
		}

        public TerrainComponent GetTerrainComponent() {
            return entities[0].entity.GetComp<TerrainComponent>();
        }


        protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{
			InitComponent(nodeBehaviour);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyCreateInBuild(nodeBehaviour);
			InitComponent(nodeBehaviour);
		}

		void InitComponent(NodeBaseBehaviour nodeBehaviour)
		{
			MaterialComponent materialComponent = null;
			TerrainComponent terrainComponent = null;
			if (!nodeBehaviour.entity.TryGetComp<TerrainComponent>(out terrainComponent))
			{
				terrainComponent = nodeBehaviour.entity.AddComp<TerrainComponent>();
			}
			if (!nodeBehaviour.entity.TryGetComp<MaterialComponent>(out materialComponent))
			{
				materialComponent = nodeBehaviour.entity.AddComp<MaterialComponent>();
				materialComponent.matId = new GameData.MaterialUnionID(8); // 默认材质的ID
			}

			var behav = nodeBehaviour as TerrainBehaviour;

            var mapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            if (mapInfo != null && mapInfo.migrateData == 1 && !terrainComponent.isMigrated) {
                terrainComponent.isMigrated = true;
                if (!materialComponent.matId.IsUGC) {
                    var groundMatConfig =  DataTables.GetGroundDataConfig(materialComponent.matId.MatId);
                    if (groundMatConfig == null) {
                        materialComponent.matId = new GameData.MaterialUnionID(8); // 默认材质的ID
                    } else {
                        materialComponent.matId = new MaterialUnionID(groundMatConfig.MatId);
                    }
                }
            }

			behav.SetMaterial(materialComponent.matId);
			behav.SetColor(materialComponent.color);
			behav.HideTerrain(!terrainComponent.IsVisible);
            behav.SetSize(terrainComponent.size);


		}
	}
}

