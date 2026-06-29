using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.CommunityGame;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.AINPCStudio
{
    public class AIBuddySelectPanel : BasePanel<AIBuddySelectPanel>
    {
        public AIBuddySelectAdapter Adapter;
        public PullToRefreshBehaviour refreshController;
        public AIBuddySelectDataLoader DataLoader;
        private GameObject _emptyView;
        private CButton backBtn;
        private CButton goMarketBtn;

        public override void OnCreate()
        {
            BindUI();
            backBtn.onClick.AddListener(OnBackClick);
            goMarketBtn.onClick.AddListener(OnGoMarketClick);
            _emptyView.SetActive(false);
        }

        private void BindUI()
        {
            backBtn = GameObjectEx.FindComponentByName<CButton>(this.transform, "BackButton");
            _emptyView = GameObjectEx.FindChildByName(this.transform, "emptyViewBig").gameObject;
            goMarketBtn = GameObjectEx.FindComponentByName<CButton>(this.transform, "Btn_GoMarket");
            // _currencyPicker = GameObjectEx.FindChildByName(this.transform, "CurrencySelectRoot").GetComponent<CurrencyPicker>();
        }

        private void OnBackClick()
        {
            CloseSelf();
        }

        private void OnGoMarketClick()
        {
            var uiPanel = UIManager.Inst.OpenPanel(PanelId.AINpcStorePanel);
            uiPanel.SetPanelActions(null, GetData);
            Adapter.Data.List.Clear();
            DataLoader.ResetCookie();
        }


        public override void OnShow(params object[] args)
        {
            //需要动态拉取数据必须要做的初始化操作
            refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
            Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
            Adapter.Data = new SimpleDataHelper<AIBuddySelectItemData>(Adapter);
            Adapter.Init();
            GetData();
        }

        public override void OnWindowShow()
        {
        }

        private void OnPullReleased()
        {
            DataLoader.GetDatas(OnReceivedNewModelsForInsert);
        }

        private void GetData()
        {
            HideGizmo();
            DataLoader.GetDatas(OnReceivedNewModelsForInsert);
        }

        private void HideGizmo()
        {
            //需要动态拉取数据必须要做的初始化操作
            refreshController.HideGizmo();
        }

        private void OnReceivedNewModelsForInsert(List<AIBuddySelectItemData> aiBuddyDatas)
        {
            if (aiBuddyDatas == null || aiBuddyDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                _emptyView.SetActive(true);
                return;
            }
            Adapter.Data.List.AddRange(aiBuddyDatas);
            Adapter.Refresh(false);
            _emptyView.SetActive(false);
        }

        public void SetItemOnClickAct(Action<AIBuddySelectItemData> act)
        {
            Adapter.OnSelectItemAct += act;
            Adapter.OnSelectItemAct += OnAIBuddySelect;
        }

        private void OnAIBuddySelect(AIBuddySelectItemData act)
        {
            CloseSelf();
        }

    }
}
