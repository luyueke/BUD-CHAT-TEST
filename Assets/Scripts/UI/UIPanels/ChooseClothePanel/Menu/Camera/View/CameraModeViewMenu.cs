using Game.Audio;
using Game.Avatar;
using Game.Utils;
using Message;
using UI.UIPanels;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CameraModeViewMenu : CameraModeMenuBase
{
    private enum ViewModeType
    {
        Oc,
        Follow,
        FirstView,
        Lens
    }

    private CameraModeToggle lens_toggle;
    private CameraModeToggle oc_toggle;
    private CameraModeToggle follow_toggle;
    private CameraModeToggle firstView_toggle;
    private CameraModeToggle selfie_toggle;

    [SerializeField] private Button lensUpBtn;
    [SerializeField] private Button lensDownBtn;
    [SerializeField] private Button lensLeftBtn;
    [SerializeField] private Button lensRightBtn; 

    private bool _isLensMode;

    private const string PREF_KEY_VIEW_MODE = "CameraMode_LastViewMode";

    private bool _isInternalToggleChange;
    private bool _isSelfieMode = false;
    private ViewModeType _lastViewModeBeforeSelfie = ViewModeType.Follow;
    private bool _pendingRestoreLastViewAfterSelfie;
    private readonly ExitSelfieEvent _exitSelfieEvent = new ExitSelfieEvent();
    private string _selfieId;

    protected override void OnInit(){
        lens_toggle = GetComponentByName<CameraModeToggle>("ToggleTargetLens");
        oc_toggle = GetComponentByName<CameraModeToggle>("ToggleOc");
        follow_toggle = GetComponentByName<CameraModeToggle>("ToggleFollow");
        firstView_toggle = GetComponentByName<CameraModeToggle>("ToggleFirstView");
        selfie_toggle = GetComponentByName<CameraModeToggle>("ToggleSelfie");

        lens_toggle.Init();
        oc_toggle.Init();
        follow_toggle.Init();
        firstView_toggle.Init();
        selfie_toggle.Init();

        lens_toggle.onValueChanged.AddListener(OnLensChange);
        follow_toggle.onValueChanged.AddListener(OnFollowChange);
        firstView_toggle.onValueChanged.AddListener(OnFirstViewChange);
        selfie_toggle.onValueChanged.AddListener(OnSelfieChange);
        oc_toggle.onValueChanged.AddListener(OnOcChange);
        MessageHelper.AddListener<bool>(MessageName.UICameraModeSelfieRequest, OnSelfieRequest);
        MessageHelper.AddListener<string>(MessageName.UICameraModeSelfieRequest, OnSelfieRequestWithId);
        _exitSelfieEvent.exitSelfie = OnExitSelfieByState;

        // Lens 上下按钮（如果 prefab 没拖引用，则尝试按名称查找）
        lensUpBtn ??= GetComponentByName<Button>("LensUpBtn");
        lensUpBtn ??= GetComponentByName<Button>("Btn_LensUp");
        lensUpBtn ??= GetComponentByName<Button>("BtnLensUp");

        lensDownBtn ??= GetComponentByName<Button>("LensDownBtn");
        lensDownBtn ??= GetComponentByName<Button>("Btn_LensDown");
        lensDownBtn ??= GetComponentByName<Button>("BtnLensDown");

        if (lensUpBtn != null)
        {
            lensUpBtn.onClick.RemoveAllListeners();
            var upHold = GetOrAddHoldPress(lensUpBtn.gameObject);
            upHold.Setup(+1f);
        }
        if (lensDownBtn != null)
        {
            lensDownBtn.onClick.RemoveAllListeners();
            var downHold = GetOrAddHoldPress(lensDownBtn.gameObject);
            downHold.Setup(-1f);
        }

        lensLeftBtn ??= GetComponentByName<Button>("LensLeftBtn");
        lensLeftBtn ??= GetComponentByName<Button>("Btn_LensLeft");
        lensLeftBtn ??= GetComponentByName<Button>("BtnLensLeft");

        lensRightBtn ??= GetComponentByName<Button>("LensRightBtn");
        lensRightBtn ??= GetComponentByName<Button>("Btn_LensRight");
        lensRightBtn ??= GetComponentByName<Button>("BtnLensRight");

        if (lensLeftBtn != null)
        {
            lensLeftBtn.onClick.RemoveAllListeners();
            var leftHold = GetOrAddHoldPress(lensLeftBtn.gameObject);
            leftHold.Setup(-1f, horizontal: true);
        }
        if (lensRightBtn != null)
        {
            lensRightBtn.onClick.RemoveAllListeners();
            var rightHold = GetOrAddHoldPress(lensRightBtn.gameObject);
            rightHold.Setup(+1f, horizontal: true);
        }
        SetLensButtonsVisible(false);


        // 默认：恢复上次保存的视角模式
        SetToggleBySavedViewMode();
        gameObject.SetActive(false); //默认隐藏
    }

    private void OnLensChange(bool isOn){
        if (_isInternalToggleChange) return;

        if (isOn)
        {
            _lastViewModeBeforeSelfie = ViewModeType.Lens;
            SaveViewMode(ViewModeType.Lens);
            // 进入 Lens：先回到 OC 的镜头角度/位置，再切成”摇杆平移 + 按钮升降”
            ForceExitSelfieMode(skipExitAnimation: true);

            CameraModeConotroller.Inst.ExitFirstViewMode();
            CameraModeConotroller.Inst.EnterLensMode();
            MessageHelper.Broadcast(MessageName.SelfieMode, false);
            MessageHelper.Broadcast(MessageName.LensMode, true);
            SetLensButtonsVisible(true);
            _isLensMode = true;
        }
        else
        {
            MessageHelper.Broadcast(MessageName.LensMode, false);
            CameraModeConotroller.Inst.ExitLensMode();
            SetLensButtonsVisible(false);
            // 退出 Lens 回到 OC 的默认相机态（保证输入句柄/虚拟相机正确）
            CameraModeConotroller.Inst.EnsureCameraMode(forceReset: true);
            _isLensMode = false;
        }
    }

    private void OnFollowChange(bool isOn){
        if (!isOn || _isInternalToggleChange) return;
        _lastViewModeBeforeSelfie = ViewModeType.Follow;
        SaveViewMode(ViewModeType.Follow);
        ForceExitSelfieMode(skipExitAnimation: true);
        
        CameraModeConotroller.Inst.ExitLensMode();
        SetLensButtonsVisible(false);
        _isLensMode = false;

        CameraModeConotroller.Inst.EnterFollowMode();
    }

    private void OnFirstViewChange(bool isOn){
        if (!isOn || _isInternalToggleChange) return;

        _lastViewModeBeforeSelfie = ViewModeType.FirstView;
        SaveViewMode(ViewModeType.FirstView);
        // 从自拍切第一人称必须即时退出自拍状态（不播退出动画），
        // 否则自拍状态异步收尾会在下一帧改写 KCC/相机，导致摇杆旋转异常。
        ForceExitSelfieMode(skipExitAnimation: true);

        CameraModeConotroller.Inst.ExitLensMode();
        SetLensButtonsVisible(false);
        _isLensMode = false;

        // 第一人称：不进入 PlayerState.CameraMode（自拍才需要）
        _isSelfieMode = false;
        MessageHelper.Broadcast(MessageName.SelfieMode, false);
        CameraModeConotroller.Inst.EnterFirstViewMode();
    }
    private void OnSelfieChange(bool isOn){
        if (!isOn || _isInternalToggleChange) return;
        var avatarCtrl = AvatarController.Inst;
        var statePlayer = avatarCtrl != null ? avatarCtrl.SelfStateController : null;
        var selfMotor = avatarCtrl != null ? avatarCtrl.SelfController?.Motor : null;
        if (statePlayer == null || selfMotor == null)
        {
            RejectSelfieToggleAndRestoreLastView(null);
            return;
        }

        if(statePlayer.ContainsCurrentState(PlayerState.LinkEmote)
        || statePlayer.ContainsCurrentState(PlayerState.LinkEmoteStart)){
            RejectSelfieToggleAndRestoreLastView("双人牵手状态下不能切自拍");
            return;
        }

        if (!CanEnterSelfieFromCurrentContext())
        {
            RejectSelfieToggleAndRestoreLastView(null);
            return;
        }

        if (selfMotor.IsDriveVehicle)
        {
            RejectSelfieToggleAndRestoreLastView("驾驶载具中不允许自拍");
            return;
        }

        _isSelfieMode = isOn;
        // 进入自拍视角
        if (!statePlayer.CanEnterState(PlayerState.CameraMode))
        {
            if(statePlayer.ContainsCurrentState(PlayerState.CameraMode)){
                MessageHelper.Broadcast(MessageName.SelfieChangePose, _selfieId);
                CameraModeConotroller.Inst.EnterSelfieMode(_selfieId);
            }

            return;
        }
        AkSoundManager.Inst.StopSound("Stop_Emote_1P", gameObject);
        statePlayer.EnterState(PlayerState.CameraMode, _exitSelfieEvent, _selfieId);
        MessageHelper.Broadcast(MessageName.SelfieMode, true);
        // 防止从第一人称切到自拍时残留跟随点
        CameraModeConotroller.Inst.ExitFirstViewMode();
        CameraModeConotroller.Inst.ExitLensMode();
        SetLensButtonsVisible(false);
        _isLensMode = false;
        CameraModeConotroller.Inst.EnterSelfieMode(_selfieId);
    }

    private void OnSelfieRequest(bool isSelfie)
    {
        if (isSelfie)
        {
            _selfieId = null;
            _pendingRestoreLastViewAfterSelfie = false;
            var statePlayer = AvatarController.Inst != null ? AvatarController.Inst.SelfStateController : null;
            bool isStateSelfie = statePlayer != null && statePlayer.ContainsCurrentState(PlayerState.CameraMode);
            if (!_isSelfieMode && !isStateSelfie)
            {
                // 必须在 selfie toggle 置为 true 前记录，否则 ToggleGroup 会先把其它视角关掉，导致丢失“进入前视角”。
                _lastViewModeBeforeSelfie = ResolveCurrentViewMode();
            }

            if (!CanEnterSelfieFromCurrentContext())
            {
                _isInternalToggleChange = true;
                if (oc_toggle != null) oc_toggle.isOn = true;
                _isInternalToggleChange = false;
                return;
            }
            // 容错：若 UI 残留为 On（但实际已不在自拍），重复赋值不会触发回调，这里主动补一次进入流程。
            if (selfie_toggle != null && selfie_toggle.isOn)
            {
                OnSelfieChange(true);
                return;
            }
            if (selfie_toggle != null) selfie_toggle.isOn = true;
        }
        else
        {
            ExitSelfieAndRestoreLastView(skipExitAnimation: true);
        }
    }

    private void OnSelfieRequestWithId(string selfieId)
    {
        _selfieId = selfieId;
        _pendingRestoreLastViewAfterSelfie = false;
        var statePlayer = AvatarController.Inst != null ? AvatarController.Inst.SelfStateController : null;
        bool isStateSelfie = statePlayer != null && statePlayer.ContainsCurrentState(PlayerState.CameraMode);
        if (!_isSelfieMode && !isStateSelfie)
        {
            // 必须在 selfie toggle 置为 true 前记录，否则 ToggleGroup 会先把其它视角关掉，导致丢失“进入前视角”。
            _lastViewModeBeforeSelfie = ResolveCurrentViewMode();
        }
        if (!CanEnterSelfieFromCurrentContext())
        {
            _isInternalToggleChange = true;
            if (oc_toggle != null) oc_toggle.isOn = true;
            _isInternalToggleChange = false;
            return;
        }
        // 容错：若 UI 残留为 On（但实际已不在自拍），重复赋值不会触发回调，这里主动补一次进入流程。
        if (selfie_toggle != null && selfie_toggle.isOn)
        {
            OnSelfieChange(true);
            return;
        }
        if (selfie_toggle != null) selfie_toggle.isOn = true;
    }

    public void HandleSelfieRequestFromPanel(bool isSelfie)
    {
        OnSelfieRequest(isSelfie);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<bool>(MessageName.UICameraModeSelfieRequest, OnSelfieRequest);
        MessageHelper.RemoveListener<string>(MessageName.UICameraModeSelfieRequest, OnSelfieRequestWithId);
        _exitSelfieEvent.exitSelfie = null;
    }

    private void SaveViewMode(ViewModeType mode)
    {
        PlayerPrefs.SetInt(PREF_KEY_VIEW_MODE, (int)mode);
        PlayerPrefs.Save();
    }

    private ViewModeType LoadSavedViewMode()
    {
        if (!PlayerPrefs.HasKey(PREF_KEY_VIEW_MODE))
            return ViewModeType.Oc;
        var val = PlayerPrefs.GetInt(PREF_KEY_VIEW_MODE, (int)ViewModeType.Oc);
        if (val < 0 || val > 3) return ViewModeType.Oc;
        return (ViewModeType)val;
    }

    /// <summary>
    /// 供 CameraModePanel.OnShow 调用：在 InitCameraMode 之后立即将相机切到上次保存的视角，
    /// 不操作 toggle UI（ViewMenu 此时可能尚未 Show）。
    /// </summary>
    public void ApplySavedCameraMode()
    {
        var saved = LoadSavedViewMode();
        switch (saved)
        {
            case ViewModeType.Follow:
                CameraModeConotroller.Inst.EnterFollowMode();
                break;
            case ViewModeType.FirstView:
                CameraModeConotroller.Inst.EnterFirstViewMode();
                break;
            case ViewModeType.Lens:
                CameraModeConotroller.Inst.EnterLensMode();
                SetLensButtonsVisible(true);
                _isLensMode = true;
                break;
            default:
                // OC：InitCameraMode 已建立 OC 基态，无需额外操作
                break;
        }
    }

    private void SetToggleBySavedViewMode()
    {
        var saved = LoadSavedViewMode();
        switch (saved)
        {
            case ViewModeType.Follow:
                if (follow_toggle != null) follow_toggle.isOn = true;
                break;
            case ViewModeType.FirstView:
                if (firstView_toggle != null) firstView_toggle.isOn = true;
                break;
            case ViewModeType.Lens:
                if (lens_toggle != null) lens_toggle.isOn = true;
                break;
            default:
                if (oc_toggle != null) oc_toggle.isOn = true;
                break;
        }
    }

    private void ApplySavedViewMode()
    {
        var saved = LoadSavedViewMode();
        // 先尝试通过设置 toggle 触发对应回调来驱动相机切换。
        // 如果 toggle 已处于 On 状态（不会触发 onValueChanged），则主动补一次相机操作。
        switch (saved)
        {
            case ViewModeType.Follow:
                if (follow_toggle != null && !follow_toggle.isOn)
                    follow_toggle.isOn = true;
                else
                {
                    CameraModeConotroller.Inst.ExitLensMode();
                    SetLensButtonsVisible(false);
                    _isLensMode = false;
                    CameraModeConotroller.Inst.EnterFollowMode();
                }
                break;
            case ViewModeType.FirstView:
                if (firstView_toggle != null && !firstView_toggle.isOn)
                    firstView_toggle.isOn = true;
                else
                {
                    CameraModeConotroller.Inst.ExitLensMode();
                    SetLensButtonsVisible(false);
                    _isLensMode = false;
                    CameraModeConotroller.Inst.EnterFirstViewMode();
                }
                break;
            case ViewModeType.Lens:
                if (lens_toggle != null && !lens_toggle.isOn)
                    lens_toggle.isOn = true;
                else
                {
                    CameraModeConotroller.Inst.ExitFirstViewMode();
                    CameraModeConotroller.Inst.EnterLensMode();
                    SetLensButtonsVisible(true);
                    _isLensMode = true;
                }
                break;
            default:
                if (oc_toggle != null && !oc_toggle.isOn)
                    oc_toggle.isOn = true;
                else
                    ApplyOcViewMode();
                break;
        }
    }

    private void OnOcChange(bool isOn){
        if (!isOn || _isInternalToggleChange) return;
        _lastViewModeBeforeSelfie = ViewModeType.Oc;
        SaveViewMode(ViewModeType.Oc);
        ApplyOcViewMode();
    }

    private void ApplyOcDefaultViewMode()
    {
        // 基础状态改为 Follow：
        // 从自拍切回默认时直接进入 Follow，不播放自拍退出动作。
        ForceExitSelfieMode(skipExitAnimation: true);
        CameraModeConotroller.Inst.ExitFirstViewMode();
        CameraModeConotroller.Inst.ExitLensMode();
        SetLensButtonsVisible(false);
        _isLensMode = false;
        CameraModeConotroller.Inst.EnterFollowMode();
        MessageHelper.Broadcast(MessageName.SelfieMode, false);
    }

    private void ApplyOcViewMode()
    {
        // 用户手动切到 OC：恢复 OC 自由镜头（保留双指缩放/平移能力）。
        ForceExitSelfieMode(skipExitAnimation: true);
        CameraModeConotroller.Inst.ExitFirstViewMode();
        CameraModeConotroller.Inst.ExitLensMode();
        SetLensButtonsVisible(false);
        _isLensMode = false;
        CameraModeConotroller.Inst.EnsureCameraMode(forceReset: true);
        MessageHelper.Broadcast(MessageName.SelfieMode, false);
    }

    private void OnExitSelfieByState()
    {
        // 只有当前仍处于自拍会话时才处理，避免“主动切回 OC 后”的异步回调再次扰动相机。
        if (!_isSelfieMode) return;

        // 动作等状态打断自拍时：固定进入 OC，并保留打断瞬间的自拍镜头位姿。
        _isSelfieMode = false;
        _pendingRestoreLastViewAfterSelfie = false;
        _lastViewModeBeforeSelfie = ViewModeType.Oc;

        CameraModeConotroller.Inst.ExitSelfieModeToOcKeepCurrentPose();
        CameraModeConotroller.Inst.ExitLensMode();
        SetLensButtonsVisible(false);
        _isLensMode = false;

        _isInternalToggleChange = true;
        if (selfie_toggle != null) selfie_toggle.isOn = false;
        if (oc_toggle != null) oc_toggle.isOn = true;
        _isInternalToggleChange = false;
        MessageHelper.Broadcast(MessageName.SelfieMode, false);
    }

    private void ForceExitSelfieMode(bool skipExitAnimation = false)
    {
        var statePlayer = AvatarController.Inst != null ? AvatarController.Inst.SelfStateController : null;
        bool isStateSelfie = statePlayer != null && statePlayer.ContainsCurrentState(PlayerState.CameraMode);
        if (!_isSelfieMode && !isStateSelfie) return;

        // 双保险：主动还原相机节点父级 + 退出状态机
        CameraModeConotroller.Inst.ExitSelfieMode();
        statePlayer?.ExitState(PlayerState.CameraMode, !skipExitAnimation);
        _isSelfieMode = false;
        MessageHelper.Broadcast(MessageName.SelfieMode, false);
    }

    private void ExitSelfieAndRestoreLastView(bool skipExitAnimation)
    {
        // 先强制退出自拍状态，随后恢复到进入自拍前的视角 toggle。
        ForceExitSelfieMode(skipExitAnimation);
        if (!gameObject.activeInHierarchy)
        {
            // 子菜单隐藏时也要立即恢复镜头状态，不能只等 UI 恢复。
            ApplyViewModeAfterSelfieByRuntime(_lastViewModeBeforeSelfie);
            ApplyViewModeToggleAfterSelfie(_lastViewModeBeforeSelfie);
            _pendingRestoreLastViewAfterSelfie = true;
            return;
        }
        RestoreLastViewModeAfterSelfie();
    }

    private void RejectSelfieToggleAndRestoreLastView(string tip)
    {
        if (!string.IsNullOrEmpty(tip))
        {
            TipPanel.ShowToast(tip);
        }

        _isInternalToggleChange = true;
        if (selfie_toggle != null) selfie_toggle.isOn = false;
        _isInternalToggleChange = false;

        ApplyViewModeToggleAfterSelfie(_lastViewModeBeforeSelfie);
        ApplyViewModeAfterSelfieByRuntime(_lastViewModeBeforeSelfie);
    }

    private static bool CanEnterSelfieFromCurrentContext()
    {
        if (CameraModeOcMenu.IsOcChangingActive)
        {
            TipPanel.ShowToast("请关闭oc更换页面再进入自拍");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 当通过主菜单直接打开 OC 子菜单时调用，将 View 的 toggle 切到 OC 并退出自拍（无退出动画），
    /// 保证 SelfieBtn 与 View 状态一致，后续可正常再进自拍。
    /// </summary>
    public void SyncToOcAndExitSelfie()
    {
        var statePlayer = AvatarController.Inst != null ? AvatarController.Inst.SelfStateController : null;
        bool isStateSelfie = statePlayer != null && statePlayer.ContainsCurrentState(PlayerState.CameraMode);
        if (!_isSelfieMode && !isStateSelfie) return;

        _isInternalToggleChange = true;
        if (selfie_toggle != null) selfie_toggle.isOn = false;
        if (oc_toggle != null) oc_toggle.isOn = true;
        ForceExitSelfieMode(skipExitAnimation: true);
        CameraModeConotroller.Inst.ExitFirstViewMode();
        CameraModeConotroller.Inst.ExitLensMode();
        SetLensButtonsVisible(false);
        _isLensMode = false;
        CameraModeConotroller.Inst.EnsureCameraMode(forceReset: true);
        MessageHelper.Broadcast(MessageName.SelfieMode, false);
        _isInternalToggleChange = false;
    }

    protected override void OnShow()
    {
        // 进入时同步一次 Toggle 状态（如果外部按钮进入过自拍，这里也要能对上）
        bool isSelfie = AvatarController.Inst != null
                        && AvatarController.Inst.SelfStateController != null
                        && AvatarController.Inst.SelfStateController.ContainsCurrentState(PlayerState.CameraMode);
        _isSelfieMode = isSelfie;
        _selfieId = null;

        _isInternalToggleChange = true;
        if (isSelfie)
        {
            if (selfie_toggle != null) selfie_toggle.isOn = true;
        }
        else
        {
            if (selfie_toggle != null) selfie_toggle.isOn = false;
            SetToggleBySavedViewMode();
        }
        _isInternalToggleChange = false;

        if (!isSelfie && _pendingRestoreLastViewAfterSelfie)
        {
            _pendingRestoreLastViewAfterSelfie = false;
            RestoreLastViewModeAfterSelfie();
        }
        else if (!isSelfie)
        {
            ApplySavedViewMode();
        }

        // 关键：该菜单可能被 Hide/Show 多次（例如切到 OC/Action 等再切回来），
        // 此时 CameraMode 的输入句柄/虚拟相机可能已被其它模块接管，需要确保恢复。
        // ApplySavedViewMode 已经为当前保存的视角模式做了完整的相机切换，
        // 这里仅在 OC 基态下兜底（Follow/FirstView/Lens 由各自 Enter 方法管理）。
        if (!isSelfie && LoadSavedViewMode() == ViewModeType.Oc
            && !CameraModeConotroller.Inst.IsFollowModeActive)
        {
            CameraModeConotroller.Inst.EnsureCameraMode();
        }

        // Lens 按钮显示与状态对齐（切回来时 Toggle 可能保持 On）
        var isLens = lens_toggle != null && lens_toggle.isOn && !isSelfie;
        SetLensButtonsVisible(isLens);
        if (isLens)
        {
            CameraModeConotroller.Inst.EnterLensMode();
            _isLensMode = true;
        }
        else
        {
            _isLensMode = false;
        }
    }

    private ViewModeType ResolveCurrentViewMode()
    {
        if (lens_toggle != null && lens_toggle.isOn) return ViewModeType.Lens;
        if (firstView_toggle != null && firstView_toggle.isOn) return ViewModeType.FirstView;
        if (follow_toggle != null && follow_toggle.isOn) return ViewModeType.Follow;
        return ViewModeType.Oc;
    }

    private void ApplyViewModeAfterSelfieByRuntime(ViewModeType mode)
    {
        switch (mode)
        {
            case ViewModeType.Lens:
                CameraModeConotroller.Inst.EnterLensMode();
                SetLensButtonsVisible(true);
                _isLensMode = true;
                return;
            case ViewModeType.FirstView:
                CameraModeConotroller.Inst.EnterFirstViewMode();
                SetLensButtonsVisible(false);
                _isLensMode = false;
                return;
            case ViewModeType.Follow:
                CameraModeConotroller.Inst.ExitLensMode();
                SetLensButtonsVisible(false);
                _isLensMode = false;
                CameraModeConotroller.Inst.EnterFollowMode();
                return;
            default:
                CameraModeConotroller.Inst.ExitFirstViewMode();
                CameraModeConotroller.Inst.ExitLensMode();
                SetLensButtonsVisible(false);
                _isLensMode = false;
                CameraModeConotroller.Inst.EnsureCameraMode(forceReset: true);
                return;
        }
    }

    private void RestoreLastViewModeAfterSelfie()
    {
        // 注意：仅恢复 toggle 会导致“UI 看起来切回了上一个视角，但运行时镜头未切回”。
        // 在 ViewMenu 处于打开态时也必须同步执行运行时切换。
        ApplyViewModeAfterSelfieByRuntime(_lastViewModeBeforeSelfie);
        ApplyViewModeToggleAfterSelfie(_lastViewModeBeforeSelfie);
    }

    private void ApplyViewModeToggleAfterSelfie(ViewModeType mode)
    {
        _isInternalToggleChange = true;
        if (selfie_toggle != null) selfie_toggle.isOn = false;

        switch (mode)
        {
            case ViewModeType.Lens:
                if (lens_toggle != null) lens_toggle.isOn = true;
                break;
            case ViewModeType.FirstView:
                if (firstView_toggle != null) firstView_toggle.isOn = true;
                break;
            case ViewModeType.Follow:
                if (follow_toggle != null) follow_toggle.isOn = true;
                break;
            default:
                if (oc_toggle != null) oc_toggle.isOn = true;
                break;
        }
        _isInternalToggleChange = false;
    }

    protected override void OnHide()
    {
        
    }

    private void SetLensButtonsVisible(bool visible)
    {
        if (lensUpBtn != null) lensUpBtn.gameObject.SetActive(visible);
        if (lensDownBtn != null) lensDownBtn.gameObject.SetActive(visible);
        if (lensLeftBtn != null) lensLeftBtn.gameObject.SetActive(visible);
        if (lensRightBtn != null) lensRightBtn.gameObject.SetActive(visible);
    }

    private static HoldPress GetOrAddHoldPress(GameObject go)
    {
        var hp = go.GetComponent<HoldPress>();
        if (hp != null) return hp;
        return go.AddComponent<HoldPress>();
    }

    private sealed class HoldPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool IsHeld { get; private set; }
        private float _dir;
        private bool _isHorizontal;

        public void Setup(float dir, bool horizontal = false)
        {
            _dir = Mathf.Sign(dir);
            if (Mathf.Abs(_dir) < 0.01f) _dir = 1f;
            _isHorizontal = horizontal;
        }

        private void Update()
        {
            if (!IsHeld) return;
            var ctrl = CameraModeConotroller.Inst;
            if (ctrl == null) return;
            var delta = _dir * CameraModeConotroller.LENS_UP_DOWN_SPEED * Time.deltaTime;
            if (_isHorizontal)
                ctrl.LensMoveHorizontal(delta);
            else
                ctrl.LensMoveVertical(delta);
        }

        public void OnPointerDown(PointerEventData eventData) => IsHeld = true;
        public void OnPointerUp(PointerEventData eventData) => IsHeld = false;
        public void OnPointerExit(PointerEventData eventData) => IsHeld = false;
    }
}
