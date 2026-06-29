using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
	public class PGCPlantViewAdapter : BasePropertyAdapter
	{
		PgcListEditSubView iconSubView;
        GameColorEditSubView colorSubView;
        PGCPlantBehaviour curBehaviour;
        PGCPlantComponent pgcComponent;
        
        private bool IsUseDefaultColor = true;

		protected override void OnCreate()
		{
			iconSubView = AddTabView<PgcListEditSubView>("种类");
			iconSubView.AddItemSelectListener(OnItemSelect);
			iconSubView.AddUndoSelectListener(OnUndoSelect);

			var configs = GlobalNodeManager.Inst.Get<PGCPlantManager>().PGCPlantConfigs;
			iconSubView.InitConfig(configs);
			
            colorSubView = AddColorSubView();
            colorSubView.AddColorChangeListener(OnColorChange);

		}

        protected override void OnSelectEntity()
        {
            var go = selectEntity.GetViewGo();
            pgcComponent = selectEntity.GetComp<PGCPlantComponent>();
            curBehaviour = go.GetComponent<PGCPlantBehaviour>();
            
            var gameComp = selectEntity.GetComp<GameObjectComponent>();

            if (pgcComponent == null) return;
            
	        iconSubView.SelectItemWithNoNotify(gameComp.PropId);
	        colorSubView.SetColorWithNoNotify(pgcComponent.Color);
	        
	        //是否使用了默认颜色，决定后续切换模型是否要使用模型默认颜色
	        var config = GlobalNodeManager.Inst.Get<PGCPlantManager>().GetConfigDataById(gameComp.PropId);
	        var colorStr = FormatUtils.ColorToString(pgcComponent.Color);
	        IsUseDefaultColor = (colorStr == config.defColor);
        }

        
        void OnColorChange(Color color)
        {
	        SetColor(color);
	        IsUseDefaultColor = false;
        }

        void SetColor(Color color)
        {
	        curBehaviour.SetColor(color);
	        pgcComponent.Color = color;
        }
        
        void OnItemSelect(string propId)
        {
	        LoggerUtils.Log("OnItemSelect: "+propId);
	        OnChangeAssetObj(propId);
        }
        
        void OnUndoSelect(string propId)
        {
	        OnChangeAssetObj(propId);
	        SelectTabItem(0);
        }

        private void OnChangeAssetObj(string propId)
        {
	        GlobalNodeManager.Inst.Get<PGCPlantManager>().lastChooseID = propId;
	        var config = GlobalNodeManager.Inst.Get<PGCPlantManager>().GetConfigDataById(propId);
	        GlobalNodeManager.Inst.Get<PGCPlantManager>().UpdateAssetObj(curBehaviour,propId);
	        Color curColor = pgcComponent.Color;
	        if (IsUseDefaultColor)
	        {
		        curColor = FormatUtils.StringToColor(config.defColor);
	        }
	        SetColor(curColor);
	        colorSubView.SetColorWithNoNotify(curColor);
        }
        
	}
}