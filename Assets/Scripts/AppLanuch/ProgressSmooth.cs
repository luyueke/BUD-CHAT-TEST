using System;
using UnityEngine;
using UnityEngine.UI;

public class ProgressSmooth : MonoBehaviour
{
    public Slider progressSlider;
    
    /// <summary>
    /// 目标进度值。
    /// </summary>
    private float _targetValue = 0;
    /// <summary>
    /// 每针速度
    /// </summary>
    private float _stepSpeed = 0;

    /// <summary>
    /// 当前的进度比值
    /// </summary>
    public float currentProgress
    {
        get
        {
            return progressSlider.value / progressSlider.maxValue;
        }
    }

    private float currentValue = 0;

    public Action<float> progressDidChangeAction;

    /// <summary>
    /// 设置目标进度数据， 需要进度后退时，keyFrameStep填写负值
    /// </summary>
    /// <param name="targetValue">目标值</param>
    /// <param name="keyFrameStep">每帧新增步长</param>
    public void SetValue(float targetValue, float frameSpeed = 1f)
    {
        if (progressSlider == null)
        {
            return;
        }

        if (targetValue > progressSlider.maxValue || targetValue < progressSlider.minValue)
        {
            return;
        }
        _targetValue = targetValue;
        _stepSpeed = frameSpeed;
    }
    
    public void ResetFlag()
    {
        currentValue = 0;
        _stepSpeed = 0;
        _targetValue = 0;
        progressSlider.value = 0;
    }
    
    private void Awake()
    {
        ResetFlag();
    }
    
    private void Update()
    {
        if (_stepSpeed > 0 && progressSlider.value > _targetValue)
        {
            return;
        }
        if (_stepSpeed < 0 && progressSlider.value < _targetValue)
        {
            return;
        }
        
        currentValue += _stepSpeed * Time.deltaTime;
        if (currentValue >= progressSlider.maxValue)
        {
            currentValue = progressSlider.maxValue;
         
        }

        if (currentValue <= progressSlider.minValue)
        {
            currentValue = progressSlider.minValue;
        }

        progressSlider.value = currentValue;
        progressDidChangeAction?.Invoke(currentValue);
    }
}
