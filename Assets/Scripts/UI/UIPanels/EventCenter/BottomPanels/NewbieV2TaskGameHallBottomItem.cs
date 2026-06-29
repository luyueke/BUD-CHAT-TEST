using System;
using System.Collections.Generic;
using Es;
using Game.Event;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewbieV2TaskGameHallBottomItem : MonoBehaviour
{

    public Text title;
    public Text desc;
    public Image finishIcon;
    private TaskItemData _taskItemData;
    private RewardItem _rewardItem;
    TaskConfig _taskItem;
    public CButton skipBtn;
    private Action<TaskConfig> clickAction;
    private Action _claimAction;
    string _taskId;

    public void OnInitCreate(TaskConfig taskItem)
    {
        this._taskItem = taskItem;
        title.text = taskItem.title;
        
    }
    
    public void SetData(TaskItemData taskItemData,Action<TaskConfig> clickAction , Action ClaimAction ,string taskId = "" )
    {
        if (taskItemData == null)
        {
            return;
        }
        this.clickAction = clickAction;
        _claimAction = ClaimAction;
        _taskItemData = taskItemData;
        skipBtn.onClick.AddListener(() => {
            if(taskItemData.eventStatus == (int)EventStatus.Claim)
            {
                OnClaimBtnClick();
            }
            else
            {
                clickAction.Invoke(_taskItem);
            }
        });
        desc.text = $"{_taskItem.title}";
        title.text = $"每日任务({taskItemData.finishAmount}/{_taskItem.num})";

        finishIcon.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.Claim);
        
        _taskId = taskId;
    }


    void OnClaimBtnClick()
    {
        EventCenterDataManager.Inst.CliamReward(this._taskId, this._taskItemData.eventId, 1, 0, (claimRspData) =>
        {
            List<TaskClaimRewardData> rewardList = claimRspData.rewardList;
           var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            List<CommonRewardItemData> items = new List<CommonRewardItemData>(); 
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
            _claimAction?.Invoke();
            AccountDataManager.Inst.BalanceInfo.Refresh();
            VipDataManager.Inst.UpdateVipStatus();
            ReddotManagerUtils.Inst.RefreshRedDot();
        });
    }

}
