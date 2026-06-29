using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using System;
using System.Collections;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCompetitionPanel : BasePanel<OcCompetitionPanel>
    {
        public CButton CloseBtn;

        public RemoteImageBehaviour Bg;

        public RemoteImageBehaviour InfoBg;

        public OcCompetitionPanelMy MyGroup;

        public OcCompetitionPanelRank RankGroup;

        public Toggle TogMy;

        public Toggle TogRank;

        public Toggle TogRankLast;

        public Text TimeTxt;

        public Text TitleTxt;
        public Text TitleTxt2;

        public OcCompetitionSystemData data => OcCompetitionSystem.Inst.data;

        public ContestInfo ContestInfo => data.ContestInfo;

        private BudTimer budTimer;

        private long Time;

        public override void OnCreate()
        {
            base.OnCreate();

            CloseBtn.onClick.AddListener(CloseSelf);
            TogMy.onValueChanged.AddListener(OnTogMy);

            TogRank.onValueChanged.AddListener(OnTogRank);

            TogRankLast.onValueChanged.AddListener(OnTogRankLast);

            MyGroup.Init(this);
            RankGroup.Init(this);
        }

        protected override void OnDestroy()
        {
            var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            if (panel != null)
            {
                panel.ShowAvatar(true);
            }
            TimerManager.Inst.Stop(budTimer);
            base.OnDestroy();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            Bg.Load(ContestInfo.backgroundUrl);

            InfoBg.Load(ContestInfo.posterImageUrl);

            TitleTxt.text = ContestInfo.contestName;
            TitleTxt2.text = ContestInfo.contestName;

            TimerManager.Inst.Stop(budTimer);

            //var cur = TimeTools.TicksToUnixTimestamp(DateTime.Now.Ticks);
            Time = ContestInfo.endTime - TcpTimeSystem.Inst.ServerTime;
            if (Time > 0)
            {
                budTimer = TimerManager.Inst.Run("OcCompetitionPanel", 0, 60, OnTimer);
            }
            else
            {
                TimeTxt.text = "活动已结束";
            }

            if (OcCompetitionSystem.Inst.InSubmission())
            {
                TogRank.gameObject.SetActive(false);
                TogRankLast.gameObject.SetActive(true);
            }
            else
            {
                TogRank.gameObject.SetActive(true);
                TogRankLast.gameObject.SetActive(false);
            }

            TogMy.isOn = true;
        }

        private void OnTogMy(bool bo) 
        {
            MyGroup.gameObject.SetActive(bo);
        }
        private void OnTogRank(bool bo)
        {
            RankGroup.gameObject.SetActive(bo);
        }

        private void OnTogRankLast(bool bo)
        {
            RankGroup.gameObject.SetActive(bo);
        }


        void OnTimer() 
        {
            if (Time > 0)
            {
                Time -= 60;
                if (OcCompetitionSystem.Inst.InSubmission()) {
                    TimeTxt.text = ($"距离投稿期结束: {OcCompetitionPanel.SecondsToTimeString(Time - 86400 * 5)}");
                }
                else
                {
                    TimeTxt.text = ($"距离作品结算: {OcCompetitionPanel.SecondsToTimeString(Time)}");
                }
                if (Time <= 0)
                {
                    TimeTxt.text = "活动已结束";
                    TimerManager.Inst.Stop(budTimer);
                }
            }
        }

        public static string SecondsToTimeString(long totalSeconds)
        {
            long days = totalSeconds / 86400;        // 86400 = 24 * 60 * 60
            long hours = (totalSeconds % 86400) / 3600;  // 3600 = 60 * 60
            long minutes = (totalSeconds % 3600) / 60;
            long seconds = totalSeconds % 60;

            if (days > 0)
            {
                return $"{days}天{hours}时{minutes}分";
            }
            else if (hours > 0)
            {
                return $"{hours}时{minutes}分";
            }
            else if (minutes > 0)
            {
                return $"{minutes}分{seconds}秒";
            }
            else
            {
                return $"{seconds}秒";
            }
        }
    }
}