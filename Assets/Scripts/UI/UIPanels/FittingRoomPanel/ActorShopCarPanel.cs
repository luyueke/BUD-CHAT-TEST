using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using Game.Database;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class ActorShopCarPanel : BasePanel<ActorShopCarPanel>
{
    [SerializeField] private GameObject TransBg;
    [SerializeField] private Transform CharacterRoot;
    [SerializeField] private AvatarCameraController AvatarCameraController;
    [SerializeField] private CButton BackButton;
    [SerializeField] private CButton LeftButton;
    [SerializeField] private CButton RightButton;
    [SerializeField] private Text TheatreName;
    [SerializeField] private CButton ActorCardBtn;
    [SerializeField] private RawImage ActorCardImg;
    [SerializeField] private Text ActorPrice;
    [SerializeField] private Text ActorStatus;
    [SerializeField] private List<WardrobeItem> WardrobeList;
    [SerializeField] private CButton AllOverBtn;
    [SerializeField] private Transform AllOverSelected;
    [SerializeField] private Text PriceNum;
    [SerializeField] private CButton BuyBtn;
    [SerializeField] private Text BuyText;

    private OCTheatreAvatarInfo dataBase;
    private CharacterWrap _characterWrap;
    private int _NowSelectIndex;

    public override void OnCreate()
    {
        base.OnCreate();
        BackButton.onClick.AddListener(CloseSelf);
        LeftButton.onClick.AddListener(LeftBtnOnClick);
        RightButton.onClick.AddListener(RightBtnOnClick);
        ActorCardBtn.onClick.AddListener(ActorCardBtnOnClick);
        AllOverBtn.onClick.AddListener(AllOverBtnOnClick);
        BuyBtn.onClick.AddListener(BuyBtnOnClick);
    }


    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if(args.Length <= 0)
        {
            Debug.LogError("传入数据为空！");
            CloseSelf();
            return;
        }
        dataBase = (OCTheatreAvatarInfo)args[0];
        if(dataBase == null)
        {
            CloseSelf();
            return;
        }
        // 隐藏 FittingRoomPanel 的角色模型，避免两个模型同时显示
        var fittingRoom = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        if (fittingRoom != null) fittingRoom.characterRoot.gameObject.SetActive(false);
        InitBGUI();
        //ResetDiscountSelect();
        ShowActorClothInfo();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // 恢复 FittingRoomPanel 的角色模型
        var fittingRoom = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        if (fittingRoom != null) fittingRoom.characterRoot.gameObject.SetActive(true);
    }

    private void InitBGUI()
    {
        if (TransBg == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";

        var item = TransBg.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "actor_bg_icon1", "actor_bg_icon2", "actor_bg_icon3", "actor_bg_icon4"
        });

        item.gameObject.SetActive(true);

    }

    private void ShowActorClothInfo()
    {
        if(dataBase.avatarClothes == null || dataBase.avatarClothes.Count <= 0)
        {
            return;
        }
        foreach(var item in WardrobeList)
        {
            item.gameObject.SetActive(false);
        }
        _NowSelectIndex = 0;
        for(int i = 0; i < dataBase.avatarClothes.Count; i++)
        {
            WardrobeList[i].gameObject.SetActive(true);
            WardrobeList[i].SetData(dataBase.avatarClothes[i]);
            WardrobeList[i].SetSelected(i == _NowSelectIndex);
            WardrobeList[i].OnSelectionChanged = OnWardrobeSelectionChanged;
            int capturedIdx = i;
            WardrobeList[i].OnItemClick = () => SwitchToCloth(capturedIdx);
            WardrobeList[i].OnDirectBuyOfficialItem = NavigateToItem;
        }
        ShowCharacter(dataBase.avatarClothes[0].clothesJson);

        if (!string.IsNullOrEmpty(dataBase.cover))
        {
            Game.Utils.GameSimpleImageDownloader.Instance.Enqueue(new Game.Utils.GameSimpleImageDownloader.Request
            {
                url = dataBase.cover,
                onDone = result => 
                {
                    ActorCardImg.texture = result.CreateTextureFromReceivedData();
                    ActorCardImg.enabled = true;
                }
            });
        }
        ActorPrice.text = dataBase.paymentInfo?.price.ToString() ?? "0";
        RefreshActorStatus();
        OnWardrobeSelectionChanged();
        UpdateTheatreName();
    }

    private void RefreshActorStatus()
    {
        bool owned = !string.IsNullOrEmpty(dataBase?.id) && AssetsDataManager.IsOwned(dataBase.id);
        ActorPrice.gameObject.SetActive(!owned);
        ActorStatus.gameObject.SetActive(owned);
    }

    private void ShowCharacter(string clothesJson)
    {
        if (string.IsNullOrEmpty(clothesJson)) return;
        if (_characterWrap != null && _characterWrap.Avatar != null)
        {
            Destroy(_characterWrap.Avatar);
            _characterWrap = null;
        }
        var characterData = CharacterData.DeserializeObject(clothesJson);
        if (characterData == null) return;
        _characterWrap = AvatarController.Inst.CreateUIAvatar(characterData);
        _characterWrap.SetParent(CharacterRoot, true);
        AvatarCameraController.RotateTarget = CharacterRoot;
        AvatarCameraController.SetCameraZoom(ViewType.ZoomWholeBody);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_characterWrap != null && _characterWrap.Avatar != null)
        {
            Destroy(_characterWrap.Avatar);
            _characterWrap = null;
        }
    }

    private void UpdateTheatreName()
    {
        var actorName = dataBase?.name ?? string.Empty;
        var clothesName = (dataBase?.avatarClothes != null && _NowSelectIndex < dataBase.avatarClothes.Count)
            ? dataBase.avatarClothes[_NowSelectIndex].clothesName ?? string.Empty
            : string.Empty;
        TheatreName.text = string.IsNullOrEmpty(clothesName) ? actorName : $"{actorName} {clothesName}";
    }

    private void SwitchToCloth(int index)
    {
        _NowSelectIndex = index;
        for (int i = 0; i < WardrobeList.Count; i++)
        {
            if (WardrobeList[i].gameObject.activeSelf)
                WardrobeList[i].SetSelected(i == _NowSelectIndex);
        }
        ShowCharacter(dataBase.avatarClothes[_NowSelectIndex].clothesJson);
        UpdateTheatreName();
    }

    private void LeftBtnOnClick()
    {
        if (dataBase?.avatarClothes == null || dataBase.avatarClothes.Count == 0) return;
        int count = dataBase.avatarClothes.Count;
        SwitchToCloth((_NowSelectIndex - 1 + count) % count);
    }

    private void RightBtnOnClick()
    {
        if (dataBase?.avatarClothes == null || dataBase.avatarClothes.Count == 0) return;
        int count = dataBase.avatarClothes.Count;
        SwitchToCloth((_NowSelectIndex + 1) % count);
    }

    private void ActorCardBtnOnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.ActorCardInfoPanel, dataBase);
    }

    private void AllOverBtnOnClick()
    {
        bool selectAll = !AllOverSelected.gameObject.activeSelf;
        AllOverSelected.gameObject.SetActive(selectAll);
        foreach (var item in WardrobeList)
        {
            if (!item.gameObject.activeSelf) continue;
            if (selectAll) item.SelectAll();
            else item.DeselectAll();
        }
        OnWardrobeSelectionChanged();
    }

    private void BuyBtnOnClick()
    {
        bool actorOwned = !string.IsNullOrEmpty(dataBase?.id) && AssetsDataManager.IsOwned(dataBase.id);
        long totalPrice = actorOwned ? 0 : (dataBase?.paymentInfo?.price ?? 0);
        foreach (var item in WardrobeList)
        {
            if (!item.gameObject.activeSelf) continue;
            foreach (var part in item.GetSelectedItems())
                totalPrice += item.GetItemPrice(part);
        }
        var currencyType = dataBase?.paymentInfo?.currencyType ?? CurrencyType.Coin;
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

        var ugcIds = new List<string>(WardrobeList.Count * 4 + 1);
        if (!string.IsNullOrEmpty(dataBase?.id) && !AssetsDataManager.IsOwned(dataBase.id))
            ugcIds.Add(dataBase.id);
        foreach (var item in WardrobeList)
        {
            if (!item.gameObject.activeSelf) continue;
            foreach (var part in item.GetSelectedItems())
                if (!string.IsNullOrEmpty(part.UId))
                    ugcIds.Add(part.UId);
        }
        ugcIds.RemoveAll(id => AssetsDataManager.IsOwned(id));

        if(ugcIds.Count == 0)
        {
            Debug.LogWarning("没有选中任何需要购买的物品");
            return;
        }

        bool actorPurchased = ugcIds.Contains(dataBase?.id);
        var rewards = new List<CommonRewardItemData>();
        if (actorPurchased && dataBase != null)
            rewards.Add(new CommonRewardItemData { UgcCover = dataBase.cover, rewardName = dataBase.name, RewardAmount = 1, rewardType = (int)BUDRewardType.RewardUgcResource });
        foreach (var wardrobeItem in WardrobeList)
        {
            if (!wardrobeItem.gameObject.activeSelf) continue;
            foreach (var part in wardrobeItem.GetSelectedItems())
            {
                var rewardItem = new CommonRewardItemData { RewardAmount = 1, rewardType = (int)BUDRewardType.RewardUgcResource };
                if (!string.IsNullOrEmpty(part.UId))
                {
                    string uid = part.UId;
                    AssetsDataManager.GetUgcInfo(uid, serverData =>
                    {
                        rewardItem.UgcCover = serverData?.UgcInfo?.cover ?? string.Empty;
                    });
                }
                else
                {
                    rewardItem.pgcId = part.Id;
                }
                rewards.Add(rewardItem);
            }
        }
        var req = new Req { ugcIds = ugcIds };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.BuyUgcPay, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            response =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                AllOverSelected.gameObject.SetActive(false);
                foreach (var item in WardrobeList)
                    if (item.gameObject.activeSelf) item.RefreshOwnedState();
                RefreshActorStatus();
                var rsp = JsonConvert.DeserializeObject<ServerBagUpdateDataRsp>(response);
                var rewardPanel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                rewardPanel.ShowRewards(rewards);
                AssetsDataManager.BuyAction?.Invoke(rsp?.popupType ?? 0);
            },
            _ => AccountDataManager.Inst.BalanceInfo.Refresh());
    }

    private void RefreshBuyBtnText()
    {
        bool actorOwned = !string.IsNullOrEmpty(dataBase?.id) && AssetsDataManager.IsOwned(dataBase.id);
        bool hasClothingToBuy = WardrobeList.Any(item =>
            item.gameObject.activeSelf && item.HasPurchasableItems());
        BuyText.text = (actorOwned && !hasClothingToBuy) ? "已拥有" : "结算";
    }

    private void OnWardrobeSelectionChanged()
    {
        bool actorOwned = !string.IsNullOrEmpty(dataBase?.id) && AssetsDataManager.IsOwned(dataBase.id);
        long totalPrice = actorOwned ? 0 : (dataBase?.paymentInfo?.price ?? 0);
        foreach (var item in WardrobeList)
        {
            if (!item.gameObject.activeSelf) continue;
            foreach (var part in item.GetSelectedItems())
                totalPrice += item.GetItemPrice(part);
        }
        var currencyType = dataBase?.paymentInfo?.currencyType ?? CurrencyType.Coin;
        PriceNum.text = ApplyMonthCardDiscount(totalPrice, currencyType).ToString();
        RefreshBuyBtnText();
    }

    private long ApplyMonthCardDiscount(long price, CurrencyType currencyType)
    {
        if (currencyType == CurrencyType.PinkCoin && AnniversaryMonthCardMgr.Inst.IsAnyMonthCardActive())
            return (long)Mathf.Ceil(price * AnniversaryMonthCardMgr.Inst.GetDiscountRate());
        return price;
    }

    private void NavigateToItem(string pgcId)
    {
        CloseSelf();
        var fittingRoom = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        if (fittingRoom != null) fittingRoom.JumpToPgcItem(pgcId);
    }

    private class Req { public List<string> ugcIds; }

}
