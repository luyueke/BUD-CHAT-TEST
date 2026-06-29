using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public enum OcCompetitionStep 
    {
        None = 0,
        Poster, // 海报
        Oc,    // 设字
        Pose, //姿势
        All, //作品列表状态
        Single, //作品状态
        Vote, //投票
    }
    public class OcCompetitionPanelMy : MonoBehaviour
    {
        public Transform RuleGroup;

        public OcCompetitionPanelOc OcGroup;

        public OcCompetitionPanelPose PoseGroup;

        public OcCompetitionPanelAvatar AvatarGroup;

        public OcCompetitionPanelStage StageGroup;

        public OcCompetitionPanelSingle SingleGroup;

        public OcCompetitionPanelAll AllGroup;

        public OcCompetitionPanelVote VoteGroup;

        public Button JoinBtn;

        [HideInInspector] public OcCompetitionPanel Root;

        [HideInInspector] public OcCompetitionStep CurStep;

        OcCompetitionSystemData data => OcCompetitionSystem.Inst.data;

        public void Init(OcCompetitionPanel root)
        {
            Root = root;

            JoinBtn.onClick.AddListener(OnJoinBtn);

            OcGroup.Init(this);
            PoseGroup.Init(this);
            AvatarGroup.Init(this);
            SingleGroup.Init(this);
            AllGroup.Init(this);
            VoteGroup.Init(this);
        }

        private void OnEnable()
        {
            if (OcCompetitionSystem.Inst.InSubmission())
            {
                if (CurStep == OcCompetitionStep.None)
                {
                    OcCompetitionSystem.Inst.OcWorks(OcCompetitionListType.MyWork, () =>
                    {
                        if (data.MyListMsg.list == null || data.MyListMsg.list.Count <= 0)
                        {
                            SetStep(OcCompetitionStep.Poster);
                        }
                        else
                        {
                            SetStep(OcCompetitionStep.All);
                        }
                    });
                }
            }
            else
            {
                SetStep(OcCompetitionStep.All);
            }
        }

        void OnJoinBtn() 
        {
            AvatarGroup.Clear();
            SetStep(OcCompetitionStep.Oc);
        }

        public void SetStep(OcCompetitionStep step) {
            CurStep = step;
            RuleGroup.gameObject.SetActive(false);
            AvatarGroup.gameObject.SetActive(false);
            OcGroup.gameObject.SetActive(false);
            PoseGroup.gameObject.SetActive(false);
            StageGroup.gameObject.SetActive(false);
            SingleGroup.gameObject.SetActive(false);
            AllGroup.gameObject.SetActive(false);
            VoteGroup.gameObject.SetActive(false);
            switch (step)
            {
                case OcCompetitionStep.None:
                    break;
                case OcCompetitionStep.Poster:
                    RuleGroup.gameObject.SetActive(true);
                    StageGroup.gameObject.SetActive(true);
                    break;
                case OcCompetitionStep.Oc:
                    OcGroup.gameObject.SetActive(true);
                    AvatarGroup.gameObject.SetActive(true);
                    StageGroup.gameObject.SetActive(true);
                    break;
                case OcCompetitionStep.Pose:
                    PoseGroup.gameObject.SetActive(true);
                    AvatarGroup.gameObject.SetActive(true);
                    StageGroup.gameObject.SetActive(true);
                    break;
                case OcCompetitionStep.All:
                    AllGroup.gameObject.SetActive(true);
                    StageGroup.gameObject.SetActive(true);
                    break;
                case OcCompetitionStep.Single:
                    StageGroup.gameObject.SetActive(true);
                    SingleGroup.gameObject.SetActive(true);
                    break;
                case OcCompetitionStep.Vote:
                    VoteGroup.gameObject.SetActive(true);
                    StageGroup.gameObject.SetActive(true);
                    break;
                default:
                    break;
            }
        }
    }
}