using Game.Database;
using GameData.Manager;
using GameUI;
using Message;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class JiejieleGiftPanelItem : MonoBehaviour
{
    public Button Buy;

    public GameObject BuyNone;

    public Button Preview;

    public Text Txt;

    public int Id;

    public Image Icon;

    Y2KSkateboardingPackage mdata;

    string _curBudOrderId;

    string productId;
    private void Awake()
    {
        Buy.onClick.AddListener(OnBuy);
        Preview.onClick.AddListener(OnPreview);
        productId = JiejieleGiftSystem.Inst.productIds[Id];
    }

    public void OnEnable()
    {
        SetData();
    }

    void OnPreview() 
    {
        UIManager.Inst.OpenPanel<ProfileNicknamePreviewPanel>(PanelId.ProfileNicknamePreviewPanel, 1);
    }

    public void SetData()
    {
        var ls = IAPDataManager.Inst.productRes.lanternFestivalPackageList;
        mdata =ls.Find(x => { return x.productId.EndsWith(productId); });
        if (mdata == null)
        {
            return;
        }
        if (mdata.isPaid == 1)
        {
            Txt.text = "限购1/1";
            Buy.gameObject.SetActive(false);
            BuyNone.gameObject.SetActive(true);
        }
        else
        {
            Txt.text = "限购0/1";
            Buy.gameObject.SetActive(true);
            BuyNone.gameObject.SetActive(false);
        }
    }

    void OnBuy()
    {
        // 如果已购买或数据为空，则不处理
        if (mdata == null || mdata.isPaid == 1)
        {
            return;
        }

        // 检查是否是官方渠道，某些渠道可能需要弹出支付方式选择
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            var panel = UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, mdata.price.ToString());
            panel.SetCallback(paymentType =>
            {
                PurchaseIAPOrder(paymentType);
            });
            return;
        }

        // 其他渠道直接使用默认支付方式
        PurchaseIAPOrder(ConfirmPaymentPanel.PaymentType.Default);
    }

    // 步骤1：获取订单ID，然后拉起平台支付
    private void PurchaseIAPOrder(ConfirmPaymentPanel.PaymentType paymentType)
    {
        // 向我们自己的服务器请求一个唯一的订单ID
        ShowPurchaseLoading();

        IAPDataManager.Inst.GetProductOrderId(mdata.productId, null, (success, orderInfo) =>
        {
            _curBudOrderId = orderInfo?.budOrderId; // 保存这个订单ID，用于后续查询

            // 如果获取订单ID失败，则结束流程
            if (!success || string.IsNullOrEmpty(_curBudOrderId))
            {
                Debug.Log("获取订单ID失败!");
                HidePurchaseLoading();
                return;
            }

            // 构造一个给手机平台（iOS/Android）支付SDK用的信息包
            var channelProductInfo = new ChannelProductInfo
            {
                productId = mdata.productId,
                productName = mdata.productName,
                productDesc = mdata.productName,
                price = mdata.price.ToString(),
                extension = JsonConvert.SerializeObject(orderInfo), // 把服务器返回的订单信息序列化后传过去
                cpOrderId = _curBudOrderId,
                paymentType = (int)paymentType
            };

            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
            // 调用原生接口，拉起支付界面
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow, JsonConvert.SerializeObject(channelProductInfo));
        });
    }

    // 步骤2：处理从手机平台返回的支付结果
    private void StartBillingFlow(string message)
    {
        Debug.Log($"StartBillingFlow 000 message={message}");
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        // TokenDataManager.Inst.GetTokenData();
        if (string.IsNullOrEmpty(message))
        {
            HidePurchaseLoading();
            return;
        }
        var billingResultResponse = JsonConvert.DeserializeObject<BillingResultResponse>(message);

        // 如果平台返回支付成功，我们就开始轮询自己的服务器确认是否到账
        if (billingResultResponse.resultType == (int)BillingResultType.UserPaySuccess)
        {
            // TokenDataManager.Inst.GetTokenData();
            StartLoopingVerify();
        }
        // 如果平台返回失败，就直接结束
        else if (billingResultResponse.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            PurchaseStatusManager.Inst.StopLoop();
        }
    }

    // 步骤3：轮询服务器，确认订单是否已发货
    private void StartLoopingVerify()
    {
        Debug.Log($"StartLoopingVerify 0000 _curBudOrderId={_curBudOrderId}");
        if (string.IsNullOrEmpty(_curBudOrderId))
        {
            HidePurchaseLoading();
            return;
        }
        Debug.Log($"StartLoopingVerify 111 _curBudOrderId={_curBudOrderId}");
        // 打开一个带倒计时的遮罩，防止用户重复点击
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            panel.StartTimer(60, () =>
            {
                Debug.Log("查询超时，停止轮询。");
                HidePurchaseLoading();
                PurchaseStatusManager.Inst.StopLoop();
                // MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
            });
        }
        Debug.Log($"StartLoopingVerify 222 _curBudOrderId={_curBudOrderId}");
        // 开始用订单ID轮询服务器
        PurchaseStatusManager.Inst.StartLoop(_curBudOrderId, (isOrderSuccess, productGemInfo) =>
        {
            // 只要有结果（无论成功失败），就关闭加载界面
            HidePurchaseLoading();
            Debug.Log($"StartLoopingVerify 333 _curBudOrderId={_curBudOrderId}");
            if (!isOrderSuccess)
            {
                Debug.Log("服务器返回订单处理失败。");
                return;
            }
            Debug.Log($"StartLoopingVerify 444 _curBudOrderId={_curBudOrderId}");
            // --- 真正的购买成功 ---
            Debug.Log("进入到购买成功回调");
            TokenDataManager.Inst.GetTokenData();
            // 刷新钻石、金币等信息
            AccountDataManager.Inst.BalanceInfo.Refresh();

            // 已经买完了
            Txt.text = "限购1/1";
            Buy.gameObject.SetActive(false);
            BuyNone.gameObject.SetActive(true);

            // 弹出通用奖励面板
            ShowPackReward();

            // 通知其他系统刷新红点
            ReddotManagerUtils.Inst.RefreshRedDot();

            // 通知其他相关系统购买成功了
            MessageHelper.Broadcast(MessageName.OnPurchaseLimitedPackageSuccess);

            IAPDataManager.Inst.GetProductInfo(null);
        });
    }
    private void ShowPurchaseLoading()
    {

        UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }
    private void HidePurchaseLoading()
    {

        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            UIManager.Inst.ClosePanel(panel);
        }
    }

    // 购买成功后，显示获得的奖励
    private void ShowPackReward()
    {
        var rewardItemDatas = new List<CommonRewardItemData>();
        var itemData = new CommonRewardItemData()
        {
            IconSp = Icon.sprite,
            RewardAmount = 1,
        };
        rewardItemDatas.Add(itemData);

        var itemData2 = new CommonRewardItemData()
        {
            IconSp = PgcUtils.LoadCurrencyIcon(CurrencyType.PurpleDreamCoin, gameObject),
            RewardAmount = 12,
        };
        rewardItemDatas.Add(itemData2);

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardItemDatas);
    }

}