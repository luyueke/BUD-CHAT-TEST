using System;
using System.Collections.Generic;
using BUD.GameStudio;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData;
using GameData.Base;
using GameData.UGCData;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfilePropEntry : MonoBehaviour
    {
        public ProfilePropAdapter adapter;
        private ProfilePropDataLoader dataLoader;
        private List<PropResInfo> allModels = new();

        private string _currenUid;
        private bool isInited = false;

        protected void Start()
        {
            //需要动态拉取数据必须要做的初始化操作
            InitView();
        }
        
        private void InitView()
        {
            if (isInited) return;
            PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
            refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
            adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);

            adapter.Data = new LazyDataHelper<PropResInfo>(adapter, CreateNewModel);
            adapter.Init();
            isInited = true;
        }


        public void SetLoader(ProfilePropDataLoader loader)
        {
            dataLoader = loader;
        }

        public void SetActions(Action<PropResInfo, Texture> dataAction, Action isEmptyAction)
        {
            adapter.dataAction = dataAction;
            // adapter.isEmptyAction = isEmptyAction;
        }

        public void InitCommunityGameDatas(List<PropResInfo> datas)
        {
            InitView();
            if (datas == null || datas.Count == 0)
            {
                adapter.OnItemsUpdated?.Invoke();
                return;
            }

            allModels = datas;
            adapter.Data.ResetItems(datas.Count, false);
        }

        /// <summary>
        /// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private PropResInfo CreateNewModel(int index)
        {
            if (index >= 0 && index < allModels.Count)
            {
                return allModels[index];
            }

            return new PropResInfo();
        }



        public void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                dataLoader.GetPublishList(OnReceivedNewModelsForInsert);
            }
        }


        void OnReceivedNewModelsForInsert(bool isSuccess,List<PropResInfo> newModels)
        {
            if (newModels == null || newModels.Count == 0)
            {
                adapter.OnItemsUpdated?.Invoke();
                return;
            }

            adapter.Data.List.AddRange(newModels);
            // adapter.OnItemsUpdated?.Invoke();
            adapter.Refresh(false);
        }
    }
}