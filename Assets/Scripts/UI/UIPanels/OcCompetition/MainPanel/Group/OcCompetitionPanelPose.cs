using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.AnimationStudio;
using Game.Store;
using GameData.BaseInfo;
using Pb.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCompetitionPanelPose : MonoBehaviour
    {
        public Button NextBtn;
        public Button LastBtn;

        public Toggle ToggleOfficial;
        public Toggle ToggleMy;
        public Toggle ToggleBuy;
        public Toggle ToggleTem;

        public OcPoseAdapter PoseAdpter;
        public PullToRefreshBehaviour PullToRefreshBehaviour;

        OcCompetitionSystemData data => OcCompetitionSystem.Inst.data;
        [HideInInspector] public OcCompetitionPanelMy Root;

        [HideInInspector] public PoseInfo PoseInfo;
        private void Awake()
        {
            NextBtn.onClick.AddListener(OnNextBtn);
            LastBtn.onClick.AddListener(OnLastBtn);
            ToggleOfficial.onValueChanged.AddListener(OnToggleOfficial);
            ToggleMy.onValueChanged.AddListener(OnToggleMy);
            ToggleBuy.onValueChanged.AddListener(OnToggleBuy);

            //PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(OnPullRefresh);
            PoseAdpter.OnItemSelected = OnItemSelected;
            PoseAdpter.Data = new LazyDataHelper<OcPoseItemData>(PoseAdpter, GetInfo);
            PoseAdpter.Init();

            data.InitDataPose(gameObject);
        }

        public void Init(OcCompetitionPanelMy root)
        {
            Root = root;
        }

        private void Start()
        {
            if (ToggleTem.isOn)
            {
                ToggleOfficial.isOn = true;

                if (PoseInfo != null)
                {
                    Root.AvatarGroup.RefreshAvatar(PoseInfo);
                    PoseInfo = null;
                }
            }
        }

        private void OnEnable()
        {
            if (ToggleTem.isOn)
            {
                ToggleOfficial.isOn = true;
            }
            else
            {
                if (ToggleOfficial.isOn) ToggleOfficial.onValueChanged.Invoke(true);

                if (ToggleMy.isOn) ToggleMy.onValueChanged.Invoke(true);

                if (ToggleBuy.isOn) ToggleBuy.onValueChanged.Invoke(true);
            }
            if (PoseInfo != null)
            {
                Root.AvatarGroup.RefreshAvatar(PoseInfo);
                PoseInfo = null;
            }
        }

        public void OnItemSelected(OcPoseItemData data)
        {
            foreach (var item in PoseAdpter._VisibleItems)
            {
                item.ContainingCellViewsHolders[0].item.selectIcon.gameObject.SetActive(false);
            }
            if (data.quickData == null && data.goodsData == null)
            {
                if (ToggleMy.isOn)
                {
                    UIManager.Inst.OpenPanel(PanelId.AnimationStudioMainPanel, AnimationStudioType.Pose);
                }
                else if (ToggleBuy.isOn)
                {
                    Root.AvatarGroup.ShowAvatar(false);

                    var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
                    if (panel != null)
                    {
                        panel.JumpTo(MainTabs.Tab.Ugc, 110100);
                        panel.transform.SetAsLastSibling();
                        panel.ShowAvatar(true);
                    }
                    else
                    {
                        panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
                        panel.JumpTo(MainTabs.Tab.Ugc, 110100);
                    }
                }
            }
            else if (data.quickData != null)
            {
                Root.AvatarGroup.RefreshAvatar(data.quickData.poseInfo);
            }
            else if (data.goodsData != null)
            {
                var asset = data.goodsData.GetFirstAsset<AssetsData>();
                var poseInfo = asset.UgcInfo.UgcInfo as PoseInfo;
                Root.AvatarGroup.RefreshAvatar(poseInfo);
            }
        }

        //private void OnPullRefresh()
        //{
        //    OcCompetitionSystem.Inst.OcInfoListReq((items) =>
        //    {
        //        PullToRefreshBehaviour.HideGizmo();
        //        PoseAdpter.Data.ResetItems(data.OcListItems.Count);
        //        PoseAdpter.Refresh();
        //    });
        //}

        private OcPoseItemData GetInfo(int index)
        {
            if (ToggleMy.isOn)
            {
                if (index == 0)
                {
                    return new OcPoseItemData();
                }
                return new OcPoseItemData(data.MyGoodsDataPose.Get(index - 1));
            }
            else if (ToggleBuy.isOn)
            {
                if (index == 0)
                {
                    return new OcPoseItemData();
                }
                return new OcPoseItemData(data.BuyGoodsDataPose.Get(index - 1));
            }
            else
            {
                return new OcPoseItemData(data.QuickPoses[index]);
            }
        }

        void OnToggleOfficial(bool bo)
        {
            if (bo)
            {
                data.GetQuickPose((ls) =>
                {
                    //PullToRefreshBehaviour.HideGizmo();
                    PoseAdpter.Data.ResetItems(data.QuickPoses.Count);
                    PoseAdpter.Refresh();
                }, gameObject);
            }
        }
        void OnToggleMy(bool bo)
        {
            if (bo)
            {
                //PullToRefreshBehaviour.HideGizmo();
                if (data.MyGoodsDataPose.Count() > 0)
                {
                    PoseAdpter.Data.ResetItems(data.MyGoodsDataPose.Count() + 1);
                }
                else
                {
                    PoseAdpter.Data.ResetItems(1);
                }
                PoseAdpter.Refresh();
            }
        }
        void OnToggleBuy(bool bo)
        {
            if (bo)
            {
                //PullToRefreshBehaviour.HideGizmo();
                if (data.BuyGoodsDataPose.Count() > 0)
                {
                    PoseAdpter.Data.ResetItems(data.BuyGoodsDataPose.Count() + 1);
                }
                else
                {
                    PoseAdpter.Data.ResetItems(1);
                }
                PoseAdpter.Refresh();
            }
        }

        void OnNextBtn()
        {
            Root.AvatarGroup.Sure();
        }

        void OnLastBtn()
        {
            Root.OcGroup.OcInfo = Root.AvatarGroup.OcInfo;
            Root.SetStep(OcCompetitionStep.Oc);
        }
    }
}