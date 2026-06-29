using Game.Event;
using Message;
using UnityEngine;

public class PackBaseView : MonoBehaviour
{
    [HideInInspector]protected PaidPackageType PaidPackageType;
    [HideInInspector]protected ProductIdType ProductIdType;
    protected PackViewConfig packViewConfig;
    protected string TaskId = "";
    
    protected PackCenterPanel mainPanel;

    public void SetMainPanel(PackCenterPanel panel)
    {
        mainPanel = panel;
    }
    

    public virtual void OnCreate(PaidPackageType paidPackageType) {}
    public virtual void OnServerDataUpdate(PaidPackageListItem packageListItem) { }

    protected virtual void OnDestroy()
    {
    }

    public virtual void CloseSelf()
    {
        mainPanel?.CloseSelf();
    }

    protected virtual void OnTaskListUpdate(TaskListRsp taskListRsp) {}
    protected virtual void OnBuySuccess(string orderId) {}
    protected void GetProductInfo()
    {
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            var paidPackageList = res.paidPackageList;
            if (paidPackageList != null && this != null)
            {
                PaidPackageListItem packageListItem =
                    paidPackageList.Find(x => x.packageType == (int)PaidPackageType);
                if (packageListItem != null)
                {
                    OnServerDataUpdate(packageListItem);
                }
                else
                {
                    LoggerUtils.LogError("PackCenterPanel IAPDataManager.Inst.GetProductInfo Error:data is null");
                }
                    
            }
        });
    }
    
    protected void RefreshTaskStatus()
    {
        IAPDataManager.Inst.GetTaskList(TaskId, (b, taskListRsp) =>
        {
            if (taskListRsp == null)
            {
                return;
            }

            OnTaskListUpdate(taskListRsp);
        });
    }

    #region 支付相关流程
    protected void Purchase(ConfirmPaymentPanel.PaymentType paymentType,ProductInfo productInfo,string productName)
    {
        if (productInfo == null || string.IsNullOrEmpty(productInfo.productId))
        {
            return;
        }
        PackPurchaseUtils.Inst.StartPurchase(productInfo,paymentType,productName,OnPurchaseSuccess);
    }

    private void OnPurchaseSuccess(string orderId,ProductGemInfo productGemInfo)
    {
        MessageHelper.Broadcast(MessageName.BuyStartPack);
        RefreshTaskStatus();
        OnBuySuccess(orderId);
    }
    #endregion
}
