using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using Game.Database;
using Game.Store;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class TheatreShopPanel : BasePanel<TheatreShopPanel>
{
    [SerializeField] private GameObject TransBg;
    [SerializeField] private Transform CharacterRoot;
    [SerializeField] private AvatarCameraController AvatarCameraController;
    [SerializeField] private CButton BackButton;
    [SerializeField] private CButton LeftButton;
    [SerializeField] private CButton RightButton;
    [SerializeField] private Text TheatreName;
    [SerializeField] private Transform ScrollContent;
    [SerializeField] private RawImage TheatreImg;
    [SerializeField] private Text TheatrePrice;
    [SerializeField] private Text TheatreStatus;
    [SerializeField] private Transform ActionTsf;
    [SerializeField] private GameObject TheatreActionItem;
    [SerializeField] private Transform NoneActionTsf;
    [SerializeField] private GameObject WardrobeItem;
    [SerializeField] private CButton AllOverBtn;
    [SerializeField] private Transform AllOverSelected;
    [SerializeField] private Text BuyText;
    [SerializeField] private Text PriceNum;
    [SerializeField] private CButton BuyBtn;
    [SerializeField] private GameObject BuyBtnTicket;

    private OCTheatreInfo _dataBase;
    private CharacterWrap _characterWrap;
    private int _actorIndex;
    private readonly Dictionary<string, OCTheatreAvatarInfo> _actorInfoCache = new();
    private readonly Dictionary<string, List<CharacterData>> _clothesCache = new();
    private readonly List<TheatreWardrobeItem> _wardrobeItems = new();
    private readonly Dictionary<string, List<Action<OCTheatreAvatarInfo>>> _actorInfoCallbacks = new();
    private bool _batchFetchPending = false;
    private Coroutine _loadActorCoroutine;
    private Coroutine _cycleCoroutine;
    private OCTheatreAvatarInfo _currentActorInfo;
    private int _currentClothesIndex;
    private CabinPgcUgcPlayController _emotePlayCtrl;

    public override void OnCreate()
    {
        base.OnCreate();
        BackButton.onClick.AddListener(CloseSelf);
        LeftButton.onClick.AddListener(LeftButtonOnClick);
        RightButton.onClick.AddListener(RightButtonOnClick);
        AllOverBtn.onClick.AddListener(AllOverBtnOnClick);
        BuyBtn.onClick.AddListener(BuyBtnOnClick);
      //  BuyBtn.onClick.AddListener(BuyBtnTicketOnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length <= 0)
        {
            Debug.LogError("TheatreShopPanel: 传入数据为空");
            CloseSelf();
            return;
        }
        _dataBase = args[0] as OCTheatreInfo;
        if (_dataBase == null)
        {
            CloseSelf();
            return;
        }

        var fittingRoom = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        if (fittingRoom != null) fittingRoom.characterRoot.gameObject.SetActive(false);

        _actorIndex = 0;
        _actorInfoCache.Clear();
        _clothesCache.Clear();
        _actorInfoCallbacks.Clear();
        _batchFetchPending = false;

        InitBGUI();
        InitTheatreUI();
        InitWardrobeItems();
        InitActionItems();

        bool hasMultiple = _dataBase.avatarList != null && _dataBase.avatarList.Count > 1;
        LeftButton.gameObject.SetActive(hasMultiple);
        RightButton.gameObject.SetActive(hasMultiple);

        LoadActor(0);
        OnWardrobeSelectionChanged();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        var fittingRoom = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        if (fittingRoom != null) fittingRoom.characterRoot.gameObject.SetActive(true);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_cycleCoroutine != null) { StopCoroutine(_cycleCoroutine); _cycleCoroutine = null; }
        DestroyCharacter();
    }

    private void InitBGUI()
    {
        if (TransBg == null) return;
        var item = TransBg.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas",
            new List<string> { "actor_bg_icon1", "actor_bg_icon2", "actor_bg_icon3", "actor_bg_icon4" });
        item.gameObject.SetActive(true);
    }

    private void InitTheatreUI()
    {
        TheatreName.text = _dataBase.name ?? string.Empty;
        var price = _dataBase.paymentInfo?.price ?? 0;
        TheatrePrice.text = price.ToString();
        bool theatreOwned = !string.IsNullOrEmpty(_dataBase?.id) && AssetsDataManager.IsOwned(_dataBase.id);
        var currencyType = _dataBase?.paymentInfo?.currencyType ?? CurrencyType.Coin;
        PriceNum.text = theatreOwned ? "0" : ApplyMonthCardDiscount(price, currencyType).ToString();

        if (!string.IsNullOrEmpty(_dataBase.cover))
        {
            Game.Utils.GameSimpleImageDownloader.Instance.Enqueue(new Game.Utils.GameSimpleImageDownloader.Request
            {
                url = _dataBase.cover,
                onDone = result =>
                {
                    TheatreImg.texture = result.CreateTextureFromReceivedData();
                    TheatreImg.enabled = true;
                }
            });
        }

        RefreshTheatreStatus();
    }

    private void RefreshTheatreStatus()
    {
        bool owned = !string.IsNullOrEmpty(_dataBase?.id) && AssetsDataManager.IsOwned(_dataBase.id);
        TheatrePrice.gameObject.SetActive(!owned);
        TheatreStatus.gameObject.SetActive(owned);
    }

    // ScrollContent: 每个演员一个 TheatreWardrobeItem
    private void InitWardrobeItems()
    {
        foreach (var old in _wardrobeItems)
            if (old != null) Destroy(old.gameObject);
        _wardrobeItems.Clear();

        if (_dataBase.avatarList == null) return;
        for (int i = 0; i < _dataBase.avatarList.Count; i++)
        {
            var go = Instantiate(WardrobeItem, ScrollContent);
            go.SetActive(true);
            if (!go.TryGetComponent<TheatreWardrobeItem>(out var item)) continue;
            var capturedIndex = i;
            item.OnSelect = () => SwitchToActor(capturedIndex);
            item.OnWardrobeSelectionChanged = OnWardrobeSelectionChanged;
            item.OnClothesItemClick = clothIndex => ShowSpecificCloth(capturedIndex, clothIndex);
            item.OnDirectBuyOfficialItem = NavigateToItem;
            item.SetSelected(i == 0);
            _wardrobeItems.Add(item);
            TryLoadWardrobeItemData(capturedIndex);
        }
        ActionTsf.SetAsLastSibling();
        NoneActionTsf.transform.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ScrollContent);
    }
    private void TryLoadWardrobeItemData(int index)
    {
        if (_dataBase?.avatarList == null || index < 0 || index >= _dataBase.avatarList.Count) return;
        var avatar = _dataBase.avatarList[index];
        if (avatar == null || string.IsNullOrEmpty(avatar.playerId)) return;

        RequestActorInfo(avatar.playerId, actorInfo =>
        {
            if (actorInfo == null)
            {
                if (index >= 0 && index < _wardrobeItems.Count)
                    if (_wardrobeItems[index] != null) _wardrobeItems[index].gameObject.SetActive(false);
                return;
            }
            if (index < 0 || index >= _wardrobeItems.Count) return;
            var item = _wardrobeItems[index];
            if (item == null) return;
            if (actorInfo.isBan >= 1 || actorInfo.isDelete >= 1)
            {
                item.gameObject.SetActive(false);
                return;
            }
            item.SetData(actorInfo);
        });
    }

    private void RequestActorInfo(string playerId, Action<OCTheatreAvatarInfo> callback)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            callback?.Invoke(null);
            return;
        }

        if (_actorInfoCache.TryGetValue(playerId, out var cachedInfo))
        {
            callback?.Invoke(cachedInfo);
            return;
        }

        if (!_actorInfoCallbacks.TryGetValue(playerId, out var callbackList))
        {
            callbackList = new List<Action<OCTheatreAvatarInfo>>();
            _actorInfoCallbacks[playerId] = callbackList;
        }
        callbackList.Add(callback);

        if (!_batchFetchPending)
        {
            _batchFetchPending = true;
            StartCoroutine(FlushBatchFetch());
        }
    }

    private IEnumerator FlushBatchFetch()
    {
        yield return null; // 等一帧，让同帧的所有请求全部入队

        _batchFetchPending = false;

        var pendingIds = _actorInfoCallbacks.Keys
            .Where(id => !_actorInfoCache.ContainsKey(id))
            .ToList();

        if (pendingIds.Count > 0)
        {
            bool done = false;
            string idList = string.Join(",", pendingIds);
            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.ActorBatchInfo, HttpMethod.GET,
                JsonConvert.SerializeObject(new { idList }),
                content =>
                {
                    var rsps = JsonConvert.DeserializeObject<BatchActorDetailRsp>(content);
                    if (rsps?.actorList != null)
                    {
                        foreach (var rsp in rsps.actorList)
                        {
                            var actorInfo = rsp?.actorInfo;
                            if (actorInfo != null) _actorInfoCache[actorInfo.id] = actorInfo;
                        }
                    }
                    done = true;
                },
                _ => done = true);

            float elapsed = 0f;
            while (!done && elapsed < 8f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        foreach (var kv in _actorInfoCallbacks.ToList())
        {
            _actorInfoCache.TryGetValue(kv.Key, out var info);
            foreach (var cb in kv.Value) cb?.Invoke(info);
        }
        _actorInfoCallbacks.Clear();
    }

    // ActionTsf: 每个动作（emote + 音效）一个 TheatreActionItem
    private void InitActionItems()
    {
        foreach (Transform child in ActionTsf)
            child.gameObject.SetActive(false);

        int count = 0;
        if (_dataBase.allUseEmote != null)
        {
            for(int i = 0; i < _dataBase.allUseEmote.Count; i++)
            {
                var emoteId = _dataBase.allUseEmote[i];
                if(ActionTsf.childCount > i)
                {
                    var existingItem = ActionTsf.GetChild(i);
                    if (existingItem != null && existingItem.TryGetComponent<TheatreActionItem>(out var item))
                    {
                        item.SetData(emoteId);
                        item.OnJumpBtnClicked = OnActionJumpBtnClicked;
                        existingItem.gameObject.SetActive(true);
                        count++;
                        continue;
                    }
                }
                else
                {
                    var go = Instantiate(TheatreActionItem, ActionTsf);
                    go.SetActive(true);
                    if (go.TryGetComponent<TheatreActionItem>(out var item))
                    {
                        item.SetData(emoteId, true);
                        item.OnJumpBtnClicked = OnActionJumpBtnClicked;
                    }
                    count++;
                }
            }

        }

        // if (_dataBase.allSoundEffects != null)
        // {
        //     int emoteCount = _dataBase.allUseEmote?.Count ?? 0;
        //     for (int i = 0; i < _dataBase.allSoundEffects.Count; i++)
        //     {
        //         var soundId = _dataBase.allSoundEffects[i];
        //         int childIdx = emoteCount + i;
        //         if (ActionTsf.childCount > childIdx)
        //         {
        //             var existingItem = ActionTsf.GetChild(childIdx);
        //             if (existingItem != null && existingItem.TryGetComponent<TheatreActionItem>(out var item))
        //             {
        //                 item.SetData(soundId, false);
        //                 existingItem.gameObject.SetActive(true);
        //                 count++;
        //                 continue;
        //             }
        //         }
        //         var go = Instantiate(TheatreActionItem, ActionTsf);
        //         go.SetActive(true);
        //         if (go.TryGetComponent<TheatreActionItem>(out var newItem))
        //             newItem.SetData(soundId, false);
        //         count++;
        //     }
        // }

        bool hasAction = count > 0;
        ActionTsf.gameObject.SetActive(hasAction);
        NoneActionTsf.gameObject.SetActive(!hasAction);
    }

    private void LoadActor(int index)
    {
        if (_cycleCoroutine != null) { StopCoroutine(_cycleCoroutine); _cycleCoroutine = null; }
        if (_loadActorCoroutine != null) { StopCoroutine(_loadActorCoroutine); _loadActorCoroutine = null; }
        _loadActorCoroutine = StartCoroutine(LoadActorCoroutine(index));
    }

    private IEnumerator LoadActorCoroutine(int index)
    {
        var avatarList = _dataBase?.avatarList;
        if (avatarList == null || avatarList.Count == 0) yield break;

        var clampedIndex = Mathf.Clamp(index, 0, avatarList.Count - 1);
        var targetAvatar = avatarList[clampedIndex];
        if (targetAvatar == null || string.IsNullOrEmpty(targetAvatar.playerId)) yield break;

        // 触发批量拉取（同帧内所有演员已入队），等待完成后缓存中有全部数据
        if (!_actorInfoCache.ContainsKey(targetAvatar.playerId))
        {
            bool done = false;
            RequestActorInfo(targetAvatar.playerId, _ => done = true);
            float elapsed = 0f;
            while (!done && elapsed < 8f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        _loadActorCoroutine = null;

        // 从 index 开始依次找第一个有效演员
        for (int i = clampedIndex; i < avatarList.Count; i++)
        {
            var avatar = avatarList[i];
            if (avatar == null || string.IsNullOrEmpty(avatar.playerId)) continue;
            if (!_actorInfoCache.TryGetValue(avatar.playerId, out var actorInfo)) continue;
            if (actorInfo.isBan >= 1 || actorInfo.isDelete >= 1) continue;
            if (actorInfo.avatarClothes == null || actorInfo.avatarClothes.Count == 0) continue;

            var validPlayerId = avatar.playerId;
            if (!_clothesCache.TryGetValue(validPlayerId, out var clothesList))
            {
                clothesList = new List<CharacterData>();
                foreach (var clothes in actorInfo.avatarClothes)
                {
                    var cd = CharacterData.DeserializeObject(clothes.clothesJson);
                    if (cd != null) clothesList.Add(cd);
                }
                _clothesCache[validPlayerId] = clothesList;
            }
            if (clothesList.Count == 0) continue;

            if (i < _wardrobeItems.Count)
                _wardrobeItems[i].SetData(actorInfo);
            _currentActorInfo = actorInfo;
            _currentClothesIndex = 0;
            for (int j = 0; j < _wardrobeItems.Count; j++)
                _wardrobeItems[j].SetClothSelected(j == i ? 0 : -1);
            ShowCharacterData(clothesList[0]);
            UpdateTheatreName();

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ScrollContent);

            if (clothesList.Count > 1)
                _cycleCoroutine = StartCoroutine(CycleClotheCoroutine(clothesList));
            yield break;
        }
    }

    private IEnumerator CycleClotheCoroutine(List<CharacterData> clothesList)
    {
        var wait = new WaitForSeconds(3f);
        int index = 0;
        while (true)
        {
            yield return wait;
            index = (index + 1) % clothesList.Count;
            _characterWrap?.SetCharacterData(clothesList[index]);
            _currentClothesIndex = index;
            UpdateTheatreName();
        }
    }

    private void UpdateTheatreName()
    {
        var actorName = _currentActorInfo?.name ?? string.Empty;
        var clothes = _currentActorInfo?.avatarClothes;
        var clothesName = (clothes != null && _currentClothesIndex < clothes.Count)
            ? clothes[_currentClothesIndex].clothesName ?? string.Empty
            : string.Empty;
        TheatreName.text = string.IsNullOrEmpty(clothesName) ? actorName : $"{actorName} {clothesName}";
    }

    private void ShowCharacterData(CharacterData characterData)
    {
        DestroyCharacter();
        _characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(characterData, CharacterRoot);
        AvatarCameraController.RotateTarget = CharacterRoot;
        AvatarCameraController.SetCameraZoom(ViewType.ZoomWholeBody);
    }

    private void DestroyCharacter()
    {
        _emotePlayCtrl = null;
        if (_characterWrap != null)
        {
            var toDestroy = _characterWrap.CustomAvatar != null ? _characterWrap.CustomAvatar : _characterWrap.Avatar;
            if (toDestroy != null) Destroy(toDestroy);
            _characterWrap = null;
        }
    }

    private void SwitchToActor(int index)
    {
        if (_dataBase?.avatarList == null || index < 0 || index >= _dataBase.avatarList.Count) return;
        _actorIndex = index;
        for (int i = 0; i < _wardrobeItems.Count; i++)
            _wardrobeItems[i].SetSelected(i == _actorIndex);
        var playerId = _dataBase.avatarList[index].playerId;
        if (_actorInfoCache.TryGetValue(playerId, out var cachedInfo))
        {
            _currentActorInfo = cachedInfo;
            _currentClothesIndex = 0;
            UpdateTheatreName();
        }
        LoadActor(_actorIndex);
    }

    private void ShowSpecificCloth(int actorIndex, int clothIndex)
    {
        if (_dataBase?.avatarList == null || actorIndex < 0 || actorIndex >= _dataBase.avatarList.Count) return;

        if (_cycleCoroutine != null) { StopCoroutine(_cycleCoroutine); _cycleCoroutine = null; }

        if (actorIndex != _actorIndex)
        {
            _actorIndex = actorIndex;
            for (int i = 0; i < _wardrobeItems.Count; i++)
                _wardrobeItems[i].SetSelected(i == _actorIndex);
        }

        var playerId = _dataBase.avatarList[actorIndex].playerId;
        if (!_clothesCache.TryGetValue(playerId, out var clothesList) || clothesList.Count == 0)
        {
            LoadActor(actorIndex);
            return;
        }

        int safeIndex = Mathf.Clamp(clothIndex, 0, clothesList.Count - 1);
        if (_actorInfoCache.TryGetValue(playerId, out var actorInfo))
            _currentActorInfo = actorInfo;
        _currentClothesIndex = safeIndex;
        for (int i = 0; i < _wardrobeItems.Count; i++)
            _wardrobeItems[i].SetClothSelected(i == actorIndex ? safeIndex : -1);
        ShowCharacterData(clothesList[safeIndex]);
        UpdateTheatreName();
    }

    private void LeftButtonOnClick()
    {
        if (_dataBase?.avatarList == null || _dataBase.avatarList.Count == 0) return;
        SwitchToActor((_actorIndex - 1 + _dataBase.avatarList.Count) % _dataBase.avatarList.Count);
    }

    private void RightButtonOnClick()
    {
        if (_dataBase?.avatarList == null || _dataBase.avatarList.Count == 0) return;
        SwitchToActor((_actorIndex + 1) % _dataBase.avatarList.Count);
    }

    private void AllOverBtnOnClick()
    {
        bool selectAll = !AllOverSelected.gameObject.activeSelf;
        AllOverSelected.gameObject.SetActive(selectAll);
        foreach (var item in _wardrobeItems)
        {
            if (selectAll) item.SelectAll();
            else item.DeselectAll();
        }
        foreach (Transform child in ActionTsf)
        {
            if (!child.gameObject.activeSelf) continue;
            if (!child.TryGetComponent<TheatreActionItem>(out var actionItem)) continue;
            if (selectAll) actionItem.SelectAction();
            else actionItem.DeselectAction();
        }
        OnWardrobeSelectionChanged();
    }

    private void RefreshBuyBtnText()
    {
        bool hasPurchasable = _wardrobeItems.Any(item => item.HasPurchasableItems());
     //   bool theatreOwned = !string.IsNullOrEmpty(_dataBase?.id) && AssetsDataManager.IsOwned(_dataBase.id);
        BuyText.text = hasPurchasable ? "结算" : "已拥有";

        bool useTicket = CanUseTheatreTicket();
        BuyBtnTicket.gameObject.SetActive(useTicket);
        BuyText.gameObject.SetActive(!useTicket);
    }

    private bool CanUseTheatreTicket()
    {
        if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityTheaterTicket) <= 0)
            return false;
        bool theatreOwned = !string.IsNullOrEmpty(_dataBase?.id) && AssetsDataManager.IsOwned(_dataBase.id);
        if (theatreOwned)
            return false;
        bool hasWardrobeSelection = _wardrobeItems.Any(
            item => item.GetTotalSelectedPrice() > 0 || item.GetActorSelectedPrice() > 0);
        if (hasWardrobeSelection)
            return false;
        long price = _dataBase?.paymentInfo?.price ?? 0;
        if (price > 350)
            return false;
        //if(long.TryParse(PriceNum.text,out long selectPrice) && selectPrice > price )
        //{
        //    return false;
        //}
        return true;
    }

    private void OnWardrobeSelectionChanged()
    {
        bool theatreOwned = !string.IsNullOrEmpty(_dataBase?.id) && AssetsDataManager.IsOwned(_dataBase.id);
        long total = theatreOwned ? 0 : (_dataBase?.paymentInfo?.price ?? 0);
        foreach (var item in _wardrobeItems)
        {
            total += item.GetTotalSelectedPrice();
            total += item.GetActorSelectedPrice();
        }
        total += GetSelectedActionsTotal();
        var currencyType = _dataBase?.paymentInfo?.currencyType ?? CurrencyType.Coin;
        PriceNum.text = ApplyMonthCardDiscount(total, currencyType).ToString();
        RefreshBuyBtnText();
    }

    private long ApplyMonthCardDiscount(long price, CurrencyType currencyType)
    {
        if (currencyType == CurrencyType.PinkCoin && AnniversaryMonthCardMgr.Inst.IsAnyMonthCardActive())
            return (long)Mathf.Ceil(price * AnniversaryMonthCardMgr.Inst.GetDiscountRate());
        return price;
    }

    private void BuyBtnTicketOnClick()
    {
        if (string.IsNullOrEmpty(_dataBase?.id)) return;
        UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
        {
            type = CurrencyType.CommunityTheaterTicket,
            pgcId = _dataBase.id,
            ClaimCallBack = () =>
            {
                foreach (var item in _wardrobeItems)
                    item.RefreshOwnedState();
                RefreshTheatreStatus();
                OnWardrobeSelectionChanged();
            }
        });
    }

    private void BuyBtnOnClick()
    {
        if(BuyBtnTicket.gameObject.activeSelf)
        {
            BuyBtnTicketOnClick();
            return;
        }
        bool theatreOwned = !string.IsNullOrEmpty(_dataBase?.id) && AssetsDataManager.IsOwned(_dataBase.id);
        long totalPrice = theatreOwned ? 0 : (_dataBase?.paymentInfo?.price ?? 0);
        foreach (var item in _wardrobeItems)
        {
            totalPrice += item.GetTotalSelectedPrice();
            totalPrice += item.GetActorSelectedPrice();
        }
        totalPrice += GetSelectedActionsTotal();
        var currencyType = _dataBase?.paymentInfo?.currencyType ?? CurrencyType.Coin;
        totalPrice = ApplyMonthCardDiscount(totalPrice, currencyType);
        long balance = AccountDataManager.Inst.BalanceInfo.GetAccountCount(currencyType);
        if (balance < totalPrice)
        {
            int needNum = (int)(totalPrice - balance);
            switch (currencyType)
            {
                case CurrencyType.Coin:
                {
                    var p = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    if (p != null) p.SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                    break;
                }
                case CurrencyType.Badge:
                {
                    var p = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    if (p != null) p.SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                    break;
                }
                case CurrencyType.PinkCoin:
                {
                    if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                    {
                        var p = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                        if (p != null) p.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                    }
                    break;
                }
                case CurrencyType.Gem:
                    UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                    break;
            }
            return;
        }

        var ugcIds = new List<string>(_wardrobeItems.Count * 4 + 1);
        if (!string.IsNullOrEmpty(_dataBase?.id) && !theatreOwned)
            ugcIds.Add(_dataBase.id);
        foreach (var item in _wardrobeItems)
            item.GetSelectedUgcIds(ugcIds);
        foreach (Transform child in ActionTsf)
        {
            if (!child.gameObject.activeSelf) continue;
            if (child.TryGetComponent<TheatreActionItem>(out var actionItem) && actionItem.IsSelected)
                ugcIds.Add(actionItem.Id);
        }
        ugcIds.RemoveAll(id => AssetsDataManager.IsOwned(id));

        if(ugcIds.Count == 0)
        {
            Debug.LogWarning("没有选中任何需要购买的物品");
            return;
        }

        bool theatrePurchased = ugcIds.Contains(_dataBase?.id);
        var rewards = new List<CommonRewardItemData>();
        if (theatrePurchased && _dataBase != null)
            rewards.Add(new CommonRewardItemData { UgcCover = _dataBase.cover, rewardName = _dataBase.name, RewardAmount = 1, rewardType = (int)BUDRewardType.RewardUgcResource });
        foreach (var item in _wardrobeItems)
            item.CollectRewardItems(rewards);
        var req = new Req { ugcIds = ugcIds };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.BuyUgcPay, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            response =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                AllOverSelected.gameObject.SetActive(false);
                foreach (var item in _wardrobeItems)
                    item.RefreshOwnedState();
                RefreshTheatreStatus();
                OnWardrobeSelectionChanged();
                var rsp = JsonConvert.DeserializeObject<ServerBagUpdateDataRsp>(response);
                var rewardPanel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                rewardPanel.ShowRewards(rewards);
                AssetsDataManager.BuyAction?.Invoke(rsp?.popupType ?? 0);
            },
            _ => AccountDataManager.Inst.BalanceInfo.Refresh());
    }
    private void OnActionJumpBtnClicked(string emoteId, bool isPgc)
    {
        PlayEmoteOnCharacter(emoteId, isPgc);
        if (!isPgc) OnWardrobeSelectionChanged();
    }

    private void PlayEmoteOnCharacter(string emoteId, bool isPgc)
    {
        if (_characterWrap == null || _characterWrap.Avatar == null) return;
        if (!_characterWrap.Avatar.TryGetComponent<PlayerAnimationCtrl>(out var animCtrl)) return;
        if (isPgc)
        {
            animCtrl.PlaySingleEmoteForUICharacter(emoteId);
            return;
        }
        if (_emotePlayCtrl == null)
        {
            _emotePlayCtrl = new CabinPgcUgcPlayController();
            _emotePlayCtrl.Init(animCtrl, _characterWrap, null, AvatarCameraController);
        }
        _emotePlayCtrl.InitData(new pEmoteData { emoteId = emoteId, ugcData = new UgcIdleData { id = emoteId } });
        _emotePlayCtrl.PlayAnim();
    }

    private long GetSelectedActionsTotal()
    {
        long total = 0;
        foreach (Transform child in ActionTsf)
        {
            if (!child.gameObject.activeSelf) continue;
            if (child.TryGetComponent<TheatreActionItem>(out var item) && item.IsSelected
                && !AssetsDataManager.IsOwned(item.Id))
                total += item.Price;
        }
        return total;
    }

    private void NavigateToItem(string pgcId)
    {
        CloseSelf();
        var fittingRoom = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        if (fittingRoom != null) fittingRoom.JumpToPgcItem(pgcId);
    }

    public class Req
    {
        public List<string> ugcIds;
    }
}
