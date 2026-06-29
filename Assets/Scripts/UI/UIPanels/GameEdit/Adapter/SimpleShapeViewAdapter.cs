using Es;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
	public class SimpleShapeViewAdapter : BasePropertyAdapter
	{
        GameColorEditSubView colorSubView;
        GameMatEditSubView matSubView;
        SimpleShapeBehaviour bev;
        MaterialComponent matComponent;

		protected override void OnCreate()
		{
            matSubView = AddMatSubView();
			colorSubView = AddColorSubView();

            matSubView.AddMatChangeListener(OnMatChange);
            matSubView.AddTilingChangeListener(OnTilingChange);
            colorSubView.AddColorChangeListener(OnColorChange);
		}

        protected override void OnSelectEntity()
        {
            var go = selectEntity.GetViewGo();
            matComponent = selectEntity.GetComp<MaterialComponent>();
            bev = go.GetComponent<SimpleShapeBehaviour>();
            
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

        void OnTilingChange(float v)
        {
            var newTile = matComponent.tile;
            newTile.x -= v;
            newTile.y -= v;
            if (newTile.x < 0) newTile.x = 0;
            if (newTile.y < 0) newTile.y = 0;
            bev.SetMaterialTiling(newTile);
            matComponent.tile = newTile;
        }
	}
}