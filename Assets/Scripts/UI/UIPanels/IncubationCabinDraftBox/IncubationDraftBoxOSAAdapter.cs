using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using UnityEngine;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// IncubationCabinDraftBox 的 OSA 列表适配器。
    /// 仿照 IncubationRolesMainOSAAdapter，数据类型为 CabinPublishData，Cell 使用 CabinCharacterCardItem
    /// （2 参 SetData 内部按 ugcclass 自动推导状态徽章），以 characterInfo.id 作为选中态唯一标识。
    /// 额外提供 SetLocalCoverTexture：拍照后把本地 RT 即时显示到对应可见 Cell 的封面上。
    /// </summary>
    public class IncubationDraftBoxOSAAdapter : GridAdapter<IncubationDraftBoxGridParams, IncubationDraftBoxViewsHolder>
    {
        /// <summary>OSA 数据集合，面板通过 Data.ResetItems() 刷新列表</summary>
        public SimpleDataHelper<CabinPublishData> Data { get; private set; }

        /// <summary>
        /// 上滑加载更多组件（在 Prefab 中绑定到 OSA ScrollRect 上的 PullToRefreshBehaviour）。
        /// 面板通过它的 OnRefreshWithSlideUp 事件挂监听，触发服务端分页拉取下一页。
        /// </summary>
        public PullToRefreshBehaviour PullToRefreshBehaviour;

        private Action<CabinPublishData> _onItemClick;

        /// <summary>"新建"卡点击回调（列表第一个 Cell）</summary>
        private Action _onCreateClick;

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
        /// 设置列表第一个"新建"卡的点击回调，由面板在 OnCreate 中绑定。
        /// </summary>
        /// <param name="onCreateClick">点击新建卡时触发，走创建角色流程</param>
        public void SetOnCreateClick(Action onCreateClick)
        {
            _onCreateClick = onCreateClick;
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
        protected override void UpdateCellViewsHolder(IncubationDraftBoxViewsHolder newOrRecycled)
        {
            var data = Data[newOrRecycled.ItemIndex];

            // characterInfo 为 null 的占位项表示"新建"卡：显示 CreatRoot，点击走创建流程，不参与选中
            if (data?.characterInfo == null)
            {
                newOrRecycled.Item.SetCreateMode(_onCreateClick);
                return;
            }

            // 数据态：先切到 DataRoot（防止从新建态复用残留），再绑定数据
            newOrRecycled.Item.SetDataMode();

            bool isSelected = !string.IsNullOrEmpty(_selectedId)
                && data.characterInfo.id == _selectedId;

            // 捕获 data 以便在 CabinCharacterCardItem 回调（参数为 CabinCharacterBaseInfo）中转回 CabinPublishData
            var capturedData = data;
            // 2 参 SetData 内部按 ugcclass 自动推导状态徽章，与原草稿箱 3 参调用等价
            newOrRecycled.Item.SetData(data.characterInfo, _ => OnItemClickInternal(capturedData));
            newOrRecycled.Item.SetSelected(isSelected);
        }

        /// <summary>
        /// 由面板调用：仅更新视觉选中状态，不触发 _onItemClick 回调。
        /// 用于首页自动选中第一项、或面板层主动切换选中。
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
                var groupVH = GetItemViewsHolder(gi) as CellGroupViewsHolder<IncubationDraftBoxViewsHolder>;

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

        /// <summary>
        /// 拍照完成后把本地 RT 即时显示到 id 对应的可见 Cell 封面上。
        /// 该 Cell 不在可见区时忽略（封面 URL 已落盘，滚动到时会按 URL 加载）。
        /// </summary>
        /// <param name="characterId">目标角色 id</param>
        /// <param name="tex">本地封面纹理（RenderTexture）</param>
        public void SetLocalCoverTexture(string characterId, Texture tex)
        {
            if (string.IsNullOrEmpty(characterId) || tex == null)
            {
                return;
            }

            for (int gi = 0; gi < VisibleItemsCount; gi++)
            {
                var groupVH = GetItemViewsHolder(gi) as CellGroupViewsHolder<IncubationDraftBoxViewsHolder>;

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

                    if (cellVH.Item.CharacterId == characterId)
                    {
                        cellVH.Item.SetLocalCoverTexture(tex);
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// IncubationDraftBoxOSAAdapter 的 GridParams 配置类，OSA 序列化使用。
    /// </summary>
    [Serializable]
    public class IncubationDraftBoxGridParams : GridParams
    {
    }

    /// <summary>
    /// OSA Cell ViewsHolder，直接持有 CabinCharacterCardItem 引用。
    /// CollectViews 从 Cell 根节点获取组件，GetViews 返回第一个子节点作为内容容器。
    /// </summary>
    public class IncubationDraftBoxViewsHolder : CellViewsHolder
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
