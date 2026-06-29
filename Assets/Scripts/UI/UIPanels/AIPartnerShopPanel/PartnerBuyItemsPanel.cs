using System;
using System.Collections.Generic;
using Game.Avatar;
using Game.Database;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

public class PartnerBuyItemsPanel : BasePanel<PartnerBuyItemsPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private Transform CharacterRoot;
    [SerializeField] private AvatarCameraController AvatarCameraController;
    [SerializeField] private CabinCharacterCardItem CardItem;
    [SerializeField] private GameObject GetOver;
    [SerializeField] private Transform ExpandContent;
    [SerializeField] private ExpandItem ExpandItem;
    [SerializeField] private CButton BuyBtn;
    [SerializeField] private Text BuyPrice;
    [SerializeField] private Transform ActorTsf;
    [SerializeField] private PartnerActorItem ActorItemPrefab;

    private CabinCharacterUgcInfo _itemData;
    private bool _isOwned;
    private List<ExpandItem> _expandItemList = new List<ExpandItem>();
    private List<PartnerActorItem> _actorItemPool = new List<PartnerActorItem>();
    private Action _onSuccess;
    private Action _onClose;
    private CharacterWrap _characterWrap;

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        BuyBtn.onClick.AddListener(OnBuyBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _itemData = args?.Length > 0 ? args[0] as CabinCharacterUgcInfo : null;
        if (_itemData == null)
        {
            Debug.LogError("选中道具信息为空！");
            return;
        }
        _onSuccess = args?.Length > 1 ? args[1] as Action : null;
        _onClose = args?.Length > 2 ? args[2] as Action : null;
        ShowPanelInfo();
    }

    public override void OnHidden()
    {
        base.OnHidden();
        _onClose?.Invoke();
        _onClose = null;
    }

    private void ShowPanelInfo()
    {
        CardItem.SetData(_itemData, _ =>
        {
            var pack = _itemData.skinPack?.Find(p => p.isDefault == 1) ?? _itemData.skinPack?[0];
            ShowSkinByPack(pack);
            RefreshPartnerActor(_itemData);
        });
        CardItem.SetBadgesVisible(false);
        var inventoryData = BagDatabase.Inst.Select(_itemData.id);
        _isOwned = (inventoryData != null && inventoryData.OwnedNum > 0)
                   || _itemData.creator == AccountDataManager.Inst.Uid;
        GetOver.SetActive(_isOwned);
        ShowCharacterModel();
        RefreshPartnerActor(_itemData);
        RefreshBuyPrice();
        CabinNetManager.Inst.GetExtensionPackBatchInfo(_itemData.extensionPackList, ShowExtensionPackBySever);
    }

    private void ShowCharacterModel()
    {
        var pack = _itemData.skinPack?.Find(p => p.isDefault == 1) ?? _itemData.skinPack?[0];
        if (pack == null || string.IsNullOrEmpty(pack.avatarJson)) return;
        var characterData = CharacterData.DeserializeObject(pack.avatarJson);
        if (_characterWrap != null)
        {
            _characterWrap.RefreshAvatar(characterData);
        }
        else
        {
            _characterWrap = AvatarController.Inst.CreateUIAvatar(characterData);
            _characterWrap.SetParent(CharacterRoot, true);
            AvatarCameraController.RotateTarget = CharacterRoot;
            AvatarCameraController.isZoomEnabled = false;
        }
    }

    private void RefreshPartnerActor(CabinCharacterBaseInfo source)
    {
        var activations = source?.activation;
        var emoteList = source?.pendingEmote?.emoteList;
        var loopEmoteList = source?.pendingEmote?.loopEmoteList;
        var voiceCmds = source?.voiceCommands;

        int needed = (activations?.Count ?? 0) + (emoteList?.Count ?? 0) + (loopEmoteList?.Count ?? 0) + (voiceCmds?.Count ?? 0);

        while (_actorItemPool.Count < needed)
        {
            var newItem = Instantiate(ActorItemPrefab, ActorTsf);
            _actorItemPool.Add(newItem);
        }

        foreach (var poolItem in _actorItemPool)
            poolItem.gameObject.SetActive(false);

        int index = 0;
        if (activations != null)
            foreach (var action in activations)
            {
                _actorItemPool[index].gameObject.SetActive(true);
                _actorItemPool[index].SetData(action);
                index++;
            }
        if (emoteList != null)
            foreach (var emote in emoteList)
            {
                _actorItemPool[index].gameObject.SetActive(true);
                _actorItemPool[index].SetData(emote);
                index++;
            }
        if (loopEmoteList != null)
            foreach (var emote in loopEmoteList)
            {
                _actorItemPool[index].gameObject.SetActive(true);
                _actorItemPool[index].SetData(emote);
                index++;
            }
        if (voiceCmds != null)
            foreach (var cmd in voiceCmds)
            {
                _actorItemPool[index].gameObject.SetActive(true);
                _actorItemPool[index].SetData(cmd);
                index++;
            }
    }

    private void ShowExtensionPackBySever(bool isSuccess, List<CabinCharacterPackInfo> dataList)
    {
        ExpandItem.gameObject.SetActive(false);
        for (int i = ExpandContent.childCount - 1; i >= 0; i--)
        {
            var child = ExpandContent.GetChild(i).gameObject;
            if (child == ExpandItem.gameObject) continue;
            Destroy(child);
        }
        _expandItemList.Clear();

        if (!isSuccess || dataList == null) return;

        foreach (var packInfo in dataList)
        {
            if (packInfo.ugcclass != (int)UGCClass.Published) continue;
            foreach (var skinInfo in packInfo.skinPack)
            {
                var item = Instantiate(ExpandItem, ExpandContent);
                item.gameObject.SetActive(true);
                item.SetData(new RoleSkinData(skinInfo, packInfo.name), packInfo, OnExpandItemSelectionChanged);
                _expandItemList.Add(item);
            }
        }
    }

    private void ShowSkinByPack(SkinPackInfo pack)
    {
        if (pack == null || string.IsNullOrEmpty(pack.avatarJson) || _characterWrap == null) return;
        _characterWrap.RefreshAvatar(CharacterData.DeserializeObject(pack.avatarJson));
    }

    private void OnExpandItemSelectionChanged(ExpandItem item)
    {
        ShowSkinByPack(item.SkinData?.skinPackInfo);
        RefreshPartnerActor(item.PackInfo);
        RefreshBuyPrice();
    }

    private List<string> GetSelectedPackIds()
    {
        var seen = new HashSet<string>();
        var ids = new List<string>();
        foreach (var item in _expandItemList)
            if (item.IsSelected && item.PackInfo != null && seen.Add(item.PackInfo.id))
                ids.Add(item.PackInfo.id);
        return ids;
    }

    private List<CabinCharacterPackInfo> GetSelectedPacks()
    {
        var seen = new HashSet<string>();
        var packs = new List<CabinCharacterPackInfo>();
        foreach (var item in _expandItemList)
            if (item.IsSelected && item.PackInfo != null && seen.Add(item.PackInfo.id))
                packs.Add(item.PackInfo);
        return packs;
    }

    private int GetSelectedPacksPrice()
    {
        var seen = new HashSet<string>();
        int total = 0;
        foreach (var item in _expandItemList)
            if (item.IsSelected && item.PackInfo != null && seen.Add(item.PackInfo.id))
                total += item.PackInfo.paymentInfo?.price ?? 0;
        return total;
    }

    private void RefreshBuyPrice()
    {
        int total = _isOwned ? 0 : (_itemData.paymentInfo?.price ?? 0);
        total += GetSelectedPacksPrice();
        BuyPrice.text = total.ToString();
        BuyBtn.gameObject.SetActive(total > 0);
    }

    private bool HasEnoughBalance(out int needNum)
    {
        int total = _isOwned ? 0 : (_itemData.paymentInfo?.price ?? 0);
        total += GetSelectedPacksPrice();

        var currencyType = _itemData.paymentInfo?.currencyType ?? CurrencyType.None;
        var balance = AccountDataManager.Inst.BalanceInfo.GetAccountCount(currencyType);
        needNum = Mathf.Max(0, total - balance);
        return needNum == 0;
    }

    private void OpenExchangePanel(int needNum)
    {
        var currencyType = _itemData.paymentInfo?.currencyType ?? CurrencyType.None;
        switch (currencyType)
        {
            case CurrencyType.Coin:
                UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                    .SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                break;
            case CurrencyType.Badge:
                UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                    .SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                break;
            case CurrencyType.PinkCoin:
                if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                    UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                        .SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                break;
            case CurrencyType.Gem:
                UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                break;
            default:
                UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                    .SetData(currencyType, CurrencyType.Gem, needNum);
                break;
        }
    }

    private void OnBuyBtnClick()
    {
        int total = _isOwned ? 0 : (_itemData.paymentInfo?.price ?? 0);
        total += GetSelectedPacksPrice();
        if (total <= 0) return;

        if (!HasEnoughBalance(out int needNum))
        {
            OpenExchangePanel(needNum);
            return;
        }

        var packIds = GetSelectedPackIds();
        var selectedPacks = GetSelectedPacks();

        if (!_isOwned)
        {
            AIPartnerShopRequestCtrl.Inst.RequestBuyPartner(_itemData.id,
                () =>
                {
                    _isOwned = true;
                    GetOver.SetActive(true);
                    RefreshBuyPrice();
                    if (packIds.Count > 0)
                        BuyExtensionPacks(packIds, selectedPacks, boughtCharacter: true);
                    else
                    {
                        AccountDataManager.Inst.BalanceInfo.Refresh();
                        CloseSelf();
                        OpenRewardPanel(includeCharacter: true, packs: null, _onSuccess);
                    }
                },
                err => Debug.LogError("购买角色失败: " + err));
        }
        else if (packIds.Count > 0)
        {
            BuyExtensionPacks(packIds, selectedPacks, boughtCharacter: false);
        }
    }

    private void BuyExtensionPacks(List<string> packIds, List<CabinCharacterPackInfo> packs, bool boughtCharacter)
    {
        AIPartnerShopRequestCtrl.Inst.RequestBuyPartner(packIds,
            () =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                RefreshBuyPrice();
                CloseSelf();
                OpenRewardPanel(boughtCharacter, packs, _onSuccess);
            },
            err => Debug.LogError("购买捆绑包失败: " + err));
    }

    private void OpenRewardPanel(bool includeCharacter, List<CabinCharacterPackInfo> packs, Action onClose = null)
    {
        var rewards = new List<BoxRewardItemData>();
        if (includeCharacter)
            rewards.Add(new BoxRewardItemData
            {
                itemType = BoxRewardItemData.ItemType.Character,
                characterData = _itemData
            });
        if (packs != null)
            foreach (var pack in packs)
                rewards.Add(new BoxRewardItemData
                {
                    itemType = BoxRewardItemData.ItemType.Skin,
                    characterData = pack
                });
        UIManager.Inst.OpenPanel<CommonBoxRewardPanel>(PanelId.CommonBoxRewardPanel).ShowReward(rewards, onClose);
    }
}
