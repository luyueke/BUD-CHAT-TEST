using Com.TheFallenGames.OSA.Util.IO;
using Game.Audio;
using Game.Database;
using Game.Store;
using GameData.BaseInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreviewBundleItem : MonoBehaviour
{
    [SerializeField] Toggle button;
    [SerializeField] RemoteImageBehaviour remoteImageBehaviour;
    [SerializeField] GameObject ownedRoot;
    [SerializeField] Image colorImage;
    [SerializeField] Image colorImage1;
    [SerializeField] Color selectedColor;

    private SkinInfo mData;

    private Action<SkinInfo> isOnAction;
    public void Awake()
    {
        button.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
                isOnAction?.Invoke(mData);
                colorImage.color = selectedColor;
                colorImage1.color = selectedColor;
            }
            else
            {
                colorImage.color = Color.white;
                colorImage1.color = Color.white;
            }
        });
    }

    public void SetData(SkinInfo info, ToggleGroup toggleGroup, Action<SkinInfo> action)
    {
        if (info == null) return;
        button.group = toggleGroup;
        isOnAction = action;
        mData = info;
        remoteImageBehaviour.Load(info.cover);
        var inventoryData = BagDatabase.Inst.Select(info.id);
        ownedRoot.SetActive(inventoryData != null && inventoryData.OwnedNum > 0);
    }

    public void DefaultOn()
    {
        button.SetIsOnWithoutNotify(true);
        isOnAction?.Invoke(mData);
        colorImage.color = selectedColor;
        colorImage1.color = selectedColor;
    }
}
