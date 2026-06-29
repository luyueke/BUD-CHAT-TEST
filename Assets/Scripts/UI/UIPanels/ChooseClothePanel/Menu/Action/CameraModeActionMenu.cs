using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Database;
using Game.Store;
using GameData;
using GameData.PgcData;
using Message;
using Product;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class CameraModeActionMenu : CameraModeMenuBase
{
    private enum ActionToggleTab
    {
        Single,
        SelfPose,
        Double,
        Pet,
        PlayerPet,
        Link
    }

    private CameraModeToggle single_toggle;
    private CameraModeToggle selfPose_toggle;
    private CameraModeToggle double_toggle;
    private CameraModeToggle pet_toggle;
    private CameraModeToggle playerPet_toggle;
    private CameraModeToggle link_toggle;

    [Header("List Components")]
    [SerializeField] private GameObject emoContentPrefab;
    [SerializeField] private GameObject cameraSelfieItemPrefab;
    [SerializeField] private ScrollRect pgcScrollRect;
    [SerializeField] private Transform emoContent;
    [SerializeField] private Text emptyTip;
    [SerializeField] private MISource animMISource;
    [SerializeField] private GameObject pgcEmoteView;
    [SerializeField] private UgcEmoView ugcEmoteView;
    [SerializeField] private Transform selfieContent;
    [SerializeField] private GameObject selfieScrollRect;
    [SerializeField] private CameraModeActionBusiness cameraModeActionBusiness;
    
    private UIEmoteType curEmoType = UIEmoteType.SinglePlayer;
    private UgcAnimSubType curUgcEmoType = UgcAnimSubType.Single;
    private MISource.Source curSource = MISource.Source.Bud;
    private ActionToggleTab lastSelectedTab = ActionToggleTab.Single;

    private readonly List<EmoContentItem> avtiveItemList = new List<EmoContentItem>();
    private readonly List<EmoContentItem> unActivceItemList = new List<EmoContentItem>();
    
    private readonly List<CameraSelfieItem> activeSelfieItemList = new List<CameraSelfieItem>();
    private readonly List<CameraSelfieItem> inactiveSelfieItemList = new List<CameraSelfieItem>();

    protected override void OnInit(){

        single_toggle = GetComponentByName<CameraModeToggle>("ToggleSingle");
        selfPose_toggle = GetComponentByName<CameraModeToggle>("ToggleSelfPose");
        double_toggle = GetComponentByName<CameraModeToggle>("ToggleDouble");
        pet_toggle = GetComponentByName<CameraModeToggle>("TogglePet");
        playerPet_toggle = GetComponentByName<CameraModeToggle>("TogglePlayerPet");
        link_toggle = GetComponentByName<CameraModeToggle>("ToggleLink");

        single_toggle.Init();
        selfPose_toggle.Init(); // 当前需求中不需要（等同 EmoMenuPanel 没有此 Tab）
        double_toggle.Init();
        pet_toggle.Init();
        playerPet_toggle.Init();
        link_toggle.Init();

        single_toggle.onValueChanged.AddListener(OnSingleChange);
        selfPose_toggle.onValueChanged.AddListener(OnSelfPoseChange);
        double_toggle.onValueChanged.AddListener(OnDoubleChange);
        pet_toggle.onValueChanged.AddListener(OnPetChange);
        playerPet_toggle.onValueChanged.AddListener(OnPlayerPetChange);
        link_toggle.onValueChanged.AddListener(OnLinkChange);

        // 按需求：vehicleToggle 不需要；SelfPose 也不在 EmoMenuPanel 里，先隐藏避免影响切换
        //if (selfPose_toggle != null) selfPose_toggle.gameObject.SetActive(false);

        // 尝试自动绑定（如果 prefab 没拖引用，也能跑起来）
        if (animMISource == null)
        {
            animMISource = GetComponentByName<MISource>("MISourceRoot");
        }
        if (pgcScrollRect == null)
        {
            pgcScrollRect = GetComponentByName<ScrollRect>("EmoScrollView");
        }
        if (pgcScrollRect != null && pgcScrollRect.content != null)
        {
            emoContent = pgcScrollRect.content;
            if (emptyTip == null)
            {
                emptyTip = GameObjectEx.FindComponentByName<Text>(pgcScrollRect.transform, "EmptyTip");
            }
            if (pgcEmoteView == null)
            {
                // 以 ScrollView 作为 PGC 容器
                pgcEmoteView = pgcScrollRect.gameObject;
            }
        }
        if (ugcEmoteView == null)
        {
            ugcEmoteView = GetComponentInChildren<UgcEmoView>(true);
        }

        if (animMISource != null)
        {
            animMISource.SetCallback(OnSourceChanged);
        }

        if (ugcEmoteView != null)
        {
            // closeSelf 在 CameraMode 子菜单里没有关闭按钮，这里传空；如果后续要关闭整个 CameraModePanel 再补
            ugcEmoteView.OnStart(curUgcEmoType, null);
            ugcEmoteView.gameObject.SetActive(false);
        }

        // 默认选中单人
        if (single_toggle != null) single_toggle.isOn = true;

        gameObject.SetActive(false); //默认隐藏
        if(cameraModeActionBusiness != null) cameraModeActionBusiness.gameObject.SetActive(false);

        MessageHelper.AddListener<bool>(MessageName.SelfieMode, OnSelfieRequest);
    }

    private void OnDestroy() {
        MessageHelper.RemoveListener<bool>(MessageName.SelfieMode, OnSelfieRequest);
    }

    private void OnSingleChange(bool isOn){
        if(isOn){
            lastSelectedTab = ActionToggleTab.Single;
            curEmoType = UIEmoteType.SinglePlayer;
            curUgcEmoType = UgcAnimSubType.Single;
            ShowEmoConent();
            ShowViewStatus();
        }
    }

    private void OnSelfPoseChange(bool isOn){
        if(isOn){
            lastSelectedTab = ActionToggleTab.SelfPose;
            curEmoType = UIEmoteType.CameraSelfiePose;
            ShowEmoConent();
            ShowViewStatus();
            selfieScrollRect.SetActive(true);
        }else{
            selfieScrollRect.SetActive(false);
        }
    }

    private void OnDoubleChange(bool isOn){
        if(isOn){
            lastSelectedTab = ActionToggleTab.Double;
            curEmoType = UIEmoteType.DoublePlayer;
            curUgcEmoType = UgcAnimSubType.Double;
            ShowEmoConent();
            ShowViewStatus();
        }
    }

    private void OnPetChange(bool isOn){
        if(isOn){
            lastSelectedTab = ActionToggleTab.Pet;
            curEmoType = UIEmoteType.PetSingle;
            curUgcEmoType = UgcAnimSubType.PetSingle;
            ShowEmoConent();
            ShowViewStatus();
        }
    }

    private void OnPlayerPetChange(bool isOn){
        if(isOn){
            lastSelectedTab = ActionToggleTab.PlayerPet;
            curEmoType = UIEmoteType.PetWithPlayer;
            curUgcEmoType = UgcAnimSubType.PetWithPlayer;
            ShowEmoConent();
            ShowViewStatus();
        }
    }

    private void OnLinkChange(bool isOn){
        if(isOn){
            lastSelectedTab = ActionToggleTab.Link;
            // 牵手：仅 PGC（对齐 EmoMenuPanel + AIBuddyOptionPanel 的规则）
            curEmoType = UIEmoteType.LinkEmote;
            curUgcEmoType = UgcAnimSubType.LinkEmote;
            if (animMISource != null)
            {
                animMISource.DefualtOn(MISource.Source.Bud);
            }
            ShowEmoConent();
            ShowViewStatus();
        }
    }

    protected override void OnShow()
    {
        //如果设置中没有打开宠物就不显示宠物的选项
        if(AccountDataManager.Inst.PetInfo.isGameHidden == 1){
            pet_toggle.gameObject.SetActive(false);
            playerPet_toggle.gameObject.SetActive(false);
        }

        ApplyLastToggleSelection();
    }

    protected override void OnHide()
    {
        
    }

    private void OnSourceChanged(MISource.Source source)
    {
        curSource = source;
        ShowViewStatus();
    }

    private void ShowViewStatus()
    {
        // 牵手：强制 PGC，不允许切换到 UGC
        bool isLink = curEmoType == UIEmoteType.LinkEmote || curEmoType == UIEmoteType.CameraSelfiePose;
        var effectiveSource = isLink ? MISource.Source.Bud : curSource;
        if (isLink) curSource = MISource.Source.Bud;
        SyncAnimMISourceVisual(effectiveSource);

        bool showPgc = effectiveSource == MISource.Source.Bud;
        if (pgcEmoteView != null) pgcEmoteView.SetActive(showPgc);

        // Link tab：只有 PGC 才显示（与 EmoMenuPanel 一致）
        if (link_toggle != null) link_toggle.gameObject.SetActive(showPgc);

        if (ugcEmoteView != null)
        {
            ugcEmoteView.gameObject.SetActive(!showPgc);
            if (ugcEmoteView.resMISource != null) ugcEmoteView.resMISource.gameObject.SetActive(!showPgc);
            if (!showPgc)
            {
                ugcEmoteView.SetDefaultMISource(curEmoType);
                ugcEmoteView.ChangeAnimType(curUgcEmoType);
                
                // 防遮挡：确保 UGC 视图层级在最上
                ugcEmoteView.transform.SetAsLastSibling();
                if (ugcEmoteView.resMISource != null) ugcEmoteView.resMISource.transform.SetAsLastSibling();
            }
        }

        // 主 MISource：牵手时隐藏；其它类型显示
        if (animMISource != null)
        {
            animMISource.gameObject.SetActive(!isLink);
            if (!isLink)
            {
                // 可能被列表盖住，强制置顶
                animMISource.transform.SetAsLastSibling();
            }
        }
    }

    /// <summary>
    /// 同步 MISource 视觉状态，避免外部切 tab 只改了 curSource 但 Toggle 显示未跟上。
    /// </summary>
    private void SyncAnimMISourceVisual(MISource.Source source)
    {
        if (animMISource == null) return;
        animMISource.DefualtOnWithoutNotify(source);
    }

    private void ShowEmoConent()
    {
        SetAllItemUnActive();

        List<InventoryData> owned = new();
        List<EmoUIConfig> emoteDataList = new();
        switch (curEmoType)
        {
            case UIEmoteType.SinglePlayer:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.Single)));
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.SingleLoop)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.Single && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.SingleLoop && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
            case UIEmoteType.DoublePlayer:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.Double)));
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.DoubleLoop)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.Double && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.DoubleLoop && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
            case UIEmoteType.PetSingle:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetSingle)));
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetSingleLoop)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.PetSingle && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.PetSingleLoop && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
            case UIEmoteType.PetWithPlayer:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetWithPlayer)));
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetWithPlayerLoop)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.PetWithPlayer && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.PetWithPlayerLoop && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
            case UIEmoteType.LinkEmote:
                owned.AddRange(BagDatabase.Inst.SelectAll(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.LinkEmote)));
                emoteDataList.AddRange(Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoType == (int)EmoteSubType.LinkEmote && AssetsDataManager.IsFreeAssets(emoData.pgcId)));
                break;
            case UIEmoteType.CameraSelfiePose:
                var selfiePoses = Es.DataTables.GetCameraSelfiePoseList();
                int selfieIndex = 0;

                // 默认项：原始自拍（固定使用 pose0 图标）
                {
                    var defaultItem = GetSelfieItem();
                    if (defaultItem != null)
                    {
                        defaultItem.transform.SetSiblingIndex(selfieIndex++);
                        defaultItem.InitDefaultData("原始自拍", "pose0", SelectSelfieItem);
                    }
                }

                for (int i = 0; i < selfiePoses.Count; i++)
                {

                    var item = GetSelfieItem();
                    if (item == null) continue;
                    item.transform.SetSiblingIndex(selfieIndex++);
                    // 点击逻辑先不做，回调传 null
                    item.InitData(selfiePoses[i], SelectSelfieItem);
                }

                if (emptyTip != null)
                {
                    emptyTip.gameObject.SetActive(selfieIndex <= 0);
                    if (selfieIndex <= 0)
                    {
                        emptyTip.SetLocalText("无自拍姿势，可前往商城获取");
                    }
                }

                SelectSelfieItem(true, null);
                return;
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

            return bTime.CompareTo(aTime);
        });

        for (int i = 0; i < emoteDataList.Count; i++)
        {
            var emoContentItem = GetEmoItem();
            if (emoContentItem == null) continue;
            emoContentItem.transform.SetSiblingIndex(i);
            // CameraMode 里不需要点了就关闭面板，所以 close 传 null
            emoContentItem.InitData(emoteDataList[i], null);
        }

        if (emptyTip != null)
        {
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

        foreach (var item in activeSelfieItemList)
        {
            inactiveSelfieItemList.Add(item);
            item.gameObject.SetActive(false);
        }
        activeSelfieItemList.Clear();
    }

    private EmoContentItem GetEmoItem()
    {
        EmoContentItem emoContentItem;
        if (unActivceItemList.Count == 0)
        {
            if (emoContentPrefab == null || emoContent == null) return null;
            emoContentItem = GameObject.Instantiate(emoContentPrefab, emoContent).GetComponent<EmoContentItem>();
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

    private CameraSelfieItem GetSelfieItem()
    {
        CameraSelfieItem item;
        if (inactiveSelfieItemList.Count == 0)
        {
            if (selfieContent == null) return null;
            var prefab = cameraSelfieItemPrefab != null ? cameraSelfieItemPrefab : emoContentPrefab;
            if (prefab == null) return null;

            var go = GameObject.Instantiate(prefab, selfieContent);
            item = go.GetComponent<CameraSelfieItem>();
            if (item == null) item = go.AddComponent<CameraSelfieItem>();
        }
        else
        {
            item = inactiveSelfieItemList[0];
            item.gameObject.SetActive(true);
            inactiveSelfieItemList.RemoveAt(0);
        }

        activeSelfieItemList.Add(item);
        return item;
    }

    private void SelectSelfieItem(bool isOwned, string selfieId = null){

        if(isOwned){
            foreach (var item in activeSelfieItemList)
            {
                item.SetSelected(false);
                if(!string.IsNullOrEmpty(selfieId) && item.GetId() == selfieId){
                    item.SetSelected(true);
                }
            }
        }

        if(!isOwned && !string.IsNullOrEmpty(selfieId) && cameraModeActionBusiness != null){
            var config = Es.DataTables.GetCameraSelfiePose(selfieId);
            if(config == null) return;
            if(config.gainType == (int)StoreSkipType.Item){
                Debug.Log("物品跳转");
                cameraModeActionBusiness.gameObject.SetActive(true);
                cameraModeActionBusiness.Init(selfieId, OnBuySelfiePose);
            }else if(config.gainType == (int)StoreSkipType.GiftPack){
                //礼包跳转
                Debug.Log("礼包跳转");
            }else if(config.gainType == (int)StoreSkipType.LimitedEvents){
                //活动跳转
                Debug.Log("活动跳转");
            }
        }
    }

    private void CheckSelfieItemOwnership(){
        foreach (var item in activeSelfieItemList)
        {
            item.RefreshOwnStatus();
        }
    }

    private void OnBuySelfiePose(bool isOwned, string selfieId){
        if(isOwned){
            SelectSelfieItem(true, selfieId);
            MessageHelper.Broadcast(MessageName.UICameraModeSelfieRequest, selfieId);
            CheckSelfieItemOwnership();
        }
    }

    private void OnSelfieRequest(bool isSelfie){
        if(!isSelfie){
            SelectSelfieItem(true, null);
        }
    }

    private void ApplyLastToggleSelection()
    {
        if (TrySetToggleOn(lastSelectedTab))
        {
            return;
        }

        // 容错：上一次 tab 当前不可用（如宠物入口被隐藏）时，回落到单人。
        if (!TrySetToggleOn(ActionToggleTab.Single))
        {
            if (single_toggle != null && single_toggle.isOn) OnSingleChange(true);
            else if (double_toggle != null && double_toggle.isOn) OnDoubleChange(true);
            else if (pet_toggle != null && pet_toggle.isOn) OnPetChange(true);
            else if (playerPet_toggle != null && playerPet_toggle.isOn) OnPlayerPetChange(true);
            else if (link_toggle != null && link_toggle.isOn) OnLinkChange(true);
        }
    }

    private bool TrySetToggleOn(ActionToggleTab tab)
    {
        var targetToggle = GetToggleByTab(tab);
        if (targetToggle == null || !targetToggle.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (targetToggle.isOn)
        {
            // isOn 未变化时不会触发回调，这里手动刷新一次内容。
            RefreshByTab(tab);
            return true;
        }

        targetToggle.isOn = true;
        return true;
    }

    private CameraModeToggle GetToggleByTab(ActionToggleTab tab)
    {
        switch (tab)
        {
            case ActionToggleTab.Single:
                return single_toggle;
            case ActionToggleTab.SelfPose:
                return selfPose_toggle;
            case ActionToggleTab.Double:
                return double_toggle;
            case ActionToggleTab.Pet:
                return pet_toggle;
            case ActionToggleTab.PlayerPet:
                return playerPet_toggle;
            case ActionToggleTab.Link:
                return link_toggle;
            default:
                return single_toggle;
        }
    }

    private void RefreshByTab(ActionToggleTab tab)
    {
        switch (tab)
        {
            case ActionToggleTab.Single:
                OnSingleChange(true);
                break;
            case ActionToggleTab.SelfPose:
                OnSelfPoseChange(true);
                break;
            case ActionToggleTab.Double:
                OnDoubleChange(true);
                break;
            case ActionToggleTab.Pet:
                OnPetChange(true);
                break;
            case ActionToggleTab.PlayerPet:
                OnPlayerPetChange(true);
                break;
            case ActionToggleTab.Link:
                OnLinkChange(true);
                break;
            default:
                OnSingleChange(true);
                break;
        }
    }
}
