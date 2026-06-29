using System;
using System.Collections.Generic;
using Basic.Extensions;
using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;
using EventTracking;
using UI.Manager;
using GameData.Manager;

public class AnniversaryCumulativeView : MonoBehaviour
{
    [Header("UI相关")] public Text Txt_ChargeCount;
    public Text Txt_ChargeCountHide;
    public CButton Btn_Close;
    public CButton Btn_ShowChargeCount;
    public CButton Btn_HideChargeCount;
    public CButton Btn_GoCharge;
    public GameObject Go_Tips;

    public Text lastTime;

    // 活动固定时间范围
    private static readonly DateTime ACTIVITY_START_TIME = new DateTime(2025, 7, 25);
    private static readonly DateTime ACTIVITY_END_TIME = new DateTime(2025, 9, 1);

    [Header("滑动条相关")] public Transform ChargeItemContent;
    public AnniversaryCumulativeItem ItemPrefab;
    public Image Img_ChargeProgress;

    private List<AnniversaryCumulativeItem> seasonCumulativeItems = new List<AnniversaryCumulativeItem>();

    private ActivityInfo _activityInfo;
    private bool isSending = false;
    private bool isInit = false;

    private List<RewardItem> _rewardItems = new List<RewardItem>();
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/AnniversaryCumulativeView/icons.spriteatlas";

    private void Start()
    {
        InitUI();
        GenerateContent();
        GetDataByHttp();
        UpdateLastTime();
    }

    private void GenerateContent()
    {
        if (isInit)
        {
            return;
        }

        isInit = true;
        string jsonPath = "Assets/Loadable/UI/UIPanel/AnniversaryCumulativeView/AnniversaryCumulativeData.json";
        var ugcAsset =
            Loader.Load<TextAsset>(
                jsonPath, this.gameObject);
        _rewardItems = JsonConvert.DeserializeObject<List<RewardItem>>(ugcAsset.text);

        for (var i = 0; i < _rewardItems.Count; i++)
        {
            var taskItem = Instantiate(ItemPrefab, ChargeItemContent);
            taskItem.transform.localScale = Vector3.one;
            taskItem.gameObject.SetActive(true);
            taskItem.OnInitCreate(_rewardItems[i]);
            seasonCumulativeItems.Add(taskItem);

        }

    }

    private void GetDataByHttp()
    {
        AnniversaryCumulativeMgr.Inst.GetDataByHttp(OnGetActivityListSuccess);
    }

    private void OnGetActivityListSuccess(List<ActivityInfo> activityList)
    {
        string activityConfigPath = "Assets/Loadable/UI/RechargePanel/CumulativeRechargePanel/Prefabs/SeasonCumulativeView.json";
        var textAsset = Loader.Load<TextAsset>(activityConfigPath, gameObject);
        if (textAsset == null)
        {
            return;
        }

        var activityInfo = activityList.Find(x => x.activityId == ActivityId.AnniversaryEvent.ToString());
        if (activityInfo == null)
        {
            return;
        }

        this._activityInfo = activityInfo;
        this.Txt_ChargeCount.text = this._activityInfo.currencyAmount.ToString();

        InitProgress(this._activityInfo.currencyAmount);
        InitListUI(activityInfo.eventList);
        ChargeItemContent.gameObject.SetActive(true);
    }

    private void InitListUI(List<ActivityEventInfo> eventDatas)
    {
        if (eventDatas == null || eventDatas.Count == 0)
        {
            return;
        }

        for (int i = 0; i < eventDatas.Count; i++)
        {
            seasonCumulativeItems[i].Init(eventDatas[i], ClaimRewardItem);
        }
    }



    private void ClaimRewardItem(ActivityEventInfo eventInfo, RewardItem rewardItem)
    {
        if ((ClaimStatus)eventInfo.eventStatus == ClaimStatus.Claimed)
        {
            return;
        }

        if ((ClaimStatus)eventInfo.eventStatus == ClaimStatus.Lock)
        {
            return;
        }


        if (isSending)
        {
            return;
        }

        isSending = true;

        JObject jObject = new JObject()
        {
            ["activityId"] = _activityInfo.activityId,
            ["eventId"] = eventInfo.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse activityEventClaimResponse =
                    JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimRewardItemSuccess(activityEventClaimResponse, rewardItem);
                TokenDataManager.Inst.GetTokenData();
                AnniversaryCumulativeMgr.Inst.GetDataByHttp(null);
                AccountDataManager.Inst.RefreshUserInfo(); //刷新UserInfo
            },
            (error) => { isSending = false; });
    }

    private void OnClaimRewardItemSuccess(ActivityEventClaimResponse response, RewardItem rewardItem)
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        var eventInfo = _activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null)
        {
            return;
        }

        eventInfo.eventStatus = response.eventInfo.eventStatus;
        RefreshRewardItems();

        ShowRewards(rewardItem);
        ReddotManagerUtils.Inst.RefreshRedDot();
        VipDataManager.Inst.UpdateVipStatus();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    private void ShowRewards(RewardItem rewardItem)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new List<CommonRewardItemData>();
        if (rewardItem.rewardIcon2.IsNullOrEmpty())
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = rewardItem.rewardId==7.ToString() ? ProfileThemeManager.Inst.LoadThemeIcon(11, gameObject) : PgcUtils.LoadCurrencyIcon((CurrencyType)rewardItem.rewardType1, gameObject),
                RewardAmount = rewardItem.rewardNum1,
                rewardName = rewardItem.rewardName1
            });
        }
        else
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = PgcUtils.LoadCurrencyIcon((CurrencyType)rewardItem.rewardType1, gameObject),
                RewardAmount = rewardItem.rewardNum1,
                rewardName = rewardItem.rewardName1
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = rewardItem.rewardId == 6.ToString() ? XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "ChatBubbleIcon_15",
                    gameObject) : XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon2,
                    gameObject),
                RewardAmount = rewardItem.rewardNum2,
                rewardName = rewardItem.rewardName2
            });
        }
        panel.ShowRewards(rewardItemDatas);

    }

    public void RefreshRewardItems()
    {
        if (_activityInfo.eventList == null)
            return;

        if (seasonCumulativeItems.Count != _activityInfo.eventList.Count)
        {
            LoggerUtils.LogError("[SunnyDoll] Refresh Event ItemView fail");
            return;
        }

        for (int i = 0; i < _activityInfo.eventList.Count; i++)
        {
            seasonCumulativeItems[i].Refresh(_activityInfo.eventList[i]);
        }
    }

    public int[] nodes = { 0, 6, 30, 98, 198, 368, 688, 1088 };
    public float[] progressValues = { 0f, 0.038f, 0.2f, 0.345f, 0.51f, 0.686f, 0.84f, 1f };
    /// <summary>
    /// 更新进度条显示
    /// </summary>
    /// <param name="currency">当前值</param>
    public void InitProgress(int currency)
    {
        // 检查节点和进度值数组是否有效
        if (nodes == null || progressValues == null || nodes.Length != progressValues.Length || nodes.Length < 2)
        {
            return;
        }

        // --- 1. 处理边界情况 ---

        // 如果当前值小于或等于第一个节点值
        if (currency <= nodes[0])
        {
            Img_ChargeProgress.fillAmount = progressValues[0];
            return;
        }

        // 如果当前值大于或等于最后一个节点值
        if (currency >= nodes[nodes.Length - 1])
        {
            Img_ChargeProgress.fillAmount = progressValues[progressValues.Length - 1];
            return;
        }

        // --- 2. 查找当前值所在的区间，并进行分段线性插值 ---

        for (int i = 1; i < nodes.Length; i++)
        {
            // 找到了 currency 所在的区间 [nodes[i-1], nodes[i]]
            if (currency <= nodes[i])
            {
                // 获取区间的起点和终点值
                float startNodeValue = nodes[i - 1];
                float endNodeValue = nodes[i];

                // 获取区间对应的起始和结束百分比
                float startProgress = progressValues[i - 1];
                float endProgress = progressValues[i];

                // 计算当前值在当前数值区间内的进度 (0到1之间)
                // (当前值 - 区间起点) / (区间终点 - 区间起点)
                float segmentT = (currency - startNodeValue) / (endNodeValue - startNodeValue);

                // 使用这个进度(t)在起始和结束百分比之间进行线性插值
                float fillAmount = Mathf.Lerp(startProgress, endProgress, segmentT);

                Img_ChargeProgress.fillAmount = fillAmount;
                return; // 找到区间后就完成计算，退出函数
            }
        }
    }
    private void UpdateLastTime()
    {
        if (lastTime == null) return;

        // 获取当前时间
        DateTime now = DateTime.Now;

        // 如果已经超过结束时间
        if (now >= ACTIVITY_END_TIME)
        {
            lastTime.text = "活动已结束";
            return;
        }

        // 如果还没到开始时间
        if (now < ACTIVITY_START_TIME)
        {
            lastTime.text = "活动未开始";
            return;
        }

        // 计算剩余时间
        TimeSpan remainingTime = ACTIVITY_END_TIME - now;

        // 格式化显示
        string timeText = string.Format("{0}天{1}小时",
            remainingTime.Days,
            remainingTime.Hours);

        lastTime.text = timeText;
    }
    private void InitUI()
    {
        ChargeItemContent.gameObject.SetActive(false);
        Txt_ChargeCount.gameObject.SetActive(false);
        Txt_ChargeCountHide.gameObject.SetActive(true);
        Btn_ShowChargeCount.gameObject.SetActive(true);
        Btn_HideChargeCount.gameObject.SetActive(false);

        // Btn_Close.onClick.AddListener(CloseSelf);
        Btn_ShowChargeCount.onClick.AddListener(() => { SetChargeCountEnable(true); });
        Btn_HideChargeCount.onClick.AddListener(() => { SetChargeCountEnable(false); });
        Btn_GoCharge.onClick.AddListener(OnBtnGoChargeClick);
    }

    private void SetChargeCountEnable(bool enable)
    {
        Btn_ShowChargeCount.gameObject.SetActive(!enable);
        Btn_HideChargeCount.gameObject.SetActive(enable);

        Txt_ChargeCount.gameObject.SetActive(enable);
        Txt_ChargeCountHide.gameObject.SetActive(!enable);
    }

    private void SetTipsEnable(bool enable)
    {
        Go_Tips.SetActive(enable);
    }

    private void OnBtnGoChargeClick()
    {
        if (UIManager.Inst.TryFindPanel<RechargePanel>(PanelId.RechargePanel, out var panel))
        {
            panel.OnTabClick(RechargeId.GemPack);
        }
        else
        {
            panel = UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel);
            panel.OnTabClick(RechargeId.GemPack);
        }
    }
}