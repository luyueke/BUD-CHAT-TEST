using Es;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using GameData;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
	public class TerrainViewAdapter : BasePropertyAdapter
	{
        GameColorEditSubView colorSubView;
        GameMatEditSubView matSubView;
        TerrainSubView terrainSubView;
        TerrainBehaviour bev;
        MaterialComponent matComponent;

		protected override void OnCreate()
		{
            matSubView = AddMatSubView();
			colorSubView = AddColorSubView();
            terrainSubView = AddTabView<TerrainSubView>("可见性");

            matSubView.HideTileSizeOption();
            matSubView.AddMatChangeListener(OnMatChange);
            colorSubView.AddColorChangeListener(OnColorChange);
		}

        protected override void OnSelectEntity()
        {
            var manager = GlobalNodeManager.Inst.Get<TerrainManager>();
            var terrain = manager.GetTerrain();
            matComponent = terrain.entity.GetComp<MaterialComponent>();
            bev = terrain.GetComponent<TerrainBehaviour>();
            
            if (matComponent != null)
            {
                colorSubView.SetColorWithNoNotify(matComponent.color);
                matSubView.SetMaterialWithNoNotify(matComponent.matId);
            }
        }

        void OnMatChange(GameMatUIData matData)
        {
            bev.SetMaterial(matData.Id);
            matComponent.matId = matData.Id;
        }

        void OnColorChange(Color color)
        {
            bev.SetColor(color);
            matComponent.color = color;
        }
	}
}