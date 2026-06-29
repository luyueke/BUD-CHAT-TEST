using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.Events;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// BoxScene 场景列表 OSA Grid 适配器，负责列表数据管理、Cell 视图更新和选中状态维护。
    /// 参考 CabinUgcAnimUgcToneInfoPanel_Adapter 模式实现。
    /// </summary>
    public class BoxScene_Adapter : GridAdapter<GridParams, BoxScene_ViewsHolder>
    {
        /// <summary>列表数据源，由外部（CabinControllScenePanel）赋值并初始化</summary>
        public SimpleDataHelper<BoxSceneInfo> Data { get; set; }

        /// <summary>数据更新完成事件，用于通知 PullToRefresh 等组件隐藏加载指示器</summary>
        public UnityEvent OnItemsUpdated;

        private Action<BoxSceneInfo> _onItemSelect;

        /// <summary>「去购买」Item 被点击时的跳转回调，由外部（CabinControllScenePanel）绑定</summary>
        private Action _onGoPurchaseClick;

        /// <summary>
        /// 刷新列表：同步数据量到 OSA，触发更新事件，调用 OSA 基类刷新
        /// </summary>
        public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {
            _CellsCount = Data.Count;
            OnItemsUpdated?.Invoke();
            base.Refresh(false, keepVelocity);
        }

        /// <summary>
        /// OSA 回调：将数据绑定到复用的 Cell ViewsHolder，并更新选中高亮状态
        /// </summary>
        protected override void UpdateCellViewsHolder(BoxScene_ViewsHolder newOrRecycled)
        {
            var curData = Data[newOrRecycled.ItemIndex];
            newOrRecycled.UpdateViews(curData, OnItemSelect);
            // 以设备实际展示的场景路径（GetBoxScene）为准，判断当前 Item 是否为激活状态
            newOrRecycled.SetSelectState(IsItemActive(curData));
        }

        /// <summary>
        /// 绑定外部场景选中回调，场景被点击时触发
        /// </summary>
        public void SetOnSceneItemSelectAct(Action<BoxSceneInfo> act)
        {
            this._onItemSelect = act;
        }

        /// <summary>
        /// 绑定「去购买」Item 被点击时的跳转回调
        /// </summary>
        public void SetOnGoPurchaseClick(Action act)
        {
            _onGoPurchaseClick = act;
        }

        /// <summary>
        /// 判断指定场景 Item 是否与设备当前展示的场景匹配：
        /// GoPurchase → 始终不激活；DefaultScene → 设备无场景时激活；普通场景 → id 与 GetBoxScene() 一致时激活。
        /// 选中态由 MQTT 同步成功后触发的 Adapter.Refresh() 更新，不随点击立即变化。
        /// </summary>
        private bool IsItemActive(BoxSceneInfo data)
        {
            if (data == null)
                return false;

            if (data.specialType == BoxSceneSpecialType.GoPurchase)
                return false;

            if (data.specialType == BoxSceneSpecialType.DefaultScene)
                return string.IsNullOrEmpty(CabinBoxManager.Inst.GetBoxScene());

            return !string.IsNullOrEmpty(data.id) && data.id == CabinBoxManager.Inst.GetBoxScene();
        }

        /// <summary>
        /// Item 被点击时的内部回调：根据 specialType 分支处理，通知外部打开弹窗。
        /// 选中态变更不在此处触发，由 MQTT 同步成功后 Adapter.Refresh() 统一驱动。
        /// </summary>
        private void OnItemSelect(BoxSceneInfo info)
        {
            if (info == null)
                return;

            if (info.specialType == BoxSceneSpecialType.GoPurchase)
            {
                // 去购买：不更新选中态，不触发场景选中回调，仅执行商店跳转
                _onGoPurchaseClick?.Invoke();
                return;
            }

            if (info.specialType == BoxSceneSpecialType.DefaultScene)
            {
                // 默认场景：向外部传递 id="" 的 info，表示"清除设备场景"语义
                // 选中态变更在 MQTT 同步成功后由 CabinControllScenePanel.OnBoxSceneSyncResult 触发 Refresh
                var emptySceneInfo = new BoxSceneInfo
                {
                    specialType = BoxSceneSpecialType.DefaultScene
                    // id 默认为 null/空，父级可据此判断需要清除场景
                };
                this._onItemSelect?.Invoke(emptySceneInfo);
                return;
            }

            // 普通场景：向外部传递场景数据，选中态在 MQTT 同步成功后刷新
            this._onItemSelect?.Invoke(info);
        }
    }

    /// <summary>
    /// BoxScene 列表 Item 的 OSA ViewsHolder，持有 CabinSceneCardItem 并负责视图绑定
    /// </summary>
    public class BoxScene_ViewsHolder : CellViewsHolder
    {
        /// <summary>场景卡片 Item 组件，在 CollectViews 中从根节点获取</summary>
        public CabinSceneCardItem SceneCardItem;

        /// <summary>
        /// OSA 回调：收集根节点上的组件引用
        /// </summary>
        public override void CollectViews()
        {
            base.CollectViews();
            root.TryGetComponent(out SceneCardItem);
        }

        /// <summary>
        /// 绑定场景数据和点击回调到 CabinSceneCardItem
        /// </summary>
        /// <param name="data">当前 Item 对应的 BoxSceneInfo</param>
        /// <param name="onSelectClick">Item 被点击时的强类型回调</param>
        public void UpdateViews(BoxSceneInfo data, Action<BoxSceneInfo> onSelectClick)
        {
            if (SceneCardItem == null)
                return;

            SceneCardItem.SetData(data);
            // 将 object 回调桥接为强类型 Action<BoxSceneInfo>，复用 CabinSceneCardItem 的 onSelectClick
            SceneCardItem.onSelectClick = (obj) => onSelectClick?.Invoke(obj);
        }

        /// <summary>
        /// 设置 Item 的选中指示器状态，由 Adapter 根据 IsItemActive() 驱动（以 GetBoxScene() 为准）
        /// </summary>
        public void SetSelectState(bool isSelect)
        {
            if (SceneCardItem == null)
                return;

            SceneCardItem.SetSelectState(isSelect);
        }
    }
}
