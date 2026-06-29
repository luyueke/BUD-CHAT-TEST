using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using Game.Base;
using Game.Props.PropsManagers;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UI;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class MusicScoreDetailView : MonoBehaviour
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
    private Action publishAction;
    private DraftListItem _draftListItem;
    [SerializeField] private AddPartTipView eightSyllableEditTipView;
    public void InitUI(Action publishAction)
    {
        Btn_Close.onClick.AddListener(HidePanel);
        Btn_DraftEdit.onClick.AddListener(OnDraftsEditBtnClick);
        Btn_EditName.onClick.AddListener(OnEditNameBtnClick);
        Btn_Copy.onClick.AddListener(OnBtnCopyClick);
        Btn_Delete.onClick.AddListener(OnBtnDeleteClick);
        Btn_Publish.onClick.AddListener(GoToPublishView);
        Btn_GameImage.onClick.AddListener(GoToPublishView);
        this.publishAction = publishAction;
        eightSyllableEditTipView.Init();
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


    /// <summary>
    /// 更新详情布局数据
    /// </summary>
    /// <param name="draftListItem"></param>
    /// <param name="studioType"></param>
    public void UpdateInfo(DraftListItem draftListItem)
    {
        if (draftListItem == null || draftListItem.musicScoreInfo == null)
        {
            LoggerUtils.LogError("详情页 - musicScoreInfo = null ");
            return;
        }

        this.gameObject.SetActive(true);
        _draftListItem = draftListItem;

        Txt_MapName.text = GameStudioUtils.GetBaseInfo(draftListItem).name;
        Txt_LastEditTime.SetLocalText("最后编辑 {0}", TimestampConverter.ConvertToDateTimeString(GameStudioUtils.GetBaseInfo(draftListItem).updateTime));

        string coverUrl = GameStudioUtils.GetBaseInfo(draftListItem).cover;
        if (!string.IsNullOrEmpty(coverUrl))
        {
            Remote_MapCover.gameObject.SetActive(true);
            Remote_MapCover.Load(coverUrl);
        }
        else
        {
            Remote_MapCover.gameObject.SetActive(false);
        }
    }

    private void HidePanel()
    {
        this.gameObject.SetActive(false);
    }

    private void OnDraftsEditBtnClick()
    {
        if (_draftListItem == null || _draftListItem.musicScoreInfo == null)
        {
            LoggerUtils.LogError("详情页 - _draftListItem.musicScoreInfo");
            return;
        }
        GameController.StartGame(EnterGameModel.UgcMusicScoreContinueEdit, _draftListItem.musicScoreInfo,targetScene:null);
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
        if (_draftListItem == null ||_draftListItem.musicScoreInfo == null)
        {
            LoggerUtils.LogError("详情页 -  _draftListItem.musicScoreInfo");
            return;
        }

        _draftListItem.musicScoreInfo.name = name;

        var req = new SetMusicScoreInfoReq
        {
            musicScoreInfo = _draftListItem.musicScoreInfo,
            setType = (int)SetType.Edit
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetMusicScore, HttpMethod.POST, JsonConvert.SerializeObject(req), OnEditSuccess, OnEditFail);
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
        if (_draftListItem == null ||_draftListItem.musicScoreInfo == null)
        {
            LoggerUtils.LogError("详情页 -  _draftListItem.musicScoreInfo");
            return;
        }

        var req = new SetMusicScoreInfoReq
        {
            musicScoreInfo = _draftListItem.musicScoreInfo,
            setType = (int)SetType.Copy
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetMusicScore, HttpMethod.POST, JsonConvert.SerializeObject(req), CopySuccess, CopyFail);
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
        if (_draftListItem == null ||_draftListItem.musicScoreInfo == null)
        {
            LoggerUtils.LogError("详情页 -  _draftListItem.musicScoreInfo");
            return;
        }
        var req = new SetMusicScoreInfoReq
        {
            musicScoreInfo = _draftListItem.musicScoreInfo,
            setType = (int)SetType.Delete
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetMusicScore, HttpMethod.POST, JsonConvert.SerializeObject(req), OnDeleteSuccess, OnDeleteFail);
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

    private void OnBtnPublishClick(CurrencyType currencyType)
    {
        if (_draftListItem == null ||_draftListItem.musicScoreInfo == null)
        {
            LoggerUtils.LogError("详情页 -  _draftListItem.musicScoreInfo");
            return;
        }
        var tmpMusicScoreInfo = _draftListItem.musicScoreInfo.Clone();
        if (!CheckEightSyllableEdit(tmpMusicScoreInfo))
        {
            eightSyllableEditTipView.Show();
            return;
        }
        bool isVip = VipDataManager.Inst.isVip;
        if (!isVip)
        {
            var joinVipType = new List<JoinVipType>();
            var joinVipTitle = "您正在使用的VIP功能：";
            if (CheckVIPTwentyTwoEdit(tmpMusicScoreInfo))
            {
                joinVipTitle += "22按键乐谱";
                joinVipType.Add(JoinVipType.MusicScoreTwentyTwoKey);
            }
            if (CheckVIPPartEdit(tmpMusicScoreInfo))
            {
                if (joinVipType.Count != 0) {
                    joinVipTitle += "、";
                }
                joinVipTitle += "多小节乐谱";
                joinVipType.Add(JoinVipType.MusicScorePartAdd);
            }
            if (joinVipType.Count>0)
            {
                string titleStr = LocalizationManager.Inst.GetLocalizedText(joinVipTitle);
                UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel,titleStr, joinVipType);
                return;
            }
        }
        var publishMachine = new UGCPublishStateMachine();
        var stateList = new List<UGCPublishStateBase>() {
        };
        stateList.Add(new UGCMusicScoreDetailState());
        publishMachine.SetStates(stateList);

        var draftInfo = MusicScoreAssetManager.Inst.GetOrCreateDraftInfo(tmpMusicScoreInfo);
        if (draftInfo == null)
        {
            return;
        }
        publishMachine.SetEditData(new MusicScoreEditData() {
            draftInfo = draftInfo,
            currencyType = currencyType
        });
        publishMachine.SetFinishCallBack(() => {
            publishAction?.Invoke();
            HidePanel();
        });
        publishMachine.Start();
    }
    //判断是否使用vip功能
    public bool CheckVIPTwentyTwoEdit(MusicScoreInfo info)
    {
        if (info.toneType == (int)ToneType.TwentyTwo)
        {
            return true;
        }
        return false;
    }
    public bool CheckVIPPartEdit(MusicScoreInfo info)
    {
        if (info.partList!=null&&info.partList.Count>1)
        {
            return true;
        }
        return false;
    }
    //特殊逻辑少于8个有效音符的乐谱不允许发布
    public bool CheckEightSyllableEdit(MusicScoreInfo info)
    {
        if (info.partList==null||info.partList.Count==0)
        {
            return false;
        }

        if (info.partList.Count <= 1)
        {
            var sList = info.partList[0].syllableInfosList;
            if (sList==null)
            {
                return false;
            }
            else
            {
                int count = 0;
                for (int i = 0; i < sList.Count; i++)
                {
                    if (sList[i].HasEdit())
                    {
                        count++;
                    }
                }
                return count >= 8;
            }
        }
        return true;

    }
    private void RefreshDraftsList()
    {
        MessageHelper.Broadcast(MessageName.OnUgcMusicScoreDraftsListChange);
    }
    private void RefreshPublishedList()
    {
        MessageHelper.Broadcast(MessageName.OnUgcMusicScorePublishedListChange);
    }
}
