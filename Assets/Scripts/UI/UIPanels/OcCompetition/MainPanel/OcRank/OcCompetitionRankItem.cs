using Com.TheFallenGames.OSA.Util.IO;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCompetitionRankItem : MonoBehaviour
    {
        public Button btn_Item;
        public RemoteImageBehaviour iconRemoteImageBehaviour;
        public Text rankText;
        public Text nameText;
        public Text scoreText;
        public Image rankBg;
        public Image rankNum;
        public Image rankGuang;

        public Sprite[] rankBgSprites = new Sprite[4];
        public Sprite[] rankNumSprites = new Sprite[3];
        public Sprite[] rankGuangSprites = new Sprite[3];
        private OcCptListMsgItem mData;

        private Action<OcCptListMsgItem> ac;
        private void Awake()
        {
            btn_Item.onClick.AddListener(OnItemClick);
            // selectedImage.gameObject.SetActive(false);

            rankText.text = "";
            nameText.text = "";
            scoreText.text = "";
            rankNum.gameObject.SetActive(false);
            rankGuang.gameObject.SetActive(false);
        }

        private void OnItemClick()
        {
            ac?.Invoke(mData);
        }

        public void SetData(OcCptListMsgItem info, Action<OcCptListMsgItem> ac,int idx)
        {
            this.ac = ac;
            mData = info;
            gameObject.SetActive(true);
            var rank = idx;
            bool isInRank3 = idx < 3;  // 0 开始
            nameText.text = info.creator.nickname;
            scoreText.text = info.scoreInfo.score.ToString();
            if(isInRank3)
            {
                rankBg.sprite = rankBgSprites[rank];
                rankNum.gameObject.SetActive(true);
                rankNum.sprite = rankNumSprites[rank];
                rankGuang.gameObject.SetActive(true);
                rankGuang.sprite = rankGuangSprites[rank];
                rankText.text = "";
            }
            else
            {
                rankBg.sprite = rankBgSprites[3];
                rankNum.gameObject.SetActive(false);
                rankGuang.gameObject.SetActive(false);
                rankText.text = (rank + 1).ToString();
            }
            iconRemoteImageBehaviour.Load(info.creator.portraitUrl);
        }
    }
}