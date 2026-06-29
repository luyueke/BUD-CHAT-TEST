using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AnniversaryTabItem : MonoBehaviour
{
    public CButton button;
    public Text title;
    public GameObject redDot;
    public Image icon;
    [SerializeField]
    private GameObject selectedObj;
    public string actId = "";
    private string atlasPath = "Assets/Loadable/UI/UIPanel/AnniversaryPanel/Icons.spriteatlas";
    private Action<int> clickAction;
    private string _selectedColor = "#FFD400";
    private string _normalColor = "#FFFFFF";
    public Es.ActivityConfig _info;

    public  void Init(Es.ActivityConfig info, Action<int> onClick)
    {
        button.onClick.AddListener(RootClick);
        _info = info;
        clickAction = onClick;
        title.SetText(info.title);
        icon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, info.iconName, icon.gameObject);
    }
    private void RootClick()
    {
        clickAction.Invoke(_info.id);
    }
    public void SetSelect(bool isSel)
    {
        
         RectTransform rectTransform = GetComponent<RectTransform>();
        if (!isSel)
        {
            float currentWidth = rectTransform.sizeDelta.x;
            rectTransform.sizeDelta = new Vector2(currentWidth, 60);
        }
        else
        {
            float currentWidth = rectTransform.sizeDelta.x;
            rectTransform.sizeDelta = new Vector2(currentWidth, 99.3f);
        }
        // 只修改宽度，保持原有高度

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
