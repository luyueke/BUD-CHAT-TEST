using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class VehicleAniPanel : BasePanel<VehicleAniPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private CButton EnterBtn;

    [SerializeField] private List<Toggle> runAniToggles;
    [SerializeField] private List<Toggle> endAniToggles;

    private int runAniType;
    private int endAniType;

    private VehicleInfo vehicleInfo;

    protected override void Awake()
    {
        CloseBtn.onClick.AddListener(CloseSelf);
        EnterBtn.onClick.AddListener(OnEnterClick);
        for (int i = 0; i < runAniToggles.Count; i++)
        {
            int index = i;
            runAniToggles[i].onValueChanged.AddListener((isOn) =>
            {
                RunAniToggleChange(isOn, index);
            });
            endAniToggles[i].onValueChanged.AddListener((isOn) =>
            {
                EndAniToggleChange(isOn, index);
            });
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args != null && args.Length > 0)
        {
            vehicleInfo = (VehicleInfo)args[0];
            runAniToggles[vehicleInfo.runAniType].isOn = true;
            endAniToggles[vehicleInfo.endAniType].isOn = true;
        }
    }

    private void OnEnterClick()
    {
        vehicleInfo.runAniType = runAniType;
        vehicleInfo.endAniType = endAniType;
        CloseSelf();
    }

    private void RunAniToggleChange(bool isOn, int type)
    {
        if(isOn)
        {
            runAniType = type;
        }
    }

    private void EndAniToggleChange(bool isOn, int type)
    {
        if (isOn)
        {
            endAniType = type;
        }
    }

}
