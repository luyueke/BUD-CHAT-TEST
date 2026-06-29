using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraModeSpannerCtrl : ICameraModeCtrl
{
    private int m_Value = 0;

    public int GetValue()
    {
        return m_Value;
    }

    public int GetMaxValue()
    {
        return 100;
    }

    public int GetMinValue()
    {
        return 0;
    }

    public void OnRelease()
    {
    }

    public void OnReset()
    {
        m_Value = 0;
    }

    public void OnTrigger()
    {
        
    }

    public void OnValueChanged(int value)
    {
        m_Value = value;
    }
}

/// <summary>
/// 可选扩展：用于让 Spanner 按控制器自定义格式显示数值（例如 1.0）。
/// 不影响现有仅用 int 的控制器。
/// </summary>
public interface ICameraModeCtrlValueText
{
    string GetValueText();
}

public class CameraModeSpanner : MonoBehaviour
{
    private ICameraModeCtrl _ctrl;

    /// <summary>
    /// 当 SetCtrl 切换控制器时触发（也会在 Start 的默认 ctrl 初始化后触发一次）。
    /// </summary>
    public event Action<ICameraModeCtrl> CtrlChanged;

    /// <summary>
    /// 当数值变化时触发（拖动过程中会频繁触发；切换 ctrl 后也会触发一次用于同步）。
    /// </summary>
    public event Action<ICameraModeCtrl, int> ValueChanged;

    public ICameraModeCtrl GetCtrl() => _ctrl;

    private bool _isDragging;
    private int _activePointerId = int.MinValue;
    private Vector2 _dragStartPointerPos;
    private float _dragStartAngle;
    private int _dragStartValue;
    private float _currentAngle;
    private bool _hasCurrentAngle;
    private bool _rotationOnly;

    private RectTransform btn_Root;
    private RectTransform pointer_Root;
    private RectTransform value_Root;
    private Text value_Text;
    private EventTrigger touch_Spanner;

    [SerializeField] private float MinAngle = 120f;
    [SerializeField] private float MaxAngle = 240f;
    
    // 如果素材默认方向是朝左(180度)而非朝右(0度)，需要补偿180度
    [SerializeField] private float AngleOffset = 180f;

    [SerializeField] private float Sensitivity = 0.03f; // 滑动敏感度，默认 1.0

    // Values will be retrieved from controller if possible
    [SerializeField] private int MinValue = 0;
    [SerializeField] private int MaxValue = 100;


    private void Start()
    {
        btn_Root = transform.Find("BtnRoot").GetComponent<RectTransform>();
        pointer_Root = transform.Find("PointerRoot").GetComponent<RectTransform>();
        value_Root = transform.Find("ValueRoot").GetComponent<RectTransform>();
        value_Text = value_Root.Find("ValueText").GetComponent<Text>();
        touch_Spanner = GetComponentInChildren<EventTrigger>();

        // Clear existing triggers to avoid duplication if Init is called multiple times
        touch_Spanner.triggers.Clear();

        // Setup EventTrigger for Drag
        EventTrigger.Entry dragEntry = new EventTrigger.Entry();
        dragEntry.eventID = EventTriggerType.Drag;
        dragEntry.callback.AddListener(OnDragTrigger);
        touch_Spanner.triggers.Add(dragEntry);

        // Setup EventTrigger for PointerDown (to allow jumping to value on touch)
        EventTrigger.Entry downEntry = new EventTrigger.Entry();
        downEntry.eventID = EventTriggerType.PointerDown;
        downEntry.callback.AddListener(OnPointerDownTrigger);
        touch_Spanner.triggers.Add(downEntry);

        // 结束拖拽/抬手时必须重置状态，避免下一次拖拽沿用旧起点导致角度跳变
        EventTrigger.Entry upEntry = new EventTrigger.Entry();
        upEntry.eventID = EventTriggerType.PointerUp;
        upEntry.callback.AddListener(OnPointerUpTrigger);
        touch_Spanner.triggers.Add(upEntry);

        EventTrigger.Entry endDragEntry = new EventTrigger.Entry();
        endDragEntry.eventID = EventTriggerType.EndDrag;
        endDragEntry.callback.AddListener(OnPointerUpTrigger);
        touch_Spanner.triggers.Add(endDragEntry);
        
        // 允许外部在 Start 前先注入 ctrl（例如 selector）
        // 避免这里覆盖成默认 ctrl，导致显示/交互起点错乱。
        if (_ctrl == null)
        {
            _ctrl = new CameraModeSpannerCtrl();
        }

        if (_ctrl != null)
        {
            UpdateVisuals();
            CtrlChanged?.Invoke(_ctrl);
            ValueChanged?.Invoke(_ctrl, _ctrl.GetValue());
        }
    }

    /// <summary>
    /// 旋钮是否只用于“旋转显示/转盘”，而不改变 ctrl 的数值（也不触发 ValueChanged）。
    /// 用于像 selector 这种：拖拽只是把按钮转盘转到合适位置，真正选中由点击按钮完成。
    /// </summary>
    public void SetRotationOnly(bool rotationOnly)
    {
        _rotationOnly = rotationOnly;
    }

    /// <summary>
    /// 是否允许该旋钮响应拖拽/点击输入。
    /// 用于像 cameraModeSelector 这种“只展示位置，不允许滑动改变”的场景。
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        // touch_Spanner 可能尚未初始化（外部在 Awake/Start 之前调用）
        touch_Spanner ??= GetComponentInChildren<EventTrigger>();
        if (touch_Spanner != null)
        {
            touch_Spanner.enabled = enabled;
        }
        if (!enabled)
        {
            _isDragging = false;
        }
    }

    public void OnDragTrigger(BaseEventData data)
    {
        UpdateValueFromInput(data as PointerEventData);
    }

    public void OnPointerDownTrigger(BaseEventData data)
    {
        var pointerData = data as PointerEventData;
        if (pointerData == null) return;
        BeginInteraction(pointerData);
    }

    public void OnPointerUpTrigger(BaseEventData data)
    {
        EndInteraction();
    }

    private void UpdateValueFromInput(PointerEventData pointerData)
    {
        if (pointerData == null) return;
        if (!_rotationOnly && _ctrl == null) return;

        // 如果拖拽尚未开始（例如直接调用 Drag），确保初始化起点
        if (!_isDragging)
        {
            BeginInteraction(pointerData);
        }
        else if (_activePointerId != pointerData.pointerId)
        {
            // UI 里可能切到另一个指针事件（或上次 PointerDown 丢失），强制重建起点
            BeginInteraction(pointerData);
        }

        // 计算竖直方向位移（屏幕坐标系 Y 轴向上为正）
        float deltaY = pointerData.position.y - _dragStartPointerPos.y;
        // 反转控制：上划减少角度，下划增加角度
        // selector 也需要受 Min/Max 限制，避免可旋转一整圈。
        float targetAngle = Mathf.Clamp(_dragStartAngle - deltaY * Sensitivity, MinAngle, MaxAngle);

        if (_rotationOnly)
        {
            _currentAngle = targetAngle;
            _hasCurrentAngle = true;
            UpdateValueText(); // selector 可能不显示文字；这里保持一致性
            UpdateVisualsByAngle(_currentAngle);
            return;
        }

        // 角度 -> 数值（保持 7 点钟 240° 为最小值，11 点钟 120° 为最大值）
        int currentMin = _ctrl.GetMinValue();
        int currentMax = _ctrl.GetMaxValue();
        float t = Mathf.InverseLerp(MaxAngle, MinAngle, targetAngle);
        int newValue = Mathf.RoundToInt(Mathf.Lerp(currentMin, currentMax, t));

        // 更新逻辑数值与显示
        _ctrl.OnValueChanged(newValue);
        UpdateValueText();
        UpdateVisualsByAngle(targetAngle);

        _currentAngle = targetAngle;
        _hasCurrentAngle = true;
        ValueChanged?.Invoke(_ctrl, _ctrl.GetValue());
    }

    public void SetCtrl(ICameraModeCtrl ctrl)
    {
        if (_ctrl != null)
        {
            _ctrl.OnRelease();
        }
        _ctrl = ctrl;
        if (_ctrl != null)
        {
            _ctrl.OnTrigger();
            
            // Sync local min/max for inspector/fallback, though logic uses ctrl directly now
            MinValue = _ctrl.GetMinValue();
            MaxValue = _ctrl.GetMaxValue();
            
            UpdateVisuals();
            CtrlChanged?.Invoke(_ctrl);
            ValueChanged?.Invoke(_ctrl, _ctrl.GetValue());
        }
    }

    /// <summary>
    /// 外部数据变化后调用：将当前 ctrl 的值重新同步到旋钮角度和文字显示。
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (_ctrl == null) return;

        UpdateValueText();

        int val = _ctrl.GetValue();
        int currentMin = _ctrl.GetMinValue();
        int currentMax = _ctrl.GetMaxValue();

        // 反向计算：数值 -> 角度
        float t = Mathf.InverseLerp(currentMin, currentMax, val);
        // 方向保持一致：Min(7点/240度) -> Max(11点/120度)
        // t=0 (Min) -> 240
        // t=1 (Max) -> 120
        float angle = Mathf.Lerp(MaxAngle, MinAngle, t);

        UpdateVisualsByAngle(angle);
        _currentAngle = angle;
        _hasCurrentAngle = true;
    }

    private void UpdateValueText()
    {
        if (value_Text == null || _ctrl == null) return;
        if (_ctrl is ICameraModeCtrlValueText valueTextCtrl)
        {
            value_Text.text = valueTextCtrl.GetValueText();
        }
        else
        {
            value_Text.text = _ctrl.GetValue().ToString();
        }
    }

    private void BeginInteraction(PointerEventData pointerData)
    {
        _isDragging = true;
        _activePointerId = pointerData.pointerId;
        _dragStartPointerPos = pointerData.position;
        if (_rotationOnly)
        {
            // rotationOnly（selector）模式下，拖拽起点应以“当前可见角度”为准，
            // 防止外部旋转后内部缓存未同步导致首次微拖跳变。
            _dragStartAngle = ResolveVisibleAngleAsCurrent();
            _currentAngle = _dragStartAngle;
            _hasCurrentAngle = true;
            _dragStartValue = 0;
        }
        else
        {
            _dragStartValue = _ctrl != null ? _ctrl.GetValue() : 0;
            _dragStartAngle = ValueToAngle(_dragStartValue);
        }
    }

    private void EndInteraction()
    {
        _isDragging = false;
        _activePointerId = int.MinValue;
    }

    private float ValueToAngle(int value)
    {
        int currentMin = _ctrl != null ? _ctrl.GetMinValue() : MinValue;
        int currentMax = _ctrl != null ? _ctrl.GetMaxValue() : MaxValue;
        float t = Mathf.InverseLerp(currentMin, currentMax, value);
        return Mathf.Lerp(MaxAngle, MinAngle, t);
    }

    private void UpdateVisualsByAngle(float angle)
    {
        // 应用偏移量，修正素材方向与数学坐标系的偏差
        float displayAngle = angle + AngleOffset;

        // 旋转 btn_Root
        if (btn_Root != null)
        {
            btn_Root.localRotation = Quaternion.Euler(0, 0, displayAngle);
        }

        // 旋转 pointer_Root
        if (pointer_Root != null)
        {
            pointer_Root.localRotation = Quaternion.Euler(0, 0, displayAngle);
        }

        // 旋转 value_Root
        if (value_Root != null)
        {
            value_Root.localRotation = Quaternion.Euler(0, 0, displayAngle);
            
            // 反向旋转文字本身，使其始终保持水平（平行）
            // 只需要抵消掉父级的旋转即可
            if (value_Text != null)
            {
                value_Text.transform.localRotation = Quaternion.Euler(0, 0, -displayAngle);
            }
        }
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    private float ResolveVisibleAngleAsCurrent()
    {
        if (btn_Root != null)
        {
            float displayAngle = btn_Root.localEulerAngles.z;
            return NormalizeAngle(displayAngle - AngleOffset);
        }
        return _hasCurrentAngle ? NormalizeAngle(_currentAngle) : 0f;
    }

    private static float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}
