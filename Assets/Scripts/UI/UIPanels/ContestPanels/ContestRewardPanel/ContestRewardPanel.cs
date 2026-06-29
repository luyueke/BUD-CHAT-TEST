using Basic.Utils;
using Es;
using Game.Avatar;
using GameData;
using GameData.PgcData;
using System.Collections;
using System.Collections.Generic;
using Game.Pet;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ContestRewardPanel : BasePanel<ContestRewardPanel>
{
    public Transform _transBG;
    public CButton _btnBack;
    public ContestRewardItem itemPrefab;
    public Transform itemParent;
    public Image descImage;
    public Text desc;
    private ContestInfo _data;

    public Transform characterRoot;
    public AvatarCameraController avatarCameraController;

    internal CharacterData saveCharacterData;
    internal CharacterWrap characterWrap;
    internal PetData savePetData;
    internal PetWrap petWrap;
    internal PlayerAnimationCtrl petAnimationCtrl;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerAnimationCtrl otherAnimationCtrl;
    private bool _srcPreviewSceneLightVisible;
    private ContestRewardItem firstItem;

    public override void OnCreate()
    {
        base.OnCreate();
        _transBG = GameObjectEx.FindChildByName(this.transform, "Trans_BG");
        _btnBack = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
        _btnBack.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _data = (ContestInfo)args[0];
        InitUI();
        InitRewardIcon();
        StartPreview();
        _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
        Invoke("Delay", 0.1f);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();
        AmbientLightManager.Inst.RevertPreviewLight(_srcPreviewSceneLightVisible);
    }

    public void StartPreview()
    {
        this.gameObject.SetActive(true);

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
        
        savePetData = AccountDataManager.Inst.PetInfo.avatarInfo;
        if (savePetData != null)
        {
            petWrap = PetAvatarController.Inst.CreateUIAvatar(savePetData);
            petWrap.SetParent(characterRoot, true);
            petAnimationCtrl = petWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;
            petWrap.Avatar.SetActive(false);
        }
    }

    private void Delay()
    {
        if (firstItem == null) return;

        ContestRewardItem.Select(firstItem);
        OnRewardClick(firstItem.Data);
    }

    private void WearPgcClothes(string pgcId)
    {
        CancelTryOn();
        CancelEmote();

        var config = DataTables.GetGameResData(pgcId);
        if (config == null) return;
        switch ((ResourceType)config.ResourceType)
        {
            case ResourceType.Avatar:
                characterWrap?.Avatar.SetActive(true);
                petWrap?.Avatar.SetActive(false);
                TryOn(pgcId);
                break;
            case ResourceType.PGCPetAvatar:
                characterWrap?.Avatar.SetActive(false);
                petWrap?.Avatar.SetActive(true);
                PetTryOn(pgcId);
                break;
            case ResourceType.Emote:
                characterWrap?.Avatar.SetActive(true);
                petWrap?.Avatar.SetActive(false);
                PreviewEmote(pgcId, (EmoteSubType)config.SubType);
                break;
        }
    }

    internal void TryOn(string pgcId)
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
    
    internal void PetTryOn(string pgcId)
    {
        if (petWrap == null)
        {
            return;
        }
        var config = DataTables.GetPetAvatarCommonData(pgcId);
        var classType = UniqueType.GetPGCPetAvatar((AvatarSubType)config.SubType);
        petWrap.ChangePart(classType, pgcId);
        petWrap.ChangeColor(classType, config.defaultColor);
        petWrap.Move(classType, config.pDef);
        petWrap.Rotate(classType, config.rDef);
        petWrap.Scale(classType, config.sDef);
        petWrap.HVScale(classType, config.vhSDef);
        petWrap.SetLeftOrRight(classType, config.leftRightType);
    }

    internal void CancelTryOn()
    {
        characterWrap.RefreshAvatar(saveCharacterData);
        petWrap?.RefreshAvatar(savePetData);
    }

    internal void PreviewEmote(string pgcId, EmoteSubType emoteSubType)
    {
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

    internal void CancelEmote()
    {
        avatarCameraController.ResetEmoteView();
        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();
        if (petAnimationCtrl != null)
        {
            petAnimationCtrl.ResetEmoteForUICharacter();
        }
    }

    private void InitRewardIcon()
    {
        Color color = Color.black;
        if (_data.themeColorList != null && _data.themeColorList.Count >= 1) ColorUtility.TryParseHtmlString(_data.themeColorList[0], out color);
        if (_data.prizes == null || _data.prizes.Count == 0)
        {
            TipPanel.ShowToast("未配置奖励");
            return;
        }
        for (int i = 0; i < _data.prizes.Count; i++)
        {
            var item = GameObject.Instantiate(itemPrefab, itemParent);
            item.UpdateViews(color, _data.prizes[i], OnRewardClick);

            if (i == 0) firstItem = item;
        }
    }

    private void OnRewardClick(ContestPrizeInfo data)
    {
        desc.SetText(data.desc);
        if (data.rewardType == (int)BUDRewardType.RewardPgcResource)
        {
            WearPgcClothes(data.pgcId);
        }
        else
        {
            if (GameUtils.IsCurrencyType(data.rewardType))
            {

            }
        }
    }

    private void InitUI()
    {
        if (_transBG == null)
        {
            return;
        }

        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/RemoteIconBg.prefab")
            .Instantiate(_transBG);
        var item = itemObj.GetComponent<RemoteIconBgPanel>();
        item.InitCustomBgItem(_data.backgroundColor, _data.backgroundIconUrlList);
        item.gameObject.SetActive(true);

        Color color = Color.black;
        if (_data.themeColorList != null && _data.themeColorList.Count >= 2) ColorUtility.TryParseHtmlString(_data.themeColorList[1], out color);
        descImage.color = color;
    }
}
