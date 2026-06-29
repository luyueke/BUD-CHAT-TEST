using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Avatar;
using Game.Store;
using GameData.Account;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine.UI;
using Game.Event;

namespace Game.AINPCStudio
{
    public class AIBuddyOwnedView : MonoBehaviour
    {
        public GameObject emptyTip;
        public CButton GoStoreBtn;
        public CButton Btn_Confirm;
        public AIBuddyOwnedAdapter Adapter;
        public PullToRefreshBehaviour refreshController;
        public AIBuddyOwnedDataLoader DataLoader;
        private Action _onGetData;
        private Action<bool> _onHasData;

        private AIBuddyInfo _curSelectData;
        
        private void Start()
        {
            this._curSelectData = AIBuddyAvatarController.Inst.SelfAIBuddyInfo;
            
            GoStoreBtn.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanel(PanelId.AINpcStorePanel);
            });
            
            Btn_Confirm.onClick.AddListener(OnBtnConfirmClick);

            BindOnClickAction();
            
            //需要动态拉取数据必须要做的初始化操作
            refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
            Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
            Adapter.Data = new SimpleDataHelper<AIBuddyInfo>(Adapter);
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
        
        public virtual void OnGetFirstPageDatas(List<AIBuddyInfo> purchasedItemDatas)
        {
            ResetAdpater();
            this._onGetData?.Invoke();
            if (purchasedItemDatas == null || purchasedItemDatas.Count == 0)
            {
                purchasedItemDatas = new List<AIBuddyInfo>();
            }
            purchasedItemDatas.Insert(0, new AIBuddyInfo(){id = ""});
            HandleHasData(true);
            Adapter.Data.ResetItems(purchasedItemDatas);
            Adapter.OnItemsUpdated?.Invoke();
        }
        
        private void OnPullReleased()
        {
            DataLoader.GetDatas(OnReceivedNewModelsForInsert);
        }
        
        private void OnReceivedNewModelsForInsert(List<AIBuddyInfo> purchasedItemDatas)
        {
            if (purchasedItemDatas == null || purchasedItemDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }
            Adapter.Data.List.AddRange(purchasedItemDatas);
            Adapter.Refresh(false);
        }

        private void HandleHasData(bool hasData)
        {
            this._onHasData?.Invoke(hasData);
            emptyTip.gameObject.SetActive(!hasData);
            GoStoreBtn.gameObject.SetActive(!hasData);
        }

        private void BindOnClickAction()
        {
            Adapter.SetOnClickAction(OnPurchasedItemClick);
            Adapter.SetGetSelectedIdFunc(GetCurSelectedId);
        }
        
        
        
        public void OnPurchasedItemClick(AIBuddyInfo data)
        {
            this._curSelectData = data;
            Btn_Confirm.gameObject.SetActive(this._curSelectData != null);
            
            var panel = UIManager.Inst.FindPanel<GameGuestPanel>(PanelId.GameGuestPanel);
            if (panel != null)
            {
                panel.SetAIBuddyChatReddot(false);
            }
        }

        private void OnBtnConfirmClick()
        {
            if (AIBuddyAvatarController.Inst.SelfStateController != null && AIBuddyAvatarController.Inst.SelfStateController.IsInDoubleEmote())
            {
                TipPanel.ShowToast("请先解除双人动作");
                return;
            }
            
            if (AvatarController.Inst.SelfStateController.IsInLinkAIBuddy())
            {
                TipPanel.ShowToast("请先解除牵手状态");
                return;
            }
            
            GameAIBuddyManager.Inst.CallSelfAIBuddy(this._curSelectData);
            //上报事件
            EventCenterDataManager.Inst.ReportTask(PostEventId.CallAIBuddyInMap);
            
            UIManager.Inst.ClosePanel(PanelId.AIBuddyOptionPanel);
            
            var panel = UIManager.Inst.FindPanel<GameGuestPanel>(PanelId.GameGuestPanel);
            if (panel != null)
            {
                panel.SetAIBuddyChatReddot(true);
            }
        }

        public string GetCurSelectedId()
        {
            if (this._curSelectData != null)
                return this._curSelectData.id;

            return "";
        }
    }
}