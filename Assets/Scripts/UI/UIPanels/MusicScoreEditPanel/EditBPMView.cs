using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class EditBPMView : MonoBehaviour
{
    public CButton closeViewBtn;
    public CButton yesBtn;
  
    [SerializeField] Slider slider;

    private Action<float> onValueChanged;
    public void Init(Action<float> action)
    {
        closeViewBtn.onClick.AddListener(Close);
        yesBtn.onClick.AddListener(OnYesBtnClick);
        slider.onValueChanged.AddListener(OnValueChanged);
        onValueChanged = action;
    }
    private void OnValueChanged(float value)
    {
        onValueChanged?.Invoke(value);
    }
    public void SetValueWithoutNotify(float value)
    {
        slider.SetValueWithoutNotify(value);
    }
    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Close()
    {
        gameObject.SetActive(false);
    }
    public void OnYesBtnClick()
    {
        Close();
    }
}
