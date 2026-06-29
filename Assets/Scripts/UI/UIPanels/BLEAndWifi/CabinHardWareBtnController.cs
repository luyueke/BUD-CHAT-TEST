using System;
using UnityEngine;

/// <summary>
/// BOX Android 硬件的按钮处理事件
/// </summary>
public class CabinHardWareBtnController
{
    /// <summary>单击事件（两次按下间隔超过 0.3s 时，第一次判定为单独点击）</summary>
    public event Action OnSingleClick;

    /// <summary>双击事件（两次按下间隔不超过 0.3s 时判定）</summary>
    public event Action OnDoubleClick;

    private const float DoubleClickInterval = 0.3f;
    private float _lastClickTime = float.MinValue;
    private bool _pendingSingle = false;

    /// <summary>
    /// 收到一次物理按钮按下。
    /// 两次按下间隔 ≤ 0.3s → 双击；超过 0.3s → 第一次为单独点击。
    /// </summary>
    public void OnClickBtnDown()
    {
        float now = Time.time;

        if (_pendingSingle && (now - _lastClickTime) <= DoubleClickInterval)
        {
            // 第二次按下且在间隔内：触发双击，取消待确认的单击
            _pendingSingle = false;
            _lastClickTime = float.MinValue;
            OnDoubleClick?.Invoke();
        }
        else
        {
            // 第一次按下（或上次已超时）：记录时间，等待第二次
            _pendingSingle = true;
            _lastClickTime = now;
        }
    }

    /// <summary>
    /// 每帧调用，用于确认超时单击。
    /// 需要由持有者在 Update 中驱动。
    /// </summary>
    public void Tick()
    {
        if (_pendingSingle && (Time.time - _lastClickTime) > DoubleClickInterval)
        {
            _pendingSingle = false;
            OnSingleClick?.Invoke();
        }
    }

    public void OnRotateBtn(int pre, int cur) { }
}
