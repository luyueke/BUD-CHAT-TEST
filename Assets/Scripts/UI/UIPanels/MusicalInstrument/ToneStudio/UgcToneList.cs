using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.BaseInfo;

namespace Game.MusicalInstrument
{
    public class UgcToneList : MonoBehaviour
    {
        public UgcToneAdapter Adapter;
        public PullToRefreshBehaviour refreshController;
        public UgcToneDataLoader DataLoader;
        public GameObject EmptyTip;
        
        public void SetOnToneItemSelectAct(Action<ToneInfo> act)
        {
            Adapter.SetOnToneItemSelectAct(act);
        }
        
        private void Start()
        {
            //需要动态拉取数据必须要做的初始化操作
            refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
            Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
            Adapter.Data = new SimpleDataHelper<ToneItemData>(Adapter);
            Adapter.Init();
        }

        public void GetPublishedData()
        {
            HideGizmo();
            Adapter.SetDeleteState(false);
            DataLoader.ResetCookie();
            DataLoader.GetTonePublishedData(OnGetFirstPageDatas);
        }
        
        public void ResetAdpater()
        {
            if(!Adapter.IsInitialized)
                return;
            // Resetting to 0 count clears everything, including visible items, so nothing will be recycled
            Adapter.ResetItems(0);
        }
        
        private void HideGizmo()
        {
            //需要动态拉取数据必须要做的初始化操作
            refreshController.HideGizmo();
        }
        
        public virtual void OnGetFirstPageDatas(List<ToneItemData> tonePublishedDatas)
        {
            ResetAdpater();
            if (tonePublishedDatas == null || tonePublishedDatas.Count == 0)
            {
                tonePublishedDatas = new List<ToneItemData>();
                EmptyTip.gameObject.SetActive(true);
            }
            else
            {
                EmptyTip.gameObject.SetActive(false);
            }
            Adapter.Data.ResetItems(tonePublishedDatas);
            Adapter.OnItemsUpdated?.Invoke();
        }
        
        private void OnPullReleased()
        {
            DataLoader.GetTonePublishedData(OnReceivedNewModelsForInsert);
        }
        
        private void OnReceivedNewModelsForInsert(List<ToneItemData> tonePublishedDatas)
        {
            if (tonePublishedDatas == null || tonePublishedDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }
            Adapter.Data.List.AddRange(tonePublishedDatas);
            Adapter.Refresh(false);
        }
    }
}
