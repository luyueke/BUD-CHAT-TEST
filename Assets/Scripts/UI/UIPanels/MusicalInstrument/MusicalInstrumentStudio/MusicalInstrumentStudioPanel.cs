using System.Collections.Generic;
using Game.Base;
using GameData;
using GameData.Base;
using Message;
using UGCAsset;
using UGCAsset.Draft;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.MusicalInstrument
{
    public class MusicalInstrumentStudioPanel : BasePanel<MusicalInstrumentStudioPanel>
    {
        [SerializeField] private Transform _trans_Bg;
        [SerializeField] private NavigationBarTabs navigationBarTabs;
        [SerializeField] private InstrumentStudioInfoPanel DraftsList;
        [SerializeField] private InstrumentStudioInfoPanel PublishedList;
        [SerializeField] private InstrumentDetailView DetailView;
        private StudioSubType curSelectType;
        
        public class InstrumentStudioConfig
        {
            public string name;
            public StudioSubType studioType;
        }

        private List<InstrumentStudioConfig> rtConfig = new()
        {
            new() { name = "草稿箱", studioType = StudioSubType.Drafts },
            new() { name = "已发布", studioType = StudioSubType.Published },
        };

        public override void OnCreate()
        {
            base.OnCreate();
            MessageHelper.AddListener(DraftMessage.RefreshDraft, OnUgcInstrumentDraftsListChange);
            MessageHelper.AddListener<SkinActionBaseDraftInfo>(DraftMessage.DraftSaveStatus, OnUgcInstrumentDraftSaveStatusChange);
            MessageHelper.AddListener(MessageName.OnUgcInstrumentDraftsListChange, OnUgcInstrumentDraftsListChange);
            MessageHelper.AddListener(MessageName.OnUgcInstrumentPublishedListChange, OnUgcInstrumentPublishedListChange);
            MessageHelper.AddListener(MessageName.OnAssetDelete, OnUgcInstrumentPublishedListChange);
            MessageHelper.AddListener<UgcBaseInfo>(MessageName.UgcInstrumentDidPublishedNew, OnUgcInstrumentPublishedNewItem);
            DetailView.InitUI();
            //事件注入
            DraftsList.StudioType = StudioSubType.Drafts;
            DraftsList.SetItemOnClickAct(OnDraftsItemClick);
            
            PublishedList.StudioType = StudioSubType.Published;
            PublishedList.SetItemOnClickAct(OnPublishedItemClick);
            InitBG();
            navigationBarTabs.AddBackBtnClickListener(OnBack);
            foreach (var cfg in rtConfig)
            {
                navigationBarTabs.CreateItem(cfg.studioType.ToString(), cfg.name).SetIsSelect(false);
            }

            navigationBarTabs.AddBackBtnClickListener(CloseSelf);
            navigationBarTabs.AddItemSelectCallBack(RTClick);
            navigationBarTabs.SetSelect((int)StudioSubType.Drafts);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            if (!GameController.IsInHallScene())
            {
                TipPanel.ShowToast("游玩过程中无法进行创作哦");
                CloseSelf();
                return;
            }
        }

        private void OnBack()
        {
            MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
            CloseSelf();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            MessageHelper.RemoveListener(DraftMessage.RefreshDraft, OnUgcInstrumentDraftsListChange);
            MessageHelper.RemoveListener<SkinActionBaseDraftInfo>(DraftMessage.DraftSaveStatus, OnUgcInstrumentDraftSaveStatusChange);
            MessageHelper.RemoveListener(MessageName.OnUgcInstrumentDraftsListChange, OnUgcInstrumentDraftsListChange);
            MessageHelper.RemoveListener(MessageName.OnUgcInstrumentPublishedListChange, OnUgcInstrumentPublishedListChange);
            MessageHelper.RemoveListener(MessageName.OnAssetDelete, OnUgcInstrumentPublishedListChange);
            MessageHelper.RemoveListener<UgcBaseInfo>(MessageName.UgcInstrumentDidPublishedNew, OnUgcInstrumentPublishedNewItem);
        }

        private void InitBG()
        {
            if (_trans_Bg == null)
            {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(_trans_Bg);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
            {
                "music_icon_1","music_icon_2","music_icon_3"
            });
            item.gameObject.SetActive(true);
        }
        
        private void RTClick(TabItem item, int index)
        {
            var data = rtConfig[index];
            OnSelectView(data.studioType);
        }
        
        private void OnSelectView(StudioSubType studioType)
        {
            curSelectType = studioType;
            switch (studioType)
            {
                case StudioSubType.Drafts:
                    DraftsList.gameObject.SetActive(true);
                    PublishedList.gameObject.SetActive(false);
                    DraftsList.GetData();
                    break;
            
                case StudioSubType.Published:
                    DraftsList.gameObject.SetActive(false);
                    PublishedList.gameObject.SetActive(true);
                    PublishedList.GetData();
                    break;
            }
        }

        private void OnUgcInstrumentDraftSaveStatusChange(SkinActionBaseDraftInfo skinActionBaseDraftInfo = null)
        {
            if(curSelectType == StudioSubType.Drafts)
                OnSelectView(StudioSubType.Drafts);
        }

        private void OnUgcInstrumentDraftsListChange()
        {
            if(curSelectType == StudioSubType.Drafts)
                OnSelectView(StudioSubType.Drafts);
            else
            {
                navigationBarTabs.SetSelect((int)StudioSubType.Drafts);
            }
        }
        
        private void OnUgcInstrumentPublishedListChange()
        {
            if(curSelectType == StudioSubType.Published)
                OnSelectView(StudioSubType.Published);
            else
            {
                navigationBarTabs.SetSelect((int)StudioSubType.Published);
            }
        }

        private ContestInfo _contestData;
        public void SetContestFlag(ContestInfo contestData)
        {
            _contestData = contestData;
        }

        private void OnUgcInstrumentPublishedNewItem(UgcBaseInfo ugcBaseInfo)
        {
            if (ugcBaseInfo == null)
            {
                return;
            }
            bool isAppealing = ugcBaseInfo?.auditInfo?.auditResult == (int)AuditResult.Appealing;
            if (isAppealing)
            {
                return;
            }
            
            bool joinContestDirect =
                _contestData != null && _contestData.CurrentContestType == BUDContestType.Instrument;
            var creationId = ugcBaseInfo.id;
            if (string.IsNullOrEmpty(creationId))
            {
                return;
            }
            
            if (joinContestDirect)
            {
                ContestDataManager.Inst.JoinContest(creationId, new List<string>() { _contestData.contestId }, b =>
                {
                    if (b)
                    {
                        var panel = UIManager.Inst.OpenPanel<JoinInContestPanel>(PanelId.JoinInContestPanel, new List<ContestInfo>() {_contestData}, creationId);
                        panel.ShowJoinSuccess(_contestData);
                    }
                });
            }
            else
            {
                var contests = ContestDataManager.Inst.canJoinContests(BUDContestType.Instrument);
                if (contests != null && contests.Count > 0)
                {
                    UIManager.Inst.OpenPanel(PanelId.JoinInContestPanel, contests, creationId);
                }
            }
        }
        
        private void OnDraftsItemClick(DraftListItem item){
            DetailView.UpdateInfo(item);
        }
        
        private void OnPublishedItemClick(DraftListItem item){
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Instrument ,item.skinInfo.id,item.skinInfo.ugcStyle);
        }
    }
}
