using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;


public class FlySwordPack : MonoBehaviour
{
    public CButton rootBtn;
    public CButton buyBtn;
    public Text saleEndTime;
    public Text priceTxt;
    public CButton buyBtnMask;

    private NewYearLimitedPackageListItem _newYearLimitedPackageListItem;
    private int costNum = 0;

    private Action<bool> BuyPackAction;
    private int maxNum = 80;
    private int leftCount = 0;
    

    private void Start()
    {

        rootBtn.onClick.AddListener(() =>
        {
            BuyPack();
        });
        buyBtn.onClick.AddListener(() =>
        {
            BuyPack();
        });
        
    }
    
    private void BuyPack()
    {
        // if (this.leftCount<=0)
        // {
        //     LoggerUtils.LogError("没有剩余数量了");
        //     return;
        // }
        // if (_newYearLimitedPackageListItem == null)
        // {
        //     LoggerUtils.LogError("_newYearLimitedPackageListItem is NULL");
        //     return;
        // }

        int leftCount = maxNum - _newYearLimitedPackageListItem.purchasedNum;
        // if (leftCount <= 0)
        // {
        //     TipPanel.ShowToast("超过购买次数限制");
        //     return;
        // }

        SpringLimitedDetailPanel panel =
            UIManager.Inst.OpenPanel<SpringLimitedDetailPanel>(PanelId.SpringLimitedDetailPanel, leftCount,
                _newYearLimitedPackageListItem);
    }


    public void SetData(NewYearLimitedPackageListItem newYearLimitedPackageListItem, Action<bool> buyPackAction)
    {
        this._newYearLimitedPackageListItem = newYearLimitedPackageListItem;
        this.BuyPackAction = buyPackAction;

        SetupUI();
    }

    private void SetupUI()
    {
        if (_newYearLimitedPackageListItem == null)
        {
            LoggerUtils.LogError("_newYearLimitedPackageListItem is NULL");
            return;
        }

        saleEndTime.text = "距离售卖结束：" + _newYearLimitedPackageListItem.endDate;
        priceTxt.text = _newYearLimitedPackageListItem.price.ToString();

        int leftCount = maxNum - _newYearLimitedPackageListItem.purchasedNum;

        this.leftCount = leftCount;
        buyBtn.gameObject.SetActive(leftCount > 0);
        buyBtnMask.gameObject.SetActive(leftCount <= 0);
        
        
    }
}