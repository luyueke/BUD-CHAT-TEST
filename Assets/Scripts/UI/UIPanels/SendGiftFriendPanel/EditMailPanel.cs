using System;
using System.Collections.Generic;
using System.Globalization;
using Basic.Extensions;
using BUD.MailBox;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Product;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class EditMailPanel : BasePanel<EditMailPanel>
{
    public Button backButton;
    public Text receiverName;
    public Text assetName;
    public Image assetsIcon;
    public Text titleText;
    public Text titleLengthLimit;
    public Text contentText;
    public Text contentLengthLimit;
    public Text priceText;
    public CButton titleBtn;
    public CButton contentBtn;
    public CButton sendGiftBtn;
    public Sprite budSprite;
    public RemoteImageBehaviour remoteAssetsIcon;
    public Image priceIcon;

    public RemoteImageBehaviour RemoteImage => remoteAssetsIcon;

    private GoodsData mData;
    private MyFriendsInfo myFriendsInfo;
    private MailInfo mailInfo;
    private string url;
    private int currentTyep; //0好友 1关注
    private string searchContent = "";
    private int inputType = 0; //0标题 1正文
    private int giftType = 0;
    private string title;
    private string content;

    private string budOrderId;
    private IapTrackData _iapTrackData = new IapTrackData();

    private Action<bool> sentMailCallback;


    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length >= 3)
        {
            myFriendsInfo = (MyFriendsInfo)args[0];
            mData = (GoodsData)args[1];
            mailInfo = (MailInfo)args[2];
            giftType = (int)args[3];
            SetupUI();
        }
    }

    public void SetSentMailCallback(Action<bool> sentMailCallback)
    {
        this.sentMailCallback = sentMailCallback;
    }

    public override void OnCreate()
    {
        base.OnCreate();

        backButton.onClick.AddListener(() => { CloseSelf(); });

        titleBtn.onClick.AddListener(() => { OnTitleBtnClick(); });

        contentBtn.onClick.AddListener(() => { OnContentBtnClick(); });

        sendGiftBtn.onClick.AddListener(() =>
        {
            if (mData != null)
            {
                Debug.LogError(mData.GoodsType);
                switch (mData.GoodsType)
                {
                    case GoodsType.ToolProduct:
                        {
                            if (mData.GiftType == GiftType.NewYearLimitedPackage || mData.GiftType == GiftType.LaborDay)
                            {
                                SendGiftInvoke(mData.Price.CurrencyType, (int)(Mathf.Ceil(mData.Price.Value)));
                            }
                            else
                            {
                                SendIapGift(mData);
                            }
                        }
                        break;
                    default:
                        SendGiftInvoke(mData.Price.CurrencyType, (int)(Mathf.Ceil(mData.Price.Value)));
                        break;
                }
            }
            else if (mailInfo != null)
            {
                SendGiftFromMailbox(mailInfo);
            }

        });

        UpdateTitleLength(0);
        UpdateContentLength(0);
    }

    private void OnTitleBtnClick()
    {
        inputType = 0;
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("我在碧优蒂为你准备了一份礼物！"),
            inputMode = 2,
            maxLength = 24,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            defaultText = "",
            returnKeyType = (int)ReturnType.Done
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, KeyboardReturn);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(keyBoardInfo));
    }

    private void OnContentBtnClick()
    {
        inputType = 1;
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("希望你喜欢呀，快来看看吧！"),
            inputMode = 2,
            maxLength = 250,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            defaultText = "",
            returnKeyType = (int)ReturnType.Done
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, KeyboardReturn);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(keyBoardInfo));
    }

    private void SendIapGift(GoodsData goodsData)
    {
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, goodsData.Price.Value.ToString());
            panel.SetCallback(paymentType =>
            {
                BuyIapInvoke(goodsData, null, paymentType);
            });
            return;
        }

        BuyIapInvoke(goodsData, null, ConfirmPaymentPanel.PaymentType.Default);
    }

    private void BuyIapInvoke(GoodsData goodsData, MailInfo mailInfo, ConfirmPaymentPanel.PaymentType paymentType)
    {
        ChannelProductInfo channelProductInfo = new ChannelProductInfo();

        if (goodsData != null)
        {
            channelProductInfo.productId = goodsData.Id;
            channelProductInfo.productName = goodsData.Name;
            channelProductInfo.productDesc = goodsData.Name;
            channelProductInfo.price = goodsData.Price.Value.ToString();
        }

        if (mailInfo != null)
        {
            if (mailInfo.attachments.IsNullOrEmpty())
            {
                LoggerUtils.LogError("attachments是空的");
                return;
            }

            var mailAttachmentsInfo = mailInfo.attachments[0];
            channelProductInfo.productId = mailAttachmentsInfo.rewardId;
            channelProductInfo.productName = mailAttachmentsInfo.rewardName;
            channelProductInfo.productDesc = mailAttachmentsInfo.rewardName;
            channelProductInfo.price = mailAttachmentsInfo.giftPrice.ToString();
        }

        var productId = channelProductInfo.productId;

        if (string.IsNullOrEmpty(productId))
        {
            return;
        }

        _iapTrackData.price = channelProductInfo.price;
        _iapTrackData.item = channelProductInfo.productName;
        _iapTrackData.channel = "";

        ShowPurchaseLoading();
        GiftOrderData giftOrderData = new GiftOrderData();
        if (goodsData != null)
        {
            giftOrderData.giftType = giftType;
            giftOrderData.toUid = myFriendsInfo.userInfo.uid;
            giftOrderData.seasonPassType = SeasonPassDataManager.Inst.GetSeasonPassName(giftType);
        }

        if (mailInfo != null)
        {
            giftOrderData.mailId = mailInfo.mailId;
        }
        IAPDataManager.Inst.GetProductOrderId(productId, giftOrderData, (b, info) =>
        {
            budOrderId = info?.budOrderId;
            if (!b || string.IsNullOrEmpty(budOrderId))
            {
                HidePurchaseLoading();
                return;
            }

            channelProductInfo.extension = JsonConvert.SerializeObject(info);
            channelProductInfo.cpOrderId = budOrderId;
            channelProductInfo.paymentType = (int)paymentType;
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(channelProductInfo));
        });
    }


    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        PurchaseProcessingPanel processingPanel =
            UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }

    private void HidePurchaseLoading()
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            UIManager.Inst.ClosePanel(panel);
        }
    }

    private void StartBillingFlow(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        BillingResultResponse billingResultResponse = JsonConvert.DeserializeObject<BillingResultResponse>(message);
        if (billingResultResponse.resultType == (int)BillingResultType.UserPaySuccess)
        {
            StartLooping();
            TrackEvent(billingResultResponse);
        }
        else if (billingResultResponse.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            PurchaseStatusManager.Inst.StopLoop();
        }
    }

    private void StartLooping()
    {
        if (string.IsNullOrEmpty(budOrderId))
        {
            return;
        }

        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            panel.StartTimer(60, () =>
            {
                PurchaseStatusManager.Inst.StopLoop();
                MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
            });
        }

        PurchaseStatusManager.Inst.StartLoop(budOrderId, (orderResult, productGemInfo) =>
        {
            if (this == null)
            {
                return;
            }

            if (!orderResult)
            {
                return;
            }

            sentMailCallback?.Invoke(true);
            AccountDataManager.Inst.BalanceInfo.Refresh();
            HidePurchaseLoading();
            TipPanel.ShowToast("赠送成功");
            CloseSelf();
            MessageHelper.Broadcast(MessageName.SendGiftSuccess);
        });
    }

    private void TrackEvent(BillingResultResponse billingResultResponse)
    {
        Dictionary<string, object> trackData = new Dictionary<string, object>();
        float result;
        if (float.TryParse(_iapTrackData.price, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            trackData.Add("priceNum", result);
        }

        trackData.Add("item", _iapTrackData.item);
        bool isUploadData = !AnalyticsManager.Inst.ContainOrderID(budOrderId);
        if (isUploadData)
        {
            trackData.Add("budOrderId", budOrderId ?? "");
            trackData.Add("method", "EditMailPanel");
            AnalyticsManager.Inst.Track(AnalyticsEventName.TOP_UP_SUCCESS, trackData);
        }
    }

    private void SendGiftInvoke(CurrencyType currencyType, int price)
    {
        var count = AccountDataManager.Inst.BalanceInfo.GetAccountCount((CurrencyType)currencyType);

        if (count < price)
        {
            // 余额不足
            BalanceNotEnough(price - count);
            return;
        }

        string toUid = myFriendsInfo.userInfo.uid;
        string giftId = mData.Id;
        int giftType = this.giftType;
        string mailTitle = title;
        string mailContent = content;

        JObject req = new JObject()
        {
            ["toUid"] = toUid,
            ["giftId"] = giftId,
            ["giftType"] = giftType,
            ["mailTitle"] = mailTitle,
            ["mailContent"] = mailContent
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GiveGift, HttpMethod.POST, JsonConvert.SerializeObject(req),
            (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                TipPanel.ShowToast("赠送成功");
                this.sentMailCallback?.Invoke(true);
                CloseSelf();
                MessageHelper.Broadcast(MessageName.SendGiftSuccess);
            },
            (error) => { Debug.LogError("Error: " + error); });
    }

    private void MailIapGift(MailInfo mailInfo)
    {
        if (this.mailInfo.attachments.IsNullOrEmpty())
        {
            LoggerUtils.LogError("attachments是空的");
            return;
        }
        int isIapProduct = this.mailInfo.attachments[0].isIapProduct;
        int price = this.mailInfo.attachments[0].giftPrice;
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, price.ToString());
            panel.SetCallback(paymentType =>
            {
                BuyIapInvoke(null, mailInfo, paymentType);
            });
            return;
        }

        BuyIapInvoke(null, mailInfo, ConfirmPaymentPanel.PaymentType.Default);
    }


    private void SendGiftFromMailbox(MailInfo mailInfo)
    {
        if (this.mailInfo.attachments.IsNullOrEmpty())
        {
            LoggerUtils.LogError("attachments是空的");
            return;
        }
        int isIapProduct = this.mailInfo.attachments[0].isIapProduct;
        int price = this.mailInfo.attachments[0].giftPrice;
        if (isIapProduct == 1)
        {
            MailIapGift(mailInfo);
        }
        else
        {
            var count = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);

            if (count < price)
            {
                // 余额不足
                BalanceNotEnough(price - count);
                return;
            }


            string mailId = mailInfo.mailId;
            int buttonType = (int)GiftButtonType.Send;
            JObject extraObj = new JObject()
            {
                ["mailTitle"] = title,
                ["mailContent"] = content
            };
            string extraData = JsonConvert.SerializeObject(extraObj);

            JObject req = new JObject()
            {
                ["mailId"] = mailId,
                ["buttonType"] = buttonType,
                ["extraData"] = extraData
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.MailClick, HttpMethod.POST, JsonConvert.SerializeObject(req),
                (_) =>
                {
                    TipPanel.ShowToast("赠送成功");

                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    this.sentMailCallback?.Invoke(true);
                    CloseSelf();
                },
                (error) =>
                {
                });
        }
    }

    private void BalanceNotEnough(int needNum)
    {
        switch (mData.Price.CurrencyType)
        {
            case CurrencyType.Coin:
                ExchangeCoinPanel exchangeCoinPanel =
                    UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                exchangeCoinPanel.SetData(CurrencyType.Coin);
                break;
            case CurrencyType.Badge:
                ExchangeCoinPanel badgePanel =
                    UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                badgePanel.SetData(CurrencyType.Badge);
                break;
            case CurrencyType.PinkCoin:
                ExchangeCoinPanel.OpenPenlByPinkCoin(needNum);
                //ExchangeCoinPanel pinkCoinPanel =
                //    UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                //pinkCoinPanel.SetData(CurrencyType.PinkCoin);
                break;
            case CurrencyType.Gem:
                UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                break;
            case CurrencyType.GreenCoin:
                UIManager.Inst.OpenPanel(PanelId.TipPanel, "当前创作者币余额不足");
                break;
            default:
                UIManager.Inst.OpenPanel(PanelId.TipPanel, "余额不足");
                break;
        }
    }

    private void SetupUI()
    {
        if (myFriendsInfo != null && mData != null)
        {


            assetName.text = mData.Name;
            receiverName.text = "收件人：" + myFriendsInfo.userInfo.nickname;

            switch (mData.GoodsType)
            {
                case GoodsType.SinglePgc:
                case GoodsType.SingleUgc:
                    SingleItemUpdate(mData.Assets);
                    break;
                case GoodsType.BundlePgc:
                    BundlePgcItemUpdate(mData.Id);
                    break;
                case GoodsType.BundleUgc:
                    BundleItemUpdate();
                    break;
                case GoodsType.ToolProduct:
                    ToolsItemUpdate();
                    break;
            }

            if (mData.GiftType == GiftType.PremiumSeasonPass || mData.GiftType == GiftType.DeluxeSeasonPass || mData.GiftType == GiftType.AdvancedSeasonPassTier || mData.GiftType == GiftType.MonthlyVip)
            {
                priceIcon.gameObject.SetActive(false);
                priceText.text = "\u00a5" + mData.Price.Value;
            }
            else
            {
                priceIcon.gameObject.SetActive(true);
                priceIcon.sprite =
                    PgcUtils.LoadCurrencyIcon((CurrencyType)mData.Price.CurrencyType, gameObject);
                priceText.text = mData.IsOwned ? mData.OriginalPrice.Value.ToString() : mData.Price.Value.ToString();
            }
        }
        else if (mailInfo != null)
        {
            if (mailInfo.attachments != null && mailInfo.attachments.Count > 0 && mailInfo.attachments[0] != null)
            {


                var attachment = mailInfo.attachments[0];
                assetName.text = attachment.rewardName;
                receiverName.text = "收件人：" + mailInfo.sender.name;
                priceText.text = attachment.giftPrice.ToString();

                priceIcon.gameObject.SetActive(true);
                priceIcon.sprite =
                    PgcUtils.LoadCurrencyIcon((CurrencyType)attachment.giftCurrencyType, gameObject);

                if (attachment.isIapProduct == 1)
                {
                    var spriteName = GetSpriteName(attachment.id);
                    assetsIcon.gameObject.SetActive(true);
                    var spriteatlasPath = "Assets/Loadable/UI/UIPanel/FittingRoomPanel/FittingRoomPanel.spriteatlas";
                    var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, spriteName, gameObject);
                    assetsIcon.sprite = sprite;

                    priceIcon.gameObject.SetActive(false);
                    priceText.text = "\u00a5" + attachment.giftPrice.ToString();
                }
                else
                {
                    if (attachment.rewardType == BUDRewardType.RewardPgcResource)
                    {
                        assetsIcon.sprite = PgcUtils.GetIconSpriteByPgcId(attachment.rewardId, assetsIcon.gameObject);

                    }
                    else if (attachment.rewardType == BUDRewardType.RewardGiftUgc)
                    {

                        remoteAssetsIcon.Load(attachment.cover, onCompleted: (bool fromCache, bool success) =>
                        {
                            assetsIcon.gameObject.SetActive(false);
                            remoteAssetsIcon.gameObject.SetActive(true);
                        });
                    }
                    else if (attachment.rewardType == BUDRewardType.RewardPgcBundle)
                    {
                        assetsIcon.sprite = PgcUtils.LoadBundleIcon(attachment.rewardId, assetsIcon.gameObject);
                    }
                    else
                    {
                        Sprite rewardSprite = GetSprite(attachment.id, attachment.rewardType);
                        assetsIcon.sprite = rewardSprite;
                    }
                }
            }
        }
    }

    private void BundleItemUpdate()
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (mData.UgcBundleInfo != null && mData.UgcBundleInfo.skinInfo != null)
        {
            RefreshCover(mData.UgcBundleInfo.skinInfo.cover);
        }
    }

    private void BundlePgcItemUpdate(string bundleId)
    {
        url = null;
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        var sprite = PgcUtils.LoadBundleIcon(bundleId, gameObject);
        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }
    private Sprite GetSprite(string productId, BUDRewardType rewardType)
    {
        var spriteName = "icon_premium_pass";
        if (productId.Contains("giftgaojipass"))
        {
            spriteName = "icon_premium_pass";
        }
        else if (productId.Contains("gifthaohuapass"))
        {
            spriteName = "icon_deluxe_pass";
        }
        else if (productId.Contains("gifthaohuauppass"))
        {
            spriteName = "icon_deluxe_pass_upgrade";
        }
        else if (productId.Contains("giftvip"))
        {
            spriteName = "icon_vip_month";
        }
        else if (productId.Contains("newYearLimitedPack1"))
        {
            spriteName = "icon_newYearLimitedPack1";
        }
        else if (productId.Contains("newYearLimitedPack2"))
        {
            spriteName = "icon_newYearLimitedPack2";
        }
        else if (productId.Contains("LaborDay"))
        {
            spriteName = "newLaborPack2";
        }
        else if (productId.Contains("newYearLimitedDancingPack"))
        {
            spriteName = "icon_newYearLimitedDancingPack";
        }


        var spriteatlasPath = "Assets/Loadable/UI/UIPanel/FittingRoomPanel/FittingRoomPanel.spriteatlas";
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, spriteName, gameObject);
        return sprite;
    }


    private string GetSpriteName(string productId)
    {
        var spriteName = "icon_premium_pass";
        if (productId.Contains("giftgaojipass"))
        {
            spriteName = "icon_premium_pass";
        }
        else if (productId.Contains("gifthaohuapass"))
        {
            spriteName = "icon_deluxe_pass";
        }
        else if (productId.Contains("gifthaohuauppass"))
        {
            spriteName = "icon_deluxe_pass_upgrade";
        }
        else if (productId.Contains("giftvip"))
        {
            spriteName = "icon_vip_month";
        }
        else if (productId.Contains("newYearLimitedPack1"))
        {
            spriteName = "icon_newYearLimitedPack1";
        }
        else if (productId.Contains("newYearLimitedPack2"))
        {
            spriteName = "icon_newYearLimitedPack2";
        }
        else if (productId.Contains("LaborDay"))
        {
            spriteName = "newLaborPack2";
        }
        else if (productId.Contains("newYearLimitedDancingPack"))
        {
            spriteName = "icon_newYearLimitedDancingPack";
        }
        return spriteName;
    }

    private void ToolsItemUpdate()
    {
        var spriteName = GetSpriteName(mData.Id);
        assetsIcon.gameObject.SetActive(true);
        var spriteatlasPath = "Assets/Loadable/UI/UIPanel/FittingRoomPanel/FittingRoomPanel.spriteatlas";
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, spriteName, gameObject);
        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }

    private void SingleItemUpdate(List<AssetsData> assetsData)
    {
        if (assetsData == null) return;
        if (assetsData.Count != 1) return;
        var assets = assetsData[0];
        // 资源类型
        switch (assets.ResourceType)
        {
            case ResourceType.Avatar:
                PgcItem(assets);
                break;
            case ResourceType.UgcAvatar:
                UgcItem((UGCAssetsData)assets);
                break;
            case ResourceType.Emote:
                EmoteItem((EmoteAssetsData)assets);
                break;
            case ResourceType.MusicScore:
                MusicScoreItem((MusicScoreAssetsData)assets);
                break;
            case ResourceType.PGCPetAvatar:
                PetPgcItem(assets);
                break;
            case ResourceType.UGCPetAvatar:
                PetUgcItem((UGCAssetsData)assets);
                break;
            case ResourceType.UgcPose:
                PetUgcPoseItem((UgcPoseAssetsData)assets);
                break;
            case ResourceType.UgcEmote:
                PetUgcAnimItem((UgcAnimAssetsData)assets);
                break;
            case ResourceType.UgcVehicle:
                VehicleItem((UgcVehicleAssetsData)assets);
                break;
        }
    }

    private void VehicleItem(UgcVehicleAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.vehicleInfo.cover);

            if (assetName != null)
            {
                assetName.text = assetsData.UgcInfo.vehicleInfo.name;
            }
        }

    }

    private void PgcItem(AssetsData assetsData)
    {
        url = null;
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);
        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }

    private void PetPgcItem(AssetsData assetsData)
    {
        url = null;
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);

        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }

    private void EmoteIdleItem()
    {
        url = null;
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcEmoteSprite);
        string spriteName = mData.Id;
        if (mData.ProductId != 0)
        {
            spriteName = mData.Id + mData.ProductId;
        }

        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, spriteName, gameObject);
        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }


    private void EmoteItem(EmoteAssetsData assetsData)
    {
        url = null;
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);
        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
    }

    private void UgcItem(UGCAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.UgcInfo.cover);
        }
    }

    private void PetUgcItem(UGCAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.UgcInfo.cover);
        }
    }


    private void PetUgcPoseItem(UgcPoseAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.UgcInfo.cover);
        }
    }

    private void PetUgcAnimItem(UgcAnimAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.UgcInfo.cover);
            if (assetName != null)
            {
                assetName.text = assetsData.UgcInfo.UgcInfo.name;
            }
        }
    }

    private void MusicScoreItem(MusicScoreAssetsData assetsData)
    {
        remoteAssetsIcon.gameObject.SetActive(false);
        assetsIcon.gameObject.SetActive(true);
        assetsIcon.sprite = budSprite;
        if (assetsData.UgcInfo != null)
        {
            RefreshCover(assetsData.UgcInfo.musicScoreInfo.cover);
        }
    }

    private void RefreshCover(string cover)
    {
        url = cover;
        remoteAssetsIcon.Load(cover, onCompleted: (bool fromCache, bool success) =>
        {
            if (this == null || url != cover) return;
            assetsIcon.gameObject.SetActive(false);
            remoteAssetsIcon.gameObject.SetActive(true);
        });
    }


    private void UpdateTitleLength(int length)
    {
        titleLengthLimit.text = $"{length}/24";
    }

    private void UpdateContentLength(int length)
    {
        contentLengthLimit.text = $"{length}/250";
    }

    void KeyboardReturn(string str)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        if (inputType == 0)
        {
            title = str;
            titleText.color = DataUtil.DeSerializeColorCheckHash("#000000");
            titleText.text = str;
            UpdateTitleLength(str.Length);
        }
        else
        {
            content = str;
            contentText.color = DataUtil.DeSerializeColorCheckHash("#000000");
            contentText.text = str;
            UpdateContentLength(str.Length);
        }
    }
}