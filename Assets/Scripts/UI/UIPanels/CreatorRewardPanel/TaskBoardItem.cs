using System;
using System.Collections.Generic;
using Game.Event;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.CreaterRewardPanel
{
    public class TaskBoardItem : MonoBehaviour
    {
        [SerializeField] internal Image bg;
        [SerializeField] internal Image finishIcon;
        [SerializeField] internal Button rootBtn;
        [SerializeField] internal Image icon;
        [SerializeField] internal Text title;


        private Action<TaskItemData> _claimAction;
        private string _taskId;
        private TaskItemData _taskItemData;
        private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/CreatorRewardPanel/CreatorRewardPanel.spriteatlas";


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
                return;
            }

            EventCenterDataManager.Inst.CliamReward(this._taskId, this._taskItemData.eventId, 1, 0,(claimRspData) =>
            {
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(new List<CommonRewardItemData>()
                {
                    new CommonRewardItemData()
                    {
                        IconSp = icon.sprite,
                        RewardAmount = 1,
                        rewardName = title.text
                    },
                });
                _claimAction.Invoke(_taskItemData);
            });
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
            finishIcon.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.Finish);
            
            Sprite bgSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath,
                taskItemData.eventStatus == (int)EventStatus.Claim ? "taskboard_claim_bg" : "taskboard_progress_bg",
                gameObject);
            bg.sprite = bgSp;
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
}