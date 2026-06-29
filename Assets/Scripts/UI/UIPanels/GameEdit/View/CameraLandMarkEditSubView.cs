using System.Collections;
using System.Collections.Generic;
using Game.AINPCStudio;
using Game.Base;
using Game.CommunityGame;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
namespace UI.UIPanels.GameEdit
{
    public class CameraLandMarkEditSubView : BasePropertyEditSubView
    {
        [SerializeField] private TextInputView InputText;
        private CameraLandMarkComponent _cameraLandMarkComponent;

        private CameraLandMarkBehaviour _cameraLandMarkBehaviour;

        protected override void OnInit()
        {
            InputText.SetOnInput(OnSetName);
        }

        public override void OnSelectEntity(SceneEntity entity)
        {
            base.OnSelectEntity(entity);
            _cameraLandMarkComponent = selectEntity.GetComp<CameraLandMarkComponent>();
            _cameraLandMarkBehaviour = selectEntity.GetBehaviour<CameraLandMarkBehaviour>();
            RefreshUI();
            InputText.SetInputWithoutNotify(_cameraLandMarkComponent.ShowText);
        }

        private void OnSetName(string nameStr)
        {
            if (_cameraLandMarkComponent != null)
            {
                _cameraLandMarkComponent.ShowText = nameStr;
            }
        }

        public void RefreshUI(){
            InputText.SetInputWithoutNotify(_cameraLandMarkComponent.ShowText);
        }

    }
}
