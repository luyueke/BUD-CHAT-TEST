using GameData.Manager;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
public class AnniversaryStoreGiftItem : MonoBehaviour
{
    public Text price;
    public Text title;
    public Text canbuyNumText;
    public Text origeText;
    public Button buyBtn;
    public Transform contont;
    public GameObject iconObj;
    BaseLimitPackageData mdata;
    public int canBuyCunt;
    string _curBudOrderId;
    public void Init(BaseLimitPackageData data)
    {
        mdata = data;
        buyBtn.onClick.AddListener(OnBuyBtnClick);
        SetIcons(data.rewardList);
        title.text = data.name;
        canbuyNumText.text = $"限购{data.leftPurchaseTimes}次";
        canBuyCunt = data.leftPurchaseTimes;
        origeText.text = data.discount + "%";
        Text enabletex = buyBtn.transform.Find("enabletext").GetComponent<Text>();
        enabletex.text = data.price.ToString() + "元";
        Text disabletex = buyBtn.transform.Find("disabletext").GetComponent<Text>();
        disabletex.text = data.price.ToString() + "元";
        if (enabletex.text.Length > 4)
        {
            enabletex.fontSize = 38;
            disabletex.fontSize = 38;
        }
        else
        {
            enabletex.fontSize = 50;
            disabletex.fontSize = 50;
        }
        // 已经买完了
        if (data.leftPurchaseTimes == 0)
        {
            buyBtn.interactable = false; // 按钮不可点击
            enabletex.gameObject.SetActive(false);
            disabletex.gameObject.SetActive(true);
        }
        // 如果还未领取
        else
        {
            buyBtn.interactable = true; // 按钮可点击
            enabletex.gameObject.SetActive(true);
            disabletex.gameObject.SetActive(false);
        }
    }
    void SetIcons(List<LimitPackageRewardData> items)
    {
        // 再创建新的物品
        foreach (var item in items)
        {
            var icon = Instantiate(iconObj, contont);
            icon.transform.Find("icon").GetComponent<Image>().sprite = PgcUtils.LoadRewardIcon((BUDRewardType)item.rewardType, icon.gameObject);
            SetSpriteSize(icon.transform.Find("icon").GetComponent<Image>());
            icon.gameObject.SetActive(true);
            icon.transform.Find("num").GetComponent<Text>().text = item.amount.ToString();
        }
    }
    void OnBuyBtnClick()
    {
        // 如果已购买或数据为空，则不处理
        if (mdata.isPurchase == 1 || mdata == null)
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
                productName = mdata.name,
                productDesc = mdata.name,
                price = mdata.price.ToString(),
                extension = JsonConvert.SerializeObject(orderInfo), // 把服务器返回的订单信息序列化后传过去
                cpOrderId = _curBudOrderId,
                paymentType = (int)paymentType
            };
            ShowPurchaseLoading();
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
            canBuyCunt -= 1;
            int num = canBuyCunt;
            num = num < 0 ? 0 : num;
            canbuyNumText.text = $"限购{num}次";
            // 已经买完了
            if (num == 0)
            {
                buyBtn.interactable = false; // 按钮不可点击
                buyBtn.transform.Find("enabletext").gameObject.SetActive(false);
                buyBtn.transform.Find("disabletext").gameObject.SetActive(true);
            }
            // 如果还未领取
            else
            {
                buyBtn.interactable = true; // 按钮可点击
                buyBtn.transform.Find("enabletext").gameObject.SetActive(true);
                buyBtn.transform.Find("disabletext").gameObject.SetActive(false);
            }
            // 弹出通用奖励面板
            ShowPackReward();

            // 通知其他系统刷新红点
            ReddotManagerUtils.Inst.RefreshRedDot();

            // 通知其他相关系统购买成功了
            MessageHelper.Broadcast(MessageName.OnPurchaseLimitedPackageSuccess);
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
        foreach (var rewardData in mdata.rewardList)
        {
            var itemData = new CommonRewardItemData()
            {
                IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)rewardData.rewardType, gameObject),
                RewardAmount = rewardData.amount,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardData.rewardType)
            };
            rewardItemDatas.Add(itemData);
        }

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardItemDatas);
    }
    private void SetSpriteSize(Image rewardImg)
    {
        if (rewardImg.sprite == null)
        {
            return;
        }

        // 1. 设置为原始尺寸，以确保宽高比正确
        rewardImg.SetNativeSize();

        // 2. 获取原始尺寸
        float originalWidth = rewardImg.rectTransform.rect.width;
        float originalHeight = rewardImg.rectTransform.rect.height;

        // 如果原始尺寸已经是0，则无需缩放
        if (originalWidth == 0 || originalHeight == 0)
        {
            return;
        }

        // 3. 计算缩放比例
        float scaleFactor = 1.0f;
        float max_size = 100f;

        // 检查宽度是否超出限制
        if (originalWidth > max_size)
        {
            scaleFactor = max_size / originalWidth;
        }

        // 检查高度是否超出限制，并取更小的缩放比，以确保高也能放进去
        if (originalHeight > max_size)
        {
            // 例如：如果宽需要缩小到0.8倍，高需要缩小到0.7倍，我们取0.7，这样能保证宽高都在100以内
            scaleFactor = Mathf.Min(scaleFactor, max_size / originalHeight);
        }

        // 4. 应用缩放
        rewardImg.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
    }


}
