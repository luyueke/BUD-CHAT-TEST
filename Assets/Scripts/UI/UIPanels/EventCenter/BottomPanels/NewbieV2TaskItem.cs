using System;
using System.Collections.Generic;
using Game.Event;
using UnityEngine;
using UnityEngine.UI;

public class NewbieV2TaskItem : MonoBehaviour
{
    public Button rootBtn;
    public GameObject overLayout;
    public GameObject todayLayout;
    public Text title;
    public Image rewardIcon;
    public Image lockIcon;
    public GameObject acticityObj;
    public GameObject selectImage;


    public TaskItemData _taskItemData;
    private RewardItem _rewardItem;
    private string _taskId;
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/EventCenter/EventCenter.spriteatlas";
    
    private Action<TaskClaimRsp> _claimAction;
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
                _claimAction.Invoke(claimRspData);
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
                
            });
        }
    }

    public void OnInitCreate(RewardItem rewardItem)
    {
        _rewardItem = rewardItem;
        title.SetLocalText(rewardItem.title);
        rewardIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1, gameObject);
    }
    public void SetSelect(bool isSelect)
    {
        selectImage.SetActive(isSelect);
    }

    public void SetData(string taskId, TaskItemData taskItemData, Action<TaskClaimRsp> claimAction, Action<RewardItem> checkAction)
    {
        this._claimAction = claimAction;
        this._checkAction = checkAction;
        if (taskItemData == null)
        {
            return;
        }
    
        _taskId = taskId;
        _taskItemData = taskItemData;

        SetStatus(taskItemData.eventStatus);
    }

    void SetStatus(int statu)
    {
        switch (statu)
        {
            case (int)EventStatus.UnClaim:
                overLayout.SetActive(false);
                lockIcon.gameObject.SetActive(true);
                acticityObj.SetActive(false);
                todayLayout.SetActive(false);
                break;
            case (int)EventStatus.Default:
                todayLayout.SetActive(true);
                overLayout.SetActive(false);
                lockIcon.gameObject.SetActive(false);
                acticityObj.SetActive(false);
                break;
            case (int)EventStatus.Claim:
                overLayout.SetActive(false);
                lockIcon.gameObject.SetActive(false);
                acticityObj.SetActive(true);
                todayLayout.SetActive(false);
                break;
            case (int)EventStatus.Finish:
                acticityObj.SetActive(false);
                lockIcon.gameObject.SetActive(false);
                overLayout.SetActive(true);
                todayLayout.SetActive(false);
                break;

        }


    }

}
