using Game.MapSetting;
using Game.Audio;
using Message;
using Cinemachine;
using UI.UIPanels;
using UnityEngine;

public class CameraModeCamDataController : MonoBehaviour
{
    
    [SerializeField] private CameraModeSpanner cameraModeSpanner; //这个是调数值的
    [SerializeField] private CameraModeSpanner cameraModeSelector; //这个是用来选择按钮的，是选择器

    [SerializeField] private CameraModeToggle bloomToggle;
    [SerializeField] private CameraModeToggle focusToggle;
    [SerializeField] private CameraModeToggle DoFToggle;
    [SerializeField] private CameraModeToggle contrastToggle;
    [SerializeField] private CameraModeToggle saturationToggle;
    [SerializeField] private CameraModeToggle exposureToggle;
    [SerializeField] private CameraModeToggle tiltToggle;
    [SerializeField] private CameraModeToggle FOVToggle;

    private CameraModeEffectCtrls _ctrls;
    private CameraModeEffectParam _currentParam = CameraModeEffectParam.Bloom;
    private bool _lensAdjustStarted;
    private CameraModeFilterMenu _filterMenu;
    
    // 摄像机 Lens 缓存：用于退出相机模式时还原（避免把 Tilt/FOV 带出 CameraMode）
    private bool _camLensCached;
    private bool _camLensDirty;
    private LensSettings _cachedLensForCameraMode;
    private LensSettings _cachedLensForPlay;

    // selector（用于选择按钮的旋钮）
    private CameraModeSelectorCtrl _selectorCtrl;
    private bool _ignoreNextSpannerValueEvent;

    // Spanner 音效：按参数范围离散成 40 段，跨段时播放一次
    private const int SpannerAudioStepCount = 24;
    private const string SpannerTurnAudioEvent = "Play_UI_Cameraturn";
    private ICameraModeCtrl _spannerAudioCtrl;
    private int _lastSpannerAudioStepIndex = -1;

    private bool _isCameraModeFeatureVersionSupported = false;
    private bool _suppressVersionCheckForProgrammaticToggle = false;
    
    /// <summary>
    /// 退出相机模式时调用：将所有参数还原到默认值，并触发对应 Apply 占位函数。
    /// </summary>
    public void ResetAllToDefaults(bool applyNow = false)
    {
        _ctrls ??= new CameraModeEffectCtrls();
        _lensAdjustStarted = false;

        // 重置所有 ctrl 到默认值
        _ctrls.Bloom.OnReset();
        _ctrls.Focus.OnReset();
        _ctrls.DoF.OnReset();
        _ctrls.Contrast.OnReset();
        _ctrls.Saturation.OnReset();
        _ctrls.Exposure.OnReset();
        _ctrls.Tilt.OnReset();
        _ctrls.FOV.OnReset();

        // 无论是否 applyNow，只要曾经改过 Tilt/FOV，都要把相机 Lens 还原
        //（applyNow=false 是退出相机模式的常用路径）
        if (_camLensDirty)
        {
            RestoreCachedCameraLens();
        }

        if (applyNow)
        {
            // 应用默认值（先占位/或接入 PostProcessManager）
            ApplyBloom(ResolveValueAsFloat(_ctrls.Bloom, _ctrls.Bloom.GetValue()));
            ApplyFocus(ResolveValueAsFloat(_ctrls.Focus, _ctrls.Focus.GetValue()));
            ApplyDoF(ResolveValueAsFloat(_ctrls.DoF, _ctrls.DoF.GetValue()));
            ApplyContrast(ResolveValueAsFloat(_ctrls.Contrast, _ctrls.Contrast.GetValue()));
            ApplySaturation(ResolveValueAsFloat(_ctrls.Saturation, _ctrls.Saturation.GetValue()));
            ApplyExposure(ResolveValueAsFloat(_ctrls.Exposure, _ctrls.Exposure.GetValue()));
            ApplyTilt(ResolveValueAsFloat(_ctrls.Tilt, _ctrls.Tilt.GetValue()));
            ApplyFov(ResolveValueAsFloat(_ctrls.FOV, _ctrls.FOV.GetValue()));
        }

        // UI 默认回到 Bloom（同时刷新旋钮显示）
        _currentParam = CameraModeEffectParam.Bloom;
        // 注意：如果 Bloom 本来就 isOn=true，直接赋值不会触发 Toggle 回调，
        // 需要手动 SelectParam 以刷新 Spanner 的 PointerRoot 显示。
        bool switchedByToggle = false;
        _suppressVersionCheckForProgrammaticToggle = true;
        try
        {
            if (bloomToggle != null && !bloomToggle.isOn)
            {
                bloomToggle.isOn = true;
                switchedByToggle = true;
            }
            if (!switchedByToggle)
            {
                SelectParam(CameraModeEffectParam.Bloom);
            }
        }
        finally
        {
            _suppressVersionCheckForProgrammaticToggle = false;
        }
    }

    void Start()
    {
        _ctrls = new CameraModeEffectCtrls();

        // selector 初始化：默认在最小值（第一个按钮），但初始隐藏
        if (cameraModeSelector != null)
        {
            _selectorCtrl = new CameraModeSelectorCtrl();
            cameraModeSelector.SetCtrl(_selectorCtrl); // 用作初始显示角度（不再用 value 来切换选项）
            // selector 允许拖动：只旋转转盘（连续角度/可循环），选项切换由点击按钮完成
            cameraModeSelector.SetRotationOnly(true);
            cameraModeSelector.SetInputEnabled(true);
            cameraModeSelector.gameObject.SetActive(false); // 初始隐藏
        }

        // 监听外部消息：关闭 selector 显示（直到触发 spanner 调参再恢复）
        MessageHelper.AddListener(MessageName.UICameraModeCloseLensControl, OnCloseLensControlMsg);
        // Lens Toggle 打开时，也需要强制打开 selector（显示默认位置/当前选项位置）
        MessageHelper.AddListener(MessageName.UICameraModeOpenLensControl, OnOpenLensControlMsg);
        // 与 SettingMenu 对齐：收到全局关闭时也要收起 selector
        MessageHelper.AddListener(MessageName.UICameraModeGlobalClose, OnGlobalCloseMsg);
        // 双指缩放等外部操作改变 FOV 后，同步 ctrl + Spanner 显示
        MessageHelper.AddListener<float>(MessageName.UICameraModeFovChanged, OnExternalFovChanged);

        // 初始化 Toggle 显示
        bloomToggle?.Init();
        focusToggle?.Init();
        DoFToggle?.Init();
        contrastToggle?.Init();
        saturationToggle?.Init();
        exposureToggle?.Init();
        tiltToggle?.Init();
        FOVToggle?.Init();

        // 绑定点击切换 -> 切换 spanner 的 ctrl
        BindToggle(bloomToggle, CameraModeEffectParam.Bloom);
        BindToggle(focusToggle, CameraModeEffectParam.Focus);
        BindToggle(DoFToggle, CameraModeEffectParam.DoF);
        BindToggle(contrastToggle, CameraModeEffectParam.Contrast);
        BindToggle(saturationToggle, CameraModeEffectParam.Saturation);
        BindToggle(exposureToggle, CameraModeEffectParam.Exposure);
        BindToggle(tiltToggle, CameraModeEffectParam.Tilt);
        BindToggle(FOVToggle, CameraModeEffectParam.FOV);

        // 监听旋钮数值变化：先留占位（实际功能后面补）
        if (cameraModeSpanner != null)
        {
            cameraModeSpanner.ValueChanged += OnSpannerValueChanged;
        }

        // 默认选中一个 Toggle（如果 prefab 上没预设）
        if (bloomToggle != null && !AnyToggleOn())
        {
            bloomToggle.isOn = true;
        }
        else
        {
            // 如果 prefab 已经有 isOn=true 的 Toggle，就按它来切一次 ctrl，保证数值显示同步
            SyncFromCurrentToggleState();
        }
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.UICameraModeCloseLensControl, OnCloseLensControlMsg);
        MessageHelper.RemoveListener(MessageName.UICameraModeOpenLensControl, OnOpenLensControlMsg);
        MessageHelper.RemoveListener(MessageName.UICameraModeGlobalClose, OnGlobalCloseMsg);
        MessageHelper.RemoveListener<float>(MessageName.UICameraModeFovChanged, OnExternalFovChanged);

        if (cameraModeSpanner != null)
        {
            cameraModeSpanner.ValueChanged -= OnSpannerValueChanged;
        }

        if (cameraModeSelector != null)
        {
            // selector 不再使用 ValueChanged 切换选项
        }
    }

    private void BindToggle(CameraModeToggle toggle, CameraModeEffectParam param)
    {
        if (toggle == null) return;
        toggle.onValueChanged.AddListener(isOn =>
        {
            if (!isOn) return;
            SelectParam(param);
            if (_suppressVersionCheckForProgrammaticToggle) return;
            if (_isCameraModeFeatureVersionSupported) return;
            _isCameraModeFeatureVersionSupported = IsCameraModeFeatureVersionSupported();
        });
    }

    private void SelectParam(CameraModeEffectParam param)
    {
        _currentParam = param;

        if (cameraModeSpanner == null) return;
        var ctrl = _ctrls?.Get(param);
        if (ctrl == null) return;

        // 仅“切换选项”不算“触发调参”，忽略一次 SetCtrl 带来的 ValueChanged 事件
        _ignoreNextSpannerValueEvent = true;
        cameraModeSpanner.SetCtrl(ctrl);
        SyncSpannerAudioStepBaseline(ctrl, ctrl.GetValue());
        // 数值显示由 Spanner 自己处理（支持小数格式）
        // 仅当景深当前为 0.0 时，切到 Focus 才提示“请配合景深使用”。
        if (param == CameraModeEffectParam.Focus && _ctrls != null && _ctrls.DoF.GetValue() == 0)
        {
            TipPanel.ShowToast("请配合景深使用");
        }
    }

    private void OnSpannerValueChanged(ICameraModeCtrl ctrl, int rawValue)
    {
        if (_ignoreNextSpannerValueEvent)
        {
            _ignoreNextSpannerValueEvent = false;
            return;
        }


        // spanner 被“真正触发调参”后：如果 selector 被外部关闭过，则恢复显示
        EnsureSelectorVisibleAndSynced(broadcastOpen: true);
        TryPlaySpannerStepAudio(ctrl, rawValue);

        // 这里只做占位：后面接真实相机/后处理参数时，在这里调用对应实现即可
        float value = ResolveValueAsFloat(ctrl, rawValue);

        switch (_currentParam)
        {
            case CameraModeEffectParam.Bloom:
                ApplyBloom(value);
                break;
            case CameraModeEffectParam.Focus:
                ApplyFocus(value);
                break;
            case CameraModeEffectParam.DoF:
                ApplyDoF(value);
                break;
            case CameraModeEffectParam.Contrast:
                ApplyContrast(value);
                break;
            case CameraModeEffectParam.Saturation:
                ApplySaturation(value);
                break;
            case CameraModeEffectParam.Exposure:
                ApplyExposure(value);
                break;
            case CameraModeEffectParam.Tilt:
                ApplyTilt(value);
                break;
            case CameraModeEffectParam.FOV:
                ApplyFov(value);
                break;
        }
    }

    private static float ResolveValueAsFloat(ICameraModeCtrl ctrl, int rawValue)
    {
        if (ctrl is CameraModeScaledFloatCtrl scaled)
        {
            return scaled.GetFloatValue();
        }
        return rawValue;
    }

    private void SyncSpannerAudioStepBaseline(ICameraModeCtrl ctrl, int rawValue)
    {
        if (ctrl == null)
        {
            _spannerAudioCtrl = null;
            _lastSpannerAudioStepIndex = -1;
            return;
        }

        _spannerAudioCtrl = ctrl;
        _lastSpannerAudioStepIndex = ResolveSpannerAudioStepIndex(ctrl, rawValue);
    }

    private void TryPlaySpannerStepAudio(ICameraModeCtrl ctrl, int rawValue)
    {
        if (ctrl == null) return;

        int stepIndex = ResolveSpannerAudioStepIndex(ctrl, rawValue);
        if (_spannerAudioCtrl != ctrl)
        {
            _spannerAudioCtrl = ctrl;
            _lastSpannerAudioStepIndex = stepIndex;
            return;
        }

        if (_lastSpannerAudioStepIndex < 0)
        {
            _lastSpannerAudioStepIndex = stepIndex;
            return;
        }

        int delta = stepIndex - _lastSpannerAudioStepIndex;
        if (delta == 0) return;

        int playCount = Mathf.Abs(delta);
        for (int i = 0; i < playCount; i++)
        {
            AkSoundManager.Inst.PlayUIEffectSound(SpannerTurnAudioEvent);
        }

        _lastSpannerAudioStepIndex = stepIndex;
    }

    private static int ResolveSpannerAudioStepIndex(ICameraModeCtrl ctrl, int rawValue)
    {
        int min = ctrl.GetMinValue();
        int max = ctrl.GetMaxValue();
        if (max <= min) return 0;

        float t = Mathf.InverseLerp(min, max, rawValue);
        return Mathf.Clamp(Mathf.FloorToInt(t * SpannerAudioStepCount), 0, SpannerAudioStepCount - 1);
    }

    private void OnCloseLensControlMsg()
    {
        if (cameraModeSelector == null) return;
        cameraModeSelector.gameObject.SetActive(false);
    }

    private void OnOpenLensControlMsg()
    {
        // Lens 入口打开时：显示 selector（此时只是展示默认位置，不代表开始调参）
        EnsureSelectorVisibleAndSynced(broadcastOpen: false);
    }

    private void OnGlobalCloseMsg()
    {
        OnCloseLensControlMsg();
    }

    /// <summary>
    /// 外部（双指缩放等）改变了相机 FOV 后回调：将实际 FOV 值回写到 ctrl 并刷新 Spanner。
    /// </summary>
    private void OnExternalFovChanged(float fov)
    {
        if (_ctrls == null) return;
        // FOV ctrl scale=10，float 值直接对应相机度数
        int rawValue = Mathf.RoundToInt(fov * 10);
        _ctrls.FOV.OnValueChanged(rawValue);

        // 仅当 Spanner 当前选中的就是 FOV 时才刷新显示，避免干扰其他参数的旋钮位置
        if (_currentParam == CameraModeEffectParam.FOV && cameraModeSpanner != null)
        {
            cameraModeSpanner.RefreshDisplay();
        }
    }

    private void EnsureSelectorVisibleAndSynced(bool broadcastOpen)
    {
        if (cameraModeSelector == null || _selectorCtrl == null) return;
        if (!cameraModeSelector.gameObject.activeSelf)
        {
            cameraModeSelector.gameObject.SetActive(true);
        }
        cameraModeSelector.SetInputEnabled(true);
        if (broadcastOpen)
        {
            MessageHelper.Broadcast(MessageName.UICameraModeOpenLensControl);
        }
    }

    private bool AnyToggleOn()
    {
        return (bloomToggle != null && bloomToggle.isOn)
               || (focusToggle != null && focusToggle.isOn)
               || (DoFToggle != null && DoFToggle.isOn)
               || (contrastToggle != null && contrastToggle.isOn)
               || (saturationToggle != null && saturationToggle.isOn)
               || (exposureToggle != null && exposureToggle.isOn)
               || (tiltToggle != null && tiltToggle.isOn)
               || (FOVToggle != null && FOVToggle.isOn);
    }

    private void SyncFromCurrentToggleState()
    {
        if (bloomToggle != null && bloomToggle.isOn) { SelectParam(CameraModeEffectParam.Bloom); return; }
        if (focusToggle != null && focusToggle.isOn) { SelectParam(CameraModeEffectParam.Focus); return; }
        if (DoFToggle != null && DoFToggle.isOn) { SelectParam(CameraModeEffectParam.DoF); return; }
        if (contrastToggle != null && contrastToggle.isOn) { SelectParam(CameraModeEffectParam.Contrast); return; }
        if (saturationToggle != null && saturationToggle.isOn) { SelectParam(CameraModeEffectParam.Saturation); return; }
        if (exposureToggle != null && exposureToggle.isOn) { SelectParam(CameraModeEffectParam.Exposure); return; }
        if (tiltToggle != null && tiltToggle.isOn) { SelectParam(CameraModeEffectParam.Tilt); return; }
        if (FOVToggle != null && FOVToggle.isOn) { SelectParam(CameraModeEffectParam.FOV); return; }
    }

    private void EnsureLensAdjustContext()
    {
        if (_lensAdjustStarted) return;
        _lensAdjustStarted = true;

        // Lens 调参和 Filter 互斥：一旦开始调参，立刻取消滤镜（并还原默认 Volume）
        _filterMenu ??= transform.root != null
            ? transform.root.GetComponentInChildren<CameraModeFilterMenu>(true)
            : GetComponentInChildren<CameraModeFilterMenu>(true);
        _filterMenu?.ResetToDefault(applyNow: true);

        // 从默认 Volume 克隆一份 runtime profile 专门给 Lens 改数值
        PostProcessManager.Inst.BeginLensAdjustMode();
    }

    // ===== 占位：具体功能后面实现 =====
    private void ApplyBloom(float value)
    {
        EnsureLensAdjustContext();
        PostProcessManager.Inst.SetLensBloom(value);
    }

    private void ApplyFocus(float value)
    {
        EnsureLensAdjustContext();
        PostProcessManager.Inst.SetLensFocus(value);
    }

    private void ApplyDoF(float value)
    {
        EnsureLensAdjustContext();
        PostProcessManager.Inst.SetLensDoF(value);
    }

    private void ApplyContrast(float value)
    {
        EnsureLensAdjustContext();
        PostProcessManager.Inst.SetLensContrast(value);
    }

    private void ApplySaturation(float value)
    {
        EnsureLensAdjustContext();
        PostProcessManager.Inst.SetLensSaturation(value);
    }

    private void ApplyExposure(float value)
    {
        EnsureLensAdjustContext();
        PostProcessManager.Inst.SetLensExposure(value);
    }

    private void ApplyTilt(float value)
    {
        EnsureLensAdjustContext();
        CacheCameraLensIfNeeded();

        var vcam = ResolveActiveVirtualCamera();
        if (vcam == null) return;

        // Cinemachine：Dutch = roll（绕镜头 Z 轴旋转，单位度）
        var lens = vcam.m_Lens;
        lens.Dutch = Mathf.Clamp(value, -180f, 180f);
        vcam.m_Lens = lens;
        _camLensDirty = true;
    }

    private void ApplyFov(float value)
    {
        EnsureLensAdjustContext();
        CacheCameraLensIfNeeded();

        var vcam = ResolveActiveVirtualCamera();
        if (vcam == null) return;

        var lens = vcam.m_Lens;
        // Cinemachine FOV（度）。避免 0 导致异常体验，做一个合理钳制
        lens.FieldOfView = Mathf.Clamp(value, 1f, 179f);
        vcam.m_Lens = lens;
        _camLensDirty = true;
    }

    private CinemachineVirtualCamera ResolveActiveVirtualCamera()
    {
        try
        {
            var ctrl = CameraModeConotroller.Inst;
            if (ctrl == null) return null;

            // CameraModeVirCamera（相机模式虚拟相机）
            var cmVcam = ctrl.CreateVirCameraOnCameraMode();
            // 场景默认玩家相机（PlayVirtualCamera）
            var playVcam = ctrl.GetNormalVirCamera();

            // 约定：两者通常只有一个 enabled=true；优先返回 enabled 的那个
            if (playVcam != null && playVcam.enabled && (cmVcam == null || !cmVcam.enabled)) return playVcam;
            if (cmVcam != null && cmVcam.enabled) return cmVcam;

            // 兜底：都没启用时，优先相机模式相机
            return cmVcam != null ? cmVcam : playVcam;
        }
        catch
        {
            return null;
        }
    }

    private void CacheCameraLensIfNeeded()
    {
        if (_camLensCached) return;
        _camLensCached = true;
        _camLensDirty = false;

        try
        {
            var ctrl = CameraModeConotroller.Inst;
            if (ctrl == null) return;

            var cmVcam = ctrl.CreateVirCameraOnCameraMode();
            var playVcam = ctrl.GetNormalVirCamera();

            if (cmVcam != null) _cachedLensForCameraMode = cmVcam.m_Lens;
            if (playVcam != null) _cachedLensForPlay = playVcam.m_Lens;
        }
        catch
        {
            // ignore
        }
    }

    private void RestoreCachedCameraLens()
    {
        if (!_camLensCached) return;

        try
        {
            var ctrl = CameraModeConotroller.Inst;
            if (ctrl == null) return;

            var cmVcam = ctrl.CreateVirCameraOnCameraMode();
            var playVcam = ctrl.GetNormalVirCamera();

            if (cmVcam != null) cmVcam.m_Lens = _cachedLensForCameraMode;
            if (playVcam != null) playVcam.m_Lens = _cachedLensForPlay;
        }
        catch
        {
            // ignore
        }
        finally
        {
            _camLensDirty = false;
            _camLensCached = false;
        }
    }

    private sealed class CameraModeSelectorCtrl : CameraModeRangedIntCtrl, ICameraModeCtrlValueText
    {
        public CameraModeSelectorCtrl() : base(min: 0, max: 7, defaultValue: 0) { }
        public string GetValueText() => string.Empty; // selector 不显示数值文字
    }

    private bool IsCameraModeFeatureVersionSupported()
    {
        if (DeviceInfoManager.Inst != null
            && DeviceInfoManager.Inst.DeviceBaseData != null
            && DeviceInfoManager.Inst.CheckVersion_1_0_18())
        {
            return true;
        }
        UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, GameData.Base.ForceUpdate.NeedUpdateFeature);
        //TipPanel.ShowToast("该功能仅在1.0.18及以上版本可用");
        return false;
    }
}
