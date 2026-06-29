using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Event;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewbieV2TaskBottomItem : MonoBehaviour
{

    public Text title;
    public CButton finishBtn;
    public CButton waitBtn;
    public Image finishIcon;
    public CButton claimBtn;
    public GameObject rewardItem;
    private TaskItemData _taskItemData;
    private RewardItem _rewardItem;
    private Action<TaskConfig> clickAction;
    private Action<TaskClaimRsp> claimAction;

    TaskConfig _taskConfig;
    public Sprite fireSp;
    string _taskId;
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/EventCenter/EventCenter.spriteatlas";
    private void Start()
    {
        finishBtn.onClick.AddListener(() =>
        {
            clickAction?.Invoke(_taskConfig);
        });
        claimBtn?.onClick.AddListener(OnClaimBtnClick);
    }
    public void OnInitCreate(TaskConfig taskItem)
    {
        this._taskConfig = taskItem;
        title.text = taskItem.title;

    }

    public void SetData(TaskItemData taskItemData,Action<TaskConfig> clickAction , Action<TaskClaimRsp> claimAction, string taskId = "")
    {
        if (taskItemData == null)
        {
            return;
        }
        this.clickAction = clickAction;
        this.claimAction = claimAction;
        _taskItemData = taskItemData;
        title.text = $"{_taskConfig.title}({taskItemData.finishAmount}/{_taskConfig.num})";

        if (taskItemData.eventStatus == (int)EventStatus.Default)
        {
            waitBtn.gameObject.SetActive(true);
            finishIcon.gameObject.SetActive(false);
            claimBtn?.gameObject.SetActive(false);
        }
        else
        {
            waitBtn?.gameObject.SetActive(false);
            claimBtn?.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.Claim);
            finishBtn.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.UnClaim);
            finishIcon.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.Finish);
        }
        _taskId = taskId;
        if (_taskId == "")
        {
            claimBtn?.gameObject.SetActive(false);
        }
        if (rewardItem != null)
        {   
            if(_taskConfig.rewardNum!=null)
            foreach (var reward in _taskConfig.rewardNum) {
                var item = reward.Split(",");
                // 克隆预制体
                GameObject cloneItem = GameObject.Instantiate(rewardItem, rewardItem.transform.parent);
                //设置icon图
                cloneItem.GetComponent<Image>().sprite = PgcUtils.LoadCurrencyIcon((CurrencyType)int.Parse(item[0]), cloneItem);
                // 找到num对象并设置文本
                Text numText = cloneItem.transform.Find("num").GetComponent<Text>();
                if (numText != null)
                {
                    numText.text = item[1];
                }
                // 激活物体
                cloneItem.SetActive(true);
            }
        }
        var a = transform.Find("Center").GetComponent<RectTransform>();
        // 强制刷新布局
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(a);

    }

    void OnClaimBtnClick()
    {
        EventCenterDataManager.Inst.CliamReward(this._taskId, this._taskItemData.eventId, 1, 0, (claimRspData) =>
        {
            List<TaskClaimRewardData> rewardList = claimRspData.rewardList;
           var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            List<CommonRewardItemData> items = new List<CommonRewardItemData>();
            this.claimAction?.Invoke(claimRspData);
            foreach (var reward in rewardList)
            {
                CommonRewardItemData item;
                if (reward.rewardType != 77)
                {//77是活跃度
                    item = new CommonRewardItemData()
                    {
                        RewardAmount = reward.amount,
                        rewardType = reward.rewardType,
                        rewardName = PgcUtils.GetRewardName((BUDRewardType)reward.rewardType)
                    };
                }
                else
                {
                    continue;
                }
                
                items.Add(item);
            }
            panel.ShowRewards(items);
            AccountDataManager.Inst.BalanceInfo.Refresh();
            VipDataManager.Inst.UpdateVipStatus();
            ReddotManagerUtils.Inst.RefreshRedDot();
        });
    }

}
