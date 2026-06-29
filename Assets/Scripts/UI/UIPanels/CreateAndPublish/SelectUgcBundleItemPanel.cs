using Game.Avatar;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.PgcData;
using System;
using System.Collections;
using System.Collections.Generic;
using Game.Pet;
using UGCAsset;
using UGCAsset.Draft;
using UI;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class SelectUgcBundleItemPanel : BasePanel<SelectUgcBundleItemPanel>
{
    [SerializeField] internal Transform transBg;
    [Header("人物形象")]
    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;
    [SerializeField] internal Button backButton;
    [SerializeField] internal Button nextButton;

    [Header("类别选择UI")]
    [SerializeField] internal ClassList classList;
    [Header("列表")]
    [SerializeField] public AvatarStudioEntry gameEntry;
    [Header("选定列表")]
    [SerializeField] internal SelectedList selectedList;
    [Header("提示语")]
    [SerializeField] internal Text tipText;

    internal BaseAvatarWrapper characterWrap;
    internal BaseAvatarData saveCharacterData;

    private CurrencyType _currencyType = CurrencyType.PinkCoin;
    private CharacterStyle _characterType;
    List<DraftListItem> selectedDatas = new();

    public override void OnCreate()
    {
        // InitUI();
        //
        // backButton.onClick.AddListener(OnBackClick);
        // saveCharacterData = CharacterData.DeserializeObject("{\"partDatas\":[{\"Id\":\"12200000\",\"Cr\":\"#FFEADA\"},{\"Id\":\"12300001\"}]}");
        // characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
        // characterWrap.SetParent(characterRoot, true);
        // avatarCameraController.RotateTarget = characterRoot;
        //
        // nextButton.onClick.AddListener(OnNextClick);
        // nextButton.interactable = selectedDatas.Count > 1;
        //
        // InitClass();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        CurrencyType currencyType = args[0] is CurrencyType ? (CurrencyType)args[0] : CurrencyType.PinkCoin;
        _characterType= (CharacterStyle)args[1]  ;
        _currencyType = currencyType;
        
        InitUI();

        backButton.onClick.AddListener(OnBackClick);
        if (_characterType == CharacterStyle.Avatar)
        {
            saveCharacterData = CharacterData.DeserializeObject("{\"partDatas\":[{\"Id\":\"12200000\",\"Cr\":\"#FFEADA\"},{\"Id\":\"12300001\"}]}");
            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData as CharacterData);
        }
        else
        {
 
            saveCharacterData = PetData.DeserializeObject("{\"partDatas\":[{\"Id\":\"72200001\",\"Cr\":\"#FFFFFF\"},]}");
            characterWrap = PetAvatarController.Inst.CreateUIAvatar(saveCharacterData as PetData);
        }
        characterWrap.SetParent(characterRoot, true);
        avatarCameraController.RotateTarget = characterRoot;
        nextButton.onClick.AddListener(OnNextClick);
        nextButton.interactable = selectedDatas.Count > 1;

        InitClass();
    }

    public void InitClass()
    {
        var list = new List<ClassData>();
        if (_characterType == CharacterStyle.Avatar)
        {
            foreach (UgcAvatarEnable subType in Enum.GetValues(typeof(UgcAvatarEnable)))
            {
                if (subType == UgcAvatarEnable.ErrUgcAvatarSubType) continue;
                if (subType == UgcAvatarEnable.Bundle) continue;
                list.Add(new ClassData(UniqueType.GetUgcAvatar((AvatarSubType)subType),  $"{subType}"));
            }
        }
        else
        {
            foreach (PetUGCAvatarEnable subType in Enum.GetValues(typeof(PetUGCAvatarEnable)))
            {
                if (subType == PetUGCAvatarEnable.ErrAvatarSubType) continue;
                if (subType == PetUGCAvatarEnable.Bundle) continue;
                list.Add(new ClassData(UniqueType.GetUGCPetAvatar((AvatarSubType)subType),   $"Pet{subType}"));
            }
        }
        if (list.Count == 0) return;
        classList.SetCallback(OnClassSelected);
        classList.SetClass(list[0], list);
    }

    private void GetPageDatas(List<DraftListItem> infos)
    {
        infos.ForEach(i =>
        {
            var data = selectedDatas.Find(s => s.skinInfo.id == i.skinInfo.id);
            i.selected = data != null;
        });
        tipText.gameObject.SetActive(infos == null || infos.Count == 0);
    }

    private void OnStudioItemClick(DraftListItem item)
    {
        if (item.skinInfo == null || string.IsNullOrEmpty(item.skinInfo.id)) return;
        var data = selectedDatas.Find(s => s.skinInfo.id == item.skinInfo.id);
        if (data != null)
        {
            item.selected = false;
            selectedDatas.Remove(data);
            gameEntry.UpdateSingleItem(item);
            characterWrap.TakeOff(item.skinInfo.skinType == (int)SkinType.Avatar?
                UniqueType.GetUgcAvatar((AvatarSubType)item.skinInfo.subType):UniqueType.GetUGCPetAvatar((AvatarSubType)item.skinInfo.subType));
        }
        else
        {
            var oldData = selectedDatas.Find(s => s.skinInfo.subType == item.skinInfo.subType);
            if (oldData != null)
            {
                selectedDatas.Remove(oldData);
                oldData.selected = false;
                gameEntry.UpdateSingleItem(oldData);
            }
            else
            {
                if (selectedDatas.Count >= 10)
                {
                    TipPanel.ShowToast("套装最多可以包含10个单品哦");
                    return;
                }
            }
            selectedDatas.Add(item);
            item.selected = true;
            gameEntry.UpdateSingleItem(item);
            characterWrap.ChangeUGCPart(item.skinInfo);
        }
        selectedList.gameObject.SetActive(selectedDatas.Count > 0);
        selectedList.SetList(selectedDatas);

        nextButton.interactable = selectedDatas.Count > 1;
    }

    private void IsEmptyAction()
    {

    }

    /// <summary>
    /// 前往模版创建view
    /// </summary>
    /// <param name="item"></param>
    /// <param name="gameStudioItem"></param>
    private void GoToTemplateView()
    {

    }

    private void OnClassSelected(ClassData classData)
    {
        gameEntry.SetActions(OnStudioItemClick, GoToTemplateView, IsEmptyAction, StudioSubType.Published, UniqueType.AvatarSubType(classData.Id), _currencyType);
        gameEntry.Init(_characterType );
        gameEntry.GetFirstPageDatas(GetPageDatas);
    }

    private void InitUI()
    {
        if (transBg == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(transBg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
            {
                "AvatarBg_icon1","AvatarBg_icon5" ,"AvatarBg_icon3","AvatarBg_icon4","AvatarBg_icon2"
            });
        item.gameObject.SetActive(true);
    }

    public void OnBackClick()
    {
        CloseSelf();
    }
    private void OnNextClick()
    {
        if (selectedDatas.Count < 2) return;

        var publishMachine = new UGCPublishStateMachine();
        var tmpPropInfo = new SkinInfo();
        tmpPropInfo.id = null;
        tmpPropInfo.templateId = null;
        tmpPropInfo.bundleIdList = new();
        tmpPropInfo.subType = (int)AvatarSubType.Bundle;
        tmpPropInfo.skinType = selectedDatas[0].skinInfo.skinType;
        var price = 0;
        var currencyType = _currencyType;
        selectedDatas.ForEach(s =>
        {
            price += s.skinInfo.paymentInfo.price;
            currencyType = s.skinInfo.paymentInfo.currencyType;
            if (s.skinInfo.ugcStyle > 0)
            {
                tmpPropInfo.ugcStyle = s.skinInfo.ugcStyle;
            }
            tmpPropInfo.bundleIdList.Add(s.skinInfo.id);
        });
        var states = new List<UGCPublishStateBase>()
        {
            new UGCBundleDetailState(),
        };

        publishMachine.SetStates(states);

        var draftInfo = new UgcBundleDraftInfo(tmpPropInfo);

        publishMachine.SetEditData(new UgcBundleEditData()
        {
            draftInfo = draftInfo,
            currencyType = currencyType,
            OriginPrice = price,
            CurrencyType = currencyType
        });
        publishMachine.SetCancelCallBack(() =>
        {

        });
        publishMachine.SetFinishCallBack(() =>
        {
            CloseSelf();
        });
        publishMachine.Start();
    }
}
