using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Message;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCompetitionPanelOc : MonoBehaviour
    {
        public Button NextBtn;

        public GameObject GrayBtn;

        public OcListAdapter OcAdpter;
        public PullToRefreshBehaviour PullToRefreshBehaviour;

        OcCompetitionSystemData data => OcCompetitionSystem.Inst.data;
        [HideInInspector]public OcCompetitionPanelMy Root;

        [HideInInspector] public OcServerData CurData;
        [HideInInspector] public OcInfo OcInfo;
        private void Awake()
        {
            NextBtn.onClick.AddListener(OnNextBtn);
            PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(OnPullRefresh);
            OcAdpter.OnItemSelected = OnItemSelected;
            OcAdpter.Data = new LazyDataHelper<OcServerData>(OcAdpter, GetInfo);
            OcAdpter.Init();

        }

        public void Init(OcCompetitionPanelMy root)
        {
            Root = root;
        }

        private void OnEnable()
        {
            MessageHelper.AddListener(MessageName.RefreshOcList, RefreshList);
            RefreshList();
        }
        private void OnDisable()
        {
            MessageHelper.RemoveListener(MessageName.RefreshOcList, RefreshList);
        }

        public void RefreshList() {
            GrayBtn.gameObject.SetActive(Root.AvatarGroup.OcInfo == null);
            data.Clear();
            OcCompetitionSystem.Inst.OcInfoListReq((items) =>
            {
                PullToRefreshBehaviour.HideGizmo();
                OcAdpter.Data.ResetItems(data.OcListItems.Count + 1);
                OcAdpter.Refresh();

                if (OcInfo != null)
                {
                    Root.AvatarGroup.RefreshAvatar(OcInfo);
                    OcInfo = null;
                }
            });
        }


        public void OnItemSelected(OcServerData _data)
        {
            foreach (var item in OcAdpter._VisibleItems)
            {
                item.ContainingCellViewsHolders[0].item.selectedImage.gameObject.SetActive(false);
            }
            Root.AvatarGroup.RefreshAvatar(_data);
            GrayBtn.gameObject.SetActive(Root.AvatarGroup.OcInfo == null);
        }

        private void OnPullRefresh()
        {
            OcCompetitionSystem.Inst.OcInfoListReq((items) =>
            {
                PullToRefreshBehaviour.HideGizmo();
                OcAdpter.Data.ResetItems(data.OcListItems.Count + 1);
                OcAdpter.Refresh();
            });
        }

        private OcServerData GetInfo(int index)
        {
            if (index == 0)
            {
                return new OcServerData();
            }
            return data.OcListItems[index - 1];
        }

        void OnNextBtn()
        {
            Root.SetStep(OcCompetitionStep.Pose);
        }
    }
}