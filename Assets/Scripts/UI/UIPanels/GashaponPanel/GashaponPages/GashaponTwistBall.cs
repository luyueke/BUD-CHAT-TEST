using System;
using GameData.Rewards;
using UnityEngine;

namespace UI.UIPanels.GashaponPanel.GashaponPages
{
    public class GashaponTwistBall : MonoBehaviour
    {
        public Transform blueRewardTrans;
        public Transform purpleRewardTrans;
        public Transform goldRewardTrans;

        public void SetLevelSizeShow(RewardLevel level)
        {
            blueRewardTrans.gameObject.SetActive(false);
            purpleRewardTrans.gameObject.SetActive(false);
            goldRewardTrans.gameObject.SetActive(false);
            switch (level)
            {
                case RewardLevel.Gold:
                    goldRewardTrans.gameObject.SetActive(true);
                    break;
                case RewardLevel.Purple:
                    purpleRewardTrans.gameObject.SetActive(true);
                    break;
                case RewardLevel.Blue:
                    blueRewardTrans.gameObject.SetActive(true);
                    break;
                default:
                    purpleRewardTrans.gameObject.SetActive(true);
                    break;
            }
        }
    }
}