using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorEmoteAvatarItem : MonoBehaviour
{
    [SerializeField] private Button selectBtn;
    [SerializeField] private GameObject selectedObj;
    [SerializeField] private RemoteImageBehaviour avatarImg;
    [SerializeField] private Text nameText;

    private OTCAvatarClothes _data;
    private Action<OTCAvatarClothes> _onSelect;

    private void Awake()
    {
        selectBtn?.onClick.AddListener(OnClick);
    }

    public void Init(OTCAvatarClothes clothes, bool isSelected, Action<OTCAvatarClothes> onSelect)
    {
        _data = clothes;
        _onSelect = onSelect;

        if (nameText != null) nameText.text = clothes?.clothesName ?? "";
        if (avatarImg != null && !string.IsNullOrEmpty(clothes?.clothesURL))
            avatarImg.Load(clothes.clothesURL);

        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedObj != null) selectedObj.SetActive(isSelected);
    }

    private void OnClick()
    {
        _onSelect?.Invoke(_data);
    }
}
