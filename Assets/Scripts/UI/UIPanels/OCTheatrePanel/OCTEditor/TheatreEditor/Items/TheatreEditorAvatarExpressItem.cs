using System;
using Com.TheFallenGames.OSA.Util.IO;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorAvatarExpressItem : MonoBehaviour
{
    [SerializeField] private Button selectBtn;
    [SerializeField] private GameObject selectedObj;
    [SerializeField] private RemoteImageBehaviour avatarImg;
    [SerializeField] private Text nameText;

    public string AvatarId { get; private set; }
    public string ExpressType { get; private set; }

    private Action<string, string> onSelected;

    public void Init(string avatarId, string expressType, string displayName, string expressUrl, string avatarName,
        Action<string, string> onSelect)
    {
        AvatarId = avatarId;
        ExpressType = expressType;
        onSelected = onSelect;

        if (string.IsNullOrEmpty(expressUrl))
        {
            gameObject.SetActive(false);
            return;
        }

        if (avatarImg != null)
            avatarImg.Load(expressUrl);
        if (nameText != null)
            nameText.text = displayName;

        selectedObj?.SetActive(false);
        selectBtn?.onClick.RemoveAllListeners();
        selectBtn?.onClick.AddListener(() => onSelected?.Invoke(AvatarId, ExpressType));
    }

    public void SetSelected(bool selected)
    {
        selectedObj?.SetActive(selected);
    }
}
