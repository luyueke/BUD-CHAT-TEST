using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileAICharacterEntry : MonoBehaviour
    {
        public ProfileAICharacterAdapter adapter;
        private ProfileAICharacterDataLoader dataLoader;
        private List<CabinCharacterUgcInfo> allModels = new();
        private bool isInited;

        protected void Start()
        {
            InitView();
        }

        private void InitView()
        {
            if (isInited) return;
            PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
            refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
            adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
            adapter.Data = new LazyDataHelper<CabinCharacterUgcInfo>(adapter, CreateModel);
            adapter.Init();
            isInited = true;
        }

        public void SetLoader(ProfileAICharacterDataLoader loader)
        {
            dataLoader = loader;
        }

        public void SetActions(Action<CabinCharacterUgcInfo> dataAction)
        {
            adapter.dataAction = dataAction;
        }

        public void InitData(List<CabinCharacterUgcInfo> datas)
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

        private CabinCharacterUgcInfo CreateModel(int index)
        {
            return index >= 0 && index < allModels.Count ? allModels[index] : new CabinCharacterUgcInfo();
        }

        private void OnPullReleased(float sign)
        {
            // 无分页，下拉刷新时重新拉取全量数据
            if (sign < 0 && dataLoader != null)
                dataLoader.FetchPublished(dataLoader.ToUid, OnRefreshReceived);
        }

        private void OnRefreshReceived(bool success, List<CabinPublishData> list)
        {
            if (!success || list == null) return;
            var infos = new List<CabinCharacterUgcInfo>();
            foreach (var d in list)
                if (d?.characterInfo != null) infos.Add(d.characterInfo);
            InitData(infos);
        }
    }
}
