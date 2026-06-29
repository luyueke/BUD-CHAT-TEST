using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class SelectSyllableItem : MonoBehaviour
{
    private Image _image;
    private CButton _button;
    private int _syllableId;

    public int Id
    {
        get
        {
            return _syllableId;
        }
    }
    private Color _canSelectColor;
    public enum Status
    {
        canSelect,
        canNotSelect,
        Selected,
    }
    public void Init(Action onClick,int syllableId,Color canSelectColor)
    {
        _button = transform.GetComponent<CButton>();
        _image = transform.GetComponent<Image>();
        _syllableId = syllableId;
        _button.onClick.AddListener(()=>onClick?.Invoke());
        _canSelectColor = canSelectColor;
    }

    public void SetStatus(Status status)
    {
        switch (status)
        {
            case Status.canSelect:
                _image.color = _canSelectColor;
                _button.interactable = true;
                break;
            case Status.canNotSelect:
                _image.color = new Color(0.62f, 0.62f, 0.62f, 1);
                _button.interactable = false;
                break;
            case Status.Selected:
                _image.color = new Color(0.66f, 0.51f, 1, 1);
                _button.interactable = true;
                break;
        }
    }
}
