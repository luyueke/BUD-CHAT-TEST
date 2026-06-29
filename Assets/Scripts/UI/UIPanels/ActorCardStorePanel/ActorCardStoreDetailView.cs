using System;
using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.RechargePanel;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

// 演员卡商城详情视图：从 FittingRoomPanel 的 Ugc+演员卡 流程迁出。
// 完整复刻原商城选中演员卡后的交互（FittingRoomPanel TryOn 的 ResourceType.AvatarCard 分支）：
//   面板内自建预览角色穿演员服装、左右箭头切换多套服装(SwitchActorCloth)、
//   演员卡信息按钮(ActorCardInfoPanel)、点赞(UgcLikeButton)；
//   购买为直购，价格/折扣展示与购买流程与 OperationView 的 BuyBtn（FittingRoomPanel.Buy）一致。
public class ActorCardStoreDetailView : MonoBehaviour
{
    [Header("商品信息")]
    [SerializeField] private UserInfoView UserInfoView;
    [SerializeField] private CButton Btn_UserHead;
    [SerializeField] private CText Txt_ItemName;

    // 价格/折扣展示复用 OperationView 同款 BuyButton 组件（月卡折扣、折扣卡、原价划线与原商城一致）。
    // 注意全限定名：全局命名空间下另有一个同名 BuyButton（UIWidgets/CommonUIWidget），不是这里要的
    [SerializeField] private UI.UIPanels.FittingRoom.BuyButton buyButton;
    [SerializeField] private GameObject Go_BuyRoot;
    [SerializeField] private GameObject Go_OwnedRoot;

    [Header("演员卡交互")]
    [SerializeField] private CButton Btn_ActorCardInfo;
    [SerializeField] private CButton Btn_ArrowLeft;
    [SerializeField] private CButton Btn_ArrowRight;
    [SerializeField] private CButton SearchButton;
    [SerializeField] private SearchView SearchView;

    


    [Header("预览角色")]
    [SerializeField] private Transform characterRoot;
    [SerializeField] private UIDragUtil dragUtil;
    [SerializeField] private UgcLikeButton UgcLikeButton;// 预览角色的点赞按钮，数据绑定到当前演员卡

    private RecommendItemData curData;
    private OCTheatreAvatarInfo curActorInfo;
    private List<CharacterData> _actorCharacterDataList = new List<CharacterData>();
    private int _actorClothesIndex = 0;

    public Action<RecommendItemData> DidPayAssetAction;
    // 搜索结果（列表数据 + 上拉加载下一页的 Action），由 ActorCardStorePanel 订阅后灌给列表
    public Action<List<RecommendItemData>, Action> SearchResultAction;
    // 取消搜索，由 ActorCardStorePanel 订阅后恢复当前栏目列表
    public Action SearchCancelAction;
    private string _searchKey;

    private CharacterWrap characterWrap;

    public void Awake()
    {
        // 与原商城 OperationView 一致：BuyButton 组件只负责价格展示，点击由同节点上的 Button 接收
        // （buyComp = BuyButton.GetComponent<BuyButton>() 的反向取法）
        if (buyButton != null)
        {
            var buyClickBtn = buyButton.GetComponent<Button>();
            if (buyClickBtn != null) buyClickBtn.onClick.AddListener(OnBuyBtnClick);
            else LoggerUtils.LogError("ActorCardStoreDetailView: buyButton 节点上缺少 Button 组件，购买无法点击");
        }
        if (Btn_ActorCardInfo != null) Btn_ActorCardInfo.onClick.AddListener(OnActorCardInfoBtnClick);
        if (Btn_ArrowLeft != null) Btn_ArrowLeft.onClick.AddListener(() => SwitchActorCloth(-1));
        if (Btn_ArrowRight != null) Btn_ArrowRight.onClick.AddListener(() => SwitchActorCloth(1));
        // 点赞：与原商城一致（OperationView 的 LikeUgc 流程），点击即发起点赞/取消点赞请求
        if (UgcLikeButton != null) UgcLikeButton.ClickAction = () => UgcLikeButton.Like();
        if (SearchButton != null) SearchButton.onClick.AddListener(OpenSearchView);
    }

    #region 搜索（参考原商城 UGCScene 的 OnSearchClick/OnSearchAction 流程）

    public void OpenSearchView()
    {
        if (SearchView == null)
        {
            return;
        }
        SearchView.searchHandle = "搜索作者ID/演员名";
        SearchView.isFittingRoom = true;
        SearchView.gameObject.SetActive(true);
        SearchView.SetSearchAction(OnSearchWord, OnSearchClear, OnSearchCancel);
        SearchView.SetInitSearchAction(OpenSearchView);
        SearchView.Show();
    }

    private void OnSearchWord(string str)
    {
        SearchLogicMgr.Inst.AddSearchHistoryWord(str);
        _searchKey = str;

        var dataHandler = AssetsDataManager.GetData<AvatarUgcSceneHandler>();
        var classType = UniqueType.Get(ResourceType.AvatarCard, (int)UgcTheatreSubType.AvatarCard);
        Action nextAction = null;
        nextAction = dataHandler.SearchGoodsData(classType, str, (searchKey, isEnd, goodsDatas) =>
        {
            if (this == null || searchKey != _searchKey)
            {
                return;
            }
            SearchView.isSearching = false;
            SearchView.HideSearchDiscoverAndHistoryView();

            // GoodsData 还原回列表用的 RecommendItemData（Assets[0].UgcInfo 即原始数据）
            var list = goodsDatas == null
                ? new List<RecommendItemData>()
                : goodsDatas
                    .Where(g => g?.Assets != null && g.Assets.Count > 0 && g.Assets[0].UgcInfo != null)
                    .Select(g => g.Assets[0].UgcInfo)
                    .ToList();

            if (isEnd && list.Count == 0)
            {
                TipPanel.ShowToast("没有找到相关内容");
            }
            SearchResultAction?.Invoke(list, nextAction);
        });
        // 注：SearchPanel.SearchType 暂无演员卡类型，故不走原商城的 7 位设计码精确搜索分支
    }

    private void OnSearchClear()
    {
        _searchKey = null;
        SearchResultAction?.Invoke(new List<RecommendItemData>(), null);
    }

    private void OnSearchCancel()
    {
        _searchKey = null;
        SearchCancelAction?.Invoke();
    }

    #endregion

    public void RefreshUIByData(RecommendItemData data)
    {
        this.curData = data;
        RefreshUIInfo(data);
    }

    private void RefreshUIInfo(RecommendItemData data)
    {
        var actorInfo = data.UgcInfo as OCTheatreAvatarInfo;
        if (actorInfo == null)
        {
            return;
        }
        curActorInfo = actorInfo;

        var ugcId = actorInfo.id;
        AccountUserInfo accountUserInfo = data?.creatorInfo;
        UserInfoView.IsOpenProfilePanel = false;
        UserInfoView.SetData(accountUserInfo);
        Btn_UserHead.onClick.RemoveAllListeners();
        Btn_UserHead.onClick.AddListener(() =>
        {
            if (this == null)
            {
                return;
            }
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Actor, ugcId);
        });

        UserInfoView.gameObject.SetActive(true);
        Txt_ItemName.gameObject.SetActive(true);

        // 点赞按钮绑定当前演员卡（UgcLikeButton 点赞成功后会直接翻转 interactInfo.liked，先确保不为 null）
        if (UgcLikeButton != null)
        {
            if (data.interactInfo == null)
            {
                data.interactInfo = new GameData.Base.BaseInteractInfo();
            }
            UgcLikeButton.gameObject.SetActive(true);
            UgcLikeButton.SetBundleData(data);
        }

        RefreshOwnedStatus();

        // 解析演员服装并预览第一套（与 FittingRoomPanel.SetCurrentActorInfo 一致）
        _actorClothesIndex = 0;
        _actorCharacterDataList = actorInfo.avatarClothes == null
            ? new List<CharacterData>()
            : actorInfo.avatarClothes
                .Select(c => CharacterData.DeserializeObject(c.clothesJson))
                .Where(d => d != null)
                .ToList();

        bool hasClothes = _actorCharacterDataList.Count >= 1;
        Btn_ArrowLeft.gameObject.SetActive(hasClothes);
        Btn_ArrowRight.gameObject.SetActive(hasClothes);
        Btn_ActorCardInfo.gameObject.SetActive(hasClothes);
        UpdateItemName();

        if (hasClothes)
        {
            ShowOrUpdateCharacter(_actorCharacterDataList[0]);
        }
    }

    // 名字展示与购物车一致：演员名 + 当前服装名
    private void UpdateItemName()
    {
        var actorName = curActorInfo?.name ?? string.Empty;
        string clothesName = string.Empty;
        if (curActorInfo?.avatarClothes != null && _actorClothesIndex < curActorInfo.avatarClothes.Count)
        {
            clothesName = curActorInfo.avatarClothes[_actorClothesIndex].clothesName ?? string.Empty;
        }
        Txt_ItemName.text = string.IsNullOrEmpty(clothesName) ? actorName : $"{actorName} {clothesName}";
    }

    private void RefreshOwnedStatus()
    {
        bool owned = (curData?.interactInfo?.consumed == 1) ||
                     (!string.IsNullOrEmpty(curActorInfo?.id) && AssetsDataManager.IsOwned(curActorInfo.id)) ||
                     curData?.creatorInfo?.uid == AccountDataManager.Inst.Uid;
        if (Go_BuyRoot != null)
        {
            Go_BuyRoot.SetActive(!owned);
        }
        if (Go_OwnedRoot != null)
        {
            Go_OwnedRoot.SetActive(owned);
        }
        if (buyButton != null)
        {
            // 已拥有隐藏购买按钮
            buyButton.gameObject.SetActive(!owned);
            if (!owned)
            {
                var goodsData = BuildGoodsData();
                if (goodsData != null)
                {
                    buyButton.SetTarget(goodsData);
                }
            }
        }
    }

    // 照 FittingRoom 原商城的方式构造演员卡 GoodsData
    // （AvatarUgcSceneHandler 的 UgcType.ActorCard 分支 + CreateUgcActorAssetsData）：
    // GoodsData.Update() 会按原商城同一套逻辑算出折后价 Price（粉币月卡折扣）与原价 OriginalPrice，
    // BuyButton.SetTarget 再按其展示折扣卡/月卡角标等，与 OperationView 的 BuyBtn 完全一致。
    // InventoryData 的 set 是 internal（仅 Game 程序集可写）这里不赋值：未拥有时 UpdatePrice 对
    // InventoryData==null 同样按全价累计，价格不受影响；已拥有判断由 RefreshOwnedStatus 兜底。
    private GoodsData BuildGoodsData()
    {
        if (curData == null || curActorInfo == null)
        {
            return null;
        }

        var assetsData = new UgcActorAssetsData
        {
            Id = curActorInfo.id,
            Name = curActorInfo.name,
            ResourceType = ResourceType.AvatarCard,
            UgcInfo = curData,
            Value = curActorInfo.paymentInfo != null
                ? new CurrencyData
                {
                    CurrencyType = curActorInfo.paymentInfo.currencyType,
                    Value = curActorInfo.paymentInfo.price
                }
                : new CurrencyData
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                }
        };

        var goodsData = new GoodsData
        {
            Id = curActorInfo.id,
            Name = curActorInfo.name,
            GoodsType = GoodsType.SingleUgc,
            subType = 1000 + (int)UgcType.ActorCard,
            Assets = new List<AssetsData> { assetsData },
        };
        goodsData.Update();
        return goodsData;
    }

    #region 预览角色

    private void ShowOrUpdateCharacter(CharacterData characterData)
    {
        if (characterData == null)
        {
            return;
        }
        if (characterWrap == null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatar(characterData);
            characterWrap.SetParent(characterRoot, true);
            if (dragUtil != null)
            {
                dragUtil.RotateTarget = characterWrap.Avatar.transform;
            }
        }
        else
        {
            characterWrap.Avatar.SetActive(true);
            characterWrap.SetCharacterData(characterData);
        }
    }

    private void SwitchActorCloth(int delta)
    {
        if (_actorCharacterDataList == null || _actorCharacterDataList.Count == 0) return;
        _actorClothesIndex = (_actorClothesIndex + delta + _actorCharacterDataList.Count) % _actorCharacterDataList.Count;
        ShowOrUpdateCharacter(_actorCharacterDataList[_actorClothesIndex]);
        UpdateItemName();
    }

    // 供面板在购物车等覆盖面板打开期间隐藏/恢复预览角色
    public void SetPreviewCharacterVisible(bool visible)
    {
        if (characterWrap != null && characterWrap.Avatar != null)
        {
            characterWrap.Avatar.SetActive(visible);
        }
    }

    #endregion

    private void OnActorCardInfoBtnClick()
    {
        if (curActorInfo == null) return;
        UIManager.Inst.OpenPanel(PanelId.ActorCardInfoPanel, curActorInfo);
    }

    // 购买：与 FittingRoomPanel.Buy（OperationView 的 BuyBtn）一致的直购流程：
    // 折后价由 GoodsData.Update() 算出（粉币月卡折扣），再叠折扣卡，走 AssetsDataManager.BuyGoods；
    // 余额不足按币种跳兑换/充值面板，成功弹 BuySuccessTipPanel。
    // 演员卡不适用 Shape 主题折扣（Id 非数字 PgcId）和社区票券（subType=1014 演员不能用券），故省略这两个分支。
    private bool _isBuying = false;
    private void OnBuyBtnClick()
    {
        if (curActorInfo == null) return;
        if (_isBuying) return;

        var data = BuildGoodsData();
        if (data == null || data.Price == null) return;

        var tmpGoodsData = new GoodsData()
        {
            ButtonType = data.ButtonType,
            Id = data.Id,
            GoodsType = data.GoodsType,
            subType = data.subType,
            Price = new CurrencyData()
            {
                CurrencyType = data.Price.CurrencyType,
                Value = data.Price.Value,
            }
        };

        // 当前拥有打折卡且当前商品支持打折卡
        if (DiscountCardUtils.IsOwnedDiscountCard() && DiscountCardUtils.IsSupportDiscountCard(data))
        {
            tmpGoodsData.Price.Value = Mathf.RoundToInt(tmpGoodsData.Price.Value * DiscountCardUtils.GetDiscount());
        }

        _isBuying = true;
        AssetsDataManager.BuyGoods(tmpGoodsData, (success, reason, needNum) =>
        {
            if (this == null)
            {
                return;
            }
            _isBuying = false;
            if (!success)
            {
                if (reason.Equals("余额不足"))
                {
                    switch (data.Price.CurrencyType)
                    {
                        case CurrencyType.Coin:
                            ExchangeCoinPanel exchangeCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                            exchangeCoinPanel.SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                            break;
                        case CurrencyType.Badge:
                            ExchangeCoinPanel badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                            badgePanel.SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                            break;
                        case CurrencyType.PinkCoin:
                            if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                            {
                                ExchangeCoinPanel pinkCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                pinkCoinPanel.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                            }
                            break;
                        case CurrencyType.Gem:
                            UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                            break;
                    }
                }
            }
            else
            {
                var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                if (panel != null)
                {
                    panel.InitData(data, "购买成功！");
                }
                if (curData != null)
                {
                    if (curData.interactInfo == null)
                    {
                        curData.interactInfo = new GameData.Base.BaseInteractInfo();
                    }
                    curData.interactInfo.consumed = 1;
                    DidPayAssetAction?.Invoke(curData);
                }
                RefreshOwnedStatus();
            }
            data.IsPayingRequest = false;
        });
    }

    private void OnDestroy()
    {
        if (characterWrap != null && characterWrap.Avatar != null)
        {
            Destroy(characterWrap.Avatar);
            characterWrap = null;
        }
    }
}
