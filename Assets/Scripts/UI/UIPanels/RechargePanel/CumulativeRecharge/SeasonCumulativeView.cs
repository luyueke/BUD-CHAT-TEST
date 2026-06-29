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
public class SeasonCumulativeView : MonoBehaviour
{
    private RechargeId _rechargeId = RechargeId.ErrRechargeId;
    public RechargeId CurRechargeId => _rechargeId;

    public void Initialize(RechargeId id)
    {
        _rechargeId = id;
    }

    private ActivityId GetCurActivityId()
    {
        var currentSeason = SeasonPassDataManager.Inst.CurrentSeasonPassType;
        if (_rechargeId == RechargeId.S14SeasonRecharge && currentSeason == SeasonPassType.S14SeasonPass) return ActivityId.S14SeasonRecharge;
        if (_rechargeId == RechargeId.S15SeasonRecharge && currentSeason == SeasonPassType.S15SeasonPass) return ActivityId.S15SeasonRecharge;
        return currentSeason switch
        {
            SeasonPassType.S14SeasonPass => ActivityId.S14SeasonRecharge,
            SeasonPassType.S15SeasonPass => ActivityId.S15SeasonRecharge,
            _ => ActivityId.S15SeasonRecharge,
        };
    }

    [Header("UI相关")] public Text Txt_ChargeCount;
    public Text Txt_ChargeCountHide;
    public CButton Btn_Close;
    public CButton Btn_ShowChargeCount;
    public CButton Btn_HideChargeCount;
    public CButton Btn_GoCharge;
    public CButton Btn_Tips;
    public CButton Btn_CloseTips;
    public GameObject Go_Tips;
    public RawImage Tex_BG;

    public GameObject bg_s14;
    public GameObject bg_s15;
    public GameObject Txt_Title_s14;
    public GameObject Txt_Title_s15;

    [Header("滑动条相关")] public Transform ChargeItemContent;
    public SeasonCumulativeItem ItemPrefab;
    public SeasonCumulativeItem ItemPrefab_s15;
    public Image Img_ChargeProgress;

    private List<SeasonCumulativeItem> seasonCumulativeItems = new List<SeasonCumulativeItem>();

    private ActivityInfo _activityInfo;
    private bool isSending = false;
    private bool isInit = false;

    private List<RewardItem> _rewardItems = new List<RewardItem>();
    private string spriteatlasPath = RechargePanel.CumulativeRechargePanelAtlas;

    private void Start()
    {
        InitUI();
        GenerateContent();
        GetDataByHttp();

        SeasonCumulativeSystem.Inst.SetRed();
    }

    private void GenerateContent()
    {
        if (isInit)
        {
            return;
        }

        isInit = true;
        string jsonPath = GetCurActivityId() == ActivityId.S14SeasonRecharge
            ? "Assets/Loadable/UI/RechargePanel/CumulativeRechargePanel/Prefabs/SeasonData_s14.json"
            : "Assets/Loadable/UI/RechargePanel/CumulativeRechargePanel/Prefabs/SeasonData_s15.json";
        var ugcAsset =
            Loader.Load<TextAsset>(
                jsonPath, this.gameObject);
        _rewardItems = JsonConvert.DeserializeObject<List<RewardItem>>(ugcAsset.text);

        var prefab = GetCurActivityId() == ActivityId.S14SeasonRecharge ? ItemPrefab : ItemPrefab_s15;
        for (var i = 0; i < _rewardItems.Count; i++)
        {
            var taskItem = Instantiate(prefab, ChargeItemContent);
            taskItem.transform.localScale = Vector3.one;
            taskItem.gameObject.SetActive(true);
            taskItem.OnInitCreate(_rewardItems[i]);
            seasonCumulativeItems.Add(taskItem);
        }
        ChargeItemContent.gameObject.SetActive(true);
    }

    private void GetDataByHttp()
    {
        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string>
        {
            GetCurActivityId().ToString()
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (content) =>
            {
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (activityResponse.list != null)
                {
                    OnGetActivityListSuccess(activityResponse.list);
                }
            },
            (error) =>
            {
            });
    }

    private void OnGetActivityListSuccess(List<ActivityInfo> activityList)
    {
        string activityConfigPath = "Assets/Loadable/UI/RechargePanel/CumulativeRechargePanel/Prefabs/SeasonCumulativeView.json";
        var textAsset = Loader.Load<TextAsset>(activityConfigPath, gameObject);
        if (textAsset == null)
        {
            return;
        }

        var activityInfo = activityList.Find(x => x.activityId == GetCurActivityId().ToString());
        if (activityInfo == null)
        {
            return;
        }

        this._activityInfo = activityInfo;
        this.Txt_ChargeCount.text = this._activityInfo.currencyAmount.ToString();

        Dictionary<string, object> superProperties = AnalyticsManager.Inst.GetSuperProperties();
        superProperties["season_charge_amount"] = _activityInfo.currencyAmount;
        AnalyticsManager.Inst.SetSuperProperties(superProperties);  //上报赛季累充公共属性


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
                AccountDataManager.Inst.RefreshUserInfo(); //刷新UserInfo
            },
            (error) =>
            { isSending = false; });
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
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1,
                    gameObject),
                RewardAmount = rewardItem.rewardNum1,
                rewardName = rewardItem.rewardName1
            });
        }
        else
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1,
                    gameObject),
                RewardAmount = rewardItem.rewardNum1,
                rewardName = rewardItem.rewardName1
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon2,
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

    public int[] nodes = { 0, 6, 30, 68, 98, 198, 368, 688, 1088 }; // 节点数组

    /// <summary>
    /// 更新进度条显示
    /// </summary>
    /// <param name="currency">当前值</param>
    public void InitProgress(int currency)
    {
        if (nodes == null || nodes.Length < 2)
        {
            Debug.LogError("节点数组至少需要两个值");
            return;
        }

        // 如果 currency 小于第一个节点
        if (currency <= nodes[0])
        {
            Img_ChargeProgress.fillAmount = 0f;
            return;
        }

        // 如果 currency 大于等于最后一个节点
        if (currency >= nodes[nodes.Length - 1])
        {
            Img_ChargeProgress.fillAmount = 1f;
            return;
        }

        // 找到 currency 所属的段
        for (int i = 1; i < nodes.Length; i++)
        {
            if (currency <= nodes[i])
            {
                // 当前段的起点和终点
                int start = nodes[i - 1];
                int end = nodes[i];

                // 当前段占总进度条的比例
                float startFill = (float)(i - 1) / (nodes.Length - 1);
                float endFill = (float)i / (nodes.Length - 1);

                // 计算 currency 在当前段的相对进度
                float segmentProgress = (float)(currency - start) / (end - start);

                // 计算总进度条的 fillAmount
                Img_ChargeProgress.fillAmount = Mathf.Lerp(startFill, endFill, segmentProgress);
                return;
            }
        }
    }

    private void InitUI()
    {
        bool isS14 = GetCurActivityId() == ActivityId.S14SeasonRecharge;
        bg_s14.SetActive(isS14);
        bg_s15.SetActive(!isS14);
        Txt_Title_s14.SetActive(isS14);
        Txt_Title_s15.SetActive(!isS14);

        ChargeItemContent.gameObject.SetActive(false);
        Txt_ChargeCount.gameObject.SetActive(false);
        Txt_ChargeCountHide.gameObject.SetActive(true);
        Btn_ShowChargeCount.gameObject.SetActive(true);
        Btn_HideChargeCount.gameObject.SetActive(false);

        // Btn_Close.onClick.AddListener(CloseSelf);
        Btn_ShowChargeCount.onClick.AddListener(() => { SetChargeCountEnable(true); });
        Btn_HideChargeCount.onClick.AddListener(() => { SetChargeCountEnable(false); });
        Btn_GoCharge.onClick.AddListener(OnBtnGoChargeClick);

        Btn_Tips.onClick.AddListener(() => { SetTipsEnable(true); });
        Btn_CloseTips.onClick.AddListener(() => { SetTipsEnable(false); });
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
    }
}