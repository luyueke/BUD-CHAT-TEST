using System;
using System.Collections.Generic;
using GameData.Gashapon;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Event
{
    public class Style2EventItem : BaseEventItem
    {
        private Animator Effect_animator;
        private Image Img_Effect;
        private Text Txt_RewardName;
        private GameObject Go_Lock;
        protected Text Txt_RewardCount_1;
        protected Image Img_RewardIcon_1;
        protected Text Txt_RewardCount_2;
        protected Image Img_RewardIcon_2;

        private bool IsInit = false;

        private void OnEnable()
        {
            if(IsInit)
                SetClaimState((TaskClaimState)this._curData.eventStatus);
        }

        public override void BindUI()
        {
            base.BindUI();
            Effect_animator = this.GetComponent<Animator>();
            Img_Effect = GameObjectEx.FindChildByName(this.transform, "login_gift_hool_eff").GetComponent<Image>();
            Txt_RewardName = GameObjectEx.FindChildByName(this.transform, "Txt_RewardName").GetComponent<Text>();
            Go_Lock = GameObjectEx.FindChildByName(this.transform, "Go_Lock").gameObject;
            Txt_RewardCount_1 = GameObjectEx.FindChildByName(this.transform, "Txt_RewardCount_1").GetComponent<Text>();
            Img_RewardIcon_1 = GameObjectEx.FindChildByName(this.transform, "Img_RewardIcon_1").GetComponent<Image>();
            Txt_RewardCount_2 = GameObjectEx.FindChildByName(this.transform, "Txt_RewardCount_2").GetComponent<Text>();
            Img_RewardIcon_2 = GameObjectEx.FindChildByName(this.transform, "Img_RewardIcon_2").GetComponent<Image>();
        }

        public override void InitData(string taskId, TaskItemData data)
        {
            base.InitData(taskId, data);
            SetRewardName(this._curData.rewardList);
            IsInit = true;
        }

        public override void SetRewardIcon(List<TaskClaimRewardData> rewardList)
        {
            if (rewardList.Count == 1)
            {
                base.SetRewardIcon(rewardList);
            }
            else if(rewardList.Count == 2)
            {
                Img_RewardIcon.gameObject.SetActive(false);
                var data1 = rewardList[0];
                var spriteName1 = "RewardIcon_" + data1.rewardType;
                Img_RewardIcon_1.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName1, gameObject);
                Img_RewardIcon_1.gameObject.SetActive(true);
                var data2 = rewardList[1];
                var spriteName2 = "RewardIcon_" + data2.rewardType;
                Img_RewardIcon_2.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName2, gameObject);
                Img_RewardIcon_2.gameObject.SetActive(true);
            }
        }

        public override void SetRewardCount(List<TaskClaimRewardData> rewardList)
        {
            if (rewardList.Count == 1)
            {
                base.SetRewardCount(rewardList);
            }
            else if(rewardList.Count == 2)
            {
                Txt_RewardCount.gameObject.SetActive(false);
                var data1 = rewardList[0];
                Txt_RewardCount_1.text = "X" + data1.amount.ToString();
                Txt_RewardCount_1.gameObject.SetActive(true);
                var data2 = rewardList[1];
                Txt_RewardCount_2.text = "X" + data2.amount.ToString();
                Txt_RewardCount_2.gameObject.SetActive(true);
            }
        }

        public void SetRewardName(List<TaskClaimRewardData> rewardList)
        {
            string rewardName = "";
            for (int i = 0; i < rewardList.Count; i++)
            {
                rewardName += PgcUtils.GetTokenName((CurrencyType)rewardList[i].rewardType);
                if(i != rewardList.Count - 1)
                    rewardName += "+";
            }
            Txt_RewardName.SetLocalText(rewardName);
        }

        public override void SetClaimState(TaskClaimState state)
        {
            base.SetClaimState(state);
            switch (state)
            {
                case TaskClaimState.Unable:
                    SetItemBg(this._taskId);
                    Btn_Claim.SetClickAble(false);
                    Go_Lock.SetActive(true);
                    Effect_animator.enabled = false;
                    Img_Effect.color = new Color(255, 255, 255, 0);
                    break;
                case TaskClaimState.Enable:
                    Btn_Claim.SetClickAble(true);
                    Go_Lock.SetActive(false);
                    Effect_animator.enabled = true;
                    Effect_animator.CrossFade("LoginGiftPanel_prompt",0.1f);
                    Img_Bg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "SevenDayItemSelected", gameObject);
                    break;
                case TaskClaimState.Finished:
                    SetItemBg(this._taskId);
                    Btn_Claim.gameObject.SetActive(false);
                    Go_Lock.SetActive(false);
                    Effect_animator.enabled = false;
                    Img_Effect.color = new Color(255, 255, 255, 0);
                    break;
            }
        }
    }
}
