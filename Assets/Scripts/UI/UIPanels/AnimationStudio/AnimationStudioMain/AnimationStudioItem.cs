using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using GameData.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.AnimationStudio {
    public class AnimationStudioItem : MonoBehaviour {
        public CButton Btn_Select;
        public GameObject publishRoot;
        public Text buyNum;
        public Text price;
        public Image currencyIcon;
        public CButton Btn_underReview;

        //Data
        private DraftListItem _curData;
        private Action<DraftListItem> _onClickAct;
        private StudioSubType _curStudioSubType;
        
        private void OnUnderReviewClick() {
            var auditResult = GetAuditResult();
            if (auditResult == AuditResult.Appealing) {
                TipPanel.ShowToast("该作品在申诉中，请耐心等待");
            }
        }


        public void InitSelectMode(DraftListItem data, Action<DraftListItem> onSelect, StudioSubType studioSubType) {
            _onClickAct = onSelect;
            _curData = data;
            _curStudioSubType = studioSubType;
            
            Btn_Select.gameObject.SetActive(true);

            Btn_Select.onClick.RemoveAllListeners();
            Btn_Select.onClick.AddListener(OnSelectBtnClick);


            Btn_underReview.onClick.RemoveAllListeners();
            Btn_underReview.onClick.AddListener(OnUnderReviewClick);
            Btn_underReview.gameObject.SetActive(false);


            if (studioSubType == StudioSubType.Drafts)
            {
                publishRoot.SetActive(false);
            }
            else
            {
                publishRoot.SetActive(true);
                if (_curData.animInfo != null && _curData.animInfo.paymentInfo != null)
                {
                    price.SetText(_curData.animInfo.paymentInfo.price.ToString());
                    currencyIcon.sprite = PgcUtils.LoadCurrencyIcon(_curData.animInfo.paymentInfo.currencyType, gameObject);
                }
                if (_curData.poseInfo != null && _curData.poseInfo.paymentInfo != null)
                {
                    price.SetText(_curData.poseInfo.paymentInfo.price.ToString());
                    currencyIcon.sprite = PgcUtils.LoadCurrencyIcon(_curData.poseInfo.paymentInfo.currencyType, gameObject);
                }
                if (_curData.interactInfo != null)
                    buyNum.text = data.interactInfo.likeAmount.ToString();
            }
			RefreshAuditState();
        }

        private void OnSelectBtnClick() {
            if (_curData == null) {
                return;
            }
            _onClickAct?.Invoke(_curData);
        }


        private void RefreshAuditState() {
            var auditResult = GetAuditResult();
            if (auditResult == AuditResult.PendingToAudit) {
                Btn_underReview.GetComponentInChildren<Text>(true).text = "审核中";
                Btn_underReview.gameObject.SetActive(true);
                publishRoot.SetActive(false);
            } else if (auditResult == AuditResult.Appealing) {
                Btn_underReview.GetComponentInChildren<Text>(true).text = "申诉中";
                Btn_underReview.gameObject.SetActive(true);
                publishRoot.SetActive(false);
            } else {
                  Btn_underReview.gameObject.SetActive(false);
            }
        }

        private AuditResult GetAuditResult() {
            var auditResult = AuditResult.Passed;
            if (_curData.animInfo != null && _curData.animInfo.auditInfo != null) {
                auditResult = (AuditResult)_curData.animInfo.auditInfo.auditResult;
            }
            if (_curData.poseInfo != null && _curData.poseInfo.auditInfo != null) {
                auditResult = (AuditResult)_curData.poseInfo.auditInfo.auditResult;
            }
            return auditResult;
        }

    }
}
