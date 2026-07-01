using System;
using Game.Database;
using Game.Store;
using GameData;
using GameData.PgcData;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Es;
using Game.AINPCStudio;
using Game.Avatar;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.IncubationCabin;

public class AIBuddyOptionPanel : BasePanel<AIBuddyOptionPanel>
{
    [SerializeField] private Button Btn_Close;
    [FormerlySerializedAs("Tog_CallAIBuddy")]
    [SerializeField] private Toggle Tog_ChangeBuddyCloths;   // 换装 tab（原召唤伙伴 tab）
    [SerializeField] private Toggle Tog_LinkAIBuddy;
    [SerializeField] private Toggle Tog_DoubleEmoteAIBuddy;
    [SerializeField] private RectTransform Rect_PgcEmoteView;
    [SerializeField] private GameObject emoContentPrefab;
    [SerializeField] private Transform emoContent;
    [SerializeField] private Text emptyTip;
    [SerializeField] private MISource animMISource;
    [SerializeField] private GameObject pgcEmoteView;
    [SerializeField] private AIBuddyUgcEmoView ugcEmoteView;
    [SerializeField] private AIBuddyOwnedView buddyOwnedView;
    [SerializeField] private GameObject tog_container;
    [SerializeField] private Toggle Tog_VehicleAIBuddy;      // 载具 tab
    [SerializeField] private UgcGameVehicleView vehicleView; // 嵌入的载具列表（仅双人）
    [SerializeField] private ScrollRect skinScrollRect;      // 换装：皮肤卡列表
    [SerializeField] private GameObject skinCardItemPrefab;  // 换装：CabinSkinCardItem 预制
    [SerializeField] private GameObject skinEmptyTip;        // 换装：未召唤/无皮肤提示（可空）
    [SerializeField] private Button skinConfirmBtn;          // 换装：确认更换按钮
    private readonly List<CabinSkinCardItem> _skinCards = new();
    private int _skinBuildToken;   // 每次重建皮肤列表自增，作废上一批扩展包异步回调
    private CabinSkinCardItem _pendingSkinCard;    // 高亮待确认的皮肤卡
    private CabinSkinCardItem _equippedSkinCard;   // 已装备的皮肤卡（显示 Current）
    private bool _vehicleViewStarted;
    private UIEmoteType curEmoType = UIEmoteType.SinglePlayer;
    private UgcAnimSubType curUgcEmoType = UgcAnimSubType.Single;
    private List<BuddyEmoConentItem> avtiveItemList = new List<BuddyEmoConentItem>();
    private List<BuddyEmoConentItem> unActivceItemList = new List<BuddyEmoConentItem>();
    public Action CloseAction { private get; set; }

    private string _selectBudddyID;

    // 地图共享 buddy 目标 key（由世界交互按钮2 在 OpenPanel 前设置，OnShow 消费）。
    // 用静态传参避免与 OnShow 既有的 args[0] is string（AIHospital NPC）模式冲突。
    public static string MapBuddyTargetKey { get; set; } = string.Empty;
    // 本次会话的地图共享 buddy key（AINpcInMap_{entityUid}）；空=本人 buddy/AI游戏模式
    private string _mapBuddyKey = string.Empty;

    public override void OnCreate()
    {
        base.OnCreate();
        Btn_Close.onClick.AddListener(OnCloseBtnClick);
        Tog_ChangeBuddyCloths.onValueChanged.AddListener(OnChangeBuddyClothsChange);
        if (skinConfirmBtn != null)
            skinConfirmBtn.onClick.AddListener(OnSkinConfirmClick);
        Tog_DoubleEmoteAIBuddy.onValueChanged.AddListener(OnDoublePlayerEmoChanged);
        Tog_LinkAIBuddy.onValueChanged.AddListener(OnLinkEmoteChanged);
        if (Tog_VehicleAIBuddy != null)
            Tog_VehicleAIBuddy.onValueChanged.AddListener(OnVehicleAIBuddyChange);
        animMISource.SetCallback(OnValueChange);
        // skinConfirmBtn/skinEmptyTip are children of pgcEmoteView in the prefab; reparent so pgcEmoteView.SetActive(false) in the skin tab doesn't hide them.
        if (skinConfirmBtn != null && skinScrollRect != null)
        {
            skinConfirmBtn.transform.SetParent(skinScrollRect.transform.parent, true);
            skinConfirmBtn.gameObject.SetActive(false);
        }
        if (skinEmptyTip != null && skinScrollRect != null)
        {
            skinEmptyTip.transform.SetParent(skinScrollRect.transform.parent, true);
            skinEmptyTip.gameObject.SetActive(false);
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        ugcEmoteView.OnStart(curUgcEmoType, OnCloseBtnClick);
        ugcEmoteView.gameObject.SetActive(false);


        // 消费地图共享 buddy 目标 key（世界交互按钮2 → 对地图 buddy 发起双人交互）
        _mapBuddyKey = MapBuddyTargetKey ?? string.Empty;
        MapBuddyTargetKey = string.Empty;
        bool isMapBuddy = !string.IsNullOrEmpty(_mapBuddyKey);

        bool isAIGame;
        if (!isMapBuddy && args != null && args.Length >= 1 && args[0] is string id)
        {
            isAIGame = true;
            _selectBudddyID = id;
            Tog_LinkAIBuddy.isOn = false;
            Tog_LinkAIBuddy.isOn = true;
        }
        else if (isMapBuddy)
        {
            // 地图共享 buddy：默认进双人动作页；换装属拥有者编辑，对其不可用（下方隐藏换装页）
            isAIGame = false;
            _selectBudddyID = string.Empty;
            Tog_ChangeBuddyCloths.isOn = false;
            Tog_DoubleEmoteAIBuddy.isOn = false;
            Tog_DoubleEmoteAIBuddy.isOn = true;
        }
        else
        {
            isAIGame = false;
            _selectBudddyID = string.Empty;
            // 置反再置真即可触发 OnChangeBuddyClothsChange(true)，不要再显式调用，否则皮肤列表会构建两次
            Tog_ChangeBuddyCloths.isOn = false;
            Tog_ChangeBuddyCloths.isOn = true;
        }
        // 旧的「召唤伙伴」列表已由 AIBoxBuddyCallPanel 取代，此处统一隐藏
        buddyOwnedView.gameObject.SetActive(false);
        tog_container.SetActive(!isAIGame);
        // 地图共享 buddy 模式隐藏换装页（换装逻辑硬绑本人 buddy）
        Tog_ChangeBuddyCloths.gameObject.SetActive(!isMapBuddy);
        // AIBuddyInMap 不支持牵手和乘坐载具，隐藏对应 tab
        Tog_LinkAIBuddy.gameObject.SetActive(!isMapBuddy);
        if (Tog_VehicleAIBuddy != null)
            Tog_VehicleAIBuddy.gameObject.SetActive(!isMapBuddy);
        if (vehicleView != null)
            vehicleView.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        CloseAction?.Invoke();
    }


    private void OnValueChange(MISource.Source source)
    {
        // 载具页：animMISource 切 官方PGC/社区UGC；resMISource(创作/购买) 仅 UGC 时显示
        if (Tog_VehicleAIBuddy != null && Tog_VehicleAIBuddy.isOn)
        {
            pgcEmoteView.SetActive(false);
            ugcEmoteView.gameObject.SetActive(false);
            // 切换瞬间换装页 off 回调会先于 OnVehicleAIBuddyChange 触发本方法；此时载具列表尚未 OnStart，等其启动后再由 DefualtOn 重新进入
            if (!_vehicleViewStarted) return;
            bool isUgc = source != MISource.Source.Bud;
            vehicleView.gameObject.SetActive(true);
            vehicleView.resMISource.gameObject.SetActive(isUgc);
            vehicleView.SetSourcePgc(!isUgc);
            return;
        }
        pgcEmoteView.SetActive(source == MISource.Source.Bud);
        ugcEmoteView.gameObject.SetActive(source != MISource.Source.Bud);
        ugcEmoteView.resMISource.gameObject.SetActive(source != MISource.Source.Bud);
        ugcEmoteView.SetDefaultMISource();
        // emoContent 中的 PGC item 不在 pgcEmoteView 层级下，需手动同步：
        // 切 UGC 时隐藏所有 PGC item；切回 PGC 时重建列表（否则 PGC item 遮挡 UGC 视图 / PGC 页空白）
        if (source != MISource.Source.Bud)
            SetAllItemUnActive();
        else
            ShowEmoConent();
    }


    private void OnCloseBtnClick()
    {
        CloseSelf();
    }

    private void OnChangeBuddyClothsChange(bool isOn)
    {
        ShowCheckedIcon(isOn, Tog_ChangeBuddyCloths.transform);
        if (!isOn)
        {
            if (skinScrollRect != null) skinScrollRect.gameObject.SetActive(false);
            if (skinEmptyTip != null) skinEmptyTip.SetActive(false);
            if (skinConfirmBtn != null) skinConfirmBtn.gameObject.SetActive(false);
            return;
        }

        SetAllItemUnActive();
        // 隐藏动作 / 载具相关视图
        pgcEmoteView.SetActive(false);
        ugcEmoteView.gameObject.SetActive(false);
        ugcEmoteView.resMISource.gameObject.SetActive(false);
        animMISource.gameObject.SetActive(false);
        if (vehicleView != null) vehicleView.gameObject.SetActive(false);

        if (skinScrollRect != null) skinScrollRect.gameObject.SetActive(true);
        if (skinConfirmBtn != null) skinConfirmBtn.gameObject.SetActive(true);
        BuildSkinList();
    }

    /// <summary>
    /// 构建当前已召唤 AI 伙伴的皮肤列表：本体 skinPack + 扩展包（CabinCharacterPackInfo）。
    /// 每个 SkinPackInfo 一张卡，选中后切换伙伴外观、待机动作组与口令。
    /// </summary>
    private void BuildSkinList()
    {
        ClearSkinList();
        var token = ++_skinBuildToken;

        var info = AIBoxBuddyCallPanel.CurrentSummonedInfo;
        if (skinScrollRect == null || skinCardItemPrefab == null)
            return;

        skinCardItemPrefab.SetActive(false);

        if (info == null)
        {
            if (skinEmptyTip != null) skinEmptyTip.SetActive(true);
            return;
        }
        if (skinEmptyTip != null) skinEmptyTip.SetActive(false);

        var skinItems = new List<CabinCharacterBaseInfo>();

        // 本体皮肤：克隆本体，skinPack 收窄为单个，保留 usingEmote / voiceCommands / activation
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

        // 扩展包皮肤：异步拉取，每个 pack 的每个 SkinPackInfo 一张卡
        if (info.extensionPackList != null && info.extensionPackList.Count > 0)
        {
            CabinNetManager.Inst.GetExtensionPackBatchInfo(info.extensionPackList, (isS, packList) =>
            {
                if (this == null || gameObject == null) return;
                if (token != _skinBuildToken) return;   // 已有更新的重建，丢弃本次过期回调
                if (isS && packList != null)
                {
                    foreach (var pack in packList)
                    {
                        if (pack == null || pack.ugcclass != (int)UGCClass.Published || pack.skinPack == null)
                            continue;
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

            var go = Instantiate(skinCardItemPrefab, skinScrollRect.content);
            go.SetActive(true);
            var card = go.GetComponent<CabinSkinCardItem>();
            if (card == null) continue;

            // 点击仅高亮选中，不立即更换（由 Confirm 按钮触发）
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

        // 当前装备皮肤：优先按 CurrentSkinPackId 匹配，回退默认皮肤 / 第一张
        var current = equippedItem ?? defaultItem ?? firstItem;
        SetEquipped(current);
        // 进入时默认选中当前皮肤（仅高亮，不重复应用）
        HighlightSkin(current);
        _pendingSkinCard = current;
    }

    /// <summary>点击皮肤卡：只高亮、记录待确认，不立即更换。</summary>
    private void OnSkinClicked(CabinSkinCardItem card)
    {
        HighlightSkin(card);
        _pendingSkinCard = card;
    }

    /// <summary>点击确认：把待选皮肤真正应用到伙伴，并更新"当前"标记。</summary>
    private void OnSkinConfirmClick()
    {
        LoggerUtils.LogError($"[SkinConfirm] pendingNull={_pendingSkinCard == null} sameAsEquipped={_pendingSkinCard == _equippedSkinCard} owned={(_pendingSkinCard != null && IsSkinOwned(_pendingSkinCard._data))}");
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

    /// <summary>应用皮肤：切换伙伴外观 + 待机动作组 + 口令/激活。成功返回 true。</summary>
    private bool ApplySkin(CabinSkinCardItem card)
    {
        var data = card?._data;
        var sp = data?.skinPack != null && data.skinPack.Count > 0 ? data.skinPack[0] : null;
        LoggerUtils.LogError($"[SkinApply] spNull={sp == null} jsonEmpty={(sp == null || string.IsNullOrEmpty(sp.avatarJson))} selfCtrl={AIBuddyAvatarController.Inst.SelfController != null}");
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
        LoggerUtils.LogError($"[SkinApply] avatarDataNull={avatarData == null} parts={(avatarData?.partDatas?.Count ?? -1)} selfWrapNull={AIBuddyAvatarController.Inst.SelfWrap == null}");
        AIBuddyAvatarController.Inst.RefreshSelfAIBuddyAvatarWithChangeClothes(avatarData);

        // 2. 待机动作组：本体用 usingEmote，扩展包用 pendingEmote（延后到换衣动作亮相后再起，避免覆盖动作）
        var standby = (data is CabinCharacterUgcInfo ugc) ? (ugc.usingEmote ?? ugc.pendingEmote) : data.pendingEmote;
        AIBoxBuddyCallPanel.StartBuddyStandbyOn(AIBuddyAvatarController.Inst.SelfStateController, standby, AIBuddyAvatarController.ChangeSkinRevealDelay);

        // 3. 口令：使用所选皮肤自身的 voiceCommands（与 IncubationCabinRolesPanel node3 取数一致：
        //    每套皮肤/扩展包各自的口令），不回退主体，保证切皮肤后口令随之切换。
        AIBoxBuddyCallPanel.ActiveSkinVoiceCommands = data.voiceCommands;
        return true;
    }

    /// <summary>设为"当前装备"皮肤：显示其 Current 标记，记忆 packId。</summary>
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

    /// <summary>皮肤是否已拥有：主体随召唤拥有；扩展包按创建者/背包判断。</summary>
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
        if (skinScrollRect != null)
        {
            var content = skinScrollRect.content;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                if (child == skinCardItemPrefab) continue;
                Destroy(child);
            }
        }
        _skinCards.Clear();
        _pendingSkinCard = null;
        _equippedSkinCard = null;
    }

    private void OnDoublePlayerEmoChanged(bool isOn)
    {
        ShowCheckedIcon(isOn, Tog_DoubleEmoteAIBuddy.transform);

        if (isOn)
        {
            Rect_PgcEmoteView.offsetMax = new Vector2(0, -124);  
            curEmoType = UIEmoteType.DoublePlayer;
            curUgcEmoType = UgcAnimSubType.Double;
            ShowEmoConent();
        }
    }

    private void OnLinkEmoteChanged(bool isOn)
    {
        ShowCheckedIcon(isOn, Tog_LinkAIBuddy.transform);

        if (isOn)
        {
            // 设置右、上边距
            Rect_PgcEmoteView.offsetMax = new Vector2(0, 0);
            curEmoType = UIEmoteType.LinkEmote;
            curUgcEmoType = UgcAnimSubType.LinkEmote;
            ShowEmoConent();
        }
    }

    private void OnVehicleAIBuddyChange(bool isOn)
    {
        ShowCheckedIcon(isOn, Tog_VehicleAIBuddy.transform);

        if (isOn)
        {
            SetAllItemUnActive();

            // 显示载具列表，仅双人
            vehicleView.gameObject.SetActive(true);
            if (!_vehicleViewStarted)
            {
                vehicleView.OnStart(GameData.PgcData.VehicleSubType.DoubleVehicle, OnCloseBtnClick);
                vehicleView.OnItemSelectedOverride = OnVehicleSelectedForBuddy;
                _vehicleViewStarted = true;
            }
            vehicleView.LockToDouble();
            // animMISource(官方PGC/社区UGC) 默认官方；触发 OnValueChange 进入载具分支，编排 PGC/UGC 与 resMISource(创作/购买)
            animMISource.DefualtOn(MISource.Source.Bud);
        }
        else
        {
            vehicleView.gameObject.SetActive(false);
            vehicleView.resMISource.gameObject.SetActive(false);
        }
    }

    // 点击双人载具：召唤载具（玩家驾驶）+ 伙伴上乘客位
    private void OnVehicleSelectedForBuddy(VehicleInfo vehicleInfo)
    {
        GameAIBuddyManager.Inst.RideVehicleWithBuddy(vehicleInfo, (isSuccess) =>
        {
            TipPanel.ShowToast(isSuccess ? "载具召唤中" : "请先召唤伙伴，或先下车再召唤载具");
        }, _mapBuddyKey);
    }

    private void ShowCheckedIcon(bool isChecked, Transform toggle)
    {
        var iconChecked = toggle.Find("Background/iconChecked").gameObject;
        var iconUnChecked = toggle.Find("Background/iconUnChecked").gameObject;

        iconChecked.SetActive(isChecked);
        iconUnChecked.SetActive(!isChecked);
        
        //双人牵手不显示"官方/社区"
        if (toggle == Tog_LinkAIBuddy.transform && isChecked)
        {
            animMISource.DefualtOn(MISource.Source.Bud);
            animMISource.gameObject.SetActive(false);
        }
        else if (toggle == Tog_ChangeBuddyCloths.transform)
        {
            animMISource.DefualtOn(MISource.Source.Bud);
            animMISource.gameObject.SetActive(false);
        }
        else
        {
            animMISource.gameObject.SetActive(true);
        }
    }

    private void ShowEmoConent()
    {
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
        ugcEmoteView.ChangeAnimType(curUgcEmoType);
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
            emoContentItem.transform.SetSiblingIndex(i);
            emoContentItem.InitData(emoteDataList[i], OnCloseBtnClick,_selectBudddyID, _mapBuddyKey);
        }

        emptyTip.gameObject.SetActive(emoteDataList.Count <= 0);
        if (emoteDataList.Count <= 0)
        {
            emptyTip.SetLocalText(curEmoType switch
                {
                    UIEmoteType.SinglePlayer => "无单人动作，可前往商城获取",
                    UIEmoteType.DoublePlayer => "无双人动作，可前往商城获取",
                    UIEmoteType.PetSingle => "无宠物动作，可前往商城获取",
                    UIEmoteType.PetWithPlayer => "无宠物与人交互动作，可前往商城获取",
                    UIEmoteType.LinkEmote => "无牵手动作，可前往商城获取",
                    _ => "无动作，可前往商城获取"
                }
            );
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
        
        buddyOwnedView.gameObject.SetActive(false);
    }

    private BuddyEmoConentItem GetEmoItem()
    {
        BuddyEmoConentItem emoContentItem;

        if (unActivceItemList.Count == 0)
        {
            emoContentItem = GameObject.Instantiate(emoContentPrefab, emoContent).GetComponent<BuddyEmoConentItem>();
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
}