using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewYearsTurntableView : ActivityBaseView {
    [SerializeField] private TurnItemView turnItemPrefab;
    [SerializeField]
    private CButton payButton;

    [SerializeField]
    private GameObject lockButton;

    [SerializeField] private CButton infoBtn;

    private Dictionary<int, TurnInfo> turnInfos;
    private Dictionary<int, TurnItemView> turnItemViews;
    private TurnBagItemView[] turnBagItemViews;
    private ActivityInfo activityInfo;

    private int currentEventId = 0;


    public override void Init(ActivityInfo info) {
        base.Init(info);
        activityInfo = info;
        InitData();
        InitUI();
    }


    #region 数据初始化

    private void InitData() {
        turnInfos = new Dictionary<int, TurnInfo>() {
            {
                1, new TurnInfo() {
                    name = "第一轮",
                    productName = "幸运福袋6元档",
                    productId = ProductIdType.product_fudai6,
                    rewardInfos = new List<TurnRewardInfo>() {
                        new TurnRewardInfo() {
                            multiple = "2倍",
                            rewardAmount = 12,
                        },
                        new TurnRewardInfo() {
                            multiple = "2.5倍",
                            rewardAmount = 15,
                        },
                        new TurnRewardInfo() {
                            multiple = "3倍",
                            rewardAmount = 18,
                        },
                        new TurnRewardInfo() {
                            multiple = "3.5倍",
                            rewardAmount = 21,
                        },
                        new TurnRewardInfo() {
                            multiple = "4倍",
                            rewardAmount = 24,
                        }
                    }
                }
            }, {
                2, new TurnInfo() {
                    name = "第二轮",
                    productName = "幸运福袋12元档",
                    productId = ProductIdType.product_fudai12,
                    rewardInfos = new List<TurnRewardInfo>() {
                        new TurnRewardInfo() {
                            multiple = "1.6倍",
                            rewardAmount = 20,
                        },
                        new TurnRewardInfo() {
                            multiple = "2倍",
                            rewardAmount = 24,
                        },
                        new TurnRewardInfo() {
                            multiple = "2.5倍",
                            rewardAmount = 30,
                        },
                        new TurnRewardInfo() {
                            multiple = "3.5倍",
                            rewardAmount = 42,
                        },
                        new TurnRewardInfo() {
                            multiple = "4.5倍",
                            rewardAmount = 54,
                        }
                    }
                }
            }, {
                3, new TurnInfo() {
                    name = "第三轮",
                    productName = "幸运福袋30元档",
                    productId = ProductIdType.product_fudai30,
                    rewardInfos = new List<TurnRewardInfo>() {
                        new TurnRewardInfo() {
                            multiple = "1.4倍",
                            rewardAmount = 42,
                        },
                        new TurnRewardInfo() {
                            multiple = "1.8倍",
                            rewardAmount = 54,
                        },
                        new TurnRewardInfo() {
                            multiple = "2.2倍",
                            rewardAmount = 68,
                        },
                        new TurnRewardInfo() {
                            multiple = "3.6倍",
                            rewardAmount = 108,
                        },
                        new TurnRewardInfo() {
                            multiple = "5倍",
                            rewardAmount = 150,
                        }
                    }
                }
            }, {
                4, new TurnInfo() {
                    name = "第四轮",
                    productName = "幸运福袋68元档",
                    productId = ProductIdType.product_fudai68,
                    rewardInfos = new List<TurnRewardInfo>() {
                        new TurnRewardInfo() {
                            multiple = "1.2倍",
                            rewardAmount = 88,
                        },
                        new TurnRewardInfo() {
                            multiple = "1.7倍",
                            rewardAmount = 118,
                        },
                        new TurnRewardInfo() {
                            multiple = "2.4倍",
                            rewardAmount = 168,
                        },
                        new TurnRewardInfo() {
                            multiple = "3.9倍",
                            rewardAmount = 268,
                        },
                        new TurnRewardInfo() {
                            multiple = "5.4倍",
                            rewardAmount = 368,
                        }
                    }
                }
            }, {
                5, new TurnInfo() {
                    name = "第五轮",
                    productName = "幸运福袋98元档",
                    productId = ProductIdType.product_fudai98,
                    rewardInfos = new List<TurnRewardInfo>() {
                        new TurnRewardInfo() {
                            multiple = "1.2倍",
                            rewardAmount = 120,
                        },
                        new TurnRewardInfo() {
                            multiple = "1.6倍",
                            rewardAmount = 158,
                        },
                        new TurnRewardInfo() {
                            multiple = "2.9倍",
                            rewardAmount = 288,
                        },
                        new TurnRewardInfo() {
                            multiple = "3.9倍",
                            rewardAmount = 388,
                        },
                        new TurnRewardInfo() {
                            multiple = "6倍",
                            rewardAmount = 588,
                        }
                    }
                }
            }, {
                6, new TurnInfo() {
                    name = "第六轮",
                    productName = "幸运福袋128元档",
                    productId = ProductIdType.product_fudai128,
                    rewardInfos = new List<TurnRewardInfo>() {
                        new TurnRewardInfo() {
                            multiple = "1.2倍",
                            rewardAmount = 160,
                        },
                        new TurnRewardInfo() {
                            multiple = "1.5倍",
                            rewardAmount = 198,
                        },
                        new TurnRewardInfo() {
                            multiple = "3倍",
                            rewardAmount = 388,
                        },
                        new TurnRewardInfo() {
                            multiple = "4倍",
                            rewardAmount = 518,
                        },
                        new TurnRewardInfo() {
                            multiple = "6.9倍",
                            rewardAmount = 888,
                        }
                    }
                }
            }
        };

#if UNITY_EDITOR
        if (activityInfo.eventList[0].eventStatus == (int)ClaimStatus.ErrStatus) {
            activityInfo.eventList[0].eventStatus = (int)ClaimStatus.Claimed;
        }
        if (activityInfo.eventList[1].eventStatus == (int)ClaimStatus.ErrStatus) {
            activityInfo.eventList[1].eventStatus = (int)ClaimStatus.Unlocked;
        }
#endif
    }

    #endregion

    private void InitUI() {
        turnItemViews = new Dictionary<int, TurnItemView>();
        foreach (var eventInfo in activityInfo.eventList) {
            if (turnInfos.TryGetValue(eventInfo.eventId, out TurnInfo info)) {
                var tmpItemView = Instantiate(turnItemPrefab, turnItemPrefab.transform.parent);
                tmpItemView.gameObject.SetActive(true);
                tmpItemView.SetTurn(eventInfo.eventId, info, OnSelectTurn);
                turnItemViews.Add(eventInfo.eventId, tmpItemView);
            }
        }

        turnItemPrefab.gameObject.SetActive(false);
        turnBagItemViews = transform.GetComponentsInChildren<TurnBagItemView>(true);
        payButton.onClick.AddListener(OnPayClick);
        infoBtn.onClick.AddListener(OnInfoClick);

    }

    private void OnInfoClick() {
        // 借用GashaponRulePanel
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, "Assets/Loadable/UI/ActivityCenterPanel/NewYearsTurntable/Rule.json");
    }

    private void OnPayClick() {
        if (!turnInfos.TryGetValue(currentEventId, out TurnInfo turnInfo)) {
            return;
        }
        ProductInfo productInfo = new ProductInfo() {
            productId = IAPDataManager.Inst.GetProductId(turnInfo.productId),
            productName = turnInfo.productName,
            productDesc = turnInfo.productName,
            price = IAPDataManager.Inst.GetPriceInfo(turnInfo.productId).price
        };
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, productInfo.price);
            panel.SetCallback(paymentType => { Purchase(currentEventId, paymentType, productInfo, turnInfo.productName); });
            return;
        }

        Purchase(currentEventId, ConfirmPaymentPanel.PaymentType.Default,productInfo,turnInfo.productName);
    }

    private void Purchase(int eventId, ConfirmPaymentPanel.PaymentType paymentType, ProductInfo productInfo, string productName) {
        ChannelProductInfo channelProductInfo = productInfo.toU8Info();
        PackPurchaseProcess process = new PackPurchaseProcess();
        process.StartPurchaseProcess(channelProductInfo,paymentType,productName, (string orderId,ProductGemInfo productGemInfo) => {
            OnPurchaseSuccess(eventId, orderId, productGemInfo);
        });

    }

    private void OnPurchaseSuccess(int eventId, string orderId,ProductGemInfo productGemInfo)
    {
        JObject jObject = new JObject()
        {
            ["activityId"] = activityInfo.activityId,
            ["eventId"] = eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse activityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);

                OnClaimSuccess(activityEventClaimResponse);
            }, null);
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response) {
        if (this == null || gameObject == null) {
            return;
        }
        var eventInfo = activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null)
        {
            return;
        }
        eventInfo.eventStatus = response.eventInfo.eventStatus;

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        panel.ShowRewards(new List<CommonRewardItemData>() {
            new CommonRewardItemData() {
                rewardName = PgcUtils.GetRewardName(BUDRewardType.RewardPurpleDreamCoin),
                rewardType = (int)BUDRewardType.RewardPurpleDreamCoin,
                RewardAmount = response.claimAmount,
                IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.RewardPurpleDreamCoin, panel.gameObject)
            }
        });
        UpdateRedDot();
        RefrashData(activityInfo);
    }



    private void OnSelectTurn(int eventId) {

        if (!turnInfos.TryGetValue(eventId, out TurnInfo turnInfo)) {
            return;
        }

        for (int i = 0; i < turnBagItemViews.Length; i++) {
            turnBagItemViews[i].SetRewardInfo(turnInfo.rewardInfos[i]);
        }

        if (turnItemViews.TryGetValue(currentEventId, out TurnItemView lastTurnItem)) {
            lastTurnItem.SetSelected(false);
        }

        currentEventId = eventId;

        if (turnItemViews.TryGetValue(currentEventId, out TurnItemView turnItemView)) {
            turnItemView.SetSelected(true);
        }

        var eventInfo = activityInfo.eventList.FirstOrDefault(tmp => tmp.eventId == eventId);
        payButton.gameObject.SetActive(false);
        var payPriceText = GameObjectEx.FindComponentByName<Text>(payButton.transform, "Price");
        payPriceText.SetLocalText("¥{0}",IAPDataManager.Inst.GetPriceInfo(turnInfo.productId).price);
        payPriceText.SetPreferredSize();
        lockButton.gameObject.SetActive(false);
        var lockPriceText = GameObjectEx.FindComponentByName<Text>(lockButton.transform, "Price");
        lockPriceText.SetLocalText("¥{0}", IAPDataManager.Inst.GetPriceInfo(turnInfo.productId).price);
        lockPriceText.SetPreferredSize();
        if (eventInfo != null) {
            switch ((ClaimStatus)eventInfo.eventStatus) {
                case ClaimStatus.Claimed:
                    break;
                case ClaimStatus.Lock:
                    lockButton.gameObject.SetActive(true);
                    break;
                case ClaimStatus.Unlocked:
                    payButton.gameObject.SetActive(true);
                    break;
            }
        }
    }

    public override void RefrashData(ActivityInfo info) {
        base.RefrashData(info);
        activityInfo = info;

        foreach (var eventInfo in info.eventList) {
            if (turnItemViews.TryGetValue(eventInfo.eventId, out var turnItemView)) {
                turnItemView.SetStatus((ClaimStatus)eventInfo.eventStatus);
            }
        }
        var unlockEventInfo = activityInfo.eventList.FirstOrDefault(tmp => tmp.eventStatus == (int)ClaimStatus.Unlocked);
        if (unlockEventInfo == null) {
            unlockEventInfo = activityInfo.eventList[0];
        }
        OnSelectTurn(unlockEventInfo.eventId);
    }
}


public class TurnInfo {
    public string name;
    public ProductIdType productId;
    public string productName;
    public List<TurnRewardInfo> rewardInfos;
}

public class TurnRewardInfo {
    public string multiple;
    public string rewardName = PgcUtils.GetRewardName(BUDRewardType.RewardPurpleDreamCoin);
    public BUDRewardType rewardType = BUDRewardType.RewardPurpleDreamCoin;
    public int rewardAmount;
}
