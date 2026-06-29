using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
	public class PGCStoneViewAdapter : BasePropertyAdapter
	{
		PgcListEditSubView iconSubView;
        GameColorEditSubView colorSubView;
        PGCStoneBehaviour curBehaviour;
        PGCStoneComponent stoneComponent;
        
        private bool IsUseDefaultColor = true;

		protected override void OnCreate()
		{
			iconSubView = AddTabView<PgcListEditSubView>("种类");
			iconSubView.AddItemSelectListener(OnItemSelect);
			iconSubView.AddUndoSelectListener(OnUndoSelect);

			var stoneConfig = GlobalNodeManager.Inst.Get<PGCStoneManager>().PGCStoneConfigs;
			iconSubView.InitConfig(stoneConfig);
			
            colorSubView = AddColorSubView();
            colorSubView.AddColorChangeListener(OnColorChange);

		}

        protected override void OnSelectEntity()
        {
            var go = selectEntity.GetViewGo();
            stoneComponent = selectEntity.GetComp<PGCStoneComponent>();
            curBehaviour = go.GetComponent<PGCStoneBehaviour>();
            
            var gameComp = selectEntity.GetComp<GameObjectComponent>();

            if (stoneComponent == null) return;
            
	        iconSubView.SelectItemWithNoNotify(gameComp.PropId);
	        colorSubView.SetColorWithNoNotify(stoneComponent.Color);
	        
	        //是否使用了默认颜色，决定后续切换模型是否要使用模型默认颜色
	        var config = GlobalNodeManager.Inst.Get<PGCStoneManager>().GetConfigDataById(gameComp.PropId);
	        var colorStr = FormatUtils.ColorToString(stoneComponent.Color);
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
	        stoneComponent.Color = color;
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
	        GlobalNodeManager.Inst.Get<PGCStoneManager>().lastChooseID = propId;
	        var config = GlobalNodeManager.Inst.Get<PGCStoneManager>().GetConfigDataById(propId);
	        GlobalNodeManager.Inst.Get<PGCStoneManager>().UpdateAssetObj(curBehaviour,propId);
	        Color curColor = stoneComponent.Color;
	        if (IsUseDefaultColor)
	        {
		        curColor = FormatUtils.StringToColor(config.defColor);
	        }
	        SetColor(curColor);
	        colorSubView.SetColorWithNoNotify(curColor);
        }
        
	}
}