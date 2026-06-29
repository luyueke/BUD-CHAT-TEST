using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using UnityEngine;

namespace UI.UIPanels.GameEdit {
    public class PreviewModelViewAdapter : BasePropertyAdapter {



        private GameColorEditSubView colorSubView;
        private PreviewModelBehaviour previewBehaviour;
        private PreviewModelComponent previewComponent;


        protected override void OnCreate() {

            colorSubView = AddColorSubView();
            colorSubView.AddColorChangeListener(OnColorChange);

        }

        private void OnColorChange(Color newColor) {
            previewComponent.color = newColor;
            previewBehaviour.RefreshColor();

        }

        protected override void OnSelectEntity() {

            var go = selectEntity.GetViewGo();
            previewBehaviour = go.GetComponent<PreviewModelBehaviour>();
            previewComponent = selectEntity.GetOrAddComp<PreviewModelComponent>();
            if (previewComponent != null)
            {
                colorSubView.SetColorWithNoNotify(previewComponent.color);
            }
        }
    }
}
