using System.Collections;
using System.Collections.Generic;
using Network;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class ReconnectTcpPanel : BasePanel<ReconnectTcpPanel>
{
    public CButton Btn_Close;
    public CButton Btn_Confirm;

    public override void OnCreate()
    {
        base.OnCreate();
        Btn_Close.onClick.AddListener(CloseSelf);
        Btn_Confirm.onClick.AddListener(OnRebootingClick);
    }

    private void OnRebootingClick()
    {
        NetworkManager.Inst.Reconnect(true);
        CloseSelf();
    }
}
