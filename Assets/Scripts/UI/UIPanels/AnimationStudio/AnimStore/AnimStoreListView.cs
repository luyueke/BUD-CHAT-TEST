using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Store;
using UnityEngine;

/// <summary>
/// 动作商店列表视图，支持两种工作模式：
///   • Section 模式（社区UGC）：通过 SetSectionActions 驱动，内部走 sectionInfoV2 分页
///   • Direct 模式（拥有/创作）：外部加载好数据后调用 ShowList 直接展示
/// </summary>
public class AnimStoreListView : MonoBehaviour
{
    public AnimStoreListAdapter adapter;

    private readonly AnimStoreListDataLoader _loader = new AnimStoreListDataLoader();

    protected void Start()
    {
        var refreshCtrl = adapter.GetComponent<PullToRefreshBehaviour>();
        if (refreshCtrl != null)
        {
            refreshCtrl.OnRefreshWithSign.AddListener(OnPullReleased);
            adapter.OnItemsUpdatedAct.AddListener(refreshCtrl.HideGizmo);
        }
    }

    // ──────────────────────────────────────────────
    // Section 模式（社区 UGC）
    // ──────────────────────────────────────────────

    /// <summary>
    /// 切换到指定 section，开始分页加载。
    /// 等同于 ToneStore 的 SetActions。
    /// </summary>
    public void SetSectionActions(string sectionId,
        Action<RecommendItemData> onSelectItem,
        Action onEmptyAction,
        Predicate<RecommendItemData> filter = null)
    {
        _sectionItemFilter = filter;
        _loader.RefreshSection(sectionId);
        adapter.OnSelectItemAct = null;  // clear before load to prevent auto-select triggering navigation
        adapter.EmptyDataAct = onEmptyAction;
        LoadFirstPage(_ => { adapter.OnSelectItemAct = onSelectItem; });
    }

    private Predicate<RecommendItemData> _sectionItemFilter;

    private void LoadFirstPage(Action<List<RecommendItemData>> complete = null)
    {
        _loader.LoadBySection(datas =>
        {
            if (_sectionItemFilter != null && datas != null)
                datas = datas.FindAll(_sectionItemFilter);
            ResetAdapter();
            adapter.OnItemsUpdatedAct?.Invoke();
            if (datas != null && datas.Count > 0)
            {
                adapter.Data.ResetItems(datas);
                adapter.SetDataSelected(datas[0]);
            }
            complete?.Invoke(datas);
        });
    }

    private void OnPullReleased(float sign)
    {
        if (sign < 0)
            _loader.LoadBySection(OnReceivedNewModels);
    }

    private void OnReceivedNewModels(List<RecommendItemData> newModels)
    {
        if (newModels == null || newModels.Count == 0)
        {
            adapter.OnItemsUpdatedAct?.Invoke();
            return;
        }
        adapter.Data.InsertItems(adapter.GetItemsCount(), newModels);
        adapter.OnItemsUpdatedAct?.Invoke();
    }

    // ──────────────────────────────────────────────
    // Direct 模式（拥有的UGC / 创作的 / PGC列表）
    // ──────────────────────────────────────────────

    /// <summary>
    /// 直接展示一组已加载好的动作数据，不走 section 分页。
    /// </summary>
    public void ShowList(List<RecommendItemData> items,
        Action<RecommendItemData> onSelectItem = null)
    {
        if (onSelectItem != null)
            adapter.OnSelectItemAct = onSelectItem;

        ResetAdapter();
        if (items == null || items.Count == 0) return;
        if (adapter?.Data == null) return;

        adapter.Data.ResetItems(items);
        adapter.OnItemsUpdatedAct?.Invoke();
    }

    // ──────────────────────────────────────────────
    // 便捷加载方法（供 EmoteEdit 一键调用）
    // ──────────────────────────────────────────────

    /// <summary>
    /// 加载「我创作的」：animPublishList + posePublishList 合并展示。
    /// animType: 0=全部, 1=单人, 3=双人
    /// </summary>
    public void LoadAndShowCreated(int animType,
        Action<RecommendItemData> onSelectItem,
        Action<bool> onLoadComplete = null)
    {
        adapter.OnSelectItemAct = onSelectItem;
        ResetAdapter();

        var animDone = false;
        var poseDone = false;
        var animItems = new List<RecommendItemData>();
        var poseItems = new List<RecommendItemData>();

        _loader.ResetCreated();

        void TryMerge()
        {
            if (!animDone || !poseDone) return;
            var merged = new List<RecommendItemData>(animItems);
            merged.AddRange(poseItems);
            ShowList(merged);
            onLoadComplete?.Invoke(merged.Count > 0);
        }

        _loader.LoadCreated(animType, isAnim: true, datas =>
        {
            animItems = datas ?? new List<RecommendItemData>();
            animDone = true;
            TryMerge();
        });

        _loader.LoadCreated(animType, isAnim: false, datas =>
        {
            poseItems = datas ?? new List<RecommendItemData>();
            poseDone = true;
            TryMerge();
        });
    }

    /// <summary>
    /// 加载「我拥有的 UGC」：已创作（animPublishList+posePublishList）+ 已购买（UGCInteractList）合并。
    /// skinType: 0=人物, 1=宠物
    /// </summary>
    public void LoadAndShowOwnedUgc(int animType, int skinType,
        Action<RecommendItemData> onSelectItem,
        Action<bool> onLoadComplete = null)
    {
        adapter.OnSelectItemAct = onSelectItem;
        ResetAdapter();

        var animDone = false;
        var poseDone = false;
        var purchasedDone = false;
        var animItems = new List<RecommendItemData>();
        var poseItems = new List<RecommendItemData>();
        var purchasedItems = new List<RecommendItemData>();

        _loader.ResetCreated();
        _loader.ResetPurchased();

        void TryMerge()
        {
            if (!animDone || !poseDone || !purchasedDone) return;
            var merged = new List<RecommendItemData>(animItems);
            merged.AddRange(poseItems);
            // 去重：purchased 里可能已包含自己创作且自购买的
            foreach (var p in purchasedItems)
            {
                if (merged.Find(x => x.ugcId == p.ugcId) == null)
                    merged.Add(p);
            }
            ShowList(merged);
            onLoadComplete?.Invoke(merged.Count > 0);
        }

        _loader.LoadCreated(animType, isAnim: true, datas =>
        {
            animItems = datas ?? new List<RecommendItemData>();
            animDone = true;
            TryMerge();
        });

        _loader.LoadCreated(animType, isAnim: false, datas =>
        {
            poseItems = datas ?? new List<RecommendItemData>();
            poseDone = true;
            TryMerge();
        });

        _loader.LoadPurchased(skinType, datas =>
        {
            purchasedItems = datas ?? new List<RecommendItemData>();
            purchasedDone = true;
            TryMerge();
        });
    }

    // ──────────────────────────────────────────────
    // 公共工具
    // ──────────────────────────────────────────────

    public void ResetAdapter()
    {
        if (adapter != null && adapter.IsInitialized)
            adapter.ResetItems(0);
    }
}
