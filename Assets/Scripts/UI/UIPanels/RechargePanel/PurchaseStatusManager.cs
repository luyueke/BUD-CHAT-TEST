using System;
using System.Timers;


public class PurchaseStatusManager : GlobalInstance<PurchaseStatusManager>
{
    private BudTimer timer;
    private string budOrderId = "";
    private Action<bool, ProductGemInfo?> action;
    
    public void StartLoop(string budOrderId, Action<bool, ProductGemInfo?> action)
    {
        this.budOrderId = budOrderId;
        this.action = action;
        TimerManager.Inst.Stop(timer);
        timer = TimerManager.Inst.Run("purchaseStatus", 0, 0.5f,() =>
        {
            PayStatusRequest();
        });
    }
    
    public override void Release()
    {
        base.Release();
        
        TimerManager.Inst.Stop(timer);
    }

    public void StopLoop()
    {
        TimerManager.Inst.Stop(timer);
    }
    
    private void PayStatusRequest()
    {
        if (string.IsNullOrEmpty(budOrderId))
        {
            return;
        }
        IAPDataManager.Inst.GetPayOrderStatus(budOrderId, (success, orderCheck) =>
        {
            var order = orderCheck?.iapProduct;
            if (success && order != null)
            {
                StopLoop();
                action.Invoke(true, order);
            }
        });
    }
    
}