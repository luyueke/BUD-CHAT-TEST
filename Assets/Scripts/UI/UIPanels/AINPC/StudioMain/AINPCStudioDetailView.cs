using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.AINPCStudio
{
    public class AINPCStudioDetailView : MonoBehaviour
    {
        public CButton Btn_Close;
        public CButton Btn_DraftEdit;
        public CButton Btn_EditName;
        public CButton Btn_Copy;
        public CButton Btn_Delete;
        public CButton Btn_Publish;
        public CButton Btn_GameImage;
        public BUD_Text Txt_MapName;
        public CText Txt_LastEditTime;
        public RemoteImageBehaviour Remote_MapCover;

        private Action<DraftListItem> reuploadAction;
        private DraftListItem _draftListItem;

        public void InitUI()
        {
            Btn_Close.onClick.AddListener(HidePanel);
            Btn_DraftEdit.onClick.AddListener(OnDraftsEditBtnClick);
            Btn_EditName.onClick.AddListener(OnEditNameBtnClick);
            Btn_Copy.onClick.AddListener(OnBtnCopyClick);
            Btn_Delete.onClick.AddListener(OnBtnDeleteClick);
            Btn_Publish.onClick.AddListener(GoToPublishView);
            Btn_GameImage.onClick.AddListener(GoToPublishView);
        }

        private bool CheckDataIllegal(DraftListItem draftListItem)
        {
            if (draftListItem == null)
            {
                LoggerUtils.LogError("详情页 - draftListItem = null");
                return true;
            }
            if (draftListItem.npc == null)
            {
                LoggerUtils.LogError("详情页 - aiNpcInfo = null");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 更新详情布局数据
        /// </summary>
        /// <param name="draftListItem"></param>
        /// <param name="studioType"></param>
        public void UpdateInfo(DraftListItem draftListItem)
        {
            if(CheckDataIllegal(draftListItem))
                return;

            this.gameObject.SetActive(true);
            _draftListItem = draftListItem;

            var npcInfo = GameStudioUtils.GetBaseInfo(draftListItem) as AINpcInfo;
            Txt_MapName.text = npcInfo?.npcName;
            Txt_LastEditTime.SetLocalText("最后编辑 {0}", TimestampConverter.ConvertToDateTimeString(GameStudioUtils.GetBaseInfo(draftListItem).updateTime));

            string coverUrl = GameStudioUtils.GetBaseInfo(draftListItem).cover;
            if (!string.IsNullOrEmpty(coverUrl))
            {
                Remote_MapCover.Load(coverUrl);
            }
        }

        private void HidePanel()
        {
            this.gameObject.SetActive(false);
        }

        private void OnDraftsEditBtnClick()
        {
            if(CheckDataIllegal(_draftListItem))
                return;
            
            UIManager.Inst.OpenPanel(PanelId.AINpcEditPanel, _draftListItem.npc);
            HidePanel();
        }

        private void OnEditNameBtnClick()
        {
            var npcInfo = GameStudioUtils.GetBaseInfo(_draftListItem) as AINpcInfo;
            var name = npcInfo?.npcName;
            var panel = UIManager.Inst.OpenPanel<EditNamePanel>(PanelId.EditNamePanel, "修改名字", name, "确定");
            panel.SetOnClickAction(ConfirmEditName);
        }

        private void ConfirmEditName(string name)
        {
            if(CheckDataIllegal(_draftListItem))
                return;

            SetAINpcInfoReq req = new SetAINpcInfoReq();
            _draftListItem.npc.npcName = name;
            req = new SetAINpcInfoReq
            {
                npc = _draftListItem.npc,
                setType = (int)SetType.Edit
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.NpcSet, HttpMethod.POST, JsonConvert.SerializeObject(req), OnEditSuccess, OnEditFail);
        }

        private void OnEditSuccess(string msg)
        {
            UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
            HidePanel();
            RefreshDraftsList();
        }

        private void OnEditFail(string failMsg)
        {
            UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
            LoggerUtils.LogError(failMsg);
        }

        private void OnBtnCopyClick()
        {
            if(CheckDataIllegal(_draftListItem))
                return;

            SetAINpcInfoReq req = new SetAINpcInfoReq();
            req = new SetAINpcInfoReq()
            {
                npc = _draftListItem.npc,
                setType = (int)SetType.Copy
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.NpcSet, HttpMethod.POST, JsonConvert.SerializeObject(req), CopySuccess, CopyFail);
        }

        private void CopySuccess(string msg)
        {
            HidePanel();
            RefreshDraftsList();
        }

        private void CopyFail(string failMsg)
        {
            LoggerUtils.LogError(failMsg);
        }

        private void OnBtnDeleteClick()
        {
            CommonConfirmPanel commonConfirmPanel =
                UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetText("确认删除", "你确定要删除该草稿吗？\n 一旦删除就无法找回", "删除", "取消");
            commonConfirmPanel.SetOnClickAction(ConfirmClick, CancelClick);
        }

        private void ConfirmClick()
        {
            if(CheckDataIllegal(_draftListItem))
                return;

            SetAINpcInfoReq req = new SetAINpcInfoReq();
            req = new SetAINpcInfoReq()
            {
                npc = _draftListItem.npc,
                setType = (int)SetType.Delete
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.NpcSet, HttpMethod.POST, JsonConvert.SerializeObject(req), OnDeleteSuccess, OnDeleteFail);
        }

        private void CancelClick()
        {
        }

        private void OnDeleteSuccess(string msg)
        {
            HidePanel();
            RefreshDraftsList();
        }

        private void OnDeleteFail(string failMsg)
        {
            LoggerUtils.LogError(failMsg);
        }

        private void GoToPublishView()
        {
            UIManager.Inst.OpenPanel<AINpcPublishPanel>(PanelId.AINpcPublishPanel, _draftListItem.npc);
        }

        private void RefreshDraftsList()
        {
            MessageHelper.Broadcast(MessageName.OnAINpcStudioDraftListChange);
        }

        private void RefreshPublishedList()
        {
            MessageHelper.Broadcast(MessageName.OnAINpcStudioPublishedListChange);
        }
    }
}