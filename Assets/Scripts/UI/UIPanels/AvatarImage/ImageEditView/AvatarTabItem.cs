
using System;
using Game.Avatar;
using GameData.PgcData;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AvatarTabItem : MonoBehaviour
{
    public Text TitleLabel;
    public GameObject UnderLine;

    private Action<AvatarSubType> ClickAction;

    private AvatarSubType itemId;
    private void Awake()
    {
        transform.GetComponent<CButton>()?.onClick.AddListener(OnClickTab);
    }

    public void SetData(string title, AvatarSubType id, Action<AvatarSubType> clickAction)
    {
        itemId = id;
        TitleLabel.text = title;
        ClickAction = clickAction;
    }

    public void UpdateSelected(bool isSelected)
    {
        UnderLine.SetActive(isSelected);
        string hex = isSelected ? "#505050" : "#D2D2D2";
        TitleLabel.color =  DataUtil.DeSerializeColorByHex(hex);
    }

    private void OnClickTab()
    {
        ClickAction?.Invoke(itemId);
    }
}
