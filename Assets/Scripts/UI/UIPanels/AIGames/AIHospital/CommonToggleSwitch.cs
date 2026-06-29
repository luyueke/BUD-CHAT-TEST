using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommonToggleSwitch : MonoBehaviour
{
    //[Header("选项")]
    //[SerializeField] private Toggle tog;
    [Header("文本")]
    [SerializeField] private Text _label;

    [Header("常规颜色")]
    [SerializeField] private string _normalColor = "";
    [Header("选中颜色")]
    [SerializeField] private string _selectColor = "";
    [Header("常规字号")]
    [SerializeField] private int _normalSize = 30;
    [Header("选中字号")]
    [SerializeField] private int _selectSize = 40;
    [Header("选中物体")]
    [SerializeField] private GameObject _selectObj;
    [Header("非选中物体")]
    [SerializeField] private GameObject _unSelectObj;
    public void SetSelectState(bool value)
    {
        if(_label != null) _label.fontSize = value ? _selectSize : _normalSize;
        SetHexColor(value?_selectColor:_normalColor);

        if (_selectObj != null)
        {
            _selectObj.gameObject.SetActive(value);
        }

        if (_unSelectObj != null)
        {
            _unSelectObj.gameObject.SetActive(!value);
        }
    }

    private void SetHexColor(string hexColor)
    {
        if (!hexColor.StartsWith("#"))
            hexColor = "#" + hexColor;

        if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
        {
            if (_label != null) _label.color = color;
        }
    }
}
