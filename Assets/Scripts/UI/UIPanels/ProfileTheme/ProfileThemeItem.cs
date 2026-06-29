using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ProfileThemeItem : MonoBehaviour
{
    [SerializeField] private CButton itemBtn;
    [SerializeField] private Image iconImg;
    [SerializeField] private GameObject selectBg;
    [SerializeField] private Text nameText;

    private int curThemeId = 0;
    private Action<int> itemClicListener;
    void Awake()
    {
        itemBtn.onClick.AddListener(OnItemClick);
    }

    public void Init(int themeId,Action<int> callback)
    {
        curThemeId = themeId;
        itemClicListener = callback;
        iconImg.sprite = ProfileThemeManager.Inst.LoadThemeIcon(themeId,gameObject);

        var config = ProfileThemeManager.Inst.GetThemeInfo(themeId);
        if (config != null)
        {
            nameText.SetLocalText(config.themeName);
        }

        if (curThemeId >= 14)
        {
            (iconImg.transform as RectTransform).sizeDelta = new Vector2(180,180);
        }
    }

    private void OnItemClick()
    {
        itemClicListener?.Invoke(curThemeId);
        SetSelect(true);
    }

    public void SetSelect(bool isSelect)
    {
        selectBg.SetActive(isSelect);
    }
}
