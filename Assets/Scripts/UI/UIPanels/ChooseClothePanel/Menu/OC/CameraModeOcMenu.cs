using Game.Audio;
using Game.Avatar;
using Game.Store;
using GameData.BaseInfo;
using Message;
using UI.UIPanels.CommonConfirm;
using UI.UIPanels.FittingRoom;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

public class CameraModeOcMenu: CameraModeMenuBase
{
    public static bool IsOcChangingActive { get; private set; }

    private CameraModeToggle player_toggle;
    private CameraModeToggle pet_toggle;

    [Header("OC Change Components")]
    [SerializeField] private OcList ocList;
    // 注意：按你的要求，这里不接 closeButton
    [SerializeField] private Button sureButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private GameObject fittingRoomCover;

    private SkinType _curSkinType = SkinType.Avatar;
    // CameraMode 属于“游戏中/拍照中”场景，逻辑对齐 OcChangeScene.Play
    private const OcChangeScene Scene = OcChangeScene.Play;
    private bool _isInChangeClothes;
    private bool _closeBySure;
    private bool _lastOcOperationState;
    private bool _isMenuVisible;

    protected override void OnInit(){
        IsOcChangingActive = false;
        player_toggle = GetComponentByName<CameraModeToggle>("TogglePlayer");
        pet_toggle = GetComponentByName<CameraModeToggle>("TogglePet");

        player_toggle.Init();
        pet_toggle.Init();
        
        player_toggle.onValueChanged.AddListener(OnPlayerChange);
        pet_toggle.onValueChanged.AddListener(OnPetChange);

        // 自动绑定（prefab 没拖引用也尽量能跑）
        if (ocList == null) ocList = GetComponentInChildren<OcList>(true);
        if (sureButton == null) sureButton = GetComponentByName<Button>("Btn_Confirm");
        if (jumpButton == null) jumpButton = GetComponentByName<Button>("GoStoreBtn");

        if (ocList != null)
        {
            ocList.SetCallback(OnOcSelect);
        }
        if (sureButton != null)
        {
            sureButton.onClick.RemoveAllListeners();
            sureButton.onClick.AddListener(OnSureClick);
        }
        if (jumpButton != null)
        {
            jumpButton.onClick.RemoveAllListeners();
            jumpButton.onClick.AddListener(OnJumpClick);
        }

        // 默认选中玩家 OC
        if (player_toggle != null) player_toggle.isOn = true;

        VipDataManager.Inst.UpdateVipStatus();

        gameObject.SetActive(false); //默认隐藏
    }
    private void OnPlayerChange(bool isOn){
        if(isOn){
            _curSkinType = SkinType.Avatar;
            if (ocList != null) ocList.gameObject.SetActive(true);
            ocList?.Init(true);
            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
        }
    }

    private void OnPetChange(bool isOn){
        if(isOn){
            _curSkinType = SkinType.Pet;
            if (ocList != null) ocList.gameObject.SetActive(true);
            ocList?.Init(false);
            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
        }
    }

    protected override void OnShow()
    {
        _isMenuVisible = true;
        IsOcChangingActive = true;
        if (ocList != null) ocList.gameObject.SetActive(true);
        _lastOcOperationState = false;
        RefreshActionButtonsVisible();

        // 从自拍通过主菜单点“更换”进入 OC 时，View 的 toggle 未经过 OnOcChange，需同步为 OC 并退出自拍，以便 SelfieBtn 可再次进入自拍
        var mainMenu = GetComponentInParent<CameraModeMainMenu>();
        if (mainMenu != null)
        {
            var viewMenu = mainMenu.GetComponentInChildren<CameraModeViewMenu>(true);
            viewMenu?.SyncToOcAndExitSelfie();
        }

        TimerManager.Inst.RunOnce("waitSelfieEnd", 0.5f, () => {
            if (!_isMenuVisible || this == null || !isActiveAndEnabled)
            {
                return;
            }
            var avatarCtrl = AvatarController.Inst;
            var selfStateCtrl = avatarCtrl != null ? avatarCtrl.SelfStateController : null;
            if(selfStateCtrl != null && selfStateCtrl.ContainsCurrentState(PlayerState.CameraMode))
            {
                return;
            }
            if (RoomEditAvatarManager.Inst != null)
            {
                RoomEditAvatarManager.Inst.OnEnterFittingRoom();
                _isInChangeClothes = true;
            }
        });

        // // 对齐 GameGuestPanel / OcChangePanel 的体验：打开列表即进入“换装中”状态（播放/保持换装动作）
        // // Avatar如果还在自拍状态，等待自拍状态结束再进入换装状态
        // if (!_isInChangeClothes)
        // {
        //     RoomEditAvatarManager.Inst.OnEnterFittingRoom();
        //     _isInChangeClothes = true;
        // }

        // 每次打开时刷新一次当前选中 Toggle 对应的列表
        if (player_toggle != null && player_toggle.isOn) OnPlayerChange(true);
        else if (pet_toggle != null && pet_toggle.isOn) OnPetChange(true);
        else if (player_toggle != null) player_toggle.isOn = true;

        //如果设置中没有打开宠物就不显示宠物的选项
        if(AccountDataManager.Inst != null && AccountDataManager.Inst.PetInfo != null && pet_toggle != null
           && AccountDataManager.Inst.PetInfo.isGameHidden == 1){
            pet_toggle.gameObject.SetActive(false);
        }

        fittingRoomCover.gameObject.SetActive(false);
    }

    protected override void OnHide()
    {
        _isMenuVisible = false;
        IsOcChangingActive = false;
        // 关闭菜单时结束“换装中”状态（对齐 GameGuestPanel.OnCloseFittingRoom）
        if (_closeBySure)
        {
            _closeBySure = false;
            return;
        }
        ExitFittingRoomIfNeeded();

        _lastOcOperationState = false;
        RefreshActionButtonsVisible();
    }

    private void OnDisable()
    {
        _isMenuVisible = false;
        IsOcChangingActive = false;
        // 兜底：如果被父级直接隐藏/销毁而非通过 Hide()，也要保证 Enter/Exit 成对。
        if (_closeBySure) return;
        ExitFittingRoomIfNeeded();
    }

    private void LateUpdate()
    {
        if (sureButton == null) return;
        var isOperation = ocList != null && ocList.IsInOperation;
        if (isOperation != _lastOcOperationState)
        {
            _lastOcOperationState = isOperation;
            RefreshActionButtonsVisible();
        }

        if (!isOperation)
        {
            sureButton.interactable = ocList != null && ocList.Selected != null;
        }
    }

    private void OnOcSelect(OcServerData ocInfo)
    {
        // CameraMode 这里不需要额外预览逻辑；确认按钮会读取 ocList.Selected
    }

    private void OnSureClick()
    {
        if (ocList == null || ocList.Selected == null) return;

        BaseAvatarData avatarData = null;
        var curSkinType = (SkinType)ocList.Selected.ocInfo.skinType;
        switch (curSkinType)
        {
            case SkinType.Pet:
                avatarData = PetData.DeserializeObject(ocList.Selected.ocInfo.avatarJson);
                if (Scene == OcChangeScene.Lobby || Scene == OcChangeScene.Play)
                {
                    if (AccountDataManager.Inst != null)
                    {
                        AccountDataManager.Inst.SyncPetAvatarData((PetData)avatarData, isSuc =>
                        {
                            if (isSuc && AvatarDataManager.Inst != null)
                            {
                                AvatarDataManager.Inst.SelfPetData = (PetData)avatarData;
                            }
                        });
                    }
                }
                break;
            case SkinType.Avatar:
            default:
                avatarData = CharacterData.DeserializeObject(ocList.Selected.ocInfo.avatarJson);
                var dataInfo = (CharacterData)avatarData;
                var shapeData = ShapeDataMgr.Inst.GetShapeData(dataInfo.bodyType);
                if (shapeData?.SaleType == ShapeSaleType.Vip && !VipDataManager.Inst.isVip)
                {
                    UIAgentManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel_Style2, new CommonSingleConfirmPanel_Style2Data()
                    {
                        CanClose = true,
                        ConfirmString = "确定",
                        ContextString = "正在使用vip体型，请开通vip后再试！",
                        TopTitleString = "提示",
                    });
                    return;
                }
                if (Scene == OcChangeScene.Lobby || Scene == OcChangeScene.Play)
                {
                    if (AccountDataManager.Inst != null)
                    {
                        AccountDataManager.Inst.SyncAvatarData(dataInfo, isSuc =>
                        {
                            if (isSuc && AvatarDataManager.Inst != null)
                            {
                                AvatarDataManager.Inst.SelfCharacterData = dataInfo;
                            }
                        });
                    }
                }
                break;
        }

        // CameraModeOcMenu 需要“直接切换衣服/形象”，对齐 GameGuestPanel.OnCloseFittingRoom 的做法：
        // 交给 RoomEditAvatarManager 走 ChangeImage 同步 + 本地 RefreshAvatar
        if (avatarData == null)
        {
            avatarData = AvatarDataManager.Inst != null ? AvatarDataManager.Inst.SelfCharacterData : null;
        }
        if (avatarData is CharacterData)
        {
            RoomEditAvatarManager.Inst?.OnExitFittingRoom((CharacterData)avatarData);
        }
        else if (avatarData is PetData)
        {
            RoomEditAvatarManager.Inst?.OnExitPetFittingRoom((PetData)avatarData);
        }

        // 换装后约1秒 RefreshAvatarByData 会被调用，部件异步加载可能重置 UserInfoHeadView 激活状态；
        // 延迟1.5秒（略晚于换装定时器）重新应用名称显示设置，确保用户偏好不被覆盖。
        TimerManager.Inst.RunOnce("cameraOcNameRestore", 1.5f, () =>
        {
            CameraModeSettingUtils.Inst?.RestoreNameSetting();
        });

        // 点确认后关闭 OcMenu（只关子菜单，不关 CameraModePanel）
        // 并且避免 OnHide 再次走一遍 OnExit 导致重复/覆盖
        _isInChangeClothes = false;
        _closeBySure = true;
        MessageHelper.Broadcast(MessageName.UICameraModeCloseCurrent);
    }

    private void OnJumpClick()
    {
        // 跳转试衣间 -> 我的 -> OC（对齐 OcChangePanel.OnJumpClick）
        FittingRoomPanel fittingRoom;
        fittingRoomCover.gameObject.SetActive(true);
        switch (_curSkinType)
        {
            case SkinType.Pet:
                fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel, true) as FittingRoomPanel;
                break;
            default:
            case SkinType.Avatar:
                fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                break;
        }

        fittingRoom?.JumpTo(MainTabs.Tab.Bag, (int)OtherClass.Oc);
        if (fittingRoom != null) fittingRoom.OnCloseAction = OnFittingRoomClose;
    }

    private void OnFittingRoomClose(CharacterData characterData)
    {
        ocList?.RefreshList();
        fittingRoomCover.gameObject.SetActive(false);

    }

    private void RefreshActionButtonsVisible()
    {
        var showMainActions = !(ocList != null && ocList.IsInOperation);
        if (sureButton != null) sureButton.gameObject.SetActive(showMainActions);
        if (jumpButton != null) jumpButton.gameObject.SetActive(showMainActions);
    }

    private void ExitFittingRoomIfNeeded()
    {
        if (!_isInChangeClothes) return;

        // 优先按当前 Toggle 类型退出；若数据缺失则回退到另一类型，避免状态悬挂。
        BaseAvatarData data = _curSkinType == SkinType.Avatar
            ? (BaseAvatarData)(AvatarDataManager.Inst != null ? AvatarDataManager.Inst.SelfCharacterData : null)
            : (BaseAvatarData)(AvatarDataManager.Inst != null ? AvatarDataManager.Inst.SelfPetData : null);

        if (data == null)
        {
            data = _curSkinType == SkinType.Avatar
                ? (BaseAvatarData)(AvatarDataManager.Inst != null ? AvatarDataManager.Inst.SelfPetData : null)
                : (BaseAvatarData)(AvatarDataManager.Inst != null ? AvatarDataManager.Inst.SelfCharacterData : null);
        }

        if (data is CharacterData characterData)
        {
            RoomEditAvatarManager.Inst?.OnExitFittingRoom(characterData);
        }
        else if (data is PetData petData)
        {
            RoomEditAvatarManager.Inst?.OnExitPetFittingRoom(petData);
        }

        _isInChangeClothes = false;
    }
}
