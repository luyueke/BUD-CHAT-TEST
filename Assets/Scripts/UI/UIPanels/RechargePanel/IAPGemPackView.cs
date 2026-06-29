using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

public class IAPGemPackView : MonoBehaviour
{
    [SerializeField] private GameObject _go_CumReddot;
    [SerializeField] private CButton _btn_Cumlative;
    [SerializeField] private List<IAPGemItemView> _itemViews;
    [SerializeField] private Transform BG;

    public Action<ProductGemInfo> OnClickProduct;

    private RechargeBenifits _rechargeBenifits;

    private void Awake()
    {
#if PACKAGE_TYPE_US
        RefreshDefaultInfo();
#endif
        //TODO:@Jaywill 海外服需要从端上获取商品列表
        var datas = IAPDataManager.Inst.GetGemPackProducts();
        this._rechargeBenifits =  IAPDataManager.Inst.GetCumulativeRechargeData();
        if(_btn_Cumlative != null) _btn_Cumlative.onClick.AddListener(OnBtnCumlativeClick);
        if (datas == null || datas.Count != _itemViews.Count)
        {
            RefreshDataFromServer();
            return;
        }
        SyncProductInfo(datas);

        ShowReddot();
    }

    private void Start() {
        if (BG == null)
        {
            return;
        }
        string atlasPath = RechargePanel.RechargePanelAtlas;
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#9859FF", atlasPath, new List<string>()
        {
            "s4_limit_bg_1", "s4_limit_bg_2", "s4_limit_bg_3"
        });
        item.gameObject.SetActive(true);

    }

    private void SyncProductInfo(List<ProductGemInfo> items)
    {
        if (items == null)
        {
            return;
        }

        if (_itemViews.Count != items.Count)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            _itemViews[i].SetData(items[i], OnClickItem);
        }
    }

    //临时Hack做法，折扣，刷新额外gem
    private void RefreshDefaultInfo()
    {
        var defaultInfos = IAPDataManager.Inst.GetPriceDefaultInfos();
        if (defaultInfos != null)
        {
            for (int i = 0; i < defaultInfos.Count; i++)
            {
                var info = defaultInfos[i];
                if (i < _itemViews.Count)
                {
                    var itemNode = _itemViews[i];
                    itemNode.SetPrice(info.price);
                    itemNode.SetGemNum(info.gems);
                    itemNode.SetBonusNum(info.bonus);
                }
            }
        }
    }

    private void RefreshDataFromServer()
    {
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            var gems = res.gemRechargeList;
            if (gems != null && this != null)
            {
                SyncProductInfo(gems);
            }
        });
    }

    private void OnClickItem(ProductGemInfo gemInfo)
    {

        if (OnClickProduct != null) {
            OnClickProduct?.Invoke(gemInfo);
            return;
        }
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, gemInfo.price);
            panel.SetCallback(paymentType => { BuyGem(gemInfo, paymentType); });
            return;
        }

        BuyGem(gemInfo, ConfirmPaymentPanel.PaymentType.Default);
    }

    private void BuyGem(ProductGemInfo gemInfo, ConfirmPaymentPanel.PaymentType paymentType)
    {
        var productId = gemInfo.productId;
        if (string.IsNullOrEmpty(productId))
        {
            LoggerUtils.LogError($"[IAP] Not get productId {gemInfo}");
            return;
        }

        string productName = gemInfo.gemNum + "钻";
        var channelProductInfo = gemInfo.toU8Info();
        PackPurchaseUtils.Inst.StartPurchase(channelProductInfo,paymentType,productName,OnBuyGemSuccess);
    }
     private void OnBuyGemSuccess(string orderId,ProductGemInfo productGemInfo)
    {
        ShowReward(productGemInfo);
    }

    private void ShowReward(ProductGemInfo productInfo)
    {
        if (productInfo == null)
        {
            return;
        }

        var coinNum = productInfo.gemNum;
        if (coinNum <= 0)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(new List<TaskRewardData>()
        {
            new TaskRewardData()
            {
                num = coinNum,
                rewardType = (int)BUDRewardType.RewardGem,
            }
        });
    }

    private void OnBtnCumlativeClick()
    {
        this._rechargeBenifits =  IAPDataManager.Inst.GetCumulativeRechargeData();
        UIManager.Inst.OpenPanel(PanelId.CumulativeRechargePanel, this._rechargeBenifits);
    }

    private void ShowReddot()
    {
        if(this._rechargeBenifits == null)
            return;
        if (this._go_CumReddot == null)
            return;
        foreach (var levelData in this._rechargeBenifits.rechargeLevelList)
        {
            if (levelData.rewardStatus == (int)ClaimStatus.Unlocked)
            {
                _go_CumReddot.SetActive(true);
                return;
            }
        }
    }
}
