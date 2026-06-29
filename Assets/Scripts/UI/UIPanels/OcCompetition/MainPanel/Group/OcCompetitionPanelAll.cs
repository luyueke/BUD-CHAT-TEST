using Com.TheFallenGames.OSA.Util.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCompetitionPanelAll : MonoBehaviour
    {
        public RemoteImageBehaviour RemoteImageBehaviour;

        public Transform ItemParent;

        public OcCptWorkItem Item;

        public Button VoteBtn;

        public Button JoinBtn;

        public Text Times;

        OcCompetitionSystemData data => OcCompetitionSystem.Inst.data;

        [HideInInspector] public OcCompetitionPanelMy Root;

        [HideInInspector] public List<OcCptWorkItem> ItemList = new List<OcCptWorkItem>();
        private void Awake()
        {
            JoinBtn.onClick.AddListener(OnJoinBtn);
            VoteBtn.onClick.AddListener(OnVoteBtn);
            Item.gameObject.SetActive(false);
        }

        public void Init(OcCompetitionPanelMy root)
        {
            Root = root;
        }

        private void OnEnable()
        {
            VoteBtn.gameObject.SetActive(!OcCompetitionSystem.Inst.InSubmission());
            JoinBtn.gameObject.SetActive(OcCompetitionSystem.Inst.InSubmission());
            Times.gameObject.SetActive(!OcCompetitionSystem.Inst.InSubmission());
            Times.text = $"剩余评选次数：{data.ContestInfo.voteTimes}次";
            foreach (var item in ItemList)
            {
                item.gameObject.SetActive(false);
            }
            OcCompetitionSystem.Inst.OcWorks(OcCompetitionListType.MyWork, () => {
                var tem = data.MyListMsg.list;
                if (tem != null && tem.Count > 0)
                {
                    RemoteImageBehaviour.gameObject.SetActive(false);
                    for (int i = 0; i < tem.Count; i++)
                    {
                        if (i >= ItemList.Count)
                        {
                            var obj = GameObject.Instantiate(Item, ItemParent).GetComponent<OcCptWorkItem>();
                            ItemList.Add(obj);
                        }
                        ItemList[i].gameObject.SetActive(true);
                        ItemList[i].SetData(tem[i]);
                        if (OcCompetitionSystem.Inst.InSubmission())
                        {
                            ItemList[i].EditBtn2.gameObject.SetActive(true);
                        }
                        else
                        {
                            ItemList[i].VoteBg.gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    RemoteImageBehaviour.gameObject.SetActive(true);
                    RemoteImageBehaviour.Load(data.ContestInfo.bannerUrl);
                }
             
            });
        }

        private void OnVoteBtn()
        {
            if (OcCompetitionSystem.Inst.InSubmission())
            {
                TipPanel.ShowToast("投稿期间只能投稿");
                return;
            }
            if (data.ContestInfo.voteTimes > 0)
            {
                Root.SetStep(OcCompetitionStep.Vote);
            }
            else
            {
                TipPanel.ShowToast("评审次数已用完");
            }
        }

        private void OnJoinBtn()
        {
            if (OcCompetitionSystem.Inst.InSubmission())
            {
                Root.AvatarGroup.Clear();
                Root.SetStep(OcCompetitionStep.Oc);
            }
        }
    }
}