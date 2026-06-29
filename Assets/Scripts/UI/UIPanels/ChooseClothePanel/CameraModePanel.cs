using System;
using Game.Audio;
using Game.Avatar;
using Game.Event;
using GameSync.Manager;
using Game.MapSetting;
using Message;
using UI.Base;
using UI.UIPanels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.KinematicCharacter;
using Game.Vehicle.PGCVehicle;

/// <summary>
/// Author:
/// Desc:
/// Date:24-07-11 17:10:25
/// </summary>
public class CameraModePanel : BasePanel<CameraModePanel>
{
    public Button CloseCameraModeBtn;
    public Button TakePhotoBtn;
    public Button SelfieBtn;


    public GameObject selfieBtnPanel;
    private ExitSelfieEvent slefieEvent = new ExitSelfieEvent();
    [SerializeField] private Image selfieBtnImage;
    [SerializeField] private float selfieBtnAnimDuration = 0.2f;
    [SerializeField] private UICameraModeGlobalClosePassthrough passThrough;
    private readonly Color selfieBtnNormalColor = Color.white;
    private readonly Color selfieBtnActiveColor = new Color(1f, 0.85f, 0f, 1f); // #FFD900

    private CameraModeMainMenu mainMenu;
    private CameraModeSettingMenu settingMenu;
    private CameraModeViewMenu viewMenu;
    private CameraModeSpanner spanner;
    private CameraModeRecordAndShotCtrl optSwticher;
    private Coroutine selfieBtnAnimCo;
    private bool _isSelfieMode;
    private float _selfieBtnBaseScaleX = 1f;

    public override void OnCreate()
    {
        base.OnCreate();
        CloseCameraModeBtn.onClick.AddListener(OnCloseCameraModeBtnClick);
        TakePhotoBtn.onClick.AddListener(OnTakePhotoBtnClick);
        if (SelfieBtn != null)
        {
            SelfieBtn.onClick.AddListener(OnSelfieBtnClick);
            if (selfieBtnImage == null) selfieBtnImage = SelfieBtn.GetComponent<Image>();
            _selfieBtnBaseScaleX = Mathf.Abs(SelfieBtn.transform.localScale.x);
            if (_selfieBtnBaseScaleX <= 0.0001f) _selfieBtnBaseScaleX = 1f;
        }
        // EnterSelfieBtn.onClick.AddListener(OnEnterSelfieClick);
        // ExitSelfieBtn.onClick.AddListener(OnExitSelfieClick);
        MessageHelper.AddListener<bool>(MessageName.SelfieMode, RefreshUI);
        MessageHelper.AddListener<bool>(MessageName.LensMode, OnLensModeChange);
        MessageHelper.AddListener(MessageName.UICameraModeReset, OnUICameraModeReset);
        InitSelfieUI();
        mainMenu = transform.GetComponentInChildren<CameraModeMainMenu>(true);
        settingMenu = transform.GetComponentInChildren<CameraModeSettingMenu>(true);
        viewMenu = transform.GetComponentInChildren<CameraModeViewMenu>(true);
        mainMenu.Init();
        settingMenu.Init();
        spanner = transform.GetComponentInChildren<CameraModeSpanner>(true);
        optSwticher = transform.GetComponentInChildren<CameraModeRecordAndShotCtrl>(true);
        optSwticher.Init();
        ApplySelfieBtnVisual(false, instant: true);
        //把passThrough移动到这个Panel的父级的最前面
        if(transform.parent != null && passThrough != null){
            passThrough.transform.SetParent(transform.parent);
            passThrough.transform.SetAsFirstSibling();
        }
    }

    public override void OnWindowBeFocused()
    {
        base.OnWindowBeFocused();
        // 从 FittingRoomPanel 等子面板返回时，重新应用用户保存的名称显示设置
        // （UIRelease 在 CameraMode 被遮盖时会临时恢复默认状态，此处纠正回来）
        CameraModeSettingUtils.Inst.RestoreNameSetting();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        // UIOrderHelper 会在 AddPanel 时按 Config.Order 自动调整 siblingIndex；
        // 这里再强制把自己放到 GuestWindow 子物体的“最底层”(渲染在后面)，避免挡住/拦截其它 UI 的点击。
        //TryMoveToBottomInGuestWindow();
        CameraModeConotroller.Inst.InitCameraMode();
        // InitCameraMode 建立 OC 基态后，立即切到用户上次保存的视角模式。
        viewMenu.ApplySavedCameraMode();
        ClientManager.Inst.IsForceSync = true;
        MessageHelper.Broadcast(MessageName.UICameraMode,true);
        mainMenu.Show();
        CameraModeSettingUtils.Inst.Init();
    }

    /// <summary>
    /// 让该 Panel 在 GuestWindow 下处于最低渲染层级（最先 sibling），避免遮挡其它 UI。
    /// 只在当前确实属于 GuestWindow 时处理，避免跨 Window 乱改 parent 导致 Close/栈逻辑异常。
    /// </summary>
    private void TryMoveToBottomInGuestWindow()
    {
        try
        {
            // 优先用 BelongWindow 判断（更可靠）
            if (BelongWindow != null && BelongWindow.config != null &&
                BelongWindow.config.WindowId == (int)WindowId.GuestWindow)
            {
                transform.SetAsFirstSibling();
                return;
            }

            // 兜底：如果父节点名字就是 GuestWindow，也做同样处理
            var parent = transform.parent;
            if (parent != null && parent.name == WindowId.GuestWindow.ToString())
            {
                transform.SetAsFirstSibling();
            }
        }
        catch (Exception e)
        {
            LoggerUtils.LogError($"TryMoveToBottomInGuestWindow failed: {e}");
        }
    }

    public override void OnHidden()
    {
        try
        {
            // 退出相机状态：清理滤镜/边框/参数等效果，回到初始默认
            ResetAllCameraEffectsToDefault(isRealtimeReset: false);

            ClientManager.Inst.IsForceSync = false;
            slefieEvent.exitSelfie = null;
            CameraModeConotroller.Inst.ExitCameraMode();
            MessageHelper.Broadcast<bool>(MessageName.UICameraMode, false);
            if (passThrough != null)
            {
                Destroy(passThrough.gameObject);
            }
            CameraModeSettingUtils.Inst.UIRelease();
            base.OnHidden();
        }catch(Exception e)
        {
            Debug.LogError("CameraModePanel OnHidden e=" + e.StackTrace);
            base.OnHidden();
        }
    
    }

    private void ResetAllCameraEffectsToDefault(bool isRealtimeReset)
    {
        // 1) 滤镜（后处理 Volume）还原
        try
        {
            PostProcessManager.Inst.RestoreDefaultVolume();
        }
        catch (Exception e)
        {
            LoggerUtils.LogError($"Reset filter volume failed: {e}");
        }

        // 2) 旋钮参数还原（目前 ApplyXXX 为空占位，但会把内部值归零/默认）
        try
        {
            var camData = transform.GetComponentInChildren<CameraModeCamDataController>(true);
            // 仅重置内部参数值与 UI，并还原镜头缓存；Volume 已在上面统一还原。
            camData?.ResetAllToDefaults(applyNow: false);
        }
        catch (Exception e)
        {
            LoggerUtils.LogError($"Reset camera param ctrls failed: {e}");
        }

        // 3) Frame/Filter 菜单内部选中态还原（保证下次打开 UI 也是默认）
        try
        {
            var filterMenu = transform.GetComponentInChildren<CameraModeFilterMenu>(true);
            filterMenu?.ResetToDefault(applyNow: false); // Volume 已在上面统一还原
        }
        catch (Exception e)
        {
            LoggerUtils.LogError($"Reset filter menu state failed: {e}");
        }

        try
        {
            var frameMenu = transform.GetComponentInChildren<CameraModeFrameMenu>(true);
            frameMenu?.ResetToDefault(applyNow: true);
        }
        catch (Exception e)
        {
            LoggerUtils.LogError($"Reset frame menu state failed: {e}");
        }

        if (isRealtimeReset)
        {
            MessageHelper.Broadcast(MessageName.UICameraModeCloseCurrent);
        }
    }

    private void OnUICameraModeReset()
    {
        ResetAllCameraEffectsToDefault(isRealtimeReset: true);
    }

    private void InitSelfieUI()
    {
    //     ExitSelfieBtn.gameObject.SetActive(false);
    //     EnterSelfieBtn.gameObject.SetActive(!UIShowAbilityManager.Inst.GetBanBility(UIAbility.EnterSelfieBtn));
    }

    public void OnCloseCameraModeBtnClick()
    {
        CloseSelf();
        OnLensModeChange(false);
    }

    private void OnTakePhotoBtnClick()
    {
        UIManager.Inst.OpenPanel<ShotBlackPanel>(PanelId.ShotBlackPanel);
        EventCenterDataManager.Inst.ReportTask(PostEventId.TakePhotoCheckIn);
    }

    private void OnSelfieBtnClick()
    {
        if (CameraModeOcMenu.IsOcChangingActive)
        {
            TipPanel.ShowToast("请关闭设子更换页面再进入自拍");
            return;
        }

        if (AvatarController.Inst.SelfController.Motor.IsDriveVehicle)
        {
            TipPanel.ShowToast("驾驶载具中不允许自拍");
            return;
        }

        //双人牵手状态下不能切第一人称
        if(AvatarController.Inst.SelfStateController.ContainsCurrentState(PlayerState.LinkEmote)
        || AvatarController.Inst.SelfStateController.ContainsCurrentState(PlayerState.LinkEmoteStart)){
            TipPanel.ShowToast("双人牵手状态下不能切第一人称");
            return;
        }

        var targetSelfieMode = !_isSelfieMode;
        if (viewMenu != null)
        {
            // 直接调用 ViewMenu，避免其 inactive 时收不到消息导致退出自拍失败。
            viewMenu.HandleSelfieRequestFromPanel(targetSelfieMode);
        }
        else
        {
            MessageHelper.Broadcast(MessageName.UICameraModeSelfieRequest, targetSelfieMode);
        }
    }

    private void OnEnterSelfieClick()
    {
        if (AvatarController.Inst.SelfController.Motor.IsDriveVehicle)
        {
            TipPanel.ShowToast("驾驶载具中不允许自拍");
            return;
        }
        slefieEvent.exitSelfie = AutoExitSelfie;
        var statePlayer = AvatarController.Inst.SelfStateController;
        if (!statePlayer.CanEnterState(PlayerState.CameraMode))
        {
            return;
        }
        AkSoundManager.Inst.StopSound("Stop_Emote_1P", gameObject);
        statePlayer.EnterState(PlayerState.CameraMode,slefieEvent);
        RefreshUI(true);
        CameraModeConotroller.Inst.EnterSelfieMode();
    }

    private void AutoExitSelfie()
    {
        RefreshUI(false);
        CameraModeConotroller.Inst.ExitSelfieMode();
    }

    private void OnExitSelfieClick()
    {
        if(AvatarController.Inst.SelfController.Motor.IsDriveVehicle)
        {
            TipPanel.ShowToast("驾驶载具中不允许自拍");
            return;
        }
        AvatarController.Inst.SelfStateController.ExitState(PlayerState.CameraMode);
    }

    public void OnEmoPanelShow(bool isActive)
    {
        TakePhotoBtn.gameObject.SetActive(isActive);
        selfieBtnPanel.SetActive(isActive);
    }

    public void RefreshUI(bool isSelfieMode)
    {
        // ExitSelfieBtn.gameObject.SetActive(isSelfieMode);
        // EnterSelfieBtn.gameObject.SetActive(!isSelfieMode);
        _isSelfieMode = isSelfieMode;
        ApplySelfieBtnVisual(isSelfieMode, instant: false);
    }

    private void ApplySelfieBtnVisual(bool isSelfieMode, bool instant)
    {
        if (SelfieBtn == null) return;

        var targetScaleSignX = isSelfieMode ? -1f : 1f;
        var targetColor = isSelfieMode ? selfieBtnActiveColor : selfieBtnNormalColor;
        if (instant)
        {
            var scale = SelfieBtn.transform.localScale;
            scale.x = _selfieBtnBaseScaleX * targetScaleSignX;
            SelfieBtn.transform.localScale = scale;
            if (selfieBtnImage != null) selfieBtnImage.color = targetColor;
            return;
        }

        if (selfieBtnAnimCo != null) StopCoroutine(selfieBtnAnimCo);
        selfieBtnAnimCo = StartCoroutine(CoAnimateSelfieBtn(targetScaleSignX, targetColor));
    }

    private System.Collections.IEnumerator CoAnimateSelfieBtn(float targetScaleSignX, Color targetColor)
    {
        var startScale = SelfieBtn.transform.localScale;
        var startX = startScale.x;
        var targetX = _selfieBtnBaseScaleX * targetScaleSignX;

        var duration = Mathf.Max(0.01f, selfieBtnAnimDuration);
        var elapsed = 0f;
        var switchedColor = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var x = Mathf.Lerp(startX, targetX, t);
            var scale = SelfieBtn.transform.localScale;
            scale.x = x;
            SelfieBtn.transform.localScale = scale;

            // 改为 scale 镜像翻转：中途切换高亮色，避免按钮 180 旋转后无法点击。
            if (selfieBtnImage != null)
            {
                if (targetScaleSignX < 0f)
                {
                    if (!switchedColor && t >= 0.5f)
                    {
                        selfieBtnImage.color = selfieBtnActiveColor;
                        switchedColor = true;
                    }
                }
                else
                {
                    selfieBtnImage.color = Color.white;
                }
            }

            yield return null;
        }

        var finalScale = SelfieBtn.transform.localScale;
        finalScale.x = targetX;
        SelfieBtn.transform.localScale = finalScale;
        if (selfieBtnImage != null) selfieBtnImage.color = targetColor;
        selfieBtnAnimCo = null;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (SelfieBtn != null) SelfieBtn.onClick.RemoveListener(OnSelfieBtnClick);
        MessageHelper.RemoveListener<bool>(MessageName.SelfieMode, RefreshUI);
        MessageHelper.RemoveListener(MessageName.UICameraModeReset, OnUICameraModeReset);
    }

    private void OnLensModeChange(bool isLensMode)
    {
        if(AvatarController.Inst.SelfController.Motor.IsDriveVehicle 
        || GameVehicleManager.Inst.IsDriver(AccountDataManager.Inst.UserInfo.uid)
        || GameVehicleManager.Inst.IsPassenger(AccountDataManager.Inst.UserInfo.uid)){
            return;
        }
        MobileJoystick.Inst.SetJumpVisible(!isLensMode);
    }
}