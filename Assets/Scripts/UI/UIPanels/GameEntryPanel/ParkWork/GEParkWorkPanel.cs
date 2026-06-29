using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.BaseInfo;
using Message;
using Sirenix.OdinInspector;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class GEParkWorkPanel : BasePanel<GEParkWorkPanel>
    {
        public CButton CloseBtn;

        public Toggle TogPublished;
        public Toggle TogDrafts;

        public GEParkWorkAdpter WorkAdpter;
        public PullToRefreshBehaviour PullToRefreshBehaviour;

        public GEParkWorkDetail ParkDetailView;


        [HideInInspector] public MapListResponseType mapListType;
        public override void OnCreate()
        {
            base.OnCreate();

            CloseBtn.onClick.AddListener(CloseSelf);

            TogPublished.onValueChanged.AddListener(OnTogPublished);
            TogDrafts.onValueChanged.AddListener(OnTogDrafts);

            WorkAdpter.OnItemSelected = OnItemSelected;
            WorkAdpter.Data = new LazyDataHelper<DraftListItem>(WorkAdpter, GetMapInfo);
            WorkAdpter.Init();
            PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(OnPullRefresh);

            MessageHelper.AddListener(DraftMessage.RefreshDraft, OnMapRefreshCallback);
            MessageHelper.AddListener(MessageName.OnAssetDelete, OnMapRefreshCallback);
        }

        protected override void OnDestroy()
        {
            GameEntrySystem.Inst.GetPublishedReq(MapListResponseType.None, null);
            MessageHelper.RemoveListener(DraftMessage.RefreshDraft, OnMapRefreshCallback);
            MessageHelper.RemoveListener(MessageName.OnAssetDelete, OnMapRefreshCallback);
            base.OnDestroy();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            ParkDetailView.HidePanel();

            TogDrafts.isOn = true;
        }

        private void OnMapRefreshCallback() {
            if (mapListType == MapListResponseType.MapDrafts)
            {
                GameEntrySystem.Inst.GetPublishedReq(MapListResponseType.None, null);
                OnPullRefresh();
            }
        }

        private DraftListItem GetMapInfo(int index)
        {
            if (mapListType == MapListResponseType.MapPublished)
            {
                return GameEntrySystem.Inst.data.MapListInfos[index];
            }
            else
            {
                if (index == 0)
                {
                    return new DraftListItem();
                }
                return GameEntrySystem.Inst.data.MapListInfos[index - 1];
            }
 
        }

        private void OnItemSelected(DraftListItem mapInfo)
        {
            if (mapListType == MapListResponseType.MapPublished)
            {
                GameEntrySystem.Inst.OpenParkDetailPanel(mapInfo.mapInfo.id,true,true);
            }
            else
            {
                ParkDetailView.ShowPanel(mapInfo);
            }
        }

        private void OnPullRefresh()
        {
            GameEntrySystem.Inst.GetPublishedReq(mapListType, (items) =>
            {
                PullToRefreshBehaviour.HideGizmo();
                if (mapListType == MapListResponseType.MapPublished)
                {
                    WorkAdpter.Data.ResetItems(GameEntrySystem.Inst.data.MapListInfos.Count);
                }
                else
                {
                    WorkAdpter.Data.ResetItems(GameEntrySystem.Inst.data.MapListInfos.Count + 1);
                }
                WorkAdpter.Refresh();
            });
        }

        public void RefreshList() 
        {
            GameEntrySystem.Inst.data.MapListResponse = null;
            GameEntrySystem.Inst.data.MapListInfos.Clear();
            OnPullRefresh();
        }

        void OnTogPublished(bool bo) {
            if (bo)
            {
                mapListType = MapListResponseType.MapPublished;
                OnPullRefresh();
            }
        }

        void OnTogDrafts(bool bo)
        {
            if (bo)
            {
                mapListType = MapListResponseType.MapDrafts;
                OnPullRefresh();
            }
        }
    }
}