
using System;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using GameData;
using GameData.Base;
using Message;
using GameData.BaseInfo;
using GameData.Manager;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Basic.Utils;
using GameData.UGCData;
using UI.Base;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class TheatreInfoPanel : BasePanel<TheatreInfoPanel>
{
    [Header("按钮")]
    [SerializeField] private Button newGameBtn;
    [SerializeField] private Button continueBtn;
    [SerializeField] private Button tryPlayBtn;
    [SerializeField] private Button backBtn;

    [Header("角色")]
    [SerializeField] private Transform avatarRoot;
    [SerializeField] private GameObject avatarItemPrefab;

    [Header("剧本")]
    [SerializeField] private Text theatreTitle;
    [SerializeField] private Text theatreDescription;
    [SerializeField] private RemoteImageBehaviour cover;

    [Header("商城数据")]
    [SerializeField] private Text dialogueText;
    [SerializeField] private Text salesText;
    [SerializeField] private Text designCodeText;
    [SerializeField] private Button designCodeCopyBtn;
    [SerializeField] private Button deleteBtn; //只有自己的才显示
    [SerializeField] private Button likeBtn; //点赞按钮，暂不显示
    [SerializeField] private Image likeIcon; //点赞图标，暂不显示
    [SerializeField] private Sprite likeSprite; //点赞图标，暂不显示
    [SerializeField] private Sprite unlikeSprite; //未点赞图标，暂不显示
    [SerializeField] private Text likeCountText; //点赞数文本，暂不显示
    [SerializeField] private Button reportBtn;

    [Header("购买按钮")]
    [SerializeField] private Button purchaseBtn;
    [SerializeField] private Text priceText;
    [SerializeField] private Image priceIcon;
    

    private OCTheatreInfo theatreInfo;
    private TheatreEnterType enterType = TheatreEnterType.None;
    private bool _isLiked;

    public override void OnCreate()
    {
        base.OnCreate();
        newGameBtn.onClick.AddListener(OnNewGameBtnClick);
        continueBtn.onClick.AddListener(OnContinueBtnClick);
        backBtn.onClick.AddListener(OnBackBtnClick);
        tryPlayBtn?.onClick.AddListener(OnTryPlayBtnClick);
        purchaseBtn?.onClick.AddListener(OnPurchaseBtnClick);

        continueBtn.gameObject.SetActive(false);

        designCodeCopyBtn?.onClick.AddListener(OnCopyDesignCodeClick);
        deleteBtn?.onClick.AddListener(OnDeleteBtnClick);
        likeBtn?.onClick.AddListener(OnLikeBtnClick);
        reportBtn?.onClick.AddListener(OnReportBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if(args.Length > 0 && args[0] is OCTheatreInfo theatreInfo){
            this.theatreInfo = theatreInfo;
            Init();
        }
        if(args.Length > 1 && args[1] is int enterType){
            this.enterType = (TheatreEnterType)enterType;
        }
    }

    private void Init()
    {
        if(theatreInfo == null){
            LoggerUtils.LogError("TheatreInfo is null");
            return;
        }
        theatreTitle.text = theatreInfo.name;
        theatreDescription.text = theatreInfo.desc;
        var rawCoverUrl = theatreInfo.cover;
        var cleanCoverUrl = StripImageQueryString(rawCoverUrl);
        if (cleanCoverUrl != rawCoverUrl)
            cover.Load(cleanCoverUrl, true, (_, success) => { if (!success) cover.Load(rawCoverUrl); });
        else
            cover.Load(rawCoverUrl);

        bool isFirstTime = OCTheatreDataManager.Inst.IsFirstTime(theatreInfo.id);
        continueBtn.gameObject.SetActive(!isFirstTime);
        LoadAvatarList();
        InitDesignCode();
        InitDeleteBtn();
        InitLikeBtn();
        InitTryPlayBtn();
        InitReportBtn();

        if (dialogueText != null)
            dialogueText.text = theatreInfo.textCount.ToString();

        if (salesText != null)
            salesText.gameObject.SetActive(false);
        FetchSalesCount(theatreInfo.id);
    }

    private void FetchSalesCount(string theatreId)
    {
        if (salesText == null && likeBtn == null && likeCountText == null) return;
        var jb = new JObject { ["id"] = theatreId };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.TheatreInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                if (theatreInfo?.id != theatreId) return;
                var rsp = JsonConvert.DeserializeObject<DetailRsp>(content);
                if (rsp?.interactInfo == null) return;
                if (salesText != null)
                {
                    salesText.text = GameUtils.ToBudCommonNumString(rsp.interactInfo.consumeAmount);
                    salesText.gameObject.SetActive(true);
                }
                if (likeBtn != null)
                {
                    _isLiked = rsp.interactInfo.liked == 1;
                    RefreshLikeIcon();
                }
                if (likeCountText != null)
                {
                    likeCountText.text = GameUtils.ToBudCommonNumString(rsp.interactInfo.likeAmount);
                    likeCountText.gameObject.SetActive(true);
                }
            },
            error => LoggerUtils.LogError($"TheatreInfoPanel fetch sales failed: {error}"));
    }

    private void InitLikeBtn()
    {
        if (likeBtn == null) return;
        _isLiked = false;
        likeBtn.gameObject.SetActive(true);
        RefreshLikeIcon();
    }

    private void RefreshLikeIcon()
    {
        if (likeIcon == null) return;
        likeIcon.sprite = _isLiked ? likeSprite : unlikeSprite;
    }

    private void OnLikeBtnClick()
    {
        if (theatreInfo == null) return;
        var likeType = _isLiked ? UGCCommonReq.LikeType.UnLike : UGCCommonReq.LikeType.Like;
        UGCCommonReq.Inst.UGCLikeReq(theatreInfo.id, likeType, success =>
        {
            if (!success) return;
            _isLiked = !_isLiked;
            RefreshLikeIcon();
        });
    }

    private void InitDesignCode()
    {
        bool hasCode = !string.IsNullOrEmpty(theatreInfo?.designCode);
        if (designCodeText != null)
        {
            designCodeText.gameObject.SetActive(hasCode);
            if (hasCode) designCodeText.text = theatreInfo.designCode;
        }
        designCodeCopyBtn?.gameObject.SetActive(hasCode);
    }

    private void InitDeleteBtn()
    {
        bool isCreator = !string.IsNullOrEmpty(theatreInfo?.creator)
                         && theatreInfo.creator == AccountDataManager.Inst.Uid;
        deleteBtn?.gameObject.SetActive(isCreator);
    }

    private void InitTryPlayBtn()
    {
        if (tryPlayBtn == null) return;
        bool isCreator = !string.IsNullOrEmpty(theatreInfo?.creator)
                         && theatreInfo.creator == AccountDataManager.Inst.Uid;
        bool isOwned = !string.IsNullOrEmpty(theatreInfo?.id) && AssetsDataManager.IsOwned(theatreInfo.id);
        bool showTryPlay = !isCreator && !isOwned;
        tryPlayBtn.gameObject.SetActive(showTryPlay);
        newGameBtn.gameObject.SetActive(!showTryPlay);
        continueBtn.gameObject.SetActive(!showTryPlay && !OCTheatreDataManager.Inst.IsFirstTime(theatreInfo.id));

        var showBuy = showTryPlay && theatreInfo?.paymentInfo != null && theatreInfo.paymentInfo.price > 0;
        if (purchaseBtn != null)
        {
            purchaseBtn.gameObject.SetActive(showBuy);
            if (showBuy)
            {
                bool useTicket = TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityTheaterTicket) > 0
                                 && theatreInfo.paymentInfo.price <= 350;
                if (useTicket)
                {
                    if (priceText != null) priceText.text = "1";
                    if (priceIcon != null)
                        priceIcon.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.CommunityTheaterTicket, purchaseBtn.gameObject);
                }
                else
                {
                    if (priceText != null)
                        priceText.text = CalcDisplayPrice(theatreInfo.paymentInfo).ToString();
                    if (priceIcon != null)
                        priceIcon.sprite = PgcUtils.LoadCurrencyIcon(theatreInfo.paymentInfo.currencyType, purchaseBtn.gameObject);
                }
            }
        }
    }

    private void InitReportBtn()
    {
        if (reportBtn == null) return;
        bool isCreator = !string.IsNullOrEmpty(theatreInfo?.creator)
                         && theatreInfo.creator == AccountDataManager.Inst.Uid;
        reportBtn.gameObject.SetActive(!isCreator);
    }

    private void LoadAvatarList()
    {
        if(theatreInfo.avatarList == null || theatreInfo.avatarList.Count == 0) return;
        foreach(var avatar in theatreInfo.avatarList){
            var avatarItem = Instantiate(avatarItemPrefab, avatarRoot);
            var item = avatarItem.GetComponent<TheatreAvatarItem>();
            item.SetData(avatar);
            item.onActorClick = OnAvatarItemClick;
        }
    }

    private void OnAvatarItemClick(OCTheatreAvatarOc avatarOc)
    {
        if (string.IsNullOrEmpty(avatarOc?.playerId)) return;
        var jb = new JObject { ["id"] = avatarOc.playerId };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.ActorInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                var rsp = JsonConvert.DeserializeObject<DetailRsp>(content);
                if (rsp?.actorInfo == null) return;
                UIManager.Inst.OpenPanel(PanelId.ActorCardInfoPanel, rsp.actorInfo);
            },
            error => LoggerUtils.LogError($"TheatreInfoPanel fetch actor failed: {error}"));
    }

    private void OnNewGameBtnClick()
    {
        OCTheatreDataManager.Inst.ClearProgress(theatreInfo.id);
        UIManager.Inst.OpenPanel(PanelId.TheatreGamePanel, theatreInfo, (int)enterType);
    }

    private void OnContinueBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.TheatreGamePanel, theatreInfo, (int)enterType);
    }

    private void OnTryPlayBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.TheatreGamePanel, theatreInfo, (int)enterType, true);
    }

    private void OnPurchaseBtnClick()
    {
        if (theatreInfo?.paymentInfo == null) return;

        bool useTicket = TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityTheaterTicket) > 0
                         && theatreInfo.paymentInfo.price <= 350;
        if (useTicket)
        {
            UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
            {
                type = CurrencyType.CommunityTheaterTicket,
                pgcId = theatreInfo.id,
                ClaimCallBack = () =>
                {
                    var successPanel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                    successPanel?.InitData(theatreInfo, "购买成功！");
                    MessageHelper.Broadcast(MessageName.OnBuyUgcItemSuccess, theatreInfo.id);
                    purchaseBtn?.gameObject.SetActive(false);
                }
            });
            return;
        }

        var currencyType = theatreInfo.paymentInfo.currencyType;
        var price = CalcDisplayPrice(theatreInfo.paymentInfo);

        var confirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        confirmPanel.SetText("购买确认", $"确认花费 {price} 购买该剧本吗？", "确认购买", "取消");
        confirmPanel.SetOnClickAction(() =>
        {
            AssetsDataManager.BuyUgc(theatreInfo.id, currencyType, price, (success, reason, needNum) =>
            {
                if (!success)
                {
                    if (reason.Equals("余额不足"))
                    {
                        switch (currencyType)
                        {
                            case CurrencyType.Coin:
                                var p1 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                p1.SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                                break;
                            case CurrencyType.Badge:
                                var p2 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                p2.SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                                break;
                            case CurrencyType.PinkCoin:
                                if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                                {
                                    var p3 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                    p3.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
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
                    var successPanel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                    successPanel?.InitData(theatreInfo, "购买成功！");
                    MessageHelper.Broadcast(MessageName.OnBuyUgcItemSuccess, theatreInfo.id);
                    purchaseBtn?.gameObject.SetActive(false);
                }
            });
        });
    }

    private void OnReportBtnClick()
    {
        if (theatreInfo == null) return;
        var req = new ErrReportReq
        {
            Uid = AccountDataManager.Inst.Uid,
            bizId = theatreInfo.id,
            scenesType = (int)ErrReportSceneType.OC,
        };
        UIManager.Inst.OpenPanel(PanelId.ReportAssetPanel, req);
    }

    private void OnBackBtnClick()
    {
        CloseSelf();
    }

    private void OnCopyDesignCodeClick()
    {
        if (theatreInfo == null || string.IsNullOrEmpty(theatreInfo.designCode)) return;
        GUIUtility.systemCopyBuffer = theatreInfo.designCode;
        TipPanel.ShowToast("已复制，去分享给好友吧");
    }

    private void OnDeleteBtnClick()
    {
        var confirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        confirmPanel.SetLocalText("确认删除", "确定要删除该剧场吗？删除后无法恢复", "删除", "取消");
        confirmPanel.SetOnClickAction(ConfirmDelete, null);
    }

    private void ConfirmDelete()
    {
        if (theatreInfo == null) return;
        var req = new SetTheatreInfoReq
        {
            theaterInfo = theatreInfo,
            setType = (int)SetType.Delete
        };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.TheatreSet,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            OnDeleteSuccess,
            OnDeleteFail);
    }

    private void OnDeleteSuccess(string msg)
    {
        TipPanel.ShowToast("删除成功");
        MessageHelper.Broadcast(MessageName.OnAssetDelete);
        CloseSelf();
    }

    private void OnDeleteFail(string error)
    {
        LoggerUtils.LogError($"Delete theatre failed: {error}");
        TipPanel.ShowToast("删除失败，请重试");
    }

    public override void OnHidden()
    {
        base.OnHidden();
        MessageHelper.Broadcast(MessageName.TheatreInfoPanelClose);
    }

    private static int CalcDisplayPrice(PaymentInfo paymentInfo)
    {
        var price = paymentInfo.price;
        if (paymentInfo.currencyType == CurrencyType.PinkCoin && AnniversaryMonthCardMgr.Inst.IsAnyMonthCardActive())
            price = (int)Mathf.Ceil(price * AnniversaryMonthCardMgr.Inst.GetDiscountRate());
        return price;
    }

    private static string StripImageQueryString(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        int q = url.IndexOf('?');
        if (q < 0) return url;
        var beforeQuery = url.Substring(0, q);
        if (beforeQuery.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) ||
            beforeQuery.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase) ||
            beforeQuery.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase) ||
            beforeQuery.EndsWith(".webp", System.StringComparison.OrdinalIgnoreCase))
            return beforeQuery;
        return url;
    }
}
