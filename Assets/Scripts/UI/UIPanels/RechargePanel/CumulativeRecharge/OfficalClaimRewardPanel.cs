using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Store;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class OfficalClaimRewardPanel : BasePanel<OfficalClaimRewardPanel>
{
    [SerializeField] private Transform BG;
    [SerializeField] private Button BackBtn;
    [SerializeField] private Transform Content;
    [SerializeField] private Text RewardName;

    [Header("人物形象")] 
    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;
    [SerializeField] private OfficalClaimRewardItem _item;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] internal GashaponPreviewAnimView gashaponPreviewAnimView;

    
    [SerializeField] internal GameObject AvatarRootObj;
    [SerializeField] internal Image CurrencyObj;
    [SerializeField] internal Camera AvatarCamera;
    
    private List<ActivityRewardItemView> itemViews = new List<ActivityRewardItemView>();

    internal CharacterWrap characterWrap;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerAnimationCtrl otherAnimationCtrl;

    internal CharacterData saveCharacterData;

    private AmbientLightSetting _srcLightSetting;
    private bool _srcHallLightVisible;
    private bool _srcGameSceneLightVisible;
    private bool _srcPreviewSceneLightVisible;

    public override void OnCreate()
    {
        BackBtn.onClick.AddListener(OnBack);
    }

    private void OnBack()
    {
        CloseSelf();
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
        
        gashaponPreviewAnimView.SetWrap(characterWrap);
        gashaponPreviewAnimView.SetColor(DataUtil.DeSerializeColorCheckHash("#330074"), DataUtil.DeSerializeColorCheckHash("#FFC01F"),DataUtil.DeSerializeColorCheckHash("#9859FF"));
    }

    private void InitUI()
    {
        if (BG == null)
        {
            return;
        }
        
        string atlasPath = RechargePanel.RechargePanelAtlas;
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#9859FF", atlasPath, new List<string>()
        {
            "s4_limit_bg_1", "s4_limit_bg_2", "s4_limit_bg_3"
        });
        item.gameObject.SetActive(true);
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
            
            var otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(AccountDataManager.Inst.UserInfo.otherAvatarInfo);
            otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
        }
    }

    public void SetPreviewData(RechargeLevelData levelData)
    {
        gashaponPreviewAnimView.SetWrap(characterWrap);
        
        var budRewardType = (BUDRewardType)levelData.rewardType;
        var pgcId = levelData.pgcId.ToString();
        string rewardName = levelData.itemName;


        var rewardNum = budRewardType == BUDRewardType.RewardPinkCoin ? 200 : 1;
        _item.SetData(budRewardType, pgcId, rewardNum);
        
        var isOwned = false;
        AvatarRootObj.SetActive(budRewardType == BUDRewardType.RewardPgcResource);
        CurrencyObj.gameObject.SetActive(budRewardType == BUDRewardType.RewardPinkCoin || budRewardType == BUDRewardType.RewardCoin || budRewardType == BUDRewardType.RewardBadge || budRewardType == BUDRewardType.RewardMagicCoin);


        //人物预览
        if (budRewardType == BUDRewardType.RewardPgcResource)
        {
            isOwned = AssetsDataManager.IsOwned(pgcId);
            TryOn(pgcId);
            GameResData resData = Es.DataTables.GetGameResData(pgcId);
        }
        else
        {
            CurrencyObj.sprite = PgcUtils.LoadRewardPreviewIcon(budRewardType, gameObject);
            CancelTryOn();
        }
        
        RewardName.text = rewardName;
        if (rewardNum > 1) {
            RewardName.SetText(rewardNum + " " + rewardName);
        }
        
        var specialSkinConfig = DataTables.GetSpecialSkinConfig(pgcId);
        if (specialSkinConfig != null && gashaponPreviewAnimView != null) {
            gashaponPreviewAnimView.gameObject.SetActive(true);
            AvatarCamera.orthographicSize = 1.5f;
        }
    }
    
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
        CancelTryOn();

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
    
    private AssetsData GetAssetDataByPgcId(string pgcId)
    {
        var productList = AssetsDataManager.StoreData.ProductList;
        for (int i = 0, C = productList.Count; i < C; i++)
        {
            var productData = productList[i];
            var assetData = productData.AssetDataList[0];
            if (assetData.PgcId.ToString() == pgcId)
            {
                var config = Es.DataTables.GetGameResData(pgcId);
                switch (productData.ProductType)
                {
                    case Product.ProductType.Avatar:
                        var assetsData = new PGCAssetsData();
                        assetsData.Id = assetData.PgcId;
                        assetsData.Name = assetData.Name;
                        assetsData.ResourceType = ResourceType.Avatar;
                        assetsData.AvatarSubType = (AvatarSubType)config.SubType;
                        return assetsData;
                        break;
                    case Product.ProductType.Emote:
                        EmoteAssetsData emoteAssetsData = new EmoteAssetsData();
                        emoteAssetsData.Id = assetData.PgcId;
                        emoteAssetsData.Name = assetData.Name;
                        emoteAssetsData.ResourceType = ResourceType.Emote;
                        emoteAssetsData.EmoteSubType = (EmoteSubType)config.SubType;
                        return emoteAssetsData;
                        break;
                }
            }
        }

        return null;
    }
}
