using System;
using System.Linq;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Store;
using Newtonsoft.Json;
using UnityEngine;


    public class AIBuddyGiftAdapter : GridAdapter<GridParams, AIBuddyGiftHolder>
    {
        public SimpleDataHelper<GoodsData> Data;
        private GoodsData mLastSelectData = null;
        private Action<GoodsData> onItemSelectListener;
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Data = new SimpleDataHelper<GoodsData>(this);
        }

        protected override void OnCellViewsHolderCreated(AIBuddyGiftHolder cellVH, CellGroupViewsHolder<AIBuddyGiftHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
          
        }

        protected override void UpdateCellViewsHolder(AIBuddyGiftHolder viewsHolder)
        {
            var model = Data[viewsHolder.ItemIndex];
            viewsHolder.UpdateViews(model);
            viewsHolder.SetClickListener(() =>
            {
                OnItemClick(viewsHolder);
            });
        }

        private void SelectItem(GoodsData itemData)
        {
            if (mLastSelectData != null)
            {
                mLastSelectData.Selected = false;
            }
            itemData.Selected = true;
            mLastSelectData = itemData;
            Refresh();
            onItemSelectListener?.Invoke(itemData);
        }

        private void OnItemClick(AIBuddyGiftHolder itemHolder)
        {
            var itemData = Data[itemHolder.ItemIndex];
            SelectItem(itemData);
        }

        public void AddItemClickListener(Action<GoodsData> callback)
        {
            onItemSelectListener += callback;
        }

        public void DefaultSelect()
        {
            if (Data.Count == 0) return;
            
            foreach (var goodData in Data)
            {
                goodData.Selected = false;
            }
            var itemData = Data.First();
            SelectItem(itemData);
        }
    }

    public class AIBuddyGiftHolder : CellViewsHolder
	{
        public AIBuddyGiftItem GiftItem;

        public override void CollectViews()
        {
            base.CollectViews();
            GiftItem = views.GetComponentInParent<AIBuddyGiftItem>(true);
        }
        
        public void UpdateViews(GoodsData data)
        {
            GiftItem?.SetData(data);
        }

        public void SetClickListener(Action callback)
        {
            GiftItem.SetClickListener(callback);
        }

        public void SetSendClickListener(Action callback)
        {
            GiftItem.SetSendClickListener(callback);
        }
    }
