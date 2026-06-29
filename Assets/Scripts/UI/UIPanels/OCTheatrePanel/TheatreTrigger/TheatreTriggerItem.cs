using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TheatreTriggerItem : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private GameObject selectedMark;
    [SerializeField] private CButton selectBtn;
    public RemoteImageBehaviour iconImage;

    public void InitTheatre(DraftListItem data, Action<DraftListItem> onSelect, bool isSelected)
    {
        nameText.text = data?.theatreInfo?.name ?? "";
        SetSelected(isSelected);
        selectBtn.onClick.RemoveAllListeners();
        selectBtn.onClick.AddListener(() => onSelect?.Invoke(data));
    }

    public void InitActor(DraftListItem data, Action<DraftListItem> onSelect, bool isSelected)
    {
        nameText.text = data?.actorInfo?.name ?? "";
        SetSelected(isSelected);
        selectBtn.onClick.RemoveAllListeners();
        selectBtn.onClick.AddListener(() => onSelect?.Invoke(data));
    }

    public void InitClothes(OTCAvatarClothes data, Action<OTCAvatarClothes> onSelect, bool isSelected)
    {
        nameText.text = data?.clothesName ?? "";
        SetSelected(isSelected);
        selectBtn.onClick.RemoveAllListeners();
        selectBtn.onClick.AddListener(() => onSelect?.Invoke(data));
    }

    public void SetSelected(bool selected)
    {
        selectedMark.SetActive(selected);
    }
}
