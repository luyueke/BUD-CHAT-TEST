using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class StartPackView : MonoBehaviour
{
    [SerializeField] private CButton viewPackage;
    [SerializeField] private Text endTxt;
    [SerializeField] private GameObject isBuyObj;
    [SerializeField] private Text btnTxt;
    [SerializeField] private Text numText;

    private PaidPackageType paidPackageType = 0;

    private PaidPackageListItem _paidPackageListItem;


    private void Start()
    {
        numText.SetText("x80");
#if PACKAGE_TYPE_US
        numText.SetText("x400");
#endif
        viewPackage.onClick.AddListener(OnItemClick);
    }

    public void SetData(PaidPackageType paidPackageType,PaidPackageListItem packageListItem)
    {
        this.paidPackageType = paidPackageType;
        this._paidPackageListItem = packageListItem;
        int isPaid = packageListItem.isPaid;
        if (isPaid == 1)
        {
            endTxt.SetLocalText("距离任务结束: {0}" , packageListItem.taskEndDate);
            btnTxt.SetLocalText("查看进度");
        }
        else
        {
            endTxt.SetLocalText("距离售卖结束: {0}" , packageListItem.endDate);
            btnTxt.SetLocalText("查看礼包");
        }

        // endTxt.text = isPaid == 1 ?"距离任务结束: " + packageListItem.taskEndDate:"距离售卖结束: " + packageListItem.endDate;
        // btnTxt.text = isPaid == 1 ? "查看进度" : "查看礼包";
        isBuyObj.gameObject.SetActive(isPaid == 1);
    }

    public PaidPackageListItem GetBindData()
    {
        return _paidPackageListItem;
    }

    private void OnItemClick()
    {
        if (paidPackageType == PaidPackageType.WeirdCorePack)
        {
            //var panel = UIManager.Inst.OpenPanel<S4ShiYuanPackPanel>(PanelId.S4ShiYuanPackPanel);
            //panel.SetData(_paidPackageListItem);
        }
        else if(paidPackageType == PaidPackageType.Y2KPack)
        {
            var panel = UIManager.Inst.OpenPanel<S4LiuYuanPackPanel>(PanelId.S4LiuYuanPackPanel);
            panel.SetData(_paidPackageListItem);
        }
        else if (paidPackageType == PaidPackageType.LimitedTimeCurrencyPack)
        {
            var panel = UIManager.Inst.OpenPanel<S4LimitCurrencyPackPanel>(PanelId.S4LimitCurrencyPackPanel);
            panel.SetData(_paidPackageListItem);
        }
        else if (paidPackageType == PaidPackageType.Illegal)
        {
            LoggerUtils.LogError("paidPackageType is Illegal");
        }
        else
        {
            UIManager.Inst.OpenPanel<PackCenterPanel>(PanelId.PackCenterPanel,paidPackageType,_paidPackageListItem);
        }
    }
}