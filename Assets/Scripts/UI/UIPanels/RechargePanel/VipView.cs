using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class VipView : MonoBehaviour
{
    [SerializeField] private CButton viewDetail;
    [SerializeField] private Text priceText;
    [SerializeField] private Text unitText;
    [SerializeField] private Text gemText;
    
    private SubscribeStatusResponse _subscribeStatusResponse;

    public void SetData(SubscribeStatusResponse subscribeStatusResponse)
    {
        this._subscribeStatusResponse = subscribeStatusResponse;
    }

    private void Start()
    {
        viewDetail.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.VipMonthPack);
        });
#if PACKAGE_TYPE_US
        AdapterPackageUI();
#endif
    }
    
    private void AdapterPackageUI()
    {
        int vipGem = RechargeConfig.VipGem;
        gemText.SetText("x"+vipGem);
        
        unitText.gameObject.SetActive(false);
        
        var priceInfo = IAPDataManager.Inst.GetPriceInfo(ProductIdType.product_budvip);
        if (priceInfo != null)
        {
            priceText.SetText(priceInfo.priceLocal);
        }
    }

}
