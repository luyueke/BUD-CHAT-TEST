
using System;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;


public class NewYearFireworkPack : MonoBehaviour
{
    
    public Text saleTimeTxt;
    public Text discountTxt;
    public Text originalPriceTxt;
    public Text discountPriceTxt;
    public CButton buyBtn;
    public CButton giftBtn;
    public CButton buyBtnMask;
    public GameObject tagDisCount;
    public CButton item1;
    public CButton item2;
    public Image item1Image;
    public Image item2Image;
    public GameObject confirmObj;
    public Button confirmBuyBtn;
    public Button confirmBgObjBtn;
    public Text confirmPriceTxt;
    private List<string> pgcIds = new List<string>() {"40300429", "40100430" };

    
    private NewYearLimitedPackageListItem _newYearLimitedPackageListItem;
    private int costNum = 0;

    private Action<bool> BuyPackAction;
    
    private void Start()
    {
        buyBtn.onClick.AddListener(() =>
        {
            confirmObj.gameObject.SetActive(!confirmObj.gameObject.activeSelf);
        });
        
        confirmBuyBtn.onClick.AddListener(() =>
        {
            confirmObj.gameObject.SetActive(false);
            PurchaseByGem();
        });

        confirmBgObjBtn.onClick.AddListener(() =>
        {
            confirmObj.gameObject.SetActive(false);
        });
        
        
        giftBtn.onClick.AddListener(() =>
        {
            var sendGiftPanel = UIManager.Inst.SwapPanel(PanelId.SendGiftPanel) as SendGiftPanel;
            sendGiftPanel?.JumpTo(SendGiftMainTabs.Tab.Tool);
        });
        
        item1.onClick.AddListener(() =>
        {
            var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
            panel.SetNewyearEventPreview(new List<string>(){pgcIds[0]},"新年快乐烟花" , "","","新春限定", null);
        });
        
        item2.onClick.AddListener(() =>
        {
            var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
            panel.SetNewyearEventPreview(new List<string>(){pgcIds[1]},"漫天烟花" , "","","新春限定", null);
        });
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

        saleTimeTxt.text = "距离售卖结束：" + _newYearLimitedPackageListItem.endDate;

        if (_newYearLimitedPackageListItem.isPaid == 1)
        {
            buyBtn.gameObject.SetActive(false);
            buyBtnMask.gameObject.SetActive(true);
            discountTxt.gameObject.SetActive(false);
            return;
        }

        var hasDiscount = _newYearLimitedPackageListItem.hasDiscount;
        if (hasDiscount == 1)
        {
            tagDisCount.gameObject.SetActive(true);
            discountTxt.gameObject.SetActive(true);
            originalPriceTxt.gameObject.SetActive(true);
            originalPriceTxt.text = _newYearLimitedPackageListItem.price.ToString();
            discountPriceTxt.text = _newYearLimitedPackageListItem.discountPrice.ToString();
            confirmPriceTxt.text = _newYearLimitedPackageListItem.discountPrice.ToString();
            discountTxt.text = "距离折扣结束：" + _newYearLimitedPackageListItem.discountEndDate;
            costNum = _newYearLimitedPackageListItem.discountPrice;
        }
        else
        {
            tagDisCount.gameObject.SetActive(false);
            discountTxt.gameObject.SetActive(false);
            originalPriceTxt.gameObject.SetActive(false);
            discountPriceTxt.text = _newYearLimitedPackageListItem.price.ToString();
            confirmPriceTxt.text = _newYearLimitedPackageListItem.price.ToString();
            costNum = _newYearLimitedPackageListItem.price;
        }
        
    }
    
    private void PurchaseByGem()
    {
        if (_newYearLimitedPackageListItem == null)
        {
            LoggerUtils.LogError("_newYearLimitedPackageListItem is NULL");
            return;
        }
        var currentGem = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
        if (currentGem < costNum)
        {
            UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, costNum - currentGem);
            return;
        }
        
        IAPDataManager.Inst.PayByGem(BUDProductType.NewYearLimitedPackage, _newYearLimitedPackageListItem.productId, resultHandler:
            result =>
            {
                if (this != null)
                {
                }
        
                if (result)
                {
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    this.BuyPackAction?.Invoke(true);
                    var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                    panel.ShowRewards(new List<CommonRewardItemData>()
                    {
                        new CommonRewardItemData()
                        {
                            IconSp = item1Image.sprite,
                            RewardAmount =  1,
                            rewardName = "新年快乐烟花"
                        },
                        new CommonRewardItemData()
                        {
                            IconSp = item2Image.sprite,
                            RewardAmount =  1,
                            rewardName = "漫天烟花"
                        }
                    });
                }
            });
    }


    
}