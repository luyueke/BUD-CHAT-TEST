using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class LimitedGiftPack : MonoBehaviour
{
    [SerializeField] 
    private CButton viewPackage;

    private LimitPackageData limitPackageData;
    private void Start()
    {
        viewPackage.onClick.AddListener(OnItemClick);
    }
    
    private void OnItemClick()
    {
        this.limitPackageData =  IAPDataManager.Inst.GetLimitedRechargeData();

        UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.LimitedRechargeGiftPack);
    }
}
