using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using UnityEngine;
using UnityEngine.Events;

public class LogicToggleGroupItem : ToggleGroupItem
{
    public void AddListenerLogic(string name, UnityAction<bool> act)
    {
        LogicToggle tgl = FindWithGoName(name) as LogicToggle;
        if (tgl)
        {
            tgl.OnValueChangedLogic.AddListener(act);
        }
    }
    
    public void ClearListenerLogicAll()
    {
        for(int i = 0; i < Toggles.Count; ++i)
        {
            LogicToggle tgl = Toggles[i] as LogicToggle;
            if (tgl)
            {
                tgl.OnValueChangedLogic.RemoveAllListeners();
            }
        }
    }

    public void AddListenerLogicAll(UnityAction<string, bool> act)
    {
        for(int i = 0; i < Toggles.Count; ++i)
        {
            LogicToggle tgl = Toggles[i] as LogicToggle;
            if (tgl)
            {
                tgl.OnValueChangedLogic.AddListener((isOn) => act?.Invoke(tgl.gameObject.name, isOn));
            }
        }
    }

    public void AddListenerUI(string name, UnityAction<bool> act)
    {
        LogicToggle tgl = FindWithGoName(name) as LogicToggle;
        if (tgl)
        {
            tgl.OnValueChangedUI.AddListener(act);
        }
    }

    public void AddListenerUIAll(UnityAction<string, bool> act)
    {
        for (int i = 0; i < Toggles.Count; ++i)
        {
            LogicToggle tgl = Toggles[i] as LogicToggle;
            if (tgl)
            {
                tgl.OnValueChangedUI.AddListener((isOn) => act?.Invoke(tgl.gameObject.name, isOn));
            }
        }
    }

    public void SetIsOnNoLogic(string name, bool isOn)
    {
        LogicToggle tgl = FindWithGoName(name) as LogicToggle;
        if (tgl)
        {
            tgl.SetIsOnNoLogic(isOn);
        }
    }

    public void SetToggleReddot(string name, bool hasReddot)
    {
        LogicToggle tgl = FindWithGoName(name) as LogicToggle;
        if (tgl)
        {
            var go_Reddot = GameUtils.FindChildByName(tgl.transform, "Go_Reddot").gameObject;
            go_Reddot?.SetActive(hasReddot);
        }
    }
}
