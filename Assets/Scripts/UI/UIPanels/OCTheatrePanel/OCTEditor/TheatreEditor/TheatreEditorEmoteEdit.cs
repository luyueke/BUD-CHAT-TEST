using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Es;
using GameData.UGCData;
using Game.Store;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 动作选择编辑页。三个 Tab：PGC官方 / UGC商城 / 我拥有的。
/// 单人/多人 Toggle 控制 animType 过滤；OwnTypeToggle 在「我拥有」Tab 下切换 PGC/UGC。
/// 点击动作 → AvatarSelector（选演员/服装）→ EmoteShowcase（预览/购买）→ 确认后回到本页，
/// 本页确认按钮点击后写入 section.Emote。
/// </summary>
public class TheatreEditorEmoteEdit : TheatreEditorUIBase<TheatreEditorDataCenter>
{
    [SerializeField] private AnimStoreListView listView;
    [SerializeField] private AnimStoreSectionView sectionView;
    [SerializeField] private USwitchToggle pgcToggle;
    [SerializeField] private USwitchToggle ugcToggle;
    [SerializeField] private USwitchToggle ownToggle;
    [SerializeField] private USwitchToggle singleToggle;
    [SerializeField] private USwitchToggle doubleToggle;
    [SerializeField] private USwitchToggle ownTypeToggle; // on = 我拥有的PGC, off = 我拥有的UGC
    [SerializeField] private Button confirmBtn;
    [SerializeField] private Text titleText;
    [SerializeField] private GameObject loadingObj;

    private POCTheatreSection _sectionData;
    private RecommendItemData _confirmedEmote;
    private List<(string avatarId, int clothesIndex)> _confirmedActors;
    private Vector3 _confirmedPosition;
    private Vector3 _confirmedRotation;
    private float _confirmedScale;
    private string _lastSectionId;
    private bool _refreshing;

    private int CurrentAnimType => singleToggle != null && singleToggle.isOn ? 1 : 3;
    private bool IsOwnTab => ownToggle != null && ownToggle.isOn;
    private bool IsSingleTab => singleToggle != null && singleToggle.isOn;

    // ── 生命周期 ──────────────────────────────────────

    public override void OnInit(TheatreEditorDataCenter param)
    {
        base.OnInit(param);

        pgcToggle?.Init();
        ugcToggle?.Init();
        ownToggle?.Init();
        singleToggle?.Init();
        doubleToggle?.Init();
        ownTypeToggle?.Init();

        pgcToggle?.onValueChanged.AddListener(isOn => { if (isOn && !_refreshing) RefreshList(); });
        ugcToggle?.onValueChanged.AddListener(isOn => { if (isOn && !_refreshing) RefreshList(); });
        ownToggle?.onValueChanged.AddListener(isOn => { if (isOn && !_refreshing) RefreshList(); });
        singleToggle?.onValueChanged.AddListener(isOn => { if (isOn && !_refreshing) RefreshList(); });
        doubleToggle?.onValueChanged.AddListener(isOn => { if (isOn && !_refreshing) RefreshList(); });
        ownTypeToggle?.onValueChanged.AddListener(_ => { if (IsOwnTab && !_refreshing) RefreshList(); });

        if (sectionView != null)
            sectionView.SelectSectionAction = sectionId =>
            {
                _lastSectionId = sectionId;
                int targetAnimType = CurrentAnimType;
                listView?.SetSectionActions(sectionId, OnItemClick, null,
                    item => (item?.UgcInfo as AnimInfo)?.animType == targetAnimType);
            };

        confirmBtn?.onClick.RemoveAllListeners();
        confirmBtn?.onClick.AddListener(OnConfirmClick);
    }

    public override void OnShow(TheatreEditorDataCenter param)
    {
        base.OnShow(param);
        DataRoot = param;
        _sectionData = param?.currentSelected;
        _confirmedEmote = null;
        _confirmedActors = null;

        RefreshConfirmBtn();
        Panel?.SetupSearchBar(DoSearch);

        _refreshing = true;
         if (pgcToggle != null && !pgcToggle.isOn) pgcToggle.isOn = false;
        
        if (ugcToggle != null && ugcToggle.isOn) ugcToggle.isOn = false;
        if (singleToggle != null) singleToggle.isOn = true;
       if (ownToggle != null && ownToggle.isOn) ownToggle.isOn = true;
        _refreshing = false;

        StartCoroutine(RefreshListNextFrame());
    }

    private IEnumerator RefreshListNextFrame()
    {
        yield return null;
        RefreshList();
    }

    public override void OnHide() { base.OnHide(); }

    // ── 列表刷新 ──────────────────────────────────────

    public void RefreshList()
    {
        _confirmedEmote = null;
        _confirmedActors = null;
        _lastSectionId = "";
        RefreshConfirmBtn();

        bool isOwn = IsOwnTab;
        bool isUgc = !isOwn && ugcToggle != null && ugcToggle.isOn;
        ownTypeToggle?.gameObject.SetActive(isOwn);
        sectionView?.gameObject.SetActive(isUgc);
        SetLoading(false);

        if (titleText != null)
        {
            titleText.gameObject.SetActive(!isUgc);
            if (!isUgc)
                titleText.text = isOwn ? "我的动作" : "官方动作";
        }

        if (isOwn)
            ShowOwnTab();
        else if (pgcToggle != null && pgcToggle.isOn)
            ShowPgcShopTab();
        else
            ShowUgcShopTab();
    }

    private void ShowPgcShopTab()
    {
        var items = BuildPgcItems(ownedOnly: false, isSingle: IsSingleTab);
        listView?.ShowList(items, OnItemClick);
    }

    private void ShowUgcShopTab()
    {
        // Both single and double UGC animations use ugcType=7 (Anim); ugcType=8 is Pose (static poses)
        sectionView?.Reload(7);
    }

    private void ShowOwnTab()
    {
        sectionView?.gameObject.SetActive(false);
        if (ownTypeToggle != null && ownTypeToggle.isOn)
        {
            var items = BuildPgcItems(ownedOnly: true, isSingle: IsSingleTab);
            listView?.ShowList(items, OnItemClick);
        }
        else
        {
            SetLoading(true);
            LoadOwnedUgcFromBag(CurrentAnimType, items =>
            {
                listView?.ShowList(items, OnItemClick);
                SetLoading(false);
            });
        }
    }

    private void LoadOwnedUgcFromBag(int animType, Action<List<RecommendItemData>> onComplete)
    {
        var handler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        var animIds = handler?.GetGoodsData(UniqueType.Get(ResourceType.UgcEmote, animType))
            ?.Where(g => !string.IsNullOrEmpty(g.Id)).Select(g => g.Id).ToList()
            ?? new List<string>();
        var poseIds = handler?.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, animType))
            ?.Where(g => !string.IsNullOrEmpty(g.Id)).Select(g => g.Id).ToList()
            ?? new List<string>();

        var result = new List<RecommendItemData>();
        var animDone = false;
        var poseDone = false;

        void TryFinish()
        {
            if (!animDone || !poseDone) return;
            onComplete?.Invoke(result);
        }

        if (animIds.Count > 0)
        {
            var jb = new JObject { ["idList"] = string.Join(",", animIds) };
            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.batchAnimInfo, HttpMethod.GET,
                JsonConvert.SerializeObject(jb),
                content =>
                {
                    if (this == null) { animDone = true; TryFinish(); return; }
                    var rsp = JsonConvert.DeserializeObject<BatchUgcAnimDetailRsp>(content);
                    if (rsp?.animationList != null)
                    {
                        foreach (var item in rsp.animationList)
                        {
                            if (item?.animInfo == null) continue;
                            result.Add(new RecommendItemData
                            {
                                ugcId = item.animInfo.id,
                                ugcType = UgcType.Anim,
                                ugcData = JsonConvert.SerializeObject(item.animInfo),
                                interactInfo = item.interactInfo ?? new BaseInteractInfo { consumed = 1 },
                                creatorInfo = item.creator,
                            });
                        }
                    }
                    animDone = true;
                    TryFinish();
                },
                _ => { if (this != null) { animDone = true; TryFinish(); } });
        }
        else
        {
            animDone = true;
        }

        if (poseIds.Count > 0)
        {
            var jb = new JObject { ["idList"] = string.Join(",", poseIds) };
            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.GetPoseBatchInfo, HttpMethod.GET,
                JsonConvert.SerializeObject(jb),
                content =>
                {
                    if (this == null) { poseDone = true; TryFinish(); return; }
                    var rsp = JsonConvert.DeserializeObject<BatchPoseDetailRsp>(content);
                    if (rsp?.poseList != null)
                    {
                        foreach (var item in rsp.poseList)
                        {
                            if (item?.poseInfo == null) continue;
                            result.Add(new RecommendItemData
                            {
                                ugcId = item.poseInfo.id,
                                ugcType = UgcType.Pose,
                                ugcData = JsonConvert.SerializeObject(item.poseInfo),
                                interactInfo = item.interactInfo ?? new BaseInteractInfo { consumed = 1 },
                                creatorInfo = item.creator,
                            });
                        }
                    }
                    poseDone = true;
                    TryFinish();
                },
                _ => { if (this != null) { poseDone = true; TryFinish(); } });
        }
        else
        {
            poseDone = true;
        }

        // Both lists empty — return immediately
        if (animIds.Count == 0 && poseIds.Count == 0)
            onComplete?.Invoke(result);
    }

    // ── 搜索 ──────────────────────────────────────────

    private void DoSearch(string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) { RefreshList(); return; }
        SetLoading(true);
        var jb = new JObject { ["searchWord"] = keyword, ["subType"] = 0 };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.SearchAnimation, HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                if (this == null) return;
                SetLoading(false);
                var rsp = JsonConvert.DeserializeObject<SearchAnimRsp>(content);
                if (rsp?.list == null || rsp.list.Count == 0)
                {
                    TipPanel.ShowToast("没有找到相关动作");
                    listView?.ResetAdapter();
                    return;
                }
                listView?.ShowList(rsp.list, OnItemClick);
            },
            _ => { if (this != null) { SetLoading(false); TipPanel.ShowToast("搜索失败，请重试"); } });
    }

    // ── Item 点击 → AvatarSelector ────────────────────

    private void OnItemClick(RecommendItemData item)
    {
        if (item == null || DataRoot == null) return;

        Panel?.ShowAvatarSelector(
            DataRoot,
            item,
            isSingle: IsSingleTab,
            onConfirm: selections => OnAvatarSelectorConfirmed(item, selections));
    }

    private void OnAvatarSelectorConfirmed(
        RecommendItemData item,
        List<(string avatarId, int clothesIndex)> selections)
    {
        var existing = _sectionData?.Emote;
        var initPos   = existing?.CustomPosition != null
            ? new Vector3(existing.CustomPosition.X, existing.CustomPosition.Y, 0f)
            : Vector3.zero;
        var initRot   = existing?.CustomRotation != null
            ? new Vector3(existing.CustomRotation.X, existing.CustomRotation.Y, 0f)
            : Vector3.zero;
        float initScale = existing?.Scale ?? 0f;

        Panel?.ShowEmoteShowcase(
            DataRoot, item, selections,
            initPos, initRot, initScale,
            onConfirm: (pos, rot, scale) =>
            {
                _confirmedEmote    = item;
                _confirmedActors   = selections;
                _confirmedPosition = pos;
                _confirmedRotation = rot;
                _confirmedScale    = scale;
                RefreshConfirmBtn();
                Panel?.BackToEmoteEdit();
            });
    }

    // ── 最终确认 ──────────────────────────────────────

    public void NotifyEmoteConfirmed(RecommendItemData emote,
        List<(string avatarId, int clothesIndex)> actors)
    {
        _confirmedEmote = emote;
        _confirmedActors = actors;
        RefreshConfirmBtn();
    }

    private void OnConfirmClick()
    {
        if (_confirmedEmote == null) { TipPanel.ShowToast("请先选择一个动作"); return; }
        ApplyEmote();
        Panel?.BackToSectionEdit();
    }

    private void ApplyEmote()
    {
        if (_sectionData == null || _confirmedEmote == null) return;

        var emote = new POCTheatreEmote
        {
            EmoteId     = _confirmedEmote.ugcId ?? _confirmedEmote.UgcInfo?.id ?? "",
            TriggerTime = _sectionData.Emote?.TriggerTime ?? 1,
            TriggerType = _sectionData.Emote?.TriggerType ?? 0,
            CustomPosition = new P_OCTVector3 { X = _confirmedPosition.x, Y = _confirmedPosition.y },
            CustomRotation = new P_OCTVector3 { X = _confirmedRotation.x, Y = _confirmedRotation.y },
            Scale          = _confirmedScale,
        };

        if (_confirmedActors != null)
        {
            foreach (var (avatarId, clothesIndex) in _confirmedActors)
            {
                var player = new POCTheatreAvatarOc
                {
                    PlayerId     = avatarId,
                    ClothesIndex = clothesIndex,
                };
                if (DataRoot?.AvatarInfoCache != null &&
                    DataRoot.AvatarInfoCache.TryGetValue(avatarId, out var info))
                {
                    player.AvatarName = info.name ?? "";
                    player.AvatarUrl  = info.cover ?? "";
                }
                emote.EmotePlayers.Add(player);
            }
        }

        DataRoot?.SetSectionEmote(_sectionData, emote);
    }

    private void RefreshConfirmBtn()
    {
        if (confirmBtn != null)
            confirmBtn.interactable = _confirmedEmote != null;
    }

    private void SetLoading(bool loading) => loadingObj?.SetActive(loading);

    // ── PGC 数据构建 ──────────────────────────────────

    private List<RecommendItemData> BuildPgcItems(bool ownedOnly, bool isSingle)
    {
        var result = new List<RecommendItemData>();
        var all = DataTables.GetEmoUIConfigList();
        if (all == null) return result;

        var priceDict = BuildPgcPriceDict();

        foreach (var cfg in all)
        {
            var subType = (EmoteSubType)cfg.emoType;
            bool isDouble = subType.IsDouble();
            if (isSingle == isDouble) continue;

            if (ownedOnly)
            {
                if (!AssetsDataManager.IsOwned(cfg.pgcId)) continue;
                if (priceDict.TryGetValue(cfg.pgcId, out var pd) && pd.Value > 0) continue;
            }

            result.Add(BuildPgcItem(cfg, AssetsDataManager.IsOwned(cfg.pgcId), priceDict));
        }
        return result;
    }

    // Builds pgcId -> CurrencyData price map from store handler (same source as FittingRoom ActionScene).
    private static Dictionary<string, CurrencyData> BuildPgcPriceDict()
    {
        var handler = AssetsDataManager.GetData<AvatarBUDSceneHandler>();
        var dict = new Dictionary<string, CurrencyData>();
        if (handler == null) return dict;

        int[] types =
        {
            UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.SingleAll),
            UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.DoubleAll),
        };
        foreach (var t in types)
        {
            var list = handler.GetGoodsData(t);
            if (list == null) continue;
            foreach (var g in list)
            {
                if (g.Assets == null || g.Assets.Count == 0) continue;
                var id = g.Assets[0].Id;
                if (!string.IsNullOrEmpty(id) && !dict.ContainsKey(id))
                    dict[id] = g.Price;
            }
        }
        return dict;
    }

    private static RecommendItemData BuildPgcItem(EmoUIConfig cfg, bool isOwned,
        Dictionary<string, CurrencyData> priceDict = null)
    {
        CurrencyData priceData = null;
        priceDict?.TryGetValue(cfg.pgcId, out priceData);
        int priceVal = priceData != null ? (int)priceData.Value : 0;

        var animInfo = new AnimInfo
        {
            id       = cfg.pgcId,
            name     = cfg.name,
            animType = ((EmoteSubType)cfg.emoType).IsDouble() ? 3 : 1,
            paymentInfo = priceData != null ? new PaymentInfo
            {
                currencyType = priceData.CurrencyType,
                price        = priceVal,
            } : null,
        };
        // Free or owned items display as owned (show name); paid unowned items show price.
        int consumed = (isOwned || priceVal == 0) ? 1 : 0;
        return new RecommendItemData
        {
            ugcId        = cfg.pgcId,
            ugcType      = UgcType.Anim,
            ugcData      = JsonConvert.SerializeObject(animInfo),
            interactInfo = new BaseInteractInfo { consumed = consumed },
        };
    }

    private class SearchAnimRsp
    {
        public List<RecommendItemData> list;
    }
}
