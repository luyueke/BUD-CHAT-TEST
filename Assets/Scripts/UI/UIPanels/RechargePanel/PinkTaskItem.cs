using System;
using System.Collections.Generic;
using Game.Event;
using Game.Store;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PinkTaskItem : MonoBehaviour
{
    public Button rootBtn;
    public GameObject coverLayout;

    public Text title;

    public Image rewardIcon;
    public Text rewardNum;
    public Image bg;
    public Text progressTxt;
    public Slider progress;
    public CButton goBtn;

    private TaskItemData _taskItemData;
    private RewardItem _rewardItem;
    private string _taskId;

    private Action<TaskItemData> _claimAction;

    private void Start()
    {
        rootBtn.onClick.AddListener(RootBtnClick);
        goBtn.onClick.AddListener(() =>
        {
            if (_rewardItem == null)
            {
                return;
            }

        });
    }

    private void RootBtnClick()
    {
        if (_taskItemData == null)
        {
            return;
        }

        if (_taskItemData.eventStatus != (int)EventStatus.Claim)
        {
            return;
        }

        if (_rewardItem.rewardId == "2")
        {
            var panel = UIManager.Inst.OpenPanel<NewbieOptionalRewardPanel>(PanelId.NewbieOptionalRewardPanel);
            panel.SetPreviewData(this._taskId, this._taskItemData.eventId,new List<string> {"10900048","10900047"},
                _ =>
                {
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
                    ReddotManagerUtils.Inst.RefreshRedDot();
                    _claimAction.Invoke(_taskItemData);
                });
            return;
        }

        EventCenterDataManager.Inst.CliamReward(this._taskId, this._taskItemData.eventId, 1, 0,(claimRspData) =>
        {
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowRewards(new List<CommonRewardItemData>()
            {
                new CommonRewardItemData()
                {
                    IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(RechargePanel.RechargePanelAtlas, _rewardItem.rewardIcon1,
                        gameObject),
                    RewardAmount = _rewardItem.num,
                    rewardName = _rewardItem.rewardName1
                }
            });
            AccountDataManager.Inst.BalanceInfo.Refresh();
            EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
            ReddotManagerUtils.Inst.RefreshRedDot();
            _claimAction.Invoke(_taskItemData);
        });

    }

    public void OnInitCreate(RewardItem rewardItem)
    {
        _rewardItem = rewardItem;
        title.SetLocalText(rewardItem.title);

        if (rewardNum != null)
        {
            rewardNum.text = rewardItem.num > 1 ? "x" + rewardItem.num : "";
        }

        rewardIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(RechargePanel.RechargePanelAtlas, rewardItem.rewardIcon1, gameObject);


    }

    public void SetData(string taskId, TaskItemData taskItemData, Action<TaskItemData> claimAction)
    {
        this._claimAction = claimAction;
        if (taskItemData == null)
        {
            return;
        }

        _taskId = taskId;
        _taskItemData = taskItemData;
        coverLayout.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.Finish);
        bg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(RechargePanel.PinkCoinPanelAtlas,
            taskItemData.eventStatus == (int)EventStatus.Claim ? "pinktask_reward_active" : "pinktask_reward_inactive",
            gameObject);
        if (taskItemData.eventStatus == (int)EventStatus.Claim)
        {
            rootBtn.transform.GetComponent<Animator>().enabled = true;
            rootBtn.transform.GetComponent<Animator>().CrossFade("LoginGiftPanel_prompt", 0.1f);
        }
        else
        {
            rootBtn.transform.GetComponent<Animator>().CrossFade("LoginGiftPanel_done", 0.1f);
        }

        progressTxt.text = taskItemData.finishAmount + "/" + _rewardItem.progress;
        progress.value = taskItemData.finishAmount /(_rewardItem.progress * 1.0f);
    }
}
