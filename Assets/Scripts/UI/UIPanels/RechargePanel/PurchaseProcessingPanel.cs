using System;
using UI.Base;
using UnityEngine.UI;

public class PurchaseProcessingPanel : BasePanel<PurchaseProcessingPanel>
{
    public Text contentTxt;

    private BudTimer timer;

    private Action timeoutAction;

    public void SetContent(string val)
    {
        if (contentTxt != null)
        {
        }
    }

    public void StartTimer(float time, Action timeoutAction)
    {
        this.timeoutAction = timeoutAction;
        TimerManager.Inst.Stop(timer);
        timer = TimerManager.Inst.RunOnce("timer", time, () =>
        {
            CloseSelf();
            TipPanel.ShowToast("支付超时");
            if (this.timeoutAction != null)
            {
                this.timeoutAction.Invoke();
            }
        });
    }
    

    protected override void OnDestroy()
    {
        base.OnDestroy();
        TimerManager.Inst.Stop(timer);
    }
}