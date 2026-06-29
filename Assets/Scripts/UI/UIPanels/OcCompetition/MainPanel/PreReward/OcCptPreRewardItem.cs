using Basic.Utils;
using GameData;
using System.Collections;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCptPreRewardItem : MonoBehaviour
    {
        public GameObject One;
        public GameObject Two;
        public GameObject Three;

        public GameObject Rank;
        public Text RankTxt;
        public Text RewardTxt;

        ContestPrizeInfo info;
        int index;

        private void Awake()
        {
            //One.gameObject.SetActive(false);
            //Two.gameObject.SetActive(false); 
            //Three.gameObject.SetActive(false);
            //Rank.gameObject.SetActive(false);
        }

        public void SetData(ContestPrizeInfo _info,int idx) {
            info = _info;

            index = idx;

            switch (idx)
            {
                case 0: One.gameObject.SetActive(true); break;
                case 1: Two.gameObject.SetActive(true); break;
                case 2: Three.gameObject.SetActive(true); break;
                default:
                    Rank.gameObject.SetActive(true);
                    RankTxt.text = (index + 1).ToString();
                    break;
            }
            if (GameUtils.IsCurrencyType(info.rewardType))
            {
                //var currencyType = GameUtils.ConvertRewardType(data.rewardType);
                //var sprite = PgcUtils.LoadRewardIcon((BUDRewardType)data.rewardType, gameObject);
                //assetsIcon.sprite = sprite;
                RewardTxt.text = $"X{info.num}";
            }

    
        }

    }
}