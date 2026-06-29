using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class TreasureHuntRulePanel : BasePanel<TreasureHuntRulePanel>
{
    [SerializeField] private CButton CloseBtn;
    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
    }


}
