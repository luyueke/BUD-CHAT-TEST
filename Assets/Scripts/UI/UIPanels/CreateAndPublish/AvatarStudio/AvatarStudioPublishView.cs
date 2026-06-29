using System;
using System.Collections.Generic;
using GameData.PgcData;
using UI;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AvatarStudioPublishView : AvatarStudioBaseView
{
    public AvatarStudioEntry gameEntry;
    private StudioSubType _studioType = StudioSubType.Published;
    private AvatarSubType _skinType;
    private TabView subTabView;
    private GameObject empty;
    protected override void Init()
    {
        base.Init();
        empty = GameObjectEx.FindChildByName(transform,"empty").gameObject;
        subTabView = GameObjectEx.FindChildByName(transform, "TabView").GetComponent<TabView>();
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
        
        
    }
    protected override void OnViewShow()
    {
        InitListener(true);
        //打开界面默认拉第一个选项
        subTabView.SelectWithoutCallback(0);
        _skinType = topBarConfig[0].type;
        RequestDataList();
    }
    protected override void OnViewHide ()
    {
        InitListener(false);
    }

    private void OnDestroy()
    {
        InitListener(false);
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
        empty.gameObject.SetActive(false);
        gameEntry.Init(currentStyle);
        gameEntry.SetActions(OnStudioItemClick, GoToTemplateView, IsEmptyAction, _studioType,_skinType, CurrencyType.PinkCoin);
        gameEntry.GetFirstPageDatas(GetPageDatas);
    }

    private void GetPageDatas(List<DraftListItem> infos)
    {

        empty.gameObject.SetActive(infos==null||infos.Count==0);
    }

    
    private void OnStudioItemClick(DraftListItem item)
    {
        var isBundle = item != null && item.skinInfo != null && item.skinInfo.subType == (int)AvatarSubType.Bundle;
        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, isBundle ? AssetDetailType.UgcBundle : AssetDetailType.Skin, item.skinInfo.id,item.skinInfo.ugcStyle);
    }
    
    private void IsEmptyAction()
    {
      
    }
   
    /// <summary>
    /// 前往模版创建view
    /// </summary>
    /// <param name="item"></param>
    /// <param name="gameStudioItem"></param>
    private void GoToTemplateView()
    {
        
    }

   
    private void InitListener(bool load)
    {
        // if (load)
        // {
        //     MessageHelper.AddListener<MapDraftInfo>(DraftMessage.DraftSaveStatus, OnMapDraftSaveCallBack);
        //     MessageHelper.AddListener<PropDraftInfo>(DraftMessage.DraftSaveStatus, OnPropDraftSaveCallback);
        //     MessageHelper.AddListener(DraftMessage.RefreshDraft, OnMapRefreshCallback);
        // }
        // else
        // {
        //     MessageHelper.RemoveListener<MapDraftInfo>(DraftMessage.DraftSaveStatus, OnMapDraftSaveCallBack);
        //     MessageHelper.RemoveListener<PropDraftInfo>(DraftMessage.DraftSaveStatus, OnPropDraftSaveCallback);
        //     MessageHelper.RemoveListener(DraftMessage.RefreshDraft, OnMapRefreshCallback);
        // }
    }

}
