using System;
using System.Collections.Generic;
using Game.Event;
using UnityEngine;
using UnityEngine.UI;

public class NewbieTaskItem : MonoBehaviour
{
    public Button rootBtn;
    public GameObject coverLayout;
    public Text title;
    public Text desc;
    public Image bg;
    public Image rewardIcon;
    public Image lockIcon;
    
    private TaskItemData _taskItemData;
    private RewardItem _rewardItem;
    private string _taskId;
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/EventCenter/EventCenter.spriteatlas";
    
    private Action<TaskItemData> _claimAction;
    private Action<RewardItem> _checkAction;

    private void Start()
    {
        rootBtn.onClick.AddListener(RootBtnClick);
    }
    
    private void RootBtnClick()
    {
        if (_taskItemData == null)
        {
            return;
        }
    
        if (_taskItemData.eventStatus != (int)EventStatus.Claim)
        {
            _checkAction?.Invoke(_rewardItem);
        }
        else
        {
            EventCenterDataManager.Inst.CliamReward(this._taskId, this._taskItemData.eventId, 1, 0, (claimRspData) =>
            {
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(new List<CommonRewardItemData>()
                {
                    new CommonRewardItemData()
                    {
                        IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, _rewardItem.rewardIcon1,
                            gameObject),
                        RewardAmount = _rewardItem.num,
                        rewardName = _rewardItem.rewardName1
                    }
                });
                AccountDataManager.Inst.BalanceInfo.Refresh();
                // EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewComerCommunityCoin);
                VipDataManager.Inst.UpdateVipStatus();
                ReddotManagerUtils.Inst.RefreshRedDot();
                _claimAction.Invoke(_taskItemData);
            });
        }
    }
    
    public void OnInitCreate(RewardItem rewardItem)
    {
        _rewardItem = rewardItem;
        title.SetLocalText(rewardItem.title);
        
        desc.text = rewardItem.rewardName1;
        rewardIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1, gameObject);
    }
    
    public void SetData(string taskId, TaskItemData taskItemData, Action<TaskItemData> claimAction, Action<RewardItem> checkAction)
    {
        this._claimAction = claimAction;
        this._checkAction = checkAction;
        if (taskItemData == null)
        {
            return;
        }
    
        _taskId = taskId;
        _taskItemData = taskItemData;
        
        lockIcon.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.UnClaim);
        coverLayout.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.Finish);
        bg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath,
            taskItemData.eventStatus == (int)EventStatus.Claim ? "newbie_task_active" : "newbie_task_topItemBg",
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
        
    }
}
