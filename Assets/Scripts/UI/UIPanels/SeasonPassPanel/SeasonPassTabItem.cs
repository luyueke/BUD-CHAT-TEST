using System;
using Es;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class SeasonPassTabItem : MonoBehaviour
{
    public CButton button;
    public Text title;
    public GameObject redDot;
    public GameObject selectedObj;

    public SeasonPassType _curSeasonPassType = SeasonPassType.ErrSeasonPassType;
    private Action<SeasonPassType> clickAction;
    private string selectedColor = "#FFD400";
    private string normalColor = "#FFFFFF";


    public void Init(SeasonPassType seasonPassType, string strTitle, Action<SeasonPassType> onClick)
    {
        button.onClick.AddListener(RootClick);
        _curSeasonPassType = seasonPassType;
        clickAction = onClick;
        title.SetText(strTitle);
    }

    private void RootClick()
    {
        clickAction.Invoke(_curSeasonPassType);
    }

    public void SetSelect(bool isSel)
    {
        var txtColor = isSel ? selectedColor : normalColor;
        title.color = DataUtil.DeSerializeColorCheckHash(txtColor);
        selectedObj.SetActive(isSel);
    }
    public void SetRedDot(bool isRed)
    {
        redDot.SetActive(isRed);
    }
}


