using System;
using System.Collections.Generic;
using Game.Avatar;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PartnerDetailPanel : BasePanel<PartnerDetailPanel>
{
    [SerializeField] private CButton closeBtn;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] private CButton showBtn;
    [SerializeField] private Text partnerName;
    [SerializeField] private CButton contextBtn;
    [SerializeField] private Transform tagContent;
    [SerializeField] private PartnerDetailTagItem tagItem;
    [SerializeField] private PartnerDetailSkinAdpter Adpter;
    [SerializeField] private List<Toggle> tabToggle;
    [SerializeField] private CButton BuyBtn;
    [SerializeField] private Text Price;
    [SerializeField] private GameObject OverHave;
    [SerializeField] private CButton ShowBgBtn;
    [Header("Node1")]
    [SerializeField] private Transform node1;
    [SerializeField] private CButton btn_all_anim;
    [SerializeField] private GameObject interactRoleItemPrefab;
    [SerializeField] private Transform Content1;
    [SerializeField] private Transform Content2;
    [SerializeField] private Text txt_buy_count;

    [Header("Node2")]
    [SerializeField] private Transform node2;
    [SerializeField] private Transform Node2Content;
    [SerializeField] private GameObject interact_node2_itemPrefab;

    [Header("Node3")]
    [SerializeField] private Transform node3;
    [SerializeField] private Transform Node3Content;
    [SerializeField] private GameObject interact_node3_itemPrefab;

    private CabinCharacterUgcInfo _characterInfo;
    private int _curTabIndex;
    private List<PartnerDetailTagItem> _tagItems = new List<PartnerDetailTagItem>();
    private PartnerDetailTagItem _selectedTagItem;
    private List<CabinCharacterPackInfo> _packDataList = new List<CabinCharacterPackInfo>();
    private CharacterWrap _characterWrap;
    private Action _onBuySuccessCallback;

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(CloseSelf);
        contextBtn.onClick.AddListener(OnContextBtnClick);
        showBtn.onClick.AddListener(OnShowBtnClick);
        BuyBtn.onClick.AddListener(OnBuyBtnClick);
        Adpter.OnItemSelected = OnSkinItemSelected;
        for (int i = 0; i < tabToggle.Count; i++)
        {
            int idx = i;
            tabToggle[i].onValueChanged.AddListener(isOn => { if (isOn) OnTabChanged(idx); });
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _characterInfo = args?.Length > 0 ? args[0] as CabinCharacterUgcInfo : null;
        if (_characterInfo == null)
        {
            Debug.LogError("PartnerDetailPanel: 角色信息为空");
            return;
        }
        _onBuySuccessCallback = args?.Length > 1 ? args[1] as Action : null;
        _curTabIndex = 0;
        node1.gameObject.SetActive(true);
        node2.gameObject.SetActive(false);
        node3.gameObject.SetActive(false);
        if (tabToggle.Count > 0) tabToggle[0].isOn = true;
        ShowCharacterModel();
        RefreshCurrentNode();
        ShowDefaultInfo();
    }

    private void ShowDefaultInfo()
    {
        partnerName.text = _characterInfo.name;
        Price.text = _characterInfo.paymentInfo?.price.ToString() ?? "0";
        var isOwned = false; // TODO: 全局缓存移除后暂不判断所有权，后续通过接口返回字段处理
        BuyBtn.gameObject.SetActive(!isOwned);
        OverHave.SetActive(isOwned);
        CabinNetManager.Inst.GetExtensionPackBatchInfo(new List<string>() { _characterInfo.id }, ShowExtensionPackBySever);
    }

    private void ShowExtensionPackBySever(bool isSuccess, List<CabinCharacterPackInfo> dataList)
    {
        _packDataList = (isSuccess && dataList != null) ? dataList : new List<CabinCharacterPackInfo>();

        for (int i = tagContent.childCount - 1; i >= 0; i--)
            Destroy(tagContent.GetChild(i).gameObject);
        _tagItems.Clear();
        _selectedTagItem = null;

        SpawnTagItem("全部", null);

        foreach (var pack in _packDataList)
            SpawnTagItem(pack.name ?? "", pack);

        if (_tagItems.Count > 0) OnTagItemClick(_tagItems[0]);
    }

    private void SpawnTagItem(string name, CabinCharacterPackInfo packInfo)
    {
        var item = Instantiate(tagItem, tagContent);
        item.gameObject.SetActive(true);
        item.SetData(name, packInfo, OnTagItemClick);
        _tagItems.Add(item);
    }

    private void OnTagItemClick(PartnerDetailTagItem item)
    {
        if (_selectedTagItem == item) return;
        if (_selectedTagItem != null) _selectedTagItem.SetSelected(false);
        _selectedTagItem = item;
        _selectedTagItem.SetSelected(true);
        RefreshSkinAdpter(item);
    }

    private void RefreshSkinAdpter(PartnerDetailTagItem tag)
    {
        if (Adpter == null) return;
        List<SkinPackInfo> skins;
        if (tag.PackInfo == null)
        {
            skins = new List<SkinPackInfo>();
            if (_characterInfo?.skinPack != null) skins.AddRange(_characterInfo.skinPack);
            foreach (var pack in _packDataList)
                if (pack.skinPack != null) skins.AddRange(pack.skinPack);
        }
        else
        {
            skins = tag.PackInfo.skinPack ?? new List<SkinPackInfo>();
        }
        Adpter.SetItems(skins);
    }

    private void OnTabChanged(int index)
    {
        _curTabIndex = index;
        node1.gameObject.SetActive(index == 0);
        node2.gameObject.SetActive(index == 1);
        node3.gameObject.SetActive(index == 2);
        RefreshCurrentNode();
    }

    private void RefreshCurrentNode()
    {
        if (_characterInfo == null) return;
        if (_curTabIndex == 0) RefreshNode1();
        else if (_curTabIndex == 1) RefreshNode2();
        else if (_curTabIndex == 2) RefreshNode3();
    }

    private void RefreshNode1()
    {
        interactRoleItemPrefab.SetActive(false);

        for (int i = Content1.childCount - 1; i >= 0; i--)
            Destroy(Content1.GetChild(i).gameObject);
        for (int i = Content2.childCount - 1; i >= 0; i--)
            Destroy(Content2.GetChild(i).gameObject);

        var loopList    = _characterInfo.pendingEmote?.loopEmoteList ?? new List<pEmoteData>();
        var nonLoopList = _characterInfo.pendingEmote?.emoteList     ?? new List<pEmoteData>();

        foreach (var d in loopList)    SpawnNode1Item(d, Content1);
        foreach (var d in nonLoopList) SpawnNode1Item(d, Content2);
    }

    private void SpawnNode1Item(pEmoteData data, Transform parent)
    {
        var go = Instantiate(interactRoleItemPrefab, parent);
        go.SetActive(true);
        go.GetComponent<RoleInteractNode1RoleItem>().Init(data);
    }

    private void RefreshNode2()
    {
        interact_node2_itemPrefab.SetActive(false);

        for (int i = Node2Content.childCount - 1; i >= 0; i--)
            Destroy(Node2Content.GetChild(i).gameObject);

        var activationList = _characterInfo.activation ?? new List<characterInteraction>();
        var isOpenList     = new List<bool>(new bool[activationList.Count]);

        for (int i = 0; i < activationList.Count; i++)
        {
            int idx = i;
            var go   = Instantiate(interact_node2_itemPrefab, Node2Content);
            go.SetActive(true);
            var item = go.GetComponent<RoleInteractNode2RoleItem>();
            item.Init(_characterInfo, activationList[idx], _characterInfo.toneId, idx, false);
            item.onCommonSelectBtnClick = () =>
            {
                isOpenList[idx] = !isOpenList[idx];
                GlobalFuncExtensions.RefreshLayout(Node2Content);
            };
        }
    }

    private void RefreshNode3()
    {
        interact_node3_itemPrefab.SetActive(false);

        for (int i = Node3Content.childCount - 1; i >= 0; i--)
            Destroy(Node3Content.GetChild(i).gameObject);

        var voiceCommandsList = _characterInfo.voiceCommands ?? new List<voiceCommands>();
        var isOpenList        = new List<bool>(new bool[voiceCommandsList.Count]);

        for (int i = 0; i < voiceCommandsList.Count; i++)
        {
            int idx = i;
            var go   = Instantiate(interact_node3_itemPrefab, Node3Content);
            go.SetActive(true);
            var item = go.GetComponent<RoleInteractNode3RoleItem>();
            item.Init(_characterInfo,voiceCommandsList[idx], _characterInfo.toneId, idx, false);
            item.editCommandBox.SetOnInput(null); // 纯展示：清除编辑回调，防止触发网络请求
            item.onCommonSelectBtnClick = () =>
            {
                isOpenList[idx] = !isOpenList[idx];
                GlobalFuncExtensions.RefreshLayout(Node3Content);
            };
        }
    }

    private void ShowCharacterModel()
    {
        var pack = _characterInfo.skinPack?.Find(p => p.isDefault == 1) ?? _characterInfo.skinPack?[0];
        if (pack == null || string.IsNullOrEmpty(pack.avatarJson)) return;
        var characterData = CharacterData.DeserializeObject(pack.avatarJson);
        if (_characterWrap != null)
        {
            _characterWrap.RefreshAvatar(characterData);
        }
        else
        {
            _characterWrap = AvatarController.Inst.CreateUIAvatar(characterData);
            _characterWrap.SetParent(characterRoot, true);
            avatarCameraController.RotateTarget = characterRoot;
        }
    }

    private void OnSkinItemSelected(SkinPackInfo pack)
    {
        if (pack == null || string.IsNullOrEmpty(pack.avatarJson) || _characterWrap == null) return;
        var characterData = CharacterData.DeserializeObject(pack.avatarJson);
        _characterWrap.RefreshAvatar(characterData);
    }

    private void OnContextBtnClick()
    {

    }

    private void OnShowBtnClick()
    {
        ShowBgBtn.onClick.RemoveAllListeners();
        ShowBgBtn.onClick.AddListener(() =>
        {
            GameObjectEx.FindChildByName(transform,"RightRoot").gameObject.SetActive(true);
            ShowBgBtn.gameObject.SetActive(false);
        });
        GameObjectEx.FindChildByName(transform,"RightRoot").gameObject.SetActive(false);
        ShowBgBtn.gameObject.SetActive(true);
    }

    private void OnBuyBtnClick()
    {
        UIManager.Inst.OpenPanel<PartnerBuyItemsPanel>(PanelId.PartnerBuyItemsPanel, _characterInfo, (Action)OnBuySuccess);
    }

    private void OnBuySuccess()
    {
        BuyBtn.gameObject.SetActive(false);
        OverHave.SetActive(true);
        _onBuySuccessCallback?.Invoke();
    }
}
