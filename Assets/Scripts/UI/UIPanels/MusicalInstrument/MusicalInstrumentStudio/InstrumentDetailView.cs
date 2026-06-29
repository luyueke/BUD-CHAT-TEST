using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Base;
using Game.Config;
using Game.MusicalInstrument;
using Game.Props.PropsManagers;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UI;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;

public class InstrumentDetailView : MonoBehaviour
{
    public CButton Btn_Close;
    public CButton Btn_DraftEdit;
    public CButton Btn_EditName;
    public CButton Btn_Copy;
    public CButton Btn_Delete;
    public CButton Btn_Publish;
    public CButton Btn_GameImage;
    public CButton Btn_ChangeType;
    public BUD_Text Txt_MapName;
    public CText Txt_LastEditTime;
    public RemoteImageBehaviour Remote_MapCover;

    private Action<DraftListItem> reuploadAction;
    private DraftListItem _draftListItem;
    private const string tempSpriteatlasPath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";

    public void InitUI()
    {
        Btn_Close.onClick.AddListener(HidePanel);
        Btn_DraftEdit.onClick.AddListener(OnDraftsEditBtnClick);
        Btn_EditName.onClick.AddListener(OnEditNameBtnClick);
        Btn_Copy.onClick.AddListener(OnBtnCopyClick);
        Btn_Delete.onClick.AddListener(OnBtnDeleteClick);
        Btn_Publish.onClick.AddListener(GoToPublishView);
        Btn_GameImage.onClick.AddListener(GoToPublishView);
        Btn_ChangeType.onClick.AddListener(OnChangeTypeBtnClick);
    }

    /// <summary>
    /// 更新详情布局数据
    /// </summary>
    /// <param name="draftListItem"></param>
    /// <param name="studioType"></param>
    public void UpdateInfo(DraftListItem draftListItem)
    {
        if (draftListItem == null || draftListItem.skinInfo == null || draftListItem.skinActionInfo == null)
        {
            LoggerUtils.LogError("详情页 - skinInfo = null || skinActionInfo = null");
            return;
        }

        this.gameObject.SetActive(true);
        _draftListItem = draftListItem;

        Txt_MapName.text = GameStudioUtils.GetBaseInfo(draftListItem).name;
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
        if (_draftListItem == null || _draftListItem.skinInfo == null || _draftListItem.skinActionInfo == null)
        {
            LoggerUtils.LogError("详情页 - skinInfo = null || skinActionInfo = null");
            return;
        }

        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tempSpriteatlasPath, "UGCMusicInstrument_1", gameObject);
        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(new SkinInfo()
        {
            name = _draftListItem.skinInfo.name
        },null ,LoadingType.MusicalInstrument,s:sprite);

        GameController.StartSkinActionGame(EnterGameModel.UgcMusicalInstrumentContinueEdit, _draftListItem.skinInfo, _draftListItem.skinActionInfo);
        HidePanel();
    }

    private void OnEditNameBtnClick()
    {
        var name = GameStudioUtils.GetBaseInfo(_draftListItem).name;
        var panel = UIManager.Inst.OpenPanel<EditNamePanel>(PanelId.EditNamePanel, "修改名字", name, "确定");
        panel.SetOnClickAction(ConfirmEditName);
    }

    private void ConfirmEditName(string name)
    {
        if (_draftListItem == null || _draftListItem.skinInfo == null || _draftListItem.skinActionInfo == null)
        {
            LoggerUtils.LogError("详情页 - skinInfo = null || skinActionInfo = null");
            return;
        }

        _draftListItem.skinInfo.name = name;

        var req = new SetSkinInfoReq
        {
            skinInfo = _draftListItem.skinInfo,
            SkinActionInfo =  _draftListItem.skinActionInfo,
            setType = (int)SetType.Edit
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetSkin, HttpMethod.POST, JsonConvert.SerializeObject(req), OnEditSuccess, OnEditFail);
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
        if (_draftListItem == null || _draftListItem.skinInfo == null || _draftListItem.skinActionInfo == null)
        {
            LoggerUtils.LogError("详情页 - skinInfo = null || skinActionInfo = null");
            return;
        }

        var req = new SetSkinInfoReq {
            skinInfo = _draftListItem.skinInfo,
            SkinActionInfo =  _draftListItem.skinActionInfo,
            setType = (int)SetType.Copy
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetSkin, HttpMethod.POST, JsonConvert.SerializeObject(req), CopySuccess, CopyFail);
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

    private void ConfirmClick() {
        if (_draftListItem == null || _draftListItem.skinInfo == null || _draftListItem.skinActionInfo == null)
        {
            LoggerUtils.LogError("详情页 - skinInfo = null || skinActionInfo = null");
            return;
        }

        var req = new SetSkinInfoReq {
            skinInfo = _draftListItem.skinInfo,
            SkinActionInfo =  _draftListItem.skinActionInfo,
            setType = (int)SetType.Delete
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetSkin, HttpMethod.POST, JsonConvert.SerializeObject(req), OnDeleteSuccess, OnDeleteFail);
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
        // PublishCurrencyPanel publishCurrencyPanel = UIManager.Inst.OpenPanel<PublishCurrencyPanel>(PanelId.PublishCurrencyPanel);
        // publishCurrencyPanel.SetCallback((type =>
        // {
        //     OnBtnPublishClick(type);
        // }));
        OnBtnPublishClick(CurrencyType.PinkCoin);
    }

    private void OnBtnPublishClick(CurrencyType currencyType)
    {
        if (_draftListItem.skinInfo == null || _draftListItem.skinActionInfo == null)
        {
            LoggerUtils.LogError("乐器发布 - 数据有误");
            return;
        }

        //音色被删除的处理
        bool IsDeleteTone = false;
        if (_draftListItem.skinActionInfo.instrumentInfo != null && _draftListItem.skinActionInfo.instrumentInfo.toneInfo != null)
        {
            if (_draftListItem.skinActionInfo.instrumentInfo.toneInfo.isDelete == 1)
            {
                IsDeleteTone = true;
            }
        }

        var tmpSkinInfo = _draftListItem.skinInfo.Clone();
        var tmpSkinActionInfo = _draftListItem.skinActionInfo.CloneSkinActionInfo();
        //1.顶点数检查
        if (tmpSkinInfo.isProp)
        {
            var detailInfo = tmpSkinInfo.skinDetailInfo;
            if (detailInfo != null && !GameProfilerManager.Inst.CheckStatisticInfoIsPass(LimitType.Prop, detailInfo))
            {
                UIManager.Inst.OpenPanel<PublishWarningPanel>(PanelId.PublishWarningPanel, LimitType.Prop, detailInfo);
                return;
            }
        }

        var publishMachine = new UGCPublishStateMachine();
        var stateList = new List<UGCPublishStateBase>() {
        };

        if (tmpSkinInfo.isProp) {
            if (_draftListItem.skinInfo.coverAutoSaved != CoverSaveStatus.ManualSaved) {
                stateList.Add(new UGCPublishStateBase(UGCPublishState.InstrumentCoverView));
            }
            stateList.Add(new UGCPublishStateBase(UGCPublishState.SetPropSkinAnchor));
            stateList.Add(new UGCPublishStateBase(UGCPublishState.SetInstrumentAdjust));
            stateList.Add(new UGCPublishStateBase(UGCPublishState.SetInstrumentAdjustWithAnim));

            if (tmpSkinInfo.skinDetailInfo == null) {
                var oriConfigData = AvatarCommonData.From(tmpSkinInfo);
                var detailInfo = new SkinDetailInfo();
                detailInfo.pDef = oriConfigData.pDef;
                detailInfo.rDef = oriConfigData.rDef;
                detailInfo.sDef = oriConfigData.sDef;
                tmpSkinInfo.skinDetailInfo = detailInfo;
            }
        }


        stateList.Add(new UGCInstrumentDetailState());
        publishMachine.SetStates(stateList);

        var instrumentDraftInfo = InstrumentAssetManager.Inst.GetOrCreateDraftInfo(tmpSkinInfo, tmpSkinActionInfo);
        if (instrumentDraftInfo == null)
        {
            return;
        }

        publishMachine.SetEditData(new InstrumentEditData() {
            skinActionDraftInfo = instrumentDraftInfo,
            currencyType = currencyType
        });
        publishMachine.SetCancelCallBack(() => {
        });
        publishMachine.SetFinishCallBack(() => {
            HidePanel();
            RefreshPublishedList();
        });

        publishMachine.Start();

        if (IsDeleteTone)
        {
            TipPanel.ShowToast("设置的自定义音色已被删除，自动切换为默认音色");
        }
    }

    private void OnChangeTypeBtnClick()
    {
        if (_draftListItem != null)
        {
            UIManager.Inst.OpenPanel(PanelId.ChangeUGCTypePanel,_draftListItem);
        }
        HidePanel();
    }

    private void RefreshDraftsList()
    {
        MessageHelper.Broadcast(MessageName.OnUgcInstrumentDraftsListChange);
    }

    private void RefreshPublishedList()
    {
        MessageHelper.Broadcast(MessageName.OnUgcInstrumentPublishedListChange);
    }
}
