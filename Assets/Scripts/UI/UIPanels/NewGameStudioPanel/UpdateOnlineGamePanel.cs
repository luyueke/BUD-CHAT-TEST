using System;
using System.Collections.Generic;
using BUD.GameStudio;
using Game.Base;
using Game.Props.PropsManagers;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class UpdateOnlineGamePanel : BasePanel<UpdateOnlineGamePanel>
{
    [SerializeField] private Button close;
    [SerializeField] private Sprite updateBtnEnable;
    [SerializeField] private Sprite updateBtnUnable;
    [SerializeField] private Button updateBtn;
    [SerializeField] private Image loading;
    [SerializeField] private GameObject tips;
    [SerializeField] private Text updateBtnText;
    [SerializeField] private Image _titleBg;
    public UpdateOnlineGameEntry gameEntry;

    private string currentSelectedId = "";
    private MapInfo selectMapInfo;
    private DraftListItem _draftListItem;
    private GameType _gameType = GameType.Normal;
    private int _gameID;

    #region UIConfig
    public override void OnCreate()
    {
        base.OnCreate();
        close.onClick.AddListener(() => { CloseSelf(); });
        updateBtn.onClick.AddListener(PublishMapInfo);
        SetButtonState();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow();
        _draftListItem = (DraftListItem)args[0];
        if (args != null && args.Length > 1 && args[1] is GameType)
        {
            _gameType = (GameType)args[1];
            if (_gameType == GameType.AIGame)
            {
                if (ColorUtility.TryParseHtmlString(AIGameHospitalConfig.hospitalThemeColor, out Color color))
                {
                    _titleBg.color = color;
                }
            }
        }
        if (args != null && args.Length > 2 && args[2] is int)
        {
            _gameID = (int)args[2];
        }
        gameEntry.SetGameID(_gameID);
        gameEntry.GetFirstPageFriendDatas(_gameType);
        gameEntry.SetAction(GameStudioDraftsLoopGridViewItemTrigger);
        gameEntry.updateItems = (empty) =>
        {
            tips.SetActive(empty);
        };
    }
    #endregion


    /**
     * Game Studio点击事件
     */
    private void GameStudioDraftsLoopGridViewItemTrigger(MapInfo item)
    {
        string mapId = item.id;
        if (String.IsNullOrEmpty(mapId))
        {
            return;
        }

        selectMapInfo = item;
        currentSelectedId = mapId;
        SetButtonState();
    }

    public void PublishMapInfo()
    {
        if(_draftListItem == null || _draftListItem.mapInfo == null)
            return;

        var curMapInfo = _draftListItem.mapInfo;
        
        var req = new SetMapInfoReq
        {
            mapInfo = curMapInfo,
            setType = (int)SetType.Replace,
            overwriteId = currentSelectedId
        };
        
        //不允许用非通关图覆盖通关图
        int currentWinCondition = _draftListItem.mapInfo.gameSetting.winCondition;
        if (selectMapInfo.IsPassLevelMap() && currentWinCondition == 0)
        {
            TipPanel.ShowToast("选择的草稿箱地图无通关条件图，覆盖更新失败");
            return;
        }

        if (GameType.Normal == _gameType&& curMapInfo.IsPassLevelMap())
        {
            //通关图发布
            UIManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel, new CommonSingleConfirmPanelData()
            {
                ContextString = "通关地图即可发布！",
                ConfirmString = "确定",
                ConfirmClickAction = OnPulishTestClick,
            });
        }
        else
        {
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST, JsonConvert.SerializeObject(req), OnUpdateSuccess, OnUpdateFail);
        }
    }

    /// <summary>
    /// 通关测试
    /// </summary>
    private void OnPulishTestClick()
    {
        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(_draftListItem.mapInfo, _draftListItem.creator, LoadingType.Map, null);
        GameController.StartGame(EnterGameModel.PublishTest, _draftListItem.mapInfo);
    }
    
    //前往杰哥发布界面-覆盖发布
    public void GoToOverwritePublishMapPanel()
    {
        var publishMachine = new UGCPublishStateMachine();
        publishMachine.SetStates(new List<UGCPublishStateBase>()
        {
            new UGCMapDetailState()
        });

        var tempMapInfo = _draftListItem.mapInfo.Clone();
        var draftInfo = MapAssetManager.Inst.GetDraftInfo(tempMapInfo);
        if (draftInfo == null)
        {
            draftInfo = new MapDraftInfo(tempMapInfo);
        }

        publishMachine.SetEditData(new MapEditData()
        {
            draftInfo = draftInfo,
            isPublish = true,
            overwriteId = currentSelectedId
        });

        publishMachine.SetFinishCallBack(() =>
        {
            OnUpdateSuccess("OverwritePublishSuccess");
        });
        publishMachine.SetCancelCallBack(() =>
        {
            OnUpdateFail("OverwritePublishFail");
        });
        publishMachine.Start();
    }

    private void OnUpdateSuccess(string msg)
    {
        TipPanel.ShowToast("更新地图成功");
        //1.跳转发布页面
        if (UIManager.Inst.TryFindPanel<GameStudioPanel>(PanelId.GameStudioPanel, out var gameStudioPanel))
        {
            gameStudioPanel.SelectTopTab(StudioSubType.Published);
        }
        //2.关闭自己
        CloseSelf();
    }

    private void OnUpdateFail(string failMsg)
    {
        CloseSelf();
    }

    private void SetButtonState()
    {
        updateBtn.interactable = !string.IsNullOrEmpty(currentSelectedId);
    }
}
