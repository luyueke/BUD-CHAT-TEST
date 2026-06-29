using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Store;
using UnityEngine;

namespace UI.UIPanels.IncubationCabin
{
    public class ToneShopOSAAdapter : GridAdapter<ToneShopGridParams, ToneShopItemViewsHolder>
    {
        public SimpleDataHelper<RecommendItemData> Data { get; private set; }

        /// <summary>
        /// 上滑加载更多组件（在 Prefab 中绑定到 OSA ScrollRect 上的 PullToRefreshBehaviour）。
        /// 面板通过它的 OnRefreshWithSlideUp 事件挂监听，触发加载下一页。
        /// </summary>
        public PullToRefreshBehaviour PullToRefreshBehaviour;

        private Action<RecommendItemData> _onItemClick;
        private string _selectedId = string.Empty;

        protected override void Start()
        {
            Data = new SimpleDataHelper<RecommendItemData>(this);
            base.Start();
        }

        public void SetOnItemClick(Action<RecommendItemData> onItemClick)
        {
            _onItemClick = onItemClick;
        }

        public void ClearSelection()
        {
            _selectedId = string.Empty;
        }

        protected override void UpdateCellViewsHolder(ToneShopItemViewsHolder newOrRecycled)
        {
            var data = Data[newOrRecycled.ItemIndex];
            bool isSelected = !string.IsNullOrEmpty(_selectedId)
                && data != null
                && data.ugcId == _selectedId;

            newOrRecycled.Item.SetData(data, onSelect: OnItemClickInternal);
            newOrRecycled.Item.SetSelectState(isSelected);
        }

        // 由面板调用：仅更新视觉选中状态，不触发 _onItemClick
        public void SetSelected(RecommendItemData data)
        {
            var newId = data?.ugcId ?? string.Empty;

            if (newId == _selectedId)
            {
                return;
            }

            var prevId = _selectedId;
            _selectedId = newId;
            UpdateSelectionVisualOnly(prevId, newId);
        }

        // 遍历可见 group → cell，只刷新前后两个 cell 的选中样式
        private void UpdateSelectionVisualOnly(string prevId, string curId)
        {
            for (int gi = 0; gi < VisibleItemsCount; gi++)
            {
                var groupVH = GetItemViewsHolder(gi) as CellGroupViewsHolder<ToneShopItemViewsHolder>;

                if (groupVH == null)
                {
                    continue;
                }

                for (int ci = 0; ci < groupVH.NumActiveCells; ci++)
                {
                    var cellVH = groupVH.ContainingCellViewsHolders[ci];

                    if (cellVH?.Item == null)
                    {
                        continue;
                    }

                    var item = Data[cellVH.ItemIndex];

                    if (item == null)
                    {
                        continue;
                    }

                    bool isPrev = item.ugcId == prevId;
                    bool isCur = item.ugcId == curId;

                    if (!isPrev && !isCur)
                    {
                        continue;
                    }

                    cellVH.Item.SetSelectState(isCur);
                }
            }
        }

        private void OnItemClickInternal(RecommendItemData data)
        {
            if (data == null)
            {
                return;
            }

            var prevId = _selectedId;
            _selectedId = data.ugcId ?? string.Empty;
            UpdateSelectionVisualOnly(prevId, _selectedId);
            _onItemClick?.Invoke(data);
        }
    }

    [Serializable]
    public class ToneShopGridParams : GridParams
    {
    }

    public class ToneShopItemViewsHolder : CellViewsHolder
    {
        public IncubationToneShopItem Item;

        protected override RectTransform GetViews()
        {
            return (RectTransform)root.GetChild(0);
        }

        public override void CollectViews()
        {
            base.CollectViews();
            Item = root.GetComponent<IncubationToneShopItem>();
        }
    }
}
