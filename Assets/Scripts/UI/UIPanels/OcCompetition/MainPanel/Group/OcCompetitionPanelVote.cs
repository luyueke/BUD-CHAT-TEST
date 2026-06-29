using Message;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCompetitionPanelVote : MonoBehaviour
    {
        public OcCptWorkItem Item1;

        public OcCptWorkItem Item2;

        public Button SwitchBtn;

        public Text Times;
        OcCompetitionSystemData data => OcCompetitionSystem.Inst.data;

        [HideInInspector] public OcCompetitionPanelMy Root;

        private void Awake()
        {
            SwitchBtn.onClick.AddListener(OnVoteBtn);

            MessageHelper.AddListener(MessageName.RefreshOcTimes, RefreshTimes);
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener(MessageName.RefreshOcTimes, RefreshTimes);
        }

        void RefreshTimes() {
            Times.text = $"换一换\n剩余次数：{data.ContestInfo.changeTimes}次";
        }

        public void Init(OcCompetitionPanelMy root)
        {
            Root = root;
        }

        private void OnEnable()
        {
            RefreshTimes();

            if (data.VoteListMsg!= null)
            {
                RefreshView();
            }
            else
            {
                OcCompetitionSystem.Inst.OcWorks(OcCompetitionListType.ChangeWork, () =>
                {
                    RefreshView();
                });
            }
        }

        void RefreshView() {
            if (data.VoteListMsg.list.Count > 0)
            {
                Item1.SetData(data.VoteListMsg.list[0]);
            }
            if (data.VoteListMsg.list.Count > 1)
            {
                Item2.SetData(data.VoteListMsg.list[1]);
            }
        }

        public void OnVoteBtn()
        {
            if (data.ContestInfo.changeTimes > 0)
            {
                OcCompetitionSystem.Inst.OcWorks(OcCompetitionListType.ChangeWork, () =>
                {
                    RefreshView();
                },true);
            }
            else
            {
                TipPanel.ShowToast("换一换剩余次数不足");
            }
        }

        public void GetVote() {
            OcCompetitionSystem.Inst.OcWorks(OcCompetitionListType.AllWork, () =>
            {
                RefreshView();
            });
        }
    }
}