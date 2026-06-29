using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileCharacterBoxEntry : MonoBehaviour
    {
        public ProfileCharacterBoxAdapter adapter;
        private List<CharacterBoxInfo> allModels = new();
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
            adapter.Data = new LazyDataHelper<CharacterBoxInfo>(adapter, CreateModel);
            adapter.Init();
            isInited = true;
        }

        public void SetActions(Action<CharacterBoxInfo> dataAction)
        {
            adapter.dataAction = dataAction;
        }

        public void InitData(List<CharacterBoxInfo> datas)
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

        private CharacterBoxInfo CreateModel(int index)
        {
            return index >= 0 && index < allModels.Count ? allModels[index] : new CharacterBoxInfo();
        }

        private void OnPullReleased(float sign) { }
    }
}
