using Basic.Utils;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class GotAStarPanel : BasePanel<GotAStarPanel>
{
    [SerializeField] private Text starName;
    [SerializeField] private Text collectTime;

    public override void OnCreate()
    {
    }

    public override void OnShow(params object[] args)
    {
        var namePara = (string)args[0];
        SetStarName(namePara);
        SetTimeTxt();

        UIManager.Inst.ControlOtherWindowShowWithLock(WindowId.CollectStarWindow, false);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        UIManager.Inst.UnLockControlledOtherWindow();
    }

    public void SetStarName(string namePara)
    {
        starName.text = namePara;
    }

    private void SetTimeTxt()
    {
        collectTime.text = $"<b>-</b> {GameUtils.GetTimeDayInDot()} <b>-</b>";
    }
}
