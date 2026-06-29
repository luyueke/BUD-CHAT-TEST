using System;
using System.Collections;
using System.Collections.Generic;
using Game.Event;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class OcActivityView : ActivityBaseView
{
    [SerializeField] private RawImage Tex_BG;
    [SerializeField] private Text Txt_CurrencyAmount;
    [SerializeField] private CButton _btn_RewardPreview;
    [SerializeField] private CButton btnGoCreate;
    [SerializeField] private CButton allClaimBtn;
    [Header("下方领奖Item")]
    [SerializeField] private Text Txt_LeftTime;
    [SerializeField] private Transform rewardItemContent;
    [SerializeField] private OcActivityRewardItem _rewardItem;
    [SerializeField] private Image progressLable;
    
    [Header("右侧任务页面")]
    [SerializeField] private OcActivityDailyView _dailyView;
    
    private bool isSending = false;

    private List<OcActivityRewardItem> eventViews = new List<OcActivityRewardItem>();

    private ActivityInfo _info;
    public const string atlasPath = "Assets/Loadable/UI/ActivityCenterPanel/OCActivity/OCActivityAtlas.spriteatlas";
    public override void Init(ActivityInfo info) {
        base.Init(info);
        this._info = info;
        _btn_RewardPreview.onClick.AddListener(OnBtnRewardPreviewClick);
        btnGoCreate.onClick.AddListener(OnBtnGoCreateClick);
        allClaimBtn.onClick.AddListener(OnClaimAll);
        isSending = false;
        InitRewardItemView();
        InitDailyView();
        UpdateCurrency(_info.currencyAmount);
        allClaimBtn.interactable = false;
    }

    private void OnClaimAll()
    {
        string activityId = _info.activityId;
        if (string.IsNullOrEmpty(activityId))
        {
            return;
        }

        if (isSending)
        {
            return;
        }

        isSending = true;
        EventCenterDataManager.Inst.CliamReward( TASK_ID.S15TheaterDaily.ToString() , 0, 2, 0, (claimRspData) =>
        {
            List<TaskClaimRewardData> rewardList = claimRspData.rewardList;
            ShowRewardPanel(rewardList);
            MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
            isSending = false;
        }, () => {
            isSending = false;
            Debug.LogError("相机活动，任务一键领取失败 ");
        });
        //JObject jObject = new JObject()
        //{
        //    ["activityId"] = activityId,
        //    ["isAll"] = 1
        //};
        //NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
        //    HttpMethod.POST,
        //    JsonConvert.SerializeObject(jObject),
        //    (content) =>
        //    {
        //        ActivityEventClaimResponse activityEventClaimResponse =   JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
        //        isSending = false;
        //    var rewardList = activityEventClaimResponse.rewardList;
        //    var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        //        List<CommonRewardItemData> items = new List<CommonRewardItemData>();
        //        for (int i = 0; i < rewardList.Count; i++)
        //        {
        //            CommonRewardItemData item;
        //            item = new CommonRewardItemData()
        //            {
        //                RewardAmount = rewardList[i].amount,
        //                rewardType = rewardList[i].rewardType,
        //                rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardList[i].rewardType),
        //            };
        //            if (rewardList[i].pgcIdList?.Count > 0)
        //            {
        //                item.pgcId = rewardList[i].pgcIdList[0];
        //            }
        //            items.Add(item);
        //        }
        //        panel.ShowRewards(items);
        //        AccountDataManager.Inst.BalanceInfo.Refresh();
        //    },
        //    (error) => { isSending = false; });
    }

    private void ShowRewardPanel(List<TaskClaimRewardData> data)
    {
        if(data?.Count >0)
        {
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            var rewardList = new List<CommonRewardItemData>();
            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
            commonRewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(OcActivityView.atlasPath, "icon", gameObject); ;
            commonRewardItemData.rewardName = "活跃度";
            commonRewardItemData.RewardAmount = data[0].amount;
            rewardList.Add(commonRewardItemData);

            panel.ShowRewards(rewardList);
        }

    }

    public override void RefrashData(ActivityInfo info) {
        //Debug.LogError("CameraActivityView RefrashData info.eventlist="+JsonConvert.SerializeObject(info.eventList));
        base.RefrashData(info);
        _info = info;

        //allClaimBtn.interactable = CanClaim();
        RefreshRewardItems();
        //_dailyView.RefreshListUI(_info.eventList);
        RequestTaskData();
        UpdateCurrency(_info.currencyAmount);

        Txt_LeftTime.text = string.Format("距活动结束还有: {0}", info.leftTime);
    }


    private bool CanClaim()
    {
        if (_info == null || _info.eventList == null)
        {
            return false;
        }
        var reward = _info.eventList.Find(p => p.eventStatus == (int)ClaimStatus.Unlocked);
        if (reward != null) return true;

        return false;
    }

    private bool TaskCanClaim(TaskListRsp rsp)
    {
        if (rsp == null || rsp.list?.Count ==0)
        {
            return false;
        }
        for(int i = 0;i<rsp.list.Count;i++)
        {
            var reward = rsp.list[i].eventList.Find(p => p.eventStatus == (int)TaskClaimState.Enable);
            if (reward != null) return true;
        }
        return false;
    }
    public void RequestTaskData()
    {
        //GetTaskListReq getTaskListReq = new GetTaskListReq()
        //{
        //    idList = new List<string> { TASK_ID.S15TheaterDaily.ToString() ,TASK_ID.S15TheaterDailyGrandTotal.ToString()}
        //};

        //var reqParam = JsonConvert.SerializeObject(getTaskListReq);

        //NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST, reqParam, content =>
        //{
        //    var taskListRsp = JsonConvert.DeserializeObject<TaskListRsp>(content);
        //    _dailyView.RefreshListUI(taskListRsp.list);
        //    //allClaimBtn.interactable =TaskCanClaim(taskListRsp);

        //}, failMessage =>
        //{
        //    Debug.LogError("RequestTaskData error =" + failMessage);
        //});

        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string>
        {
            ActivityId.S15TheaterDaily.ToString()
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (content) =>
            {
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (activityResponse.list != null)
                {
                    //var taskListRsp = JsonConvert.DeserializeObject<TaskListRsp>(content);
                    _info.currencyAmount = activityResponse.list[0].currencyAmount;
                    InitProgress();
                    _dailyView.RefreshListUI(activityResponse.list);
                    //OnGetActivityListSuccess(activityResponse.list, isClaimSuccess);
                }
            },
            (error) =>
            {
            });
    }
    private void OnBtnRewardPreviewClick()
    {
        if (_info == null)
        {
            return;
        }
        var panel = UIManager.Inst.OpenPanel<ActivityRewardPanel>(PanelId.ActivityRewardPanel,ActivityId.AnimeShoppingFestival);
        panel.SetEventPreview(_info, "在方寸剧场，遇见万千心动！", Tex_BG.texture,atlasPath);
    }

    private void OnBtnGoCreateClick()
    {
        UIManager.Inst.OpenPanel(PanelId.TheatreStudioCategoryPanel);
    }

    #region 初始化下方RewardItem

    private void InitRewardItemView()
    {
        InitListUI(_info.eventList, _info.rewardList);
        InitProgress();
    }

    private void InitProgress()
    {
        float progress = 0;
        if (_info.currencyAmount != 0)
        {
            InitProgress(_info.currencyAmount);
        }
    }
    
    //public int[] nodes = { 50, 100, 200, 300, 500, 800, 1000 }; // 节点数组
    public int[] nodes = { 50, 200, 300, 500, 600, 800, 1000 }; // 节点数组
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
            progressLable.fillAmount = 0f;
            return;
        }

        // 如果 currency 大于等于最后一个节点
        if (currency >= nodes[nodes.Length - 1])
        {
            progressLable.fillAmount = 1f;
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
                progressLable.fillAmount = Mathf.Lerp(startFill, endFill, segmentProgress);
                return;
            }
        }
    }

    private void InitListUI(List<ActivityEventInfo> eventDatas, List<ActivityRewardInfo> rewardDatas)
    {
        if (eventDatas == null || eventDatas.Count == 0 || rewardDatas == null || rewardDatas.Count == 0)
        {
            return;
        }

        for (int i = 0; i < rewardDatas.Count; i++)
        {
            var obj = Instantiate(_rewardItem, rewardItemContent);
            obj.transform.localScale = Vector3.one;
            obj.gameObject.SetActive(true);
            OcActivityRewardItem eventItem = obj.GetComponent<OcActivityRewardItem>();
            var eventData = eventDatas.Find(x => x.eventId == rewardDatas[i].rewardId);
            eventItem.Init(eventData, rewardDatas[i], ClaimRewardItem);
            eventViews.Add(eventItem);
        }
    }
    
    public void RefreshRewardItems()
    {
        eventViews.ForEach(rewardItem =>
        {
            var curTargetData = _info.eventList.Find(actInfo => actInfo.eventId == rewardItem.eventId);
            if(curTargetData != null)
                rewardItem.Refresh(curTargetData);
        });
    }
    
    private void ClaimRewardItem(ActivityEventInfo eventInfo) {
        //ShowRewardPanel(eventInfo);
        //OnBtnRewardPreviewClick();
        //return;
        if ((ClaimStatus)eventInfo.eventStatus == ClaimStatus.Claimed) {
            return;
        }

        if ((ClaimStatus)eventInfo.eventStatus == ClaimStatus.Lock) {
            // TipPanel.ShowToast("当前购物车余额不足");
            OnBtnRewardPreviewClick();
            return;
        }


        if (isSending)
        {
            return;
        }
        isSending = true;

        //JObject jObject = new JObject()
        //{
        //    ["activityId"] = _info.activityId,
        //    ["eventId"] = eventInfo.eventId
        //};
        //NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
        //    HttpMethod.POST,
        //    JsonConvert.SerializeObject(jObject),
        //    (content) =>
        //    {
        //        ActivityEventClaimResponse activityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
        //        isSending = false;
        //        OnClaimRewardItemSuccess(activityEventClaimResponse);
        //    },
        //    (error) =>
        //    {
        //        isSending = false;
        //    });


        JObject jObject = new JObject()
        {
            ["activityId"] = _info.activityId,
            ["eventId"] = eventInfo.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse activityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                //OnClaimSuccess(activityEventClaimResponse, rewardItem);
                OnClaimRewardItemSuccess(activityEventClaimResponse);
            },
            (error) =>
            {
                isSending = false;
            });
    }
    
    private void OnClaimRewardItemSuccess(ActivityEventClaimResponse response) {
        if (this == null || gameObject == null) {
            return;
        }
        var eventInfo = _info.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        var rewardInfo = _info.rewardList.Find(x => x.rewardId == response.eventInfo.eventId);
        if (eventInfo==null)
        {
            return;
        }

        eventInfo.eventStatus = response.eventInfo.eventStatus;
        RefreshRewardItems();

        //var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        //if (eventInfo.rewardType == (int)BUDRewardType.RewardPgcResource)
        //{
        //    panel.ShowPgcRewards(new List<string>() {eventInfo.pgcId}, rewardInfo.rewardName);
        //}
        //else
        //{


        //}
        ShowRewardPanel(eventInfo);
        UpdateRedDot();

    }

   private void ShowRewardPanel(ActivityEventInfo eventInfo)
    {
        var rewardInfo = _info.rewardList.Find((v) => v.rewardId == eventInfo.eventId);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        var rewardList = new List<CommonRewardItemData>();
        CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
        if (rewardInfo.budRewardType == (int)BUDRewardType.RewardTypeCameraPose || rewardInfo.budRewardType == (int)BUDRewardType.RewardTypeSelfDefine)
        {
            commonRewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, rewardInfo.rewardIcon, gameObject);
        }else
        {
            commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)eventInfo.rewardType, gameObject);
        }
        commonRewardItemData.rewardName = rewardInfo.rewardName;
        commonRewardItemData.RewardAmount = rewardInfo.rewardNum;
        rewardList.Add(commonRewardItemData);

        panel.ShowRewards(rewardList);
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }
    #endregion
    

    #region 每日任务相关
    //初始化每日任务
    private void InitDailyView()
    {
        _dailyView.InitListUI(_info.eventList, _info.activityId);
        _dailyView.ClaimSuccessAction = ClaimSuccessHandler;
    }
    
    private void ClaimSuccessHandler(ActivityEventClaimResponse res) {
        if (res == null) {
            return;
        }
        
        var d = _info.eventList.Find(x => x.eventId == res.eventInfo.eventId);
        if (d == null) {
            return;
        }
        _info.currencyAmount = res.currencyAmount;
        d.eventStatus = (int)TaskClaimState.Finished;
        UpdateCurrency(res.currencyAmount);
        InitProgress();
        UpdateRedDot();
        //_dailyView.RefreshReddot(_info.eventList);
        
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }
    #endregion
    
    private void UpdateCurrency(int currencyAmount)
    {
        Txt_CurrencyAmount.text = currencyAmount.ToString();
    }
}

