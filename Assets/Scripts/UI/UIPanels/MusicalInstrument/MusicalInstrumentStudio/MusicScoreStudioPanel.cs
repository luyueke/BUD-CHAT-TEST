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

public class MusicScoreStudioPanel : BasePanel<MusicScoreStudioPanel>
    {
        [SerializeField] private Transform _trans_Bg;
        [SerializeField] private NavigationBarTabs navigationBarTabs;
        [SerializeField] private MusicScoreInfoPanel DraftsList;
        [SerializeField] private MusicScoreInfoPanel PublishedList;
        [SerializeField] private MusicScoreDetailView DetailView;
        
        public class MusicScoreStudioConfig
        {
            public string name;
            public StudioSubType studioType;
        }

        private List<MusicScoreStudioConfig> rtConfig = new()
        {
            new() { name = "草稿箱", studioType = StudioSubType.Drafts },
            new() { name = "已发布", studioType = StudioSubType.Published },
        };

        public override void OnCreate()
        {
            base.OnCreate();
            MessageHelper.AddListener(DraftMessage.RefreshDraft, OnUgcMusicScoreDraftsListChange);
            MessageHelper.AddListener(MessageName.OnUgcMusicScoreDraftsListChange, OnUgcMusicScoreDraftsListChange);
            MessageHelper.AddListener(MessageName.OnUgcMusicScorePublishedListChange, OnUgcMusicScorePublishedListChange);
            MessageHelper.AddListener<UgcBaseInfo>(MessageName.UgcMusicScoreDidPublishedNew, OnUgcMusicScorePublishedNewItem);
            DetailView.InitUI(()=>navigationBarTabs.SetSelect((int)StudioSubType.Published));
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
                UIManager.Inst.ClosePanel(PanelId.MusicScoreStudioPanel);
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
            MessageHelper.RemoveListener(DraftMessage.RefreshDraft, OnUgcMusicScoreDraftsListChange);
            MessageHelper.RemoveListener(MessageName.OnUgcMusicScoreDraftsListChange, OnUgcMusicScoreDraftsListChange);
            MessageHelper.RemoveListener(MessageName.OnUgcMusicScorePublishedListChange, OnUgcMusicScorePublishedListChange);
            MessageHelper.RemoveListener<UgcBaseInfo>(MessageName.UgcMusicScoreDidPublishedNew, OnUgcMusicScorePublishedNewItem);
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

        private void OnUgcMusicScoreDraftsListChange()
        {
            DraftsList.GetData();
        }
        
        private void OnUgcMusicScorePublishedListChange()
        {
            PublishedList.GetData();
        }
        
        private void OnDraftsItemClick(DraftListItem item){
            DetailView.UpdateInfo(item);
        }
        
        private void OnPublishedItemClick(DraftListItem item){
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.MusicScore ,item.musicScoreInfo.id);
        }

        #region Contest

        private ContestInfo _contestData;
        public void SetContestFlag(ContestInfo contestData)
        {
            _contestData = contestData;
        }

        private void OnUgcMusicScorePublishedNewItem(UgcBaseInfo ugcBaseInfo)
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
                _contestData != null && _contestData.CurrentContestType == BUDContestType.MusicScore;
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
                var contests = ContestDataManager.Inst.canJoinContests(BUDContestType.MusicScore);
                if (contests != null && contests.Count > 0)
                {
                    UIManager.Inst.OpenPanel(PanelId.JoinInContestPanel, contests, creationId);
                }
            }
        }

        #endregion
    }
