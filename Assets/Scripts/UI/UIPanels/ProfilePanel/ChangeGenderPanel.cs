using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ChangeGenderPanel : BasePanel<ChangeGenderPanel>
{
    public enum GenderType
    {
        Man = 1,
        Women = 2
    }

    public Toggle ManToggle;
    public Toggle WomenToggle;

    public CButton CloseBtn;
    public CButton ComfirmBtn;
    private int gender;
    public Action<int> OnComplete { set; private get; }

    void Start()
    {
        ManToggle.onValueChanged.AddListener(OnManToggleChange);
        WomenToggle.onValueChanged.AddListener(OnWomenToggleChange);
        ComfirmBtn.onClick.AddListener(OnComfirmClick);
        CloseBtn.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        gender = (int) args[0];
        if (gender == (int) GenderType.Man)
        {
            ManToggle.isOn = true;
        }
        else
        {
            WomenToggle.isOn = true;
        }
    }

    private void OnComfirmClick()
    {
        OnComplete?.Invoke(gender);
        CloseSelf();
    }

    private void OnManToggleChange(bool isValue)
    {
        if (isValue)
        {
            gender = (int)GenderType.Man;
        }
    }

    private void OnWomenToggleChange(bool isValue)
    {
        if (isValue)
        {
            gender = (int)GenderType.Women;
        }
    }
}
