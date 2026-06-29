using System;
using Game.COSXML;
using Es;
using Game.Audio;
using Game.Base;
using Game.Config;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using UnityEngine;
using UnityEngine.U2D;

public class ContestAvatarStudioView: MonoBehaviour
{
    [SerializeField] private Transform BG;
    [SerializeField] private NavigationBar navigationBar;
    private ContestAvatarTemplateView templateView;

    private void Awake()
    {
        InitUI();
        navigationBar.AddBackBtnClickListener(() =>
        {
            ContestDataManager.Inst.SkinContest = null;
            UIManager.Inst.BackToLastWindow();
        });
    }
    
    private ContestInfo contestInfo;

    public ContestInfo ActiveContestInfo
    {
        get
        {
            return contestInfo;
        }
    }
    public void Show(ContestInfo info)
    {
        this.contestInfo = info;
        var isPetSkin = contestInfo.CurrentContestType == BUDContestType.PetSkin;
        ContestEventManager.Inst.SetCustomBg(BG, contestInfo.backgroundIconUrlList, contestInfo.backgroundColor);

        var templateLists = contestInfo?.templateIdList;
        templateView.SetData(templateLists, isPet:isPetSkin);
    }
    
    private void InitUI()
    {
        templateView = GameObjectEx.FindChildByName(transform, "TemplateView").GetComponent<ContestAvatarTemplateView>();
        templateView.ClickTemplateAction = OnClickTempalte;
    }

    private void OnClickTempalte(ClothesTemplate cData, bool IsPet)
    { 
        MessageHelper.AddListener(MessageName.DidOpenUgcCloseEidtPage, OnOpenUgcEditPage);
        
        string tempSpriteatlasPath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";
        var resData = DataTables.GetGameResData(cData.Id);
        if (resData==null)
        {
            TipPanel.ShowToast("未找到ID为"+cData.Id+"的模版");
            return;
        }
        //创建名字
        string formattedDate = DateTime.Now.ToString("yyyy-MM-dd");
        string defaultName = LocalizationManager.Inst.GetLocalizedText("草稿");
        string name = defaultName+"-" + formattedDate;
        //增加封面
        var iconAtlas =Loader.Load<SpriteAtlas>( tempSpriteatlasPath,this.gameObject);
        ClothesTemplate template = null;
        
        if (IsPet)
        {
            template = DataTables.GetPetClothesTemplate(cData.Id);
        }
        else
        {
            template = DataTables.GetClothesTemplate(cData.Id);
        }
        var sprite = iconAtlas.GetSprite(template.Cover);
        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(new SkinInfo()
        {
            name = name
        },null ,LoadingType.Cloth,s:sprite);

        var defUrl = GameConsts.TransparentPart.Contains(cData.Id)
            ? CosXmlUploadManager.GetBusinessRootUrl() + "/template/metadata_alpha.png"
            :CosXmlUploadManager.GetBusinessRootUrl() + "/template/metadata.png";
        
        GameController.StartGame(EnterGameModel.UgcSkinEmpty, new SkinInfo()
        {
            name = name,
            templateId = cData.Id,
            subType = resData.SubType,
            clothesUrl = defUrl,
            isProp = cData.IsProp,
            skinType = IsPet ? 1 : 0,
            canvasType = VipDataManager.Inst.isVip?(int)CanvasType.Canvas_64:(int)CanvasType.Canvas_32
        }, true, cData.IsProp ? GameController.MapScene : null);
        
        gameObject.SetActive(false);
    }

    private void OnOpenUgcEditPage()
    {
        TimerManager.Inst.RunOnce("OpenUgcCloseEidtPageOnceKey", 0.1f, () =>
        {
            MessageHelper.RemoveListener(MessageName.DidOpenUgcCloseEidtPage, OnOpenUgcEditPage);
        });
       
        if (contestInfo == null)
        {
            return;
        }
        
        if (UIManager.Inst.TryFindPanel<UGCResourceEditPanel>(PanelId.UGCResourceEditPanel, out var panel))
        {
            panel.setContestInfo(contestInfo);
        }
    }
}
