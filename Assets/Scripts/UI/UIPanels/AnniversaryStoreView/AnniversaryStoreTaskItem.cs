using Es;
using Game.Event;
using GameData.Manager;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AnniversaryStoreTaskItem : MonoBehaviour
{
    // UI组件
    public Text title;
    public Slider slider;
    public Text targetNum;
    public Text num;
    public Button claimBtn;
    public Button goBtn;
    public GameObject overObj;

    public GameObject dailyImage;
    public GameObject weekImage;
    public GameObject ingImage;
    string _taskId;
    int _eventId;
    TaskConfig _config;
    public Action<TaskClaimRsp> claimAction;

    void SetStatus(int statu)
    {   
        if(_config.eventId == 1 || _config.eventId == 5 || (_config.eventId == 2 && _config.taskId == "S11CelebrationStoreWeeklyTask"))
        {   
            if(statu == (int)EventStatus.UnClaim)
            {
                goBtn.gameObject.SetActive(false);
                claimBtn.gameObject.SetActive(false);
                overObj.gameObject.SetActive(false);
                ingImage.gameObject.SetActive(true);
                return;
            }
        }
        switch (statu)
        {
            case (int)EventStatus.UnClaim:
                goBtn.gameObject.SetActive(true);
                claimBtn.gameObject.SetActive(false);
                overObj.gameObject.SetActive(false);
                break;
            case (int)EventStatus.Claim:
                goBtn.gameObject.SetActive(false);
                claimBtn.gameObject.SetActive(true);
                overObj.gameObject.SetActive(false);
                break;
            case (int)EventStatus.Finish:
                goBtn.gameObject.SetActive(false);
                claimBtn.gameObject.SetActive(false);
                overObj.gameObject.SetActive(true);
                break;
        }
    }

    public void SetData(TaskConfig config , TaskItemData serverData)
    {
        _config = config;
        title.text = config.title;
        slider.value = serverData.finishAmount / config.num;
        targetNum.text = $"{serverData.finishAmount}/{config.num}";
        num.text = config.rewardNum[0].Split(",")[1];
        _taskId = config.taskId;
        _eventId = config.eventId;
        if (config.taskId == "S11CelebrationStoreDailyTask")
        {
            dailyImage.SetActive(true);
            weekImage.SetActive(false);
        }
        else
        {
            dailyImage.SetActive(false);
            weekImage.SetActive(true);
        }
        SetStatus(serverData.eventStatus);
        goBtn.onClick.AddListener(()=> {
            NewbieTaskSkipManager.Inst.HandleSkip(config.progress[0]);
        });
        claimBtn.onClick.AddListener(OnClaimBtnClick);
    }
    void OnClaimBtnClick()
    {
        EventCenterDataManager.Inst.CliamReward(this._taskId, this._eventId, 1, 0, (claimRspData) =>
        {
            List<TaskClaimRewardData> rewardList = claimRspData.rewardList;
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            List<CommonRewardItemData> items = new List<CommonRewardItemData>();
            this.claimAction?.Invoke(claimRspData);
            TokenDataManager.Inst.GetTokenData();
            MessageHelper.Broadcast(MessageName.UpdateHallTask);
            TokenDataManager.Inst.GetTokenData();
            foreach (var reward in rewardList)
            {
                CommonRewardItemData item;
                MessageHelper.Broadcast(MessageName.OnPlayerInfoAccountChange, (CurrencyType)reward.rewardType);
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
            SetStatus(claimRspData.eventList.Find(x => x.eventId == _config.eventId).eventStatus);
            panel.ShowRewards(items);

            AccountDataManager.Inst.BalanceInfo.Refresh();
            VipDataManager.Inst.UpdateVipStatus();
            ReddotManagerUtils.Inst.RefreshRedDot();
        });
    }
}
