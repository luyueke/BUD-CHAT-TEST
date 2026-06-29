using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Author : Tee Li
/// 描述：用Toggle选择多种类型的面板Item
/// 日期：2022/10/10
/// </summary>

public class ToggleGroupItem : MonoBehaviour
{
    public GameObject toggleParent;
    public List<Toggle> Toggles { get; private set; }

    private void Awake()
    {
        InitOnAwake();
    }

    public virtual void InitOnAwake()
    {
        Toggles?.Clear();
        Toggles = new List<Toggle>(toggleParent.GetComponentsInChildren<Toggle>());
    }
    
    public void AddListener(string name, UnityAction<bool> onChange)
    {
        Toggle tgl = FindWithGoName(name);
        tgl?.onValueChanged.AddListener(onChange);
    }

    public void SetValue(string name, bool isOn)
    {
        Toggle tgl = FindWithGoName(name);
        if (tgl)
        {
            tgl.isOn = isOn;
            tgl.GetComponent<TabScrollColorTab>().UpdateColor(isOn);
        }
    }

    public void SetValueWithoutNotify(string name, bool isOn)
    {
        Toggle tgl = FindWithGoName(name);
        if (tgl != null)
        {
            tgl.SetIsOnWithoutNotify(isOn);
            if (tgl.TryGetComponent<UToggleHelper>(out var toggleHelper))
            {
                toggleHelper.SetIsOnWithoutNotify(isOn);
            }
        }
    
    }

    public Toggle FindWithGoName(string name)
    {
        return Toggles.Find(t => t.gameObject.name == name);
    }

    public Toggle GetCurrentActive()
    {
        return Toggles.Find(t => t.isOn);
    }
}
