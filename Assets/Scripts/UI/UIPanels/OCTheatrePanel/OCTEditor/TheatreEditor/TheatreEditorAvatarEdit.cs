using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 演员选择页面。
/// 展示玩家已发布的演员列表，允许最多选择 <see cref="TheatreEditorDataCenter.MaxAvatarCount"/> 名演员参与剧场演出。
/// 选中状态写入 DataCenter.SelectedAvatarIds。
/// 通过 leftMenu 的 theatreAvatarToggle 打开（TheatreEditorPanel.ShowPage(EditorPageType.Avatar)）。
/// </summary>
public class TheatreEditorAvatarEdit : TheatreEditorUIBase<TheatreEditorDataCenter>
{
    [SerializeField] private TheatreEditorAvatarAdapter avatarAdapter;
    [SerializeField] private PullToRefreshBehaviour refreshController;
    [SerializeField] private TheatreEditorAvatarDataLoader dataloader;
    [SerializeField] private Text avatarCountTitle;
    [SerializeField] private GameObject emptyHint;
    [SerializeField] private Button goToAvatarDraftBtn;

    public override void OnInit(TheatreEditorDataCenter param)
    {
        base.OnInit(param);

        if (avatarAdapter == null || refreshController == null || dataloader == null) return;

        // OSA 必须先绑定 Data 再 Init
        avatarAdapter.Data = new SimpleDataHelper<OCTheatreAvatarInfo>(avatarAdapter);
        avatarAdapter.Init();

        // 下拉加载更多
        refreshController.OnRefreshWithSlideUp.AddListener(OnPullToRefresh);
        avatarAdapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);

        goToAvatarDraftBtn?.onClick.AddListener(OnGoToAvatarDraftClicked);
    }

    public override void OnShow(TheatreEditorDataCenter param)
    {
        base.OnShow(param);
        DataRoot = param;

        if (avatarAdapter == null) return;

        // 注入选中状态查询
        avatarAdapter.IsSelected = id => DataRoot?.IsAvatarSelected(id) ?? false;
        avatarAdapter.OnToggleItem = OnAvatarToggled;

        // 初始化 DataLoader（每次进入页面重新拉第一页）
        if (dataloader != null)
        {
            dataloader.Init(DataRoot);
            dataloader.ResetPaging();
        }

        emptyHint?.SetActive(false);
        goToAvatarDraftBtn?.gameObject.SetActive(false);
        StartCoroutine(LoadFirstPageNextFrame());
    }

    private IEnumerator LoadFirstPageNextFrame()
    {
        yield return null;
        LoadFirstPage();
        UpdateCountTitle();
    }

    public override void OnHide() { base.OnHide(); }

    /// <summary>
    /// Fetches the first page of all owned avatars into DataCenter.AvatarInfoCache.
    /// Called from TheatreEditorPanel.OnShow so SectionItems can resolve avatar URLs without
    /// requiring the user to visit the AvatarEdit page first.
    /// </summary>
    public void PreloadOwned(TheatreEditorDataCenter dc, Action onDone)
    {
        if (dc == null) { onDone?.Invoke(); return; }
        if (dataloader == null) { onDone?.Invoke(); return; }
        dataloader.Init(dc);
        dataloader.GetAvatarList(_ => onDone?.Invoke());
    }

    /// <summary>
    /// Ensures avatar data (with clothes) is loaded into DataCenter.AvatarInfoCache.
    /// If any selected avatar lacks clothes data, triggers a server fetch first.
    /// Calls onDone immediately if data is already sufficient.
    /// </summary>
    public void PreloadAvatarData(TheatreEditorDataCenter dc, Action onDone)
    {
        if (dc == null || dc.SelectedAvatarIds == null || dc.SelectedAvatarIds.Count == 0)
        {
            onDone?.Invoke();
            return;
        }

        bool needsFetch = false;
        foreach (var id in dc.SelectedAvatarIds)
        {
            if (!dc.AvatarInfoCache.TryGetValue(id, out var info) ||
                info?.avatarClothes == null || info.avatarClothes.Count == 0)
            {
                needsFetch = true;
                break;
            }
        }

        if (!needsFetch)
        {
            onDone?.Invoke();
            return;
        }

        if (dataloader == null)
        {
            onDone?.Invoke();
            return;
        }

        dataloader.Init(dc);
        dataloader.GetAvatarList(_ => onDone?.Invoke());
    }

    // ─── 数据加载 ───────────────────────────────────────────────

    private void LoadFirstPage()
    {
        if (!avatarAdapter.IsInitialized) return;
        refreshController?.HideGizmo();

        // 清空列表
        avatarAdapter.ResetItems(0);
        avatarAdapter.ClearPool();

        dataloader?.ResetPaging();
        dataloader?.GetAvatarList(OnFirstPageLoaded);
    }

    private void OnFirstPageLoaded(List<OCTheatreAvatarInfo> list)
    {
        if (!avatarAdapter.IsInitialized) return;

        list ??= new List<OCTheatreAvatarInfo>();
        avatarAdapter.Data.ResetItems(list);
        avatarAdapter.Refresh(false);
        avatarAdapter.OnItemsUpdated?.Invoke();
        emptyHint?.SetActive(list.Count == 0);
        goToAvatarDraftBtn?.gameObject.SetActive(list.Count == 0);
    }

    private void OnPullToRefresh()
    {
        dataloader?.GetAvatarList(OnMorePageLoaded);
    }

    private void OnMorePageLoaded(List<OCTheatreAvatarInfo> list)
    {
        if (list == null || list.Count == 0)
        {
            avatarAdapter.OnItemsUpdated?.Invoke();
            return;
        }
        avatarAdapter.Data.List.AddRange(list);
        avatarAdapter.Refresh(false);
    }

    // ─── 选中逻辑 ───────────────────────────────────────────────

    private void OnAvatarToggled(OCTheatreAvatarInfo info, bool isSelected)
    {
        if (info == null || DataRoot == null) return;

        // 超出上限时拒绝，并将 UI 回退到正确状态
        if (isSelected && DataRoot.SelectedAvatarIds.Count >= TheatreEditorDataCenter.MaxAvatarCount)
        {
            TipPanel.ShowToast($"最多选择 {TheatreEditorDataCenter.MaxAvatarCount} 个演员");
            // 强制刷新所有可见格子，使 Item 视觉回退
            avatarAdapter.Refresh(false);
            return;
        }

        DataRoot.ToggleAvatarSelected(info.id);
        UpdateCountTitle();
    }

    private void UpdateCountTitle()
    {
        if (avatarCountTitle == null || DataRoot == null) return;
        avatarCountTitle.text = $"({DataRoot.SelectedAvatarIds.Count}/{TheatreEditorDataCenter.MaxAvatarCount})";
    }

    private void OnGoToAvatarDraftClicked()
    {
        //UIManager.Inst.OpenPanel(PanelId.TheatreActorEditorMainPanel, null, LoadFirstPage);
        UIManager.Inst.OpenPanel(PanelId.ActorCardStorePanel);
    }
}
