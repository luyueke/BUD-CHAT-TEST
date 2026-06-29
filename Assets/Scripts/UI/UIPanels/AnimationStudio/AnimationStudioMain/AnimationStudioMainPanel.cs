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

namespace Game.AnimationStudio
{
    public class AnimationStudioMainPanel : BasePanel<AnimationStudioMainPanel>
    {
        [SerializeField] private Transform _trans_Bg;
        [SerializeField] private NavigationBarTabs navigationBarTabs;
        [SerializeField] private AnimationStudioTemplatePanel TemplateList;
        [SerializeField] private AnimationStudioInfoPanel DraftsList;
        [SerializeField] private AnimationStudioInfoPanel PublishedList;
        [SerializeField] private AnimationStudioDetailView DetailView;
        private StudioSubType curSelectType;
        private AnimationStudioType _animationStudioType;
        
        public class AnimStudioConfig
        {
            public string name;
            public StudioSubType studioType;
        }

        private List<AnimStudioConfig> rtConfig = new()
        {
            new() { name = "模版", studioType = StudioSubType.Template },
            new() { name = "草稿箱", studioType = StudioSubType.Drafts },
            new() { name = "已发布", studioType = StudioSubType.Published },
        };

        public override void OnCreate()
        {
            base.OnCreate();
            MessageHelper.AddListener(DraftMessage.RefreshDraft, OnUgcAnimStudioDraftListChange);
            MessageHelper.AddListener(DraftMessage.DraftSaveStatus, OnUgcAnimStudioDraftSaveStatusChange);
            
            MessageHelper.AddListener(MessageName.OnUgcAnimStudioDraftListChange, OnUgcAnimStudioDraftListChange);
            MessageHelper.AddListener(MessageName.OnUgcAnimStudioPublishedListChange, OnUgcAnimStudioPublishedListChange);
            MessageHelper.AddListener(MessageName.OnAssetDelete, OnUgcAnimStudioPublishedListChange);
            MessageHelper.AddListener<UgcBaseInfo>(MessageName.OnUgcAnimDidPublishedNew, OnUgcAnimDidPublishedNew);
            
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

            int defaultTabIndex = 0;
            if (args != null && args.Length > 0)
            {
                _animationStudioType = (AnimationStudioType)args[0];
            }
            else
            {
                _animationStudioType = AnimationStudioType.Animation;
                defaultTabIndex = 1; // "草稿箱"
            }
            DetailView.InitType(_animationStudioType);
            InitBottomPanel(defaultTabIndex);
        }

        
        private void InitBottomPanel(int defaultTabIndex = 0)
        {
            TemplateList.Init(_animationStudioType);
            
            //事件注入
            DraftsList.AnimationStudioType = _animationStudioType;
            DraftsList.StudioType = StudioSubType.Drafts;
            DraftsList.SetItemOnClickAct(OnDraftsItemClick);
            
            PublishedList.AnimationStudioType = _animationStudioType;
            PublishedList.StudioType = StudioSubType.Published;
            PublishedList.SetItemOnClickAct(OnPublishedItemClick);
            navigationBarTabs.AddBackBtnClickListener(CloseSelf);
            foreach (var cfg in rtConfig)
            {
                navigationBarTabs.CreateItem(cfg.studioType.ToString(), cfg.name).SetIsSelect(false);
            }

            navigationBarTabs.AddBackBtnClickListener(CloseSelf);
            navigationBarTabs.AddItemSelectCallBack(RTClick);
            navigationBarTabs.SetSelect(defaultTabIndex);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            MessageHelper.RemoveListener(DraftMessage.RefreshDraft, OnUgcAnimStudioDraftListChange);
            MessageHelper.RemoveListener(DraftMessage.DraftSaveStatus, OnUgcAnimStudioDraftSaveStatusChange);
            MessageHelper.RemoveListener(MessageName.OnUgcAnimStudioDraftListChange, OnUgcAnimStudioDraftListChange);
            MessageHelper.RemoveListener(MessageName.OnUgcAnimStudioPublishedListChange, OnUgcAnimStudioPublishedListChange);
            MessageHelper.RemoveListener(MessageName.OnAssetDelete, OnUgcAnimStudioPublishedListChange);
            MessageHelper.RemoveListener<UgcBaseInfo>(MessageName.OnUgcAnimDidPublishedNew, OnUgcAnimDidPublishedNew);
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
                "animStudio_icon1","animStudio_icon2","animStudio_icon3"
            });
            item.gameObject.SetActive(true);
        }
        
        private void RTClick(TabItem item, int index)
        {
            var data = rtConfig[index];
            OnSelectView(data.studioType);
        }
        
        public void OnSelectView(StudioSubType studioType)
        {
            curSelectType = studioType;
            switch (studioType)
            {
                case StudioSubType.Template:
                    TemplateList.gameObject.SetActive(true);
                    DraftsList.gameObject.SetActive(false);
                    PublishedList.gameObject.SetActive(false);
                    break;
                
                case StudioSubType.Drafts:
                    DraftsList.gameObject.SetActive(true);
                    PublishedList.gameObject.SetActive(false);
                    TemplateList.gameObject.SetActive(false);
                    DraftsList.OnSelectView();
                    break;
            
                case StudioSubType.Published:
                    DraftsList.gameObject.SetActive(false);
                    PublishedList.gameObject.SetActive(true);
                    TemplateList.gameObject.SetActive(false);
                    PublishedList.OnSelectView();
                    break;
            }
        }

        private void OnUgcAnimStudioDraftSaveStatusChange()
        {
            if(curSelectType == StudioSubType.Drafts)
                OnSelectView(StudioSubType.Drafts);
        }

        private void OnUgcAnimStudioDraftListChange()
        {
            if(curSelectType == StudioSubType.Drafts)
                OnSelectView(StudioSubType.Drafts);
            else
            {
                navigationBarTabs.SetSelect(1);
            }
        }
        
        private void OnUgcAnimStudioPublishedListChange()
        {
            if(curSelectType == StudioSubType.Published)
                OnSelectView(StudioSubType.Published);
            else
            {
                navigationBarTabs.SetSelect(2);
            }
        }

        private ContestInfo _contestData;
        public void SetContestFlag(ContestInfo contestData)
        {
            _contestData = contestData;
        }

        private void OnUgcAnimDidPublishedNew(UgcBaseInfo ugcBaseInfo)
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
            switch (_animationStudioType)
            {
                case AnimationStudioType.Animation:
                    UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcAnim ,item.animInfo.id);
                    break;
                case AnimationStudioType.Pose:
                    UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcPose ,item.poseInfo.id);
                    break;
            }
        }
    }
    
    public enum AnimationStudioType
    {
        Animation = 1,
        Pose = 2,
    }
}
