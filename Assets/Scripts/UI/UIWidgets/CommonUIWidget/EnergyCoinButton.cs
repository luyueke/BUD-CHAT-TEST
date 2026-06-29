using System;
using Basic.Utils;
using UI.BaseWidgets;
using UnityEngine;

public class EnergyCoinButton : CommonUIWidget
{
    public CButton Btn_EnergyCoin;
    public GameObject Go_Liked;
    public CText Txt_LikeNum;

    public Action<int> energyNumChange;
    private string _ugcId;
    private int _energyAmount;

    private string toUid;

    private void Awake()
    {
        Btn_EnergyCoin.onClick.AddListener(OnEnergyCoinClick);
    }
    
    public override void SetData(params object[] args)
    {
        base.SetData(args);

        toUid = (string)args[0];
        _ugcId = (string)args[1];
        _energyAmount = (int)args[2];
        
        RefreshEnergyAmount();
    }

    private void RefreshEnergyAmount()
    {
        var likeNumStr = GameUtils.ToBudCommonNumString(_energyAmount);
        Txt_LikeNum.text = likeNumStr;
    }

    private void OnEnergyCoinClick()
    {
        bool isSelf = toUid == AccountDataManager.Inst.Uid;
        if (isSelf)
        {
            return;
        }
        var panel = UIManager.Inst.OpenPanel<SendCoinPanel>(PanelId.SendCoinPanel, toUid, _ugcId);
        panel.SetCallback((amount =>
        {
            _energyAmount += amount;
            RefreshEnergyAmount();
        }));
    }
}
