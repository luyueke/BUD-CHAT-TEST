using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Base;
using Game.Props.PropsManagers;
using GameData;
using GameData.MapData;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.AssetToolBox
{
    public class PublishAssetOSAView : MonoBehaviour
    {
        public CButton Btn_Bottom;
        public PullToRefreshBehaviour RefreshCtr;
        public PublishAssetOSAAdapter Adapter;
        public PublishAssetBaseDataLoader DataLoader;
        public bool IsInit = false;
        private List<PropResInfo> allDatas = new List<PropResInfo>();
        private Action _bottomBtnAct;
        public GameObject NoneTips;
        public List<PropResInfo> AllDatas => allDatas;

        public void Start()
        {
            //需要动态拉取数据必须要做的初始化操作
            RefreshCtr.OnRefreshWithSign.AddListener(OnPullReleased);
            
            ResetData();
            Adapter.Init();

            if(Btn_Bottom)
                Btn_Bottom.onClick.AddListener(OnBottomBtnClick);

            GetFirstPageDatas();
            IsInit = true;
        }

        public void OnEnable()
        {
            GetFirstPageDatas();
        }

        private void ResetData()
        {
            allDatas.Clear();
            DataLoader.ResetCookie();
            Adapter.OnItemsUpdated.RemoveAllListeners();
            Adapter.OnItemsUpdated.AddListener(RefreshCtr.HideGizmo);
            Adapter.Data = new LazyDataHelper<PropResInfo>(Adapter, CreateNewModel);
        }

        public void SetBottomBtnAct(Action act)
        {
            this._bottomBtnAct = act;
        }

        public void SetOSAItemClickAct(Action<PropResInfo> act)
        {
            Adapter.SetOnSelectAct(act);
        }

        public void GetFirstPageDatas()
        {
            ResetData();
            DataLoader.GetDatas((datas) =>
            {
                if (datas == null || datas.Count == 0)
                {
                    NoneTips.SetActive(true);
                    return;
                }
                NoneTips.SetActive(false);
                allDatas.AddRange(datas);
                Adapter.Data.ResetItems(datas.Count, false);
                Adapter.OnItemsUpdated?.Invoke();
            });
        }

        /// <summary>
        /// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private PropResInfo CreateNewModel(int index)
        {
            if (index >= 0 && index < allDatas.Count)
            {
                return allDatas[index];
            }

            return new PropResInfo();
        }

        private void OnReceivedNewModelsForInsert(bool isSuccess, List<PropResInfo> newDatas)
        {
            if (newDatas == null || newDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }

            Adapter.Data.List.AddRange(newDatas);
            Adapter.Refresh(false);
        }

        private void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                DataLoader.GetPublishList(OnReceivedNewModelsForInsert);
            }
        }

        private void OnBottomBtnClick()
        {
            this._bottomBtnAct?.Invoke();
        }
    }
}
