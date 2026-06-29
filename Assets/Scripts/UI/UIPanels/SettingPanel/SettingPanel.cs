using System;
using System.Collections.Generic;
using System.IO;
using Com.TheFallenGames.OSA.Util.IO;
using Game.GameSetting;
using GameData;
using GameData.Account;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel<SettingPanel>
{
    private CButton closeBtn;
    [SerializeField] private TabView tabView;
    private GameObject soundView;
    private GameObject petView;
    private GameObject graphicsView;
    private GameObject userCenterView;
    public Action<bool, bool, bool, AIBuddyInfo> PetAndNpcVisible { set; get; }
    // 大厅本地渲染所选伙伴皮肤（后端持久化未就绪，仅本地即时预览）
    public Action<string> RenderHallBuddyLocal { set; get; }

    //伙伴设置相关
    [SerializeField] private CabinCharacterCardItem buddyCharacterItem; //固定只显示一个，用ChangeBtn点击切换
    [SerializeField] private GameObject buddySkinItemPrefab;
    [SerializeField] private Transform skinRoot;
    [SerializeField] private Button changeCharacterBtn;

    public enum SettingViewEnum
    {
        Sound,//声音
        Pet,
        Graphics,//图像
        UserCenter//用户中心
    }
    public class SettingConfig
    {
        public string name;
        public SettingViewEnum type;
        public GameObject view;
    }
    private List<SettingConfig> rtConfig = new()
    {
        new(){name = "声音", type = SettingViewEnum.Sound},
        new(){name = "大厅", type = SettingViewEnum.Pet},
        new(){name = "图像", type = SettingViewEnum.Graphics},
        new(){name = "用户中心", type = SettingViewEnum.UserCenter},
    };
    public override void OnCreate()
    {
        InitUI();
        rtConfig[0].view = soundView;
        rtConfig[1].view = petView;
        rtConfig[2].view = graphicsView;
        rtConfig[3].view = userCenterView;
        foreach (var cfg in rtConfig)
        {
            tabView.CreateItem(cfg.type.ToString(), cfg.name).SetIsSelect(false);
        }
        tabView.AddItemSelectCallBack(SettingTabClick);
        tabView.SetSelect((int)SettingViewEnum.Sound);

    }
    private void SettingTabClick(TabItem item, int index)
    {
        // item.ItemNameText.color =
        for (int i = 0; i < rtConfig.Count; i++)
        {
            rtConfig[i].view.SetActive(false);
        }
        rtConfig[index].view.SetActive(true);
    }
    private void InitUI()
    {
        closeBtn = GameObjectEx.FindChildByName(transform,"ClosePanelBtn").GetComponent<CButton>();
        soundView= GameObjectEx.FindChildByName(transform,"SoundView" ).gameObject;
        petView = GameObjectEx.FindChildByName(transform,"PetView" ).gameObject;
        graphicsView= GameObjectEx.FindChildByName(transform,"GraphicsView" ).gameObject;
        userCenterView= GameObjectEx.FindChildByName(transform,"UserCenterView" ).gameObject;
        closeBtn.onClick.AddListener( ()=> UIManager.Inst.ClosePanel(this));
        InitSoundUI();
        InitPetUI();
        InitGraphicsUI();
        InitUserCenterUI();
    }


    #region 声音

    private Slider musicSound;
    private Slider effectSound;
    private void InitSoundUI()
    {
        musicSound = GameObjectEx.FindChildByName(soundView,"MusicSound").GetComponent<Slider>();
        effectSound = GameObjectEx.FindChildByName(soundView,"EffectSound").GetComponent<Slider>();
        musicSound.onValueChanged.AddListener(OnMusicChange);
        effectSound.onValueChanged.AddListener(OnSoundEffectChange);

        musicSound.value = GlobalSettingManager.Inst.GetBgmVolume();
        effectSound.value = GlobalSettingManager.Inst.GetSoundEffectVolume();
    }


    private GameObject buddyContainer;   // 节点 BuddyContainer：当前伙伴卡 + 切换按钮，仅展示伙伴时显示
    private GameObject skinContainer;    // 节点 SkinContainer：皮肤列表，仅展示伙伴时显示

    private GameObject showContainer;
    private Toggle selectToggle;

    private bool isHiddenPet;
    private bool isHiddenNpc;
    private bool isHiddenVehicle;
    private AIBuddyInfo selectBuddyInfo;

    // 当前展示伙伴（Cabin 角色）及其皮肤卡，构建皮肤列表用
    private CabinCharacterUgcInfo _currentBuddyInfo;
    private readonly List<CabinSkinCardItem> _skinCards = new();
    private int _skinBuildToken;
    private CabinSkinCardItem _selectedSkinCard;
    private Toggle _npcToggle; // "展示伙伴" toggle，选伙伴成功后需主动切到它
    private string _equippedPackId; // 当前装备皮肤 packId（持久化/选中态用）

    private void InitPetUI() {
        isHiddenPet = AccountDataManager.Inst.PetInfo.isHidden == 1;
        isHiddenNpc = HallCharacterManager.IsHidden;
        isHiddenVehicle = AccountDataManager.Inst.VehicleInfo == null || AccountDataManager.Inst.VehicleInfo.isHidden == 1;

        showContainer = GameObjectEx.FindChildByName(petView, "ShowContainer").gameObject;
        buddyContainer = GameObjectEx.FindChildByName(petView, "BuddyContainer").gameObject;
        skinContainer = GameObjectEx.FindChildByName(petView, "SkinContainer").gameObject;

        if (changeCharacterBtn != null) {
            changeCharacterBtn.onClick.AddListener(OpenBuddySelectPanel);
        }

        var showToggles = showContainer.GetComponentsInChildren<Toggle>();
        selectToggle = showToggles[0];
        foreach (var toggle in showToggles) {
            if (toggle.name == "ToggleNpc") {
                _npcToggle = toggle;
            }
            if (toggle.name == "TogglePet" && !isHiddenPet) {
                selectToggle = toggle;
                toggle.SetIsOnWithoutNotify(true);
            } else if (toggle.name == "ToggleNpc" && !isHiddenNpc) {
                selectToggle = toggle;
                toggle.SetIsOnWithoutNotify(true);
            } else if (toggle.name == "ToggleVehicle" && !isHiddenVehicle) {
                selectToggle = toggle;
                toggle.SetIsOnWithoutNotify(true);
            }
            toggle.onValueChanged.AddListener((isOn) => {
                if (!isOn) {
                    return;
                }
                // 选"展示伙伴"但当前无伙伴：先回退到上一个 toggle 并打开选择页，
                // 只有真正选中伙伴(OnBuddyImported)后才切到 Npc 并加载容器；未选则保持原选择。
                if (toggle.name == "ToggleNpc" && !HasAnyBuddy()) {
                    RevertToggleTo(selectToggle, toggle);
                    OpenBuddySelectPanel();
                    return;
                }
                ApplyToggleSelection(toggle);
            });
        }

        UpdateBuddyContainersVisible();
        if (!isHiddenNpc) {
            RefreshCurrentBuddyDisplay();
        }
    }

    // BuddyContainer / SkinContainer 仅在选中"展示伙伴"(ToggleNpc) 时显示
    private void UpdateBuddyContainersVisible() {
        if (buddyContainer != null) buddyContainer.SetActive(!isHiddenNpc);
        if (skinContainer != null) skinContainer.SetActive(!isHiddenNpc);
    }

    // 提交某个展示 toggle 的选择：刷新可见性/大厅、记录当前选中；展示伙伴时刷新伙伴卡+皮肤
    private void ApplyToggleSelection(Toggle toggle, bool refreshBuddyDisplay = true) {
        isHiddenPet = true;
        isHiddenNpc = true;
        isHiddenVehicle = true;
        if (toggle.name == "TogglePet") {
            isHiddenPet = false;
        }
        else if (toggle.name == "ToggleNpc") {
            isHiddenNpc = false;
        }
        else if (toggle.name == "ToggleVehicle") {
            isHiddenVehicle = false;
        }
        selectBuddyInfo = AccountDataManager.Inst.AIBuddyInfo;
        PetAndNpcVisible?.Invoke(isHiddenPet, isHiddenNpc, isHiddenVehicle, selectBuddyInfo);
        selectToggle = toggle;
        UpdateBuddyContainersVisible();
        if (!isHiddenNpc && refreshBuddyDisplay) {
            RefreshCurrentBuddyDisplay();
        }
    }

    // 是否已有可展示的伙伴（会话内已选 或 大厅已配置）
    private bool HasAnyBuddy() {
        return _currentBuddyInfo != null || HallCharacterManager.HasCharacter;
    }

    // 把展示 toggle 从 current 回退到 target（无通知，避免触发回调与循环）
    private void RevertToggleTo(Toggle target, Toggle current) {
        if (current != null) current.SetIsOnWithoutNotify(false);
        if (target != null) target.SetIsOnWithoutNotify(true);
    }

    // 拉取并展示"当前展示的伙伴"。会话内已选过则直接复用；否则取大厅角色 id 拉完整 Cabin 数据（含全部皮肤）
    private void RefreshCurrentBuddyDisplay() {
        if (_currentBuddyInfo != null) {
            ShowBuddy(_currentBuddyInfo, _equippedPackId);
            return;
        }
        var cur = HallCharacterManager.Current;
        if (cur == null || string.IsNullOrEmpty(cur.id)) {
            if (buddyCharacterItem != null) buddyCharacterItem.gameObject.SetActive(false);
            ClearSkinList();
            return;
        }
        // 大厅存的 characterInfo.skinPack 已收窄为当前装备皮肤，取其 packId 作为选中态
        _equippedPackId = cur.GetEquippedSkin()?.packId;
        CabinNetManager.Inst.GetCabinCharacterInfo(cur.id, (ok, detail) => {
            if (this == null) return;
            UnityEngine.Debug.LogError($"[HallSkin] fetch ok={ok} curId={cur.id} skinPack={detail?.characterInfo?.skinPack?.Count} ext={detail?.characterInfo?.extensionPackList?.Count}");
            if (!ok || detail?.characterInfo == null) return;
            _currentBuddyInfo = detail.characterInfo;
            ShowBuddy(_currentBuddyInfo, _equippedPackId);
        });
    }

    // 点击"切换"或选中伙伴：打开独立的角色选择面板（导入模式），确认在该面板内完成
    private void OpenBuddySelectPanel() {
        Action<CabinCharacterBaseInfo, Action<bool>> onImport = (data, onDone) => {
            var info = data as CabinCharacterUgcInfo;
            if (info == null) {
                onDone?.Invoke(false);
                return;
            }
            OnBuddyImported(info);
            onDone?.Invoke(true);
        };
        UIManager.Inst.OpenPanel<IncubationCabinRolesMainPanel>(PanelId.IncubationCabinRolesMainPanel, onImport);
    }

    // 选择面板"确认导入"回调：换伙伴默认初始皮肤，本地渲染到大厅并刷新展示
    private void OnBuddyImported(CabinCharacterUgcInfo info) {
        _currentBuddyInfo = info;
        var defaultSkin = CabinTools.GetDefaultSkin(info.skinPack);
        if (defaultSkin == null && info.skinPack != null && info.skinPack.Count > 0) {
            defaultSkin = info.skinPack[0];
        }
        _equippedPackId = defaultSkin?.packId;
        if (defaultSkin != null && !string.IsNullOrEmpty(defaultSkin.avatarJson)) {
            RenderHallBuddyLocal?.Invoke(defaultSkin.avatarJson); // 即时本地预览
        }
        // 持久化：写入所选伙伴 + 默认初始皮肤（收窄为该皮肤），展示中 isHidden=false
        HallCharacterManager.SetHallCharacter(info, defaultSkin, false);
        ShowBuddy(info, defaultSkin?.packId);

        // 选定伙伴后正式切到"展示伙伴"：选中 Npc toggle、关掉旧选择、提交可见性并显示容器
        if (_npcToggle != null) {
            if (selectToggle != null && selectToggle != _npcToggle) {
                selectToggle.SetIsOnWithoutNotify(false);
            }
            _npcToggle.SetIsOnWithoutNotify(true);
            ApplyToggleSelection(_npcToggle, refreshBuddyDisplay: false);
        }
    }

    // 展示当前伙伴卡片并构建其皮肤列表
    private void ShowBuddy(CabinCharacterUgcInfo info, string equippedPackId) {
        if (buddyCharacterItem != null) {
            buddyCharacterItem.gameObject.SetActive(true);
            buddyCharacterItem.SetShowBadge(false);
            buddyCharacterItem.SetData(info);
            buddyCharacterItem.SetSelected(false);
        }
        BuildSkinList(info, equippedPackId);
    }

    // ───────────── 皮肤列表（主体皮肤 + 扩展包，对标 AIBuddyEditSubView.BuildSkinList） ─────────────

    private void BuildSkinList(CabinCharacterUgcInfo info, string equippedPackId) {
        ClearSkinList();
        if (info == null || buddySkinItemPrefab == null || skinRoot == null) return;

        var token = ++_skinBuildToken;
        buddySkinItemPrefab.SetActive(false);

        var skinItems = new List<CabinCharacterBaseInfo>();
        // 主体皮肤：每个 skinPack 克隆成一张卡（skinPack 收窄为单个）
        if (info.skinPack != null) {
            foreach (var sp in info.skinPack) {
                var json = JsonConvert.SerializeObject(info);
                var wrapper = JsonConvert.DeserializeObject<CabinCharacterUgcInfo>(json);
                wrapper.skinPack = new List<SkinPackInfo> { sp };
                skinItems.Add(wrapper);
            }
        }

        // 扩展包皮肤（异步）
        if (info.extensionPackList != null && info.extensionPackList.Count > 0) {
            CabinNetManager.Inst.GetExtensionPackBatchInfo(info.extensionPackList, (isS, packList) => {
                if (this == null) return;
                if (token != _skinBuildToken) return; // 过期回调作废
                if (isS && packList != null) {
                    foreach (var pack in packList) {
                        if (pack == null || pack.ugcclass != (int)UGCClass.Published || pack.skinPack == null) continue;
                        foreach (var sp in pack.skinPack) {
                            var json = JsonConvert.SerializeObject(pack);
                            var wrapper = JsonConvert.DeserializeObject<CabinCharacterPackInfo>(json);
                            wrapper.skinPack = new List<SkinPackInfo> { sp };
                            skinItems.Add(wrapper);
                        }
                    }
                }
                SpawnSkinCards(skinItems, equippedPackId);
            });
        } else {
            SpawnSkinCards(skinItems, equippedPackId);
        }
    }

    private void SpawnSkinCards(List<CabinCharacterBaseInfo> datas, string equippedPackId) {
        CabinSkinCardItem firstItem = null;
        CabinSkinCardItem currentItem = null;

        int ownedCnt = 0;
        foreach (var d in datas) if (d != null && IsSkinOwned(d)) ownedCnt++;
        UnityEngine.Debug.LogError($"[HallSkin] spawn total={datas.Count} owned={ownedCnt}");

        foreach (var data in datas) {
            if (data == null) continue;
            if (!IsSkinOwned(data)) continue; // 仅展示玩家拥有的皮肤，未拥有的不展示

            var go = Instantiate(buddySkinItemPrefab, skinRoot);
            go.SetActive(true);
            var card = go.GetComponent<CabinSkinCardItem>();
            if (card == null) continue;

            card.SetData(data, _ => OnSkinClicked(card));
            card.SetLockVisible(false);   // 已过滤未拥有皮肤，恒不锁
            card.SetBadgesVisible(false);
            card.SetCurrent(false);
            _skinCards.Add(card);

            if (firstItem == null) firstItem = card;
            var sp = data.skinPack != null && data.skinPack.Count > 0 ? data.skinPack[0] : null;
            if (currentItem == null && sp != null && !string.IsNullOrEmpty(equippedPackId) && sp.packId == equippedPackId)
                currentItem = card;
        }

        SetCurrentSkin(currentItem ?? firstItem);
    }

    private void OnSkinClicked(CabinSkinCardItem card) {
        var data = card?._data;
        var sp = data?.skinPack != null && data.skinPack.Count > 0 ? data.skinPack[0] : null;
        if (sp == null || string.IsNullOrEmpty(sp.avatarJson)) return;

        _equippedPackId = sp.packId;
        RenderHallBuddyLocal?.Invoke(sp.avatarJson); // 即时本地预览
        // 持久化：当前伙伴 + 所选皮肤（收窄为该皮肤）
        if (_currentBuddyInfo != null) {
            HallCharacterManager.SetHallCharacter(_currentBuddyInfo, sp, false);
        }
        SetCurrentSkin(card);
    }

    private void SetCurrentSkin(CabinSkinCardItem card) {
        foreach (var c in _skinCards) c?.SetSelected(false);
        if (_selectedSkinCard != null) _selectedSkinCard.SetCurrent(false);
        _selectedSkinCard = card;
        if (card == null) return;
        card.SetSelected(true);
        card.SetCurrent(true);
    }

    // 主体皮肤随伙伴即拥有；扩展包皮肤按创建者或背包持有判断
    private static bool IsSkinOwned(CabinCharacterBaseInfo data) {
        if (data is CabinCharacterUgcInfo) return true;
        if (data == null) return false;
        if (!string.IsNullOrEmpty(data.creator) && data.creator == AccountDataManager.Inst.Uid) return true;
        var inv = !string.IsNullOrEmpty(data.id) ? Game.Database.BagDatabase.Inst.Select(data.id) : null;
        return inv != null && inv.OwnedNum > 0;
    }

    private void ClearSkinList() {
        foreach (var c in _skinCards)
            if (c != null) Destroy(c.gameObject);
        _skinCards.Clear();
        _selectedSkinCard = null;
    }

    public override void OnHidden()
    {
        base.OnHidden();
    }

    void OnMusicChange(float value)
    {
        GlobalSettingManager.Inst.BgmChange(value);
    }

    void OnSoundEffectChange(float value)
    {
        GlobalSettingManager.Inst.SoundEffectChange(value);
    }

    #endregion

    #region 画质
    [SerializeField] private TabView graphicsTabView;
    private CButton graphicsBtn;
    private Transform selectIcon;
    private GraphicsEnum curGraphics = GraphicsEnum.None;

    private class GraphicsConfig
    {
        public string name;
        public GraphicsEnum type;
    }
    private List<GraphicsConfig> gConfig = new()
    {
        new(){name = "高", type = GraphicsEnum.High},
        new(){name = "中", type = GraphicsEnum.Medium},
        new(){name = "低", type = GraphicsEnum.Low},
    };
    private void InitGraphicsUI()
    {
        graphicsBtn= GameObjectEx.FindChildByName(graphicsView,"GraphicsButton").GetComponent<CButton>();
        selectIcon = GameObjectEx.FindChildByName(graphicsView, "SelectIcon");
        var graphics = GlobalSettingManager.Inst.GetGraphics();
        graphics = graphics == (int)GraphicsEnum.None ? (int)GraphicsEnum.High : graphics;
        curGraphics = (GraphicsEnum)graphics;

        foreach (var cfg in gConfig)
        {
            graphicsTabView.CreateItem(cfg.type.ToString(), cfg.name).SetIsSelect(false);
        }
        graphicsTabView.AddItemSelectCallBack(GraphicsTabClick);
        graphicsTabView.SetSelect(gConfig.FindIndex(x=>x.type == curGraphics));
        graphicsBtn.onClick.AddListener(OnGraphicsButtonClick);
    }
    private void GraphicsTabClick(TabItem item, int index)
    {
        var iconParent = GameObjectEx.FindChildByName(item.transform, "SelectIconNode");
        selectIcon.SetParent(iconParent);
        selectIcon.localPosition = Vector3.one;
        curGraphics = gConfig[index].type;

    }
    private void OnGraphicsButtonClick()
    {
        GlobalSettingManager.Inst.GraphicsChange((int)curGraphics);
    }
    #endregion
    #region 用户中心

    private Text userName;
    private RemoteImageBehaviour headImg;
    private CButton logOutButton;
    private CButton userPolicyButton;
    private CButton privacyPolicyButton;
    private CButton childPolicyButton;
    private CButton thirdInfoButton;
    private CButton deleteUserButton;
    private CButton termsServiceButton;
    private void InitUserCenterUI()
    {
        userName = GameObjectEx.FindComponentByName<Text>(transform, "UserName");
        headImg= GameObjectEx.FindComponentByName<RemoteImageBehaviour>(transform, "UserHead");
        logOutButton= GameObjectEx.FindComponentByName<CButton>(transform, "LogOutButton");
        userPolicyButton= GameObjectEx.FindComponentByName<CButton>(transform, "UserPolicyButton");
        privacyPolicyButton= GameObjectEx.FindComponentByName<CButton>(transform, "PrivacyPolicyButton");
        childPolicyButton= GameObjectEx.FindComponentByName<CButton>(transform, "ChildPolicyButton");
        thirdInfoButton= GameObjectEx.FindComponentByName<CButton>(transform, "ThirdInfoButton");
        deleteUserButton= GameObjectEx.FindComponentByName<CButton>(transform, "DeleteButton");
        termsServiceButton = GameObjectEx.FindComponentByName<CButton>(transform, "TermsServiceButton");
        logOutButton.onClick.AddListener(OnLogOutClick);
        userPolicyButton.onClick.AddListener(OnUserPolicyButtonClick);
        privacyPolicyButton.onClick.AddListener(OnPrivacyPolicyButtonClick);
        childPolicyButton?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.ChildrenPolicy);
        });
        thirdInfoButton?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.ThreePartyInfo);
        });
        deleteUserButton?.onClick.AddListener(OnDeleteUserClick);
        SetUserInfo();

        termsServiceButton?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.TermsService);
        });

#if PACKAGE_TYPE_US
        childPolicyButton.gameObject.SetActive(false);
        thirdInfoButton.gameObject.SetActive(false);
        userPolicyButton.gameObject.SetActive(false);
        termsServiceButton.gameObject.SetActive(true);
#else
        childPolicyButton.gameObject.SetActive(true);
        thirdInfoButton.gameObject.SetActive(true);
        userPolicyButton.gameObject.SetActive(true);
        termsServiceButton.gameObject.SetActive(false);
#endif

    }
    private void SetUserInfo()
    {
        AccountUserInfo userInfo = AccountDataManager.Inst.UserInfo;
        userName.text = userInfo.nickname;
        var path = userInfo.portraitUrl;
        if (!string.IsNullOrEmpty(path))
        {
            headImg.Load(path, true, (from, success) => { });
        }
    }

    private void OnLogOutClick()
    {
#if PACKAGE_TYPE_US
        if (AccountDataManager.Inst.accountPlatform == AccountPlatform.Tourists)
        {
            UIManager.Inst.OpenPanel<LinkTipsPanel>(PanelId.LinkTipsPanel);
            return;
        }
#endif
        CommonConfirmPanel commonConfirmPanel =
            UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("退出登录","确定要退出登录吗？", "是的", "取消");
        commonConfirmPanel.SetOnClickAction(() =>
            {
                UIManager.Inst.OpenPanel<LogOutPanel>(PanelId.LogOutPanel);
                CloseSelf();
            }, null);
    }

    private void OnUserPolicyButtonClick()
    {
        BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.Permission);
    }
    private void OnPrivacyPolicyButtonClick()
    {
        BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.Privacy);
    }

    private void OnDeleteUserClick()
    {
       var panel = UIManager.Inst.OpenPanel<AccountDeletePanel>(PanelId.AccountDeletePanel);
       panel.DeleteSuccessAction = () => {
           CloseSelf();
           GameInstanceManager.Release();
           AccountDataManager.Inst.DeleteCache();
           UIManager.Inst.ClosePanel(PanelId.GameHallPanel);
           UIManager.Inst.OpenPanel(PanelId.SignInPanel);
           MobileInterface.Instance.SendMessage(MobileInterfaceDefine.logout,
               "");
       };
    }
    #endregion

}
