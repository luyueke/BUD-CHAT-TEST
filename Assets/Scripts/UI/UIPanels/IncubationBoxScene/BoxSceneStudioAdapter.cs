using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using UGCAsset;
using UnityEngine;

namespace Game.IncubationBoxScene
{
    /// <summary>
    /// Box 场景工作室列表的 OSA（Optimized Scroll View）适配器。
    /// 对应 AnimationStudioAdapter，支持草稿和已发布两种模式的虚拟滚动列表渲染。
    /// 统一使用 CharacterBoxPublishItem 作为数据单元：
    ///   草稿模式 → CharacterBoxPublishItem.characterBoxInfo 存放草稿数据，interactInfo 为 null；
    ///   已发布模式 → 直接使用完整 CharacterBoxPublishItem。
    /// </summary>
    public class BoxSceneStudioAdapter : GridAdapter<GridParams, BoxSceneStudioListViewsHolder>
    {
        /// <summary>当前列表类型（草稿 / 已发布），影响条目的渲染模式</summary>
        public StudioSubType CurStudioType;

        /// <summary>条目被点击时的回调，参数为被点击的 CharacterBoxPublishItem</summary>
        public Action<CharacterBoxPublishItem> OnSelectItemAct;

        /// <summary>列表数据更新时触发（可用于外部刷新数量显示等 UI）</summary>
        public UnityEngine.Events.UnityEvent OnItemsUpdated;

        /// <summary>列表数据集合，由外部赋值后调用 Refresh 驱动刷新</summary>
        public SimpleDataHelper<CharacterBoxPublishItem> Data { get; set; }

        /// <summary>封面图纹理池，最多缓存 12 张，超出则按 FIFO 顺序淘汰</summary>
        public IPool TexturePool;

        /// <summary>
        /// 初始化纹理池，在 OSA 内部 Start 之前完成准备。
        /// </summary>
        protected override void Start()
        {
            TexturePool = new FIFOCachingPool(12, TextureDestroyer);
            base.Start();
        }

        /// <summary>
        /// 组件销毁时清空纹理池，防止 Texture 内存泄漏。
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearPool();
        }

        /// <summary>
        /// 手动清空纹理池，释放已缓存的封面图资源。
        /// </summary>
        public void ClearPool()
        {
            if (TexturePool != null)
            {
                TexturePool.Clear();
            }
        }

        /// <summary>
        /// 纹理淘汰回调，销毁被移出池的 Texture 对象。
        /// </summary>
        /// <param name="urlKey">纹理对应的 URL key</param>
        /// <param name="texture">被淘汰的 Texture 对象</param>
        private void TextureDestroyer(object urlKey, object texture)
        {
            var asUnityObject = texture as UnityEngine.Object;
            if (asUnityObject != null)
            {
                Destroy(asUnityObject);
            }
        }

        /// <summary>
        /// 刷新列表。调用前需确保 Data 已赋值，否则直接返回。
        /// </summary>
        public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {
            if (Data == null)
            {
                return;
            }

            _CellsCount = Data.Count;
            OnItemsUpdated?.Invoke();
            base.Refresh(false, keepVelocity);
        }

        /// <summary>
        /// 新条目视图持有者创建时，将纹理池绑定到封面图远程加载组件（若存在）。
        /// </summary>
        protected override void OnCellViewsHolderCreated(
            BoxSceneStudioListViewsHolder cellVH,
            CellGroupViewsHolder<BoxSceneStudioListViewsHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);

            if (cellVH.IconRemoteImageBehaviour != null)
            {
                cellVH.IconRemoteImageBehaviour.InitializeWithPool(TexturePool);
            }
        }

        /// <summary>
        /// 用数据填充或复用条目视图，同时加载封面图。
        /// </summary>
        protected override void UpdateCellViewsHolder(BoxSceneStudioListViewsHolder newOrRecycled)
        {
            var model = Data[newOrRecycled.ItemIndex];
            newOrRecycled.UpdateViews(model, OnSelectItemAct, CurStudioType);

            // 若条目预制体带封面图组件，加载远程封面 URL
            var coverUrl = model?.characterBoxInfo?.cover;
            if (!string.IsNullOrEmpty(coverUrl) && newOrRecycled.IconRemoteImageBehaviour != null)
            {
                newOrRecycled.IconRemoteImageBehaviour.Load(coverUrl);
            }
        }
    }

    /// <summary>
    /// Box 场景工作室列表条目的 OSA 视图持有者。
    /// 对应 AnimationStudioListViewsHolder，负责收集并持有封面图和条目 UI 组件的引用。
    /// </summary>
    public class BoxSceneStudioListViewsHolder : CellViewsHolder
    {
        /// <summary>
        /// 封面图远程加载组件，对应预制体中名为 "BoxSceneItemIcon" 的子节点。
        /// 若预制体上不存在该节点则为 null，不影响列表正常运行。
        /// </summary>
        public RemoteImageBehaviour IconRemoteImageBehaviour;

        /// <summary>条目 UI 组件，承载名称、审核状态等显示逻辑</summary>
        public BoxSceneStudioItem StudioItem;

        /// <summary>
        /// 从视图 GameObject 中收集组件引用，由 OSA 在创建视图持有者时自动调用。
        /// </summary>
        public override void CollectViews()
        {
            base.CollectViews();

            // 封面图组件为可选，找不到节点不报错，仅跳过封面显示
            var iconGO = GameObjectEx.FindChildByName(views, "BoxSceneItemIcon");
            if (iconGO != null)
            {
                IconRemoteImageBehaviour = iconGO.GetComponent<RemoteImageBehaviour>();
            }

            root.TryGetComponent(out StudioItem);
        }

        /// <summary>
        /// 用数据刷新当前条目的视图。
        /// characterBoxInfo.id 为 null 时识别为"去创作"入口，调用 InitAsCreateItem；
        /// 否则按草稿/已发布模式调用 InitSelectMode。
        /// </summary>
        /// <param name="data">条目数据（统一封装为 CharacterBoxPublishItem）</param>
        /// <param name="onSelect">点击回调，参数为被点击的数据包</param>
        /// <param name="studioType">列表类型（草稿 / 已发布），决定条目渲染模式</param>
        public virtual void UpdateViews(
            CharacterBoxPublishItem data,
            Action<CharacterBoxPublishItem> onSelect,
            StudioSubType studioType)
        {
            if (StudioItem == null)
            {
                return;
            }

            // id 为空表示"去创作"入口条目，使用专属初始化（回调透传给外部处理跳转逻辑）
            if (string.IsNullOrEmpty(data?.characterBoxInfo?.id))
            {
                StudioItem.InitAsCreateItem(() => onSelect?.Invoke(data));
                return;
            }
            StudioItem.InitSelectMode(data, onSelect, studioType);
        }
    }
}
