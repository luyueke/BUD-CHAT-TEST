using System;
using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using UnityEngine;
using UnityEngine.UI;

public class TimbreCommunityView : MonoBehaviour
{
    [SerializeField] private TimbreCommunityEntry _entry;
    [SerializeField] private Transform tabViewContent;
    [SerializeField] private TimbreCommunityTabItem tabItemView;
    [SerializeField] private Text EmptyText;
    
    private Dictionary<string, TimbreCommunityTabItem> _tabItemInfo = new Dictionary<string, TimbreCommunityTabItem>();
    private List<string> tabNames = new List<string>() { "我发布的", "我购买的" };
    private TimbreStoreViewType pageType = TimbreStoreViewType.Publish;

    private Action<ToneInfo> act;
    private bool InitFlag = false;
    public void InitOnce()
    {
        if (InitFlag)
        {
            return;
        }

        InitFlag = true;
        
        for (int i = 0; i < tabNames.Count; i++)
        {
            var key = tabNames[i];
            var itemView = Instantiate(tabItemView, tabViewContent);
            itemView.gameObject.SetActive(true);
            itemView.SetData(key, OnCategoryItemClick);
            _tabItemInfo[key] = itemView;
        }

        if (tabNames.Count > 0)
        {
            OnCategoryItemClick(tabNames[0]);
            RefreshPage(pageType);
        }
    }
    private void Start()
    {
        InitOnce();
    }

    private void OnCategoryItemClick(string key)
    {
        foreach (var element in _tabItemInfo)
        {
            element.Value.UpdateSelected(element.Key == key);
        }
        var index = tabNames.FindIndex(x => x == key);
        if ((int)pageType == index)
        {
            return;
        }
        RefreshPage((TimbreStoreViewType)index);
    }

    public void SetOnToneItemSelectAct(Action<ToneInfo> act)
    {
        this.act = act;
    }
    
    public void ShowAndReloadPublish()
    {

        if (pageType == TimbreStoreViewType.Publish)
        {
            RefreshPage(viewType: TimbreStoreViewType.Publish);
        }
        else
        {
            if (tabNames.Count > 0)
            {
                OnCategoryItemClick(tabNames[0]);
            }
        }
    }
    
    private void RefreshPage(TimbreStoreViewType viewType)
    {
        pageType = viewType;
        _entry.SetActions(pageType: viewType, OnSelectItem, OnShowEmpty);
        _entry.GetFirstPageDatas();
    }

    private void OnSelectItem(ToneFixedInfo info)
    {
        if (info.style == ToneFixedInfo.ToneFixedStyle.Store)
        {
            var panel = UIManager.Inst.OpenPanel<TimbreStorePanel>(PanelId.TimbreStorePanel);
            panel.DidPurchasedAction = UgcId =>
            {
                if (this == null)
                {
                    return;
                }
                if (string.IsNullOrEmpty(UgcId))
                {
                    return;
                }

                if (pageType == TimbreStoreViewType.Purchased)
                {
                    RefreshPage(viewType: TimbreStoreViewType.Purchased);
                }
            };
        } else if (info.style == ToneFixedInfo.ToneFixedStyle.Publish)
        {
            UIManager.Inst.OpenPanel(PanelId.PublishTonePanel);
        }
        else
        {
            act?.Invoke(info?.toneInfo);
            var UgcID = info?.toneInfo?.id;
            if (string.IsNullOrEmpty(UgcID))
            {
                return;
            }
            
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel,AssetDetailType.MusicTone, UgcID);
        }
    }

    private void OnShowEmpty()
    {
        
    }
    
}
