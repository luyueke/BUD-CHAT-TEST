using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MusicalInstrument {
    public class UgcToneInfoPublishedItem : MonoBehaviour {
        public CButton Btn_Create;
        public CButton Btn_Select;
        public CButton Btn_Delete;
        public GameObject Go_Selected;
        public Text Txt_Title;
        public GameObject Go_Normal;
        public GameObject Go_TryPlay;

        public GameObject Go_UnderReview;



        private bool _isCreate = false;
        private ToneInfo _curToneInfo;
        private Action<ToneInfo> _onItemSelect;
        private Action<ToneInfo> _onDeleteTone;
        private Action<ToneInfo> _onPreviewTone;
        private int _curUgcItemCount;

        private void Awake() {
            Btn_Create.onClick.AddListener(OnCreateUgcToneClick);
            Btn_Select.onClick.AddListener(OnToneItemSelect);
            Btn_Delete.onClick.AddListener(OnBtnToneDeleteClick);
            Go_UnderReview.GetComponent<Button>().onClick.AddListener(OnUnderReviewClick);
            Go_Normal.SetActive(true);
            Go_TryPlay.SetActive(false);
            Go_UnderReview.SetActive(false);
        }

        private void OnUnderReviewClick() {
            var auditResult = GetAuditResult();
            if (auditResult == AuditResult.Appealing) {
                TipPanel.ShowToast("该作品在申诉中，请耐心等待");
            } else if (auditResult == AuditResult.PendingToAudit)
            {
                TipPanel.ShowToast("该作品在审核中，请耐心等待");
            }
            else if (auditResult == AuditResult.Rejected) {

                UIManager.Inst.OpenPanel<UGCAuditRejectedPanel>(PanelId.UGCAuditRejectedPanel, new UGCAuditRejectedPanel.RejectedAppealArgs {
                    req = new UGCAsset.UGCSetRequest(_curToneInfo, UGCAsset.UGCOperationType.Appeal),
                    onRejectedCallBack = OnAppealCallBack,
                    reason = _curToneInfo.auditInfo.rejectReason,
                });
            }
        }

        private void OnAppealCallBack(bool isAppeal) {
            if (!isAppeal) {
                EditToneInfoReq req = new EditToneInfoReq() {
                    musicToneInfo = _curToneInfo,
                    setType = (int)SetType.Delete
                };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetUGCTone, HttpMethod.POST, JsonConvert.SerializeObject(req),
                    (content) => {
                        this._onDeleteTone?.Invoke(_curToneInfo);
                        MessageHelper.Broadcast(MessageName.OnUgcTonePublishedListChange);
                    }, (error) => {

                    });
            } else {

                if (_curToneInfo.auditInfo != null) {
                    _curToneInfo.auditInfo.auditResult = (int)AuditResult.Appealing;
                }
                RefreshAuditStatus();

            }
        }

        public void InitCreateMode(int ugcItemCount)
        {
            this._curUgcItemCount = ugcItemCount;
            _isCreate = true;
            Btn_Create.gameObject.SetActive(true);
            Btn_Select.gameObject.SetActive(false);
            Btn_Delete.transform.parent.gameObject.SetActive(false);
        }

        public void InitSelectMode(ToneItemData info, Action<ToneInfo> act, Action<ToneInfo> deleteAct, Action<ToneInfo> previewToneAct) {
            _isCreate = false;
            this._curToneInfo = info.musicToneInfo;
            this._onItemSelect = act;
            this._onDeleteTone = deleteAct;
            this._onPreviewTone = previewToneAct;
            Btn_Create.gameObject.SetActive(false);
            Btn_Select.gameObject.SetActive(true);
            Btn_Delete.transform.parent.gameObject.SetActive(false);
            this.Txt_Title.SetLocalText(this._curToneInfo.name);
            RefreshAuditStatus();
        }

        public void InitDeleteMode() {
            if (_isCreate)
                return;

            Btn_Delete.transform.parent.gameObject.SetActive(true);
        }

        private void OnCreateUgcToneClick()
        {
            if (DeviceInfoManager.Inst.DeviceBaseData.version == "1.0.1" ||
                DeviceInfoManager.Inst.DeviceBaseData.version == "1.0.0")
            {
                UIAgentManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, (int)ForceUpdate.NeedUpdateFeature);
                return;
            }

            if (this._curUgcItemCount >= 20)
            {
                TipPanel.ShowToast("最多支持保存20个本地音色，请删除不要的音色再创建新本地音色");
                return;
            }

            UIManager.Inst.OpenPanel(PanelId.CreateUgcTonePanel);
        }

        private void OnToneItemSelect() {
            PlayMusic();
            this._onItemSelect?.Invoke(this._curToneInfo);
        }

        private void OnBtnToneDeleteClick() {
            CommonBoxConfirmWithTitlePanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
            commonConfirmPanel.SetLocalText("删除音色", "你确定删除选中的音色吗？", "删除", "取消");
            commonConfirmPanel.SetOnClickAction(() => {
                EditToneInfoReq req = new EditToneInfoReq() {
                    musicToneInfo = _curToneInfo,
                    setType = (int)SetType.Delete
                };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetUGCTone, HttpMethod.POST, JsonConvert.SerializeObject(req),
                    (content) => {
                        this._onDeleteTone?.Invoke(_curToneInfo);
                        MessageHelper.Broadcast(MessageName.OnUgcTonePublishedListChange);
                    }, (error) => {

                    });
            }, () => {

            });
        }

        public void SetSelectState(bool isSelect) {
            if (_isCreate)
                return;

            Go_Selected.SetActive(isSelect);

            Go_Normal.SetActive(!isSelect);
            Go_TryPlay.SetActive(isSelect);
        }

        private void PlayMusic() {
            this._onPreviewTone?.Invoke(_curToneInfo);
        }

        private void RefreshAuditStatus() {

            var auditResult = GetAuditResult();
            var TxtComp = Go_UnderReview.GetComponentInChildren<Text>();
            if (auditResult == AuditResult.Rejected) {
                TxtComp.SetLocalText("审核失败");
                GameObjectEx.FindChildByName(this.transform, "Go_UnderReview/Image").GetComponent<Image>().gameObject.SetActive(true);
                Go_UnderReview.SetActive(true);
            } else if (auditResult == AuditResult.Appealing) {
                TxtComp.SetLocalText("申诉中");
                GameObjectEx.FindChildByName(this.transform, "Go_UnderReview/Image").GetComponent<Image>().gameObject.SetActive(true);
                Go_UnderReview.SetActive(true);
            }
            else if (auditResult == AuditResult.PendingToAudit) {
                TxtComp.SetLocalText("审核中");
                GameObjectEx.FindChildByName(this.transform, "Go_UnderReview/Image").GetComponent<Image>().gameObject.SetActive(false);
                Go_UnderReview.GetComponentInChildren<Image>().gameObject.SetActive(false);
                Go_UnderReview.SetActive(true);
            }
        }

        private AuditResult GetAuditResult() {
            var auditResult = AuditResult.Passed;
            if (_curToneInfo.auditInfo != null) {
                auditResult = (AuditResult)_curToneInfo.auditInfo.auditResult;
            }
            return auditResult;
        }






    }
}
