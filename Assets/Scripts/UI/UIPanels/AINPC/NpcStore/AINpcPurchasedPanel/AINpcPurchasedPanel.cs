using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Store;


namespace Game.AINPCStudio
{
    public class AINpcPurchasedPanel : MonoBehaviour
    {
        public AINpcPurchasedAdapter Adapter;
        public PullToRefreshBehaviour refreshController;
        public AINpcPurchasedDataLoader DataLoader;
        private Action _onGetData;
        private Action<bool> _onHasData;
        
        private void Start()
        {
            //需要动态拉取数据必须要做的初始化操作
            refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
            Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
            Adapter.Data = new SimpleDataHelper<AINpcPurchasedItemData>(Adapter);
            Adapter.Init();
        }

        public void GetData(Action act = null, Action<bool> hasData = null)
        {
            this._onGetData = act;
            this._onHasData = hasData;
            HideGizmo();
            DataLoader.ResetCookie();
            DataLoader.GetDatas(OnGetFirstPageDatas);
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
        
        public virtual void OnGetFirstPageDatas(List<AINpcPurchasedItemData> purchasedItemDatas)
        {
            ResetAdpater();
            this._onGetData?.Invoke();
            if (purchasedItemDatas == null || purchasedItemDatas.Count == 0)
            {
                this._onHasData?.Invoke(false);
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }
            this._onHasData?.Invoke(true);
            Adapter.Data.ResetItems(purchasedItemDatas);
            Adapter.OnItemsUpdated?.Invoke();
        }
        
        private void OnPullReleased()
        {
            DataLoader.GetDatas(OnReceivedNewModelsForInsert);
        }
        
        private void OnReceivedNewModelsForInsert(List<AINpcPurchasedItemData> purchasedItemDatas)
        {
            if (purchasedItemDatas == null || purchasedItemDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }
            Adapter.Data.List.AddRange(purchasedItemDatas);
            Adapter.Refresh(false);
        }
    }
}