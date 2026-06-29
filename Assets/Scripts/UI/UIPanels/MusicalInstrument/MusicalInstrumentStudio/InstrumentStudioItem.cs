using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using GameData.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MusicalInstrument {
    public class InstrumentStudioItem : MonoBehaviour {
        public CButton Btn_Create;
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
        private EnterGameModel _enterGameModel;

        public void InitCreateMode(EnterGameModel enterGameModel) {
            _enterGameModel = enterGameModel;
            Btn_Create.gameObject.SetActive(true);
            Btn_Select.gameObject.SetActive(false);
            Btn_Create.onClick.RemoveAllListeners();
            Btn_Create.onClick.AddListener(OnCreateBtnClick);
        }
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
            Btn_Create.gameObject.SetActive(false);
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
                if (_curData.skinInfo.paymentInfo!=null)
                {
                    price.SetText(_curData.skinInfo.paymentInfo.price.ToString());
                    currencyIcon.sprite =
                        PgcUtils.LoadCurrencyIcon(_curData.skinInfo.paymentInfo.currencyType, gameObject);
                }
                if (_curData.interactInfo != null)
                    buyNum.text = data.interactInfo.likeAmount.ToString();
            }
			RefreshAuditState();
        }

        private void OnCreateBtnClick() {
            if(_enterGameModel == EnterGameModel.UgcMusicalInstrumentEmpty)
            {
                UIManager.Inst.OpenPanel(PanelId.CreateMusicalInstrumentPanel, _enterGameModel);
            }
            else if (_enterGameModel == EnterGameModel.UgcVehicleEmpty)
            {
                UIManager.Inst.OpenPanel(PanelId.CreateVehiclePanel, _enterGameModel);
            }
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
                Btn_underReview.GetComponentInChildren<Text>(true).SetLocalText("审核中");
                Btn_underReview.gameObject.SetActive(true);
                publishRoot.SetActive(false);
            } else if (auditResult == AuditResult.Appealing) {
                Btn_underReview.GetComponentInChildren<Text>(true).SetLocalText("申诉中");
                Btn_underReview.gameObject.SetActive(true);
                publishRoot.SetActive(false);
            } else {
                  Btn_underReview.gameObject.SetActive(false);
            }
        }

        private AuditResult GetAuditResult() {
            var auditResult = AuditResult.Passed;
            if (_curData.skinInfo != null && _curData.skinInfo.auditInfo != null) {
                auditResult = (AuditResult)_curData.skinInfo.auditInfo.auditResult;
            }
            return auditResult;
        }

    }
}
