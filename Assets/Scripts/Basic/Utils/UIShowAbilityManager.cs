using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UIAbility
{
   GuestEmoteBtn,
   GuestJumpBtn,
   GuestBackToSpawnBtn,
   GuestChangeOcBtn,
   GuestInstrumentBtn,
   EnterSelfieBtn,
   GuestVehicleControlBtn,
   GuestUgcVehicleBtn,
   GuestCameraModeBtn,
}

public enum AbilityKey
{
    LinkEmote,
    CameraMode,
    VehicleMode,
}

public class UIShowAbilityManager : GlobalInstance<UIShowAbilityManager>
{
    //禁用列表
    private Dictionary<UIAbility, List<string>> noAbilityDict = new Dictionary<UIAbility, List<string>>();

    private Action<UIAbility, bool> banBilityChange;//监听某个UI行为现在是否可见

    public void AddBanBility(UIAbility ability, AbilityKey callKey)
    {
        AddBanBility(ability,callKey.ToString());
    }

    public void AddBanBility(UIAbility ability,string callKey)
    {
        if (!noAbilityDict.ContainsKey(ability))
        {
            noAbilityDict.Add(ability,new List<string>());
        }

        if (!noAbilityDict[ability].Contains(callKey))
        {
            if (noAbilityDict[ability].Count <= 0)
            {
                banBilityChange?.Invoke(ability,true);
            }
            noAbilityDict[ability].Add(callKey);
        }
    }
    
    public void RemoveBanBility(UIAbility ability, AbilityKey callKey)
    {
        RemoveBanBility(ability,callKey.ToString());
    }

    public void RemoveBanBility(UIAbility ability, string callKey)
    {
        if (noAbilityDict.ContainsKey(ability))
        {
            noAbilityDict[ability].Remove(callKey);

            if (noAbilityDict[ability].Count <= 0)
            {
                banBilityChange?.Invoke(ability,false);
            }
        }
    }

    public bool GetBanBility(UIAbility ability)
    {
        return noAbilityDict.ContainsKey(ability) && noAbilityDict[ability].Count > 0;
    }

    public void ClearBanBility(UIAbility ability)
    {
        noAbilityDict.Remove(ability);
        banBilityChange?.Invoke(ability,false);
    }

    public void AddBanChangeListener(Action<UIAbility, bool> callback)
    {
        banBilityChange += callback;
    }
    
    public void RemoveBanChangeListener(Action<UIAbility, bool> callback)
    {
        banBilityChange -= callback;
    }

    public void ClearBanChangeListener()
    {
        banBilityChange = null;
    }

    public void ClearGameGuest()
    {
        ClearBanBility(UIAbility.GuestEmoteBtn);
        ClearBanBility(UIAbility.GuestJumpBtn);
        ClearBanBility(UIAbility.GuestBackToSpawnBtn);
        ClearBanBility(UIAbility.GuestChangeOcBtn);
        ClearBanBility(UIAbility.GuestInstrumentBtn);
    }
}
