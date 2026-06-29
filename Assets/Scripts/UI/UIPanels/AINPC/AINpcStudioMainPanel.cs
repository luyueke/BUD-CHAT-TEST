using System.Collections.Generic;
using EventTracking;
using Game.Base;
using GameData;
using GameData.Base;
using Message;
using UGCAsset;
using UGCAsset.Draft;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.AINPCStudio
{
    public class AINpcStudioMainPanel : BasePanel<AINpcStudioMainPanel>
    {
        [SerializeField] private Transform _trans_Bg;
        [SerializeField] private NavigationBarTabs navigationBarTabs;
        [SerializeField] private AINPCStudioInfoPanel DraftsList;
        [SerializeField] private AINPCStudioInfoPanel PublishedList;
        [SerializeField] private AINPCStudioDetailView DetailView;
        private StudioSubType curSelectType;
        public class AINpcStudioConfig
        {
            public string name;
            public StudioSubType studioType;
        }

        private List<AINpcStudioConfig> rtConfig = new()
        {
            new() { name = "草稿箱", studioType = StudioSubType.Drafts },
            new() { name = "已发布", studioType = StudioSubType.Published },
        };

        public override void OnCreate()
        {
            base.OnCreate();
            MessageHelper.AddListener(DraftMessage.RefreshDraft, OnAINpcStudioDraftListChange);
            MessageHelper.AddListener(DraftMessage.DraftSaveStatus, OnAINpcStudioDraftSaveStatusChange);
            LoadEvent.ReportTask(157, 10);
            MessageHelper.AddListener(MessageName.OnAINpcStudioDraftListChange, OnAINpcStudioDraftListChange);
            MessageHelper.AddListener(MessageName.OnAINpcStudioPublishedListChange, OnAINpcStudioPublishedListChange);
            MessageHelper.AddListener(MessageName.OnAssetDelete, OnAINpcStudioPublishedListChange);
            MessageHelper.AddListener(MessageName.OnBuyUgcItemSuccess, OnAINpcStudioPublishedListChange);
            MessageHelper.AddListener<UgcBaseInfo>(MessageName.OnAINpcDidPublishedNew, OnAINpcDidPublishedNew);
            
            DetailView.InitUI();
            InitBG();
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
            
            InitBottomPanel();
        }

        
        private void InitBottomPanel()
        {
            //事件注入
            DraftsList.StudioType = StudioSubType.Drafts;
            DraftsList.SetItemOnClickAct(OnDraftsItemClick);
            
            PublishedList.StudioType = StudioSubType.Published;
            PublishedList.SetItemOnClickAct(OnPublishedItemClick);
            navigationBarTabs.AddBackBtnClickListener(CloseSelf);
            foreach (var cfg in rtConfig)
            {
                navigationBarTabs.CreateItem(cfg.studioType.ToString(), cfg.name).SetIsSelect(false);
            }

            navigationBarTabs.AddBackBtnClickListener(CloseSelf);
            navigationBarTabs.AddItemSelectCallBack(RTClick);
            navigationBarTabs.SetSelect(0);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            MessageHelper.RemoveListener(DraftMessage.RefreshDraft, OnAINpcStudioDraftListChange);
            MessageHelper.RemoveListener(DraftMessage.DraftSaveStatus, OnAINpcStudioDraftSaveStatusChange);
            MessageHelper.RemoveListener(MessageName.OnAINpcStudioDraftListChange, OnAINpcStudioDraftListChange);
            MessageHelper.RemoveListener(MessageName.OnAINpcStudioPublishedListChange, OnAINpcStudioPublishedListChange);
            MessageHelper.RemoveListener(MessageName.OnAssetDelete, OnAINpcStudioPublishedListChange);
            MessageHelper.RemoveListener(MessageName.OnBuyUgcItemSuccess, OnAINpcStudioPublishedListChange);
            MessageHelper.RemoveListener<UgcBaseInfo>(MessageName.OnAINpcDidPublishedNew, OnAINpcDidPublishedNew);
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
                "avatar_icon_1","avatar_icon_2","avatar_icon_3","avatar_icon_4"
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
                case StudioSubType.Template:
                    DraftsList.gameObject.SetActive(false);
                    PublishedList.gameObject.SetActive(false);
                    break;
                
                case StudioSubType.Drafts:
                    DraftsList.gameObject.SetActive(true);
                    PublishedList.gameObject.SetActive(false);
                    DraftsList.OnSelectView();
                    break;
            
                case StudioSubType.Published:
                    DraftsList.gameObject.SetActive(false);
                    PublishedList.gameObject.SetActive(true);
                    PublishedList.OnSelectView();
                    break;
            }
        }

        private void OnAINpcStudioDraftSaveStatusChange()
        {
            if(curSelectType == StudioSubType.Drafts)
                OnSelectView(StudioSubType.Drafts);
        }

        private void OnAINpcStudioDraftListChange()
        {
            if(curSelectType == StudioSubType.Drafts)
                OnSelectView(StudioSubType.Drafts);
            else
            {
                navigationBarTabs.SetSelect(0);
            }
        }
        
        private void OnAINpcStudioPublishedListChange()
        {
            DetailView.gameObject.SetActive(false);
            
            if(curSelectType == StudioSubType.Published)
                OnSelectView(StudioSubType.Published);
            else
            {
                navigationBarTabs.SetSelect(1);
            }
        }

        private ContestInfo _contestData;
        public void SetContestFlag(ContestInfo contestData)
        {
            _contestData = contestData;
        }

        private void OnAINpcDidPublishedNew(UgcBaseInfo ugcBaseInfo)
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
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.AINpc ,item.npc.id);
        }
    }
}
