using System;
using System.Collections;
using System.Collections.Generic;
using Game.Store;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音效选择编辑页。从 SectionEdit 的 MusicInput 进入。
/// 三个 Tab：商城 / 我制作的 / 我拥有的，均在同一个 listView 中展示。
/// 搜索走 SearchAnimationMusic 接口，结果同样展示在 listView。
/// 确认时：已拥有 → 直接写入 section.Audio；未拥有 → 弹出 BusinessView 购买确认。
/// </summary>
public class TheatreEditorMusicEdit : TheatreEditorUIBase<TheatreEditorDataCenter>
{
    [SerializeField] private UgcAnimToneStoreSectionView sectionView;
    [SerializeField] private UgcAnimToneStoreListView listView;
    [SerializeField] private USwitchToggle shopToggle;
    [SerializeField] private USwitchToggle createdToggle;
    [SerializeField] private USwitchToggle ownToggle;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Text titleText;
    [SerializeField] private GameObject loadingObj;

    private POCTheatreSection sectionData;
    private RecommendItemData selectedItem;
    private string lastSelectedSectionId; // 记录商城最后选中的分区，切回时恢复列表

    // ──────────────────────────────────────────────
    // TheatreEditorUIBase 生命周期
    // ──────────────────────────────────────────────

    public override void OnInit(TheatreEditorDataCenter param)
    {
        base.OnInit(param);

        shopToggle?.Init();
        createdToggle?.Init();
        ownToggle?.Init();

        shopToggle?.onValueChanged.AddListener(isOn => { if (isOn) ShowShopTab(); });
        createdToggle?.onValueChanged.AddListener(isOn => { if (isOn) ShowCreatedTab(); });
        ownToggle?.onValueChanged.AddListener(isOn => { if (isOn) ShowOwnedTab(); });

        confirmButton?.onClick.RemoveAllListeners();
        confirmButton?.onClick.AddListener(OnConfirmClicked);

        // SectionView 选中分区 → ListView 加载该分区数据，同时记录分区 id 供切回时恢复
        if (sectionView != null)
            sectionView.SelectSectionAction = sectionId =>
            {
                lastSelectedSectionId = sectionId;
                listView?.SetActions(sectionId, OnItemSelected, null);
            };

        // 全局设置 ListView 选中回调（SetActions 内部也会覆盖，但 Created/Owned 手动 Reset 时需要此处的回调）
        if (listView?.adapter != null)
            listView.adapter.OnSelectItemAct = OnItemSelected;
    }

    public override void OnShow(TheatreEditorDataCenter param)
    {
        base.OnShow(param);
        DataRoot = param;
        sectionData = param?.currentSelected;

        selectedItem = null;
        // lastSelectedSectionId 保留：返回时恢复商城上次选中分区
        RefreshConfirmBtn();
        SetTitle("");
        SetLoading(false);

        // 向 Panel 注册搜索回调
        Panel?.SetupSearchBar(DoSearch);

        // 每次进入页面重置到商城 Tab（延迟一帧确保 OSA adapter 完成初始化）
        StartCoroutine(InitShopTabNextFrame());
    }

    private IEnumerator InitShopTabNextFrame()
    {
        yield return null;
        if (shopToggle != null)
        {
            if (shopToggle.isOn) ShowShopTab();
            else shopToggle.isOn = true;
        }
    }

    public override void OnHide() { base.OnHide(); }

    // ──────────────────────────────────────────────
    // Tab 切换
    // ──────────────────────────────────────────────

    private void ShowShopTab()
    {
        sectionView?.gameObject.SetActive(true);
        SetTitle("");
        SetLoading(false);
        ClearList();
        sectionView?.Reload();
        // 若之前选过分区，直接恢复该分区列表（Reload 不保证重新触发 SelectSectionAction）
        if (!string.IsNullOrEmpty(lastSelectedSectionId))
            listView?.SetActions(lastSelectedSectionId, OnItemSelected, null);
    }

    private void ShowCreatedTab()
    {
        sectionView?.gameObject.SetActive(false);
        SetTitle("我的创作");
        SetLoading(true);
        var jb = new JObject
        {
            ["cookie"] = "",
            ["uid"] = AccountDataManager.Inst.Uid
        };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.animMusicPublishList, HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                if (this == null) return;
                SetLoading(false);
                var rsp = JsonConvert.DeserializeObject<MusicListRsp>(content);
                ShowMusicList(ConvertToRecommendItems(rsp?.list, isOwned: true));
            },
            _ => { if (this != null) { SetLoading(false); TipPanel.ShowToast("加载失败，请重试"); } });
    }

    private void ShowOwnedTab()
    {
        sectionView?.gameObject.SetActive(false);
        SetTitle("我的拥有");
        SetLoading(true);
        var jb = new JObject
        {
            ["cookie"] = "",
            ["targetUid"] = AccountDataManager.Inst.Uid,
            ["interactType"] = 27,
            ["noPublish"] = 1
        };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.UGCInteractList, HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                if (this == null) return;
                SetLoading(false);
                var rsp = JsonConvert.DeserializeObject<MusicListRsp>(content);
                ShowMusicList(ConvertToRecommendItems(rsp?.list, isOwned: true));
            },
            _ => { if (this != null) { SetLoading(false); TipPanel.ShowToast("加载失败，请重试"); } });
    }

    // ──────────────────────────────────────────────
    // 辅助：标题 / 加载状态
    // ──────────────────────────────────────────────

    private void SetTitle(string t)
    {
        if (titleText == null) return;
        bool show = !string.IsNullOrEmpty(t);
        titleText.gameObject.SetActive(show);
        if (show) titleText.text = t;
    }

    private void SetLoading(bool loading)
    {
        loadingObj?.SetActive(loading);
    }

    // ──────────────────────────────────────────────
    // 搜索
    // ──────────────────────────────────────────────

    private void DoSearch(string keyword)
    {
        SetLoading(true);
        var jb = new JObject { ["searchWord"] = keyword, ["subType"] = 0 };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.SearchAnimationMusic, HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                if (this == null) return;
                SetLoading(false);
                var rsp = JsonConvert.DeserializeObject<SearchMusicRsp>(content);
                if (rsp?.list == null || rsp.list.Count == 0)
                {
                    TipPanel.ShowToast("没有找到相关音效");
                    return;
                }
                ShowMusicList(rsp.list);
            },
            _ => { if (this != null) { SetLoading(false); TipPanel.ShowToast("搜索失败，请重试"); } });
    }

    // ──────────────────────────────────────────────
    // ListView 操作
    // ──────────────────────────────────────────────

    private void ShowMusicList(List<RecommendItemData> items)
    {
        if (listView == null) return;
        // 确保选中回调指向当前页面
        if (listView.adapter != null)
            listView.adapter.OnSelectItemAct = OnItemSelected;

        ClearList();
        if (items == null || items.Count == 0) return;
        if (listView.adapter?.Data == null) return;

        listView.adapter.Data.ResetItems(items);
        listView.adapter.OnItemsUpdatedAct?.Invoke();
    }

    private void ClearList()
    {
        selectedItem = null;
        RefreshConfirmBtn();
        listView?.ResetAdpater();
    }

    private void OnItemSelected(RecommendItemData item)
    {
        selectedItem = item;
        RefreshConfirmBtn();
    }

    private void RefreshConfirmBtn()
    {
        // 确认按钮始终显示；未选中时点击会给出 Toast 提示
        confirmButton?.gameObject.SetActive(true);
    }

    // ──────────────────────────────────────────────
    // 确认 / 购买
    // ──────────────────────────────────────────────

    private void OnConfirmClicked()
    {
        if (selectedItem == null)
        {
            TipPanel.ShowToast("请先选择一个音效");
            return;
        }

        bool isOwned = selectedItem.interactInfo?.consumed == 1;
        string displayName = (selectedItem.UgcInfo as AnimMusicInfo)?.name
                             ?? selectedItem.ugcId
                             ?? "";
        if (isOwned)
        {
            ApplyAudio();
            Panel?.NotifyMusicSelected(displayName);
        }
        else
        {
            var info = selectedItem.UgcInfo as AnimMusicInfo;
            Panel?.ShowBuyView(info, () =>
            {
                if (selectedItem.interactInfo == null)
                    selectedItem.interactInfo = new BaseInteractInfo();
                selectedItem.interactInfo.consumed = 1;
                ApplyAudio();
                Panel?.NotifyMusicSelected(displayName);
            });
        }
    }

    /// <summary>
    /// 将选中音效写入 section.Audio，保留已有的 TriggerTime / IsLoop / TriggerType。
    /// Type=2 表示 UGC URL 音效。
    /// </summary>
    private void ApplyAudio()
    {
        if (sectionData == null || selectedItem == null) return;
        var info = selectedItem.UgcInfo;
        var existing = sectionData.Audio;
        var audio = new POCTheatreAudio
        {
            Type = 2,
            AudioId  = info?.id ?? selectedItem.ugcId ?? "",
            AudioUrl = info?.metaDataUrl ?? "",
            TriggerTime = existing?.TriggerTime ?? 1,
            IsLoop      = existing?.IsLoop ?? 0,
            TriggerType = existing?.TriggerType ?? 0,
        };
        Panel?.DataCenter?.SetSectionAudio(sectionData, audio);
    }

    // ──────────────────────────────────────────────
    // 数据转换
    // ──────────────────────────────────────────────

    private static List<RecommendItemData> ConvertToRecommendItems(
        List<AnimMusicListRspData> list, bool isOwned)
    {
        var result = new List<RecommendItemData>();
        if (list == null) return result;
        foreach (var rsp in list)
        {
            var info = rsp.animMusicInfo ?? rsp.ugcInfo;
            if (info == null) continue;
            result.Add(new RecommendItemData
            {
                ugcId        = info.id,
                ugcType      = UgcType.AnimMusic,
                ugcData      = JsonConvert.SerializeObject(info),
                interactInfo = isOwned ? new BaseInteractInfo { consumed = 1 } : null,
            });
        }
        return result;
    }

    // ──────────────────────────────────────────────
    // 私有 DTO
    // ──────────────────────────────────────────────

    private class MusicListRsp : HttpPageBaseData
    {
        public List<AnimMusicListRspData> list = new();
    }

    private class SearchMusicRsp
    {
        public List<RecommendItemData> list;
    }
}
