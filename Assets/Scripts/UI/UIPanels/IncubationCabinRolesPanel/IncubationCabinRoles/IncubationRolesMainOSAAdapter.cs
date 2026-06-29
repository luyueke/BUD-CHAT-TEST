using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using UnityEngine;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// IncubationCabinRolesMainPanel 的 OSA 列表适配器。
    /// 仿照 ToneShopOSAAdapter，数据类型为 CabinPublishData，
    /// Cell 直接使用 CabinCharacterCardItem，以 characterInfo.id 作为选中态唯一标识。
    /// </summary>
    public class IncubationRolesMainOSAAdapter : GridAdapter<IncubationRolesMainGridParams, IncubationRolesMainViewsHolder>
    {
        /// <summary>OSA 数据集合，面板通过 Data.ResetItems() 刷新列表</summary>
        public SimpleDataHelper<CabinPublishData> Data { get; private set; }

        /// <summary>
        /// 上滑加载更多组件（在 Prefab 中绑定到 OSA ScrollRect 上的 PullToRefreshBehaviour）。
        /// 面板通过它的 OnRefreshWithSlideUp 事件挂监听，触发服务端分页拉取下一页。
        /// </summary>
        public PullToRefreshBehaviour PullToRefreshBehaviour;

        private Action<CabinPublishData> _onItemClick;

        /// <summary>当前选中角色的 id，空字符串表示无选中</summary>
        private string _selectedId = string.Empty;

        protected override void Start()
        {
            Data = new SimpleDataHelper<CabinPublishData>(this);
            base.Start();
        }

        /// <summary>
        /// 设置 Cell 点击回调，由面板在 OnCreate 中绑定。
        /// </summary>
        /// <param name="onItemClick">点击某个 Cell 时触发，传回对应 CabinPublishData</param>
        public void SetOnItemClick(Action<CabinPublishData> onItemClick)
        {
            _onItemClick = onItemClick;
        }

        /// <summary>
        /// 清空选中状态（不刷新可见 Cell 视觉），用于列表重置时调用。
        /// </summary>
        public void ClearSelection()
        {
            _selectedId = string.Empty;
        }

        /// <summary>
        /// OSA 核心回调：当 Cell 需要显示或复用时，将数据绑定到 ViewsHolder。
        /// </summary>
        protected override void UpdateCellViewsHolder(IncubationRolesMainViewsHolder newOrRecycled)
        {
            var data = Data[newOrRecycled.ItemIndex];
            bool isSelected = !string.IsNullOrEmpty(_selectedId)
                && data?.characterInfo?.id == _selectedId;

            // 捕获 data 以便在 CabinCharacterCardItem 回调（参数为 CabinCharacterBaseInfo）中转回 CabinPublishData
            var capturedData = data;
            newOrRecycled.Item.SetData(data?.characterInfo, _ => OnItemClickInternal(capturedData));
            newOrRecycled.Item.SetSelected(isSelected);
        }

        /// <summary>
        /// 由面板调用：仅更新视觉选中状态，不触发 _onItemClick 回调。
        /// 用于导入模式自动选中第一项、或面板层主动切换选中。
        /// </summary>
        /// <param name="data">要选中的数据；传 null 则清空选中</param>
        public void SetSelected(CabinPublishData data)
        {
            var newId = data?.characterInfo?.id ?? string.Empty;

            if (newId == _selectedId)
            {
                return;
            }

            var prevId = _selectedId;
            _selectedId = newId;
            UpdateSelectionVisualOnly(prevId, newId);
        }

        /// <summary>
        /// 遍历当前可见 Group → Cell，仅刷新前一个选中和当前选中两个 Cell 的视觉态，
        /// 避免全量 ResetItems 带来的重绘开销。
        /// </summary>
        private void UpdateSelectionVisualOnly(string prevId, string curId)
        {
            for (int gi = 0; gi < VisibleItemsCount; gi++)
            {
                var groupVH = GetItemViewsHolder(gi) as CellGroupViewsHolder<IncubationRolesMainViewsHolder>;

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

                    if (item?.characterInfo == null)
                    {
                        continue;
                    }

                    bool isPrev = item.characterInfo.id == prevId;
                    bool isCur = item.characterInfo.id == curId;

                    if (!isPrev && !isCur)
                    {
                        continue;
                    }

                    cellVH.Item.SetSelected(isCur);
                }
            }
        }

        /// <summary>
        /// Cell 内部点击时触发：更新选中 id，刷新视觉，再向面板回传事件。
        /// </summary>
        private void OnItemClickInternal(CabinPublishData data)
        {
            if (data?.characterInfo == null)
            {
                return;
            }

            var prevId = _selectedId;
            _selectedId = data.characterInfo.id ?? string.Empty;
            UpdateSelectionVisualOnly(prevId, _selectedId);
            _onItemClick?.Invoke(data);
        }
    }

    /// <summary>
    /// IncubationDraftBoxOSAAdapter 的 GridParams 配置类，OSA 序列化使用。
    /// </summary>
    [Serializable]
    public class IncubationRolesMainGridParams : GridParams
    {
    }

    /// <summary>
    /// OSA Cell ViewsHolder，直接持有 CabinCharacterCardItem 引用。
    /// CollectViews 从 Cell 根节点获取组件，GetViews 返回第一个子节点作为内容容器。
    /// </summary>
    public class IncubationRolesMainViewsHolder : CellViewsHolder
    {
        /// <summary>Cell 根节点上的 CabinCharacterCardItem 组件</summary>
        public CabinCharacterCardItem Item;

        /// <summary>返回 Cell 内容的根 RectTransform（根节点的第一个子节点）</summary>
        protected override RectTransform GetViews()
        {
            return (RectTransform)root.GetChild(0);
        }

        /// <summary>从根节点收集组件引用，OSA 初始化时自动调用</summary>
        public override void CollectViews()
        {
            base.CollectViews();
            Item = root.GetComponent<CabinCharacterCardItem>();
        }
    }
}
