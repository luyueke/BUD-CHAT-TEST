using Game.BudBox;
using Message;
using UI.UIPanels.IncubationCabin; // 交互埋点上报方法 IncubationCabinControll.ReportThinkingData
using UnityEngine;
using UnityEngine.EventSystems; // 滑动条松手回调参数 PointerEventData
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 结算面板 - 体验设置子面板
///       包含硬件控制（音量 / 亮度 / 静音）、休眠设置以及屏幕保护功能
/// Date: 26-04-09
/// </summary>
public class CabinControllExperiencePanel : CabinControllSettleSubPanel
{
    [Header("硬件控制")]
    [SerializeField] private Slider VolumeSlider;          // 音量滑动条 (0~100)
    [SerializeField] private Image VolumeSliderFill;       // 音量滑动条 Fill Image（进度填充部分）
    [SerializeField] private Image VolumeSliderHandle;     // 音量滑动头 Image（拖拽把手）
    [SerializeField] private Slider BrightnessSlider;      // 亮度滑动条 (0~100)
    [SerializeField] private Image BrightnessSliderFill;   // 亮度滑动条 Fill Image（进度填充部分）
    [SerializeField] private Image BrightnessSliderHandle; // 亮度滑动头 Image（拖拽把手）
    [SerializeField] private GoToggle isMuteTg;            // 静音开关
    [SerializeField] private Dropdown DormantTimer;        // 休眠时间下拉列表
    [SerializeField] private Image DormantImage;           // 休眠图标（需渲染在 Dropdown List 之上）
    [SerializeField] private Canvas DormantCanvas;         // DormantImage 上的独立 Canvas，用于控制渲染层级

    [Header("滑动条图片")]
    [SerializeField] private Sprite ActiveSliderSprite;    // 激活状态：滑动条图片（图片1）
    [SerializeField] private Sprite ActiveHandleSprite;    // 激活状态：滑动头图片（图片2）
    [SerializeField] private Sprite InactiveSliderSprite;  // 失活状态：滑动条图片（图片3）
    [SerializeField] private Sprite InactiveHandleSprite;  // 失活状态：滑动头图片（图片4）

    [Header("屏幕保护")]
    [SerializeField] private GoToggle ProtectScreenTg;        // 屏幕保护总开关
    [SerializeField] private GameObject ProtectContentNode;   // 开启屏幕保护时显示的内容容器
    [SerializeField] private Toggle ScheduledScreenTg;      // 定时熄屏模式选项
    [SerializeField] private Toggle AutoScreenTg;           // 自动熄屏模式选项
    [SerializeField] private GameObject ScheduledTimeNode;    // 定时熄屏时间设置容器
    [SerializeField] private Dropdown StartHourDropdown;      // 开始时间-小时(0-23)
    [SerializeField] private Dropdown StartMinuteDropdown;    // 开始时间-分钟(0/15/30/45)
    [SerializeField] private Dropdown EndHourDropdown;        // 结束时间-小时(0-23)
    [SerializeField] private Dropdown EndMinuteDropdown;      // 结束时间-分钟(0/15/30/45)
    [SerializeField] private GameObject AutoTimeNode;         // 自动熄屏设置容器
    [SerializeField] private Dropdown AutoTimerDropdown;      // 自动熄屏倒计时下拉框
    [SerializeField] private GameObject ProtectOffNode;   // 关闭屏幕保护时固定显示的 Tips 节点
    [SerializeField] private GameObject NextDayNode;      // 跨天时显示在结束时间后的「（次日）」标签

    private int _lastSentVolume = -1;     // 上次发送 MQTT 时的音量值，-1 表示未初始化
    private int _lastSentBrightness = -1; // 上次发送 MQTT 时的亮度值，-1 表示未初始化

    private bool _volumeChangedDuringDrag = false;     // 本次拖动音量是否发生过变化（松手时据此决定是否上报埋点）
    private bool _brightnessChangedDuringDrag = false; // 本次拖动亮度是否发生过变化（松手时据此决定是否上报埋点）

    // 分钟选项数组，下标对应 Dropdown 各选项（倒序：45/30/15/00）
    private readonly int[] MinuteArr = new int[4] { 45, 30, 15, 0 };

    // 自动熄屏倒计时选项（分钟），下标对应 Dropdown 各选项（倒序：60/45/30/15/10/5/1）
    private readonly int[] AutoTimerArr = new int[7] { 60, 45, 30, 15, 10, 5, 1 };

    // 当前有效的定时熄屏时间下标，用于验证失败时回退到上一次有效值
    // 小时 Dropdown 为倒序（Index 0 = 23时，Index 23 = 0时），转换公式：hour = 23 - index
    private int _startHourIndex = 21;   // 默认开始时间 2:00 → index = 23 - 2 = 21
    private int _startMinuteIndex = 3;  // 默认 00分 → MinuteArr 倒序后 index = 3
    private int _endHourIndex = 16;     // 默认结束时间 7:00 → index = 23 - 7 = 16
    private int _endMinuteIndex = 3;    // 默认 00分 → MinuteArr 倒序后 index = 3

    private void Awake()
    {
        DormantTimer.onValueChanged.AddListener(OnDormantTimer);

        var notifier = DormantTimer.gameObject.AddComponent<DropdownOpenCloseNotifier>();
        notifier.onShown  += OnDormantDropdownOpen;
        notifier.onHidden += OnDormantDropdownClose;
        isMuteTg.Set(CabinBoxManager.Inst.GetBoxIsMute(), false);
        isMuteTg.onValueChanged.AddListener(OnMuteTg);

        // ─── 音量滑动条 初始化 ───
        VolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        // 监听松手事件：拖动结束时若本次确实调过音量，则上报一次埋点
        var volumeUpListener = VolumeSlider.gameObject.AddComponent<ClickEventListener>();
        volumeUpListener.AddPointerUpHandler(OnVolumeSliderPointerUp);

        // ─── 亮度滑动条 初始化 ───
        BrightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        // 监听松手事件：拖动结束时若本次确实调过亮度，则上报一次埋点
        var brightnessUpListener = BrightnessSlider.gameObject.AddComponent<ClickEventListener>();
        brightnessUpListener.AddPointerUpHandler(OnBrightnessSliderPointerUp);

        // ─── 屏幕保护 初始化 ───
        ProtectScreenTg.onValueChanged.AddListener(OnProtectScreenTg);
        ScheduledScreenTg.onValueChanged.AddListener(OnScheduledScreenTg);
        AutoScreenTg.onValueChanged.AddListener(OnAutoScreenTg);
        StartHourDropdown.onValueChanged.AddListener(OnStartHourChanged);
        StartMinuteDropdown.onValueChanged.AddListener(OnStartMinuteChanged);
        EndHourDropdown.onValueChanged.AddListener(OnEndHourChanged);
        EndMinuteDropdown.onValueChanged.AddListener(OnEndMinuteChanged);
        AutoTimerDropdown.onValueChanged.AddListener(OnAutoTimerChanged);
        MessageHelper.AddListener<string>(MessageName.OnBudBoxBaseDataRest, OnBudBoxBaseDataRest);
        MessageHelper.AddListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
    }

    private void OnEnable()
    {
        OnBudBoxBaseDataRest(CabinBoxManager.Inst.GetCurrentDeviceId());
    }

    /// <summary>
    /// 静音开关变更：开启静音时保存当前音量，关闭静音时自动恢复；校验设备状态后更新本地缓存并发送 MQTT 指令
    /// </summary>
    private void OnMuteTg(bool isOn)
    {
        if (CabinBoxManager.Inst.GetCabinBudBoxData() == null)
        {
            TipPanel.ShowToast("设备不存在!");
            return;
        }
        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
        {
            TipPanel.ShowToast("设备离线无法设置!");
            return;
        }

        // 静音开关交互埋点：开/关两态均上报
        IncubationCabinControll.ReportThinkingData("mute_toggle");

        if (isOn)
        {
            // 静音前保存当前音量（大于 0 才有保存意义）
            int currentVolume = (int)VolumeSlider.value;
            if (currentVolume > 0)
            {
                CabinBoxManager.Inst.SetBoxPreMuteVolume(currentVolume);
            }
        }
        else
        {
            // 取消静音：自动恢复静音前的音量，无需玩家手动调节
            int preMuteVolume = CabinBoxManager.Inst.GetBoxPreMuteVolume();
            if (preMuteVolume > 0)
            {
                CabinBoxManager.Inst.SetBoxVolume(preMuteVolume);
                CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_volume);
                VolumeSlider.SetValueWithoutNotify(preMuteVolume);
                _lastSentVolume = preMuteVolume;
                CabinBoxManager.Inst.SetBoxPreMuteVolume(0);
            }
        }

        CabinBoxManager.Inst.SetBoxIsMute(isOn ? 1 : 0);
        CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_souceOff);
    }

    /// <summary>
    /// 设备基础数据重置回调：同步亮度、音量、静音值以及屏幕保护状态到 UI 控件（暂停监听避免触发回调）。
    /// 静音期间若已保存静音前音量，则显示静音前音量，避免硬件上报 0 时滑块被置零。
    /// </summary>
    private void OnBudBoxBaseDataRest(string deviceId)
    {
        if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId())
            return;

        // 同步硬件当前值到滑动条（不触发 onValueChanged 回调）
        int brightness = CabinBoxManager.Inst.GetBoxIuminance();
        int volume = CabinBoxManager.Inst.GetBoxVolume();
        BrightnessSlider.SetValueWithoutNotify(brightness);
        _lastSentBrightness = brightness;

        bool isMuted = CabinBoxManager.Inst.GetBoxIsMute();
        isMuteTg.Set(isMuted, false);

        // 静音期间若已保存静音前音量，则显示静音前音量；否则显示硬件上报的音量
        int preMuteVolume = CabinBoxManager.Inst.GetBoxPreMuteVolume();
        if (isMuted && preMuteVolume > 0)
        {
            VolumeSlider.SetValueWithoutNotify(preMuteVolume);
            _lastSentVolume = preMuteVolume;
        }
        else
        {
            VolumeSlider.SetValueWithoutNotify(volume);
            _lastSentVolume = volume;
        }

        RefreshSliderInteractable();

        // 同步屏幕保护 UI 状态
        RefreshProtectScreenUI();
    }

    /// <summary>
    /// 设备连接状态变更回调：实时更新 Slider 可交互状态
    /// </summary>
    private void OnBoxDeviceStateChanged(string deviceId)
    {
        if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId())
            return;

        RefreshSliderInteractable();
    }

    /// <summary>
    /// 根据设备在线状态启用或禁用音量/亮度滑动条及静音开关，
    /// 同时切换滑动条 Fill 和滑动头 Handle 的图片：
    /// 激活时使用 ActiveSliderSprite（图片1）/ ActiveHandleSprite（图片2），
    /// 失活时使用 InactiveSliderSprite（图片3）/ InactiveHandleSprite（图片4）。
    /// </summary>
    private void RefreshSliderInteractable()
    {
        bool isOnline = CabinBoxManager.Inst.GetBoxState() == BoxState.Online;

        // 根据在线状态选择对应的滑动条和滑动头图片
        Sprite sliderSprite = isOnline ? ActiveSliderSprite : InactiveSliderSprite;
        Sprite handleSprite = isOnline ? ActiveHandleSprite : InactiveHandleSprite;

        // ── 音量 Slider ──
        VolumeSlider.interactable = isOnline;

        if (VolumeSliderFill != null && sliderSprite != null)
        {
            VolumeSliderFill.sprite = sliderSprite;
        }

        if (VolumeSliderHandle != null && handleSprite != null)
        {
            VolumeSliderHandle.sprite = handleSprite;
        }


        // ── 亮度 Slider ──
        BrightnessSlider.interactable = isOnline;

        if (BrightnessSliderFill != null && sliderSprite != null)
        {
            BrightnessSliderFill.sprite = sliderSprite;
        }

        if (BrightnessSliderHandle != null && handleSprite != null)
        {
            BrightnessSliderHandle.sprite = handleSprite;
        }

        isMuteTg.interactable = isOnline;
    }

    // 休眠时间选项（分钟），下标对应 Dropdown 各选项，0 表示永不休眠
    private int[] DormantTimerArr = new int[8] { 1, 2, 5, 10, 20, 30, 60, 0 };

    // 用户选择休眠时间后同步设置到硬件
    private void OnDormantTimer(int index)
    {
        if (CabinBoxManager.Inst.GetCabinBudBoxData() == null)
        {
            TipPanel.ShowToast("设备不存在!");
            return;
        }
        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
        {
            TipPanel.ShowToast("设备离线无法设置!");
            return;
        }
        if (index>= DormantTimerArr.Length)
        {
            return;
        }
        int timer = DormantTimerArr[index];
        CabinBoxManager.Inst.SetBoxDormantTimer(timer);
        CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_dormant);
    }

    private void OnVolumeChanged(float value)
    {
        int intValue = (int)value;
        CabinBoxManager.Inst.SetBoxVolume(intValue);

        // 本次拖动确实改变了音量，置脏标记（松手时再上报埋点，避免拖动过程频繁上报）
        _volumeChangedDuringDrag = true;

        // 变化超过 1 才发送，避免滑动过程中每帧发包
        if (Mathf.Abs(intValue - _lastSentVolume) > 1)
        {
            _lastSentVolume = intValue;
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_volume);
        }
    }

    private void OnBrightnessChanged(float value)
    {
        int intValue = (int)value;
        CabinBoxManager.Inst.SetBoxIuminance(intValue);

        // 本次拖动确实改变了亮度，置脏标记（松手时再上报埋点，避免拖动过程频繁上报）
        _brightnessChangedDuringDrag = true;

        // 变化超过 1 才发送，避免滑动过程中每帧发包
        if (Mathf.Abs(intValue - _lastSentBrightness) > 1)
        {
            _lastSentBrightness = intValue;
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_brightness);
        }
    }

    /// <summary>
    /// 音量滑动条松手回调：本次拖动确实调整过音量才上报一次「调整音量」埋点，随后复位脏标记。
    /// 离线时滑动条不可交互、不会触发 OnVolumeChanged，脏标记保持 false，不会误报。
    /// </summary>
    private void OnVolumeSliderPointerUp(GameObject go, PointerEventData eventData)
    {
        if (!_volumeChangedDuringDrag)
            return;

        _volumeChangedDuringDrag = false;
        IncubationCabinControll.ReportThinkingData("volume_adjust");
    }

    /// <summary>
    /// 亮度滑动条松手回调：本次拖动确实调整过亮度才上报一次「调整亮度」埋点，随后复位脏标记。
    /// 离线时滑动条不可交互、不会触发 OnBrightnessChanged，脏标记保持 false，不会误报。
    /// </summary>
    private void OnBrightnessSliderPointerUp(GameObject go, PointerEventData eventData)
    {
        if (!_brightnessChangedDuringDrag)
            return;

        _brightnessChangedDuringDrag = false;
        IncubationCabinControll.ReportThinkingData("brightness_adjust");
    }

    // Dropdown 展开时提升休眠图标的渲染层级，防止被 Dropdown List 遮挡
    private void OnDormantDropdownOpen()
    {
        if (DormantCanvas == null)
        {
            DormantCanvas = DormantImage.gameObject.GetComponent<Canvas>() ?? DormantImage.gameObject.AddComponent<Canvas>();
        }
        DormantCanvas.overrideSorting = true;
        DormantCanvas.sortingOrder = 30001;
    }

    // Dropdown 收起时还原层级覆盖，避免影响其他 UI 渲染
    private void OnDormantDropdownClose()
    {
        if (DormantCanvas != null)
        {
            DormantCanvas.overrideSorting = false;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 屏幕保护功能
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 屏幕保护总开关切换回调。
    /// 开启时直接更新状态并下发 MQTT；关闭时弹出二确弹窗，用户确认后再执行。
    /// </summary>
    private void OnProtectScreenTg(bool isOn)
    {
        if (CabinBoxManager.Inst.GetCabinBudBoxData() == null)
        {
            TipPanel.ShowToast("设备不存在!");
            // 回退 Toggle 状态，不触发回调
            ProtectScreenTg.Set(!isOn, false);
            return;
        }

        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
        {
            TipPanel.ShowToast("设备离线无法设置!");
            ProtectScreenTg.Set(!isOn, false);
            return;
        }

        if (isOn)
        {
            // 直接开启屏幕保护
            CabinBoxManager.Inst.SetProtectScreenState(1);
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_protectScreenState);
            RefreshProtectScreenUI();
            // 屏幕保护开关交互埋点：开启路径
            IncubationCabinControll.ReportThinkingData("screen_saver_toggle");
        }
        else
        {
            // 关闭前先回退 Toggle，等待用户确认
            ProtectScreenTg.Set(true, false);

            // 弹出二确弹窗
            var panel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
            panel.SetTextAndAction(
                "关闭屏幕保护功能",
                "长时间保持屏幕亮起会严重降低BOX使用寿命\n是否要确认关闭?",
                "确定",
                "取消",
                confirmClick: () =>
                {
                    // 用户确认：真正关闭屏幕保护
                    CabinBoxManager.Inst.SetProtectScreenState(0);
                    CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_protectScreenState);
                    RefreshProtectScreenUI();
                    // 屏幕保护开关交互埋点：关闭路径（用户二次确认后）
                    IncubationCabinControll.ReportThinkingData("screen_saver_toggle");
                },
                cancelClick: () =>
                {
                    // 用户取消：保持开启状态，无需额外操作
                });
        }
    }

    /// <summary>
    /// 勾选「定时熄屏」时触发。
    /// 仅在 isOn 为 true 时生效（防止另一个 Toggle 取消时重复触发），切换保护方式并刷新 UI。
    /// </summary>
    private void OnScheduledScreenTg(bool isOn)
    {
        if (!isOn)
            return;

        if (CabinBoxManager.Inst.GetCabinBudBoxData() == null)
        {
            TipPanel.ShowToast("设备不存在!");
            return;
        }

        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
        {
            TipPanel.ShowToast("设备离线无法设置!");
            return;
        }

        // 切换为定时熄屏模式（0）
        CabinBoxManager.Inst.SetProtectScreenSwith(0);
        CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_protectScreenSwith);

        // 定时熄屏交互埋点：仅在选中该模式（isOn）时上报一次
        IncubationCabinControll.ReportThinkingData("screen_off_scheduled");

        // 同步模式 Toggle 状态，不触发回调
        AutoScreenTg.SetIsOnWithoutNotify(false);

        // 更新容器显示：展示定时时间节点，隐藏自动时间节点
        if (ScheduledTimeNode != null)
        {
            ScheduledTimeNode.SetActive(true);
        }

        if (AutoTimeNode != null)
        {
            AutoTimeNode.SetActive(false);
        }
    }

    /// <summary>
    /// 勾选「自动熄屏」时触发。
    /// 仅在 isOn 为 true 时生效，切换保护方式并刷新 UI。
    /// </summary>
    private void OnAutoScreenTg(bool isOn)
    {
        if (!isOn)
            return;

        if (CabinBoxManager.Inst.GetCabinBudBoxData() == null)
        {
            TipPanel.ShowToast("设备不存在!");
            return;
        }

        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
        {
            TipPanel.ShowToast("设备离线无法设置!");
            return;
        }

        // 切换为自动熄屏模式（1）
        CabinBoxManager.Inst.SetProtectScreenSwith(1);
        CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_protectScreenSwith);

        // 自动熄屏交互埋点：仅在选中该模式（isOn）时上报一次
        IncubationCabinControll.ReportThinkingData("screen_off_auto");

        // 同步模式 Toggle 状态，不触发回调
        ScheduledScreenTg.SetIsOnWithoutNotify(false);

        // 更新容器显示：隐藏定时时间节点，展示自动时间节点
        if (ScheduledTimeNode != null)
        {
            ScheduledTimeNode.SetActive(false);
        }

        if (AutoTimeNode != null)
        {
            AutoTimeNode.SetActive(true);
        }
    }

    /// <summary>定时熄屏时间 Dropdown 字段标识</summary>
    private enum TimeField { StartHour, StartMinute, EndHour, EndMinute }

    /// <summary>
    /// 开始时间「小时」下拉框变更回调。
    /// </summary>
    private void OnStartHourChanged(int index)
    {
        OnTimeDropdownChanged(index, TimeField.StartHour);
    }

    /// <summary>
    /// 开始时间「分钟」下拉框变更回调。
    /// </summary>
    private void OnStartMinuteChanged(int index)
    {
        OnTimeDropdownChanged(index, TimeField.StartMinute);
    }

    /// <summary>
    /// 结束时间「小时」下拉框变更回调。
    /// </summary>
    private void OnEndHourChanged(int index)
    {
        OnTimeDropdownChanged(index, TimeField.EndHour);
    }

    /// <summary>
    /// 结束时间「分钟」下拉框变更回调。
    /// </summary>
    private void OnEndMinuteChanged(int index)
    {
        OnTimeDropdownChanged(index, TimeField.EndMinute);
    }

    /// <summary>
    /// 定时熄屏时间 Dropdown 变更的统一处理逻辑。
    /// 支持跨天设置：开始时间 >= 结束时间时自动显示「（次日）」标签，不再校验顺序。
    /// </summary>
    /// <param name="index">新选中的 Dropdown 下标</param>
    /// <param name="field">变更的时间字段</param>
    private void OnTimeDropdownChanged(int index, TimeField field)
    {
        // 直接更新对应缓存下标（支持跨天，无需校验开始早于结束）
        switch (field)
        {
            case TimeField.StartHour:   _startHourIndex   = index; break;
            case TimeField.StartMinute: _startMinuteIndex = index; break;
            case TimeField.EndHour:     _endHourIndex     = index; break;
            case TimeField.EndMinute:   _endMinuteIndex   = index; break;
        }

        ApplyScheduledTime();
        RefreshNextDayLabel();
    }

    /// <summary>
    /// 将当前记录的开始/结束时间下标写入 DeviceState 并下发 MQTT 指令。
    /// </summary>
    private void ApplyScheduledTime()
    {
        // 小时 Dropdown 倒序，还原实际小时：hour = 23 - index
        int startMins = (23 - _startHourIndex) * 60 + MinuteArr[_startMinuteIndex];
        int endMins   = (23 - _endHourIndex)   * 60 + MinuteArr[_endMinuteIndex];
        string timeStr = $"{startMins}_{endMins}";

        CabinBoxManager.Inst.SetProtectScreenTime(timeStr);
        CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_protectScreenTime);
    }

    /// <summary>
    /// 自动熄屏倒计时下拉框变更回调：更新倒计时时间并下发 MQTT。
    /// </summary>
    private void OnAutoTimerChanged(int index)
    {
        if (CabinBoxManager.Inst.GetCabinBudBoxData() == null)
        {
            TipPanel.ShowToast("设备不存在!");
            return;
        }

        if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
        {
            TipPanel.ShowToast("设备离线无法设置!");
            return;
        }

        if (index >= AutoTimerArr.Length)
            return;

        int minutes = AutoTimerArr[index];
        CabinBoxManager.Inst.SetProtectScreenAuto(minutes);
        CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_protectScreenAuto);

        // 自动熄屏时间调整交互埋点
        IncubationCabinControll.ReportThinkingData("screen_off_auto_time_adjust");
    }

    /// <summary>
    /// 根据 DeviceState 同步所有屏幕保护 UI 控件的显示状态（不触发各控件的回调）。
    /// </summary>
    private void RefreshProtectScreenUI()
    {
        int state = CabinBoxManager.Inst.GetProtectScreenState();
        int swith = CabinBoxManager.Inst.GetProtectScreenSwith();
        string timeStr = CabinBoxManager.Inst.GetProtectScreenTime();
        int autoMinutes = CabinBoxManager.Inst.GetProtectScreenAuto();

        // 同步屏幕保护总开关（不触发回调）
        ProtectScreenTg.Set(state == 1, false);

        // 根据开关状态控制内容容器和关闭 Tips 的显示
        if (ProtectContentNode != null)
        {
            ProtectContentNode.SetActive(state == 1);
        }

        if (ProtectOffNode != null)
        {
            ProtectOffNode.SetActive(state == 0);
        }

        // 同步模式选项（不触发回调）
        ScheduledScreenTg.SetIsOnWithoutNotify(swith == 0);
        AutoScreenTg.SetIsOnWithoutNotify(swith == 1);

        // 根据模式切换容器显示
        if (ScheduledTimeNode != null)
        {
            ScheduledTimeNode.SetActive(swith == 0);
        }

        if (AutoTimeNode != null)
        {
            AutoTimeNode.SetActive(swith == 1);
        }

        // 解析定时熄屏时间字符串并同步 Dropdown（格式：开始分钟数_结束分钟数）
        if (!string.IsNullOrEmpty(timeStr))
        {
            string[] parts = timeStr.Split('_');
            if (parts.Length == 2
                && int.TryParse(parts[0], out int startMins)
                && int.TryParse(parts[1], out int endMins))
            {
                // 解算小时和分钟索引
                int startHour = startMins / 60;
                int startMinute = startMins % 60;
                int endHour = endMins / 60;
                int endMinute = endMins % 60;

                // 将分钟值转换为 MinuteArr 中的下标（找最近的合法值）
                int startMinIdx = GetMinuteIndex(startMinute);
                int endMinIdx = GetMinuteIndex(endMinute);

                // 缓存下标，供次日标签计算及 Dropdown 回显使用
                // 小时 Dropdown 倒序，转换公式：index = 23 - hour
                _startHourIndex = 23 - Mathf.Clamp(startHour, 0, 23);
                _startMinuteIndex = startMinIdx;
                _endHourIndex = 23 - Mathf.Clamp(endHour, 0, 23);
                _endMinuteIndex = endMinIdx;

                StartHourDropdown.SetValueWithoutNotify(_startHourIndex);
                StartMinuteDropdown.SetValueWithoutNotify(_startMinuteIndex);
                EndHourDropdown.SetValueWithoutNotify(_endHourIndex);
                EndMinuteDropdown.SetValueWithoutNotify(_endMinuteIndex);
            }
        }

        // 同步自动熄屏倒计时 Dropdown
        int autoIndex = System.Array.IndexOf(AutoTimerArr, autoMinutes);
        if (autoIndex < 0)
        {
            autoIndex = 4; // 默认 10 分钟（倒序后下标 4）
        }

        AutoTimerDropdown.SetValueWithoutNotify(autoIndex);

        // 同步次日标签显示状态
        RefreshNextDayLabel();
    }

    /// <summary>
    /// 根据当前开始/结束时间判断是否跨天，刷新「（次日）」标签的显示状态。
    /// 开始时间 >= 结束时间时显示，表示结束时间为次日；否则隐藏。
    /// </summary>
    private void RefreshNextDayLabel()
    {
        if (NextDayNode == null)
            return;

        // 小时 Dropdown 倒序：hour = 23 - index
        int startMins = (23 - _startHourIndex) * 60 + MinuteArr[_startMinuteIndex];
        int endMins   = (23 - _endHourIndex)   * 60 + MinuteArr[_endMinuteIndex];

        // 开始 >= 结束说明跨天，显示次日标签
        NextDayNode.SetActive(startMins >= endMins);
    }

    /// <summary>
    /// 将分钟值映射到 MinuteArr 中最近的合法下标（0/15/30/45）。
    /// </summary>
    /// <param name="minute">原始分钟值</param>
    /// <returns>MinuteArr 中的下标</returns>
    private int GetMinuteIndex(int minute)
    {
        // 找出与 minute 最接近的分钟选项下标
        int bestIdx = 0;
        int bestDiff = int.MaxValue;

        for (int i = 0; i < MinuteArr.Length; i++)
        {
            int diff = Mathf.Abs(MinuteArr[i] - minute);

            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestIdx = i;
            }
        }

        return bestIdx;
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<string>(MessageName.OnBudBoxBaseDataRest, OnBudBoxBaseDataRest);
        MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
    }
}
