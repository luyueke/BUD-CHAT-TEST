using System;
using System.Collections.Generic;
using Game.Base;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.PgcData;
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
public class AvatarStudioDraftsView : AvatarStudioBaseView
{
    public AvatarStudioEntry gameEntry;
    private StudioSubType _studioType = StudioSubType.Drafts;
    private AvatarSubType _skinType;
    private TabView subTabView;
    public Action GotoTemplate;
    public Action GotoPublish;
    private AvatarStudioDraftsDetail _detailInfoView;


    protected override void Init()
    {
        base.Init();
        var bundleconfig = topBarConfig.Find(x=>x.type == AvatarSubType.Bundle);
        topBarConfig.Remove(bundleconfig);
        subTabView = GameObjectEx.FindChildByName(transform, "TabView").GetComponent<TabView>();
        _detailInfoView = GameObjectEx.FindChildByName(transform, "DraftsInfoView").GetComponent<AvatarStudioDraftsDetail>();
        InitGameDetailView();
        _skinType = topBarConfig[0].type;
        var iconAtlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.ClassSprite);
        foreach (var cfg in topBarConfig)
        {
            var item = subTabView.CreateItem(cfg.type.ToString());
            item.SetIsSelect(false);
            var itemIcon = GameObjectEx.FindChildByName(item.transform, "Icon").GetComponent<Image>();
            itemIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(iconAtlasPath, cfg.path, gameObject);
        }
        subTabView.SetSelect(0);
        subTabView.AddItemSelectCallBack(OnTogChange);
        MessageHelper.AddListener<SkinDraftInfo>(DraftMessage.DraftSaveStatus, DraftRefresh);
    }
    protected override void OnViewShow()
    {
        //打开界面默认拉第一个选项
        subTabView.SelectWithoutCallback(0);
        _skinType = topBarConfig[0].type;
        RequestDataList();
    }
    protected override void OnViewHide ()
    {

    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<SkinDraftInfo>(DraftMessage.DraftSaveStatus, DraftRefresh);
    }

   
    private void DraftRefresh(SkinDraftInfo draftInfo)
    {
        if (IsShow()&&gameObject.activeInHierarchy)
        {
            RequestDataList();
        }
    }
    private void OnTogChange(TabItem item, int i)
    {
        _skinType = topBarConfig[i].type;
        RequestDataList();
    }
    /// <summary>
    /// 重新请求数据
    /// </summary>
    public void RequestDataList()
    {
        gameEntry.Init(currentStyle);
        gameEntry.SetActions(OnStudioItemClick, GoToTemplateView, IsEmptyAction, _studioType,_skinType, CurrencyType.PinkCoin);
        gameEntry.GetFirstPageDatas(GetPageDatas);
    }

    private void GetPageDatas(List<DraftListItem> infos)
    {

    }

    private void InitGameDetailView()
    {
        _detailInfoView.InitUI();
        _detailInfoView.InitAction(OnPublishCallBack,OnEditClick,OnItemInfoChange,CopyGame,DeleteGame,RetrySave);
    }

    public void OnPublishCallBack(bool isSuccess)
    {
        GotoPublish?.Invoke();
    }
    public void OnEditClick(DraftListItem item)
    {
        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(new SkinInfo()
        {
            name = item.skinInfo.name,
            cover = item.skinInfo.cover
        },item.creator ,LoadingType.Cloth);
        GameController.StartGame(EnterGameModel.UgcSkinContinueEdit,  item.skinInfo, true, item.skinInfo.isProp ? GameController.MapScene : null);

    }
    private void EditInfo(DraftListItem draftListItem)
    {
        if (GameStudioUtils.GetBaseInfo(draftListItem) != null)
        {
            UpdateSingleItem(draftListItem);
        }
    }
    private void DeleteGame(DraftListItem draftListItem)
    {
        UgcBaseInfo ugcBaseInfo = GameStudioUtils.GetBaseInfo(draftListItem);
        if (ugcBaseInfo != null)
        {
            if (ugcBaseInfo.isLocal)
            {
                //删除本地文件
                MainViewType mainViewType = GameStudioUtils.GetMainViewType(draftListItem);
                if (mainViewType == MainViewType.Cloth)
                {
                    SkinAssetManager.Inst.DeleteDraftInfo(ugcBaseInfo.id);
                }
            }
            RemoveSingleItem(ugcBaseInfo.id);
        }
    }
    private void RetrySave(DraftListItem draftListItem)
    {
        var draftInfo = SkinAssetManager.Inst.GetOrCreateDraftInfo(draftListItem.skinInfo);
        if (draftInfo != null) 
        {
            draftInfo.UploadAndSave((info, isSuccess) => {
                TipPanel.ShowToast("保存成功:D");
            });
        }
    }
    public void RemoveSingleItem(string mapId)
    {
        if (gameEntry != null)
        {
            gameEntry.RemoveSingleItem(mapId);
        }
    }
    private void CopyGame()
    {
        RequestDataList();
    }
    public void OnItemInfoChange(DraftListItem item)
    {
        EditInfo(item);
    }
    public void UpdateSingleItem(DraftListItem draftListItem)
    {
        if (gameEntry != null && draftListItem != null)
        {
            gameEntry.UpdateSingleItem(draftListItem);
            UpdateDetailView(draftListItem);
        }
    }


    private void OnStudioItemClick(DraftListItem item)
    {
        UpdateDetailView(item);
    }

    private void IsEmptyAction()
    {
        _detailInfoView.Hide();
    }
    /// <summary>
    /// 显示有右边detail布局
    /// </summary>
    /// <param name="mapInfo"></param>
    private void UpdateDetailView(DraftListItem draftListItem)
    {
        if (draftListItem != null)
        {
            //调用接口获取草稿状态
            _detailInfoView.UpdateInfo(draftListItem);
        }
    }
    /// <summary>
    /// 前往模版创建view
    /// </summary>
    /// <param name="item"></param>
    /// <param name="gameStudioItem"></param>
    private void GoToTemplateView()
    {
        GotoTemplate?.Invoke();
    }



}

