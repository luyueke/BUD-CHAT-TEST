using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ActivityTabItem : MonoBehaviour
{
    public CButton button;
    public Text title;
    public GameObject redDot;
    [SerializeField]
    private GameObject selectedObj;
    public string actId = "";
    private string atlasPath = "Assets/Loadable/UI/UIPanel/AnniversaryCelebrationPanel/tabIcons.spriteatlas";
    private Action<string> clickAction;
    private string _selectedColor = "#FFD400";
    private string _normalColor = "#FFFFFF";


    public  void Init(ActivityInfo info,Action<string> onClick)
    {
        button.onClick.AddListener(RootClick);
        actId = info.activityId;
        clickAction = onClick;
        title.SetText(info.activityTital);
    }
    private void RootClick()
    {
        clickAction.Invoke(actId);
    }
    public void SetSelect(bool isSel)
    {
        var txtColor = isSel ? _selectedColor : _normalColor;
        title.color = DataUtil.DeSerializeColorCheckHash(txtColor);
        selectedObj.SetActive(isSel);
    }

    public void UpdateRedDot(bool show)
    {
        if (redDot != null)
        {
            redDot.gameObject.SetActive(show);
        }
    }
}
