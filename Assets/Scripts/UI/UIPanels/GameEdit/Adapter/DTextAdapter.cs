using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using UnityEngine;
using Game.ECS;

namespace UI.UIPanels.GameEdit
{
    public class DTextAdapter : BasePropertyAdapter
    {
        InputTextSubView inputTextSubView;
        GameColorEditSubView colorEditSubView;
        DTextBehaviour bev;
        DTextComponent dTextComponent;

        protected override void OnCreate()
        {
            inputTextSubView = this.AddTabView<InputTextSubView>("文本");
            colorEditSubView = this.AddColorSubView();

            inputTextSubView.OnTextChanged = OnTextChange;
            colorEditSubView.AddColorChangeListener(OnColorChange);
        }
        protected override void OnSelectEntity()
        {
            var go = selectEntity.GetViewGo();
            dTextComponent = selectEntity.GetComp<DTextComponent>();
            bev = go.GetComponent<DTextBehaviour>();
            
            if (dTextComponent != null)
            {
                colorEditSubView.SetColorWithNoNotify(dTextComponent.TextColor);
                inputTextSubView.SetText(dTextComponent.Content);
            }
        }

        void OnTextChange(string str)
        {
            if(!this || bev == null || dTextComponent == null) return;
            bev.SetText(str);
            dTextComponent.Content = str; 
        }
        
        void OnColorChange(Color color)
        {
            bev.SetColor(color);
            dTextComponent.TextColor = color;
        }
    }
}