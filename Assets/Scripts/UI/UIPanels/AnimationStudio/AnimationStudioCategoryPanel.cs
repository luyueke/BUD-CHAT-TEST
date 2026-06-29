using System.Collections.Generic;
using EventTracking;
using GameData;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace Game.AnimationStudio
{
    public class AnimationStudioCategoryPanel : BasePanel<AnimationStudioCategoryPanel>
    {
        public Transform BG;
        public CButton Btn_Back;
        public CButton Btn_AnimStudio;
        public CButton Btn_PostureStudio;
        public CButton Btn_HumanAnimStore;
        public CButton Btn_PetAnimStore;
        public CButton Btn_BgmStore;
        public CButton PoseStoreBtn;

        public Transform ActiveContestView;
        public SelectableContestGroupView contestView;


        public override void OnCreate()
        {
            base.OnCreate();
            InitBG();
            AddListener();
            InitContest();
            LoadEvent.ReportTask(157, 7);
        }

        private void InitBG()
        {
            if (BG == null)
            {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(BG);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
            {
                "animStudio_icon1", "animStudio_icon2", "animStudio_icon3"
            });
            item.gameObject.SetActive(true);
        }

        private void AddListener()
        {
            Btn_Back.onClick.AddListener(CloseSelf);
            Btn_AnimStudio.onClick.AddListener(OnBtnAnimStudioClick);
            Btn_PostureStudio.onClick.AddListener(OnBtnPostureStudioClick);
            Btn_HumanAnimStore.onClick.AddListener(OnBtnHumanAnimStoreClick);
            Btn_PetAnimStore.onClick.AddListener(OnBtnPetAnimStoreClick);
            Btn_BgmStore.onClick.AddListener(OnBtnBgmStoreClick);
            PoseStoreBtn.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanel<PoseStorePanel>(PanelId.PoseStorePanel);
            });
        }

        #region ButtonFunc

        private void OnBtnAnimStudioClick()
        {
            UIManager.Inst.OpenPanel<AnimationStudioMainPanel>(PanelId.AnimationStudioMainPanel, AnimationStudioType.Animation);
        }

        private void OnBtnPostureStudioClick()
        {
            UIManager.Inst.OpenPanel<AnimationStudioMainPanel>(PanelId.AnimationStudioMainPanel, AnimationStudioType.Pose);
        }

        private void OnBtnHumanAnimStoreClick()
        {
            var panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            panel.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.UgcEmote, (int)GameData.PgcData.UgcAnimSubType.PeopleAll));
        }

        private void OnBtnPetAnimStoreClick()
        {
            var panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel, true);
            panel.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.UgcEmote, (int)GameData.PgcData.UgcAnimSubType.PetAll));
        }

        private void OnBtnBgmStoreClick()
        {
            UIManager.Inst.OpenPanel<UgcAnimToneStorePanel>(PanelId.UgcAnimToneStorePanel);
        }

        #endregion

        #region Contest

        private void InitContest()
        {
            var showContestTypes = new List<BUDContestType>()
            {
                BUDContestType.Vehicle,
                BUDContestType.Instrument,
                BUDContestType.MusicScore
            };
            bool hasActiveContest = ContestDataManager.Inst.HasActiveContest(showContestTypes);
            ActiveContestView.gameObject.SetActive(hasActiveContest);

            if (hasActiveContest)
            {
                contestView.InitViewInfo(showContestTypes);
                contestView.OnSelect = OnSelectContest;
            }
        }

        private void OnSelectContest(ContestInfo contestInfo)
        {
            if (contestInfo == null)
            {
                return;
            }

            ContestEventManager.Inst.OpenContestPage(contestInfo.contestId);
        }

        #endregion
    }
}
