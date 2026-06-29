using System;
using System.Collections.Generic;
using Game.Props.PropsManagers;
using GameData;
using GameData.BaseInfo;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

/// <summary>
/// Author:
/// Desc:
/// Date:24-07-03 13:46:43
/// </summary>
public class AssetStudioDraftCommonPanel : BasePanel<AssetStudioDraftCommonPanel>
{

    [SerializeField] private NavigationBarTabs navigationBarTabs;
    [SerializeField] private AssetStudioListView draftView;
    [SerializeField] private AssetStudioListView publishView;
    [SerializeField] private AssetRightMenuView draftEditView;
    [SerializeField] private GameObject mask;

    private MainViewType _studioType = MainViewType.Prop;

    public override void OnShow(params object[] args)
    {

        if (args.Length > 0)
        {
            var type = (MainViewType)args[0];
            if (type != MainViewType.Prop && type != MainViewType.Material)
            {
                LoggerUtils.LogError("[Draft] 目前暂时不支持 除【Prop、Material】外逻辑");
            }
            _studioType = (MainViewType)type;
        }

        draftView.InitUI(StudioSubType.Drafts, _studioType,  OnClickDraftItem, OnClickCreateDraft);
        publishView.InitUI(StudioSubType.Published, _studioType,  OnClickPublishItem);
        publishView.gameObject.SetActive(false);
        draftView.RequestDataList();
    }

    public override void OnWindowPop()
    {
    }

    public class GameStudioConfig
    {
        public string name;
        public StudioSubType StudioSubType;
    }

    private List<GameStudioConfig> rtConfig = new()
    {
        new() { name = "草稿箱", StudioSubType = StudioSubType.Drafts },
        new() { name = "已发布", StudioSubType = StudioSubType.Published },
    };

    public override void OnCreate()
    {
        navigationBarTabs.AddBackBtnClickListener(OnBackBtnClick);
        foreach (var cfg in rtConfig)
        {
            navigationBarTabs.CreateItem(cfg.StudioSubType.ToString(), cfg.name).SetIsSelect(false);
        }

        navigationBarTabs.SetSelect(0);
        navigationBarTabs.AddItemSelectCallBack(RTClick);

        draftEditView?.InitUI();
        draftEditView?.InitAction(OnPublishCallBack,OnEditClick,OnItemInfoChange,CopyGame,DeleteGame);
    }

    void RTClick(TabItem item, int index)
    {
        var data = rtConfig[index];
        draftView.gameObject.SetActive(data.StudioSubType == StudioSubType.Drafts);
        publishView.gameObject.SetActive(data.StudioSubType == StudioSubType.Published);

        if (data.StudioSubType == StudioSubType.Drafts)
        {
            draftView.RequestDataList();
        } else if (data.StudioSubType == StudioSubType.Published)
        {
            publishView.RequestDataList();
        }
    }

    private void OnBackBtnClick()
    {
        UIManager.Inst.ClosePanel(this);
    }

    public override void OnHidden()
    {

    }

    protected override void OnDestroy()
    {

    }

    public override void OnWindowBeFocused()
    {
        mask?.gameObject.SetActive(false);
    }

    private void OnClickDraftItem(DraftListItem item)
    {
        draftEditView.UpdateInfo(item);
    }

    private void OnClickCreateDraft()
    {
        DateTime currentDate = DateTime.Now;
        string formattedDate = currentDate.ToString("yyyy-MM-dd");
        var name = $"{LocalizationManager.Inst.GetLocalizedText("未命名")}-{formattedDate}";

        if (_studioType == MainViewType.Material)
        {
            LoggerUtils.Log("[Draft] 创建材质");
            mask?.gameObject.SetActive(true);
            Game.Base.GameController.StartGame(EnterGameModel.UgcMaterialEmpty, new GameData.BaseInfo.MaterialInfo()
            {
                name = name,
                templateId = "990100001",
                canvasType = VipDataManager.Inst.isVip?(int)CanvasType.Canvas_64:(int)CanvasType.Canvas_32
            }, true, null);

        } else if (_studioType == MainViewType.Prop)
        {
            mask?.gameObject.SetActive(true);
            Game.Base.GameController.StartGame(EnterGameModel.UgcPropEmpty, new GameData.BaseInfo.PropInfo()
            {
                name = name,
                templateId = "990200001",
            }, true);
        }
    }

    private void OnClickPublishItem(DraftListItem item)
    {
        if (_studioType == MainViewType.Material)
        {
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Mat, item.materialInfo.id,item.materialInfo.ugcStyle);

        } else if (_studioType == MainViewType.Prop)
        {
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel,AssetDetailType.Prop, item.propInfo.id,item.propInfo.ugcStyle);
        }
    }

    public void OnPublishCallBack(bool isSuccess)
    {
        navigationBarTabs.SetSelect(1);
        publishView.RequestDataList();
    }

    private void OnEditClick(DraftListItem item)
    {
        if (_studioType == MainViewType.Material)
        {
            LoggerUtils.Log("[Draft] 编辑材质");
            var materialInfo = item.materialInfo;
            if (materialInfo == null)
            {
                return;
            }

            mask?.gameObject.SetActive(true);
            Game.Base.GameController.StartGame(EnterGameModel.UgcMaterialContinueEdit, materialInfo, true, null);
        } else if (_studioType == MainViewType.Prop)
        {
            LoggerUtils.Log("[Draft] 编辑道具");
            var propInfo = item.propInfo;
            if (propInfo == null)
            {
                return;
            }

            mask?.gameObject.SetActive(true);
            Game.Base.GameController.StartGame(EnterGameModel.UgcPropContinueEdit, propInfo);
        }
    }

    private void OnItemInfoChange(DraftListItem item)
    {
        draftView.EditInfo(item);
    }

    private void CopyGame()
    {
        draftView.RequestDataList();
    }

    private void DeleteGame(DraftListItem draftListItem)
    {
        draftView.DeleteGame(draftListItem);
    }

}
