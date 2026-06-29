using System;
using Game.Props.PropsManagers;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class WarningPanel : BasePanel<WarningPanel>
{
    [SerializeField]
    private CText contentText;
    [SerializeField]
    private CButton btnGotIt;

    private Action onCloseAct;

    public override void OnCreate()
    {
        base.OnCreate();
        btnGotIt.onClick.RemoveAllListeners();
        btnGotIt.onClick.AddListener(OnGotItBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        var TitleStr = (string)args[0];
        contentText.SetLocalText(TitleStr);
    }

    private void OnGotItBtnClick()
    {
        onCloseAct?.Invoke();
        CloseSelf();
    }
}
