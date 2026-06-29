using System;
using UnityEngine;

/// <summary>
/// 相机模式参数控制器集合：
/// - 提供 8 个类似 CameraModeSpannerCtrl 的 ICameraModeCtrl 实现类
/// - 带小数的参数使用“缩放后的 int”来兼容现有 Spanner（并通过 ICameraModeCtrlValueText 显示为小数）
/// </summary>
public enum CameraModeEffectParam
{
    Bloom,       // 柔光
    Focus,       // 对焦
    DoF,         // 景深
    Contrast,    // 对比度
    Saturation,  // 饱和度
    Exposure,    // 曝光
    Tilt,        // 倾斜
    FOV          // 广角
}

public sealed class CameraModeEffectCtrls
{
    public CameraModeBloomCtrl Bloom { get; } = new CameraModeBloomCtrl();
    public CameraModeFocusCtrl Focus { get; } = new CameraModeFocusCtrl();
    public CameraModeDoFCtrl DoF { get; } = new CameraModeDoFCtrl();
    public CameraModeContrastCtrl Contrast { get; } = new CameraModeContrastCtrl();
    public CameraModeSaturationCtrl Saturation { get; } = new CameraModeSaturationCtrl();
    public CameraModeExposureCtrl Exposure { get; } = new CameraModeExposureCtrl();
    public CameraModeTiltCtrl Tilt { get; } = new CameraModeTiltCtrl();
    public CameraModeFovCtrl FOV { get; } = new CameraModeFovCtrl();

    public ICameraModeCtrl Get(CameraModeEffectParam param)
    {
        return param switch
        {
            CameraModeEffectParam.Bloom => Bloom,
            CameraModeEffectParam.Focus => Focus,
            CameraModeEffectParam.DoF => DoF,
            CameraModeEffectParam.Contrast => Contrast,
            CameraModeEffectParam.Saturation => Saturation,
            CameraModeEffectParam.Exposure => Exposure,
            CameraModeEffectParam.Tilt => Tilt,
            CameraModeEffectParam.FOV => FOV,
            _ => null
        };
    }
}

/// <summary>
/// 通用区间控制器：存 int，支持默认值 Reset。
/// </summary>
public abstract class CameraModeRangedIntCtrl : ICameraModeCtrl
{
    private int _value;

    protected CameraModeRangedIntCtrl(int min, int max, int defaultValue)
    {
        Min = min;
        Max = max;
        DefaultValue = defaultValue;
        _value = defaultValue;
    }

    protected int Min { get; }
    protected int Max { get; }
    protected int DefaultValue { get; }

    public virtual void OnTrigger() { }

    public virtual void OnValueChanged(int value)
    {
        _value = Mathf.Clamp(value, Min, Max);
    }

    public virtual void OnRelease() { }

    public virtual void OnReset()
    {
        _value = DefaultValue;
    }

    public int GetValue() => _value;
    public int GetMaxValue() => Max;
    public int GetMinValue() => Min;
}

/// <summary>
/// 通用“缩放浮点”控制器：
/// - 通过 scale 把 float 映射到 int，以兼容 Spanner 的 int 接口
/// - 显示时按 decimals 输出（例如 1.0）
/// </summary>
public abstract class CameraModeScaledFloatCtrl : CameraModeRangedIntCtrl, ICameraModeCtrlValueText
{
    private readonly int _scale;
    private readonly int _decimals;

    protected CameraModeScaledFloatCtrl(float min, float max, float defaultValue, int scale, int decimals)
        : base(
            min: Mathf.RoundToInt(min * scale),
            max: Mathf.RoundToInt(max * scale),
            defaultValue: Mathf.RoundToInt(defaultValue * scale)
        )
    {
        _scale = Mathf.Max(1, scale);
        _decimals = Mathf.Max(0, decimals);
    }

    public float GetFloatValue() => GetValue() / (float)_scale;

    public string GetValueText()
    {
        // 固定小数位，确保 1.0 这种显示
        return GetFloatValue().ToString("F" + _decimals);
    }
}

// ===== 8 个参数控制器实现 =====

/// <summary>柔光：0.0 ~ 5.0，默认 0</summary>
public sealed class CameraModeBloomCtrl : CameraModeScaledFloatCtrl
{
    public CameraModeBloomCtrl() : base(min: 0.0f, max: 5.0f, defaultValue: 0.0f, scale: 10, decimals: 1) { }
}

/// <summary>对焦：0.0 ~ 20.0，默认 20</summary>
public sealed class CameraModeFocusCtrl : CameraModeScaledFloatCtrl
{
    public CameraModeFocusCtrl() : base(min: 0.0f, max: 20.0f, defaultValue: 20.0f, scale: 10, decimals: 1) { }
}

/// <summary>景深：0.0 ~ 1.0，默认 0</summary>
public sealed class CameraModeDoFCtrl : CameraModeScaledFloatCtrl
{
    public CameraModeDoFCtrl() : base(min: 0.0f, max: 1.0f, defaultValue: 0.0f, scale: 10, decimals: 1) { }
}

/// <summary>对比度：0.0 ~ 3.0，默认 1</summary>
public sealed class CameraModeContrastCtrl : CameraModeScaledFloatCtrl
{
    public CameraModeContrastCtrl() : base(min: 0.0f, max: 3.0f, defaultValue: 1.0f, scale: 10, decimals: 1) { }
}

/// <summary>饱和度：0.0 ~ 3.0，默认 1</summary>
public sealed class CameraModeSaturationCtrl : CameraModeScaledFloatCtrl
{
    public CameraModeSaturationCtrl() : base(min: 0.0f, max: 3.0f, defaultValue: 1.0f, scale: 10, decimals: 1) { }
}

/// <summary>曝光：0.0 ~ 2.0，默认 1</summary>
public sealed class CameraModeExposureCtrl : CameraModeScaledFloatCtrl
{
    public CameraModeExposureCtrl() : base(min: 0.0f, max: 2.0f, defaultValue: 1.0f, scale: 10, decimals: 1) { }
}

/// <summary>倾斜：-180.0 ~ 180.0，默认 0</summary>
public sealed class CameraModeTiltCtrl : CameraModeRangedIntCtrl
{
    public CameraModeTiltCtrl() : base(min: -180, max: 180, defaultValue: 0) { }
}

/// <summary>广角：0.0 ~ 100.0，默认 50</summary>
public sealed class CameraModeFovCtrl : CameraModeScaledFloatCtrl
{
    public CameraModeFovCtrl() : base(min: 0.0f, max: 100.0f, defaultValue: 50.0f, scale: 10, decimals: 1) { }
}

