using Com.TheFallenGames.OSA.DataHelpers;
using Es;
using EventTracking;
using Game.Avatar;
using Game.Config;
using Game.Store;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace UI.UIPanels.CreaterRewardPanel
{
    public class CreatorRewardView : MonoBehaviour
    {
        [SerializeField] internal SpriteAtlas usedSA;
        [Header("人物形象")] [SerializeField] internal Transform characterRoot;
        [SerializeField] internal AvatarCameraController avatarCameraController;
        [SerializeField] internal Button backButton;
        [SerializeField] internal Button rewardBackButton;

        [Header("操作按钮UI")] [SerializeField] internal OperationView operationUI;
        [Header("操作按钮UI")] [SerializeField] internal ItemInfo itemInfoUI;
        [Header("列表")] [SerializeField] internal FittingRoomAdapter assetsList;
        [Header("系列背景")] [SerializeField] internal ActivityCenterBgItem seriesBg;
        [Header("奖品界面")] [SerializeField] internal GameObject RewardView;

        [Header("创作者中心")] [SerializeField] internal Text greenCoinLeft;
        [SerializeField] internal Text greenCoinCount;
        [SerializeField] internal Text greenCoinUsed;
        [SerializeField] internal List<CreatorSeriesItem> gachaponList;
        [SerializeField] internal List<CreatorSeriesItem> seriesList;

        internal CharacterWrap characterWrap;
        internal CharacterWrap otherCharacterWrap;
        internal PlayerAnimationCtrl animationCtrl;
        internal PlayerAnimationCtrl otherAnimationCtrl;

        internal CharacterData saveCharacterData;

        private AmbientLightSetting _srcLightSetting;
        private bool _srcHallLightVisible;
        private bool _srcGameSceneLightVisible;
        private bool _srcPreviewSceneLightVisible;

        private CreatorRewardHandler dataHandler;

        private List<GoodsData> goodsDatas;
        private string pgcId;

        private bool initedRewardView;

        private static int allConsume;
        private Action<bool> _backAction;
        private Action<bool> _clickAction;
        private bool isInit = false;

        public void OnInitCreated(Action<bool> backAction, Action<bool> clickAciton)
        {
            if (isInit)
            {
                return;
            }

            isInit = true;
            this._backAction = backAction;
            this._clickAction = clickAciton;

            backButton.onClick.AddListener(() =>
            {
                _backAction?.Invoke(true);
            });

            _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
            _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
            _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
            _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();

            dataHandler = AssetsDataManager.GetData<CreatorRewardHandler>();
            dataHandler.AddDataChange(gameObject, OnDataChange);
            var series = dataHandler.GetSeriesList(Product.SeriesType.CreatorExchangeSeries);
            for (int i = 0; i < seriesList.Count; i++)
            {
                var itemNode = seriesList[i];
                var data = series.Find(x => x.ServerData?.Id == itemNode.SeriesId);
                if (data != null)
                {
                    itemNode.gameObject.SetActive(true);
                    itemNode.SetData(data);
                    itemNode.Button.onClick.RemoveAllListeners();
                    itemNode.Button.onClick.AddListener(() =>
                    {
                        _clickAction?.Invoke(true);
                        OnSeriesEnter(data);
                    });
                }
            }
           
            for (int i = 0; i < gachaponList.Count; i++)
            {
                var item = gachaponList[i];
                item.gameObject.SetActive(true);
                item.Button.onClick.RemoveAllListeners();
                item.Button.onClick.AddListener(() =>
                {
                    var viewId = item.ViewType;
                    string lotteryId = GashaponDataManager.Inst.GetGashaponViewCfg(viewId).GashaId;
                    if (string.IsNullOrEmpty(lotteryId))
                    {
                        return;
                    }
                    GashaponDataManager.Inst.JumpToGashapon(lotteryId);
                });
            }

            assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
            assetsList.OnItemSelected = OnItemSelected;

            rewardBackButton.onClick.AddListener(CloseRewardView);
            AccountDataManager.Inst.BalanceInfo.Refresh();
            Refresh();

            CheckBusinessLive();
        }

        private void Start()
        {
            BusinessLiveManager.Inst.AddConfigUpdateListener(OnBusinessConfigUpdate);
        }

        private void OnDestroy()
        {
            BusinessLiveManager.Inst.RemoveConfigUpdateListener(OnBusinessConfigUpdate);
        }

        private void OnBusinessConfigUpdate(BusinessLiveConfig config)
        {
            CheckBusinessLive();
        }


        private void CheckBusinessLive()
        {
            for (int i = 0; i < gachaponList.Count; i++)
            {
                var item = gachaponList[i];
                if (BusinessLiveManager.Inst.IsGashaponLive((int)item.ViewType) && item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(true);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }
            
            for (int i = 0; i < seriesList.Count; i++)
            {
                var item = seriesList[i];
                var data = item.GetData();
                if (data != null && data.ServerData != null && BusinessLiveManager.Inst.IsSeriesLive(data.ServerData.Id.ToString()) && item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(true);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }
        }

        private void Awake()
        {
            assetsList.Init();
            RewardView.gameObject.SetActive(false);
        }

        private void CloseRewardView()
        {
            avatarCameraController.ResetEmoteView();
            animationCtrl.ResetEmoteForUICharacter();
            otherAnimationCtrl.gameObject.SetActive(false);
            otherAnimationCtrl.ResetEmoteForUICharacter();

            RewardView.gameObject.SetActive(false);
            _clickAction?.Invoke(false);
        }

        private void OnSeriesEnter(BUDSeriesData data)
        {
            seriesBg.gameObject.SetActive(true);

            var ViewCfg = Es.DataTables.GetSeriesViewConfig(data.ServerData.Id);
            if (ViewCfg != null)
            {
                seriesBg.InitCustomBgItem(ViewCfg.BgColor, ViewCfg.AtlasPath, ViewCfg.BgSpriteIds);
                ColorUtility.TryParseHtmlString(ViewCfg.ItemBgColor, out assetsList.BgColor);
            }

            ColorUtility.TryParseHtmlString("#FFD400", out assetsList.SelectedColor);
            RewardView.gameObject.SetActive(true);
            seriesBg.GetComponent<ColorBgPanel>()?.RefreshSprite();
            if (!initedRewardView) InitRewardView();
            goodsDatas = data.GoodsList;
            assetsList.Data.ResetItems(data.GoodsList.Count);
        }

        private void OnDataChange(AssetsData[] assets)
        {
            assetsList.Data.ResetItems(goodsDatas.Count);
        }

        private void InitRewardView()
        {
            assetsList.Init();

            saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
            if (saveCharacterData == null)
                saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
            if (saveCharacterData != null)
            {
                characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
                characterWrap.SetParent(characterRoot, true);
                animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                avatarCameraController.RotateTarget = characterRoot;

                otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(AccountDataManager.Inst.UserInfo.otherAvatarInfo);
                otherCharacterWrap.SetParent(characterRoot, true);
                otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                otherCharacterWrap.Avatar.gameObject.SetActive(false);
            }

            initedRewardView = true;
        }

        internal void OnItemSelected(GoodsData data)
        {
            pgcId = data.Id;

            try
            {
                TryOn(data);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + e.StackTrace);
            }

            assetsList.Data.ResetItems(goodsDatas.Count);
        }

        public GoodsData CreateNewModel(int index)
        {
            var assetsData = goodsDatas[index];
            assetsData.Selected = pgcId == assetsData.Id;
            if (assetsData.Selected) RefreshUI(assetsData);
            return assetsData;
        }

        /// <summary>
        /// 选择item后的更新UI
        /// </summary>
        /// <param name="data"></param>
        public void RefreshUI(GoodsData data)
        {
            itemInfoUI.gameObject.SetActive(true);
            itemInfoUI.SetTarget(data);

            operationUI.gameObject.SetActive(true);
            operationUI.SetTarget(data, false);
            operationUI.OnOperation = (operation, target) =>
            {
                switch (operation)
                {
                    case Operation.Buy:
                        Buy(data);
                        break;
                    case Operation.ChangeOtherOc:
                        ChangeOtherOc();
                        break;
                }
            };
        }

        public void ChangeOtherOc()
        {
            UIManager.Inst.OpenPanelTakeAni<OcChangePanel>(PanelId.OcChangePanel, OcChangeScene.DoubleEmote).OnCloseAction = ChangeOtherOc;
        }

        private void ChangeOtherOc(BaseAvatarData baseAvatarData)
        {
            if (baseAvatarData != null)
            {
                try
                {
					var data = (CharacterData)baseAvatarData;
                    otherCharacterWrap.SetCharacterData(data);
                    PlayerPrefs.SetString(GameConsts.EmoteOtherPlayerOcKey + AccountDataManager.Inst.Uid, CharacterData.SerializeObject(data));
                }
                catch { }
            }
        }

        public void Buy(GoodsData data)
        {
            AssetsDataManager.BuyGoods(data, (success, reason, needNum) =>
            {
                if (!success)
                {
                    if (reason.Equals("余额不足"))
                    {
                        switch (data.Price.CurrencyType)
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
                                UIManager.Inst.OpenPanel(PanelId.TipPanel, reason);
                                break;
                        }

                        return;
                    }
                    //UIManager.Inst.OpenPanel(PanelId.TipPanel, reason);
                }
                else
                {
                    Refresh();
                    if (data.Price.CurrencyType == CurrencyType.PinkCoin)
                    {                 
                        LoadEvent.ReportPopupStatus(data.Price.Value.ToString(), "PaymentSuccessful-Value"); ;
                    }
                    var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                    panel.InitData(data, "购买成功！");
                }

                data.IsPayingRequest = false;
            });
        }

        public void OnHidden()
        {
            AmbientLightManager.Inst.CloseUILight(_srcLightSetting);
            AmbientLightManager.Inst.RevertHallLight(_srcHallLightVisible);
            AmbientLightManager.Inst.RevertGameSceneLight(_srcGameSceneLightVisible);
            AmbientLightManager.Inst.RevertPreviewLight(_srcPreviewSceneLightVisible);

            if (!initedRewardView) return;

            avatarCameraController.ResetEmoteView();
            animationCtrl.ResetEmoteForUICharacter();
            otherAnimationCtrl.gameObject.SetActive(false);
            otherAnimationCtrl.ResetEmoteForUICharacter();
        }


        internal void CancelTryOn()
        {
            characterWrap.RefreshAvatar(saveCharacterData);
        }


        /// <summary>
        /// 试穿 重置参数
        /// </summary>
        /// <param name="goodsData"></param>
        internal void TryOn(GoodsData goodsData)
        {
            CancelTryOn();

            avatarCameraController.ResetEmoteView();
            animationCtrl.ResetEmoteForUICharacter();
            otherAnimationCtrl.gameObject.SetActive(false);
            otherAnimationCtrl.ResetEmoteForUICharacter();

            for (int i = 0, C = goodsData.Assets.Count; i < C; i++)
            {
                var assets = goodsData.Assets[i];
                switch (assets.ResourceType)
                {
                    case ResourceType.Avatar:
                        var pgcAssets = assets as PGCAssetsData;
                        var subType = UniqueType.GetAvatar(pgcAssets.AvatarSubType);
                        var config = DataTables.GetAvatarCommonData(goodsData.GetPgcId());
                        goodsData.Loading(true);
                        characterWrap.ChangePart(subType, pgcAssets.Id, () => goodsData.Loading(false));
                        characterWrap.ChangeColor(subType, config.defaultColor);
                        characterWrap.Move(subType, config.pDef);
                        characterWrap.Rotate(subType, config.rDef);
                        characterWrap.Scale(subType, config.sDef);
                        characterWrap.HVScale(subType, config.vhSDef);
                        characterWrap.SetLeftOrRight(subType, config.leftRightType);
                        break;
                    case ResourceType.UgcAvatar:
                        var ugcAssets = assets as UGCAssetsData;
                        goodsData.Loading(true);
                        characterWrap.ChangeUGCPart(ugcAssets.UgcInfo.skinInfo, () => goodsData.Loading(false));
                        break;
                    case ResourceType.Emote:
                        var emoteAssets = assets as EmoteAssetsData;
                        avatarCameraController.SetEmoteView(emoteAssets.Id);
                        switch (emoteAssets.EmoteSubType)
                        {
                            case EmoteSubType.Single:
                            case EmoteSubType.SingleLoop:
                                goodsData.Loading(true);
                                animationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id,
                                    OnDownloadOver: () => goodsData.Loading(false));
                                break;
                            case EmoteSubType.Double:
                            case EmoteSubType.DoubleLoop:
                                goodsData.Loading(true);
                                animationCtrl.PlayDoubleEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl,
                                    OnDownloadOver: () => goodsData.Loading(false));
                                break;
                        }

                        break;
                }
            }
        }

        public void Refresh()
        {
            SetConsume();
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.creatorConsume, HttpMethod.GET, "",
                onReceive: arg0 =>
                {
                    var data = JsonConvert.DeserializeObject<CreatorConsumeData>(arg0);
                    allConsume = data.allConsume;
                    SetConsume();
                },
                onFail: arg0 => { }
            );
        }

        private void SetConsume()
        {
            var num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.GreenCoin);
            greenCoinLeft.text = MaxNum(num);
            greenCoinCount.text = MaxNum(num + allConsume);
            greenCoinUsed.text = MaxNum(allConsume);
        }

        private string MaxNum(int num)
        {
            if (num < 1000000)
            {
                return num.ToString();
            }
            else
            {
                return "999999";
            }
        }
    }

    public class CreatorConsumeData
    {
        public int allConsume;
        public int todayConsume;
    }

    public class CreatorTopConfig
    {
        public int tapId;
        public string name;
    }
}
