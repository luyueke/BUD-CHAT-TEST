using Basic.Utils;
using ChocDino.UIFX;
using Com.TheFallenGames.OSA.Util.IO;
using EventTracking;
using Game.Event;
using Game.Store;
using GameData.Base;
using GameData.BaseInfo;
using GameData.PgcData;
using Newbie;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UI.Manager;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class FittingRoomItem : MonoBehaviour
    {
        [Header("颜色")]
        [SerializeField] Image background;
        [SerializeField] Image colorBackground;

        [SerializeField] GameObject assetRoot;
        [SerializeField] GameObject takeOffRoot;
        [SerializeField] GameObject designRoot;
        [SerializeField] GameObject banRoot;
        [Header("节点")]
        [SerializeField] Image selectedImage;
        [SerializeField] GameObject infoRoot;
        [SerializeField] Image iconImage;
        [SerializeField] Text text;
        [SerializeField] Image assetsIcon;
        [SerializeField] RemoteImageBehaviour remoteAssetsIcon;
        [SerializeField] BlurFilter iconBlurFilter;
        [SerializeField] Sprite budSprite;
        [SerializeField] Color selectedColor;
        [SerializeField] Font numFont;
        [SerializeField] Font textFont;
        [SerializeField] GameObject loadingRoot;
        [SerializeField] GameObject extInfoRoot;
        [SerializeField] Text extInfoText;
        [SerializeField] Text designText;
        [SerializeField] Text assetName;
        // [SerializeField] GameObject gemIcon;
        [SerializeField] private GameObject timeNode;
        [SerializeField] private Text timeText;
        [Header("下架")]
        [SerializeField] Button banRewardBtn;
        [SerializeField] Text banRewardText;

        [Header("红点")]
        [SerializeField] GameObject redDotRoot;

        private Action<GoodsData> onItemSelected;
        public GoodsData mData;
        private string url;
        private Vector2 _remoteIconOriginalSize;


        string key = "FirstClickFittingRoomItem" + AccountDataManager.Inst.Uid; //第一次打开要播放引导
        /// <summary>
        /// 确定是否是试衣间Panel显示，需要判断Item来源，需要单独处理
        /// </summary>
        private bool isFittingRoomPanel;

        int curId;
        [Header("限时物品")]
        [SerializeField] GameObject timeLimitRoot;
        [SerializeField] Text timeLimitText;

        public RemoteImageBehaviour RemoteImage => remoteAssetsIcon;

        /// <summary>
        /// 外部调用的购买接口
        /// 直接实现该物品的购买流程，无需通过点击事件
        /// </summary>
        public void BuyItemDirectly()
        {
            if (mData == null)
            {
                Debug.LogWarning("[FittingRoomItem] 无法购买：商品数据为空");
                return;
            }

            if (mData.IsOwned)
            {
                Debug.LogWarning("[FittingRoomItem] 无法购买：该商品已拥有");
                return;
            }

            if (mData.Price == null)
            {
                Debug.LogWarning("[FittingRoomItem] 无法购买：商品价格信息为空");
                return;
            }

            // 检查是否是特殊按钮类型
            if (mData.ButtonType == ButtonType.Design || mData.ButtonType == ButtonType.TakeOff)
            {
                Debug.LogWarning("[FittingRoomItem] 无法购买：该商品为特殊按钮类型，不支持直接购买");
                return;
            }

            // 检查UGC商品的强制更新状态
            if (mData.GoodsType == GoodsType.SingleUgc)
            {
                var assetData = mData.GetFirstAsset<AssetsData>();
                if (assetData?.UgcInfo?.UgcInfo != null)
                {
                    var updateState = (ForceUpdate)assetData.UgcInfo.UgcInfo.forceUpdate;
                    if (updateState != ForceUpdate.Default)
                    {
                        UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, updateState);
                        return;
                    }
                }
            }

            // 检查限时商品的过期状态
            if (isFittingRoomPanel && mData.GoodsType == GoodsType.SinglePgc && !mData.IsOwned)
            {
                if (mData.SourceData?.Source != Source.Mall && mData.EndTime > 0)
                {
                    DateTime endTime = GameUtils.GetDataTimeStamp(mData.EndTime);
                    if (endTime <= DateTime.Now)
                    {
                        TipPanel.ShowToast("该商品为限时活动商品，该活动已结束");
                        return;
                    }
                }
            }

            // 创建临时商品数据，应用打折卡逻辑
            var tmpGoodsData = new GoodsData()
            {
                ButtonType = mData.ButtonType,
                Id = mData.Id,
                GoodsType = mData.GoodsType,
                subType = mData.subType,
                Price = new CurrencyData()
                {
                    CurrencyType = mData.Price.CurrencyType,
                    Value = mData.Price.Value,
                }
            };

            // 当前拥有打折卡且当前商品支持打折卡
            if (DiscountCardUtils.IsOwnedDiscountCard() && DiscountCardUtils.IsSupportDiscountCard(mData))
            {
                tmpGoodsData.Price.Value = Mathf.RoundToInt(tmpGoodsData.Price.Value * DiscountCardUtils.GetDiscount());
            }

            // 直接调用购买接口
            AssetsDataManager.BuyGoods(tmpGoodsData, (success, reason, needNum) =>
            {
                if (!success)
                {
                    if (reason.Equals("余额不足"))
                    {
                        //新用户行为埋点
                        if (SignInPanel.isNewPlayer)
                        {
                            LoadEvent.ReportPopupStatus("fail", "pay_fail");
                        }
                        switch (mData.Price.CurrencyType)
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
                        return;
                    }
                    //UIManager.Inst.OpenPanel(PanelId.TipPanel, reason);
                }
                else
                {
                    //UGC商城埋点
                    EventTracking.LoadEvent.ReportPopupStatus("3", "guide_official_done");
                    if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
                    {
                        LoadEvent.ReportPopupStatus(tmpGoodsData.Price.Value.ToString(), "PaySuccessfulValue");
                        LoadEvent.ReportPopupStatus(tmpGoodsData.subType.ToString(), "PaySuccessfulType");

                    }
                    //新用户行为埋点
                    if (SignInPanel.isNewPlayer)
                    {
                        LoadEvent.ReportPopupStatus("PinkCoin_" + tmpGoodsData.Price.Value.ToString(), "pay_success");
                    }
                    var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                    panel?.InitData(mData, "购买成功！");
                    EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
                }
                mData.IsPayingRequest = false;
            });
        }

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnItemClick);
        }

        private void OnDestroy()
        {
            // 注销所有可能注册的引导遮罩回调
            GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack1;
            GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack2;
        }
        public void OnItemClick()
        {
            if (!PlayerPrefs.HasKey(key) && AccountDataManager.Inst.UserInfo.isNewUser == 1)
            {
                TimerManager.Inst.RunOnce("Boot", 0.2f, () =>
                {
                    UIManager.Inst.OpenPanel(PanelId.BootPanel, WindowId.FittingRoomWindow, 2);
                    EventTracking.LoadEvent.ReportPopupStatus("6", "guide_ID");
                });
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }
            if (mData != null && mData.GoodsType == GoodsType.SingleUgc)
            {
                var assetData = mData.GetFirstAsset<AssetsData>();
                if (assetData == null || assetData.UgcInfo == null || assetData.UgcInfo.UgcInfo == null) return;
                var updateState = (ForceUpdate)mData.GetFirstAsset<AssetsData>().UgcInfo.UgcInfo.forceUpdate;
                if (updateState != ForceUpdate.Default)
                {
                    UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, updateState);
                    return;
                }
            }
            else if (isFittingRoomPanel && mData != null && mData.GoodsType == GoodsType.SinglePgc && !mData.IsOwned)
            {
                if (mData.SourceData.Source != Source.Mall && mData.EndTime > 0)
                {
                    DateTime endTime = GameUtils.GetDataTimeStamp(mData.EndTime);
                    if (endTime <= DateTime.Now)
                    {
                        TipPanel.ShowToast("该商品为限时活动商品，该活动已结束");
                        return;
                    }
                }


            }
            if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
            {
                LoadEvent.ReportPopupStatus(mData.subType.ToString(), "ClickUGCGood");
                switch (mData.subType)
                {
                    case 24:
                        LoadEvent.ReportTask(147, 5);
                        break;
                    case 0:
                        LoadEvent.ReportTask(147, 7);
                        break;
                    default:
                        LoadEvent.ReportTask(147, 2);
                        break;

                }
            }
            if (mData.IsBagScene) AssetsDataManager.ClearNewTip(mData);
            onItemSelected?.Invoke(mData);

            var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            if (panel != null)
            {
                var bo = true;
                switch (mData.GoodsType)
                {
                    case GoodsType.SinglePgc:
                    case GoodsType.SingleUgc:
                        if (mData.Assets != null && mData.Assets.Count > 0)
                        {
                            var assets = mData.Assets[0];
                            // 资源类型
                            switch (assets.ResourceType)
                            {
                                case ResourceType.Avatar:
                                case ResourceType.UgcAvatar:
                                    var ugc = (AvatarAssetsData)assets;
                                    if (ugc.AvatarSubType == AvatarSubType.MusicalInstrument || ugc.AvatarSubType == AvatarSubType.SpecialSkin)
                                    {
                                        bo = false;
                                    }
                                    break;
                                case ResourceType.Emote:
                                    bo = false;
                                    break;
                                case ResourceType.MusicScore:
                                    bo = false;
                                    break;
                                case ResourceType.PGCPetAvatar:
                                    break;
                                case ResourceType.UGCPetAvatar:
                                    break;
                                case ResourceType.UgcPose:
                                    bo = false;
                                    break;
                                case ResourceType.UgcEmote:
                                    bo = false;
                                    break;
                                case ResourceType.Vehicle:
                                    bo = false;
                                    break;
                                case ResourceType.UgcVehicle:
                                    bo = false;
                                    break;
                                case ResourceType.AvatarCard:
                                case ResourceType.Theatre:
                                    bo = false;
                                    break;
                            }
                        }
                        break;
                    case GoodsType.BundleUgc:
                        break;
                }
                panel.ShowOcCompetitionBtn(bo);
            }
        }
        private void ResetAllUI()
        {
            GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack1;
            GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack2;
            if (iconBlurFilter) iconBlurFilter.Blur = 0;
            assetRoot.SetActive(false);
            takeOffRoot.SetActive(false);
            designRoot.SetActive(false);
            banRoot.SetActive(false);
            if (infoRoot)
            {
                infoRoot.SetActive(false);
            }
            if (redDotRoot) redDotRoot.SetActive(false);
            if (extInfoRoot) extInfoRoot.SetActive(false);
            //物品不使用时删除身上的MaskMono组件
            {
                var existingBootMonoComponents = this.gameObject.GetComponents<BootMaskMono>();
                foreach (var component in existingBootMonoComponents)
                {
                    Destroy(component);
                }
            }
        }

        public void SetStyle(Color color1, Color color2)
        {
            colorBackground.color = color1;
            selectedColor = color2;
        }

        private void RemoveMaskCallBack1()
        {
            // 添加安全检查，确保组件和数据仍然有效
            if (this == null || mData == null || onItemSelected == null)
            {
                return;
            }
            // 检查当前游戏对象是否仍然激活
            if (!gameObject.activeInHierarchy)
            {
                return;
            }
            OnItemClick();
            GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack1;
        }

        private void RemoveMaskCallBack2()
        {
            // 添加安全检查，确保组件和数据仍然有效
            if (this == null || mData == null)
            {
                return;
            }

            // 检查当前游戏对象是否仍然激活
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            // 检查价格信息是否有效
            if (mData.Price == null)
            {
                Debug.LogWarning("[FittingRoomItem] RemoveMaskCallBack2: 商品价格信息为空，无法进行购买");
                return;
            }

            // 如果钱不足也返回
            if (AccountDataManager.Inst.BalanceInfo.GetAccountCount(mData.Price.CurrencyType) < mData.Price.Value)
            {
                return;
            }
            BuyItemDirectly();
            GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack2;
        }

        public void UpdateViews(GoodsData goodsData, bool isFittingRoom, Action<GoodsData> action, int id)
        {
            isFittingRoomPanel = isFittingRoom;
            curId = id;
            mData = goodsData;
            bool isFirstBudStoreFirst = id == 1 && BootPanel.currId == 109;
            bool isFirstCloth = mData.subType == 4 && id == 1;
            bool isFirstShu = id == 1 && BootPanel.currId == 1;
            bool isFirstHair = mData.subType == 8 && id == 2;
            onItemSelected = action;
            ResetAllUI();
            Debug.Log($"UpdateViews id={id},mData.subType={mData.subType},BootPanel.currId ={BootPanel.currId}，isFirstBudStoreFirst={isFirstBudStoreFirst} , IsNew={mData.IsNew}");
            // 清理所有现有的BootMaskMono组件

            var existingBootMonoComponents = this.gameObject.GetComponents<BootMaskMono>();
            foreach (var component in existingBootMonoComponents)
            {
                Destroy(component);

            }

            if (goodsData.ButtonType == ButtonType.Design)
            {
                var bootMono = this.gameObject.AddComponent<BootMaskMono>();
                bootMono.id.Add(2);
            }
            else
            {
                if (isFirstShu)
                {
                    var bootMono = this.gameObject.AddComponent<BootMaskMono>();
                    bootMono.id.Add(1099);
                    GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack1;
                    GuideMaskUtils.Inst.removeMaskCallBack += RemoveMaskCallBack1;
                }
                if (isFirstHair)
                {
                    var bootMono = this.gameObject.AddComponent<BootMaskMono>();
                    bootMono.id.Add(106);
                    GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack1;
                    GuideMaskUtils.Inst.removeMaskCallBack += RemoveMaskCallBack1;
                }
                if (mData.Assets != null && mData.Assets.Count > 0 && mData.Assets[0] is AvatarAssetsData avatarAsset && avatarAsset.AvatarSubType == AvatarSubType.Hair && curId == 2)
                {
                    var bootMono = this.gameObject.AddComponent<BootMaskMono>();
                    bootMono.id.Add(106);
                    GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack1;
                    GuideMaskUtils.Inst.removeMaskCallBack += RemoveMaskCallBack1;
                }

                if (isFirstCloth)
                {
                    var bootMono = this.gameObject.AddComponent<BootMaskMono>();
                    bootMono.id.Add(101);
                    bootMono.id.Add(108);
                    GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack1;
                    GuideMaskUtils.Inst.removeMaskCallBack += RemoveMaskCallBack1;
                }

                if (isFirstBudStoreFirst)
                {
                    var bootMono = this.gameObject.AddComponent<BootMaskMono>();
                    bootMono.id.Add(1010);

                    GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack2;
                    GuideMaskUtils.Inst.removeMaskCallBack += RemoveMaskCallBack2;
                }
                string bootKey = "NewBieBootPlay_" + AccountDataManager.Inst.Uid;
                if (!PlayerPrefs.HasKey(key) && !PlayerPrefs.HasKey(bootKey) && !BootPanel.isPlaying && AccountDataManager.Inst.UserInfo.isNewUser == 1 && UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel).isCharacterFittingRoom)
                {

                    UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel).maskObject.SetActive(false);
                    UIManager.Inst.OpenPanel(PanelId.BootPanel, WindowId.FittingRoomWindow, 1);
                    PlayerPrefs.SetInt(bootKey, 1);
                    PlayerPrefs.Save();

                }
            }

            SetSelected(goodsData.Selected);
            if (timeLimitRoot != null)
            {
                timeLimitRoot.SetActive(false);
            }
            if (timeNode != null)
            {
                timeNode.SetActive(false);
            }
            if (mData.ButtonType == ButtonType.TakeOff)
            {
                takeOffRoot.SetActive(true);
                return;
            }
            else if (mData.ButtonType == ButtonType.Design)
            {
                designRoot.SetActive(true);
                if (designText != null && !string.IsNullOrEmpty(goodsData.AddTips)) designText.SetLocalText(goodsData.AddTips);
                return;
            }
            else if (mData.ButtonType == ButtonType.EmoteIdle)
            {
                assetRoot.SetActive(true);
                EmoteIdleItem();
                return;
            }

            assetRoot.SetActive(true);
            // 商品类型 单品或者捆绑包
            switch (goodsData.GoodsType)
            {
                case GoodsType.SinglePgc:
                case GoodsType.SingleUgc:
                    SingleItemUpdate(goodsData.Assets);
                    break;
                case GoodsType.BundleUgc:
                    BundleItemUpdate();
                    break;
            }

            mData.LoadingAction = OnLoadingAction;
            loadingRoot.SetActive(mData.IsLoading);
            var limitData = LimitTimePropManager.Inst.GetLimitTimePorp(mData.Id);
            if (limitData != null)
            {
                if (timeNode != null)
                {
                    timeNode.SetActive(true);
                    timeText.text = limitData.leftTime;
                }
            }
            else
            {
                if (timeNode != null)
                {
                    timeNode.SetActive(false);
                }
            }
            if (mData.IsBagScene)
            {
                if (redDotRoot)
                {
                    redDotRoot.SetActive(mData.IsNew);
                }
                if (!UIManager.Inst.FindPanel(PanelId.AINpcIdlePanel))
                {
                    if (infoRoot)
                        infoRoot.SetActive(false);
                }
                return;
            }
            if (infoRoot)
                infoRoot.SetActive(true);
            SetSelected(goodsData.Selected);
            iconImage.gameObject.SetActive(false);
            text.gameObject.SetActive(true);
            text.font = textFont;


            if (mData.IsOwned)
            {
                if (UIManager.Inst.FindPanel(PanelId.IncubationCabinEmotePopPanel) && !string.IsNullOrEmpty(mData.Name))
                    text.text = mData.Name;
                else
                    text.SetLocalText("已拥有");
                return;
            }

            if (mData.SourceData == null || mData.SourceData.Source == Source.Jump)
            {
                text.SetLocalText("官方");
                return;
            }

            if (mData.Price == null)
            {
                text.SetLocalText("加载中");
                return;
            }

            if (timeLimitRoot != null && isFittingRoomPanel)
            {
                // 试衣间特有逻辑
                if (mData.EndTime > 0 && !mData.IsOwned)
                {
                    timeLimitRoot.SetActive(true);

                    DateTime endTime = GameUtils.GetDataTimeStamp(mData.EndTime);
                    if (endTime > DateTime.Now)
                    {
                        TimeSpan span = endTime - DateTime.Now;
                        if (span.TotalDays > 1d)
                        {
                            timeLimitText.SetLocalText("距结束:{0}", LocalizationManager.Inst.GetLocalizedText("{0}天{1}小时", Math.Floor(span.TotalDays), span.Hours));
                        }
                        else
                        {
                            timeLimitText.SetLocalText("距结束:{0}", LocalizationManager.Inst.GetLocalizedText("{0}小时{1}分", Math.Floor(span.TotalHours), span.Minutes));
                        }
                    }
                    else
                    {
                        timeLimitText.SetLocalText("活动已结束");
                    }
                }
                else
                {
                    timeLimitRoot.SetActive(false);
                }
            }

            switch (mData.SourceData.Source)
            {
                case Source.Mall:
                case Source.Ugc:

                    if (mData.Price.Value == 0 || mData.OriginalPrice.CurrencyType == CurrencyType.None)
                    {
                        text.SetLocalText("免费");
                    }
                    else
                    {
                        var currencySprite = PgcUtils.LoadCurrencyIcon((CurrencyType)mData.OriginalPrice.CurrencyType, gameObject);
                        iconImage.gameObject.SetActive(currencySprite != null);
                        if (currencySprite != null) iconImage.sprite = currencySprite;
                        text.font = numFont;
                        text.text = $"{mData.OriginalPrice.Value}";
                        // if (mData.SourceData.Source == Source.Ugc)
                        // {
                        //     gemIcon.gameObject.SetActive((CurrencyType)mData.OriginalPrice.CurrencyType == CurrencyType.Gem);
                        // }
                    }

                    break;

                case Source.CreatorReward:
                    if (isFittingRoomPanel)
                    {
                        text.SetLocalText(mData.SourceData.Source.GetName(mData.SourceData.Id));
                    }
                    else
                    {
                        if (mData.Price.Value == 0 || mData.OriginalPrice.CurrencyType == CurrencyType.None)
                        {
                            text.SetLocalText("免费");
                        }
                        else
                        {
                            iconImage.gameObject.SetActive(true);
                            iconImage.sprite =
                                PgcUtils.LoadCurrencyIcon((CurrencyType)mData.OriginalPrice.CurrencyType, gameObject);
                            text.font = numFont;
                            text.text = $"{mData.OriginalPrice.Value}";
                            // if (mData.SourceData.Source == Source.Ugc)
                            // {
                            //     gemIcon.gameObject.SetActive((CurrencyType)mData.OriginalPrice.CurrencyType == CurrencyType.Gem);
                            // }
                        }
                    }
                    break;
                case Source.Gashapon:
                    text.SetLocalText(mData.SourceData.Source.GetName(mData.SourceData.Id));
                    break;

                case Source.SeasonPass:
                case Source.VIP:
                case Source.Activity:
                case Source.HotSales:
                case Source.Gift:
                case Source.BeginnerTask:
                case Source.Task:
                default:
                    if (isFittingRoomPanel)
                    {
                        text.SetLocalText(mData.SourceData.Source.GetName(mData.SourceData.Id));
                    }
                    else
                    {
                        text.SetLocalText("官方");
                    }
                    break;
            }
        }
        private void OnLoadingAction(bool loading, GoodsData goodsData)
        {
            if (this == null || goodsData != mData || loadingRoot == null) return;
            loadingRoot.SetActive(loading);
        }

        // 单品
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
                    VehicleUgcItem((UgcVehicleAssetsData)assets);
                    break;
                case ResourceType.Vehicle:
                    VehiclePgcItem((VehicleAssetsData)assets);
                    break;
                case ResourceType.AvatarCard:
                    ActorCardItem((UgcActorAssetsData)assets);
                    break;
                case ResourceType.Theatre:
                    TheatreCardItem((UgcTheatreAssetsData)assets);
                    break;
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

        private void PgcItem(AssetsData assetsData)
        {
            url = null;
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);
            if (sprite != null) assetsIcon.sprite = sprite;
            else assetsIcon.sprite = budSprite;
            if (assetName != null)
                assetName.text = Es.DataTables.GetPgcNameData(assetsData.Id)?.Name;
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

            if (UIManager.Inst.FindPanel(PanelId.AINpcIdlePanel))
            {
                string itemName = "";
                if (mData.Id.Equals("leisure"))
                {
                    itemName = LocalizationManager.Inst.GetLocalizedText("默认");
                }
                else if (mData.Id.Equals("default"))
                {
                    itemName = LocalizationManager.Inst.GetLocalizedText("站立");
                }
                if (infoRoot)
                    infoRoot.SetActive(true);
                text.text = itemName;
                text.gameObject.SetActive(true);
            }
        }


        private void EmoteItem(EmoteAssetsData assetsData)
        {
            url = null;
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);
            if (sprite != null) assetsIcon.sprite = sprite;
            else assetsIcon.sprite = budSprite;

            if (UIManager.Inst.FindPanel(PanelId.AINpcIdlePanel))
            {
                if (infoRoot)
                    infoRoot.SetActive(true);
                text.text = assetsData.Name;
                text.gameObject.SetActive(true);
            }
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

            //if (!mData.IsBagScene) return;

            CheckUgcIsBan(assetsData);

            if (UIManager.Inst.FindPanel(PanelId.AINpcIdlePanel))
            {
                if (infoRoot)
                    infoRoot.SetActive(true);
                text.text = assetsData.UgcInfo?.animInfo?.name;
                text.gameObject.SetActive(true);
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

            //if (!mData.IsBagScene) return;

            CheckUgcIsBan(assetsData);
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
            CheckUgcIsBan(assetsData);
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
            CheckUgcIsBan(assetsData);
        }

        private void PetDefalutUgcAnimItem(UgcAnimAssetsData assetsData)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            assetsIcon.sprite = budSprite;
            if (assetsData.UgcInfo != null)
            {
                RefreshCover(assetsData.UgcInfo.UgcInfo.cover);
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
                extInfoRoot.SetActive(true);
                extInfoText.SetLocalText(assetsData.UgcInfo.musicScoreInfo.toneType == (int)ToneType.Fifteen ? "15音" : "22音");
            }

            //if (!mData.IsBagScene) return;

            CheckUgcIsBan(assetsData);
        }

        private void VehiclePgcItem(VehicleAssetsData assetsData)
        {
            // 清空 url，使复用 holder 时上一条 UGC 远程图的异步回调因 url != cover 而失效，避免残留旧图
            url = null;
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            assetsIcon.sprite = budSprite;
            var sprite = PgcUtils.GetIconSpriteByPgcId(assetsData.Id, gameObject);
            if (sprite != null) assetsIcon.sprite = sprite;
            else assetsIcon.sprite = budSprite;
            extInfoRoot.SetActive(true);
            extInfoText.SetLocalText(assetsData.VehicleSubType == VehicleSubType.SingleVehicle ? "单人" : "双人");
        }

        private void TheatreCardItem(UgcTheatreAssetsData assetsData)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            assetsIcon.sprite = budSprite;
            if (assetsData.UgcInfo != null)
            {
                var theatreInfo = assetsData.UgcInfo.theatreInfo;
                if (theatreInfo != null)
                {
                    RefreshCoverWithAspectHeight(theatreInfo.cover);
                    if (assetName != null)
                        assetName.text = theatreInfo.name;
                }
            }
            banRewardBtn.onClick.RemoveAllListeners();
            AssetsDataManager.GetTheatreInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess || serverData == null) return;
                assetsData.UgcInfo = serverData;
                if (this == null) return;
                if (mData.Id != serverData.UgcInfo.id) return;
                var theatreInfo = serverData.theatreInfo;
                if (theatreInfo == null) return;
                assetsData.Name = theatreInfo.name;
                RefreshCoverWithAspectHeight(theatreInfo.cover);
                if (assetName != null)
                    assetName.text = theatreInfo.name;
                if (!mData.IsBagScene) return;
                Ban(theatreInfo.isBan, theatreInfo.paymentInfo);
            });
        }

        private void ActorCardItem(UgcActorAssetsData assetsData)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            assetsIcon.sprite = budSprite;
            if (assetsData.UgcInfo != null)
            {
                var actorInfo = assetsData.UgcInfo.theatreAvatarInfo;
                if (actorInfo != null)
                {
                    RefreshCover(actorInfo.cover);
                    if (assetName != null)
                        assetName.text = actorInfo.name;
                }
            }
            banRewardBtn.onClick.RemoveAllListeners();
            AssetsDataManager.GetActorInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess || serverData == null) return;
                assetsData.UgcInfo = serverData;
                if (this == null) return;
                if (mData.Id != serverData.UgcInfo.id) return;
                var actorInfo = serverData.theatreAvatarInfo;
                if (actorInfo == null) return;
                assetsData.Name = actorInfo.name;
                RefreshCover(actorInfo.cover);
                if (assetName != null)
                    assetName.text = actorInfo.name;
                if (!mData.IsBagScene) return;
                Ban(actorInfo.isBan, actorInfo.paymentInfo);
            });
        }

        private void VehicleUgcItem(UgcVehicleAssetsData assetsData)
        {
            remoteAssetsIcon.gameObject.SetActive(false);
            assetsIcon.gameObject.SetActive(true);
            assetsIcon.sprite = budSprite;
            if (assetsData.UgcInfo != null)
            {
                RefreshCover(assetsData.UgcInfo.UgcInfo.cover);
                extInfoRoot.SetActive(true);
                extInfoText.SetLocalText(assetsData.VehicleSubType == VehicleSubType.SingleVehicle ? "单人" : "双人");
            }
            CheckUgcIsBan(assetsData);
        }

        private void CheckUgcIsBan(UGCAssetsData assetsData)
        {
            banRewardBtn.onClick.RemoveAllListeners();
            AssetsDataManager.GetUgcInfo(assetsData.Id, (serverData) =>
            {
                assetsData.AvatarSubType = (AvatarSubType)serverData.skinInfo.subType;
                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.skinInfo.name;

                if (this == null) return;
                if (mData.Id != serverData.UgcInfo.id) return;
                RefreshCover(serverData.UgcInfo.cover);
                if (assetName != null)
                    assetName.text = serverData.skinInfo.name;
                if (!mData.IsBagScene) return;
                Ban(serverData.skinInfo.isBan, serverData.skinInfo.paymentInfo);
            });
        }

        private void CheckUgcIsBan(MusicScoreAssetsData assetsData)
        {
            banRewardBtn.onClick.RemoveAllListeners();
            AssetsDataManager.GetMusicScoreInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;

                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.musicScoreInfo.name;
                if (this == null) return;
                if (mData.Id != serverData.musicScoreInfo.id) return;

                RefreshCover(serverData.musicScoreInfo.cover);
                extInfoRoot.SetActive(true);
                extInfoText.SetLocalText(assetsData.UgcInfo.musicScoreInfo.toneType == (int)ToneType.Fifteen ? "15音" : "22音");
                if (!mData.IsBagScene) return;
                Ban(serverData.musicScoreInfo.isBan, serverData.musicScoreInfo.paymentInfo);
            });
        }

        private void CheckUgcIsBan(UgcPoseAssetsData assetsData)
        {
            banRewardBtn.onClick.RemoveAllListeners();
            AssetsDataManager.GetPoseInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;

                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.poseInfo.name;
                if (this == null) return;
                if (mData.Id != serverData.poseInfo.id) return;

                RefreshCover(serverData.poseInfo.cover);
                if (!mData.IsBagScene) return;
                Ban(serverData.poseInfo.isBan, serverData.poseInfo.paymentInfo);
            });
        }

        private void CheckUgcIsBan(UgcAnimAssetsData assetsData)
        {
            banRewardBtn.onClick.RemoveAllListeners();
            AssetsDataManager.GetUgcAnimInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;

                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.animInfo.name;

                if (this == null) return;
                if (mData.Id != serverData.animInfo.id) return;

                RefreshCover(serverData.animInfo.cover);
                if (assetName != null)
                {
                    assetName.text = serverData.animInfo.name;
                }
                if (!mData.IsBagScene) return;
                Ban(serverData.animInfo.isBan, serverData.animInfo.paymentInfo);


                if (UIManager.Inst.FindPanel(PanelId.AINpcIdlePanel))
                {
                    if (infoRoot)
                        infoRoot.SetActive(true);
                    text.text = assetsData.UgcInfo?.UgcInfo?.name;
                    text.gameObject.SetActive(true);
                }
            });
        }

        private void CheckUgcIsBan(UgcVehicleAssetsData assetsData)
        {
            banRewardBtn.onClick.RemoveAllListeners();
            AssetsDataManager.GetUgcVehicleInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;
                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.vehicleInfo.name;
                assetsData.VehicleSubType = (VehicleSubType)serverData.vehicleInfo.vehicleType;
                if (this == null) return;
                if (mData.Id != serverData.vehicleInfo.id) return;
                RefreshCover(serverData.vehicleInfo.cover);
                extInfoRoot.SetActive(true);
                extInfoText.SetLocalText(assetsData.VehicleSubType == VehicleSubType.SingleVehicle ? "单人" : "双人");
                if (!mData.IsBagScene) return;
                //Ban(serverData.vehicleInfo.isBan, serverData.vehicleInfo.paymentInfo);
            });
        }

        private void RefreshCover(string cover)
        {
            var rt = remoteAssetsIcon.transform as RectTransform;
            if (_remoteIconOriginalSize == Vector2.zero) _remoteIconOriginalSize = rt.sizeDelta;
            rt.sizeDelta = _remoteIconOriginalSize;
            url = cover;
            remoteAssetsIcon.Load(cover, onCompleted: (bool fromCache, bool success) =>
            {
                if (this == null || url != cover) return;
                assetsIcon.gameObject.SetActive(false);
                remoteAssetsIcon.gameObject.SetActive(true);
            });
        }

        private void RefreshCoverWithAspectHeight(string cover)
        {
            var rt = remoteAssetsIcon.transform as RectTransform;
            if (_remoteIconOriginalSize == Vector2.zero) _remoteIconOriginalSize = rt.sizeDelta;
            url = cover;
            remoteAssetsIcon.Load(cover, onCompleted: (bool fromCache, bool success) =>
            {
                if (this == null || url != cover) return;
                assetsIcon.gameObject.SetActive(false);
                remoteAssetsIcon.gameObject.SetActive(true);
                if (!success) return;
                var texture = remoteAssetsIcon.RawImage.texture;
                if (texture == null || texture.height == 0) return;
                float height = _remoteIconOriginalSize.y;
                rt.sizeDelta = new Vector2(height * texture.width / texture.height, height);
            });
        }

        private void Ban(int isBan, PaymentInfo paymentInfo)
        {
            if (isBan >= 1)
            {
                iconBlurFilter.Blur = 10;
                banRoot.SetActive(true);
                banRewardText.SetLocalText(isBan == 1 ? "领取补偿" : "确认");
                var haveReward = isBan == 1 && paymentInfo != null;
                banRewardBtn.onClick.RemoveAllListeners();
                banRewardBtn.onClick.AddListener(() =>
                {
                    banRewardBtn.interactable = false;
                    AssetsDataManager.GetBanReward(mData.Id, (success) =>
                    {
                        if (banRewardBtn != null) banRewardBtn.interactable = !success;

                        if (success && haveReward)
                        {
                            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
                            commonRewardItemData.IconSp = PgcUtils.LoadCurrencyIcon(CurrencyType.Badge, gameObject);
                            commonRewardItemData.rewardName = PgcUtils.GetTokenName(CurrencyType.Badge);
                            commonRewardItemData.RewardAmount = paymentInfo.price;
                            panel.ShowRewards(new List<CommonRewardItemData>() { commonRewardItemData });
                        }
                    });
                });
            }
        }

        private void SetSelected(bool flag)
        {
            selectedImage.gameObject.SetActive(false);
            background.color = Color.white;

            if (!flag) return;
            if (infoRoot != null && infoRoot.activeSelf)
            {
                background.color = selectedColor;
            }
            else
            {
                selectedImage.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack1;
            GuideMaskUtils.Inst.removeMaskCallBack -= RemoveMaskCallBack2;
            var existingBootMonoComponents = this.gameObject.GetComponents<BootMaskMono>();
            foreach (var component in existingBootMonoComponents)
            {
                Destroy(component);
            }
        }
    }
}
