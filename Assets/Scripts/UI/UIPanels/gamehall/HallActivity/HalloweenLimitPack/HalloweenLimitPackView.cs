using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.GashaponPanel;
using UnityEngine;

public class HalloweenLimitPackView : BasePanel<HalloweenLimitPackView>
{
    public CButton btn_close;
    public CButton btn_buy;
    public CButton btn_tips;

    public List<HalloweenLimitPackItem> itemList;

    protected override void Awake()
    {
        base.Awake();
        AddEventListeners();
        //SetRulePath(RulePath);
        SetItemInfo();
    }

    private void AddEventListeners()
    {
        btn_close.onClick.AddListener(() =>
        {
            CloseSelf();
        });
        btn_buy.onClick.AddListener(()=>
        {
            HalloweenLimitPackMgr.Inst.BuyPackage();
            //HalloweenLimitPackMgr.Inst.TestInfo();
        });
        btn_tips.onClick.AddListener(()=>
        {
            UIManager.Inst.OpenPanel<HalloweenLimitPackRulePanel>(PanelId.HalloweenLimitPackRulePanel);
        });
    }

    private void SetItemInfo()
    {
        for (int i = 0; i < itemList.Count; i++)
        {
            itemList[i].SetData(i + 1);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
