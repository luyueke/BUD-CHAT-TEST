using System;
using System.Collections;
using System.Collections.Generic;
using Game.CommunityGame;
using Game.Store;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PartnerShopItemsPanel : BasePanel<PartnerShopItemsPanel>
{
    [SerializeField] private CButton CloseMask;
    [SerializeField] private AIPartnerAdpter Adpter;
    [SerializeField] private PartnerBoxAdpter BoxAdpter;
    [SerializeField] private Transform ShopTagTsf;
    [SerializeField] private ShopTagItem ShopTagItem;
    [SerializeField] private CButton SearchCharacterBtn;
    [SerializeField] private CButton InputBtn;
    [SerializeField] private CButton clearInputButton;
    [SerializeField] private Text SearchInputText;

    private Action<CabinCharacterUgcInfo> _onItemSelected;
    private Action<string, List<CabinCharacterUgcInfo>> _onSectionChanged;
    private List<ShopTagItem> _tagItems = new List<ShopTagItem>();
    private List<CabinCharacterUgcInfo> _curSectionItems = new List<CabinCharacterUgcInfo>();
    private string _curSectionId;
    private string _searchKeyword = string.Empty;
    private bool _isBoxMode;
    private Action<CharacterBoxInfo> _onBoxItemSelected;
    private Action<string, List<CharacterBoxInfo>> _onBoxSectionChanged;
    private List<CharacterBoxInfo> _curBoxSectionItems = new List<CharacterBoxInfo>();

    public override void OnCreate()
    {
        base.OnCreate();
        CloseMask.onClick.AddListener(CloseSelf);
        Adpter.OnItemSelected = OnPartnerItemSelected;
        BoxAdpter.OnItemSelected = OnBoxItemSelected;
        InputBtn.onClick.AddListener(OnInputBtnClick);
        SearchCharacterBtn.onClick.AddListener(OnSearchCharacterBtnClick);
        clearInputButton.onClick.AddListener(OnClearInputBtnClick);
        clearInputButton.gameObject.SetActive(false);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        var boxList  = args?.Length > 0 ? args[0] as List<CharacterBoxInfo> : null;
        _isBoxMode   = boxList != null;

        Adpter.gameObject.SetActive(!_isBoxMode);
        BoxAdpter.gameObject.SetActive(_isBoxMode);

        if (_isBoxMode)
        {
            _curBoxSectionItems  = boxList;
            _onBoxItemSelected   = args?.Length > 1 ? args[1] as Action<CharacterBoxInfo> : null;
            var sectionDatas     = args?.Length > 2 ? args[2] as List<UgcSectionData> : null;
            var curSectionId     = args?.Length > 3 ? args[3] as string : null;
            _onBoxSectionChanged = args?.Length > 4 ? args[4] as Action<string, List<CharacterBoxInfo>> : null;
            InitTags(sectionDatas, curSectionId);
            StartCoroutine(SetBoxItemsNextFrame(boxList));
        }
        else
        {
            var list          = args?.Length > 0 ? args[0] as List<CabinCharacterUgcInfo> : null;
            _curSectionItems  = list ?? new List<CabinCharacterUgcInfo>();
            _onItemSelected   = args?.Length > 1 ? args[1] as Action<CabinCharacterUgcInfo> : null;
            var sectionDatas  = args?.Length > 2 ? args[2] as List<UgcSectionData> : null;
            var curSectionId  = args?.Length > 3 ? args[3] as string : null;
            _onSectionChanged = args?.Length > 4 ? args[4] as Action<string, List<CabinCharacterUgcInfo>> : null;
            InitTags(sectionDatas, curSectionId);
            StartCoroutine(SetItemsNextFrame(list));
        }
    }

    private void InitTags(List<UgcSectionData> sectionDatas, string curSectionId)
    {
        for (int i = _tagItems.Count - 1; i >= 0; i--)
            Destroy(_tagItems[i].gameObject);
        _tagItems.Clear();
        if (sectionDatas == null || sectionDatas.Count == 0) return;

        var group = ShopTagTsf.GetComponent<ToggleGroup>();
        if (group == null) group = ShopTagTsf.gameObject.AddComponent<ToggleGroup>();
        group.allowSwitchOff = false;

        _curSectionId = curSectionId;
        for (int i = 0; i < sectionDatas.Count; i++)
        {
            var tag = Instantiate(ShopTagItem, ShopTagTsf);
            tag.gameObject.SetActive(true);
            tag.InitItem(new SectionItemData
            {
                sectionId   = sectionDatas[i].sectionId,
                sectionName = sectionDatas[i].sectionName
            }, i, OnSectionTagClick);
            tag.Tog.group = group;
            _tagItems.Add(tag);
        }

        // 所有 tag 创建完毕后再设置选中状态，避免 ToggleGroup 在循环中途被后续 tag 打断
        ShopTagItem tagToSelect = _tagItems.Find(t => t.SectionId == curSectionId);
        if (tagToSelect == null && _tagItems.Count > 0) tagToSelect = _tagItems[0];
        if (tagToSelect != null) tagToSelect.Tog.SetIsOnWithoutNotify(true);
    }

    private void OnSectionTagClick(string sectionId)
    {
        if (_curSectionId == sectionId) return;
        _curSectionId = sectionId;

        if (_isBoxMode)
        {
            AIPartnerShopRequestCtrl.Inst.RequestGetBoxSectionInfo(sectionId, list =>
            {
                if (BoxAdpter == null) return;
                _curBoxSectionItems = list ?? new List<CharacterBoxInfo>();
                BoxAdpter.SetItems(_curBoxSectionItems);
                _onBoxSectionChanged?.Invoke(sectionId, _curBoxSectionItems);
            });
            return;
        }

        AIPartnerShopRequestCtrl.Inst.RequestGetPartnerSectionInfo(sectionId, (list) =>
        {
            if (Adpter == null) return;
            _curSectionItems = list ?? new List<CabinCharacterUgcInfo>();
            SetInputDefault();
            _onSectionChanged?.Invoke(sectionId, _curSectionItems);
        });
    }

    public void SyncSection(string sectionId, List<CabinCharacterUgcInfo> list)
    {
        if (_curSectionId == sectionId) return;
        _curSectionId = sectionId;
        foreach (var tag in _tagItems)
            tag.Tog.SetIsOnWithoutNotify(tag.SectionId == sectionId);
        _curSectionItems = list ?? new List<CabinCharacterUgcInfo>();
        SetInputDefault();
    }

    private void OnSearchAction(string keyword)
    {
        AIPartnerShopRequestCtrl.Inst.RequestSearchPartner(keyword, (isSuccess, result) =>
        {
            Adpter.SetItems(isSuccess ? result : null);
        });
    }

    private void OnClearAction()
    {
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

    private IEnumerator SetItemsNextFrame(List<CabinCharacterUgcInfo> list)
    {
        yield return null;
        Adpter.SetItems(list);
    }

    private IEnumerator SetBoxItemsNextFrame(List<CharacterBoxInfo> list)
    {
        yield return null;
        BoxAdpter.SetItems(list);
    }

    private void OnBoxItemSelected(CharacterBoxInfo info)
    {
        if (info == null) return;
        _onBoxItemSelected?.Invoke(info);
        CloseSelf();
    }

    public void SyncBoxSection(string sectionId, List<CharacterBoxInfo> list)
    {
        if (_curSectionId == sectionId) return;
        _curSectionId = sectionId;
        foreach (var tag in _tagItems)
            tag.Tog.SetIsOnWithoutNotify(tag.SectionId == sectionId);
        _curBoxSectionItems = list ?? new List<CharacterBoxInfo>();
        BoxAdpter.SetItems(_curBoxSectionItems);
    }

    private void OnPartnerItemSelected(CabinCharacterUgcInfo info)
    {
        if (info == null) return;
        _onItemSelected?.Invoke(info);
        CloseSelf();
    }
}
