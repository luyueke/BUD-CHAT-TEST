using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using UI;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;

public class TheatreActorDetailView : MonoBehaviour
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

    private DraftListItem _draftListItem;

    public void InitUI()
    {
        Btn_Close.onClick.RemoveAllListeners();
        Btn_Close.onClick.AddListener(HidePanel);
        Btn_DraftEdit.onClick.RemoveAllListeners();
        Btn_DraftEdit.onClick.AddListener(OnDraftsEditBtnClick);
        Btn_EditName.onClick.RemoveAllListeners();
        Btn_EditName.onClick.AddListener(OnEditNameBtnClick);
        Btn_Copy.onClick.RemoveAllListeners();
        Btn_Copy.onClick.AddListener(OnBtnCopyClick);
        Btn_Delete.onClick.RemoveAllListeners();
        Btn_Delete.onClick.AddListener(OnBtnDeleteClick);
        Btn_Publish.onClick.RemoveAllListeners();
        Btn_Publish.onClick.AddListener(OnBtnPublishClick);
        Btn_GameImage.onClick.RemoveAllListeners();
        Btn_GameImage.onClick.AddListener(OnBtnPublishClick);
    }

    private bool CheckDataIllegal(DraftListItem draftListItem)
    {
        if (draftListItem == null || draftListItem.actorInfo == null)
        {
            LoggerUtils.LogError("详情页 - actor = null");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 更新详情布局数据
    /// </summary>
    public void UpdateInfo(DraftListItem draftListItem)
    {
        if (CheckDataIllegal(draftListItem))
            return;

        this.gameObject.SetActive(true);
        _draftListItem = draftListItem;

        Txt_MapName.text = draftListItem.actorInfo.name;
        Txt_LastEditTime.SetLocalText("最后编辑 {0}", TimestampConverter.ConvertToDateTimeString(draftListItem.actorInfo.updateTime));
        string coverUrl = draftListItem.actorInfo.cover;
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
        if (CheckDataIllegal(_draftListItem))
            return;

        UIManager.Inst.OpenPanel(PanelId.TheatreCreaterActorPanel, _draftListItem.actorInfo);
        HidePanel();
    }

    private void OnEditNameBtnClick()
    {
        if (CheckDataIllegal(_draftListItem))
            return;

        var panel = UIManager.Inst.OpenPanel<EditNamePanel>(PanelId.EditNamePanel, "修改名字", _draftListItem.actorInfo.name, "确定");
        panel.SetOnClickAction(ConfirmEditName);
    }

    private void ConfirmEditName(string name)
    {
        if (CheckDataIllegal(_draftListItem))
            return;

        _draftListItem.actorInfo.name = name;
        var req = new SetActorInfoReq
        {
            actorInfo = _draftListItem.actorInfo,
            setType = (int)SetType.Edit
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActorSet, HttpMethod.POST, JsonConvert.SerializeObject(req), OnEditSuccess, OnEditFail);
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
        if (CheckDataIllegal(_draftListItem))
            return;

        var req = new SetActorInfoReq
        {
            actorInfo = _draftListItem.actorInfo,
            setType = (int)SetType.Copy
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActorSet, HttpMethod.POST, JsonConvert.SerializeObject(req), CopySuccess, CopyFail);
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
        CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("确认删除", "你确定要删除该草稿吗？\n 一旦删除就无法找回", "删除", "取消");
        commonConfirmPanel.SetOnClickAction(ConfirmClick, CancelClick);
    }

    private void ConfirmClick()
    {
        if (CheckDataIllegal(_draftListItem))
            return;

        var req = new SetActorInfoReq
        {
            actorInfo = _draftListItem.actorInfo,
            setType = (int)SetType.Delete
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActorSet, HttpMethod.POST, JsonConvert.SerializeObject(req), OnDeleteSuccess, OnDeleteFail);
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

    private void OnBtnPublishClick()
    {
        if (CheckDataIllegal(_draftListItem))
            return;
        var publishMachine = new UGCPublishStateMachine();
        var stateList = new List<UGCPublishStateBase> { new UGCActorDetailState() };
        publishMachine.SetStates(stateList);
        publishMachine.SetEditData(new ActorEditData { actorInfo = _draftListItem.actorInfo });
        publishMachine.SetCancelCallBack(() => { });
        publishMachine.SetFinishCallBack(() =>
        {
            UIManager.Inst.ClosePanel(PanelId.UGCPublishPanel);
            HidePanel();
            RefreshPublishedList();
        });
        
        publishMachine.Start();
    }

    private void RefreshDraftsList()
    {
        MessageHelper.Broadcast(MessageName.OnActorStudioDraftListChange);
    }

    private void RefreshPublishedList()
    {
        MessageHelper.Broadcast(MessageName.OnActorStudioPublishedListChange);
    }
}
