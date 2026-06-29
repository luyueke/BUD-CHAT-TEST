using System;
using Game.Store;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseButton : CommonUIWidget
{
    public CButton Btn_Buy;
    public LoadingButton BtnLoading;
    public Text Txt_Price;
    public GameObject Go_BuyRoot;
    public GameObject Go_OwnedRoot;
    public Image Go_CurrencyIcon;
    public Text Txt_OriginPrice;
    public GameObject OriginPrice_Tips;
    public GameObject DiscountCard_Tips;

    [SerializeField] private GameObject monthCardTipGo;
    [SerializeField] private Text txt_monthCardDiscount;

    private UgcBaseInfo _ugcInfo;
    private string _ugcId;
    private PaymentInfo _paymentInfo;
    private bool _isOwned;
    private SkinInfo _skinInfo;
    private Action onBuyUpdate;
    private Action onBuySuccess;
    int originPrice = 0;
    private void Awake()
    {
        Btn_Buy.onClick.AddListener(OnBuyBtnClick);
        if (monthCardTipGo != null)
        {
            Image image = monthCardTipGo.GetComponent<Image>();
            if (image != null)
            {
                string spriteatlasPath = "Assets/Loadable/UI/UIPanel/FittingRoomPanel/FittingRoomPanel.spriteatlas";
                Sprite sp1 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "monthcardTip", gameObject);
                if (sp1 != null)
                {
                    image.sprite = sp1;
                }
            }
        }
    }

    /// <summary>
    /// 购买所需参数 args[0]UgcId, args[1] consumed 消费状态
    /// </summary>
    /// <param name="args"></param>
    public override void SetData(params object[] args)
    {
        base.SetData(args);
        if (args.Length >= 3)
        {
            _ugcInfo = (UgcBaseInfo)args[0];
            _ugcId = _ugcInfo.id;
            if(args[1] == null)
                _isOwned = false;
            else
                _isOwned = (int)args[1] == 1;
            _paymentInfo = (PaymentInfo)args[2];
        }
        if (args.Length >= 4)
        {
            _skinInfo = (SkinInfo)args[3];
        }
        RefreshData();
    }

    public void ShowOriginPrice(PaymentInfo origin)
    {
        Txt_OriginPrice.gameObject.SetActive(true);
        Txt_OriginPrice.text = origin.price.ToString();
        originPrice = origin.price;
        OriginPrice_Tips?.SetActive(true);
    }

    public void ShowOriginAndNowPrice(int originPri, float nowPri)
    {
        Debug.Log("PurchaseButton ShowOriginAndNowPrice originPri=" + originPri + " nowPri=" + nowPri);
        Txt_OriginPrice.text = originPri.ToString();
        this.originPrice = originPri;
        Txt_Price.text = nowPri.ToString();
        Txt_OriginPrice.gameObject.SetActive(originPri != nowPri);
        OriginPrice_Tips?.SetActive(true);
    }


    public void SetBuyAction(Action onSucc = null)
    {
        this.onBuySuccess = onSucc;
    }

    public void SetBuyUpdate(Action buyUpdate)
    {
        this.onBuyUpdate = buyUpdate;
    }

    private void OnBuyBtnClick()
    {
        if (_isOwned)
            return;
        if (_ugcInfo == null)
        {
            return;
        }
        var updateState = (ForceUpdate)_ugcInfo.forceUpdate;
        if (updateState != ForceUpdate.Default)
        {
            UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, updateState);
            return;
        }

        onBuyUpdate?.Invoke();
        BtnLoading.ShowLoading();
        BuyCurrencyItem();
    }

    private void BuyCurrencyItem()
    {
        var price = _paymentInfo != null ? _paymentInfo.price : 0;
        CurrencyType currencyType = _paymentInfo != null ? (CurrencyType)_paymentInfo.currencyType : CurrencyType.Gem;
        if (_ugcInfo is AINpcInfo && LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice == 1)
        {
            price = Mathf.FloorToInt(price * 0.5f);
        }
        else
        {
            if (_paymentInfo != null && _paymentInfo.currencyType == CurrencyType.PinkCoin && AnniversaryMonthCardMgr.Inst.IsAnyMonthCardActive())
            {
                price = (int)(Mathf.Ceil((price * AnniversaryMonthCardMgr.Inst.GetDiscountRate())));
            }
        }

        BuyUgcItem(_ugcInfo, currencyType, price);
    }
    bool CheckedTickUser(string pgcid, int subType, UgcBaseInfo ugcInfo, int price)
    {
        float priceNum = originPrice > price ? originPrice : price;
        switch (subType)
        {
            case 0:
            case 1008://姿势和乐谱不能用卷
            case 1014://演员不能用卷
                return false;
            case 1007: //动作卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityAnimationTicket) <= 0
                    || priceNum > 200) return false;
                UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
                {
                    type = CurrencyType.CommunityAnimationTicket,
                    pgcId = pgcid,
                    ClaimCallBack = () =>
                    {
                        var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                        panel?.InitData(ugcInfo, "购买成功！");
                        OnBuySuccess();
                    }
                });
                BtnLoading.HideLoading();
                return true;
            case 24: //乐器卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityInstrumentTicket) <= 0
                    || priceNum > 200) return false;
                UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
                {
                    type = CurrencyType.CommunityInstrumentTicket,
                    pgcId = pgcid,
                    ClaimCallBack = () =>
                    {
                        var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                        panel?.InitData(ugcInfo, "购买成功！");
                        OnBuySuccess();
                    }
                });
                BtnLoading.HideLoading();
                return true;
            case 1011: //载具卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityVehicleTicket) <= 0
                    || priceNum > 200) return false;
                UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
                {
                    type = CurrencyType.CommunityVehicleTicket,
                    pgcId = pgcid,
                    ClaimCallBack = () =>
                    {
                        var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                        panel?.InitData(ugcInfo, "购买成功！");
                        OnBuySuccess();
                    }
                });
                BtnLoading.HideLoading();
                return true;
            case 1015: //载具卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityTheaterTicket) <= 0
                    || priceNum > 350) return false;
                UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
                {
                    type = CurrencyType.CommunityTheaterTicket,
                    pgcId = pgcid,
                    ClaimCallBack = () =>
                    {
                        var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                        panel?.InitData(ugcInfo, "购买成功！");
                        OnBuySuccess();
                    }
                });
                BtnLoading.HideLoading();
                return true;
            default: //其余都算皮肤卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunitySkinTicket) <= 0
                    || priceNum > 60) return false;
                UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
                {
                    type = CurrencyType.CommunitySkinTicket,
                    pgcId = pgcid,
                    ClaimCallBack = () =>
                    {
                        var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                        panel?.InitData(ugcInfo, "购买成功！");
                        OnBuySuccess();
                    }
                });
                BtnLoading.HideLoading();
                return true;
        }
    }

    void BuyUgcItem(UgcBaseInfo ugcInfo, CurrencyType currencyType, int price)
    {
        if (_skinInfo != null)
            if (CheckedTickUser(ugcInfo.id, _skinInfo.subType, ugcInfo, price))
            {
                return;
            }

        // 捕获 buyUgcId 和其他参数
        AssetsDataManager.BuyUgc(ugcInfo.id, currencyType, price, (success, reason, needNum) =>
        {
            if (!success)
            {
                if (reason.Equals("余额不足"))
                {
                    switch (currencyType)
                    {
                        case CurrencyType.Coin:
                            ExchangeCoinPanel panel1 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                            panel1.SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                            break;
                        case CurrencyType.Badge:
                            ExchangeCoinPanel panel2 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                            panel2.SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                            break;
                        case CurrencyType.PinkCoin:
                            if(ExchangeCoinPanel.JudgePinkCoin(needNum))
                            {
                                ExchangeCoinPanel panel3 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                panel3.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                            }
            
                            break;
                        case CurrencyType.Gem:
                            UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                            break;
                    }
                }
                OnBuyFail(reason);
            }
            else
            {
                var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                panel?.InitData(ugcInfo, "购买成功！");
                MessageHelper.Broadcast(MessageName.OnBuyUgcItemSuccess, ugcInfo.id);

                if (ugcInfo is AINpcInfo && LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice == 1)
                {
                    Txt_OriginPrice.gameObject.SetActive(false);
                    LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice = 0;
                }

                OnBuySuccess();
            }
        });
    }


    private void OnBuySuccess(string content = "")
    {
        _isOwned = true;
        RefreshData();
        onBuySuccess?.Invoke();
    }

    private void OnBuyFail(string error)
    {
        LoggerUtils.Log($"购买UGC商品失败 [{_ugcId}]:" + error);
        BtnLoading.HideLoading();
        // onBuyFail?.Invoke();
    }

    private void RefreshData()
    {
        Go_BuyRoot.SetActive(!_isOwned);
        Go_OwnedRoot.SetActive(_isOwned);
        monthCardTipGo?.SetActive(false);
        OriginPrice_Tips?.SetActive(false);
        DiscountCard_Tips?.SetActive(false);

        var price = 0;
        if (_paymentInfo != null)
        {
            price = _paymentInfo.price;
        }

        if (price == 0)
        {
            Go_CurrencyIcon.gameObject.SetActive(false);
            Txt_Price.SetLocalText("免费");
        }
        else
        {
            Go_CurrencyIcon.gameObject.SetActive(true);
            Go_CurrencyIcon.sprite = PgcUtils.LoadCurrencyIcon(_paymentInfo.currencyType, gameObject);
            Txt_Price.text = price.ToString();

            if (DiscountCardUtils.IsSupportDiscountCard(_ugcInfo) && DiscountCardUtils.IsOwnedDiscountCard())
            {
                Txt_OriginPrice.gameObject.SetActive(true);
                Txt_OriginPrice.text = price.ToString();
                Txt_Price.text = Mathf.FloorToInt(price * DiscountCardUtils.GetDiscount()).ToString();
                DiscountCard_Tips.SetActive(true);
                DiscountCard_Tips.GetComponentInChildren<Text>(true).SetLocalText("{0}折生效中", Mathf.RoundToInt(DiscountCardUtils.GetDiscount() * 10));
            }

            if (_ugcInfo is AINpcInfo && LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice == 1)
            {
                Txt_OriginPrice.gameObject.SetActive(true);
                Txt_OriginPrice.text = price.ToString();
                Txt_Price.text = Mathf.FloorToInt(price * 0.5f).ToString();
                DiscountCard_Tips.SetActive(true);
                DiscountCard_Tips.GetComponentInChildren<Text>(true).SetLocalText("首次购买半价");
            }
            else
            {
                if (_paymentInfo != null)
                {
                    if (_paymentInfo.currencyType == CurrencyType.PinkCoin)
                    {
                        if (AnniversaryMonthCardMgr.Inst.IsAnyMonthCardActive())//银卡或金卡用户
                        {
                            if (_skinInfo == null || !ChekcUseTicket(_skinInfo.subType, _paymentInfo.price))
                            {
                                monthCardTipGo?.SetActive(true);
                                txt_monthCardDiscount.text = $"月卡{AnniversaryMonthCardMgr.Inst.GetDiscountRate() * 10}折权益";
                                var nowPrice = (_paymentInfo.price * AnniversaryMonthCardMgr.Inst.GetDiscountRate());
                                nowPrice = Mathf.Round(nowPrice * 10f) / 10f;
                                if (Mathf.Approximately(nowPrice, Mathf.Floor(nowPrice)))
                                {
                                    nowPrice = Mathf.Floor(nowPrice);
                                }
                                ShowOriginAndNowPrice(_paymentInfo.price, nowPrice);
                            }
                        }
                    }
                }
            }
        }
      
        if (_skinInfo != null)
        {
            int priceNum = originPrice > price ? originPrice : price;
            CheckedTickUser(_skinInfo.subType, priceNum);
        }

        BtnLoading?.HideLoading();
    }
    bool CheckedTickUser(int subType, int price)
    {

        switch (subType)
        {
            case 0:
            case 1008://姿势和乐谱不能用卷
                return false;
            case 1007: //动作卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityAnimationTicket) <= 0
                    || price > 200) return false;
                Go_CurrencyIcon.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.CommunityAnimationTicket, Go_CurrencyIcon.gameObject);
                Txt_OriginPrice.gameObject.SetActive(false);
                DiscountCard_Tips.gameObject.SetActive(false);
                Txt_Price.text = "1";
                return true;
            case 24: //乐器卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityInstrumentTicket) <= 0
                    || price > 200) return false;
                Go_CurrencyIcon.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.CommunityInstrumentTicket, Go_CurrencyIcon.gameObject);
                Txt_OriginPrice.gameObject.SetActive(false);
                DiscountCard_Tips.gameObject.SetActive(false);
                Txt_Price.text = "1";
                return true;
            case 1011: //载具卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityVehicleTicket) <= 0
                    || price > 200) return false;
                Go_CurrencyIcon.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.CommunityVehicleTicket, Go_CurrencyIcon.gameObject);
                Txt_OriginPrice.gameObject.SetActive(false);
                DiscountCard_Tips.gameObject.SetActive(false);
                Txt_Price.text = "1";
                return true;
            case 1015: //剧场卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityTheaterTicket) <= 0
                    || price > 350) return false;
                Go_CurrencyIcon.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.CommunityTheaterTicket, Go_CurrencyIcon.gameObject);
                Txt_OriginPrice.gameObject.SetActive(false);
                DiscountCard_Tips.gameObject.SetActive(false);
                Txt_Price.text = "1";
                return true;
            default: //其余都算皮肤卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunitySkinTicket) <= 0
                    || price > 60) return false;
                Go_CurrencyIcon.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.CommunitySkinTicket, Go_CurrencyIcon.gameObject);
                Txt_OriginPrice.gameObject.SetActive(false);
                DiscountCard_Tips.gameObject.SetActive(false);
                Txt_Price.text = "1";
                return true;
        }
    }

    public bool ChekcUseTicket(int subType, int price)
    {
        switch (subType)
        {
            case 0:
            case 1008://姿势和乐谱不能用卷
                return false;
            case 1007: //动作卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityAnimationTicket) <= 0
                    || price > 200) return false;
                return true;
            case 24: //乐器卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityInstrumentTicket) <= 0
                    || price > 200) return false;
                return true;
            case 1011: //乐器卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityVehicleTicket) <= 0
                    || price > 200) return false;
                return true;
            case 1015: //乐器卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityTheaterTicket) <= 0
                    || price > 350) return false;
                return true;
            default: //其余都算皮肤卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunitySkinTicket) <= 0
                    || price > 60) return false;
                return true;
        }
    }
    public void SetOwned()
    {
        var isOwned = true;
        Go_BuyRoot.SetActive(!isOwned);
        Go_OwnedRoot.SetActive(isOwned);
    }
}
