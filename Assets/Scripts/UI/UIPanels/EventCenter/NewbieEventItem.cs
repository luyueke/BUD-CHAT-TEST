using System.Collections.Generic;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Event
{
    public class NewbieEventItem : BaseEventItem
    {
        private Animator Effect_animator;
        private Image Img_Effect;
        private Image Img_EventIcon;
        private GameObject Go_Lock;
        private CButton Btn_GoFinish;
        private string ClaimBtnEnableColor = "#FFD400";
        private string ClaimBtnUnableColor = "#865CFF";
        private bool _isLock;
        
        public override void BindUI()
        {
            base.BindUI();
            Effect_animator = this.GetComponent<Animator>();
            Img_Effect = GameObjectEx.FindChildByName(this.transform, "login_gift_hool_eff").GetComponent<Image>();
            Go_Lock = GameObjectEx.FindChildByName(this.transform, "Go_Lock").gameObject;
            Btn_GoFinish = GameObjectEx.FindChildByName(this.transform, "Btn_GoFinish").GetComponent<CButton>();
            Img_EventIcon = GameObjectEx.FindChildByName(this.transform, "Img_EventIcon").GetComponent<Image>();
            
            Btn_GoFinish.onClick.AddListener(() =>
            {
                EventCenterDataManager.Inst.SkipToTask((EventCenterSkipType)_curData.skipType);
            });
        }

        public void InitData(string taskId, TaskItemData data, bool isLock)
        {
            this._isLock = isLock;
            base.InitData(taskId, data);
            SetEventIcon((EventCenterSkipType)this._curData.skipType);
        }
        
        public void SetEventIcon(EventCenterSkipType Id)
        {
            var spriteName = "TaskItemIcon_" + (int)Id;
            Img_EventIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, gameObject);
        }
        
        public override void SetClaimState(TaskClaimState state)
        {
            base.SetClaimState(state);
            if (_isLock)
            {
                Btn_GoFinish.gameObject.SetActive(false);
                Btn_Claim.gameObject.SetActive(false);
                Go_Finished.SetActive(false);
                Go_Lock.SetActive(_isLock);
                return;
            }
            
            Go_Lock.SetActive(false);
            switch (state)
            {
                case TaskClaimState.Unable:
                    Btn_GoFinish.gameObject.SetActive(true);
                    Btn_Claim.gameObject.SetActive(false);
                    Effect_animator.enabled = false;
                    Img_Effect.color = new Color(255, 255, 255, 0);
                    break;
                case TaskClaimState.Enable:
                    Btn_GoFinish.gameObject.SetActive(false);
                    Btn_Claim.gameObject.SetActive(true);
                    Effect_animator.enabled = true;
                    Effect_animator.CrossFade("LoginGiftPanel_prompt",0.1f);
                    break;
                case TaskClaimState.Finished:
                    Btn_GoFinish.gameObject.SetActive(false);
                    Btn_Claim.gameObject.SetActive(false);
                    Go_Finished.SetActive(true);
                    Effect_animator.enabled = false;
                    Img_Effect.color = new Color(255, 255, 255, 0);
                    break;
            }
        }
    }
}
