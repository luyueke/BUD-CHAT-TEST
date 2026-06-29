using Com.TheFallenGames.OSA.Util.IO;
using EventTracking;
using Game.Event;
using Game.Store;
using GameData.Manager;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
public class WeekRecommendedHairInfo
{
    public List<WeekRecommendedHairConfig> list;

}
public class WeekRecommendedHairConfig
{
    public string productId;
    public string price;
    public string period;
    public string unDiscountedPrice;
    public int hasPurchased;
    public List<int> pgcList;
    public WeekRecommendedHairNextWeekConfig nextWeek;
}
public class WeekRecommendedHairNextWeekConfig
{
    public string icon;
    public string name;
}

public class WeekRecommendedHairPanel : BasePanel<WeekRecommendedHairPanel>
{   
    //UI组件
    public Button backBtn;
    public Button buyBtn;
    public Image iconImage;
    public Image giveIconImage;
    public Image nextIconImage;
    public Text hairName;
    public Text nextHairName;
    public Text giveName;
    public Text time;
    public Text priceTxt;
    public Text origePriceTxt;
    public GameObject overObj;
    //商品数据
    WeekRecommendedHairConfig mData;
    string productId;
    string price;
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        backBtn.onClick.AddListener(CloseSelf);
        buyBtn.onClick.AddListener(() =>
        {
            OnPayBtnClick();
        });
        ReferenshData();

    }
    public void ReferenshData()
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.WeekRecommendedHair,
                HttpMethod.GET, "",
                onReceive: arg0 =>
                {
                    var data = JsonConvert.DeserializeObject<WeekRecommendedHairInfo>(arg0);
                    mData = data.list[0];
                    UpdateUI();
                }, onFail: arg0 =>
                {
                    LoggerUtils.LogError(HttpUrlDefine.TokenData + "请求出错" + arg0);
                });

    }
    void UpdateUI()
    {   
        //设置购买数据
        productId = mData.productId;
        price = mData.price;

        //更新UI
        priceTxt.text = mData.price;
        time.text = mData.period;

        iconImage.sprite = PgcUtils.LoadAvatarIcon(mData.pgcList[0].ToString(), iconImage.gameObject);
        giveIconImage.sprite = PgcUtils.LoadAvatarIcon(mData.pgcList[1].ToString(), giveIconImage.gameObject);

        var remoteRewardRawImg = nextIconImage.GetComponent<RemoteImageBehaviour>();
        remoteRewardRawImg.Load(
                    mData.nextWeek.icon,
                    true,
                    (fromCache, success) => {
                        //如果加载成功
                        if (success)
                        {
                        }
                        else
                        {
                            LoggerUtils.LogError("无法读取下周图片");
                        }
                    }
                );
        hairName.text = Es.DataTables.GetPgcNameData(mData.pgcList[0].ToString()).Name;
        giveName.text = Es.DataTables.GetPgcNameData(mData.pgcList[1].ToString()).Name;
        nextHairName.text = mData.nextWeek.name;

        overObj.gameObject.SetActive(mData.hasPurchased == 1);
        buyBtn.gameObject.SetActive(mData.hasPurchased != 1);
    }
    public void OnPayBtnClick()
    {
        // 检查是否是官方渠道
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            var panel = UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, price);
            panel.SetCallback(paymentType => StartPurchase(paymentType));
        }
        else
        {
            StartPurchase(ConfirmPaymentPanel.PaymentType.Default);
        }
    }

    private void StartPurchase(ConfirmPaymentPanel.PaymentType paymentType)
    {
        ShowPurchaseLoading();

        IAPDataManager.Inst.GetProductOrderId(productId, null, (success, info) =>
        {
            if (!success || string.IsNullOrEmpty(info?.budOrderId))
            {
                HidePurchaseLoading();
                return;
            }
            var channelProductInfo = new ChannelProductInfo
            {
                productId = productId,
                productName = productId,
                productDesc = productId,
                price = price,
                extension = JsonConvert.SerializeObject(info),
                cpOrderId = info.budOrderId,
                paymentType = (int)paymentType
            };

            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(channelProductInfo));
        });
    }

    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }

    private void HidePurchaseLoading()
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            UIManager.Inst.ClosePanel(panel);
        }
    }

    private void StartBillingFlow(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;
        var response = JsonConvert.DeserializeObject<BillingResultResponse>(message);
        if (response.resultType == (int)BillingResultType.UserPaySuccess)
        {
            // 购买成功
            AccountDataManager.Inst.BalanceInfo.Refresh();
            overObj.gameObject.SetActive(true);
            buyBtn.gameObject.SetActive(false);


            HidePurchaseLoading();
        }
        else if (response.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            // 处理失败...
        }
    }

}
