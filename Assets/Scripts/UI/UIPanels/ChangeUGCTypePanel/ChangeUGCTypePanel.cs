using System.Collections;
using System.Collections.Generic;
using Es;
using GameData.PgcData;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.U2D;

public enum ChangeUGCViewType
{
    Choose,
    Result
}

public class ChangeUGCTypePanel :  BasePanel<ChangeUGCTypePanel>
{
    [SerializeField] private Transform BG;
    [SerializeField] private SpriteAtlas bgAtlas;
    [SerializeField] private GameObject chooseViewRoot;
    [SerializeField] private GameObject resultViewRoot;
    [SerializeField] private CButton backBtn;

    [Header("ChooseView")]
    [SerializeField] private LoadingButton nextBtn;

    [SerializeField] private Transform content;

    [SerializeField] private GameObject itemPrefab;

    [Header("ResultView")]
    [SerializeField] private CButton gotoBtn;

    [SerializeField] private ChangeUGCTypeItem fromItem;
    [SerializeField] private ChangeUGCTypeItem toItem;
    
    private ChangeUGCViewType curViewType = ChangeUGCViewType.Choose;
    private DraftListItem curUgcInfo;
    private ChangeUGCTypeConfig srcChangeConfig;
    private ChangeUGCTypeConfig targetChangeConfig;
    private List<ChangeUGCTypeItem> itemList = new List<ChangeUGCTypeItem>();
    private AssetDetailType _curOriAssetDetailType;
    
    public override void OnCreate()
    {
        base.OnCreate();
        backBtn.onClick.AddListener(OnBackBtnClick);
        nextBtn.onClick.AddListener(OnNextBtnClick);
        gotoBtn.onClick.AddListener(OnGotoBtnClick);
        
        nextBtn.SetClickAble(false);
        ShowView(ChangeUGCViewType.Choose);
        
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args == null || args.Length <= 0)
        {
            LoggerUtils.LogError("ChangeUGCTypePanel OnShow arg is null");
            return;
        }

        curUgcInfo = (DraftListItem)args[0];
        InitChooseView(curUgcInfo);
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
        switch (_curOriAssetDetailType)
        {
            case AssetDetailType.Pet:
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "pet_icon_1", "pet_icon_2", "pet_icon_3", "pet_icon_4"
                });
                break;
            
            case AssetDetailType.Prop:
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "store_icon1", "store_icon2", "store_icon3", "store_icon4", "store_icon5", "store_icon6"
                });
                break;
            
            default:
            case AssetDetailType.Instrument:
            case AssetDetailType.Skin:
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "avatar_icon_1", "avatar_icon_2", "avatar_icon_3", "avatar_icon_4"
                });
                break;
        }
        item.gameObject.SetActive(true);
    }

    public void ShowView(ChangeUGCViewType viewType)
    {
        if (viewType == ChangeUGCViewType.Choose)
        {
            chooseViewRoot.SetActive(true);
            resultViewRoot.SetActive(false);
        }
        else
        {
            chooseViewRoot.SetActive(false);
            resultViewRoot.SetActive(true);
        }

        curViewType = viewType;
    }
    
    #region ChooseView
    private void InitChooseView(DraftListItem draftListItem)
    {
        //衣服3D模版+乐器
        var tempConfigData = DataTables.GetClothesTemplateList();
        var clothesConfigData = tempConfigData.FindAll(x => x.IsProp);
        foreach (var config in clothesConfigData)
        {
            ChangeUGCTypeConfig changeUgcTypeConfig = new ChangeUGCTypeConfig
            {
                Name = config.Name,
                Cover = config.Cover,
                TemplateId = config.Id,
                AssetType = AssetDetailType.Skin
            };

            int subType = ChangeUGCTypeManager.Inst.GetAvatarSubTypeByTempleteId(config.Id);
            if (subType == (int)AvatarSubType.MusicalInstrument)//乐器
            {
                changeUgcTypeConfig.AssetType = AssetDetailType.Instrument;
            }

            CreatChooseItem(changeUgcTypeConfig);
        }
        
        //素材，目前仅有一个
        var propTempConfig = DataTables.GetPropTemplateList();
        foreach (var propConfig in propTempConfig)
        {  
            ChangeUGCTypeConfig changeUgcTypeConfig = new ChangeUGCTypeConfig
            {
                Name = "素材",
                Cover = "UGCProp_1",
                TemplateId = propConfig.Id,
                AssetType = AssetDetailType.Prop
            };
            
            CreatChooseItem(changeUgcTypeConfig);
        }
        
        //宠物3D模版
        var tempPetConfigData = DataTables.GetPetClothesTemplateList();
        var petClothesConfigData = tempPetConfigData.FindAll(x => x.IsProp);
        foreach (var config in petClothesConfigData)
        {
            ChangeUGCTypeConfig changeUgcTypeConfig = new ChangeUGCTypeConfig
            {
                Name = config.Name,
                Cover = config.Cover,
                TemplateId = config.Id,
                AssetType = AssetDetailType.Pet
            };
            CreatChooseItem(changeUgcTypeConfig);
        }
        
        //设置灰色
        if (draftListItem == null) return;
        foreach (var item in itemList)
        {
            var config = item.GetConfigData();
            if (draftListItem.propInfo != null && config.AssetType == AssetDetailType.Prop)
            {
                item.SetBan(true);
                srcChangeConfig = config;
                _curOriAssetDetailType = config.AssetType;
                break;
            }
            else if (draftListItem.skinInfo != null && draftListItem.skinInfo.templateId == config.TemplateId)
            {
                item.SetBan(true);
                srcChangeConfig = config;
                _curOriAssetDetailType = config.AssetType;
                break;
            }
        }
    }


    private ChangeUGCTypeItem CreatChooseItem(ChangeUGCTypeConfig template)
    {
        var item = Instantiate(itemPrefab,content);
        item.SetActive(true);
        var itemComp = item.GetComponent<ChangeUGCTypeItem>();
        itemComp.InitData(template);
        itemComp.AddClickListener(() =>
        {
            OnItemClick(itemComp);
        });
        itemList.Add(itemComp);
        return itemComp;
    }

    private void UnSelectAll()
    {
        foreach (var itemComp in itemList)
        {
            itemComp.SetSelect(false);
        }
        nextBtn.SetClickAble(false);
    }

    private void OnItemClick(ChangeUGCTypeItem item)
    {
        UnSelectAll();
        item.SetSelect(true);
        targetChangeConfig = item.GetConfigData();
        nextBtn.SetClickAble(true);
    }

    private void OnNextBtnClick()
    {
        bool isVip = VipDataManager.Inst.isVip;
        if (!isVip)
        {
            var joinVipType = new List<JoinVipType>();
            string joinVipTitle = LocalizationManager.Inst.GetLocalizedText("您正在使用的VIP功能：自由切换商品种类");
            joinVipType.Add(JoinVipType.ChangeUGCType);
            if (joinVipType.Count>0)
            {
                UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel,joinVipTitle, joinVipType);
                return;
            }
        }

        if (nextBtn.IsLoading)
        {
            TipPanel.ShowToast("正在转换，请稍候");
        }

        nextBtn.ShowLoading();
        ChangeUGCTypeManager.Inst.ChangeType(curUgcInfo,targetChangeConfig, (isSuccess) =>
        {
            nextBtn.HideLoading();
            if (isSuccess)
            {
                ShowView(ChangeUGCViewType.Result);
                InitResultView();
            }
            else
            {
                TipPanel.ShowToast("转换失败，请重试");
            }
        });
    }

    
    #endregion 
    
    
    #region ResultView

    private void InitResultView()
    {
        fromItem.InitData(srcChangeConfig);
        toItem.InitData(targetChangeConfig);
    }

    private void OnGotoBtnClick()
    {
        CloseOtherDraftPanel();
        if (targetChangeConfig.AssetType == AssetDetailType.Prop)
        {
            UIManager.Inst.OpenPanel(PanelId.AssetStudioDraftCommonPanel, MainViewType.Prop);
        }
        else if (targetChangeConfig.AssetType == AssetDetailType.Pet)
        {
            UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel, CharacterStyle.Pet, (int)AvatarStudioViewEnum.Drafts);
        }
        else if (targetChangeConfig.AssetType == AssetDetailType.Skin)
        {
            UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel, CharacterStyle.Avatar, (int)AvatarStudioViewEnum.Drafts);
        }
        else if (targetChangeConfig.AssetType == AssetDetailType.Instrument)
        {
            UIManager.Inst.OpenPanel(PanelId.MusicalInstrumentStudioPanel);
        }



        CloseSelf();
    }
    #endregion

    private void CloseOtherDraftPanel()
    {
        UIManager.Inst.ClosePanel(PanelId.AssetStudioDraftCommonPanel);
        UIManager.Inst.ClosePanel(PanelId.MusicalInstrumentStudioPanel);
        UIManager.Inst.ClosePanel(PanelId.AvatarStudioMainPanel);
    }

    private void OnBackBtnClick()
    {
        if (curViewType == ChangeUGCViewType.Result)
        {
            CloseOtherDraftPanel();
        }
        CloseSelf();
    }
    


}
