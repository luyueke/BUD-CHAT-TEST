using Basic.Extensions;
using Game.Event;
using GameData.Manager;
using GameUI;
using Message;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class QQWWGiftViewItem : MonoBehaviour
{
    public GameObject Claimed;

    public Button BtnBuyPreview;

    public Button BtnBuy;

    public Text TxtPrice;
    public Text TxtDiscount;

    //public int Id;

    public int Count;

    //public string Name;

    public Sprite Icon;
    public Sprite Icon2;

    Y2KSkateboardingPackage mdata;

    //string productId;

    private int id;

    

    string _curBudOrderId;
    private void Awake()
    {
        BtnBuyPreview.onClick.AddListener(OnBtnBuyPreview);
        BtnBuy.onClick.AddListener(OnBtnBuy);
    }

    //private void OnEnable()
    //{
    //    Debug.Log("LoverGiftViewItem");
    //    //SetData();
    //}

    public void SetData(int id)
    {
        var ls = IAPDataManager.Inst.productRes.qianqianWanwanPackageList;
        //Debug.LogError("qianqianWanwanPackageList=" + JsonConvert.SerializeObject(ls));
        this.id = id;
        if (ls == null)
        {
            return;
        }
        mdata = ls[id];

        if (mdata == null)
        {
            return;
        }
        TxtPrice.text = mdata.price + "元";
        TxtDiscount.text = mdata.discount;
        Claimed.gameObject.SetActive(mdata.isPaid == 1);
    }

    public void UpdateSort()
    {
        if (mdata == null)
        {
            return;
        }
        //if(mdata.isPaid == 1)
        //{
        //    transform.SetAsLastSibling();
        //}
    }
    void OnBtnBuy()
    {
        //ShowPackReward();
        //return;
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
            ShowPurchaseLoading();
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
            // 调用原生接口，拉起支付界面
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow, JsonConvert.SerializeObject(channelProductInfo));
            //StartLoopingVerify();
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


            // 弹出通用奖励面板
            ShowPackReward();

            // 通知其他系统刷新红点
            ReddotManagerUtils.Inst.RefreshRedDot();

            // 通知其他相关系统购买成功了
            MessageHelper.Broadcast(MessageName.OnPurchaseLimitedPackageSuccess);

            IAPDataManager.Inst.GetProductInfo();
        });
    }
    // 购买成功后，显示获得的奖励
    private void ShowPackReward()
    {
        // 已经买完了
        Claimed.gameObject.SetActive(true);
        //transform.SetAsLastSibling();
        var rewardItemDatas = new List<CommonRewardItemData>();
        //foreach (var rewardData in mdata.rewardList)
        //{
        //    var itemData = new CommonRewardItemData()
        //    {
        //        IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)rewardData.rewardType, gameObject),
        //        RewardAmount = rewardData.amount,
        //        rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardData.rewardType)
        //    };
        //    rewardItemDatas.Add(itemData);
        //}
        var itemData = new CommonRewardItemData()
        {
            IconSp = Icon,
            RewardAmount = 1,
            //rewardSpecial = Name,
        };
        rewardItemDatas.Add(itemData);

        if(Icon2 != null)
        {
            var itemData3 = new CommonRewardItemData()
            {
                IconSp = Icon2,
                RewardAmount = 1,
                //rewardSpecial = Name,
            };
            rewardItemDatas.Add(itemData3);
        }
        else
        {
            var itemData2 = new CommonRewardItemData()
            {
                IconSp = PgcUtils.LoadCurrencyIcon(CurrencyType.PurpleDreamCoin, gameObject),
                RewardAmount = Count,
                //rewardSpecial = Name,
            };
            rewardItemDatas.Add(itemData2);
        }


        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardItemDatas);
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

    void OnBtnBuyPreview()
    {


        switch(id)
        {
            case 0:
                UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, ProfileTheme.QianqianWanwan);
                break;
            case 1:
                //var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                //panel.PreviewTitle((int)TitleType.TitleQianqianWanwan);
                UIManager.Inst.OpenPanel<ProfileNicknamePreviewPanel>(PanelId.ProfileTitlePreviewPanel, (int)TitleType.TitleQianqianWanwan);
                break;
            case 2:
                UIManager.Inst.OpenPanel<ProfileNicknamePreviewPanel>(PanelId.ProfileNicknamePreviewPanel, 2);
                break;
            case 3:
                GameChatBubbleData chatData = UserUIWidgetManager.Inst.GetChatDataByID(25);
                if (chatData != null)
                {
                    var tem = new RewardPreviewInfo(BUDRewardType.RewardChatBubbles, CurrencyType.None, chatData.PgcId, chatData.Name, "");
                    tem.SetTitleAndDes(chatData.Name, chatData.Desc);
                    PreviewManager.Inst.ShowPreview(tem);
                }
                break;
        }
    }

    //void RewarPreview(string id)
    //{
    //    switch (id)
    //    {
    //        case "1":
    //            var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
    //            panel.UpdateUI(PgcUtils.LoadRewardIcon(BUDRewardType.RewardVipFreeTrail, panel.gameObject), "VIP体验卡1天", null, "特殊权益，领取后VIP月卡有效期增加1天");
    //            break;
    //        case "2":
    //            PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardYouYouCoin, CurrencyType.YouYouCoin, "", "", ""));
    //            break;
    //        case "3":
    //            var panel3 = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
    //            //panel3.SetEventPreview(new List<string> { "40300503" }, "冬日荡秋千", "", spriteatlasPath, "赛季累充福利", "#3586FF", "SeasonCumulativeBg");
    //            break;
    //        case "4":
    //            PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardYouYouCoin, CurrencyType.YouYouCoin, "", "", ""));
    //            break;
    //        case "5":
    //            HeadCycleData headData = UserUIWidgetManager.Inst.GetHeadCycleDataById(35);
    //            if (headData != null)
    //            {
    //                PreviewManager.Inst.ShowAvatarFramePreview(headData.Id);
    //            }
    //            break;
    //        case "6":
    //            GameChatBubbleData chatData = UserUIWidgetManager.Inst.GetChatDataByID(21);
    //            if (chatData != null)
    //            {
    //                var tem = new RewardPreviewInfo(BUDRewardType.RewardChatBubbles, CurrencyType.None, chatData.PgcId, chatData.Name, "");
    //                tem.SetTitleAndDes(chatData.Name, chatData.Desc);
    //                PreviewManager.Inst.ShowPreview(tem);
    //            }
    //            break;
    //        case "7":
    //            UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, ProfileTheme.S13Season);
    //            break;


    //    }
    //}
}