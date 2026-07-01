using System;
using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using Game.CommunityGame;
using Game.Database;
using Game.Store;
using GameData;
using GameData.UGCData;
using Newtonsoft.Json;
using xasset;
using UI.Base;
using UI.BaseWidgets;
using Com.TheFallenGames.OSA.Util.IO;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

public enum AIPartnerTabSecond
{
    AICharacter = 0,//AI伙伴
    PartnerBox = 1,//伙伴盒子
}

public class AIPartnerShopPanel : BasePanel<AIPartnerShopPanel>
{
    [Header("模型商品")]
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private Transform CharacterRoot;
    [SerializeField] private AvatarCameraController AvatarCameraController;
    [SerializeField] private CButton BuyBtn;
    [SerializeField] private GameObject BuyBtnGray;
    [SerializeField] private Text Price;
    [SerializeField] private Text PartnerName;
    [SerializeField] private CButton ShrinkBtn;
    [SerializeField] private Transform ShopTagTsf;
    [SerializeField] private ShopTagItem ShopTagItem;
    [SerializeField] private CButton SearchCharacterBtn;
    [SerializeField] private CButton InputBtn;
    [SerializeField] private CButton clearInputButton;
    [SerializeField] private Text SearchInputText;
    [SerializeField] private GameObject NoneTips;
    [SerializeField] private Text NoneTipsText;
    [SerializeField] private List<Toggle> TagToggles;
    [Header("AI伙伴")]
    [SerializeField] private GameObject AICharacterRoot;
    [SerializeField] private PartnerSkinAdpter SkinAdpter;
    [SerializeField] private CButton DetailBtn;
    [SerializeField] private AIPartnerAdpter Adpter;
    [SerializeField] private CButton ToCreateBtn;
    [SerializeField] private GameObject AvatarCamera;
    [Header("Box盒子")]
    [SerializeField] private GameObject PartnerBoxRoot;
    [SerializeField] private PartnerBoxAdpter BoxAdpter;
    [SerializeField] private Transform BoxModelRoot;
    [SerializeField] private CButton BoxCreateBtn;
    [SerializeField] private Transform HeadRoot;
    [SerializeField] private CButton HeadBtn;
    [SerializeField] private RemoteImageBehaviour HeadImg;
    [SerializeField] private GameObject BoxCamera;
    // 盒子 3D 预览组件，统一负责盒子模型生成、纹理加载与资源释放（放置在 BoxModelRoot 节点下）
    [SerializeField] private BudBoxModel BudBoxModel;

    private List<ShopTagItem> _tagItems = new List<ShopTagItem>();
    private List<CabinCharacterUgcInfo> _curSectionItems = new List<CabinCharacterUgcInfo>();
    private List<UgcSectionData> _sectionDatas;
    private string _curSectionId;
    private CharacterWrap _characterWrap;
    private string _curSelectedId;
    private CabinCharacterUgcInfo _curSelectData;
    private SkinPackInfo _currentSkinPack;
    private PlayerAnimationCtrl _animationCtrl;
    private CabinPgcUgcPlayController _cabinPlayCtrl = new();
    private CabinStandbyAnimController _standbyAnimCtrl = new();
    private string _searchKeyword = string.Empty;
    private List<CharacterBoxInfo> _curBoxSectionItems = new List<CharacterBoxInfo>();
    private CharacterBoxInfo _curSelectBoxData;
    private AIPartnerTabSecond _currentTab = (AIPartnerTabSecond)(-1);
    private readonly List<Text> _tabToggleTexts = new List<Text>();
    private static readonly Color TabSelectedColor = DataUtil.DeSerializeColor("CDFF71");
    private static readonly Color TabNormalColor = Color.white;

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        DetailBtn.onClick.AddListener(DetailBtnOnClick);
        BuyBtn.onClick.AddListener(BuyBtnOnClick);
        ShrinkBtn.onClick.AddListener(ShrinkBtnOnClick);
        InputBtn.onClick.AddListener(OnInputBtnClick);
        SearchCharacterBtn.onClick.AddListener(OnSearchCharacterBtnClick);
        clearInputButton.onClick.AddListener(OnClearInputBtnClick);
        clearInputButton.gameObject.SetActive(false);
        ToCreateBtn.onClick.AddListener(ToCreateBtnOnClick);
        BoxCreateBtn.onClick.AddListener(BoxCreateBtnOnClick);
        HeadBtn.onClick.AddListener(OnHeadBtnClick);
        Adpter.OnItemSelected = OnPartnerItemSelected;
        SkinAdpter.SetOnItemClick(OnSkinItemSelected);
        BoxAdpter.OnItemSelected = OnBoxItemSelected;
        for (int i = 0; i < TagToggles.Count; i++)
        {
            _tabToggleTexts.Add(TagToggles[i].GetComponentInChildren<Text>());
            var tab = (AIPartnerTabSecond)i;
            TagToggles[i].onValueChanged.AddListener(isOn => { if (isOn) SwitchToTab(tab); });
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _currentTab = (AIPartnerTabSecond)(-1);

        // 支持外部传入初始页签（例如从 CabinControllScenePanel 「去购买」跳转时指定 PartnerBox），默认打开 AICharacter
        var initialTab = AIPartnerTabSecond.AICharacter;

        if (args != null && args.Length > 0 && args[0] is AIPartnerTabSecond requestedTab)
        {
            initialTab = requestedTab;
        }

        int tabIndex = (int)initialTab;

        if (TagToggles.Count > tabIndex)
        {
            TagToggles[tabIndex].SetIsOnWithoutNotify(true);
            SwitchToTab(initialTab);
        }
    }

    private void SwitchToTab(AIPartnerTabSecond tab)
    {
        if (_currentTab == tab) return;
        _currentTab = tab;
        RefreshTabToggleColors();

        AICharacterRoot.SetActive(tab == AIPartnerTabSecond.AICharacter);
        PartnerBoxRoot.SetActive(tab == AIPartnerTabSecond.PartnerBox);
        // AI伙伴页用 AvatarCamera，盒子页用 BoxCamera
        if (AvatarCamera != null) AvatarCamera.SetActive(tab == AIPartnerTabSecond.AICharacter);
        if (BoxCamera != null) BoxCamera.SetActive(tab == AIPartnerTabSecond.PartnerBox);
        DetailBtn.gameObject.SetActive(tab == AIPartnerTabSecond.AICharacter);
        CharacterRoot.gameObject.SetActive(tab == AIPartnerTabSecond.AICharacter);
        BoxModelRoot.gameObject.SetActive(tab == AIPartnerTabSecond.PartnerBox);
        if (HeadRoot != null) HeadRoot.gameObject.SetActive(tab == AIPartnerTabSecond.PartnerBox);
        if (tab == AIPartnerTabSecond.PartnerBox) _standbyAnimCtrl.Stop();
        AvatarCameraController.RotateTarget = tab == AIPartnerTabSecond.PartnerBox
            ? BoxModelRoot
            : CharacterRoot;

        switch (tab)
        {
            case AIPartnerTabSecond.AICharacter:
                AIPartnerShopRequestCtrl.Inst.RequestGetPartnerSection(UgcType.AIPartner, RefreshBySeverInfo, _ =>
                {
                    Debug.LogError("获取section分类失败!");
                });
                break;
            case AIPartnerTabSecond.PartnerBox:
                AIPartnerShopRequestCtrl.Inst.RequestGetPartnerSection(UgcType.PartnerBox, RefreshBySeverInfo, _ =>
                {
                    Debug.LogError("获取section分类失败!");
                });
                break;
        }
    }

    private void RefreshTabToggleColors()
    {
        for (int i = 0; i < _tabToggleTexts.Count; i++)
        {
            if (_tabToggleTexts[i] != null)
                _tabToggleTexts[i].color = TagToggles[i].isOn ? TabSelectedColor : TabNormalColor;
        }
    }


    private void RefreshBySeverInfo(List<UgcSectionData> sectionDatas)
    {
        if (sectionDatas == null || sectionDatas.Count == 0) return;
        _sectionDatas = sectionDatas;

        for (int i = _tagItems.Count - 1; i >= 0; i--)
            Destroy(_tagItems[i].gameObject);
        _tagItems.Clear();

        for (int i = 0; i < sectionDatas.Count; i++)
        {
            var tag = Instantiate(ShopTagItem, ShopTagTsf);
            tag.gameObject.SetActive(true);
            tag.InitItem(new SectionItemData
            {
                sectionId   = sectionDatas[i].sectionId,
                sectionName = sectionDatas[i].sectionName
            }, i, OnSectionTagClick);
            _tagItems.Add(tag);
        }

        _curSectionId = null;
        if (_tagItems.Count > 0)
        {
            _tagItems[0].Tog.SetIsOnWithoutNotify(true);
            OnSectionTagClick(sectionDatas[0].sectionId);
        }
    }

    private void OnPartnerItemSelected(CabinCharacterUgcInfo info)
    {
        if (info == null || info.id == _curSelectedId) return;
        _curSelectedId = info.id;
        _curSelectData = info;
        PartnerName.text = info.name;
        Price.text = info.paymentInfo.price.ToString();
        var inventoryData = BagDatabase.Inst.Select(info.id);
        var isOwned = (inventoryData != null && inventoryData.OwnedNum > 0)
                      || info.creator == AccountDataManager.Inst.Uid;
        if (BuyBtnGray != null) BuyBtnGray.gameObject.SetActive(isOwned);
        if (BuyBtn != null) BuyBtn.interactable = !isOwned;
        Price.gameObject.SetActive(!isOwned);
        var pack = info.skinPack?.Find(p => p.isDefault == 1) ?? info.skinPack?[0];
        if (pack == null || string.IsNullOrEmpty(pack.avatarJson)) return;

        _standbyAnimCtrl.Stop();
        var characterData = CharacterData.DeserializeObject(pack.avatarJson);
        ApplyBodyTypeOffset(characterData.bodyType);
        if (_characterWrap != null)
        {
            _characterWrap.RefreshAvatar(characterData);
        }
        else
        {
            _characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(characterData, CharacterRoot);
            _animationCtrl = _characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            AvatarCameraController.RotateTarget = CharacterRoot;
        }

        _cabinPlayCtrl.Init(_animationCtrl, _characterWrap, null, AvatarCameraController);
        _standbyAnimCtrl.Start(_cabinPlayCtrl, info.pendingEmote);

        if (info.skinPack != null)
        {
            _currentSkinPack = pack;
            SkinAdpter.SetSelected(pack);
            var allSkins = new List<SkinPackInfo>(info.skinPack);
            if (info.extensionPackList != null && info.extensionPackList.Count > 0)
            {
                CabinNetManager.Inst.GetExtensionPackBatchInfo(info.extensionPackList,
                    (isSuccess, dataList) =>
                    {
                        if (isSuccess && dataList != null)
                            foreach (var packInfo in dataList)
                                if (packInfo.ugcclass == (int)UGCClass.Published && packInfo.skinPack != null)
                                    allSkins.AddRange(packInfo.skinPack);
                        SkinAdpter.Data.ResetItems(allSkins, true);
                    });
            }
            else
            {
                SkinAdpter.Data.ResetItems(allSkins, true);
            }
        }

        Adpter.SelectById(info.id);
    }

    public void OnPartnerSelectedFromExternal(CabinCharacterUgcInfo info)
    {
        if (info == null) return;
        _curSelectedId = null;
        OnPartnerItemSelected(info);
        Adpter.SelectById(info.id);
    }

    private void OnSkinItemSelected(SkinPackInfo pack)
    {
        if (pack == null || string.IsNullOrEmpty(pack.avatarJson) || _characterWrap == null) return;
        _currentSkinPack = pack;
        var characterData = CharacterData.DeserializeObject(pack.avatarJson);
        ApplyBodyTypeOffset(characterData.bodyType);
        _characterWrap.RefreshAvatar(characterData);
    }

    private void OnSectionTagClick(string sectionId)
    {
        if (_curSectionId == sectionId) return;
        _curSectionId = sectionId;
        _curSelectedId = null;

        if (_currentTab == AIPartnerTabSecond.PartnerBox)
        {
            AIPartnerShopRequestCtrl.Inst.RequestGetBoxSectionInfo(sectionId, list =>
            {
                _curBoxSectionItems = list ?? new List<CharacterBoxInfo>();
                if (BoxAdpter != null) BoxAdpter.SetItems(list);
                if (list != null && list.Count > 0)
                    OnBoxItemSelected(list[0]);
                var itemsPanel = UIManager.Inst.FindPanel<PartnerShopItemsPanel>(PanelId.PartnerShopItemsPanel);
                if (itemsPanel != null) itemsPanel.SyncBoxSection(sectionId, list);
            });
            return;
        }

        AIPartnerShopRequestCtrl.Inst.RequestGetPartnerSectionInfo(sectionId, (list) =>
        {
            if (Adpter != null)
            {
                _curSectionItems = list ?? new List<CabinCharacterUgcInfo>();
                if (list != null && list.Count > 0)
                {
                    bool isFirst = _curSelectedId == null;
                    var selectedId = isFirst ? list[0].id : _curSelectedId;
                    Adpter.SetItems(list, selectedId);
                    if (isFirst) OnPartnerItemSelected(list[0]);
                }
                else
                {
                    Adpter.SetItems(list);
                }
                var itemsPanel = UIManager.Inst.FindPanel<PartnerShopItemsPanel>(PanelId.PartnerShopItemsPanel);
                if (itemsPanel != null) itemsPanel.SyncSection(sectionId, list);
            }
        });
    }

    private void OnSectionChangedFromItemsPanel(string sectionId, List<CabinCharacterUgcInfo> list)
    {
        if (_curSectionId == sectionId) return;
        _curSectionId = sectionId;
        _curSelectedId = null;
        foreach (var tag in _tagItems)
            tag.Tog.SetIsOnWithoutNotify(tag.SectionId == sectionId);
        _curSectionItems = list ?? new List<CabinCharacterUgcInfo>();
        if (list != null && list.Count > 0)
        {
            Adpter.SetItems(list, list[0].id);
            OnPartnerItemSelected(list[0]);
        }
        else
        {
            Adpter.SetItems(list);
        }
    }


    private void DetailBtnOnClick()
    {
        if (_curSelectData == null) return;
        CabinRolesNetManager.Inst.OpenPanelWithLocalData(
            _curSelectData,
            _currentSkinPack?.packId,
            () => CharacterRoot.gameObject.SetActive(true),
            isFromShop: true);
    }

    private void OnBuySuccess()
    {
        var sectionId = _curSectionId;
        var savedSelectedId = _curSelectedId;

        AIPartnerShopRequestCtrl.Inst.RequestGetPartnerSectionInfo(sectionId, (list) =>
        {
            if (Adpter == null) return;
            _curSectionItems = list ?? new List<CabinCharacterUgcInfo>();

            var selectedItem = list?.Find(item => item.id == savedSelectedId)
                               ?? (list != null && list.Count > 0 ? list[0] : null);

            if (selectedItem != null)
            {
                Adpter.SetItems(list, selectedItem.id);
                _curSelectedId = null;
                OnPartnerItemSelected(selectedItem);

                CharacterRoot.gameObject.SetActive(false);
                CabinRolesNetManager.Inst.OpenPanelWithFreshData(
                    selectedItem.id,
                    _currentSkinPack?.packId,
                    () => CharacterRoot.gameObject.SetActive(true),
                    isFromShop: true);
            }
            else
            {
                Adpter.SetItems(list);
            }

            var itemsPanel = UIManager.Inst.FindPanel<PartnerShopItemsPanel>(PanelId.PartnerShopItemsPanel);
            if (itemsPanel != null) itemsPanel.SyncSection(sectionId, list);
        });
    }

    private void OnBoxItemSelected(CharacterBoxInfo info)
    {
        if (info == null) return;
        _curSelectBoxData = info;

        PartnerName.text = info.name;
        var isOwned = IsBoxOwned(info);
        if (BuyBtnGray != null) BuyBtnGray.SetActive(isOwned);
        if (BuyBtn != null) BuyBtn.interactable = !isOwned;
        Price.gameObject.SetActive(!isOwned);
        if (!isOwned) Price.text = info.paymentInfo?.price.ToString() ?? "0";

        // 交由 BudBoxModel 统一加载盒子模型与自定义纹理
        if (BudBoxModel != null)
        {
            BudBoxModel.LoadBoxScene(info.metaDataUrl);
        }

        BoxAdpter.SelectById(info.id);
        FetchBoxCreator(info.creator);
    }

    private void FetchBoxCreator(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return;
        AIPartnerShopRequestCtrl.Inst.RequestGetUserInfo(uid, userInfo =>
        {
            if (userInfo != null && HeadImg != null) HeadImg.Load(userInfo.portraitUrl);
        });
    }

    private void OnHeadBtnClick()
    {
        if (_curSelectBoxData == null) return;
        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.CharacterBox, _curSelectBoxData.id);
    }

    private static bool IsBoxOwned(CharacterBoxInfo info)
    {
        return info.consumed == 1
               || info.creator == AccountDataManager.Inst.Uid;
    }

    private void BuyBtnOnClick()
    {
        if (_currentTab == AIPartnerTabSecond.PartnerBox)
        {
            if (_curSelectBoxData == null) return;
            UIManager.Inst.OpenPanel<BuyBoxPartnerItemsPanel>(
                PanelId.BuyBoxPartnerItemsPanel,
                _curSelectBoxData,
                (Action)OnBoxBuySuccess,
                (Action)OnBoxBuyClose);
            return;
        }
        if(_curSelectData == null) return;
        CharacterRoot.gameObject.SetActive(false);
        UIManager.Inst.OpenPanel<PartnerBuyItemsPanel>(PanelId.PartnerBuyItemsPanel, _curSelectData, (Action)OnBuySuccess, (Action)(() => CharacterRoot.gameObject.SetActive(true)));
    }

    private void OnBoxBuySuccess()
    {
        if (_curSectionId == null) return;
        var savedId = _curSelectBoxData?.id;
        AIPartnerShopRequestCtrl.Inst.RequestGetBoxSectionInfo(_curSectionId, list =>
        {
            _curBoxSectionItems = list ?? new List<CharacterBoxInfo>();
            if (BoxAdpter != null) BoxAdpter.SetItems(list);
            var updatedItem = list?.Find(i => i.id == savedId) ?? _curSelectBoxData;
            if (updatedItem != null)
            {
                _curSelectBoxData = null;
                OnBoxItemSelected(updatedItem);
            }
        });
    }

    private void OnBoxBuyClose()
    {
        // 购买面板关闭后无需额外操作（盒子模型仍在 BoxModelRoot 上）
    }

    private void ShrinkBtnOnClick()
    {
        if (_currentTab == AIPartnerTabSecond.PartnerBox)
        {
            UIManager.Inst.OpenPanel<PartnerShopItemsPanel>(PanelId.PartnerShopItemsPanel,
                _curBoxSectionItems,
                (Action<CharacterBoxInfo>)OnBoxSelectedFromExternal,
                _sectionDatas,
                _curSectionId,
                (Action<string, List<CharacterBoxInfo>>)OnBoxSectionChangedFromItemsPanel);
        }
        else
        {
            UIManager.Inst.OpenPanel<PartnerShopItemsPanel>(PanelId.PartnerShopItemsPanel,
                _curSectionItems,
                (Action<CabinCharacterUgcInfo>)OnPartnerSelectedFromExternal,
                _sectionDatas,
                _curSectionId,
                (Action<string, List<CabinCharacterUgcInfo>>)OnSectionChangedFromItemsPanel);
        }
    }

    public void OnBoxSelectedFromExternal(CharacterBoxInfo info)
    {
        if (info == null) return;
        OnBoxItemSelected(info);
    }

    private void OnBoxSectionChangedFromItemsPanel(string sectionId, List<CharacterBoxInfo> list)
    {
        if (_curSectionId == sectionId) return;
        _curSectionId = sectionId;
        _curBoxSectionItems = list ?? new List<CharacterBoxInfo>();
        foreach (var tag in _tagItems)
            tag.Tog.SetIsOnWithoutNotify(tag.SectionId == sectionId);
        if (BoxAdpter != null) BoxAdpter.SetItems(_curBoxSectionItems);
        if (_curBoxSectionItems.Count > 0)
            OnBoxItemSelected(_curBoxSectionItems[0]);
    }


    private void ToCreateBtnOnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.IncubationCabinDraftBox);
        CloseSelf();
    }

    private void BoxCreateBtnOnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.BoxSceneStudioMainPanel);
        CloseSelf();
    }

    private void ApplyBodyTypeOffset(int bodyType)
    {
        var y = -0.5f;
        switch ((CustomBodyTypeController.BodyType)bodyType)
        {
            case CustomBodyTypeController.BodyType.Type1:
            case CustomBodyTypeController.BodyType.Type2: y = -10f;  break;
            case CustomBodyTypeController.BodyType.Type3: y = 7f; break;
            case CustomBodyTypeController.BodyType.Type4: y = 23f; break;
            case CustomBodyTypeController.BodyType.Type5: y = -0.5f;  break;
            case CustomBodyTypeController.BodyType.Type6: y = -7f; break;
        }
        CharacterRoot.localPosition = new Vector3(0, y - 50, 0);
    }

    private void SetNoneTipsVisible(bool visible)
    {
        if (visible && NoneTipsText != null)
            NoneTipsText.text = _currentTab == AIPartnerTabSecond.PartnerBox
                ? "没有满足条件的伙伴盒子"
                : "没有满足条件的伙伴";
        NoneTips.SetActive(visible);
    }

    private void OnSearchAction(string keyword)
    {
        if (_currentTab == AIPartnerTabSecond.PartnerBox)
        {
            CabinBoxSceneNetManager.Inst.SearchCharacterBox(keyword, (isSuccess, result) =>
            {
                var list = isSuccess ? result?.ConvertAll(item => item.characterBoxInfo) : null;
                BoxAdpter.SetItems(list);
                SetNoneTipsVisible(list == null || list.Count == 0);
            });
        }
        else
        {
            AIPartnerShopRequestCtrl.Inst.RequestSearchPartner(keyword, (isSuccess, result) =>
            {
                Adpter.SetItems(isSuccess ? result : null);
                SetNoneTipsVisible(!isSuccess || result == null || result.Count == 0);
            });
        }
    }

    private void OnClearAction()
    {
        SetNoneTipsVisible(false);
        if (_currentTab == AIPartnerTabSecond.PartnerBox)
            BoxAdpter.SetItems(_curBoxSectionItems);
        else
            Adpter.SetItems(_curSectionItems);
    }

    private void OnInputBtnClick()
    {
        var keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "搜索AI伙伴名称",
            inputMode = (int)KeyBoardInputMode.All,
            maxLength = 30,
            inputFlag = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("您的输入超出了限制"),
            defaultText = _searchKeyword,
            returnKeyType = (int)ReturnType.Search,
            textSecurity = 1
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboardInput);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }

    private void OnKeyboardInput(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            SetInputDefault();
            return;
        }
        _searchKeyword = input;
        SearchInputText.color = Color.black;
        SearchInputText.text = input;
        clearInputButton.gameObject.SetActive(true);
        OnSearchAction(input);
    }

    private void OnSearchCharacterBtnClick()
    {
        if (string.IsNullOrEmpty(_searchKeyword)) return;
        OnSearchAction(_searchKeyword);
    }

    private void OnClearInputBtnClick()
    {
        SetInputDefault();
    }

    private void SetInputDefault()
    {
        _searchKeyword = string.Empty;
        SearchInputText.color = DataUtil.DeSerializeColor("9E9E9E");
        SearchInputText.text = "搜索AI伙伴名称";
        clearInputButton.gameObject.SetActive(false);
        OnClearAction();
    }

    public override void OnHidden()
    {
        base.OnHidden();
        _standbyAnimCtrl.Stop();

        // 释放盒子模型及其动态纹理，防止内存泄漏
        if (BudBoxModel != null)
        {
            BudBoxModel.Clear();
        }
    }
}
