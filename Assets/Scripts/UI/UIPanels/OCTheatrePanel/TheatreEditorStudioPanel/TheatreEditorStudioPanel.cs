using System.Collections.Generic;
using Game.Base;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public enum TheatreStudioSubType
{
    Drafts,
    Published
}

public class TheatreEditorStudioPanel : BasePanel<TheatreEditorStudioPanel>
{
    [SerializeField] private Transform _trans_Bg;
    [SerializeField] private NavigationBarTabs navigationBarTabs;
    [SerializeField] private TheatreEditorStudioInfoPanel DraftsList;
    [SerializeField] private TheatreEditorStudioInfoPanel PublishedList;
    [SerializeField] private TheatreEditorStudioDetailView DetailView;
    private TheatreStudioSubType curSelectType;

    public class TheatreStudioConfig
    {
        public string name;
        public TheatreStudioSubType studioType;
    }

    private List<TheatreStudioConfig> rtConfig = new()
    {
        new() { name = "草稿箱", studioType = TheatreStudioSubType.Drafts },
        new() { name = "已发布", studioType = TheatreStudioSubType.Published },
    };

    public override void OnCreate()
    {
        base.OnCreate();
        MessageHelper.AddListener(MessageName.OnTheatreStudioDraftListChange, OnTheatreDraftsListChange);
        MessageHelper.AddListener(MessageName.OnTheatreStudioPublishedListChange, OnTheatrePublishedListChange);
        MessageHelper.AddListener(MessageName.OnAssetDelete, OnTheatrePublishedListChange);
        DetailView.InitUI();

        DraftsList.studioType = TheatreStudioSubType.Drafts;
        DraftsList.SetItemOnClickAct(OnDraftsItemClick);

        PublishedList.studioType = TheatreStudioSubType.Published;
        PublishedList.SetItemOnClickAct(OnPublishedItemClick);
        InitBG();
        navigationBarTabs.AddBackBtnClickListener(OnBack);
        foreach (var cfg in rtConfig)
        {
            navigationBarTabs.CreateItem(cfg.studioType.ToString(), cfg.name).SetIsSelect(false);
        }

        navigationBarTabs.AddBackBtnClickListener(CloseSelf);
        navigationBarTabs.AddItemSelectCallBack(RTClick);
        navigationBarTabs.SetSelect((int)TheatreStudioSubType.Drafts);
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
        MessageHelper.RemoveListener(MessageName.OnTheatreStudioDraftListChange, OnTheatreDraftsListChange);
        MessageHelper.RemoveListener(MessageName.OnTheatreStudioPublishedListChange, OnTheatrePublishedListChange);
        MessageHelper.RemoveListener(MessageName.OnAssetDelete, OnTheatrePublishedListChange);
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
            "theatre_icon1", "theatre_icon2"
        });
        item.gameObject.SetActive(true);
    }

    private void RTClick(TabItem item, int index)
    {
        var data = rtConfig[index];
        OnSelectView(data.studioType);
    }

    private void OnSelectView(TheatreStudioSubType studioType)
    {
        curSelectType = studioType;
        switch (studioType)
        {
            case TheatreStudioSubType.Drafts:
                DraftsList.gameObject.SetActive(true);
                PublishedList.gameObject.SetActive(false);
                DraftsList.GetData();
                break;

            case TheatreStudioSubType.Published:
                DraftsList.gameObject.SetActive(false);
                PublishedList.gameObject.SetActive(true);
                PublishedList.GetData();
                break;
        }
    }

    private void OnTheatreDraftsListChange()
    {
        if (curSelectType == TheatreStudioSubType.Drafts)
            OnSelectView(TheatreStudioSubType.Drafts);
        else
        {
            navigationBarTabs.SetSelect((int)TheatreStudioSubType.Drafts);
        }
    }

    private void OnTheatrePublishedListChange()
    {
        if (curSelectType == TheatreStudioSubType.Published)
            OnSelectView(TheatreStudioSubType.Published);
        else
        {
            navigationBarTabs.SetSelect((int)TheatreStudioSubType.Published);
        }
    }

    private void OnDraftsItemClick(DraftListItem item)
    {
        DetailView.UpdateInfo(item);
    }

    private void OnPublishedItemClick(DraftListItem item)
    {
        if (item?.theatreInfo == null) return;
        UIManager.Inst.OpenPanel(PanelId.TheatreInfoPanel, item.theatreInfo);
    }
}
