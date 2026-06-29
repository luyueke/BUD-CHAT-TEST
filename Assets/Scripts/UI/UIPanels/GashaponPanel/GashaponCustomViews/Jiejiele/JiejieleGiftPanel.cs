using System.Collections;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class JiejieleGiftPanel : BasePanel<JiejieleGiftPanel>
{
    public Button Close;
    public override void OnCreate()
    {
        base.OnCreate();
        Close.onClick.AddListener(CloseSelf);
    }


    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
    }
}