using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Store;
using GameData.PgcData;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class PaidPackRewardData
{
    public List<string> pgcIds;
    public bool isBundle;
    public string name;
    public string bundleId;
}

public class PaidPackRewardPanel : BasePanel<PaidPackRewardPanel>
{
    [SerializeField] private RawImage Tex_Bg;
    [SerializeField] private Transform BG;
    [SerializeField] private Button BackBtn;
    [SerializeField] private Transform Content;
    [SerializeField] private Text RewardName;

    [Header("人物形象")] [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;

    [SerializeField] private PaidPackRewardItem _item;
    [SerializeField] private Text Txt_SubTitle;
    
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Text titleTxt;


    [SerializeField] internal GameObject AvatarRootObj;

    [HideInInspector]public List<PaidPackRewardItem> itemViews = new List<PaidPackRewardItem>();

    internal CharacterWrap characterWrap;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerAnimationCtrl otherAnimationCtrl;

    internal CharacterData saveCharacterData;
    [SerializeField] private RewardBundleList RewardBundleList;
     [SerializeField] internal GameObject Title;
     [SerializeField] internal GameObject Title_s15;
    [SerializeField] internal GameObject bg_s14;

    [SerializeField] internal GameObject bg_s15;

    private AmbientLightSetting _srcLightSetting;
    private bool _srcHallLightVisible;
    private bool _srcGameSceneLightVisible;
    private bool _srcPreviewSceneLightVisible;


    private static int allConsume;

    private Action<int> balanceChange;

    private bool isOnlyPreview = false;
    // private string currentPgcId;
    private PaidPackRewardData currentPaidPackRewardData;

    public override void OnCreate()
    {
        BackBtn.onClick.AddListener(OnBack);

        switch (SeasonPassDataManager.Inst.CurrentSeasonPassType)
        {
            case SeasonPassType.S14SeasonPass:
                Title.SetActive(true);
                Title_s15.SetActive(false);
                bg_s14.SetActive(true);
                bg_s15.SetActive(false);
                break;
            case SeasonPassType.S15SeasonPass:
                Title.SetActive(false);
                Title_s15.SetActive(true);
                bg_s14.SetActive(false);
                bg_s15.SetActive(true);
                break;
            default:
                Title.SetActive(false);
                Title_s15.SetActive(true);
                bg_s14.SetActive(false);
                bg_s15.SetActive(true);
                break;
        }
    }

    private void OnBack()
    {
        CloseSelf();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    public override void OnShow(params object[] args)
    {

        InitUI();
        _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
        _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
        _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
        _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();

        InitRewardView();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    private void InitUI()
    {
        if (BG == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();

        item.gameObject.SetActive(true);
        var spriteatlasPath = "Assets/Loadable/UI/UIPanel/CommonSprite/CommonSprite.spriteatlas";
        string iconName = "icn_common_piano_big";
        string title = "奖品预览";
   
        titleTxt.text = title;
    }

    private void InitRewardView()
    {
        saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData == null)
            saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
        if (saveCharacterData != null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrap.SetParent(characterRoot, true);
            animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;

            var otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
        }
    }

    public string itemBgColorStr = "#FF6363";
    public string bundleBgColorStr = "#FFFFFF";
    public string bundleContentBgColorStr = "#FF9B9B";

    public void SetPreviewData(List<PaidPackRewardData> paidPackRewardDatas)
    {
        isOnlyPreview = true;

        Color itemBgColor = DataUtil.DeSerializeColorCheckHash(itemBgColorStr);

        if (paidPackRewardDatas != null)
        {
            foreach (var paidPackRewardData in paidPackRewardDatas)
            {
                var item = GameObject.Instantiate(_item, Content);
                item.SetPreviewData(paidPackRewardData, itemBgColor, OnClickItem);
                itemViews.Add(item);
            }

            scrollRect.verticalNormalizedPosition = 1f;

            if (paidPackRewardDatas.Count > 0)
            {
                OnClickItem(paidPackRewardDatas[0]);
            }
        }

    }

    public void SetStyle(string bgColor, string atlasPath, List<string> bgSpriteIds, string itemColor)
    {
        Tex_Bg.enabled = false;
        var itemObj = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab").Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem(bgColor, atlasPath, bgSpriteIds);
        item.gameObject.SetActive(true);

        itemBgColorStr = itemColor;
        RewardBundleList.transform.GetChild(0).GetComponent<Image>().color = DataUtil.DeSerializeColorByHex(itemColor);
    }

    public void SetStyle(Texture2D bg, string itemColor)
    {
        Tex_Bg.enabled = false;
        var itemObj = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab").Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomTextureBg(bg);
        item.gameObject.SetActive(true);

        itemBgColorStr = itemColor;
        RewardBundleList.transform.GetChild(0).GetComponent<Image>().color = DataUtil.DeSerializeColorByHex(itemColor);
    }

    public void SetEventPreview(List<PaidPackRewardData> paidPackRewardDatas,string subTitle, Texture bg)
    {
 
        SetPreviewData(paidPackRewardDatas);
        Txt_SubTitle.gameObject.SetActive(true);
        Tex_Bg.gameObject.SetActive(true);
        Txt_SubTitle.text = subTitle;
        Tex_Bg.texture = bg;
        var sizeToFit = Tex_Bg.GetComponentInParent<UIBGSizeToFit>();
        sizeToFit.Resize();
    }


    private ActivityRewardInfo activeData;

    public void OnClickItem(PaidPackRewardData paidPackRewardData)
    {
        this.currentPaidPackRewardData = paidPackRewardData;
        foreach (var element in itemViews)
        {
            element.SetSelect(element.PaidPackRewardData == paidPackRewardData);
        }

        AvatarRootObj.SetActive(true);

       
        CancelTryOn();
        List<string> pgcIds = paidPackRewardData.pgcIds;
        foreach (var pgcId in pgcIds)
        {
            if (!string.IsNullOrEmpty(pgcId))
            {
                TryOn(pgcId);
            }
        }
        
        if (paidPackRewardData.isBundle)
        {
            RewardName.gameObject.SetActive(false);
            RewardBundleList.gameObject.SetActive(true);
            RewardBundleList.SetTarget(pgcIds, paidPackRewardData.name, bundleBgColorStr, bundleContentBgColorStr);
        }
        else
        {
            RewardName.gameObject.SetActive(true);
            RewardName.text = paidPackRewardData.name;
            RewardBundleList.gameObject.SetActive(false);
        }

        if(pgcIds.Contains("40300505") || pgcIds.Contains("10900499"))
        {
            characterRoot.transform.localScale = Vector3.one * 0.8f; //放烟花，需要缩小，才能拍到天空。
        }else
        {
            characterRoot.transform.localScale = Vector3.one * 1.1f; //恢复prefab中设置的值
        }
    }

    private bool isSending = false;
    

    internal void CancelTryOn()
    {
        characterWrap.RefreshAvatar(saveCharacterData);
    }

    /// <summary>
    /// 试穿 重置参数
    /// </summary>
    /// <param name="goodsData"></param>
    internal void TryOn(string pgcId)
    {

        avatarCameraController.ResetEmoteView();
        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();

        GameResData config = Es.DataTables.GetGameResData(pgcId);
        if (config == null)
        {
            return;
        }

        switch ((ResourceType)config.ResourceType)
        {
            case ResourceType.Avatar:
                OnWearAvatar(pgcId);
                break;
            case ResourceType.Emote:
                PreviewEmote(pgcId, (EmoteSubType)config.SubType);
                break;
        }
    }

    internal void OnWearAvatar(string pgcId)
    {
        avatarCameraController.SetCameraZoom( ViewType.ZoomWholeBody);
        var config = DataTables.GetAvatarCommonData(pgcId);
        var classType = UniqueType.GetAvatar(pgcId);
        characterWrap.ChangePart(classType, pgcId);
        characterWrap.ChangeColor(classType, config.defaultColor);
        characterWrap.Move(classType, config.pDef);
        characterWrap.Rotate(classType, config.rDef);
        characterWrap.Scale(classType, config.sDef);
        characterWrap.HVScale(classType, config.vhSDef);
        characterWrap.SetLeftOrRight(classType, config.leftRightType);
    }

    internal void PreviewEmote(string pgcId, EmoteSubType emoteSubType)
    {
        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();
        avatarCameraController.SetEmoteView(pgcId);
        switch (emoteSubType)
        {
            case EmoteSubType.Single:
            case EmoteSubType.SingleLoop:
                animationCtrl.PlaySingleEmoteForUICharacter(pgcId, null);
                break;
            case EmoteSubType.Double:
            case EmoteSubType.DoubleLoop:
                animationCtrl.PlayDoubleEmoteForUICharacter(pgcId, otherAnimationCtrl, null);
                break;
        }
    }
}
