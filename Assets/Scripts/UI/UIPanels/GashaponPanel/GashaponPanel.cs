using System;
using System.Collections.Generic;
using Basic.Utils;
using Es;
using Game.Audio;
using Game.Avatar;
using Game.Config;
using Game.Pet;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.CreaterRewardPanel;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
using xasset;

namespace UI.UIPanels.GashaponPanel
{
    public enum GashaponThemePageStyle
    {
        PreviewStyle, //主题皮肤预览模式
        TwistAnimStyle, //扭蛋动画模式
        ContinueTwist,//在扭蛋动画界面继续扭蛋
        EvnetPreview,// 活动页面下的预览
    }

    public class GashaponPanel : BasePanel<GashaponPanel>
    {
        [SerializeField]private CButton backBtn;
        [SerializeField] private Transform bgNode;
        [Header("抽奖按钮相关")]
        [SerializeField]private CButton twistBtn;
        [SerializeField]private CButton twist10Btn;
        [SerializeField]private Image singleIcon;
        [SerializeField]private Image tenIcon;
        [SerializeField]private Text singleText;//单抽价格
        [SerializeField]private Text tenText;//十连抽价格
        [SerializeField]private Text tenDicText;//折后十连抽价格
        [SerializeField]private Text dicText;//折扣文本
        [SerializeField]private GameObject discountNumLine;
        [SerializeField]private GameObject discountTag;//折扣tag节点
        [SerializeField]private CButton infoBtn;
        [SerializeField]private CButton musicPreviewBtn;
        [SerializeField]private CButton changeOtherOcBtn;

        [Header("列表相关")]
        [SerializeField] private Transform cacheNode;
        [SerializeField] private Transform scrollContent;
        [SerializeField] private GashaponPriceItem itemPrefab;

        [Header("道具信息")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Image currencyRewardImage;
        [SerializeField] private Text currencyRewardNum;

        [Header("人物展示")]
        [SerializeField] private GameObject playerImageView;
        [SerializeField] internal Transform characterRoot;
        [SerializeField] internal AvatarCameraController avatarCameraController;


        [Header("各个根节点")]
        [SerializeField] private GameObject previewRoot;//人物预览页
        [SerializeField] private GameObject twistBtnsRoot;//按钮页
        [SerializeField] private GashaponTwistAnimView twistAnimView;//抽奖动画页
        [SerializeField] private GashaponRuleView ruleView;
        [SerializeField] private GashaponLuckValueView gashaponLuckValueView;

        [Header("右上角活动货币")]
        [SerializeField] private GameObject Go_EventCurrencyView;
        [SerializeField] private Text Txt_EventCurrency;

        private const int OneTimeGasha = 1;
        private const int TemTimeGasha = 10;

        protected GashaponStoreHandler dataHandler;
        private string curGashaponId;
        private GashaponData curGashaponData;
        private List<GashaponPriceItem> items = new List<GashaponPriceItem>();
        private LinkedList<GashaponPriceItem> cacheItems = new LinkedList<GashaponPriceItem>();

        //人物3d预览
        internal CharacterWrap characterWrap;
        // 宠物3D 预览
        internal PetWrap petWrap;

        internal BaseAvatarWrapper avatarWrapper;
        internal CharacterWrap otherCharacterWrap;
        internal PlayerAnimationCtrl animationCtrl;
        internal PlayerAnimationCtrl otherAnimationCtrl;
        internal PetAnimationCtrl petAnimationCtrl;

        private List<AccountWidget> _widgets;

        public GashaponThemePageStyle CurPageStyle;
        public override void OnCreate()
        {
            base.OnCreate();
            InitUI();
        }

        private void InitUI()
        {
            backBtn.onClick.AddListener(OnBackBtnClick);
            twistBtn.onClick.AddListener(OnTwistClick);
            twist10Btn.onClick.AddListener(OnTwist10Click);
            infoBtn.onClick.AddListener(() => OnInfoClick());
            twistAnimView.AddBackListener(OnAnimViewBackClick);
            SwitchStyle(GashaponThemePageStyle.PreviewStyle);
            dataHandler = AssetsDataManager.GetData<GashaponStoreHandler>();
            dataHandler.AddDataChange(gameObject, OnDataChange);
            ruleView.gameObject.SetActive(false);
            musicPreviewBtn?.onClick.AddListener(OnMusicalInstrumentsPreview);
            changeOtherOcBtn?.onClick.AddListener(ChangeOtherOc);
        }

        private void InitPreviewPlayer()
        {
            var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
            if (saveCharacterData != null && characterWrap == null)
            {
                characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
                characterWrap.SetParent(characterRoot, true);
                animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                avatarCameraController.RotateTarget = characterRoot;
                animationCtrl.gameObject.SetActive(true);

                otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(AccountDataManager.Inst.UserInfo.otherAvatarInfo);
                otherCharacterWrap.SetParent(characterRoot, true);
                otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                otherCharacterWrap.Avatar.gameObject.SetActive(false);
            }
        }

        private void InitPreviewPet() {
            var savePetData = AccountDataManager.Inst.PetInfo.avatarInfo;
            if (savePetData != null && petWrap == null) {
                petWrap = PetAvatarController.Inst.CreateUIAvatar(savePetData);
                petWrap.SetParent(characterRoot, true);
                petAnimationCtrl = petWrap.Avatar.GetComponentInChildren<PetAnimationCtrl>();
                petAnimationCtrl.gameObject.SetActive(false);
                avatarCameraController.RotateTarget = characterRoot;
            }
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            if (args is { Length: > 0 })
            {
                curGashaponId = args[0] as string;
            }

            InitPreviewPlayer();
            InitPreviewPet();

            InitData(curGashaponId);

            LoggerUtils.Log("###Scheduler.MaxRequests " + Scheduler.MaxRequests + "  Scheduler.MaxUpdateTimeSlice:"+Scheduler.MaxUpdateTimeSlice);
        }

        public override void OnHidden()
        {
            base.OnHidden();
            StopAvatarAnim();
        }

        private void StopAllEmoteSound()
        {
            //关闭的时候清除所有音效
            if (animationCtrl != null && animationCtrl.gameObject != null)
            {
                AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
            }

            if (otherAnimationCtrl != null && otherAnimationCtrl.gameObject != null)
            {
                AkSoundManager.Inst.StopAll(otherAnimationCtrl.gameObject);
            }

            if (petAnimationCtrl != null && petAnimationCtrl.gameObject != null) {
                AkSoundManager.Inst.StopAll(petAnimationCtrl.gameObject);
            }

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            dataHandler.RemoveDataChange(OnDataChange);
            StopAllEmoteSound();
        }

        private void InitData(string id)
        {
            if(string.IsNullOrEmpty(id)) return;
            UpdateBg(id);
            twistAnimView.InitTwistAnimStyle(id);

            curGashaponData = dataHandler.GetGashaponData(id);
            if (curGashaponData == null)
            {
                LoggerUtils.LogError("GashaponPanel InitData Error curGashaponData is null id:"+id);
                return;
            }

            UpdateBtnUI(curGashaponData);
            UpdateWidgetView(curGashaponData);
            UpdateListview(curGashaponData);
            gashaponLuckValueView.Refresh(id, curGashaponData.CurrencyType);
        }

        protected override void Start()
        {
            base.Start();
            Invoke("DefClickFirst", 0.2f);
        }

        public void SwitchStyle(GashaponThemePageStyle target)
        {
            switch (target)
            {
                case GashaponThemePageStyle.PreviewStyle:
                    previewRoot.SetActive(true);
                    twistAnimView.gameObject.SetActive(false);
                    twistBtnsRoot.SetActive(true);
                    gashaponLuckValueView.gameObject.SetActive(true);
                    infoBtn.gameObject.SetActive(true);
                    break;
                case GashaponThemePageStyle.TwistAnimStyle:
                    previewRoot.gameObject.SetActive(false);
                    twistAnimView.gameObject.SetActive(true);
                    twistBtnsRoot.SetActive(false);
                    gashaponLuckValueView.gameObject.SetActive(false);
                    infoBtn.gameObject.SetActive(false);
                    StopAvatarAnim();
                    break;
                case GashaponThemePageStyle.ContinueTwist:
                    previewRoot.gameObject.SetActive(false);
                    twistAnimView.gameObject.SetActive(true);
                    twistBtnsRoot.SetActive(true);
                    gashaponLuckValueView.gameObject.SetActive(true);
                    infoBtn.gameObject.SetActive(true);
                    break;
                case GashaponThemePageStyle.EvnetPreview:
                    previewRoot.SetActive(true);
                    twistAnimView.gameObject.SetActive(false);
                    twistBtnsRoot.SetActive(false);
                    infoBtn.gameObject.SetActive(true);
                    gashaponLuckValueView.gameObject.SetActive(true);
                    gashaponLuckValueView.Refresh(curGashaponData.Id, CurrencyType.Coin);
                    break;
            }
            twistAnimView.OnAnimationRunning(false);
            CurPageStyle = target;
        }

        public void SetEvenetCurrency(int currencyAmount)
        {
            Go_EventCurrencyView.SetActive(true);
            Txt_EventCurrency.text = currencyAmount.ToString();
        }

        private void UpdateBg(string gashaponId)
        {
            var ViewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
            if (ViewCfg == null)
            {
                return;
            }

            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(bgNode);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem(ViewCfg.BgColor, ViewCfg.AtlasPath, ViewCfg.BgSpriteIds);
            item.gameObject.SetActive(true);
        }

        private void UpdateWidgetView(GashaponData data)
        {
            if (data == null) return;
            if (_widgets == null)
            {
                var widgets = gameObject.GetComponentsInChildren<AccountWidget>(true);
                _widgets = new List<AccountWidget>(widgets);
            }

            foreach (var widget in _widgets)
            {
                if ((data.CurrencyType == CurrencyType.Badge || data.CurrencyType == CurrencyType.Coin)&& widget.type == CurrencyType.EnergyCoin)
                {
                    widget.gameObject.SetActive(true);
                    continue;
                }
                if ((int)widget.type == (int)data.CurrencyType)
                {
                    widget.gameObject.SetActive(true);
                }
                else
                {
                    widget.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateBtnUI(GashaponData data)
        {
            if (data == null) return;
            var iconSprite = PgcUtils.LoadCurrencyIcon((int)data.CurrencyType, this.gameObject);
            if (iconSprite != null)
            {
                singleIcon.sprite = iconSprite;
                tenIcon.sprite = iconSprite;
            }

            titleText.SetLocalText(data.Name);
            singleText.SetText(data.SinglePrice.ToString());
            tenText.SetText(data.TenDrawPrice.ToString());

            bool isShowDiscount = data.Discount != 0;
            discountNumLine?.SetActive(isShowDiscount);
            discountTag?.SetActive(isShowDiscount);
            string priceTxt = ((100 - data.Discount) * 0.01 * data.TenDrawPrice).ToString();
            tenDicText.SetText(priceTxt);
            dicText.SetText(string.Format("-{0}%",data.Discount));
        }


        private int GetRealTenPrice(GashaponData data)
        {
            return (int)((100 - data.Discount) * 0.01 * data.TenDrawPrice);
        }

        //背包数据发生变化
        private void OnDataChange(AssetsData[] changes)
        {
            UpdateListview(curGashaponData);
            DefClickFirst();
        }

        private void UpdateListview(GashaponData data)
        {
            ClearItems();
            if(data == null || data.RewardList == null || data.RewardList.Count <= 0) return;
            for (int i = 0; i < data.RewardList.Count; i++)
            {
                GashaponRewardData priceData = data.RewardList[i];
                GashaponPriceItem itemScript = GetItem();
                itemScript.Init(priceData, OnItemClick);
                items.Add(itemScript);
            }
        }

        private void DefClickFirst()
        {
            StopAllEmoteSound();
            if (items.Count > 0)
            {
                items[0].OnItemClick();
            }
        }

        private void SetRewardRawImageShow(bool isShow){
            playerImageView.SetActive(!isShow);
            currencyRewardImage.gameObject.SetActive(isShow);
        }

        private void OnItemClick(GashaponPriceItem item,GashaponRewardData info)
        {
            bool isSelect = false;

            string pgcId = "";
            if (GashaponUtils.HasPGCData(info))
            {
                pgcId = info.PgcDatas[0].Id;
            }

            musicPreviewBtn?.gameObject.SetActive(false);
            changeOtherOcBtn?.gameObject.SetActive(false);

            if (!string.IsNullOrEmpty(pgcId))
            {
                GameResData resData = Es.DataTables.GetGameResData(pgcId);
                if (resData.ResourceType == (int)ResourceType.Avatar || resData.ResourceType == (int)ResourceType.UgcAvatar)
                {
                    bool isMusic = resData.SubType == (int)AvatarSubType.MusicalInstrument;
                    musicPreviewBtn.gameObject.SetActive(isMusic);
                    if (avatarWrapper != characterWrap && avatarWrapper != null) {
                        avatarWrapper.Avatar.SetActive(false);
                    }
                    avatarWrapper = characterWrap;
                    avatarWrapper.Avatar.gameObject.SetActive(true);
                    SetCharacterAvatarCamera();
                } else if (resData.ResourceType == (int)ResourceType.PGCPetAvatar || resData.ResourceType == (int)ResourceType.UGCPetAvatar) {
                    characterWrap.Avatar.SetActive(false);
                    otherCharacterWrap.Avatar.SetActive(false);

                    if (avatarWrapper != petWrap && avatarWrapper != null) {
                        avatarWrapper.Avatar.SetActive(false);
                    }
                    avatarWrapper = petWrap;
                    avatarWrapper.Avatar.gameObject.SetActive(true);
                    SetPetAvatarCamera();
                } else if(resData.ResourceType == (int)ResourceType.Emote) {
                    var emoteSubType = ((EmoteSubType)resData.SubType);
                    if (emoteSubType.IsPet()) {
                        if (avatarWrapper != petWrap && avatarWrapper != null) {
                            avatarWrapper.Avatar.SetActive(false);
                        }

                        avatarWrapper = petWrap;
                        SetPetAvatarCamera();
                    } else {
                        if (avatarWrapper != characterWrap && avatarWrapper != null) {
                            avatarWrapper.Avatar.SetActive(false);
                        }
                        avatarWrapper = characterWrap;
                        SetCharacterAvatarCamera();
                    }
                    avatarWrapper.Avatar.gameObject.SetActive(true);
                    changeOtherOcBtn.gameObject.SetActive(emoteSubType.IsDouble() && !emoteSubType.IsPet());
                }
            }

            var currencyType = GameUtils.ConvertRewardType((int)info.RewardType);
            if (GameUtils.IsCurrencyType((int)currencyType))
            {
                SetRewardRawImageShow(true);
                var iconSprite = PgcUtils.LoadCurrencyIcon(currencyType, currencyRewardImage.gameObject);
                if (iconSprite != null)
                {
                    currencyRewardImage.sprite = iconSprite;
                }
                currencyRewardNum.SetText(info.Num > 1 ? "x" + info.Num : "");
            }
            else
            {
                SetRewardRawImageShow(false);
                CancelTryOn();
                TryOn(info.PgcDatas[0],item);
                PreviewEmote(info.PgcDatas[0]);
            }

            if (GashaponUtils.HasPGCData(info))
            {
                itemNameText.gameObject.SetActive(true);
                itemNameText.SetLocalText(info.PgcDatas[0].Name);
            }
            else
            {
                itemNameText.gameObject.SetActive(false);
            }


            for (int i = 0; i < items.Count; i++)
            {
                items[i].SetSelectStatus(false);
            }

        }

        private void StopAvatarAnim()
        {
            avatarCameraController.ResetEmoteView();
            animationCtrl.ResetEmoteForUICharacter();
            petAnimationCtrl.ResetEmoteForUICharacter();
            otherAnimationCtrl.gameObject.SetActive(false);
            otherAnimationCtrl.ResetEmoteForUICharacter();
        }

        private void HideItemsLoading()
        {
            foreach (var item in items)
            {
                item.SetLoadingVisible(false);
            }
        }

        private void CancelTryOn()
        {
            StopAvatarAnim();
            ResetCharacterRotate();
            if (avatarWrapper is PetWrap) {
                avatarWrapper?.RefreshAvatar(AccountDataManager.Inst.PetInfo.avatarInfo);
            } else if (avatarWrapper is CharacterWrap) {
                avatarWrapper?.RefreshAvatar(AccountDataManager.Inst.UserInfo.avatarInfo);
            }

        }

        internal void TryOn(AssetsData asset,GashaponPriceItem itemNode)
        {
            HideItemsLoading();
            if (asset == null || (asset.ResourceType != ResourceType.Avatar && asset.ResourceType != ResourceType.PGCPetAvatar)) return;

            itemNode?.SetLoadingVisible(true);
            var pgcAssets = asset as PGCAssetsData;
            var classType = asset.ResourceType == ResourceType.Avatar ? UniqueType.GetAvatar(pgcAssets.AvatarSubType) : UniqueType.GetPGCPetAvatar(pgcAssets.AvatarSubType);
            var config = asset.ResourceType == ResourceType.Avatar ? DataTables.GetAvatarCommonData(asset.Id) : DataTables.GetPetAvatarCommonData(asset.Id);
            avatarWrapper.ChangePart(classType, pgcAssets.Id, () =>
            {
                itemNode?.SetLoadingVisible(false);
            });
            avatarWrapper.ChangeColor(classType, config.defaultColor);
            avatarWrapper.Move(classType, config.pDef);
            avatarWrapper.Rotate(classType, config.rDef);
            avatarWrapper.Scale(classType, config.sDef);
            avatarWrapper.HVScale(classType, config.vhSDef);
            avatarWrapper.SetLeftOrRight(classType, config.leftRightType);
            avatarCameraController.SetCameraZoom(classType);
        }

        private void ResetCharacterRotate()
        {
            characterRoot.eulerAngles = new Vector3(0, -180, 0);
        }

        internal void PreviewEmote(AssetsData asset)
        {
            if (asset == null || asset.ResourceType != ResourceType.Emote) return;

            var emoteAssets = asset as EmoteAssetsData;
            switch (emoteAssets.EmoteSubType)
            {
                case EmoteSubType.Single:
                case EmoteSubType.SingleLoop:
                    animationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id);
                    break;
                case EmoteSubType.Double:
                case EmoteSubType.DoubleLoop:
                    animationCtrl.PlayDoubleEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl);
                    break;
                case EmoteSubType.PetSingle:
                case EmoteSubType.PetSingleLoop:
                    petAnimationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id);
                    break;
                case EmoteSubType.PetWithPlayer:
                case EmoteSubType.PetWithPlayerLoop:
                    petAnimationCtrl.PlayPetWithPlayerEmoteForUICharacter(emoteAssets.Id, animationCtrl);
                    break;

            }

            var classType = UniqueType.Get(emoteAssets.ResourceType, (int)emoteAssets.EmoteSubType);
            avatarCameraController.SetCameraZoom(classType);
            avatarCameraController.SetEmoteView(emoteAssets.Id);
        }

        #region Item相关

        private GashaponPriceItem GetItem()
        {
            if (cacheItems != null && cacheItems.Count != 0)
            {
                GashaponPriceItem cache = cacheItems.Last.Value;
                cacheItems.RemoveLast();
                cache.gameObject.SetActive(true);
                cache.transform.SetParent(scrollContent);
                return cache;
            }
            GashaponPriceItem newIns = Instantiate(itemPrefab, scrollContent);
            return newIns;
        }
        private void RecycleItem(GashaponPriceItem item)
        {
            if (cacheItems == null)
            {
                cacheItems = new LinkedList<GashaponPriceItem>();
            }

            cacheItems.AddLast(item);
            item.gameObject.SetActive(false);
            item.SetSelectStatus(false);
            item.transform.SetParent(cacheNode);
        }
        private void ClearItems()
        {
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    RecycleItem(items[i]);
                }

                items.Clear();
            }
        }

        #endregion

        private void OnTwistClick()
        {
            SendGashaponRequestOnce();
        }

        private void OnTwist10Click()
        {
            SendGashaponRequestTenTimes();
        }

        private void OnBackBtnClick()
        {
            UIManager.Inst.ClosePanel(this);
        }

        private void OnAnimViewBackClick()
        {
            SwitchStyle(GashaponThemePageStyle.PreviewStyle);
        }

        private void OnInfoClick()
        {
            ruleView.SetRuleEnable(curGashaponId, true, curGashaponData.CurrencyType);
        }

        private void OnGashaTwistAnimComplete(GashaponRsp gashaponRsp)
        {
            gashaponLuckValueView?.AdjustUI(gashaponRsp.lotteryInfo, curGashaponData.CurrencyType);
            SwitchStyle(GashaponThemePageStyle.ContinueTwist);

            if (gashaponRsp == null || gashaponRsp.rewardList == null)
            {
                return;
            }

            var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
            panel.ShowRewards(curGashaponId, gashaponRsp);

            if (UIManager.Inst.TryFindPanel<CreatorRewardPanel>(WindowId.CreatorWindow,PanelId.CreatorCenterPanel, out var creatorRewardPanel) && curGashaponData.CurrencyType == CurrencyType.GreenCoin)
            {
                creatorRewardPanel.Refresh();
            }
        }

        private bool CurrencyIsEnough(int currencyType,int price)
        {
            // Accountb
            bool canConvert = Enum.IsDefined(typeof(CurrencyType), currencyType);
            if (canConvert)
            {
                CurrencyType bType = (CurrencyType)currencyType;
                int value = AccountDataManager.Inst.BalanceInfo.GetAccountCount(bType);
                return value >= price;
            }

            //
            return false;
        }

        private void ShowCurrencyNoEnough(int currencyType,int price)
        {
            CurrencyType bType = (CurrencyType)currencyType;
            int count = AccountDataManager.Inst.BalanceInfo.GetAccountCount(bType);
            int needNum = price - count;
            switch (currencyType)
            {

                case (int)CurrencyType.Coin:
                    ExchangeCoinPanel exchangeCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    exchangeCoinPanel.SetData(CurrencyType.Coin, CurrencyType.Gem,needNum);
                    break;
                case (int)CurrencyType.Badge:
                    ExchangeCoinPanel badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    badgePanel.SetData(CurrencyType.Badge, CurrencyType.Gem,needNum);
                    break;
                case (int)CurrencyType.Gem:
                    // CurrencyType bType = (CurrencyType)currencyType;
                    // int count = AccountDataManager.Inst.BalanceInfo.GetAccountCount(bType);
                    // int needNum = price - count;
                    UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                    break;
            }
        }

        private void SetPetAvatarCamera() {
            avatarCameraController.ZoomUpperPosY = -0.1f;
            avatarCameraController.ZoomUppereCameraSize = 0.4f;
            avatarCameraController.ZoomWholePosY = -0.2f;
            avatarCameraController.ZoomWholeCameraSize = 0.7f;
            avatarCameraController.ZoomFootPosY = -0.46f;
            avatarCameraController.ZoomFootCameraSize = 0.3f;

            avatarCameraController.customEmoteCameraScale = 0.7f;
        }

        private void SetCharacterAvatarCamera() {
            avatarCameraController.ZoomUpperPosY = 0.3f;
            avatarCameraController.ZoomUppereCameraSize = 0.6f;
            avatarCameraController.ZoomWholePosY = 0;
            avatarCameraController.ZoomWholeCameraSize = 1f;
            avatarCameraController.ZoomFootPosY = -0.375f;
            avatarCameraController.ZoomFootCameraSize = 0.75f;
            avatarCameraController.customEmoteCameraScale = 1.0f;
        }


        private void OnMusicalInstrumentsPreview()
        {
            UIManager.Inst.SwapPanel(PanelId.TryMusicalInstrumentPanel, ((CharacterWrap)avatarWrapper).ChaData.Clone());
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

        #region 网络请求和回报

        protected void SendGashaponRequestOnce()
        {
            if (curGashaponData == null)
            {
                TipPanel.ShowToast("数据异常，请关闭重试");
                return;
            }

            if(!CurrencyIsEnough((int)curGashaponData.CurrencyType,curGashaponData.SinglePrice))
            {
                if (curGashaponData.CurrencyType == CurrencyType.GreenCoin)
                {
                    TipPanel.ShowToast("当前创作者币余额不足");
                    return;
                }
                ShowCurrencyNoEnough((int)curGashaponData.CurrencyType,curGashaponData.SinglePrice);
                return;
            }
            var gId = curGashaponId;
            GashaponDataManager.Inst.RequestGashapon(gId, OneTimeGasha, OnGashaOnceRsp);
        }

        protected void SendGashaponRequestTenTimes()
        {
            if (curGashaponData == null)
            {
                TipPanel.ShowToast("数据异常，请关闭重试");
                return;
            }

            int temPrice = GetRealTenPrice(curGashaponData);
            if(!CurrencyIsEnough((int)curGashaponData.CurrencyType,temPrice))
            {
                if (curGashaponData.CurrencyType == CurrencyType.GreenCoin)
                {
                    TipPanel.ShowToast("当前创作者币余额不足");
                    return;
                }
                ShowCurrencyNoEnough((int)curGashaponData.CurrencyType,temPrice);
                return;
            }
            var gId = curGashaponId;
            GashaponDataManager.Inst.RequestGashapon(gId, TemTimeGasha, OnGashaTenRsp);
        }

        public void OnGashaOnceRsp(GashaponRsp gashaponRsp)
        {
            if (!this || gashaponRsp == null || gashaponRsp.rewardList == null)
            {
                return;
            }

            SwitchStyle(GashaponThemePageStyle.TwistAnimStyle);
            twistAnimView.PlayOneTwistAnimation(gashaponRsp.rewardList, () =>
            {
                OnGashaTwistAnimComplete(gashaponRsp);

            });
        }

        protected void OnGashaTenRsp(GashaponRsp gashaponRsp)
        {
            if (!this || gashaponRsp == null || gashaponRsp.rewardList == null)
            {
                return;
            }

            SwitchStyle(GashaponThemePageStyle.TwistAnimStyle);
            twistAnimView.PlayTenTwistAnimation(gashaponRsp.rewardList, () =>
            {
                OnGashaTwistAnimComplete(gashaponRsp);
            });
        }

        public void TaskOnGashaOnceRsp(GashaponRsp gashaponRsp)
        {
            if (!this || gashaponRsp == null || gashaponRsp.rewardList == null)
            {
                return;
            }

            SwitchStyle(GashaponThemePageStyle.TwistAnimStyle);
            twistAnimView.PlayOneTwistAnimation(gashaponRsp.rewardList, () =>
            {
                OnGashaTwistAnimComplete(gashaponRsp);
                CloseSelf();
            });
        }

        public void TaskOnGashaTenRsp(GashaponRsp gashaponRsp)
        {
            if (!this || gashaponRsp == null || gashaponRsp.rewardList == null)
            {
                return;
            }

            SwitchStyle(GashaponThemePageStyle.TwistAnimStyle);
            twistAnimView.PlayTenTwistAnimation(gashaponRsp.rewardList, () =>
            {
                OnGashaTwistAnimComplete(gashaponRsp);
                CloseSelf();
            });
        }
        #endregion
    }
}
