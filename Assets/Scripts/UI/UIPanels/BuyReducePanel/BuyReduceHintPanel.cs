using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class BuyReduceHintPanel : BasePanel<BuyReduceHintPanel>
{
    [SerializeField] private Button BackAction;
    [SerializeField] private CButton EnterBtn;
    [SerializeField] private CButton CancleBtn;
    [SerializeField] private Text ContentText;

    public Action EnterCallBack;

    protected override void Awake()
    {
        base.Awake();
        BackAction.onClick.AddListener(CloseSelf);
        CancleBtn.onClick.AddListener(CloseSelf);
        EnterBtn.onClick.AddListener(() =>
        {
            EnterCallBack?.Invoke();
            CloseSelf();
        });
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if(args.Length > 0)
        {
            ContentText.text = (string)args[0] + "所选商品吗？";
        }
    }

    public void SetAction(Action action)
    {
        EnterCallBack = action;
    }
}
