using System.Collections;
using System.Collections.Generic;
using Basic.Extensions;
using EventTracking;
using Game.Audio;
using Game.Base;
using GameData;
using Message;
using Newbie;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public enum AvatarStudioViewEnum
{
    Temp,
    Drafts,
    Publish
}
public class AvatarStudioMainPanel : BasePanel<AvatarStudioMainPanel>
{
    public Transform BG;
    [SerializeField] private NavigationBarTabs navigationBarTabs;
    [SerializeField] private GameObject recommendView;
    private AvatarStudioTemplateView templateView;
    private AvatarStudioDraftsView draftsView;
    private AvatarStudioPublishView publishView;
    private ContestAvatarStudioView contestTemplateView;
    private CButton leaderBoardBtn;
    private bool IsForceRefreshData = false;
    private CharacterStyle currentCharacterStyle = CharacterStyle.Avatar;
    string key = "FirstOpenAvatrStudio_" + AccountDataManager.Inst.Uid;

    public class AvatarStudioConfig
    {
        public string name;
        public AvatarStudioViewEnum type;
    }
    private List<AvatarStudioConfig> rtConfig = new()
    {
        new(){name = "模板", type = AvatarStudioViewEnum.Temp},
        new(){name = "草稿箱", type = AvatarStudioViewEnum.Drafts},
        new(){name = "已发布", type = AvatarStudioViewEnum.Publish},
    };
    public override void OnCreate()
    {
        InitUI();
        navigationBarTabs.AddBackBtnClickListener(OnBackBtnClick);
        LoadEvent.ReportTask(157, 2);
        foreach (var cfg in rtConfig)
        {
            navigationBarTabs.CreateItem(cfg.type.ToString(), cfg.name).SetIsSelect(false);
        }
        MessageHelper.AddListener(DraftMessage.RefreshDraft, OnClothGameOut);
        MessageHelper.AddListener(MessageName.ClothPublishInEditSuccess, ForceGotoPublishView);
        MessageHelper.AddListener(MessageName.OnAssetDelete, ForceGotoPublishView);
        string key = "FirstOpenFittingRoomPanel" + AccountDataManager.Inst.UserInfo.uid;
        if (SignInPanel.isNewPlayer && !PlayerPrefs.HasKey(key)&& !PlayerPrefs.HasKey("guide_ID_6"))
        {
            PlayerPrefs.SetInt("guide_ID_6", 1);
            PlayerPrefs.Save();
            LoadEvent.ReportPopupStatus("6", "guide_ID");
        }
        
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        recommendView?.SetActive(AccountDataManager.Inst.UserInfo.isNewUser == 1);
        if (!GameController.IsInHallScene())
        {
            TipPanel.ShowToast("游玩过程中无法进行创作哦");
            CloseSelf();
            return;
        }
        int index = (int) AvatarStudioViewEnum.Temp;
        if (args.Length > 0)
        {
            currentCharacterStyle = (CharacterStyle)args[0];
        }
                
        //跳转Tab
        if (args.Length == 2)
        {
            templateView.gameObject.SetActive(false);
            draftsView.gameObject.SetActive(false);
            publishView.gameObject.SetActive(false);
            index = (int)args[1];
        }
        
        navigationBarTabs.RemoveItemSelectCallBack(RTClick);
        navigationBarTabs.AddItemSelectCallBack(RTClick);
        navigationBarTabs.PublishBundleButton.onClick.AddListener(OnPublishBundleClick);
        var curTab = navigationBarTabs.GetItemByIndex(index);
        navigationBarTabs.SelectWithoutCallback(index);
        RTClick(curTab,index);
        InitBG();
    }
    
    private void InitBG()
    {
        if (BG == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        switch (currentCharacterStyle)
        {
            case CharacterStyle.Pet:
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "pet_icon_1", "pet_icon_2", "pet_icon_3", "pet_icon_4"
                });
                break;
            
            default:
            case CharacterStyle.Avatar:
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "avatar_icon_1", "avatar_icon_2", "avatar_icon_3", "avatar_icon_4"
                });
                break;
        }
        item.gameObject.SetActive(true);
    }

    private void InitUI()
    {
        templateView = GameObjectEx.FindChildByName(transform, "TemplateView").GetComponent<AvatarStudioTemplateView>();
        draftsView = GameObjectEx.FindChildByName(transform, "DraftsView").GetComponent<AvatarStudioDraftsView>();
        publishView = GameObjectEx.FindChildByName(transform, "PublishView").GetComponent<AvatarStudioPublishView>();
        contestTemplateView = GameObjectEx.FindChildByName(transform, "ContestTemplateView").GetComponent<ContestAvatarStudioView>();
        contestTemplateView.gameObject.SetActive(false);
        draftsView.GotoTemplate = ForceGotoTemplateView;
        draftsView.GotoPublish = ForceGotoPublishView;
    }
    private void RTClick(TabItem item, int index)
    {
        OpenViewByIndex(index);
    }

    private void OpenViewByIndex(int index)
    {
        var data = rtConfig[index];
        SetViewOpen(data);
    }

    public void OpenView(AvatarStudioViewEnum viewEnum)
    {
        int index = (int)viewEnum;
        OpenViewByIndex(index);
        navigationBarTabs.SelectWithoutCallback(index);
    }

    private void SetViewOpen(AvatarStudioConfig config)
    {
        //关闭开启的view
        if (config.type!=AvatarStudioViewEnum.Temp)
        {
            templateView.Hide();
        }
        if (config.type!=AvatarStudioViewEnum.Drafts)
        {
            draftsView.Hide();
        }
        if (config.type!=AvatarStudioViewEnum.Publish)
        {
            publishView.Hide();
        }
        navigationBarTabs.PublishBundleButton.gameObject.SetActive(false);
        switch (config.type)
        {
            case AvatarStudioViewEnum.Temp:
                templateView.Show(currentCharacterStyle);
                break;
            case AvatarStudioViewEnum.Drafts:
                draftsView.Show(currentCharacterStyle);
                break;
            case AvatarStudioViewEnum.Publish:
                publishView.Show(currentCharacterStyle);
                navigationBarTabs.PublishBundleButton.gameObject.SetActive(true);
                break;
        }
    }

    public void OnClothGameOut()
    {
        if (draftsView.IsShow())
        {
            draftsView.RequestDataList();
        }
        else
        {
            navigationBarTabs.SetSelect((int)AvatarStudioViewEnum.Drafts);
        }
      
    }

    public void OnPublishBundleClick()
    {
        // PublishCurrencyPanel publishCurrencyPanel = UIManager.Inst.OpenPanel<PublishCurrencyPanel>(PanelId.PublishCurrencyPanel);
        // publishCurrencyPanel.SetCallback((type =>
        // {
        //     UIManager.Inst.OpenPanel(PanelId.SelectUgcBundleItemPanel, type);
        // }));
        UIManager.Inst.OpenPanel(PanelId.SelectUgcBundleItemPanel, CurrencyType.PinkCoin,currentCharacterStyle);
    }

    public void ForceGotoTemplateView()
    {
        navigationBarTabs.SetSelect((int)AvatarStudioViewEnum.Temp);
    }
    public void ForceGotoPublishView()
    {
        if (publishView.IsShow())
        {
            publishView.RequestDataList();
        }
        else
        {
            navigationBarTabs.SetSelect((int)AvatarStudioViewEnum.Publish);
        }
    }
    private void OnBackBtnClick()
    {
        ContestDataManager.Inst.SkinContest = null;
        UIManager.Inst.BackToLastWindow();
    }

    public override void OnHidden()
    {
        
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener(DraftMessage.RefreshDraft, OnClothGameOut);
        MessageHelper.RemoveListener(MessageName.ClothPublishInEditSuccess, ForceGotoPublishView);
        MessageHelper.RemoveListener(MessageName.OnAssetDelete, ForceGotoPublishView);
    }

    #region 数据强刷控制

    public void SetForceRefreshData()
    {
        IsForceRefreshData = true;
    }
    private void OpenViewSetFreshData(AvatarStudioViewEnum type)
    {
        if (type != AvatarStudioViewEnum.Temp)
        {
            SetForceRefreshData();
        }
    }

    #endregion

    #region 衣服大赛

    public void ShowContestView(ContestInfo info)
    {
        if (info.CurrentContestType == BUDContestType.Skin || info.CurrentContestType == BUDContestType.PetSkin) 
        {
            contestTemplateView.gameObject.SetActive(true);
            contestTemplateView.Show(info);  
        }
        
        ContestDataManager.Inst.SkinContest = info;
    }
    
    #endregion
}
