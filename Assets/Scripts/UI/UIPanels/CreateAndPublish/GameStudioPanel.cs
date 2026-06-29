using System.Collections.Generic;
using BUD.GameStudio;
using Game.Audio;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

/// <summary>
/// 草稿箱页面
/// </summary>
public class GameStudioPanel : BasePanel<GameStudioPanel>
{
    [SerializeField] private Transform _trans_Bg;
    [SerializeField] private NavigationBarTabs navigationBarTabs;
    [SerializeField] private GameStudioMainView gameStudioMainView;
    [SerializeField] private GameDetailView gameDetailView;

    public enum GameStudioEnum
    {
        Map,
        Prop,
        Material,
        Avatar
    }

    public class GameStudioConfig
    {
        public string name;
        public GameStudioEnum type;
        public GameStudioEnum GameStudioEnum;
        public StudioSubType StudioSubType;
    }

    private List<GameStudioConfig> rtConfig = new()
    {
        new() { name = "草稿箱", GameStudioEnum = GameStudioEnum.Map, StudioSubType = StudioSubType.Drafts },
        new() { name = "已发布", GameStudioEnum = GameStudioEnum.Map, StudioSubType = StudioSubType.Published },
    };

    public override void OnCreate()
    {
        base.OnCreate();
        InitBG();
        navigationBarTabs.AddBackBtnClickListener(OnBackBtnClick);
        foreach (var cfg in rtConfig)
        {
            navigationBarTabs.CreateItem(cfg.StudioSubType.ToString(), cfg.name).SetIsSelect(false);
        }

        navigationBarTabs.AddItemSelectCallBack(RTClick);
        navigationBarTabs.SetSelect((int)StudioSubType.Drafts);
        
        gameDetailView.SetChangeViewAction((studioSubType) =>
        {
            navigationBarTabs.SetSelect((int)studioSubType);
        });
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
            "store_icon4","store_icon5","store_icon6"
        });
        item.gameObject.SetActive(true);
    }

    private void RTClick(TabItem item, int index)
    {
        var data = rtConfig[index];
        gameStudioMainView.InitUI(data.StudioSubType);
    }

    private void OnBackBtnClick()
    {
        UIManager.Inst.ClosePanel(this);
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
        gameStudioMainView.DestroyUI();
    }

    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }

    public void GoToPublishMapPanel()
    {
        if (gameStudioMainView && gameStudioMainView.gameDetailView)
        {
            gameStudioMainView.gameDetailView.GoToPublishMapPanel();
        }
    }

    //跳转指定顶部Tab
    public void SelectTopTab(StudioSubType studioSubType)
    {
        if (navigationBarTabs != null)
        {
            navigationBarTabs.SetSelect((int)studioSubType); 
        }
    }
}
