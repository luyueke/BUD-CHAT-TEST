using System;
using System.Collections;
using System.Collections.Generic;
using Game.Event;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class S9LimitedTimePinkCoin: PackBaseView
{
    [SerializeField] private Transform bg;
    [SerializeField] private Text endTime;
    [SerializeField] private CButton buyBtn;
    [SerializeField] private PackCommonItem packItem;
    [SerializeField] private GameObject rightArrow;
    [SerializeField] private PackCommonItem packBigItem;
    [SerializeField] private Text taskEndTime;
    [SerializeField] protected GameObject buyNode;
    [SerializeField] protected GameObject taskEndTips;
    [SerializeField] public Transform taskContent;
    [SerializeField] private Text priceText;

    private List<PackCommonItem> packItemNodeList = new List<PackCommonItem>();
    private PaidPackageListItem _paidPackageListItem;
    private TaskInfoData _taskInfoData;

    private string rewardIconName = "icon_reward";
    private string configPath;
    private string spriteatlasPath;

    private void InitConfig(PaidPackageType paidPackageType)
    {
        //注意海外服需要手动配置该参数（商品id，跟运营拿，海外服需要，国服不需要）
        ProductIdType = ProductIdType.product_budlimiteds8;
        LoadConfig(paidPackageType);
        
    }

    private void LoadConfig(PaidPackageType paidPackageType)
    {
        PaidPackageType = paidPackageType;
        string packName = PaidPackageType.ToString();
        string configPath = string.Format(PackCenterPanel.ViewBasePath + "{0}/{1}.json",packName, packName);
        var textAsset = Loader.Load<TextAsset>(configPath, gameObject);
        packViewConfig = JsonConvert.DeserializeObject<PackViewConfig>(textAsset.text);
        TaskId = packViewConfig.taskId;//跟后端(谢梓峰)拿
        spriteatlasPath = string.Format(PackCenterPanel.ViewBasePath + "{0}/{1}.spriteatlas",packName, packName);
        if (!string.IsNullOrEmpty(packViewConfig.spriteatlasPath))
        {
            spriteatlasPath = packViewConfig.spriteatlasPath;
        }
    }

    private void Start() {
        EventTracking.LoadEvent.ReportPopupStatus(RechargeId.S9LimitedTimeCurrencyPack.ToString());
        InitConfig(PaidPackageType.S9LimitedTimeCurrencyPack);
        InitBg();
        GetProductInfo();
        InitClickListener();
        CreateItems(packViewConfig.rewardDataList);
        RefreshTaskStatus();

#if PACKAGE_TYPE_US
        AdapterPackageUI();
#endif
    }


    public override void OnCreate(PaidPackageType paidPackageType)
    {
        InitConfig(paidPackageType);
        InitBg();
        InitClickListener();
        CreateItems(packViewConfig.rewardDataList);
        RefreshTaskStatus();

#if PACKAGE_TYPE_US
        AdapterPackageUI();
#endif
    }

    private void AdapterPackageUI()
    {
        var productInfo = IAPDataManager.Inst.GetPriceInfo(ProductIdType);
        priceText.SetLocalText(productInfo.priceLocal);
    }

    public override void OnServerDataUpdate(PaidPackageListItem packageListItem)
    {
        this._paidPackageListItem = packageListItem;
        buyNode.gameObject.SetActive(packageListItem.isPaid != 1);
        taskEndTips.gameObject.SetActive(packageListItem.isPaid == 1);
        if(packageListItem.isPaid != 1)
        {
            endTime.SetLocalText("距售卖时间结束还有: {0}", packageListItem.endDate);
        }
        else
        {
            endTime.SetLocalText("距任务时间结束还有: {0}", packageListItem.endDate);
        }
    }


    private void InitBg()
    {
        var itemObj = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")?.Instantiate(bg);
        if (itemObj)
        {
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item?.InitCustomBgItem(packViewConfig.bgColor, spriteatlasPath,packViewConfig.bgSpriteList);
            item?.gameObject.SetActive(true);
        }
    }


    private void InitClickListener()
    {
        buyBtn.onClick.AddListener(OnBuyBtnClick);
    }

    protected override void OnTaskListUpdate(TaskListRsp taskListRsp)
    {
        taskContent.gameObject.SetActive(true);
        List<TaskInfoData> taskInfoDatas = taskListRsp.list;
        if (taskInfoDatas == null || taskInfoDatas.Count <= 0)
        {
            return;
        }

        TaskInfoData tsTaskInfoData = taskInfoDatas[0];
        if (tsTaskInfoData == null)
        {
            return;
        }

        this._taskInfoData = tsTaskInfoData;

        List<TaskItemData> eventList = tsTaskInfoData.eventList;
        taskEndTime.SetLocalText("距任务结束还有: {0}" , _taskInfoData.endDate);
        for (int i = 0; i < packItemNodeList.Count; i++)
        {
            packItemNodeList[i].SetData(TaskId, eventList[i], taskItemData => { RefreshTaskStatus(); }, () =>
            {
                if (_paidPackageListItem.isPaid != 1)
                {
                    string localProductName = LocalizationManager.Inst.GetLocalizedText(packViewConfig.productName);
                    string tips = LocalizationManager.Inst.GetLocalizedText("购买{0}获取对应礼品哦",localProductName);
                    TipPanel.ShowToast(tips);
                }
                else
                {
                    string localProductName = LocalizationManager.Inst.GetLocalizedText(packViewConfig.productName);
                    string tips = LocalizationManager.Inst.GetLocalizedText("继续登录即可解锁奖励");
                    TipPanel.ShowToast(tips);
                }
            });
        }
    }

    protected override void OnBuySuccess(string orderId)
    {
        ShowPackReward();
        buyNode.gameObject.SetActive(false);
        taskEndTips.gameObject.SetActive(true);
        _paidPackageListItem.isPaid = 1;
    }

    protected void ShowPackReward()
    {
        Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardIconName, gameObject),
                RewardAmount = 1,
                rewardName = packViewConfig.productName
            }
        });
    }

    private void CreateItems(List<RewardItem> rewardDataList)
    {
        // taskContent.gameObject.SetActive(false);
        packItemNodeList.Clear();
        foreach (Transform child in taskContent)
        {
            Destroy(child.gameObject);
        }

        for (var i = 0; i < rewardDataList.Count; i++)
        {
            if (string.IsNullOrEmpty(rewardDataList[i].rewardName2))
            {
                var taskItem = Instantiate(packBigItem, taskContent);
                taskItem.SetAtlasPath(spriteatlasPath);
                taskItem.OnInitCreate(rewardDataList[i], true);
                packItemNodeList.Add(taskItem);
            }
            else
            {
                var taskItem = Instantiate(packItem, taskContent);
                taskItem.SetAtlasPath(spriteatlasPath);
                taskItem.OnInitCreate(rewardDataList[i], false);
                packItemNodeList.Add(taskItem);
            }
            if (i != rewardDataList.Count -1)
            {
                Instantiate(rightArrow, taskContent);
            }
        }
    }

    private void OnBuyBtnClick()
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
            panel.SetCallback(paymentType => { Purchase(paymentType,productInfo,packViewConfig.productName); });
            return;
        }

        Purchase(ConfirmPaymentPanel.PaymentType.Default,_paidPackageListItem.productInfo,packViewConfig.productName);
    }
}
