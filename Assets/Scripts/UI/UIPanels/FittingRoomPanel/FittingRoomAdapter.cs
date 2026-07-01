using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using frame8.Logic.Misc.Other.Extensions;
using Game.Store;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class FittingRoomAdapter : GridAdapter<FittingRoomGridParams, FittingRoomItemHolder>
    {
        public PullToRefreshBehaviour PullToRefreshBehaviour;
        public LazyDataHelper<GoodsData> Data { get; set; }

        public Action<GoodsData> OnItemSelected;
        public Action OnNearEnd;

        public Color BgColor;
        public Color SelectedColor;

        /// <summary>
        /// 太多地方用这个 Item了，需要识别是否是试衣间
        /// </summary>
        [HideInInspector]
        public bool IsFittingRoomPanel = false;
        private int lastRefreshCount = 0;
        private Dictionary<string, bool> lastItemNewStatus = new Dictionary<string, bool>();

        public void ResetColor()
        {
            ColorUtility.TryParseHtmlString("#EDE8FF", out BgColor);
            ColorUtility.TryParseHtmlString("#FFCD19", out SelectedColor);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void UpdateCellViewsHolder(FittingRoomItemHolder viewsHolder)
        {
            var model = Data.GetOrCreate(viewsHolder.ItemIndex);
            if (model == null)
            {
                return;
            }
            viewsHolder.UpdateColor(BgColor, SelectedColor);
            viewsHolder.UpdateViews(model, IsFittingRoomPanel, OnItemSelected, viewsHolder.ItemIndex);

            if (OnNearEnd != null && Data.Count > 0 && viewsHolder.ItemIndex >= Data.Count - 4)
                OnNearEnd.Invoke();
        }

        protected override void OnCellViewsHolderCreated(FittingRoomItemHolder cellVH, CellGroupViewsHolder<FittingRoomItemHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
        }

        protected override CellGroupViewsHolder<FittingRoomItemHolder> GetNewCellGroupViewsHolder()
        {
            return new FittingRoomCellGroupViewsHolder();
        }

        protected override void UpdateViewsHolder(CellGroupViewsHolder<FittingRoomItemHolder> newOrRecycled)
        {
            base.UpdateViewsHolder(newOrRecycled);

            if (newOrRecycled.NumActiveCells > 0)
            {
                var firstCellVH = newOrRecycled.ContainingCellViewsHolders[0];
                var itemData = Data.GetOrCreate(firstCellVH.ItemIndex);
                if (itemData == null)
                {
                    return;
                }
                var newOrRecycledCasted = newOrRecycled as FittingRoomCellGroupViewsHolder;
                if (itemData.ButtonType == ButtonType.NoMoreTips)
                    newOrRecycledCasted.ShowTips();
                else
                    newOrRecycledCasted.HideTips();
            }
            ScheduleComputeVisibilityTwinPass();
        }

        /// <summary>
        /// 重写Refresh方法，防重复刷新
        /// </summary>
        public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {

            // 检查是否只是新物品状态变化、如果是就不刷新整个列表
            if (Data != null && Data.Count == lastRefreshCount && IsOnlyNewStatusChanged())
            {
                // 即使只是新状态变化，也要更新UI以确保选中状态正确显示
                UpdateSelectionStateOnly();
                return;
            }

            lastRefreshCount = Data != null ? Data.Count : 0;
            
            // 记录当前所有物品的新状态
            RecordCurrentNewStatus();
            base.Refresh(contentPanelEndEdgeStationary, keepVelocity);
        }

        /// <summary>
        /// 重写ResetItems方法，防重复刷新
        /// </summary>
        public override void ResetItems(int itemsCount, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {

            // 检查是否只是新物品状态变化、如果是就不刷新整个列表
            if (Data != null && itemsCount == lastRefreshCount && IsOnlyNewStatusChanged())
            {
                // 即使只是新状态变化，也要更新UI以确保选中状态正确显示
                UpdateSelectionStateOnly();
                return;
            }

            lastRefreshCount = itemsCount;
            
            // 记录当前所有物品的新状态
            RecordCurrentNewStatus();
            base.ResetItems(itemsCount, contentPanelEndEdgeStationary, keepVelocity);
        }

        /// <summary>
        /// 检查是否只是新物品状态发生变化
        /// </summary>
        private bool IsOnlyNewStatusChanged()
        {
            if (Data == null) return false;
            
            var currentItems = new Dictionary<string, bool>();
            
            // 收集当前所有物品的新状态
            for (int i = 0; i < Data.Count; i++)
            {
                var item = Data.GetOrCreate(i);
                if (item != null)
                {
                    currentItems[item.Id] = item.IsNew;
                }
            }
            
            // 比较新旧状态
            if (currentItems.Count != lastItemNewStatus.Count) return false;
            
            foreach (var kvp in currentItems)
            {
                if (!lastItemNewStatus.ContainsKey(kvp.Key) || lastItemNewStatus[kvp.Key] != kvp.Value)
                {
                    // 只有新状态变化，其他字段没变
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 记录当前所有物品的新状态
        /// </summary>
        private void RecordCurrentNewStatus()
        {
            lastItemNewStatus.Clear();
            if (Data == null) return;
            
            for (int i = 0; i < Data.Count; i++)
            {
                var item = Data.GetOrCreate(i);
                if (item != null)
                {
                    lastItemNewStatus[item.Id] = item.IsNew;
                }
            }
        }

        /// <summary>
        /// 只更新选中状态，不刷新整个列表
        /// </summary>
        private void UpdateSelectionStateOnly()
        {
            // 更新所有可见的item的选中状态
            if (VisibleItemsCount > 0)
            {
                for (int i = 0; i < VisibleItemsCount; i++)
                {
                    var groupVH = GetItemViewsHolder(i) as CellGroupViewsHolder<FittingRoomItemHolder>;
                    if (groupVH != null)
                    {
                        for (int j = 0; j < groupVH.NumActiveCells; j++)
                        {
                            var cellVH = groupVH.ContainingCellViewsHolders[j];
                            if (cellVH != null)
                            {
                                // 重新获取数据并更新选中状态
                                var model = Data.GetOrCreate(cellVH.ItemIndex);
                                if (model != null)
                                {
                                    // 重新调用UpdateViews来刷新选中状态
                                    cellVH.UpdateViews(model, IsFittingRoomPanel, OnItemSelected, cellVH.ItemIndex);
                                }
                            }
                        }
                    }
                }
            }
        }

    }

    public class FittingRoomItemHolder : CellViewsHolder
    {
        public FittingRoomItem item;
        public CanvasGroup canvasGroup;

        public override void CollectViews()
        {
            base.CollectViews();
            item = root.GetComponent<FittingRoomItem>();
            canvasGroup = root.GetComponent<CanvasGroup>();
        }

        public void UpdateColor(Color color1, Color color2)
        {
            item.SetStyle(color1, color2);
        }

        public void UpdateViews(GoodsData data, bool isFittingRoom, Action<GoodsData> action , int id)
        {
            if (data == null)
            {
                return;
            }
            
            if (data.ButtonType == ButtonType.NoMoreTips)
            {
                if (canvasGroup != null) canvasGroup.alpha = 0;
            }
            else
            {
                if (canvasGroup != null) canvasGroup.alpha = 1;
                item.UpdateViews(data, isFittingRoom, action , id);
            }
        }
    }

    [Serializable]
    public class FittingRoomGridParams : GridParams
    {
        [SerializeField]
        GameObject CellGroupPrefab = null;

        protected override GameObject CreateCellGroupPrefabGameObject()
        {
            return CellGroupPrefab ? CellGroupPrefab : base.CreateCellGroupPrefabGameObject();
        }
    }

    public class FittingRoomCellGroupViewsHolder : CellGroupViewsHolder<FittingRoomItemHolder>
    {
        public ContentSizeFitter contentSizeFitterComponent;

        Transform tipPanel;

        public override void CollectViews()
        {
            base.CollectViews();

            contentSizeFitterComponent = root.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitterComponent.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitterComponent.enabled = true;

            root.GetComponentAtPath("NoMoreDataTips", out tipPanel);
        }

        public void ShowTips()
        {
            tipPanel?.gameObject.SetActive(true);
        }

        public void HideTips()
        {
            tipPanel?.gameObject.SetActive(false);
        }
    }
}
