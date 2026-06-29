using System;
using System.Collections;
using System.Collections.Generic;
using Game.CommunityGame;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ShopTagItem : MonoBehaviour
{
    public Text Txt_Title;
    public Text Txt_Default;
    public Toggle Tog;
    public string SectionId => _curData?.sectionId;
    private Action<string> _onSelect;
    private SectionItemData _curData;

    public void InitItem(SectionItemData data, int index, Action<string> act)
    {
        _curData = data;
        Txt_Title.text = data.sectionName;
        Txt_Default.text = data.sectionName;
        this._onSelect = act;
        Tog.onValueChanged.RemoveAllListeners();
        Tog.isOn = false;
        Tog.onValueChanged.AddListener(SetSelectState);
    }

    public void SetSelectState(bool isSelect)
    {
        if (isSelect)
        {
            _onSelect?.Invoke(this._curData.sectionId);
        }
    }
}
