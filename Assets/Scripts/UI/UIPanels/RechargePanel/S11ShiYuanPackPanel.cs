using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Event;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;


public class S11ShiYuanPackPanel : MonoBehaviour
{
    [SerializeField] private Transform bg;
    [SerializeField] private CButton backBtn;
    [SerializeField] private Text endTime;
    [SerializeField] private CButton buyBtn;
    [SerializeField] private CButton previewBtn;
    [SerializeField] private StartPackItem startPackItem;
    [SerializeField] private GameObject rightArrow;
    [SerializeField] private StartPackItem startpackBigItem;
    [SerializeField] private GameObject budObj;
    [SerializeField] private GameObject overObj;
    [SerializeField] private GameObject taskEndObj;
    [SerializeField] private Text taskEndTime;

    int isPaid = 0;
    public Transform taskContent;
    private List<StartPackItem> startPackItems = new List<StartPackItem>();
    private List<RewardItem> _rewardItems = new List<RewardItem>();
    private IapTrackData _iapTrackData = new IapTrackData();


    private PaidPackageListItem _paidPackageListItem;
    private TaskInfoData _taskInfoData;

    private string budOrderId = "";
    private string _taskId = "WasabiPack";
    private string spriteatlasPath = RechargePanel.RechargePanelAtlas;


    void Start()
    {
        SetData(null);
        var itemObj = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            ?.Instantiate(bg);
        if (itemObj)
        {
            string atlasPath = RechargePanel.S4ShiYuanPackPanelAtlas;
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item?.InitCustomBgItem( "#A4D1F2", atlasPath,
                new List<string>()
                {
                    "s11_shiyuan_bg_1", "s11_shiyuan_bg_2", "s11_shiyuan_bg_3","s11_shiyuan_bg_4"
                });
            item?.gameObject.SetActive(true);
        }
        
        InitClickListener();
        GenerateContent();
        MessageHelper.AddListener(MessageName.UpdateHallTask, RefreshTaskStatus);
    }

    private void TrackEvent(BillingResultResponse billingResultResponse)
    {
        Dictionary<string, object> trackData = new Dictionary<string, object>();
        float result;
        if (float.TryParse(_iapTrackData.price, NumberStyles.Float, CultureInfo.InvariantCulture,out result))
        {
            trackData.Add("priceNum", result);
        }

        trackData.Add("item", _iapTrackData.item);
        bool isUploadData = !AnalyticsManager.Inst.ContainOrderID(budOrderId);
        if (isUploadData)
        {
            trackData.Add("budOrderId", budOrderId ?? "");
            trackData.Add("method", "S4ShiYuanPackPanel");
            AnalyticsManager.Inst.Track(AnalyticsEventName.TOP_UP_SUCCESS, trackData);
        }
    }

    private void GenerateContent()
    {
        var ugcAsset =
            Loader.Load<TextAsset>(
                "Assets/Loadable/UI/UIPanel/S11ShiYuanPackgePanel/S11ShiYuanPackConfig.json", this.gameObject);
        _rewardItems = JsonConvert.DeserializeObject<List<RewardItem>>(ugcAsset.text);
        taskContent.gameObject.SetActive(false);

        startPackItems.Clear();
        foreach (Transform child in taskContent)
        {
            Destroy(child.gameObject);
        }

        for (var i = 0; i < _rewardItems.Count; i++)
        {
            if (string.IsNullOrEmpty(_rewardItems[i].rewardName2))
            {
                var taskItem = Instantiate(startpackBigItem, taskContent);
                taskItem.OnInitCreate(_rewardItems[i], true);
                startPackItems.Add(taskItem);
            }
            else
            {
                var taskItem = Instantiate(startPackItem, taskContent);
                taskItem.OnInitCreate(_rewardItems[i], false);
                startPackItems.Add(taskItem);
            }
            if (i != _rewardItems.Count -1)
            {
                Instantiate(rightArrow, taskContent);
            }
        }
    }

    void OnDestroy()
    {
    }

    private void StartBillingFlow(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        BillingResultResponse billingResultResponse = JsonConvert.DeserializeObject<BillingResultResponse>(message);
        if (billingResultResponse.resultType == (int)BillingResultType.UserPaySuccess)
        {
            StartLooping();
            TrackEvent(billingResultResponse);
        }
        else if (billingResultResponse.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            PurchaseStatusManager.Inst.StopLoop();
        }
    }

    private void StartLooping()
    {
        if (string.IsNullOrEmpty(budOrderId))
        {
            return;
        }

        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            panel.StartTimer(60, () =>
            {
                PurchaseStatusManager.Inst.StopLoop(); 
                MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
            });
        }

        PurchaseStatusManager.Inst.StartLoop(budOrderId, (orderResult, productGemInfo) =>
        {
            if (this == null)
            {
                return;
            }

            if (!orderResult)
            {
                return;
            }

            MessageHelper.Broadcast(MessageName.BuyStartPack);
            AccountDataManager.Inst.BalanceInfo.Refresh();
            HidePurchaseLoading();
            ShowStartPackReward();
            RefreshTaskStatus();
            budObj.gameObject.SetActive(false);
            taskEndObj.gameObject.SetActive(true);
            _paidPackageListItem.isPaid = 1;
        });
    }

    private void ShowStartPackReward()
    {
        Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck); 
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "oneyuanpack_reward",
                    gameObject),
                RewardAmount = 1,
                rewardName = "10元礼包"
            }
        });
    }

    public void SetData(PaidPackageListItem packageListItem)
    {
        if (packageListItem == null)
        {
            IAPDataManager.Inst.GetProductInfo(res =>
            {
                var paidPackageList = res.paidPackageList;
                if (paidPackageList != null && this != null)
                {
                    PaidPackageListItem packageListItem =
                        paidPackageList.Find(x => x.packageType == 4);
                    if (packageListItem == null)
                    {
                        Debug.Log("PaidPackageType.WeirdCorePack为空！");
                        return;
                    }
                    isPaid = packageListItem.isPaid;
                    LoadDefaultUI(packageListItem);
                }
                RefreshTaskStatus();
            });
        }
        else
        {
            RefreshTaskStatus();
            LoadDefaultUI(packageListItem);
        }

    }

    private void LoadDefaultUI(PaidPackageListItem packageListItem)
    {
        this._paidPackageListItem = packageListItem;
        budObj.gameObject.SetActive(packageListItem.isPaid != 1);
        endTime.gameObject.SetActive(packageListItem.isPaid != 1);
        endTime.SetLocalText("距售卖时间结束还有: {0}",packageListItem.endDate);
    }

    public static void GetTaskList(Action ac)
    {
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            var paidPackageList = res.paidPackageList;
            if (paidPackageList != null)
            {
                PaidPackageListItem packageListItem =
                    paidPackageList.Find(x => x.packageType == 4);
                if (packageListItem == null)
                {
                    Debug.Log("PaidPackageType.WeirdCorePack为空！");
                    return;
                }
                if (packageListItem.isPaid == 1)
                {
                    IAPDataManager.Inst.GetTaskList("WasabiPack", (b, taskInfoResponse) =>
                    {
                        List<TaskInfoData> taskInfoDatas = taskInfoResponse.list;
                        if (taskInfoDatas == null || taskInfoDatas.Count <= 0)
                        {
                            return;
                        }

                        TaskInfoData tsTaskInfoData = taskInfoDatas[0];
                        if (tsTaskInfoData == null)
                        {
                            return;
                        }

                        if (tsTaskInfoData.taskStatus == 2)
                        {
                            return;
                        }

                        foreach (TaskItemData item in tsTaskInfoData.eventList)
                        {
                            if (item.eventStatus != 3)
                            {
                                ac?.Invoke();
                                return;
                            }
                        }
                    });
                }
            }
        });
    }

    private void RefreshTaskStatus()
    {
        IAPDataManager.Inst.GetTaskList(_taskId, (b, taskInfoResponse) =>
        {
            if (taskInfoResponse == null)
            {   
                return;
            }

            
            taskContent.gameObject.SetActive(true);
            List<TaskInfoData> taskInfoDatas = taskInfoResponse.list;
            if (taskInfoDatas == null || taskInfoDatas.Count <= 0)
            {
                return;
            }

            TaskInfoData tsTaskInfoData = taskInfoDatas[0];
            if (tsTaskInfoData == null)
            {
                return;
            }
            overObj.gameObject.SetActive(tsTaskInfoData.taskStatus == 2 && isPaid == 1);
            taskEndObj.gameObject.SetActive(tsTaskInfoData.taskStatus != 2 && isPaid == 1);
            this._taskInfoData = tsTaskInfoData;

            List<TaskItemData> eventList = tsTaskInfoData.eventList;
            taskEndTime.SetLocalText("距任务结束还有: {0}" , _taskInfoData.endDate);
            for (int i = 0; i < startPackItems.Count; i++)
            {
                startPackItems[i].SetData(_taskId, eventList[i], taskItemData => { RefreshTaskStatus(); }, () =>
                {
                    if (_paidPackageListItem.isPaid != 1)
                    {
                        TipPanel.ShowToast("购买10元礼包获取对应礼品哦");
                    }
                }, PaidPackageType.WasabiPack, tsTaskInfoData.taskStatus == 2);
            }
        });
    }

    private void InitClickListener()
    {
        backBtn.onClick.AddListener(() => { Destroy(gameObject); });

        buyBtn.onClick.AddListener(() => { BuyStartpack(); });

        previewBtn.onClick.AddListener(() =>
        {
            var pgcIds = new List<string>();
            pgcIds.Add("10200021");
            pgcIds.Add("10900308");
            pgcIds.Add("11000110");
            pgcIds.Add("10400307");
            pgcIds.Add("11600018");
            if (pgcIds == null || pgcIds.Count == 0)
            {
                return;
            }

            EventRewardPanelData data = new EventRewardPanelData()
            {
                bgColor = "#A4D1F2",
                rewardItemBgColor = "#89958D",
                atlasPath = RechargePanel.S4ShiYuanPackPanelAtlas,
                iconList = new List<string>()
                {
                    "s11_shiyuan_bg_1", "s11_shiyuan_bg_2", "s11_shiyuan_bg_3","s11_shiyuan_bg_4"
                },
                rewardList = pgcIds
            };
            UIManager.Inst.OpenPanel<EventCenterRewardPanel>(PanelId.EventCenterRewardPanel, data);
        });
    }


    private void BuyStartpack()
    {
        
        if (_paidPackageListItem == null)
        {
            return;
        }

        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ProductInfo productInfo = _paidPackageListItem.productInfo;
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, productInfo.price);
            panel.SetCallback(paymentType => { BuyOnePack(paymentType); });
            return;
        }

        BuyOnePack(ConfirmPaymentPanel.PaymentType.Default);
    }

    private void BuyOnePack(ConfirmPaymentPanel.PaymentType paymentType)
    {
        ProductInfo productInfo = _paidPackageListItem.productInfo;
        if (productInfo == null)
        {
            return;
        }

        ChannelProductInfo channelProductInfo = productInfo.toU8Info();
        var productId = channelProductInfo.productId;
        if (string.IsNullOrEmpty(productId))
        {
            return;
        }

        _iapTrackData.price = channelProductInfo.price;
        _iapTrackData.item = "10元礼包";
        _iapTrackData.channel = "";

        ShowPurchaseLoading();
        IAPDataManager.Inst.GetProductOrderId(productId, null,(b, info) =>
        {
            budOrderId = info?.budOrderId;
            if (!b || string.IsNullOrEmpty(budOrderId))
            {
                HidePurchaseLoading();
                return;
            }

            channelProductInfo.extension = JsonConvert.SerializeObject(info);
            channelProductInfo.cpOrderId = budOrderId;
            channelProductInfo.paymentType = (int)paymentType;
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(channelProductInfo));
        });
    }

    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        PurchaseProcessingPanel processingPanel =
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
}