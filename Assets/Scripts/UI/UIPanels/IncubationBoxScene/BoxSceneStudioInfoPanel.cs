using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.IncubationBoxScene
{
    /// <summary>
    /// Box 场景工作室列表子面板，负责管理单个列表（草稿或已发布）的分页加载和条目渲染。
    /// 挂载在 BoxSceneStudioMainPanel 的子 GameObject 上，通过 StudioType 区分草稿/已发布。
    /// 参考 AnimationStudioInfoPanel，通过 BoxSceneStudioAdapter（OSA）实现虚拟滚动列表。
    /// 数据加载由 BoxSceneDataLoader 驱动，支持首页加载和下拉触发追加分页。
    /// </summary>
    public class BoxSceneStudioInfoPanel : MonoBehaviour
    {
        /// <summary>OSA 列表适配器，负责虚拟滚动渲染，需在 Inspector 中绑定</summary>
        public BoxSceneStudioAdapter Adapter;

        /// <summary>下拉刷新控制器，可选。绑定后支持下拉追加下一页数据</summary>
        [SerializeField] private PullToRefreshBehaviour refreshController;

        /// <summary>数据加载器，负责分页请求逻辑，需在 Inspector 中绑定</summary>
        public BoxSceneDataLoader DataLoader;

        public GameObject DefNull;
        /// <summary>当前列表类型：Drafts=草稿，Published=已发布</summary>
        public StudioSubType StudioType { get; private set; }

        private void Awake()
        {
            if (refreshController != null)
            {
                // 下拉触发加载下一页
                refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
            }

            // 数据刷新完成时自动隐藏下拉 Gizmo（对应 AnimationStudioInfoPanel 中 OnItemsUpdated 绑定）
            Adapter.OnItemsUpdated.AddListener(HideRefreshGizmo);

            // 初始化 OSA 数据源并启动适配器（对应 AnimationStudioInfoPanel.Awake 中的 Adapter.Init）
            Adapter.Data = new SimpleDataHelper<CharacterBoxPublishItem>(Adapter);
            Adapter.Init();
        }

        // ──────────────────────────────────────────────
        // 公共接口
        // ──────────────────────────────────────────────

        public void SetStudioType(StudioSubType studioSubType)
        {
            StudioType = studioSubType;
        }

        /// <summary>
        /// 设置条目点击回调，草稿和已发布统一使用 CharacterBoxPublishItem。
        /// 对应 AnimationStudioInfoPanel.SetItemOnClickAct。
        /// </summary>
        /// <param name="act">回调，参数为被点击的 CharacterBoxPublishItem</param>
        public void SetItemOnClickAct(Action<CharacterBoxPublishItem> act)
        {
            Adapter.OnSelectItemAct = act;
        }

        /// <summary>
        /// 切换到当前面板时调用，同步适配器类型、重置分页状态并加载第一页数据。
        /// 对应 AnimationStudioInfoPanel.OnSelectView。
        /// </summary>
        public void OnSelectView()
        {
            LoggerUtils.Log($"[BoxSceneStudioInfoPanel] OnSelectView，StudioType={StudioType}");

            HideRefreshGizmo();

            // 同步适配器的列表类型，确保条目渲染模式正确
            Adapter.CurStudioType = StudioType;

            // 重置 DataLoader 分页游标，从第一页开始
            DataLoader.InitData(StudioType);

            if (StudioType == StudioSubType.Drafts)
            {
                // 草稿接口返回 CharacterBoxInfo，先统一转换为 CharacterBoxPublishItem 再交给首页回调处理
                DataLoader.GetDraftList(rawList =>
                {
                    var items = rawList?.ConvertAll(info => new CharacterBoxPublishItem { characterBoxInfo = info });
                    OnGetFirstPage(items);
                });
            }
            else if (StudioType == StudioSubType.Published)
            {
                DataLoader.GetPublishedList(OnGetFirstPage);
            }
        }

        // ──────────────────────────────────────────────
        // 首页数据回调
        // ──────────────────────────────────────────────

        /// <summary>
        /// 首页数据到达时，清空旧条目并渲染新列表。
        /// 草稿模式下首位自动插入"去创作"入口（id 为 null）；已发布模式下直接渲染。
        /// 对应 AnimationStudioInfoPanel.OnGetFirstPageDatas。
        /// </summary>
        /// <param name="list">首页数据列表（统一为 CharacterBoxPublishItem）</param>
        private void OnGetFirstPage(List<CharacterBoxPublishItem> list)
        {
            ResetAdapter();

            var items = new List<CharacterBoxPublishItem>();

            // 草稿模式下首位固定插入"去创作"入口
            if (StudioType == StudioSubType.Drafts)
            {
                items.Add(BuildCreateItem());
            }

            if (list != null && list.Count > 0)
            {
                items.AddRange(list);
            }

            Adapter.Data.ResetItems(items);
            Adapter.OnItemsUpdated?.Invoke();


            if (DefNull != null)
            {
                DefNull.SetActive(items.Count == 0);
            }

            LoggerUtils.Log($"[BoxSceneStudioInfoPanel] {StudioType} 首页加载完成，共 {items.Count} 条");
        }

        // ──────────────────────────────────────────────
        // 下拉追加分页
        // ──────────────────────────────────────────────

        /// <summary>
        /// 下拉刷新触发时调用，请求下一页数据并追加到列表末尾。
        /// </summary>
        private void OnPullReleased()
        {
            if (StudioType == StudioSubType.Drafts)
            {
                DataLoader.GetDraftList(OnGetNextPageDrafts);
            }
            else if (StudioType == StudioSubType.Published)
            {
                DataLoader.GetPublishedList(OnGetNextPagePublished);
            }
        }

        /// <summary>
        /// 草稿下一页数据到达时，追加新条目到列表末尾。
        /// 对应 AnimationStudioInfoPanel.OnReceivedNewModelsForInsert。
        /// </summary>
        private void OnGetNextPageDrafts(List<CharacterBoxInfo> list)
        {
            if (list == null || list.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }

            foreach (var info in list)
            {
                Adapter.Data.List.Add(new CharacterBoxPublishItem { characterBoxInfo = info });
            }

            Adapter.Refresh(false);
            LoggerUtils.Log($"[BoxSceneStudioInfoPanel] 草稿列表追加 {list.Count} 条");
        }

        /// <summary>
        /// 已发布下一页数据到达时，追加新条目到列表末尾。
        /// 对应 AnimationStudioInfoPanel.OnReceivedNewModelsForInsert。
        /// </summary>
        private void OnGetNextPagePublished(List<CharacterBoxPublishItem> list)
        {
            if (list == null || list.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }

            Adapter.Data.List.AddRange(list);
            Adapter.Refresh(false);
            LoggerUtils.Log($"[BoxSceneStudioInfoPanel] 已发布列表追加 {list.Count} 条");
        }

        // ──────────────────────────────────────────────
        // 辅助方法
        // ──────────────────────────────────────────────

        /// <summary>
        /// 重置适配器，清空已渲染的条目和纹理池缓存。
        /// 对应 AnimationStudioInfoPanel.ResetAdpater。
        /// </summary>
        private void ResetAdapter()
        {
            if (!Adapter.IsInitialized)
                return;

            Adapter.ResetItems(0);
            Adapter.ClearPool();
        }

        /// <summary>
        /// 隐藏下拉刷新提示 Gizmo（若有绑定）。
        /// </summary>
        private void HideRefreshGizmo()
        {
            if (refreshController != null)
            {
                refreshController.HideGizmo();
            }
        }

        /// <summary>
        /// 构建"去创作"入口数据包。
        /// characterBoxInfo.id 保持 null，视图层（BoxSceneStudioListViewsHolder）据此识别并调用 InitAsCreateItem。
        /// </summary>
        private CharacterBoxPublishItem BuildCreateItem()
        {
            return new CharacterBoxPublishItem
            {
                characterBoxInfo = new CharacterBoxInfo { name = "去创作" }
            };
        }
    }
}
