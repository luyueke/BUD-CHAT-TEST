using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class CreatorRewardRulePanel : BasePanel<CreatorRulePanel>
{
    [SerializeField] private CButton CloseBtn;
    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
    }
}
