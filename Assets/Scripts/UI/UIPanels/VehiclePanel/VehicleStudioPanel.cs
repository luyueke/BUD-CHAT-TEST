using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.MusicalInstrument;
using GameData;
using GameData.Base;
using Message;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class VehicleStudioPanel : BasePanel<VehicleStudioPanel>
{
    [SerializeField] private Transform _trans_Bg;
    [SerializeField] private NavigationBarTabs navigationBarTabs;
    [SerializeField] private VehicleStudioInfoPanel DraftsList;
    [SerializeField] private VehicleStudioInfoPanel PublishedList;
    [SerializeField] private VehicleDetailView DetailView;

    private StudioSubType curSelectType;

    public class VehicleStudioConfig
    {
        public string name;
        public StudioSubType studioType;
    }

    private List<VehicleStudioConfig> rtConfig = new()
    {
        new() { name = "草稿箱", studioType = StudioSubType.Drafts },
        new() { name = "已发布", studioType = StudioSubType.Published },
    };

    public override void OnCreate()
    {

        DetailView.InitUI();

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
        MessageHelper.AddListener(DraftMessage.RefreshDraft, OnUgcVehicleDraftsListChange);
        MessageHelper.AddListener(MessageName.OnUgcVehicleDraftsListChange, OnUgcVehicleDraftsListChange);
        MessageHelper.AddListener<VehicleDraftInfo>(DraftMessage.DraftSaveStatus, OnUgcVehicleDraftSaveStatusChange);
        MessageHelper.AddListener(MessageName.OnUgcVehiclePublishedListChange, OnUgcVehiclePublishedListChange);
        MessageHelper.AddListener(MessageName.OnAssetDelete, OnUgcVehiclePublishedListChange);
        MessageHelper.AddListener<UgcBaseInfo>(MessageName.UgcVehicleDidPublishedNew, OnUgcVehiclePublishedNewItem);
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

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener(DraftMessage.RefreshDraft, OnUgcVehicleDraftsListChange);
        MessageHelper.RemoveListener(MessageName.OnUgcVehicleDraftsListChange, OnUgcVehicleDraftsListChange);
        MessageHelper.RemoveListener<VehicleDraftInfo>(DraftMessage.DraftSaveStatus, OnUgcVehicleDraftSaveStatusChange);
        MessageHelper.RemoveListener(MessageName.OnUgcVehiclePublishedListChange, OnUgcVehiclePublishedListChange);
        MessageHelper.RemoveListener(MessageName.OnAssetDelete, OnUgcVehiclePublishedListChange);
        MessageHelper.RemoveListener<UgcBaseInfo>(MessageName.UgcVehicleDidPublishedNew, OnUgcVehiclePublishedNewItem);

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
                "vehicle_icon_1","vehicle_icon_2","vehicle_icon_3"
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

    private void OnUgcVehicleDraftSaveStatusChange(VehicleDraftInfo skinActionBaseDraftInfo = null)
    {
        if (curSelectType == StudioSubType.Drafts)
            OnSelectView(StudioSubType.Drafts);
    }

    private void OnBack()
    {
        CloseSelf();
    }


    private void OnDraftsItemClick(DraftListItem item)
    {
        DetailView.UpdateInfo(item);
    }

    private void OnPublishedItemClick(DraftListItem item)
    {
        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Vehicle, item.vehicleInfo.id, item.vehicleInfo.ugcStyle);
    }

    private void OnUgcVehicleDraftsListChange()
    {
        if (curSelectType == StudioSubType.Drafts)
            OnSelectView(StudioSubType.Drafts);
        else
        {
            navigationBarTabs.SetSelect((int)StudioSubType.Drafts);
        }
    }

    private void OnUgcVehiclePublishedListChange()
    {
        if (curSelectType == StudioSubType.Published)
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

    private void OnUgcVehiclePublishedNewItem(UgcBaseInfo ugcBaseInfo)
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
            _contestData != null && _contestData.CurrentContestType == BUDContestType.Vehicle;
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
                    var panel = UIManager.Inst.OpenPanel<JoinInContestPanel>(PanelId.JoinInContestPanel, new List<ContestInfo>() { _contestData }, creationId);
                    panel.ShowJoinSuccess(_contestData);
                }
            });
        }
        else
        {
            var contests = ContestDataManager.Inst.canJoinContests(BUDContestType.Vehicle);
            Debug.LogError("OnUgcVehiclePublishedNewItem 444 _contestData=" + JsonConvert.SerializeObject(contests));
            if (contests != null && contests.Count > 0)
            {
                UIManager.Inst.OpenPanel(PanelId.JoinInContestPanel, contests, creationId);
            }
        }
    }
}
