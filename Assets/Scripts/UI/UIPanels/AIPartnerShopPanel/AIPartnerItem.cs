using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AIPartnerItem : MonoBehaviour
{
    [SerializeField] private CButton ClickBtn;
    [SerializeField] private RemoteImageBehaviour CoverImage;
    [SerializeField] private Text Name;
    [SerializeField] private GameObject SelectedIndicator;


    public void SetData(CabinCharacterUgcInfo data, Action<CabinCharacterUgcInfo> action, int idx, bool isSelected = false)
    {
        CoverImage.Load(data?.cover, true, null);
        Name.text = data.name;
        SelectedIndicator.SetActive(isSelected);
        ClickBtn.onClick.RemoveAllListeners();
        ClickBtn.onClick.AddListener(() =>
        {
            action?.Invoke(data);
        });
    }

}
