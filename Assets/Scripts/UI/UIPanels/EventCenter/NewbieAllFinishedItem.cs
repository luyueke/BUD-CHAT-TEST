using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Event
{
    public class NewbieAllFinishedItem : BaseEventItem
    {
        private Animator Effect_animator;
        private Image Img_Effect;
        private GameObject Go_Lock;
        private CButton Btn_RewardList;
        private bool _isLock;
        
        public override void BindUI()
        {
            base.BindUI();
            Effect_animator = this.GetComponent<Animator>();
            Img_Effect = GameObjectEx.FindChildByName(this.transform, "login_gift_hool_eff").GetComponent<Image>();
            Go_Lock = GameObjectEx.FindChildByName(this.transform, "Go_Lock").gameObject;
            Btn_RewardList = GameObjectEx.FindChildByName(this.transform, "Btn_RewardList").GetComponent<CButton>();
            Btn_RewardList.onClick.AddListener(OnBtnRewardListClick);
        }

        public void InitData(string taskId, TaskItemData data, bool isLock)
        {
            BindUI();
            this._taskId = taskId;
            this._isLock = isLock;
            this._curData = data;
            SetClaimState((TaskClaimState)this._curData.eventStatus);
        }
        
        public override void SetClaimState(TaskClaimState state)
        {
            base.SetClaimState(state);
            Go_Lock.SetActive(_isLock);
            
            if (_isLock)
            {
                Btn_Claim.gameObject.SetActive(false);
                Go_Finished.SetActive(false);
                return;
            }
            
            switch (state)
            {
                case TaskClaimState.Unable:
                    Effect_animator.enabled = false;
                    Img_Effect.color = new Color(255, 255, 255, 0);
                    break;
                case TaskClaimState.Enable:
                    Effect_animator.enabled = true;
                    Effect_animator.CrossFade("LoginGiftPanel_prompt",0.1f);
                    break;
                case TaskClaimState.Finished:
                    Effect_animator.enabled = false;
                    Img_Effect.color = new Color(255, 255, 255, 0);
                    break;
            }
        }

        private void OnBtnRewardListClick()
        {
            EventRewardPanelData data = new EventRewardPanelData()
            {
                bgColor = "#FFFFFF",
                rewardItemBgColor = "#926BF9",
                atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas",
                iconList = new List<string>()
                {
                    "color_bg_icon_newbie_01", "color_bg_icon_newbie_02", "color_bg_icon_newbie_03"
                },
                rewardList = this._curData.rewardPgcList
            };
            UIManager.Inst.OpenPanel<EventCenterRewardPanel>(PanelId.EventCenterRewardPanel, data);
        }

        public override void OnBtnClaimClick()
        {
            EventCenterDataManager.Inst.CliamReward(this._taskId, this._curData.eventId, 1,0, (claimRspData) =>
            {
                if (claimRspData == null || claimRspData.eventList == null)
                    return;

                // var itemData = claimRspData.eventList[0];
                // //1.刷新本地数据
                // this._curData.RefreshEventStatus(itemData.eventStatus);
                // SetClaimState((TaskClaimState)this._curData.eventStatus);
                EventCenterDataManager.Inst.GetTaskInfo();
                
                //2.弹出领奖弹窗
                var rewardList = new List<CommonRewardItemData>();
                for (int i = 0; i < _curData.rewardPgcList.Count; i++)
                {
                    CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
                    commonRewardItemData.IconSp = PgcUtils.GetIconSpriteByPgcId(_curData.rewardPgcList[i], gameObject);
                    rewardList.Add(commonRewardItemData);
                }
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(rewardList);
            });
        }
    }
}
