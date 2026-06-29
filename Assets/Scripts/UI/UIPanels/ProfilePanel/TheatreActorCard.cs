using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class TheatreActorCard : BaseCard
    {
        [SerializeField] private int shrinkLimit;
        [SerializeField] private Transform NoneTips;
        public TheatreActorProfileAdpter Adpter;
        private readonly List<DraftListItem> _datas = new();

        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            cardBgType = ProfileCardBgType.Bg3;
            if (Adpter.Data == null)
                Adpter.Data = new LazyDataHelper<DraftListItem>(Adpter, GetInfo);
            Adpter.OnItemClick = OnActorItemClick;
            TryInitAdapter();
        }

        private void OnActorItemClick(DraftListItem data)
        {
            if (data?.actorInfo == null) return;
            UIManager.Inst.OpenPanel(PanelId.AssetDetailPanel, AssetDetailType.Actor, data.actorInfo.id);
        }

        public bool RefreshWithData(string uid, MapListResponse actorList)
        {
            if (actorList == null || actorList.list == null || actorList.list.Count <= 0)
            {
                Show(false);
                return false;
            }
            Show(true);

            if (actorList.list.Count <= shrinkLimit)
                Shrink();
            else
                Expand();

            NoneTips.gameObject.SetActive(false);
            TryInitAdapter();
            _datas.Clear();
            _datas.AddRange(actorList.list);
            Adpter.Data.ResetItems(_datas.Count);
            Adpter.Refresh();
            return true;
        }

        private void TryInitAdapter()
        {
            if (Adpter == null) return;
            if (Adpter.IsInitialized) return;
            if (!Adpter.gameObject.activeInHierarchy) return;
            Adpter.Init();
        }

        private DraftListItem GetInfo(int index) => _datas[index];
    }
}
