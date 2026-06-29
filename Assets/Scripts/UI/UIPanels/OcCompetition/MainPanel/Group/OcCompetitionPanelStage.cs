using GameData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace GameUI
{
    public class OcCompetitionPanelStage : MonoBehaviour
    {
        public Button HelpBtn;

        public Button PreRewardBtn;

        public Image Fill;

        public List<Image> Images1;

        public List<Image> Images2;

        private ContestStatus Status;
        OcCompetitionSystemData data => OcCompetitionSystem.Inst.data;
        private void Awake()
        {
            HelpBtn.onClick.AddListener(OnBtn);
            PreRewardBtn.onClick.AddListener(OnRewardBtn);
        }

        void OnBtn() 
        {
            UIManager.Inst.OpenPanel<ContestDetailTipsPanel>(PanelId.ContestDetailTipsPanel, data.ContestInfo);
        }

        void OnRewardBtn() 
        {
            OcCompetitionSystem.Inst.OpenPreRewardPanel();
        }

        private void OnEnable()
        {
            images = null;
            Status = (ContestStatus)data.ContestInfo.status;
            switch (Status)
            {
                case ContestStatus.NotStart:
                    Fill.fillAmount = 0f;
                    foreach (var item in Images1)
                    {
                        item.gameObject.SetActive(false);
                    }
                    foreach (var item in Images2)
                    {
                        item.gameObject.SetActive(false);
                    }
                    break;
                case ContestStatus.InProgress:
                    Fill.fillAmount = 0.39f;
                    foreach (var item in Images1)
                    {
                        item.gameObject.SetActive(true);
                    }
                    foreach (var item in Images2)
                    {
                        item.gameObject.SetActive(false);
                    }
                    images = Images2;
                    break;
                case ContestStatus.Completed:
                case ContestStatus.Outdated:
                    Fill.fillAmount = 1f;
                    foreach (var item in Images1)
                    {
                        item.gameObject.SetActive(true);
                    }
                    foreach (var item in Images2)
                    {
                        item.gameObject.SetActive(true);
                    }
                    break;
                case ContestStatus.Submission:
                    Fill.fillAmount = 0.085f;
                    foreach (var item in Images1)
                    {
                        item.gameObject.SetActive(false);
                    }
                    foreach (var item in Images2)
                    {
                        item.gameObject.SetActive(false);
                    }
                    images = Images1;
                    break;
                default:
                    break;
            }

            time = 1;
            idx = 0;
        }

        private float time;
        private int idx;
        private List<Image> images;
        private void Update()
        {
            time -= Time.deltaTime;
            if (time <= 0)
            {
                time = 1;
                if (images != null)
                {
                    if (idx >= images.Count)
                    {
                        idx = 0;
                        foreach (var item in images) 
                        {
                            item.gameObject.SetActive(false);
                        }
                    }
                    images[idx].gameObject.SetActive(true);
                    idx++;
                }
            }
        }
    }
}