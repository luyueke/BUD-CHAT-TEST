using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
	public class PGCEffectViewAdapter : BasePropertyAdapter
	{
		private PgcListEditSubView iconSubView;
		private GameColorEditSubView colorSubView;
		private PGCEffectBehaviour curBehaviour;
		private PGCEffectComponent pgcComponent;
		private PgcEffectSoundSubView soundSubView;
        
        private bool IsUseDefaultColor = true;

		protected override void OnCreate()
		{
			iconSubView = AddTabView<PgcListEditSubView>("种类");
			iconSubView.AddItemSelectListener(OnItemSelect);
			iconSubView.AddUndoSelectListener(OnUndoSelect);

			var configs = GlobalNodeManager.Inst.Get<PGCEffectManager>().PGCConfigs;
			iconSubView.InitConfig(configs);
			
            colorSubView = AddColorSubView();
            colorSubView.AddColorChangeListener(OnColorChange);

            soundSubView = AddTabView<PgcEffectSoundSubView>("设置");
            soundSubView.AddSoundChangeListener(OnSoundChange);

		}

        protected override void OnSelectEntity()
        {
            var go = selectEntity.GetViewGo();
            pgcComponent = selectEntity.GetComp<PGCEffectComponent>();
            curBehaviour = go.GetComponent<PGCEffectBehaviour>();
            
            var gameComp = selectEntity.GetComp<GameObjectComponent>();

            if (pgcComponent == null) return;
            
	        iconSubView.SelectItemWithNoNotify(gameComp.PropId);
	        colorSubView.SetColorWithNoNotify(pgcComponent.Color);
	        
	        //是否使用了默认颜色，决定后续切换模型是否要使用模型默认颜色
	        var config = GlobalNodeManager.Inst.Get<PGCEffectManager>().GetConfigDataById(gameComp.PropId);
	        var colorStr = FormatUtils.ColorToString(pgcComponent.Color);
	        IsUseDefaultColor = (colorStr == config.defColor);
	        
	        soundSubView.SetSoundWithoutNotify(pgcComponent.PlaySound);
	        soundSubView.SetToggleEnable(!string.IsNullOrEmpty(config.soundName));
	        
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

        void OnSoundChange(int playSound)
        {
	        pgcComponent.PlaySound = playSound;
	        
	        //调试代码
	        #if UNITY_EDITOR
	        curBehaviour.PlaySound(playSound == 1);
	        #endif
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
	        //调试代码
			#if UNITY_EDITOR
	        curBehaviour.PlaySound(false);
			#endif
	        
	        GlobalNodeManager.Inst.Get<PGCEffectManager>().lastChooseID = propId;
	        var config = GlobalNodeManager.Inst.Get<PGCEffectManager>().GetConfigDataById(propId);
	        GlobalNodeManager.Inst.Get<PGCEffectManager>().UpdateAssetObj(curBehaviour,propId);
	        Color curColor = pgcComponent.Color;
	        if (IsUseDefaultColor)
	        {
		        curColor = FormatUtils.StringToColor(config.defColor);
	        }
	        SetColor(curColor);
	        colorSubView.SetColorWithNoNotify(curColor);
	        
	        soundSubView.SetSoundWithoutNotify(pgcComponent.PlaySound);
	        // soundSubView.SetToggleEnable(!string.IsNullOrEmpty(config.soundName));
        }
        
	}
}