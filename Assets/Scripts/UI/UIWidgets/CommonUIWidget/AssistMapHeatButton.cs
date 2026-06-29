using Basic.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AssistMapHeatButton : CommonUIWidget
{
    [SerializeField] private CButton _assistBtn;
    //public GameObject _;
    public Text Txt_HeatNum;

    public Action<int> energyNumChange;
    private string _ugcId;
    private int _heatAmount;

    private string toUid;

    private void Awake()
    {
        _assistBtn.onClick.AddListener(OnAssistMapHeatClick);
    }

    public override void SetData(params object[] args)
    {
        base.SetData(args);

        toUid = (string)args[0];
        _ugcId = (string)args[1];
        _heatAmount = (int)args[2];

        RefreshEnergyAmount();
    }

    private void RefreshEnergyAmount()
    {
        var likeNumStr = GameUtils.ToBudCommonNumString(_heatAmount);
        Txt_HeatNum.text = likeNumStr;
    }

    private void OnAssistMapHeatClick()
    {
        //bool isSelf = toUid == AccountDataManager.Inst.Uid;
        //if (isSelf)
        //{
        //    return;
        //}
        var panel = UIManager.Inst.OpenPanel<AIHospitalMapHeatAssistPanel>(PanelId.AIHospitalMapHeatAssistPanel, toUid, _ugcId,_heatAmount);
        panel.SetCallback((amount =>
        {
            _heatAmount += amount;
            RefreshEnergyAmount();
        }));
    }
}
