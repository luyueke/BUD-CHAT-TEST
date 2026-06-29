using System;
using System.Collections;
using Game.Audio;
using Game.Base;
using Game.Props.PropsManagers;
using Game.Scene.ModeController;
using Game.Utils;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AttentionButtonBehaviour : NodeBaseBehaviour
    {
        
        private Color[] originColor;
        private Animator mAnimator;
        private MeshRenderer mButtonRenderer;
        
        private Color selectColor = new Color(1f, 1f, 1f);
        private Color unSelectColor = new Color(0, 0, 0);
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            mAnimator = GetComponentInChildren<Animator>(true);
            mButtonRenderer = GameObjectEx.FindChildByName(transform, "thumbsuptap").GetComponent<MeshRenderer>();
            SetLocalColor(mButtonRenderer, unSelectColor);
        }

        public override void OnTouchClick()
        {
            base.OnTouchClick();
            if(!IsCanClick) return;
            var Manager = GlobalNodeManager.Inst.Get<AttentionButtonManager>();
            if (Manager.IsGuest())
            {
                Manager.SendRequestAttention();
            }
            else
            {
                UIAgentManager.Inst.ShowToast("已关注");
                Manager.SetAllBtnState(1,true);
            }
        }

        public void SetSelectState(int selectState,bool playAnim = true)
        {
            if (selectState == 1)
            {
                IsCanClick = false;
                mAnimator.Play("push");
                if (playAnim)
                {
                    PlaySound();
                    CoroutineManager.Inst.StartCoroutine(DelayAni(0.8f, () =>
                    {
                        SetLocalColor(mButtonRenderer, selectColor);
                    }, 0));
                }
                else
                {
                    SetLocalColor(mButtonRenderer, selectColor);
                }
            }
            else
            {
                IsCanClick = true;
                SetLocalColor(mButtonRenderer, unSelectColor);
                mAnimator.Play("pull");
            }
        }

        private void PlaySound()
        {
            AkSoundManager.Inst.PostEvent("Play_General_Button", gameObject);
        }

        IEnumerator DelayAni(float animTime, Action aniCallBack, float DelayTime)
        {
            yield return new WaitForSeconds(animTime + DelayTime);
            aniCallBack();
        }
        
        private void SetLocalColor(MeshRenderer render, Color color)
        {
            if (render == null)
            {
                LoggerUtils.Log("render is null");
                return;
            }
            
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            render.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", color);
            render.SetPropertyBlock(mpb);
        }

        public override void HighLight(bool isHigh)
        {
            base.HighLight(isHigh);
            var renderers = GetComponentsInChildren<Renderer>(true);
            GamePropUtils.HighLight(isHigh,ref originColor,renderers,2);
        }
    }
}
        
