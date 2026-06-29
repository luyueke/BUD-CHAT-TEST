using System.Collections.Generic;
using Game.Base;
using Game.MusicalInstrument;
using GameData.Base;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

/// <summary>
/// game studio数据管理
/// </summary>
public enum ActorSubType
{
    Drafts,
    Published
}

public class TheatreActorEditorMainPanel : BasePanel<TheatreActorEditorMainPanel>
{
    [SerializeField] private Transform _trans_Bg;
    [SerializeField] private NavigationBarTabs navigationBarTabs;
    [SerializeField] private TheatreActorEditorInfoPanel DraftsList;
    [SerializeField] private TheatreActorEditorInfoPanel PublishedList;
    [SerializeField] private TheatreActorDetailView DetailView;
    private ActorSubType curSelectType;

    public class TheatreActorConfig
    {
        public string name;
        public ActorSubType actorType;
    }

    private List<TheatreActorConfig> rtConfig = new()
    {
        new() { name = "草稿箱", actorType = ActorSubType.Drafts },
        new() { name = "已发布", actorType = ActorSubType.Published },
    };

    public override void OnCreate()
    {
        base.OnCreate();
        MessageHelper.AddListener(MessageName.OnActorStudioDraftListChange, OnUgcInstrumentDraftsListChange);
        MessageHelper.AddListener(MessageName.OnActorStudioPublishedListChange, OnUgcInstrumentPublishedListChange);
        MessageHelper.AddListener(MessageName.OnAssetDelete, OnUgcInstrumentPublishedListChange);
        DetailView.InitUI();
        //事件注入
        DraftsList.actorType = ActorSubType.Drafts;
        DraftsList.SetItemOnClickAct(OnDraftsItemClick);

        PublishedList.actorType = ActorSubType.Published;
        PublishedList.SetItemOnClickAct(OnPublishedItemClick);
        InitBG();
        navigationBarTabs.AddBackBtnClickListener(OnBack);
        foreach (var cfg in rtConfig)
        {
            navigationBarTabs.CreateItem(cfg.actorType.ToString(), cfg.name).SetIsSelect(false);
        }

        navigationBarTabs.AddBackBtnClickListener(CloseSelf);
        navigationBarTabs.AddItemSelectCallBack(RTClick);
        navigationBarTabs.SetSelect((int)ActorSubType.Drafts);
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
        MessageHelper.RemoveListener(MessageName.OnActorStudioDraftListChange, OnUgcInstrumentDraftsListChange);
        MessageHelper.RemoveListener(MessageName.OnActorStudioPublishedListChange, OnUgcInstrumentPublishedListChange);
        MessageHelper.RemoveListener(MessageName.OnAssetDelete, OnUgcInstrumentPublishedListChange);
    }

    private void InitBG()
    {
        if (_trans_Bg == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab").Instantiate(_trans_Bg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "theatre_icon1", "theatre_icon2"
        });

        item.gameObject.SetActive(true);
    }

    private void RTClick(TabItem item, int index)
    {
        var data = rtConfig[index];
        OnSelectView(data.actorType);
    }

    private void OnSelectView(ActorSubType actorType)
    {
        curSelectType = actorType;
        switch (actorType)
        {
            case ActorSubType.Drafts:
                DraftsList.gameObject.SetActive(true);
                PublishedList.gameObject.SetActive(false);
                DraftsList.GetData();
                break;

            case ActorSubType.Published:
                DraftsList.gameObject.SetActive(false);
                PublishedList.gameObject.SetActive(true);
                PublishedList.GetData();
                break;
        }
    }

    private void OnUgcInstrumentDraftsListChange()
    {
        if (curSelectType == ActorSubType.Drafts)
            OnSelectView(ActorSubType.Drafts);
        else
        {
            navigationBarTabs.SetSelect((int)ActorSubType.Drafts);
        }
    }

    private void OnUgcInstrumentPublishedListChange()
    {
        if (curSelectType == ActorSubType.Published)
            OnSelectView(ActorSubType.Published);
        else
        {
            navigationBarTabs.SetSelect((int)ActorSubType.Published);
        }
    }

    private void OnDraftsItemClick(DraftListItem item)
    {
        DetailView.UpdateInfo(item);
    }

    private void OnPublishedItemClick(DraftListItem item)
    {
        if (item?.actorInfo == null) return;
        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Actor, item.actorInfo.id);
    }
}

