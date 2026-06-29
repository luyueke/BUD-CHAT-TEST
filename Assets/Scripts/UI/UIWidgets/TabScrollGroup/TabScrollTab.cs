using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabScrollTab : MonoBehaviour
{
    public int groupId;

    public LogicToggle logicToggle;

    protected Action<bool> onToggleUI;

    protected virtual bool OverrideActiveCheck => false;
    public bool IsElementActive => OverrideActiveCheck ? GetIsElementActive() : gameObject.activeSelf;

    protected virtual void Start()
    {
        logicToggle.OnValueChangedUI.AddListener(OnToggleUIInternal);
    }

    public void AddOnToggleUI(Action<bool> act)
    {
        onToggleUI += act;
    }

    protected void OnToggleUIInternal(bool isOn)
    {
        OnToggleUIChanged(isOn);
        onToggleUI?.Invoke(isOn);
    }

    protected virtual void OnToggleUIChanged(bool isOn) { }

    protected virtual bool GetIsElementActive()
    {
        return true;
    }
}
