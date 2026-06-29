using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 可以自定义Toggle点亮后的ui行为 以及可以拆分Toggle的ui逻辑和业务逻辑
/// Author : Tee Li
/// </summary>
public class LogicToggle : Toggle
{
    public UnityEvent<bool> OnValueChangedLogic { get; private set; } = new UnityEvent<bool>();
    public UnityEvent<bool> OnValueChangedUI { get; private set; } = new UnityEvent<bool>();

    protected override void Awake()
    {
        onValueChanged.AddListener(OnChangedInternal);
    }

    public void SetIsOnNoLogic(bool isOn)
    {
        if (isOn && group && !group.allowSwitchOff) 
        {
            foreach(Toggle t in group.ActiveToggles())
            {
                LogicToggle tgl = t as LogicToggle;
                if(tgl && t.gameObject != gameObject)
                {
                    tgl.OnValueChangedUI?.Invoke(false);
                }
            }
        }
        SetIsOnWithoutNotify(isOn);
        
        OnValueChangedUI?.Invoke(isOn);
    }

    protected void OnChangedInternal(bool isOn)
    {
        OnValueChangedUI?.Invoke(isOn);
        OnValueChangedLogic?.Invoke(isOn);
    }
}
