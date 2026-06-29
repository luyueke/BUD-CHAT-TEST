using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using GameData.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.AINPCStudio {
    public class AINPCStudioItem : MonoBehaviour {
        public CButton Btn_Select;
        public CButton Btn_Create;
        public CButton Btn_GoToStore;
        public GameObject Go_InfoView;
        public GameObject Go_CreateView;
        public GameObject Go_GoToStore;
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


        public void InitSelectMode(DraftListItem data, Action<DraftListItem> onSelect, StudioSubType studioSubType) 
        {
            Go_InfoView.SetActive(true);
            Go_CreateView.SetActive(false);
            Go_GoToStore.SetActive(false);
            
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
                if (_curData.npc != null && _curData.npc.paymentInfo != null)
                {
                    price.SetText(_curData.npc.paymentInfo.price.ToString());
                    currencyIcon.sprite = PgcUtils.LoadCurrencyIcon(_curData.npc.paymentInfo.currencyType, gameObject);
                }
                if (_curData.interactInfo != null)
                    buyNum.text = data.interactInfo.likeAmount.ToString();
            }
			RefreshAuditState();
        }

        public void InitCreateMode()
        {
            Go_InfoView.SetActive(false);
            Go_CreateView.SetActive(true);
            Go_GoToStore.SetActive(false);
            
            Btn_Create.onClick.RemoveAllListeners();
            Btn_Create.onClick.AddListener(OnBtnCreateClick);
        }

        public void InitGoToStore()
        {
            Go_InfoView.SetActive(false);
            Go_CreateView.SetActive(false);
            Go_GoToStore.SetActive(true);
            
            Btn_GoToStore.onClick.RemoveAllListeners();
            Btn_GoToStore.onClick.AddListener(OnGoToStoreClick);
        }

        private void OnBtnCreateClick()
        {
            UIManager.Inst.OpenPanel(PanelId.AINpcCreatePanel);
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
            if (_curData.npc != null && _curData.npc.auditInfo != null) {
                auditResult = (AuditResult)_curData.npc.auditInfo.auditResult;
            }
            return auditResult;
        }

        private void OnGoToStoreClick()
        {
            UIManager.Inst.OpenPanel<AINpcStorePanel>(PanelId.AINpcStorePanel);
        }
    }
}
