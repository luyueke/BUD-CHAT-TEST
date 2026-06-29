using System.Collections.Generic;
using Es;
using Game.AINPCStudio;
using Game.Avatar;
using Game.Database;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

public class CameraModeNpcMenu : CameraModeMenuBase
{
    private CameraModeToggle single_toggle;
    private CameraModeToggle double_toggle;
    private CameraModeToggle link_toggle;
    private CameraModeToggle skin_toggle;
    private CameraModeToggle vehicle_toggle;


    private MISource misource; // 双人动作：官方/社区（PGC/UGC）切换
    private AIBuddyUgcEmoView ugcEmoteView;

    private UIEmoteType curEmoType = UIEmoteType.SinglePlayer;
    private UgcAnimSubType curUgcEmoType = UgcAnimSubType.Single;

    [Header("List Components")]
    [SerializeField] private GameObject emoContentPrefab;
    [SerializeField] private GameObject buddySkinPrefab;        // 换装皮肤卡（CabinSkinCardItem）
    [SerializeField] private Transform emoContent; // PGC 列表 Content（运行时会重定向到 `EmoScrollView` 的 Content）
    [SerializeField] private Transform ugcContent;
    [SerializeField] private Text emptyTip;        // PGC 列表空态（运行时会重定向到 `EmoScrollView` 的 EmptyTip）
    [SerializeField] private RectTransform Rect_EmoteView;
    [SerializeField] private Button confirmSkinBtn;            // 换装确认按钮
    [SerializeField] private Button confirmBuddyBtn;           // 召唤确认按钮

    [Header("Summon / Skin / Vehicle")]
    [SerializeField] private ScrollRect buddyCallScrollRect;   // 召唤列表（CabinCharacterCardItem）
    [SerializeField] private GameObject aiBuddyItemPrefab;     // AI 伙伴召唤卡（CabinCharacterCardItem），与 emote 的 emoContentPrefab 区分
    [SerializeField] private ScrollRect skinScrollRect;        // 换装皮肤列表
    [SerializeField] private UgcGameVehicleView vehicleView;   // 双人载具列表（仅双人，UGC/PGC）



    private ScrollRect pgcScrollRect;
    private Transform pgcContent;
    private Text pgcEmptyTip;

    // 5 个互斥子视图根节点（按名解析，限定本菜单子树取自身实例；HideAllSubViews 统一强制收起，
    // 保证同一时刻只显示一个，避免 serialized 引用错配/为空导致残留遮挡）
    private readonly GameObject[] _exclusiveViews = new GameObject[5];

    private List<BuddyEmoConentItem> avtiveItemList = new List<BuddyEmoConentItem>();
    private List<BuddyEmoConentItem> unActivceItemList = new List<BuddyEmoConentItem>();

    // 召唤列表
    private readonly List<CabinCharacterCardItem> _callCards = new();
    private CabinCharacterCardItem _selectedCallCard;
    private CabinCharacterUgcInfo _selectedCallInfo;

    // 换装皮肤列表
    private readonly List<CabinSkinCardItem> _skinCards = new();
    private int _skinBuildToken;
    private CabinSkinCardItem _pendingSkinCard;
    private CabinSkinCardItem _equippedSkinCard;

    private bool _vehicleStarted;

    protected override void OnInit(){
        single_toggle = GetComponentByName<CameraModeToggle>("ToggleSingle");
        double_toggle = GetComponentByName<CameraModeToggle>("ToggleDouble");
        link_toggle = GetComponentByName<CameraModeToggle>("ToggleLink");
        skin_toggle = GetComponentByName<CameraModeToggle>("ToggleChangeSkin");
        vehicle_toggle = GetComponentByName<CameraModeToggle>("ToggleVehicle");
        single_toggle.Init();
        double_toggle.Init();
        link_toggle.Init();
        if (skin_toggle != null) skin_toggle.Init();
        if (vehicle_toggle != null) vehicle_toggle.Init();

        single_toggle.onValueChanged.AddListener(OnSingleChange);
        double_toggle.onValueChanged.AddListener(OnDoubleChange);
        link_toggle.onValueChanged.AddListener(OnLinkChange);
        if (skin_toggle != null) skin_toggle.onValueChanged.AddListener(OnSkinChange);
        if (vehicle_toggle != null) vehicle_toggle.onValueChanged.AddListener(OnVehicleChange);

        if (confirmBuddyBtn != null) confirmBuddyBtn.onClick.AddListener(OnConfirmBuddyClick);
        if (confirmSkinBtn != null) confirmSkinBtn.onClick.AddListener(OnSkinConfirmClick);

        // Prefab 里有 2 个 MISource：`MISourceRoot`(主切换) 和 `SecondMISourceRoot`(UGC 内部 resMISource)
        // 这里必须拿主切换，否则会被 UGC 显隐逻辑干掉，导致“什么都不显示”
        misource = GetComponentByName<MISource>("MISourceRoot");
        ugcEmoteView = GetComponentInChildren<AIBuddyUgcEmoView>(true);
        if (misource != null) misource.SetCallback(OnValueChange);

        // PGC 列表实际展示在 `EmoScrollView` 下（它自带 EmptyTip），因此运行时把引用重定向过去
        pgcScrollRect = GetComponentByName<ScrollRect>("EmoScrollView");
        if (pgcScrollRect != null && pgcScrollRect.content != null)
        {
            emoContent = pgcScrollRect.content;
            var pgcEmpty = GameObjectEx.FindComponentByName<Text>(pgcScrollRect.transform, "EmptyTip");
            if (pgcEmpty != null) emptyTip = pgcEmpty;
        }

        // IMPORTANT:
        // UGC 视图(`AIBuddyUgcEmoView`)内部也有自己的 Content(OSA)，切换到 UGC 时会用到。
        // 所以我们在这里把“PGC 列表”引用独立出来，后续显隐/填充只操作 pgcContent/pgcEmptyTip，
        // 避免误把 UGC 的 Content SetActive(false) 导致“UGC 没内容且 Content 变灰(inactive)”。
        pgcContent = emoContent;
        pgcEmptyTip = emptyTip;

        if (ugcEmoteView != null) ugcEmoteView.OnStart(curUgcEmoType, () => { });

        // 缓存 5 个互斥子视图根（按名，限定本菜单子树 → 自动取 NPC 自己的实例，不会误取 ActionMenu 的同名物体）
        _exclusiveViews[0] = FindViewRoot("EmoScrollView");
        _exclusiveViews[1] = FindViewRoot("CharacterScrollView");
        _exclusiveViews[2] = FindViewRoot("SkinScrollView");
        _exclusiveViews[3] = FindViewRoot("UgcView");
        _exclusiveViews[4] = FindViewRoot("UgcVehicleView");

        gameObject.SetActive(false); //默认隐藏
        HideAllSubViews();
    }

    protected override void OnShow()
    {
        // 根据“是否已召唤”刷新 Toggle 显隐：未召唤只显示 single，召唤后隐藏 single、显示其余
        RefreshToggleVisibility();

        bool hasBuddy = AIBuddyAvatarController.Inst.SelfController != null;
        if (!hasBuddy)
        {
            if (single_toggle != null)
            {
                if (single_toggle.isOn) OnSingleChange(true);
                else single_toggle.isOn = true;
            }
            return;
        }

        // 已召唤：single 已隐藏；若之前停留在 single 则切到双人动作，否则刷新当前选中页
        if (single_toggle != null && single_toggle.isOn)
        {
            if (double_toggle != null) double_toggle.isOn = true;
        }
        else if (double_toggle != null && double_toggle.isOn) OnDoubleChange(true);
        else if (link_toggle != null && link_toggle.isOn) OnLinkChange(true);
        else if (skin_toggle != null && skin_toggle.isOn) OnSkinChange(true);
        else if (vehicle_toggle != null && vehicle_toggle.isOn) OnVehicleChange(true);
        else if (double_toggle != null) double_toggle.isOn = true;
    }

    protected override void OnHide()
    {

    }

    /// <summary>未召唤只显示 single；已召唤隐藏 single、显示双人/牵手/换装/载具。</summary>
    private void RefreshToggleVisibility()
    {
        bool hasBuddy = AIBuddyAvatarController.Inst.SelfController != null;
        if (single_toggle != null) single_toggle.gameObject.SetActive(!hasBuddy);
        if (double_toggle != null) double_toggle.gameObject.SetActive(hasBuddy);
        if (link_toggle != null) link_toggle.gameObject.SetActive(hasBuddy);
        if (skin_toggle != null) skin_toggle.gameObject.SetActive(hasBuddy);
        if (vehicle_toggle != null) vehicle_toggle.gameObject.SetActive(hasBuddy);
    }

    /// <summary>隐藏所有子视图（切换 Toggle 前统一收起，各 Toggle 再各自显示所需）。</summary>
    private void HideAllSubViews()
    {
        SetAllItemUnActive();
        // 强制收起全部 5 个互斥子视图根：即使下方 serialized 引用错配/为空，也保证不残留遮挡其它视图
        for (int i = 0; i < _exclusiveViews.Length; i++)
            if (_exclusiveViews[i] != null) _exclusiveViews[i].SetActive(false);
        SetPgcListVisible(false);
        SetUgcVisible(false);
        if (Rect_EmoteView != null) Rect_EmoteView.gameObject.SetActive(false);
        if (misource != null) misource.gameObject.SetActive(false);
        if (buddyCallScrollRect != null) buddyCallScrollRect.gameObject.SetActive(false);
        if (confirmBuddyBtn != null) confirmBuddyBtn.gameObject.SetActive(false);
        if (skinScrollRect != null) skinScrollRect.gameObject.SetActive(false);
        if (confirmSkinBtn != null) confirmSkinBtn.gameObject.SetActive(false);
        if (vehicleView != null) vehicleView.gameObject.SetActive(false);
    }

    /// <summary>在本菜单子树内按名找子视图根 GameObject（限定子树，自动取 NPC 自身实例，避免误取 ActionMenu 同名物体）。</summary>
    private GameObject FindViewRoot(string name)
    {
        var t = GameObjectEx.FindChildByName(transform, name);
        return t != null ? t.gameObject : null;
    }

    // ──────────────── 召唤（single） ────────────────

    private void OnSingleChange(bool isOn){
        if (!isOn) return;

        curEmoType = UIEmoteType.SinglePlayer;
        HideAllSubViews();

        // 召唤页对标 AIBoxBuddyCallPanel：显示官方/社区切换 + 召唤列表 + 确认按钮
        if (misource != null)
        {
            misource.gameObject.SetActive(true);
            // MISourceRoot 排在 ScrollView 之前，易被底图盖住，置顶保证可见可点
            misource.transform.SetAsLastSibling();
        }
        if (confirmBuddyBtn != null) confirmBuddyBtn.gameObject.SetActive(true);

        // 列表显隐与拉取交给 OnValueChange 按 source 分流；默认社区(UGC)
        if (misource != null)
        {
            misource.DefualtOn(MISource.Source.Create);
        }
        else
        {
            if (buddyCallScrollRect != null) buddyCallScrollRect.gameObject.SetActive(true);
            FetchCallList();
        }
    }

    private void FetchCallList()
    {
        ClearCallList();
        if (confirmBuddyBtn != null) confirmBuddyBtn.interactable = false;
        CabinNetManager.Inst.GetNetCabinCharacterPublishList(CabinPurchasedType.All, (ok, list) =>
        {
            if (this == null || gameObject == null) return;
            if (!ok || list == null)
            {
                TipPanel.ShowToast("获取 AI 伙伴列表失败，请稍后重试");
                return;
            }
            BuildCallList(list);
        });
    }

    private void BuildCallList(List<CabinPublishData> list)
    {
        ClearCallList();
        if (aiBuddyItemPrefab == null || buddyCallScrollRect == null) return;
        aiBuddyItemPrefab.SetActive(false);

        foreach (var data in list)
        {
            var info = data?.characterInfo as CabinCharacterUgcInfo;
            if (info == null) continue;

            var go = Instantiate(aiBuddyItemPrefab, buddyCallScrollRect.content);
            go.SetActive(true);
            var card = go.GetComponent<CabinCharacterCardItem>();
            if (card == null) continue;

            card.SetData(info, _ => OnCallItemClicked(card, info));
            card.SetBadgesVisible(false);
            _callCards.Add(card);
        }
    }

    private void OnCallItemClicked(CabinCharacterCardItem card, CabinCharacterUgcInfo info)
    {
        foreach (var c in _callCards) c?.SetSelected(false);
        card.SetSelected(true);
        _selectedCallCard = card;
        _selectedCallInfo = info;
        if (confirmBuddyBtn != null) confirmBuddyBtn.interactable = true;
    }

    private void OnConfirmBuddyClick()
    {
        if (_selectedCallInfo == null)
        {
            TipPanel.ShowToast("请先选择一个 AI 伙伴");
            return;
        }
        AIBoxBuddyCallPanel.SummonByCabin(_selectedCallInfo);
        // 召唤后：隐藏 single，显示其余 Toggle，并默认切到双人动作
        RefreshToggleVisibility();
        if (double_toggle != null) double_toggle.isOn = true;
    }

    private void ClearCallList()
    {
        foreach (var c in _callCards)
            if (c != null) Destroy(c.gameObject);
        _callCards.Clear();
        _selectedCallCard = null;
        _selectedCallInfo = null;
    }

    // ──────────────── 双人 / 牵手动作 ────────────────

    private void OnDoubleChange(bool isOn){
        if (!isOn) return;

        // Double：展示双人动作；支持 PGC/UGC 切换
        curEmoType = UIEmoteType.DoublePlayer;
        curUgcEmoType = UgcAnimSubType.Double;

        HideAllSubViews();

        Rect_EmoteView.offsetMax = new Vector2(-20f,-100f);
        Rect_EmoteView.gameObject.SetActive(true);

        if (misource != null)
        {
            misource.gameObject.SetActive(true);
            // Prefab 里 `MISourceRoot` 在 `EmoScrollView` 前面，容易被 ScrollView 的底图盖住
            // 强制置顶，保证可见且可点击
            misource.transform.SetAsLastSibling();
            // 默认选中 Bud(PGC)
            misource.DefualtOn(MISource.Source.Bud);
        }

        // 先构建 PGC 列表；实际显示由 OnValueChange 决定
        ShowEmoConent();
        OnValueChange(MISource.Source.Bud);
    }

    private void OnLinkChange(bool isOn){
        if (!isOn) return;

        // Link：展示牵手动作；强制 PGC，不显示官方/社区切换，也不显示 UGC 视图
        curEmoType = UIEmoteType.LinkEmote;
        curUgcEmoType = UgcAnimSubType.LinkEmote;

        HideAllSubViews();

        Rect_EmoteView.offsetMax = new Vector2(-20f,-20f);
        Rect_EmoteView.gameObject.SetActive(true);

        if (misource != null)
        {
            misource.DefualtOn(MISource.Source.Bud);
            misource.gameObject.SetActive(false);
        }

        ShowEmoConent();
        // 强制按 PGC 规则刷新一次（即使 MISource 不显示）
        OnValueChange(MISource.Source.Bud);
    }

    private void OnValueChange(MISource.Source source)
    {
        // 召唤(single)页：官方/社区切换走召唤列表逻辑（对标 AIBoxBuddyCallPanel）
        if (curEmoType == UIEmoteType.SinglePlayer)
        {
            bool isCreate = source == MISource.Source.Create;
            if (buddyCallScrollRect != null) buddyCallScrollRect.gameObject.SetActive(isCreate);
            if (isCreate)
            {
                FetchCallList();
            }
            else
            {
                // 官方(PGC) 暂未实现，清空列表
                ClearCallList();
                if (confirmBuddyBtn != null) confirmBuddyBtn.interactable = false;
            }
            return;
        }

        // 只有“双人动作”允许切到 UGC；其它类型一律按 PGC 展示
        bool allowUgc = curEmoType == UIEmoteType.DoublePlayer;
        bool isPgc = !allowUgc || source == MISource.Source.Bud;

        SetPgcListVisible(isPgc);
        SetUgcVisible(!isPgc);

        if (!isPgc && ugcEmoteView != null)
        {
            ugcEmoteView.SetDefaultMISource();
            ugcEmoteView.ChangeAnimType(curUgcEmoType);
        }

        if (emptyTip != null)
        {
            // UGC 视图不走这里的空态；PGC 列表才显示 emptyTip
            if (pgcEmptyTip != null) pgcEmptyTip.gameObject.SetActive(isPgc && avtiveItemList.Count <= 0);
        }
    }

    private void ShowEmoConent(){
        SetAllItemUnActive();
        List<InventoryData> owned = new();
        List<EmoUIConfig> emoteDataList = new();
        switch (curEmoType)
        {
            case UIEmoteType.DoublePlayer:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.Double)));
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.DoubleLoop)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.Double && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.DoubleLoop && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
            case UIEmoteType.LinkEmote:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.LinkEmote)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.LinkEmote && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
        }
        if (ugcEmoteView != null) ugcEmoteView.ChangeAnimType(curUgcEmoType);

        for (int i = owned.Count - 1; i >= 0; i--)
        {
            var config = Es.DataTables.GetEmoUIConfig(owned[i].Id);
            if (config != null)
            {
                emoteDataList.Insert(0, config);
            }
        }
        emoteDataList.Sort((a, b) =>
        {
            var aData = BagDatabase.Inst.Select(a.pgcId);
            long aTime = (aData == null) ? 0 : aData.Timestamp;

            var bData = BagDatabase.Inst.Select(b.pgcId);
            long bTime = (bData == null) ? 0 : bData.Timestamp;

            // 降序排序
            return bTime.CompareTo(aTime);
        });

        for (int i = 0; i < emoteDataList.Count; i++)
        {
            var emoContentItem = GetEmoItem();
            if (emoContentItem != null)
            {
                emoContentItem.transform.SetSiblingIndex(i);
                emoContentItem.InitData(emoteDataList[i], null, string.Empty);
            }
        }

        if (pgcEmptyTip != null)
        {
            pgcEmptyTip.gameObject.SetActive(emoteDataList.Count <= 0);
            if (emoteDataList.Count <= 0)
            {
                pgcEmptyTip.SetLocalText(curEmoType switch
                {
                    UIEmoteType.SinglePlayer => "无单人动作，可前往商城获取",
                    UIEmoteType.DoublePlayer => "无双人动作，可前往商城获取",
                    UIEmoteType.PetSingle => "无宠物动作，可前往商城获取",
                    UIEmoteType.PetWithPlayer => "无宠物与人交互动作，可前往商城获取",
                    UIEmoteType.LinkEmote => "无牵手动作，可前往商城获取",
                    _ => "无动作，可前往商城获取"
                });
            }
        }
    }

    private void SetAllItemUnActive()
    {
        foreach (var emoData in avtiveItemList)
        {
            unActivceItemList.Add(emoData);
            emoData.gameObject.SetActive(false);
        }

        avtiveItemList.Clear();
    }

    private BuddyEmoConentItem GetEmoItem()
    {
        BuddyEmoConentItem emoContentItem;

        if (unActivceItemList.Count == 0)
        {
            if (emoContentPrefab == null || pgcContent == null) return null;
            emoContentItem = GameObject.Instantiate(emoContentPrefab, pgcContent).GetComponent<BuddyEmoConentItem>();
        }
        else
        {
            emoContentItem = unActivceItemList[0];
            emoContentItem.gameObject.SetActive(true);

            unActivceItemList.RemoveAt(0);
        }

        avtiveItemList.Add(emoContentItem);

        return emoContentItem;
    }

    private void SetPgcListVisible(bool visible)
    {
        // PGC 列表显示/隐藏：只操作 PGC 的 ScrollView/Content/EmptyTip，避免影响 UGC 视图内部的 Content(OSA)
        if (pgcScrollRect != null) pgcScrollRect.gameObject.SetActive(visible);
        if (pgcContent != null) pgcContent.gameObject.SetActive(visible);
        if (!visible && pgcEmptyTip != null) pgcEmptyTip.gameObject.SetActive(false);
    }

    private void SetUgcVisible(bool visible)
    {
        if (ugcEmoteView == null) return;
        ugcEmoteView.gameObject.SetActive(visible);
        if (ugcEmoteView.resMISource != null) ugcEmoteView.resMISource.gameObject.SetActive(visible);

        if (!visible) return;

        ugcContent.gameObject.SetActive(true);
    }

    // ──────────────── 换装（ToggleChangeSkin） ────────────────

    private void OnSkinChange(bool isOn)
    {
        if (!isOn) return;
        HideAllSubViews();
        if (skinScrollRect != null) skinScrollRect.gameObject.SetActive(true);
        if (confirmSkinBtn != null) confirmSkinBtn.gameObject.SetActive(true);
        BuildSkinList();
    }

    private void BuildSkinList()
    {
        ClearSkinList();
        var token = ++_skinBuildToken;

        var info = AIBoxBuddyCallPanel.CurrentSummonedInfo;
        if (skinScrollRect == null || buddySkinPrefab == null || info == null)
            return;

        buddySkinPrefab.SetActive(false);

        var skinItems = new List<CabinCharacterBaseInfo>();
        // 本体皮肤：每个 skinPack 一张卡（克隆本体，skinPack 收窄为单个）
        if (info.skinPack != null)
        {
            foreach (var sp in info.skinPack)
            {
                var json = JsonConvert.SerializeObject(info);
                var wrapper = JsonConvert.DeserializeObject<CabinCharacterUgcInfo>(json);
                wrapper.skinPack = new List<SkinPackInfo> { sp };
                skinItems.Add(wrapper);
            }
        }

        // 扩展包皮肤（异步）
        if (info.extensionPackList != null && info.extensionPackList.Count > 0)
        {
            CabinNetManager.Inst.GetExtensionPackBatchInfo(info.extensionPackList, (isS, packList) =>
            {
                if (this == null || gameObject == null) return;
                if (token != _skinBuildToken) return; // 已有更新的重建，丢弃过期回调
                if (isS && packList != null)
                {
                    foreach (var pack in packList)
                    {
                        if (pack.ugcclass != (int)UGCClass.Published) continue;
                        if (pack.skinPack == null) continue;
                        foreach (var sp in pack.skinPack)
                        {
                            var json = JsonConvert.SerializeObject(pack);
                            var wrapper = JsonConvert.DeserializeObject<CabinCharacterPackInfo>(json);
                            wrapper.skinPack = new List<SkinPackInfo> { sp };
                            skinItems.Add(wrapper);
                        }
                    }
                }
                SpawnSkinCards(skinItems);
            });
        }
        else
        {
            SpawnSkinCards(skinItems);
        }
    }

    private void SpawnSkinCards(List<CabinCharacterBaseInfo> datas)
    {
        CabinSkinCardItem firstItem = null;
        CabinSkinCardItem defaultItem = null;
        CabinSkinCardItem equippedItem = null;
        var equippedPackId = AIBoxBuddyCallPanel.CurrentSkinPackId;

        foreach (var data in datas)
        {
            if (data == null) continue;

            var go = Instantiate(buddySkinPrefab, skinScrollRect.content);
            go.SetActive(true);
            var card = go.GetComponent<CabinSkinCardItem>();
            if (card == null) continue;

            card.SetData(data, _ => OnSkinClicked(card));
            // 主体（含其皮肤包）随召唤即拥有，强制解锁；扩展包按拥有情况判断
            if (data is CabinCharacterUgcInfo)
                card.SetLockVisible(false);
            else
                card.RefreshLock();
            card.SetBadgesVisible(false);
            card.SetCurrent(false);
            _skinCards.Add(card);

            if (firstItem == null) firstItem = card;
            var sp = data.skinPack != null && data.skinPack.Count > 0 ? data.skinPack[0] : null;
            if (defaultItem == null && sp != null && sp.isDefault == 1)
                defaultItem = card;
            if (equippedItem == null && sp != null && !string.IsNullOrEmpty(equippedPackId) && sp.packId == equippedPackId)
                equippedItem = card;
        }

        var current = equippedItem ?? defaultItem ?? firstItem;
        SetEquipped(current);
        HighlightSkin(current);
        _pendingSkinCard = current;
    }

    private void OnSkinClicked(CabinSkinCardItem card)
    {
        HighlightSkin(card);
        _pendingSkinCard = card;
    }

    private void OnSkinConfirmClick()
    {
        if (_pendingSkinCard == null) return;
        if (_pendingSkinCard == _equippedSkinCard)
        {
            TipPanel.ShowToast("已是当前皮肤");
            return;
        }
        if (!IsSkinOwned(_pendingSkinCard._data))
        {
            TipPanel.ShowToast("请先获得该皮肤");
            return;
        }
        if (ApplySkin(_pendingSkinCard))
            SetEquipped(_pendingSkinCard);
    }

    private bool ApplySkin(CabinSkinCardItem card)
    {
        var data = card?._data;
        var sp = data?.skinPack != null && data.skinPack.Count > 0 ? data.skinPack[0] : null;
        if (sp == null || string.IsNullOrEmpty(sp.avatarJson))
        {
            TipPanel.ShowToast("皮肤数据异常，请稍后重试");
            return false;
        }
        if (AIBuddyAvatarController.Inst.SelfController == null)
        {
            TipPanel.ShowToast("请先召唤 AI 伙伴");
            return false;
        }

        // 1. 外观（原地刷新，保留位置）
        var avatarData = CharacterData.DeserializeObject(sp.avatarJson);
        AIBuddyAvatarController.Inst.RefreshSelfAIBuddyAvatarWithChangeClothes(avatarData);

        // 2. 待机动作组：本体用 usingEmote，扩展包用 pendingEmote（延后到换衣动作亮相后再起，避免覆盖动作）
        var standby = (data is CabinCharacterUgcInfo ugc) ? (ugc.usingEmote ?? ugc.pendingEmote) : data.pendingEmote;
        AIBoxBuddyCallPanel.StartBuddyStandbyOn(AIBuddyAvatarController.Inst.SelfStateController, standby, AIBuddyAvatarController.ChangeSkinRevealDelay);

        // 3. 口令：使用所选皮肤自身的 voiceCommands（每套皮肤/扩展包各自的口令）
        AIBoxBuddyCallPanel.ActiveSkinVoiceCommands = data.voiceCommands;
        return true;
    }

    private void SetEquipped(CabinSkinCardItem card)
    {
        foreach (var c in _skinCards)
            c?.SetCurrent(false);
        _equippedSkinCard = card;
        if (card == null) return;
        card.SetCurrent(true);
        var sp = card._data?.skinPack;
        AIBoxBuddyCallPanel.CurrentSkinPackId = sp != null && sp.Count > 0 ? sp[0].packId : AIBoxBuddyCallPanel.CurrentSkinPackId;
    }

    private static bool IsSkinOwned(CabinCharacterBaseInfo data)
    {
        if (data is CabinCharacterUgcInfo) return true;
        if (data == null) return false;
        if (!string.IsNullOrEmpty(data.creator) && data.creator == AccountDataManager.Inst.Uid) return true;
        var inv = !string.IsNullOrEmpty(data.id) ? BagDatabase.Inst.Select(data.id) : null;
        return inv != null && inv.OwnedNum > 0;
    }

    private void HighlightSkin(CabinSkinCardItem card)
    {
        foreach (var c in _skinCards)
            c?.SetSelected(false);
        if (card != null) card.SetSelected(true);
    }

    private void ClearSkinList()
    {
        if (skinScrollRect != null && skinScrollRect.content != null)
        {
            var content = skinScrollRect.content;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                if (child == buddySkinPrefab) continue;
                Destroy(child);
            }
        }
        _skinCards.Clear();
        _pendingSkinCard = null;
        _equippedSkinCard = null;
    }

    // ──────────────── 双人载具（ToggleVehicle） ────────────────

    private void OnVehicleChange(bool isOn)
    {
        if (!isOn) return;
        HideAllSubViews();
        if (vehicleView == null) return;

        vehicleView.gameObject.SetActive(true);
        if (!_vehicleStarted)
        {
            vehicleView.OnStart(VehicleSubType.DoubleVehicle, () => { });
            vehicleView.OnItemSelectedOverride = OnVehicleSelectedForBuddy;
            _vehicleStarted = true;
        }
        vehicleView.SetDefaultMISource(UIEmoteType.Vehicle);
        // 双人载具：「社区」加载 UGC、「官方」加载 PGC
        vehicleView.LockToDouble(true);
    }

    // 点击双人载具：召唤载具（玩家驾驶）+ 伙伴上乘客位
    private void OnVehicleSelectedForBuddy(VehicleInfo vehicleInfo)
    {
        GameAIBuddyManager.Inst.RideVehicleWithBuddy(vehicleInfo, (isSuccess) =>
        {
            TipPanel.ShowToast(isSuccess ? "载具召唤中" : "请先召唤伙伴，或先下车再召唤载具");
        });
    }
}
