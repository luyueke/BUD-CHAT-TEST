using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.PgcData;
using UI.BaseWidgets;
using UnityEngine.UI;

namespace Game.AINPCStudio
{
    public class AINpcOwnedPublishedPanel : MonoBehaviour
    {
        public AINpcOwnedPublishedAdapter Adapter;
        public PullToRefreshBehaviour refreshController;
        public AINPCStudioDataLoader DataLoader;
        private NpcStoreEnterType _curEnterType = NpcStoreEnterType.Store;
        
        public void InitEnterMode(NpcStoreEnterType enterType)
        {
            this._curEnterType = enterType;
        }

        private void Start()
        {
            //需要动态拉取数据必须要做的初始化操作
            refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
            Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
            Adapter.Data = new SimpleDataHelper<DraftListItem>(Adapter);
            Adapter.Init();
        }

        public void GetData()
        {
            HideGizmo();
            DataLoader.InitData(StudioSubType.Published);
            DataLoader.GetInstrumentStudioList(OnGetFirstPageDatas);
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
        
        public virtual void OnGetFirstPageDatas(List<DraftListItem> ListDatas)
        {
            ResetAdpater();
            if (ListDatas == null || ListDatas.Count == 0)
            {
                ListDatas = new List<DraftListItem>();
            }

            if (_curEnterType == NpcStoreEnterType.Store)
            {
                ListDatas.Insert(0, new DraftListItem());
            }
            
            Adapter.Data.ResetItems(ListDatas);
            Adapter.OnItemsUpdated?.Invoke();
        }
        
        private void OnPullReleased()
        {
            DataLoader.GetInstrumentStudioList(OnReceivedNewModelsForInsert);
        }
        
        private void OnReceivedNewModelsForInsert(List<DraftListItem> ListDatas)
        {
            if (ListDatas == null || ListDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }
            Adapter.Data.List.AddRange(ListDatas);
            Adapter.Refresh(false);
        }
    }
}