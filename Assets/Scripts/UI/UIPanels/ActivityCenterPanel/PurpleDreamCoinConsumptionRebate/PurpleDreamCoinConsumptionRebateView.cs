using System.Collections.Generic;
using Basic.Utils;
using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class PurpleDreamCoinConsumptionRebateView : ActivityBaseView
{
    [SerializeField] private RawImage Tex_BG;
    [SerializeField] private Text Txt_CurrencyAmount;
    [Header("下方领奖Item")]
    [SerializeField] private Transform rewardItemContent;
    [SerializeField] private PurpleDreamCoinConsumptionRebateRewardItem _rewardItem;
    [SerializeField] private Image progressLable;
    
    private bool isSending = false;
    private List<PurpleDreamCoinConsumptionRebateRewardItem> eventViews = new List<PurpleDreamCoinConsumptionRebateRewardItem>();
    private ActivityInfo _info;
    private string atlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";


    public override void Init(ActivityInfo info) {
        base.Init(info);
        this._info = info;
        isSending = false;
        InitRewardItemView();
        UpdateCurrency(_info.currencyAmount);
    }
    
    public override void RefrashData(ActivityInfo info) {
        base.RefrashData(info);
        _info = info;
        RefreshRewardItems();
        UpdateCurrency(_info.currencyAmount);
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
    
    public int[] nodes = { 6, 30, 68, 168, 388, 568, 888 }; // 节点数组
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
            PurpleDreamCoinConsumptionRebateRewardItem eventItem = obj.GetComponent<PurpleDreamCoinConsumptionRebateRewardItem>();
            var eventData = eventDatas.Find(x => x.eventId == rewardDatas[i].rewardId);
            eventItem.Init(eventData, rewardDatas[i], ClaimRewardItem, GoPreviewPage);
            eventViews.Add(eventItem);
        }
    }

    private void GoPreviewPage(ActivityRewardInfo rewardInfo)
    {
        if (_info == null)
        {
            return;
        }
        BUDRewardType rewardType = (BUDRewardType)rewardInfo.budRewardType;
        if (rewardType == BUDRewardType.RewardPurpleDreamCoin)
        {
            CurrencyType currencyType = GameUtils.ConvertRewardType(rewardInfo.budRewardType);
            UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, currencyType);
        }
        else if (rewardType == BUDRewardType.RewardPgcResource)
        {
            var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
            panel.SetEventPreview(new List<string>(){rewardInfo.pgcId},rewardInfo.rewardName , "","","消费紫梦币，领限定动作", null,Tex_BG.texture);
        }
    }
    
    public void RefreshRewardItems()
    {
        if(_info.eventList == null)
            return;
        
        if (eventViews.Count != _info.eventList.Count)
        {
            LoggerUtils.LogError("[SunnyDoll] Refresh Event ItemView fail");
            return;
        }

        for (int i = 0; i < _info.eventList.Count; i++)
        {
            eventViews[i].Refresh(_info.eventList[i]);
        }
    }
    
    private void ClaimRewardItem(ActivityEventInfo eventInfo) {
        if ((ClaimStatus)eventInfo.eventStatus == ClaimStatus.Claimed) {
            return;
        }

        if ((ClaimStatus)eventInfo.eventStatus == ClaimStatus.Lock) {
            if (eventInfo.rewardType == (int)BUDRewardType.RewardPgcResource && !string.IsNullOrEmpty(eventInfo.pgcId))
            {
            }
            return;
        }


        if (isSending)
        {
            return;
        }
        isSending = true;

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

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        if (eventInfo.rewardType == (int)BUDRewardType.RewardPgcResource)
        {
            panel.ShowPgcRewards(new List<string>() {eventInfo.pgcId}, rewardInfo.rewardName);
        }
        else
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            var rewardList = new List<CommonRewardItemData>();
            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
            if (!string.IsNullOrEmpty(rewardInfo.rewardIcon) &&  (BUDRewardType)eventInfo.rewardType != BUDRewardType.RewardPurpleDreamCoin)
            {
                commonRewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, rewardInfo.rewardIcon, gameObject);
                commonRewardItemData.rewardName = rewardInfo.rewardName;
                commonRewardItemData.RewardAmount = 1;
            }
            else
            {
                commonRewardItemData.IconSp = (BUDRewardType)eventInfo.rewardType != BUDRewardType.RewardPgcResource ? PgcUtils.LoadRewardIcon((BUDRewardType)eventInfo.rewardType, gameObject) : PgcUtils.GetIconSpriteByPgcId(eventInfo.pgcId, gameObject);
                commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)eventInfo.rewardType);
                commonRewardItemData.RewardAmount = response.claimAmount;
            }
            rewardList.Add(commonRewardItemData);

            panel.ShowRewards(rewardList);
        }

        UpdateRedDot();

    }
    #endregion
    
    private void UpdateCurrency(int currencyAmount)
    {
        Txt_CurrencyAmount.text = currencyAmount.ToString();
    }
}

