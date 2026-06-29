using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class VehiclePosSelectPanel : BasePanel<VehiclePosePanel>
{
    [SerializeField] private Toggle Tog_Select1;
    [SerializeField] private Toggle Tog_Select2;
    [SerializeField] private CButton EnterBtn;

    private int selectType;

    public Action<int> OnSelectPoseEnter;

    public override void OnCreate()
    {
        base.OnCreate();
        selectType = 1;
        Tog_Select1.onValueChanged.AddListener((isOn) =>
        {
            if (isOn) selectType = 1;
        });
        Tog_Select2.onValueChanged.AddListener((isOn) => 
        {
            if (isOn) selectType = 2;
        });
        EnterBtn.onClick.AddListener(() =>
        {
            OnSelectPoseEnter?.Invoke(selectType);
            CloseSelf();
        });
    }

}
