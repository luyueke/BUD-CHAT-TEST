using System;
using System.Collections.Generic;
using Game.Base;
using Game.Config;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 更新地图页面
/// </summary>
public class UpdateMapPanel : BasePanel<UpdateMapPanel>
{
    public UpdateMapEntry gameEntry;

    private Button closeBtn;
    private LoadingButton updateBtn;
    private Image updateBtnImage;
    private Text updateBtnText;
    private Text emptyText;

    public Sprite enableImage;
    public Sprite unableImage;

    private DraftListItem _draftListItem;

    private Action<bool> _updateMapAction;

    private DraftListItem originalDraftsItem;

    public void SetUpdateMapAction(Action<bool> updateMapAction)
    {
        _updateMapAction = updateMapAction;
    }

    public override void OnCreate()
    {
        closeBtn = GameObjectEx.FindChildByName(transform, "Close").GetComponent<Button>();
        updateBtn = GameObjectEx.FindChildByName(transform, "UpdateBtn").GetComponent<LoadingButton>();
        updateBtnImage = GameObjectEx.FindChildByName(transform, "UpdateBtn").GetComponent<Image>();
        updateBtnText = GameObjectEx.FindChildByName(transform, "UpdateBtn/ButtonText").GetComponent<Text>();
        emptyText = GameObjectEx.FindChildByName(transform, "EmptyText").GetComponent<Text>();
        closeBtn.onClick.AddListener(CloseClick);
        gameEntry.SetLoader(UpdateMapDataManager.Inst);
        gameEntry.SetActions(GameStudioDraftsLoopGridViewItemTrigger, IsEmptyAction);
        updateBtn.onClick.AddListener(UpdateMap);
        RequestDataList();
    }


    /// <summary>
    /// 更新地图
    /// </summary>
    private void UpdateMap()
    {
        if (_draftListItem == null || _draftListItem == null || originalDraftsItem == null ||
            originalDraftsItem.mapInfo == null)
        {
            return;
        }
        
        //原始地图通关条件
        // int originalWinCondition = originalDraftsItem.mapInfo.gameSetting.winCondition;
        //当前地图通关条件
        int currentWinCondition = _draftListItem.mapInfo.gameSetting.winCondition;

        //不允许用非通关图覆盖通关图
        if (originalDraftsItem.mapInfo.IsPassLevelMap() && currentWinCondition == 0)
        {
            TipPanel.ShowToast("选择的草稿箱地图无通关条件图，覆盖更新失败");
            return;
        }

        if (!_draftListItem.mapInfo.metaDataUrl.StartsWith(GameConsts.BusinessCdnUrl) &&
            !_draftListItem.mapInfo.metaDataUrl.StartsWith(GameConsts.BusinessBaseUrl) &&
            !_draftListItem.mapInfo.metaDataUrl.StartsWith(GameConsts.AccBusinessBaseUrl))
        {
            TipPanel.ShowToast("选择的草稿箱地图未上传远端，覆盖更新失败");
            return;
        }

        if (_draftListItem.mapInfo.IsPassLevelMap())
        {
            //新图是通关图，则都需要先进自测模式
            UIManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel, new CommonSingleConfirmPanelData()
            {
                ContextString = "通关地图即可更新！",
                ConfirmString = "确定",
                ConfirmClickAction = OnUpdatePulishTestClick,
            });
        }
        else
        {
            //非通关地图
            SendUpdateHttpRequest();
        }
    }
    
    void OnUpdatePulishTestClick()
    {
        GameController.StartGame(EnterGameModel.UpdatePublishTest, _draftListItem.mapInfo);
    }

    /// <summary>
    /// 发送后台请求覆盖更新地图
    /// </summary>
    public void SendUpdateHttpRequest()
    {
        if (updateBtn.IsLoading)
        {
            return;
        }
        updateBtn.ShowLoading();
        var req = new SetMapInfoReq
        {
            mapInfo = _draftListItem.mapInfo,
            setType = (int)SetType.Replace,
            overwriteId = originalDraftsItem.mapInfo.id
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST, JsonConvert.SerializeObject(req), OnUpdateSuccess, OnUpdateFail);
    }

    private void OnUpdateSuccess(string msg)
    {
        updateBtn.HideLoading();
        CloseSelf();
        _updateMapAction?.Invoke(true);
    }

    private void OnUpdateFail(string failMsg)
    {
        updateBtn.HideLoading();
        CloseSelf();
        _updateMapAction?.Invoke(false);
    }

    private void RequestDataList()
    {
        gameEntry.GetFirstPageDatas(GetPageDatas);
    }

    private void GetPageDatas(List<DraftListItem> infos)
    {
        if (UpdateMapDataManager.Inst.isRequestingData)
        {
            return;
        }
        
        if (infos == null || infos.Count == 0)
        {
            emptyText.gameObject.SetActive(true);
        }
        else
        {
            emptyText.gameObject.SetActive(false);
        }
    }

    private void GameStudioDraftsLoopGridViewItemTrigger(DraftListItem item)
    {
        //更新地图逻辑
        if (item != null)
        {
            updateBtnImage.sprite = enableImage;
            _draftListItem = item;
        }
    }

    private void IsEmptyAction()
    {
    }


    private void CloseClick()
    {
        CloseSelf();
    }

    public override void OnShow(params object[] args)
    {
        originalDraftsItem = args[0] as DraftListItem;
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
    }

    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }
}