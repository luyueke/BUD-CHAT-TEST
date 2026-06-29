using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCptPreRewardPanel : BasePanel<OcCptPreRewardPanel>
    {
        public Button CloseBtn;

        public Button CloseBtn2;

        public Button CloseBtn3;

        public OcCptPreRewardItem RewardItem;

        public Transform RewardItemParent;

        public Transform Group1;
        public List<OcCptPreRewardBtn> BtnChenhao;
        public List<OcCptPreRewardBtn> BtnBobao;

        public Transform Group2;
        public Text Title2;
        public Text Content2;
        public List<Image> Image2;

        public Transform Group3;
        public Text Title3;
        public Text Content3;
        public Text Time3;

        public override void OnCreate()
        {
            base.OnCreate();
            CloseBtn.onClick.AddListener(CloseSelf);
            CloseBtn2.onClick.AddListener(() =>
            {
                Group1.gameObject.SetActive(true);
                Group2.gameObject.SetActive(false);
            });
            CloseBtn3.onClick.AddListener(() =>
            {
                Group1.gameObject.SetActive(true);
                Group3.gameObject.SetActive(false);
            });
            foreach (var item in BtnChenhao)
            {
                item.ac = OnShowGroup3;
            }
            foreach (var item in BtnBobao)
            {
                item.ac = OnShowGroup2;
            }
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
        }


        public void OnShowGroup2(string title,string content,int idx) 
        {
            Group1.gameObject.SetActive(false);
            Group2.gameObject.SetActive(true);
            Title2.text = $"{title}进房播报";
            if (idx == 0)
            {
                Content2.text = $"{content}授予每周设子搭配大赛第一名的设计师";
            }
            if (idx == 1) {
                Content2.text = $"{content}授予每周设子搭配大赛第二到三名的设计师";
            }
            if (idx == 2)
            {
                Content2.text = $"{content}授予每周设子搭配大赛第四到十名的设计师";
            }

            for (int i = 0; i < Image2.Count; i++)
            {
                Image2[i].gameObject.SetActive(i == idx);
            }
        }


        public void OnShowGroup3(string title,string content,int idx) {
            Group1.gameObject.SetActive(false);
            Group3.gameObject.SetActive(true);
            Title3.text = $"{title}";

            if (idx == 0)
            {
                Content3.text = $"{content}称号授予每周设子搭配大赛第一名的设计师，有效期为7天";
            }
            if (idx == 1)
            {
                Content3.text = $"{content}称号授予每周设子搭配大赛第二到三名的设计师，有效期为7天";
            }
            if (idx == 2)
            {
                Content3.text = $"{content}称号授予每周设子搭配大赛第四到十名的设计师，有效期为7天";
            }
      

            var start = OcCompetitionSystem.Inst.data.ContestInfo.endTime;
            var startTime = TimeTools.SecondsToDateTime(start);

            var end = OcCompetitionSystem.Inst.data.ContestInfo.endTime + 86400 * 6;
            var endTime = TimeTools.SecondsToDateTime(end);

            Time3.text = $"称号有效期{startTime.Month}/{startTime.Day}-{endTime.Month}/{endTime.Day}";
        }
    }
}