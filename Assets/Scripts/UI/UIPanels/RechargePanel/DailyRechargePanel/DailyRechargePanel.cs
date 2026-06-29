using System.Collections.Generic;
using System.Linq;
using Game.Event;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Basic.Extensions;
using Basic.Utils;
using System;
using GameData;
using UnityEngine.U2D;


/// <summary>
/// 每日充值面板
/// </summary>
public class DailyRechargePanel : BasePanel<DailyRechargePanel>
{
    [SerializeField] private GameObject rewardItemPrefab;  // 每日奖励项预制体
    [SerializeField] private Transform rewardContent;      // 奖励容器
    [SerializeField] private CButton TipsBtn;             // 返回按钮
    [SerializeField] private CButton buyBtn;              // 购买按钮
    [SerializeField] private Text TipsText;           // 进度文本
    [SerializeField] private Text pysText;               // 提示文本
    [SerializeField] private Slider  slider;
    public GameObject tipsGame;

    private readonly List<double> rewardSliderVls = new List<double>() {  0, 0.074 ,0.232, 0.36, 0.5, 0.653, 0.787, 1 };  // 滑动条的value
    public Text IUILifetime;
    private RawImage bg;
    private List<DailyItem> dailyRewardItems = new List<DailyItem>();
    private readonly List<int> rewardEventIds = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };  // 7天奖励的事件ID
    private bool isSending;
    private ActivityInfo activityInfo;
    private bool isRequesting = false;
    int cnt = 0;
    private const string ACTIVITY_ID = "ReChargeDaily";  // 活动ID
    private const int DAILY_RECHARGE_AMOUNT = 6;  // 每日充值金额

    private string spriteatlasPath = "Assets/Loadable/UI/RechargePanel/DailyRechargePanel/DailyRecharge.spriteatlas"; //图集地址
    private void Start()
    {
        InitUI();

    }

    private void OnEnable()
    {
        GetDataByHttp();//跳转到购买页面，回来要刷新状态
    }


    void InitUI()
    {
        slider = transform.Find("BaseLayout2D/Down/Slider").GetComponent<Slider>();
        buyBtn = transform.Find("BaseLayout2D/PayCButton").GetComponent<CButton>();
        TipsBtn = transform.Find("BaseLayout2D/LuleCButton").GetComponent<CButton>();

        GenerateContent();
        GetDataByHttp();
        
    }

    public void OnLuleClick()
    {
        UIManager.Inst.OpenPanel<ActivityRulePanel>(PanelId.ActivityRulePanel, "Assets/Loadable/UI/RechargePanel/DailyRechargePanel/Rule.json");
    }
    public void OnBuyClick()
    {
        if (UIManager.Inst.TryFindPanel<RechargePanel>(PanelId.RechargePanel, out var panel))
        {
            panel.OnTabClick(RechargeId.GemPack);
        }
    }

    public override void OnCreate()
    {
        base.OnCreate();
    }


    private void GenerateContent()
    {
        // 遍历所有子物体
        foreach (Transform child in rewardContent)
        {
            DailyItem item = child.GetComponent<DailyItem>();
            if (item != null)
            {
                dailyRewardItems.Add(item);
                LoggerUtils.Log($"找到DailyItem: {item.gameObject.name}");
                
            }
        }

        

    }


    private void GetDataByHttp(bool isClaimSuccess = false)
    {
        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string>
    {
        ACTIVITY_ID
    };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (content) =>
            {
                try
                {
                    ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                    if (activityResponse?.list == null || activityResponse.list.Count == 0)
                    {
                        Debug.LogWarning("Daily recharge activity data is empty");
                        return;
                    }

                    foreach (var activity in activityResponse.list)
                    {
                        IUILifetime.text = "活动时间：" + activity.leftTime;
                        pysText.text = activity.currencyAmount.ToString();

                        if (activity.eventList != null)
                        {
                        // 检查数组长度
                        if (dailyRewardItems == null)
                            {
                                Debug.LogError("dailyRewardItems is null");
                                return;
                            }

                            cnt = 0;
                            foreach (var Event in activity.eventList)
                            {
                            // 检查索引是否越界
                            if (cnt >= dailyRewardItems.Count)
                                {
                                    Debug.LogWarning($"Event list count ({activity.eventList.Count}) exceeds dailyRewardItems length ({dailyRewardItems.Count})");
                                    break;
                                }
                                dailyRewardItems[cnt].eventInfo = Event;
                                dailyRewardItems[cnt++].Init(OnClaimCallBack);
                            }

                            cnt = 0;
                            if (activity.rewardList != null)
                            {
                                foreach (var reward in activity.rewardList)
                                {
                                // 检查索引是否越界
                                if (cnt >= dailyRewardItems.Count)
                                    {
                                        Debug.LogWarning($"Reward list count ({activity.rewardList.Count}) exceeds dailyRewardItems length ({dailyRewardItems.Count})");
                                        break;
                                    }
                                    dailyRewardItems[cnt++].rewardItem = reward;
                                }
                            }
                        }
                    }

                    OnGetActivityListSuccess(activityResponse.list, isClaimSuccess);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing daily recharge data: {e.Message}\nStack: {e.StackTrace}");
                }
            },
            (error) =>
            {
                LoggerUtils.LogError($"[DailyRecharge] 请求失败: {error}");
            });
        UpdateSliderProgress();
    }


    private void OnGetActivityListSuccess(List<ActivityInfo> activityList, bool isClaimSuccess)
    {   
        var activityInfo = activityList.Find(x => x.activityId == ACTIVITY_ID);
        if (activityInfo == null) return;

        this.activityInfo = activityInfo;
        
        SetupUI(activityInfo);
        UpdateSliderProgress();
    }

    private void SetupUI(ActivityInfo activityInfo)
    {
        // 设置每个奖励项的状态
        for (int i = 0; i < rewardEventIds.Count; i++)
        {
            var eventInfo = activityInfo.eventList.FirstOrDefault(tmp => rewardEventIds[i] == tmp.eventId);
            if (eventInfo == null) continue;  
        }
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response, ActivityRewardInfo rewardItem)
    {
        try
        {
            var rewardList = new List<CommonRewardItemData>();
            
            // 根据rewardType获取正确的图标名称
            string iconName = response.rewardList[0].rewardType.ToString();

            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, iconName, gameObject);

            rewardList.Add(new CommonRewardItemData()
            {
                IconSp = sprite,
                RewardAmount = response.rewardList[0].amount,
                rewardName = GetRewardName(response.rewardList[0].rewardType)
            });

            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            if (panel != null)
            {
                panel.ShowRewards(rewardList);;
            }
        }
        catch (Exception e)
        {
            LoggerUtils.LogError($"❌ 显示奖励面板时出错: {e.Message}\n{e.StackTrace}");
        }

        // 1. 更新事件状态
        var eventInfo = activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null) return;
        eventInfo.eventStatus = response.eventInfo.eventStatus;
        
        // 2. 刷新所有DailyItem的状态
        for (int i = 0; i < dailyRewardItems.Count; i++)
        {
            var item = dailyRewardItems[i];
            if (item.eventInfo.eventId == response.eventInfo.eventId)
            {
                // 更新这个item的状态
                item.eventInfo = eventInfo;
                item.RefreshClaimStatus();  // 需要在DailyItem中实现这个方法
            }
        }

        // 3. 更新进度条
        UpdateSliderProgress();

        // 4. 刷新账户信息
        AccountDataManager.Inst.BalanceInfo.Refresh();

        // 5. 重新获取最新数据
        GetDataByHttp(true);

        // 6. 刷新红点
        ReddotManagerUtils.Inst.RefreshRedDot();

    }





    private void OnClaimCallBack(ActivityEventInfo eventInfo, ActivityRewardInfo rewardItem , CurrencyType whowID)
    {   


        // 如果状态不是可领取，则显示预览或提示
        if (eventInfo.eventStatus != (int)ClaimStatus.Unlocked) {
            if(whowID == CurrencyType.None)
            {
                tipsGame.SetActive(true);
                return;
            }

            UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, whowID);
            return;
        }
           
        
        // 发送领取奖励请求
        if (isSending) return;

        isSending = true;
        JObject jObject = new JObject()
        {
            ["activityId"] = activityInfo.activityId,
            ["eventId"] = eventInfo.eventId,
            ["isAll"] = 0,
            ["expireClaim"] = 0
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse activityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(activityEventClaimResponse, rewardItem);
            },
            (error) =>
            {
                LoggerUtils.LogError($"发送奖励失败！！，失败原因{error}");
                isSending = false;
            });
        GetDataByHttp()
;
    }

    private void UpdateSliderProgress()
    {

        // 计算已领取的奖励数量
        int claimedCount = 0;
        foreach (var item in dailyRewardItems)
        {   
            if(item.eventInfo!=null)
            if (item.eventInfo.eventStatus == (int)ClaimStatus.Claimed || item.eventInfo.eventStatus == (int)ClaimStatus.Unlocked)
            {
                claimedCount++;
            }
            else
            {
                break;  // 一旦遇到未领取的就停止计数
            }
        }

        // 设置进度条值
        if(slider!=null)
        if (claimedCount == 0)
        {
            slider.value = 0f;
        }
        else if (claimedCount <= rewardSliderVls.Count)
        {
            slider.value = (float)rewardSliderVls[claimedCount];
        }

    }

    private string GetRewardName(int rewardType)
    {
        switch (rewardType)
        {
            case 7:
                return "社区商品币"; 
            case 40:
                return "优优币"; 
            case 41:
                return "紫梦币"; 
            default:
                return "未知奖励";
        }
    }
}