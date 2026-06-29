
using Game.Audio;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Scene.ModeController;
using Game.Utils;
using TMPro;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class SwitchBtnBehaviour : NodeBaseBehaviour
    {
        public bool isWork = false;
        private TextMeshPro _textMeshPro;
        private Animator _animator;
        private Color[] originColor;
        
        public override void OnInitByCreate()
        {
            _animator = GetComponentInChildren<Animator>();
            _textMeshPro = GetComponentInChildren<TextMeshPro>();
            // _animator.Play("Inacbtn", 0, 0);
        }

        public override void HighLight(bool isHigh)
        {
            base.HighLight(isHigh);
            var renderers = GetComponentsInChildren<Renderer>(true);
            GamePropUtils.HighLight(isHigh,ref originColor,renderers,2);
        }


        public void SetTextVisible(bool value)
        {
            if (_textMeshPro != null)
            {
                _textMeshPro.gameObject.SetActive(value);
            }
        }
        
        public void RefreshIndex()
        {
            var tComp = entity.GetComp<SwitchBtnComponent>();
            _textMeshPro.text = tComp.SwitchIndex.ToString();
        }
        
        public override void OnTouchClick()
        {
            AkSoundManager.Inst.PlayInteractable3DSound("General_Button",gameObject);
            _animator.Play("Inacbtn", 0, 0);
            isWork = !isWork;
            Invoke("OnSwitchClickedLocal", 0.5f);//延迟0.5秒，对齐动画
            
            if (GlobalNodeManager.Inst.Get<SwitchBtnManager>().IsGuest())
            {
                GlobalNodeManager.Inst.Get<SwitchBtnManager>().SendRequest(this);
            }
        }
        
        private void OnSwitchClickedLocal()
        {
            GlobalNodeManager.Inst.Get<SwitchBtnManager>().HandleSwitchClick(this);
        }

    }
}
        
