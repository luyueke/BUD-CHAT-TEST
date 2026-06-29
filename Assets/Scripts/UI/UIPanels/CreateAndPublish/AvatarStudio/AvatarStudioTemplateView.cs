
using System;
using System.Collections.Generic;
using System.Linq;
using Game.COSXML;
using Es;
using Game.Base;
using Game.Config;
using GameData;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
public class AvatarStudioTemplateView : AvatarStudioBaseView
{
    private const string tempItemPath = "Assets/Loadable/UI/UIPanel/CreateAndPublish/AvatarStudio/AvatarUgcTemplateItem.prefab";
    private const string tempSpriteatlasPath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";
    #region 模板选择页面改版

    private Transform newContentParent;
    private GridLayoutGroup allContentParent;
    private Transform contentParent;
    private Transform ActiveContestView;
    private SelectableContestGroupView contestView;
    private GameObject leftInset;
    private GameObject rightInset;
    #endregion

    protected override void Init()
    {
        base.Init();
        ActiveContestView = GameObjectEx.FindChildByName(transform, "ActiveContestObj");
        leftInset = GameObjectEx.FindChildByName(transform, "LeftInset").gameObject;
        rightInset = GameObjectEx.FindChildByName(transform, "RightInset").gameObject;

        contestView = GameObjectEx.FindChildByName(ActiveContestView, "ActiveContests").GetComponent<SelectableContestGroupView>();
        newContentParent = GameObjectEx.FindChildByName(transform, "RecommendContent"); 
        allContentParent = GameObjectEx.FindChildByName(transform, "ContentParent").GetComponent<GridLayoutGroup>();
        contentParent = GameObjectEx.FindChildByName(transform, "ContentParent");
        
        bool isPet = currentStyle == CharacterStyle.Pet;
        var showContestTypes = new List<BUDContestType>()
        {
            BUDContestType.Skin,
            BUDContestType.OC,
            BUDContestType.Bundle
        };
        if (isPet)
        {
            showContestTypes = new List<BUDContestType>()
            {
                BUDContestType.PetSkin,
                BUDContestType.PetOC,
                BUDContestType.PetBundle
            };
        }
        
        bool hasActiveContest = ContestDataManager.Inst.HasActiveContest(showContestTypes);
        ActiveContestView.gameObject.SetActive(hasActiveContest);
        leftInset.SetActive(hasActiveContest);
        rightInset.SetActive(hasActiveContest);
        
        if (hasActiveContest)
        {
            contestView.SetDraftListUI();
            contestView.InitViewInfo(showContestTypes);
            allContentParent.spacing = new Vector2(10, 20);
            allContentParent.constraintCount = 6;
            contestView.OnSelect = OnSelectContest;
        }
        
        CreateNewTemplateContent();
        CreateTemplateContent();
    }


    private void CreateNewTemplateContent()
    {
        var configData = currentStyle == CharacterStyle.Avatar
            ? DataTables.GetClothesTemplateList()
            : DataTables.GetPetClothesTemplateList();
        var skinOrderData = currentStyle == CharacterStyle.Avatar
            ? DataTables.GetSkinOrderConfigList()
            : DataTables.GetPetSkinOrderConfigList();
        var headTemplateList = skinOrderData.Where(x => x.sort > 0).ToList();
        headTemplateList.Sort((x,y)=>x.sort.CompareTo(y.sort));
        
        var iconAltas = Loader.Load<SpriteAtlas>(tempSpriteatlasPath, this.gameObject);
        for (int i = 0; i < headTemplateList.Count; i++)
        {
            var cData = configData.Find(x => x.Id == headTemplateList[i].Id);
            if (cData==null)
            {
                continue;
            }
            var itemGo = Loader.Load<GameObject>(tempItemPath).Instantiate(newContentParent.transform);
            var itemComp = itemGo.GetComponent<AvatarUgcTemplateItem>();
            itemComp.transform.Find("Bg").GetComponent<Image>().color = DataUtil.DeSerializeColor("A982FF");
            itemComp.OnItemCreate(cData,OnSelectTempItem,iconAltas , currentStyle == CharacterStyle.Avatar);
        }
    }

    private void CreateTemplateContent()
    {
        var iconAtlas =
            Loader.Load<SpriteAtlas>(
                tempSpriteatlasPath,this.gameObject);
        var configData = currentStyle == CharacterStyle.Avatar
            ? DataTables.GetClothesTemplateList()
            : DataTables.GetPetClothesTemplateList();
        var skinOrderData = currentStyle == CharacterStyle.Avatar
            ? DataTables.GetSkinOrderConfigList()
            : DataTables.GetPetSkinOrderConfigList();
        List<ClothesTemplate> orderedData = new List<ClothesTemplate>();
        for (int i = 0; i < skinOrderData.Count; i++)
        {
            orderedData.Add(configData.Find(x=>x.Id == skinOrderData[i].Id));
        }
        for (int i = 0; i < orderedData.Count; i++)
        {
            var data = orderedData[i];
            var itemGo= Loader.Load<GameObject>(tempItemPath).Instantiate(contentParent);
            var itemComp = itemGo.GetComponent<AvatarUgcTemplateItem>();
            itemComp.OnItemCreate(data, OnSelectTempItem,iconAtlas ,currentStyle == CharacterStyle.Avatar);
        }
    }
    private void OnSelectTempItem(ClothesTemplate cData)
    {
        if (cData == null)
        {
            LoggerUtils.LogError("cData is null");
            return;
        }
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
        template = currentStyle == CharacterStyle.Avatar
            ? DataTables.GetClothesTemplate(cData.Id)
            : DataTables.GetPetClothesTemplate(cData.Id);
        
        if (template == null)
        {
            LoggerUtils.LogError("template is null  cData.Id =" + cData.Id);
            return;
        }
        
        var sprite = iconAtlas.GetSprite(template.Cover);
        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        
        if (p == null)
        {
            LoggerUtils.LogError("template is null  PanelId.UgcLoadingPanel");
            return;
        }
        
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
            skinType = currentStyle == CharacterStyle.Avatar?0:1,
            canvasType = VipDataManager.Inst.isVip?(int)CanvasType.Canvas_64:(int)CanvasType.Canvas_32
        }, true, cData.IsProp ? GameController.MapScene : null);
    }
    private void OnClosePanel()
    {
        Hide();
    }

    private void OnSelectContest(ContestInfo contestInfo)
    {
        if (contestInfo == null)
        {
            return;
        }
        
        ContestEventManager.Inst.OpenContestPage(contestInfo.contestId);
    }

}
