using System;
using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using UnityEngine;
using UnityEngine.UI;

public class VehiclePoseSource : MonoBehaviour
{
    public enum VehicleSource
    {
        Free,
        Create,
        Buy
    }

    [SerializeField] List<Toggle> sourceToggles;

    private Action<VehicleSource> onValueChanged;

    private void Awake()
    {
        for (int i = 0, C = sourceToggles.Count; i < C; i++)
        {
            var sourceToggle = sourceToggles[i];
            VehicleSource source = (VehicleSource)i;
            sourceToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn) onValueChanged?.Invoke(source);
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
            });
        }
    }

    public void SetCallback(Action<VehicleSource> action)
    {
        onValueChanged = action;
    }

    public void DefualtOn(VehicleSource source)
    {
        if (sourceToggles[(int)source].isOn == true)
        {
            onValueChanged?.Invoke(source);
        }
        else
        {
            sourceToggles[(int)source].isOn = true;
        }

    }

}
