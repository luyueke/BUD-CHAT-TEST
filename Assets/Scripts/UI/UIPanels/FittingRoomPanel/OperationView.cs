using System;
using System.Collections;
using System.Collections.Generic;
using EventTracking;
using Game.Base;
using Game.Config;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Product;
using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public enum Operation
    {
        Wear,
        Buy,
        ShoppingBuy,
        ShoppingCart,
        SaveOc,
        Jump,
        SkinTicketButton,
        TryPlayMusic,
        ChangeOtherOc,
        LikeUgc,
        Select
    }

    public class OperationView : MonoBehaviour
    {
        [SerializeField] Button WearButton;
        [SerializeField] Button BuyButton;
        [SerializeField] Button ShoppingBuyButton;
        [SerializeField] Button ShoppingCartButton;
        [SerializeField] Button JumpButton;
        [SerializeField] Button PlayMusicButton;
        [SerializeField] Button PlayVehicleButton;
        [SerializeField] Button ChangeOtherOcButton;

        [SerializeField] UgcLikeButton UgcLikeButton;

        [SerializeField] Button SelectButton;
        [SerializeField] Button SelectedButton;


        private GoodsData target;
        public Action<Operation, GoodsData> OnOperation;

        private BuyButton buyComp;
        private BuyButton shoppingBuyComp;
        private Text JumpText;

        /// <summary>
        /// 确定是否是试衣间Panel显示，需要判断Item来源，需要单独处理
        /// </summary>
        private bool isFittingRoomPanel;
    private const string tempSpriteatlasPath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";

        public void Awake()
        {
            WearButton.onClick.AddListener(() =>
            {
                OperationInvoke(Operation.Wear);
                var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(WindowId.FittingRoomWindow, PanelId.FittingRoomPanel);
                if (panel != null && panel.IsActorShowMode)
                {
                    WearButton.gameObject.SetActive(false);
                    panel.RefreshActorShowBtns();
                }
            });

            buyComp = BuyButton.GetComponent<BuyButton>();
            BuyButton.onClick.AddListener(() =>
            {
                if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
                    LoadEvent.ReportPopupStatus("BuyBtnClicked", "ClickUGCBuy");
                if (SignInPanel.isNewPlayer)
                {
                    LoadEvent.ReportPopupStatus("open", "pay_open");
                    LoadEvent.ReportPopupStatus(target.Price.Value.ToString() + target.Name, "pay_id");
                }
                TryBuyWithAutoExchange(Operation.Buy);
            });

            if (ShoppingBuyButton != null)
            {
                shoppingBuyComp = ShoppingBuyButton.GetComponent<BuyButton>();
                ShoppingBuyButton.onClick.AddListener(() =>
                {
                    if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
                        LoadEvent.ReportPopupStatus("BuyBtnClicked", "ClickUGCBuy");
                    if (SignInPanel.isNewPlayer)
                    {
                        LoadEvent.ReportPopupStatus("open", "pay_open");
                        LoadEvent.ReportPopupStatus(target.Price.Value.ToString() + target.Name, "pay_id");
                    }
                    TryBuyWithAutoExchange(Operation.ShoppingBuy);
                });
            }

            if (ShoppingCartButton != null)
            {
                ShoppingCartButton.onClick.AddListener(() =>
                {
                    // 只有 UGC 商品能加入购物车,非 UGC 弹提示并拦截
                    if (target == null || (target.GoodsType != GoodsType.SingleUgc && target.GoodsType != GoodsType.BundleUgc))
                    {
                        TipPanel.ShowToast("仅UGC商品可加入购物车");
                        return;
                    }
                    AddCurrentTargetToShoppingCart();
                    OperationInvoke(Operation.ShoppingCart);
                });
            }

            JumpText = JumpButton.GetComponentInChildren<Text>();
            JumpButton.onClick.AddListener(() =>
            {
                OperationInvoke(Operation.Jump);
            });

            PlayMusicButton.onClick.AddListener(() =>
            {
                OperationInvoke(Operation.TryPlayMusic);
                LoadEvent.ReportTask(147, 6);
            });
            PlayVehicleButton.onClick.AddListener(() =>
            {
                // 当前 OperationView 选中的商品就是 target；UGC载具的 VehicleInfo 在 AssetsData.UgcInfo.vehicleInfo 上
                if(!GameController.IsInHallScene())
                {
                    TipPanel.ShowToast("在社区地图中无法进入试驾驶状态，请退出社区地图后重试");
                    return;
                }
                var vehicleInfo = GetCurrentSelectedVehicleInfo();
                if (vehicleInfo == null)
                {
                    Debug.LogError("[OperationView] 当前未获取到选中载具的 VehicleInfo");
                    return;
                }
                var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tempSpriteatlasPath, "UGCVehicle_1", gameObject);
                var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
                p.Init(new VehicleInfo()
                {
                    name = vehicleInfo.name
                }, null, LoadingType.Vehicle, s: sprite);

                // 进图试玩载具：不打开 UGCVehicleEditPanel，直接打开 VehiclePlayPanel
                GameController.StartVehicleActionGame(EnterGameModel.UgcVehicleTryPlay, vehicleInfo);
                //GameController.StartVehicleActionGame(EnterGameModel.UgcVehicleEmptyContinueEdit, vehicleInfo);
            });
            if (ChangeOtherOcButton != null)
            {
                ChangeOtherOcButton.onClick.AddListener(() =>
                {
                    OperationInvoke(Operation.ChangeOtherOc);
                });
            }
            if (UgcLikeButton != null)
            {
                UgcLikeButton.ClickAction = () =>
                {
                    OperationInvoke(Operation.LikeUgc);
                };
            }

            if (SelectButton != null) SelectButton.onClick.AddListener(OnSelectBtn);
            MessageHelper.AddListener(MessageName.BuyShapeRefresh, RefreshItem);
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener(MessageName.BuyShapeRefresh, RefreshItem);
        }

        private void RefreshItem()
        {
            SetBuyButtonActive(false);
        }

        private void OperationInvoke(Operation operation)
        {
            OnOperation?.Invoke(operation, target);
        }

        private void AddCurrentTargetToShoppingCart()
        {
            if (target == null || target.Price == null) return;
            var data = new ShoppingCartItemData
            {
                id = target.Id,
                price = (int)target.Price.Value,
                originalPrice = (int)(target.OriginalPrice?.Value ?? target.Price.Value),
                currencyType = (int)target.Price.CurrencyType,
                name = target.Name,
                subType = target.subType,
                goodsType = (int)target.GoodsType,
                type = 0,
                iconPath = string.Empty,
            };
            if (target.GoodsType == GoodsType.BundleUgc && target.UgcBundleInfo != null && target.UgcBundleInfo.UgcInfo != null)
            {
                // 套装:封面用套装自身；type 从第一个子资源取（宠物套装=UGCPetAvatar，人物套装=UgcAvatar）
                data.type = (target.Assets != null && target.Assets.Count > 0)
                    ? (int)target.Assets[0].ResourceType
                    : (int)ResourceType.UgcAvatar;
                data.iconPath = target.UgcBundleInfo.UgcInfo.cover;
            }
            else if (target.Assets != null && target.Assets.Count > 0)
            {
                var asset = target.Assets[0];
                data.type = (int)asset.ResourceType;
                if (asset is UGCAssetsData ugcAsset && ugcAsset.UgcInfo != null && ugcAsset.UgcInfo.UgcInfo != null)
                {
                    data.iconPath = ugcAsset.UgcInfo.UgcInfo.cover;
                }
            }
            // 直接用 isCharacterFittingRoom 打标，比从 ResourceType 推断更可靠（套装子资源可能有缓存问题）
            var fittingRoomPanel = UIManager.Inst.FindPanel<FittingRoomPanel>(WindowId.FittingRoomWindow, PanelId.FittingRoomPanel);
            if (fittingRoomPanel != null) data.isPet = !fittingRoomPanel.isCharacterFittingRoom;
            bool added = ShoppingCartManager.Inst.AddItem(data, target);
            if (fittingRoomPanel != null) fittingRoomPanel.RefreshShoppingCartRedPoint();
            // 只有实际新增才提示，避免旧数据 id 重复导致 toast 误弹
            if (added) TipPanel.ShowToast("已加入购物车");
            else TipPanel.ShowToast("已在购物车中");
        }

        private VehicleInfo GetCurrentSelectedVehicleInfo()
        {
            if (target == null)
            {
                return null;
            }

            // 兜底：如果未来 UGC 载具也出现 Bundle 形态，允许从 UgcBundleInfo 取（它本质也是 RecommendItemData）
            if (target.GoodsType == GoodsType.BundleUgc)
            {
                return target.UgcBundleInfo?.vehicleInfo;
            }

            var asset = target.GetFirstAsset<AssetsData>();
            return asset?.UgcInfo?.vehicleInfo;
        }

        public void SetTarget(GoodsData goodsData, bool isFittingRoom = true)
        {
            isFittingRoomPanel = isFittingRoom;
            target = goodsData;
            Reset();

            // 商品类型 单品或者捆绑包
            switch (goodsData.GoodsType)
            {
                case GoodsType.SinglePgc:
                case GoodsType.SingleUgc:
                    SingleItemUpdate(goodsData.Assets);
                    break;
                case GoodsType.BundleUgc:
                    UgcBundleItemUpdate(goodsData.UgcBundleInfo);
                    break;
            }


        }

        private void SingleItemUpdate(List<AssetsData> assetsData)
        {

            if (assetsData == null) return;
            if (assetsData.Count != 1) return;
            var assets = assetsData[0];

            if (assets.ResourceType == ResourceType.Avatar || assets.ResourceType == ResourceType.UgcAvatar)
            {
                var avatarAssetsData = assets as AvatarAssetsData;

                if (avatarAssetsData.AvatarSubType == AvatarSubType.MusicalInstrument)
                {
                    PlayMusicButton.gameObject.SetActive(true);
                }
            }
            else if(assets.ResourceType == ResourceType.UgcVehicle)
            {
                PlayVehicleButton.gameObject.SetActive(true);
            }

            AdjustLikeButtonState(assets);
            if (assets.ResourceType == ResourceType.Avatar)
            {
                if (target.IsOwned)
                {
                    var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(WindowId.FittingRoomWindow, PanelId.FittingRoomPanel);
                    if (panel != null && panel.HaveSelectData() && (panel.IsClassType(10024) || panel.IsClassType(50024)))
                    {
                        if (panel.CanSelect(target))
                        {
                            SelectButton.gameObject.SetActive(true);
                        }
                        else
                        {
                            SelectedButton.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        if (!target.CantWear && !target.IsBagScene) WearButton.gameObject.SetActive(true);
                    }
                    return;
                }
            }
            else if(assets.ResourceType == ResourceType.UgcAvatar)
            {
                if (target.IsOwned || (target.Price != null && target.Price.Value == 0))
                {
                    var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(WindowId.FittingRoomWindow, PanelId.FittingRoomPanel);
                    if (panel != null && panel.HaveSelectData() && (panel.IsClassType(10024) || panel.IsClassType(50024)))
                    {
                        if (panel.CanSelect(target))
                        {
                            SelectButton.gameObject.SetActive(true);
                        }
                        else
                        {
                            SelectedButton.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        if (!target.CantWear && !target.IsBagScene) WearButton.gameObject.SetActive(true);
                    }
                    return;
                }
            }else if (assets.ResourceType == ResourceType.UgcVehicle)
            {
                var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(WindowId.FittingRoomWindow, PanelId.FittingRoomPanel);
                if (target.IsOwned || (target.Price != null && target.Price.Value == 0))
                {
               
                    if (panel != null)
                    {
                        if (!target.CantWear && !target.IsBagScene) panel.ShowVehicleButton();
                    }
                    return;
                }
                else
                {
                    if (panel != null)
                    {
                       panel.HideVihicleButton();
                    }

                }


            }

            // 资源类型
            switch (assets.ResourceType)
            {
                case ResourceType.Avatar:
                case ResourceType.PGCPetAvatar:
                    PgcItem(assets as PGCAssetsData);
                    break;
                case ResourceType.Emote:
                    var emoteData = assets as EmoteAssetsData;
                    PgcItem(emoteData);
                    ChangeOtherOcButton?.gameObject.SetActive(emoteData.EmoteSubType == EmoteSubType.Double || emoteData.EmoteSubType == EmoteSubType.DoubleLoop || emoteData.EmoteSubType == EmoteSubType.PetWithPlayer || emoteData.EmoteSubType == EmoteSubType.PetWithPlayerLoop);
                    break;
                case ResourceType.UGCPetAvatar:
                case ResourceType.UgcAvatar:
                case ResourceType.MusicScore:
                    UgcItem(assets as AssetsData);
                    break;
                case ResourceType.UgcPose:
                    var ugcPoseData = assets as UgcPoseAssetsData;
                    UgcItem(assets as AssetsData);
                    ChangeOtherOcButton?.gameObject.SetActive(ugcPoseData.UgcPoseSubType == UgcPoseSubType.Double || ugcPoseData.UgcPoseSubType == UgcPoseSubType.PetWithPlayer);
                    break;
                case ResourceType.UgcEmote:
                    var ugcEmoteData = assets as UgcAnimAssetsData;
                    UgcItem(assets as AssetsData);
                    ChangeOtherOcButton?.gameObject.SetActive(ugcEmoteData.UgcAnimSubType == UgcAnimSubType.Double || ugcEmoteData.UgcAnimSubType == UgcAnimSubType.PetWithPlayer);
                    break;
                case ResourceType.UgcVehicle:
                    var ugcVehicleData = assets as UgcVehicleAssetsData;
                    UgcItem(assets as AssetsData);

                    break;
                case ResourceType.Vehicle:
                    PgcItem(assets as PGCAssetsData);
                    break;
                case ResourceType.Theatre:
                case ResourceType.AvatarCard:
                    UgcItem(assets as AssetsData);
                    break;

            }
        }

        private void UgcBundleItemUpdate(RecommendItemData bundleInfo)
        {
            Debug.LogError(bundleInfo.ugcId);
            if (!string.IsNullOrEmpty(bundleInfo.ugcId) && !target.IsBagScene)
            {
                UgcLikeButton?.gameObject.SetActive(true);
                UgcLikeButton?.SetBundleData(bundleInfo);
            }
            else
            {

                UgcLikeButton?.gameObject.SetActive(false);
            }

         //   Debug.LogError($"UgcBundleItemUpdate bundleInfo.type={bundleInfo.ugcType},num={bundleInfo.vehicleInfo.paymentInfo.price}");

            if (target.IsOwned)
            {
                if (!target.CantWear && !target.IsBagScene) WearButton.gameObject.SetActive(true);
                return;
            }
            SetBuyButtonActive(true);
        }

        private void PgcItem(AssetsData assetsData)
        {
            if (target.SourceData == null || target.SourceData.Source == Source.Ugc) return;

            if (target.SourceData.Source == Source.Mall || target.SourceData.Source == Source.CreatorReward && !isFittingRoomPanel)
            {
                SetBuyButtonActive(!target.IsOwned);
                return;
            }

            if (target.SourceData.Source == Source.Jump)
            {
                JumpButton.gameObject.SetActive(true);
                JumpText.SetLocalText("未知跳转");
                return;
            }
            JumpButton.gameObject.SetActive(true);
            JumpText.SetLocalText(target.SourceData.Source.GetName(target.SourceData.Id));
        }

        private void UgcItem(AssetsData assetsData)
        {
            bool show = !(target != null && target.IsOwned);
            SetBuyButtonActive(show);
        }

        private void AdjustLikeButtonState(AssetsData assetsData)
        {
            if (assetsData == null || target.IsBagScene)
            {
                UgcLikeButton?.gameObject.SetActive(false);
                return;
            }

            bool isShowLikeBtn = assetsData.ResourceType == ResourceType.MusicScore || assetsData.ResourceType == ResourceType.UgcAvatar || assetsData.ResourceType == ResourceType.UGCPetAvatar || assetsData.ResourceType == ResourceType.UgcEmote || assetsData.ResourceType == ResourceType.UgcPose || assetsData.ResourceType == ResourceType.UgcVehicle || assetsData.ResourceType == ResourceType.Theatre || assetsData.ResourceType == ResourceType.AvatarCard;
            UgcLikeButton?.gameObject.SetActive(isShowLikeBtn);
            UgcLikeButton?.SetData(assetsData);
        }

        // Buy 与 ShoppingBuy 互斥显示:UGC tab 显示 ShoppingBuy,其它显示 Buy
        private void SetBuyButtonActive(bool active)
        {
            bool isUgc = FittingRoomPanel.curTab == MainTabs.Tab.Ugc;
            BuyButton.gameObject.SetActive(active && !isUgc);
            if (ShoppingBuyButton != null) ShoppingBuyButton.gameObject.SetActive(active && isUgc);
            // ShoppingCart 与 ShoppingBuy 成对显隐(同为 UGC 时显示)
            if (ShoppingCartButton != null) ShoppingCartButton.gameObject.SetActive(active && isUgc);
            if (active)
            {
                if (isUgc) { if (shoppingBuyComp != null) shoppingBuyComp.SetTarget(target); }
                else buyComp.SetTarget(target);
            }
        }

        public void Reset()
        {
            WearButton.gameObject.SetActive(false);

            SetBuyButtonActive(false);

            if (ShoppingCartButton != null) ShoppingCartButton.gameObject.SetActive(false);

            JumpButton.gameObject.SetActive(false);

            PlayMusicButton.gameObject.SetActive(false);

            PlayVehicleButton.gameObject.SetActive(false);

            ChangeOtherOcButton?.gameObject.SetActive(false);

            UgcLikeButton?.gameObject.SetActive(false);

            SelectButton?.gameObject.SetActive(false);

            SelectedButton?.gameObject.SetActive(false);
        
        }

        public void Like(Action<string> completeAction = null)
        {
            UgcLikeButton?.Like(completeAction);
        }

        // 余额不足时弹 BuyTipsPanel 静默兑换后再购买，支持所有货币类型
        // 参考 ShoppingCartRootView：先检查钻石是否够兑换，够才弹 BuyTipsPanel，否则引导充值
        private void TryBuyWithAutoExchange(Operation operation)
        {
            if (target?.Price == null) { OperationInvoke(operation); return; }

            var currencyType = target.Price.CurrencyType;
            int price = Mathf.CeilToInt(target.Price.Value);
            int have = AccountDataManager.Inst.BalanceInfo.GetAccountCount(currencyType);

            if (have >= price) { OperationInvoke(operation); return; }

            int needNum = price - have;
            int gemNum = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
            int gemsToExchange;

            switch (currencyType)
            {
                case CurrencyType.PinkCoin:
                    // 与 ShoppingCartRootView 保持一致：JudgePinkCoin 内部处理钻石不足（弹充值引导）
                    if (!ExchangeCoinPanel.JudgePinkCoin(needNum)) return;
                    gemsToExchange = needNum;
                    break;
                case CurrencyType.Badge:
                    gemsToExchange = needNum; // 1:1
                    if (gemNum < gemsToExchange)
                    {
                        UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, gemsToExchange - gemNum);
                        return;
                    }
                    break;
                case CurrencyType.Coin:
                    gemsToExchange = Mathf.CeilToInt(needNum / 10f); // 1:10
                    if (gemNum < gemsToExchange)
                    {
                        UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, gemsToExchange - gemNum);
                        return;
                    }
                    break;
                case CurrencyType.Gem:
                    UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                    return;
                default:
                    OperationInvoke(operation);
                    return;
            }

            // 钻石充足，弹确认面板后静默兑换再购买
            var tipsPanel = UIManager.Inst.OpenPanel<RechargeBuyTipsPanel>(PanelId.RechargeBuyTipsPanel);
            tipsPanel.Init(needNum, gemsToExchange, currencyType, () =>
            {
                var req = new ExchangeReq
                {
                    fromCurrency = (int)CurrencyType.Gem,
                    toCurrency   = (int)currencyType,
                    exchangeNum  = gemsToExchange
                };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.exchangePay, HttpMethod.POST,
                    JsonConvert.SerializeObject(req),
                    _ => AccountDataManager.Inst.BalanceInfo.Refresh(() => OperationInvoke(operation)),
                    failData =>
                    {
                        AccountDataManager.Inst.BalanceInfo.Refresh();
                        var rsp = JsonConvert.DeserializeObject<HttpResponseRawData>(failData);
                        if (rsp != null && !string.IsNullOrEmpty(rsp.rmsg)) TipPanel.ShowToast(rsp.rmsg);
                    });
            });
        }

        private void OnSelectBtn()
        {
            OperationInvoke(Operation.Select);

        }

    }
}
