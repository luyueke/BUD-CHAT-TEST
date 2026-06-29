using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwapTabButton : TabButton
{
    [SerializeField] GameObject _normalTarget;
    [SerializeField] Text _normalText;
    [SerializeField] GameObject _selectedTarget;
    [SerializeField] Text _selectedText;

    private void Awake()
    {
        SetTargetActive();
    }

    internal override void OnSelected(bool value)
    {
        if(_isSelected != value)
        {
            base.OnSelected(value);
            SetTargetActive();
        }
    }

    void SetTargetActive()
    {
        _normalTarget.gameObject.SetActive(!_isSelected);
        _selectedTarget.gameObject.SetActive(_isSelected);
    }

    public Text getSelectedText()
    {
        return _selectedText;
    }

    public Text getNormalText()
    {
        return _normalText;
    }
}
