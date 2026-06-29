using System;
using BUD.GameStudio;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Props.PropsManagers;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UI.BaseWidgets;
using UnityEngine;

namespace AIGame.Base
{
    public class AIHospitalUgcGameDetailView : MonoBehaviour
    {
        public CButton Btn_Close;
        //发布
        public CButton Btn_Publish;
        //试玩
        public CButton Btn_TryPlay;
        //编辑
        public CButton Btn_DraftEdit;
        //重命名
        public CButton Btn_EditName;
        //删除
        public CButton Btn_Delete;
        //试玩
        public CButton Btn_GameImage;
        
        public BUD_Text Txt_MapName;
        public CText Txt_LastEditTime;
        public RemoteImageBehaviour Remote_MapCover;

        private DraftListItem _draftListItem;

        private void Awake()
        {
            AddListener();
        }

        public void ShowPanel(DraftListItem draftListItem)
        {
            this.gameObject.SetActive(true);
            this._draftListItem = draftListItem;
            UpdateInfo();
        }

        public void HidePanel()
        {
            MessageHelper.Broadcast(DraftMessage.RefreshDraft);
            this.gameObject.SetActive(false);
        }

        private void UpdateInfo()
        {
            Txt_MapName.text = GameStudioUtils.GetBaseInfo(this._draftListItem).name;
            Txt_LastEditTime.SetLocalText("最后编辑 {0}",
                TimestampConverter.ConvertToDateTimeString(this._draftListItem.mapInfo.updateTime));
            string coverUrl = this._draftListItem.mapInfo.cover;
            if (!string.IsNullOrEmpty(coverUrl))
            {
                Remote_MapCover.Load(coverUrl);
            }
        }

        private void AddListener()
        {
            Btn_Close.onClick.AddListener(OnBtnCloseClick);
            Btn_Publish.onClick.AddListener(OnBtnPublishClick);
            Btn_TryPlay.onClick.AddListener(OnBtnTryPlayClick);
            Btn_GameImage.onClick.AddListener(OnBtnTryPlayClick);
            Btn_DraftEdit.onClick.AddListener(OnBtnDraftEditClick);
            Btn_EditName.onClick.AddListener(OnBtnEditNameClick);
            Btn_Delete.onClick.AddListener(OnBtnDeleteClick);
        }

        private void OnBtnCloseClick()
        {
            HidePanel();
        }

        private void OnBtnPublishClick()
        {

            if (_draftListItem.mapInfo == null)
            {
                LoggerUtils.LogError("mapInfo 为空");
                return;
            }

            var detailInfo = _draftListItem.mapInfo.detailInfo;
            //判断是否通过顶点数校验
            if (detailInfo != null && !GameProfilerManager.Inst.CheckStatisticInfoIsPass(LimitType.Map, detailInfo))
            {
                UIManager.Inst.OpenPanel<PublishWarningPanel>(PanelId.PublishWarningPanel, LimitType.Map, detailInfo);
                return;
            }


            PublishSelectData data = new PublishSelectData()
            {
                DraftListItem = _draftListItem,
                PublishNormalMapAct = ()=>
                {
                    HidePanel();
                    UIManager.Inst.OpenPanel<AIHospitalUgcPublishPanel>(PanelId.AIHospitalUgcPublishPanel,
                    this._draftListItem.mapInfo);
                }
            };

            UIManager.Inst.OpenPanel(PanelId.PublishSelectWayPanel, data,GameType.AIGame);
        }

        private void OnBtnTryPlayClick()
        {
            var curMapId = this._draftListItem.mapInfo.id;
            //if (this._draftListItem.mapInfo.gameSetting.aiGameId == (int)PGCGameType.AIPark)
            //{
            //    AIParkUtils.Inst.EnterUgcParkGame(curMapId);
            //}
            //else
            {
                AIHospitalUtils.Inst.EnterUgcHospitalGame(curMapId);
            }
        }

        private void OnBtnDraftEditClick()
        {
            HidePanel();
            //if (this._draftListItem.mapInfo.gameSetting.aiGameId == (int)PGCGameType.AIPark)
            //{
            //    var panel = UIManager.Inst.OpenPanel<AIParkUgcEditPanel>(PanelId.AIParkUgcEditPanel, EditType.Edit, this._draftListItem.mapInfo);
            //    //panel.SetSaveSuccessAction(() => { MessageHelper.Broadcast(DraftMessage.RefreshDraft); });
            //}
            //else
            {
                var panel = UIManager.Inst.OpenPanel<AIHospitalUgcEditPanel>(PanelId.AIHospitalUgcEditPanel, EditType.Edit, this._draftListItem.mapInfo);
                panel.SetSaveSuccessAction(() => { MessageHelper.Broadcast(DraftMessage.RefreshDraft); });
            }
        }

        private void OnBtnEditNameClick()
        {
            var name = GameStudioUtils.GetBaseInfo(_draftListItem).name;
            var panel = UIManager.Inst.OpenPanel<EditNamePanel>(PanelId.EditNamePanel, "修改名字", name, "确定");
            panel.SetOnClickAction(ConfirmEditName);
        }

        private void ConfirmEditName(string name)
        {
            if (_draftListItem != null)
            {
                _draftListItem.mapInfo.name = name;
                var req = new SetMapInfoReq
                {
                    mapInfo = _draftListItem.mapInfo,
                    setType = (int)SetType.Edit
                };

                _draftListItem.mapInfo.name = name;
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST,
                    JsonConvert.SerializeObject(req),
                    OnEditSuccess, OnEditFail);
            }
        }

        /// <summary>
        /// 修改名字成功
        /// </summary>
        /// <param name="msg"></param>
        private void OnEditSuccess(string msg)
        {
            UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
        }

        /// <summary>
        /// 修改名字失败
        /// </summary>
        /// <param name="failMsg"></param>
        private void OnEditFail(string failMsg)
        {
            UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
            HttpResponseFailDataStruct repData = JsonConvert.DeserializeObject<HttpResponseFailDataStruct>(failMsg);
            string errormessage = repData.rmsg;
            if (!String.IsNullOrEmpty(errormessage))
            {
                TipPanel.ShowToast(errormessage);
            }
        }

        private void OnBtnDeleteClick()
        {
            CommonConfirmPanel commonConfirmPanel =
                UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetLocalText("确认删除", "你确定要删除该草稿吗？\n 一旦删除就无法找回", "删除", "取消");
            commonConfirmPanel.SetOnClickAction(ConfirmDeleteClick, null);
        }

        private void ConfirmDeleteClick()
        {
            if (_draftListItem != null)
            {
                var req = new SetMapInfoReq
                {
                    mapInfo = _draftListItem.mapInfo,
                    setType = (int)SetType.Delete
                };

                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST,
                    JsonConvert.SerializeObject(req),
                    (content) => { HidePanel(); }, null);
            }
        }
    }
}
