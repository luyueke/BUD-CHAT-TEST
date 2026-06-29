using System.Collections.Generic;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Event
{
    public class Style1EventBottomPanel : EventCenterBottomPanel
    {
        public GameObject ItemPrefab;
        public CButton BtnClaimAll;
        public Image ImgBtnClaimBg;
        public Sprite EnableBtnSP;
        public Sprite UnableBtnSP;
        
        public override void InitItems(TaskInfoData infoData)
        {
            base.InitItems(infoData);
            var eventList = infoData.eventList;
            for (int i = 0; i < eventList.Count; i++)
            {
                var itemObj = GameObject.Instantiate(ItemPrefab, ItemContent);
                var itemComp = itemObj.GetComponent<Style1EventItem>();
                _baseEventItems.Add(itemComp);
            }
        }

        public override void RefreshItems(TaskInfoData syncTaskData)
        {
            base.RefreshItems(syncTaskData);
            
            var eventList = this._taskInfoData.eventList;
            for (int i = 0; i < eventList.Count; i++)
            {
                var itemComp = (Style1EventItem)_baseEventItems[i];
                itemComp.InitData(this._taskInfoData.taskId, eventList[i]);
            }
            
            bool hasClaimItem = false;
            this._taskInfoData.eventList.ForEach(x =>
            {
                if (x.eventStatus == (int)TaskClaimState.Enable)
                    hasClaimItem = true;
            });
            SetClaimBtnEnable(hasClaimItem);
        }

        public override void BindUI()
        {
            base.BindUI();
            BtnClaimAll.onClick.RemoveAllListeners();
            BtnClaimAll.onClick.AddListener(OnBtnClaimAllClick);
            SetClaimBtnEnable(false);
        }

        private void OnBtnClaimAllClick()
        {
            EventCenterDataManager.Inst.CliamReward(_taskId.ToString(), 0,2, 0,(claimRspData) =>
            {
                TaskInfoData syncData = new TaskInfoData()
                {
                    taskId = this._taskId.ToString(),
                    taskStatus = 1,
                    eventList = claimRspData.eventList
                };
                
                //1.刷新数据
                EventCenterDataManager.Inst.GetTaskInfo();
                
                //2.弹出领奖弹窗
                var rewardList = new List<CommonRewardItemData>();
                foreach (var rewardData in claimRspData.rewardList)
                {
                    CommonRewardItemData commonRewardItemData = null;
                    commonRewardItemData = rewardList.Find(x => x.rewardType == rewardData.rewardType);

                    if (commonRewardItemData == null)
                    {
                        commonRewardItemData = new CommonRewardItemData();
                        rewardList.Add(commonRewardItemData);
                    }
                    
                    commonRewardItemData.rewardType = rewardData.rewardType;
                    var spriteName = "RewardIcon_" + rewardData.rewardType;
                    var sp  = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, gameObject);
                    commonRewardItemData.IconSp = sp;
                    commonRewardItemData.rewardName = PgcUtils.GetTokenName((CurrencyType)rewardData.rewardType);
                    commonRewardItemData.RewardAmount += rewardData.amount;
                }
                
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(rewardList);
                
                AccountDataManager.Inst.BalanceInfo.Refresh();
            });
        }

        private void SetClaimBtnEnable(bool enable)
        {
            BtnClaimAll.SetClickAble(enable);
            // ImgBtnClaimBg.sprite = enable ? EnableBtnSP : UnableBtnSP;
        }
    }
}