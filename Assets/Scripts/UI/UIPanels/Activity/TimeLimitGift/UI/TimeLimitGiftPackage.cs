using Es;
using Game.Database;
using Game.Store;
using GameData.Manager;
using Message;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class TimeLimitGiftPackage : MonoBehaviour
    {
        public Button Buy;

        public GameObject None;

        public string productId;

        public TimeLimitGiftType type;

        public List<GameObject> Items;

        PopupPackageList mdata;

        string _curBudOrderId;

        private void Awake()
        {
            Buy.onClick.AddListener(OnBuy);
        }

        public void SetData(PopupPackageList _date) {
            mdata = _date;
            if (mdata == null)
            {
                return;
            }
            None.gameObject.SetActive(mdata.isPaid == 1);
            Buy.gameObject.SetActive(mdata.isPaid != 1);
            if (Items.Count == 4)
            {
                var pdcId = BagDatabase.Inst.Select("40100513");
                if (/*(pdcId != null && pdcId.OwnedNum > 0) &&*/ TimeLimitGiftSystem.Inst.GetBuyKey())
                {
                    Items[3].gameObject.SetActive(true);
                    Items[2].gameObject.SetActive(false);
                }
                else
                {
                    Items[3].gameObject.SetActive(false);
                    Items[2].gameObject.SetActive(true);
                }
            }
        }

        void OnBuy() {
            // 如果已购买或数据为空，则不处理
            if (mdata.isPaid == 1 || mdata == null)
            {
                return;
            }

            // 检查是否是官方渠道，某些渠道可能需要弹出支付方式选择
            if (IAPDataManager.Inst.IsOfficialChannel())
            {
                var panel = UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, mdata.productInfo.price.ToString());
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

            IAPDataManager.Inst.GetProductOrderId(mdata.productInfo.productId, null, (success, orderInfo) =>
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
                    productId = mdata.productInfo.productId,
                    productName = mdata.productInfo.productName,
                    productDesc = mdata.productInfo.productName,
                    price = mdata.productInfo.price.ToString(),
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
                Buy.gameObject.SetActive(false);
                None.gameObject.SetActive(true);

                // 弹出通用奖励面板
                ShowPackReward();

                //TimeLimitGiftSystem.Inst.SetCooling(TcpTimeSystem.Inst.ServerTime); // 购买时 同步冷却时间

                // 通知其他系统刷新红点
                ReddotManagerUtils.Inst.RefreshRedDot();

                // 通知其他相关系统购买成功了
                MessageHelper.Broadcast(MessageName.OnPurchaseLimitedPackageSuccess);

                IAPDataManager.Inst.GetProductInfo(TimeLimitGiftSystem.Inst.RefreshBtn);
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
            foreach (var item in Items)
            {
                if (item.gameObject.activeSelf)
                {
                    var itemData = new CommonRewardItemData()
                    {
                        IconSp = item.transform.GetChild(0).GetComponent<Image>().sprite,
                        RewardAmount = 1,
                        rewardSpecial = item.transform.GetChild(1).GetComponent<Text>().text,
                    };
                    rewardItemDatas.Add(itemData);
                }
            }
            
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowRewards(rewardItemDatas);
        }
    }
}