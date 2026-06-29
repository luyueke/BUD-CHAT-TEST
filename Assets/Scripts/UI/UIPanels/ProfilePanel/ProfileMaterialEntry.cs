using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileMaterialEntry : MonoBehaviour
    {
        public ProfileMaterialAdapter adapter;
        private ProfileMaterialDataLoader dataLoader;
        private List<MaterialResInfo> allModels = new();

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

            adapter.Data = new LazyDataHelper<MaterialResInfo>(adapter, CreateNewModel);
            adapter.Init();
            isInited = true;
        }


        public void SetLoader(ProfileMaterialDataLoader loader)
        {
            dataLoader = loader;
        }

        public void SetActions(Action<MaterialResInfo, Texture> dataAction, Action isEmptyAction)
        {
            adapter.dataAction = dataAction;
            // adapter.isEmptyAction = isEmptyAction;
        }

        public void InitCommunityGameDatas(List<MaterialResInfo> datas)
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
        private MaterialResInfo CreateNewModel(int index)
        {
            if (index >= 0 && index < allModels.Count)
            {
                return allModels[index];
            }
            return new MaterialResInfo();
        }



        public void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                dataLoader.GetPublishList(OnReceivedNewModelsForInsert);
            }
        }


        void OnReceivedNewModelsForInsert(bool isSuccess,List<MaterialResInfo> newModels)
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