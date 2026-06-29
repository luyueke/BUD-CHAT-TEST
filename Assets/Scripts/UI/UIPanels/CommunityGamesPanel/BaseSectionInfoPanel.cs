using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Store;

namespace Game.CommunityGame
{
    public class BaseSectionInfoPanel : MonoBehaviour
    {
        public BaseSectionInfoAdapter Adapter;
        public PullToRefreshBehaviour refreshController;
        public SectionInfoDataLoader DataLoader;
        private string _curSectionId;
        private int _curCurrencyType;
        private Action _onGetData;
        private Action<bool> _onHasData;
        
        private void Awake()
        {
            // Awake 在首次激活时立即执行，即使同帧内再次 SetActive(false) 也能保证初始化完成
            Adapter.Data = new SimpleDataHelper<RecommendItemData>(Adapter);
            Adapter.Init();
            refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
            Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        }

        public void OnSelectSection(string sectionId, int currencyType, Action act = null, Action<bool> hasData = null)
        {
            this._curSectionId = sectionId;
            this._onGetData = act;
            this._onHasData = hasData;
            this._curCurrencyType = currencyType;
            HideGizmo();
            DataLoader.RefreshSectionId(this._curSectionId, currencyType);
            DataLoader.GetSectionInfoData(OnGetFirstPageDatas);
        }
        
        public void ResetAdpater()
        {
            if(!Adapter.IsInitialized)
                return;
            // Resetting to 0 count clears everything, including visible items, so nothing will be recycled
            Adapter.ResetItems(0);
            Adapter.ClearPool();
        }
        
        private void HideGizmo()
        {
            //需要动态拉取数据必须要做的初始化操作
            refreshController.HideGizmo();
        }
        
        public virtual void OnGetFirstPageDatas(List<RecommendItemData> recommendItemDatas)
        {
            ResetAdpater();
            this._onGetData?.Invoke();
            if (recommendItemDatas == null || recommendItemDatas.Count == 0)
            {
                this._onHasData?.Invoke(false);
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }
            this._onHasData?.Invoke(true);
            Adapter.Data.ResetItems(recommendItemDatas);
            Adapter.OnItemsUpdated?.Invoke();
        }
        
        private void OnPullReleased()
        {
            DataLoader.GetSectionInfoData(OnReceivedNewModelsForInsert);
        }
        
        private void OnReceivedNewModelsForInsert(List<RecommendItemData> recommendItemDatas)
        {
            if (recommendItemDatas == null || recommendItemDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }
            Adapter.Data.List.AddRange(recommendItemDatas);
            Adapter.Refresh(false);
        }
    }
}
