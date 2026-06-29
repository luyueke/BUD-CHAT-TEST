using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML;
using Es;
using Game.Avatar;
using Game.Store;
using Game.Utils;
using GameData;
using GameData.PgcData;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Base;
using Game.Event;
using Game.MusicalInstrument;
using Game.Pet;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.CommonConfirm;
using UnityEngine;
using UnityEngine.UI;
using Message;
using Game.AnimationStudio;
using BUD.AnimPose;

namespace UI.UIPanels.FittingRoom
{
    public class SendGiftPanel : BasePanel<SendGiftPanel>
    {
        [SerializeField] internal Transform transBg;
        [Header("人物形象")]
        [SerializeField] internal Transform characterRoot;
        [SerializeField] internal AvatarCameraController avatarCameraController;
        [SerializeField] internal Button backButton;
        [SerializeField] internal Text editNameText;
        [Header("主标签UI")]
        [SerializeField] internal SendGiftMainTabs mainTabsUI;
        [Header("类别选择UI")]
        [SerializeField] internal ClassList classList;
        [Header("操作按钮UI")]
        [SerializeField] internal ItemInfo itemInfoUI;
        [Header("第二颜色选择UI")]
        [SerializeField] internal ColorPicker secondColorUI;
        [Header("Ugc推荐选择UI")]
        [SerializeField] internal SectionList sectionList;
        [Header("背包类别选择UI")]
        [SerializeField] internal BagTabs bagTabsUI;
        [Header("背包UGC来源UI")]
        [SerializeField] internal UgcSource ugcSourceUI;
        [Header("列表")]
        [SerializeField] internal SendGiftGridItemAdapter assetsList;

        [Header("主颜色选择UI")]
        [SerializeField] internal ColorPicker mainColorUI;
        [Header("调整按钮")]
        [SerializeField] internal AdjustNode adjustUI;
        [Header("调整界面")]
        [SerializeField] internal AdjustView adjustView;
        [Header("宠物大小调整界面")]
        [SerializeField] internal PetSizeAdjustView petSizeAdjustView;
        [Header("调整界面")]
        [SerializeField] internal OcList ocList;
        [Header("余额")]
        [SerializeField] internal SendGiftAccountWidgets accountWidgets;
        [Header("ugc搜索框")]
        [SerializeField] internal SearchUgcView searchUgcView;
        [Header("提示语")]
        [SerializeField] internal Text tipText;
        [Header("系列背景")]
        [SerializeField] internal ActivityCenterBgItem seriesBg;
        [Header("背包乐谱来源UI")]
        [SerializeField] internal UgcSource musicScoreSourceUI;
        [Header("乐谱试听乐器UI")]
        [SerializeField] internal SwitchMI switchMI;
        [Header("保存OC")]
        [SerializeField] internal LoadingButton saveOcButton;
		[Header("乐器试听乐谱UI")]
        [SerializeField] internal SwitchMS switchMS;
        [Header("Bundle详情Item列表")]
        [SerializeField] internal BundleItemsList bundleItemsList;

        [Header("Ugc姿势动画单双人选择")]
        [SerializeField] internal AnimSubTypeSelect animSubTypeUI;

        [Header("左边人物布局")]
        [SerializeField] internal RectTransform leftRoot;

        [SerializeField] internal Button sendGiftBtn;
        [SerializeField] internal Button requestGiftBtn;
        [SerializeField] internal GameObject operationUI;
        [SerializeField] internal Text giftPriceText;
        [SerializeField] internal Image giftPriceIcon;
        [SerializeField] internal GameObject toolsUI;
        [SerializeField] internal Image toolsIcon;
        [SerializeField] internal GameObject disableUI;
        [SerializeField] internal Text disableText;
        [SerializeField] internal GameObject ugcUI;
        [SerializeField] internal RemoteImageBehaviour ugcRemoteImage;

        [Header("双人牵手动作等")]
        [SerializeField] internal SwitchAnimView switchAnimView;

        internal CharacterWrap otherCharacterWrap;
        internal BaseAvatarWrapper avatarWrapper;
        internal BaseAvatarData saveAvatarData;
        internal bool isCharacterFittingRoom = true;

        #region 人物相关


        internal PlayerAnimationCtrl animationCtrl;
        internal AnimIKController animationCtrlIK;
        internal PlayerHoldBehaviour playerHold;
        internal PlayerAnimationCtrl otherAnimationCtrl;
        internal AnimIKController otherAnimationCtrlIK;
        internal PlayMusicScoreBev playMusicScoreBev;

        #endregion

        #region 宠物相关
        internal PetAnimationCtrl petAnimationCtrl;
        internal AnimIKController petAnimationCtrlIK;
        #endregion



        private Dictionary<SendGiftMainTabs.Tab, SendGiftBaseScene> mainSceneDict;

        private SendGiftBaseScene scene;
        public Action<CharacterData> OnCloseAction;

        private AmbientLightSetting _srcLightSetting;
        private bool _srcHallLightVisible;
        private bool _srcGameSceneLightVisible;
        private bool _srcPreviewSceneLightVisible;
        private SendGiftMainTabs.Tab currentTab = SendGiftMainTabs.Tab.BUD;

        public void CloseAllSubUI()
        {
            operationUI.gameObject.SetActive(false);
            secondColorUI.gameObject.SetActive(false);
            bagTabsUI.gameObject.SetActive(false);
            assetsList.gameObject.SetActive(false);
            mainColorUI.gameObject.SetActive(false);
            adjustUI.gameObject.SetActive(false);
            adjustView.gameObject.SetActive(false);
            sectionList.gameObject.SetActive(false);
            ugcSourceUI.gameObject.SetActive(false);
            itemInfoUI.gameObject.SetActive(false);
            ocList.gameObject.SetActive(false);
            tipText.gameObject.SetActive(false);
            seriesBg.gameObject.SetActive(false);
            musicScoreSourceUI.gameObject.SetActive(false);
            switchMI?.gameObject.SetActive(false);
            saveOcButton?.gameObject.SetActive(false);
            saveOcButton?.SetClickAble(true);
            switchMS?.gameObject.SetActive(false);
            bundleItemsList?.gameObject.SetActive(false);
            animSubTypeUI?.gameObject.SetActive(false);
            petSizeAdjustView.gameObject.SetActive(false);
            switchAnimView?.gameObject.SetActive(false);
            avatarCameraController.roleCamera.backgroundColor = new Color(1, 1, 1, 0);
            ResetLastTryOn(ResourceType.ErrResourceType);

        }

        public override void OnCreate()
        {
            // 试衣间打开检查一次背包数据
            Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck);

            MessageHelper.AddListener<string>(MessageName.ShowToast, ShowToast);

            MessageHelper.AddListener<string>(MessageName.SelectAssetItem, SelectAssetItem);

            // 刷新余额
            AccountDataManager.Inst.BalanceInfo.Refresh();

            backButton.onClick.AddListener(OnBackClick);
            playMusicScoreBev = GetComponent<PlayMusicScoreBev>();

            adjustUI.OnClick = () =>
            {
                ResetLastTryOn(ResourceType.ErrResourceType);
                adjustView.gameObject.SetActive(true);
            };
            saveOcButton.onClick.AddListener(SaveOc);

            mainTabsUI.SetCallback(OnMainTabs);
            mainSceneDict = new();

            classList.onValueChangedBase = OnClassSelected;

            itemInfoUI.OnHeadClick = OnHeadClick;
            // discountCardContainer.GetComponent<CButton>().onClick.AddListener(OnDiscountCardClick);
            editNameText.text = AccountDataManager.Inst.PetInfo.nickname;

            switchAnimView?.SetCallBack(OnSwitchAnimItemClick);
        }

        private void OnSwitchAnimItemClick(SpecialAnim anim)
        {
            var emoteId = switchAnimView.GetPgcId();
            if (!string.IsNullOrEmpty(emoteId))
            {
                animationCtrl.PlayLinkEmoteForUICharacter(emoteId, anim,otherAnimationCtrl);
            }
        }

        private void OnDiscountCardClick() {
            UIManager.Inst.OpenPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel, ActivityId.MusicAndDanceCommunity.ToString());
        }


        private void InitCharacterWrapper() {
            var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;

            var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
            animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            animationCtrl.CheckAndOverrideSpecialAnim();
            animationCtrlIK = characterWrapper.Avatar.GetComponent<AnimIKController>();
            playerHold = characterWrapper.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
            avatarCameraController.RotateTarget = characterRoot;

            otherCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(AccountDataManager.Inst.UserInfo.otherAvatarInfo, characterRoot);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherAnimationCtrlIK = otherCharacterWrap.Avatar.GetComponent<AnimIKController>();
            avatarWrapper = characterWrapper;
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
            saveAvatarData = saveCharacterData;
        }
        private void InitPetWrapper() {
            var savePetData = AccountDataManager.Inst.PetInfo.avatarInfo;

            var petWrapper = PetAvatarController.Inst.CreateUIAvatarWithIKController(savePetData, characterRoot);
            petAnimationCtrl = petWrapper.Avatar.GetComponentInChildren<PetAnimationCtrl>();
            petAnimationCtrlIK = petWrapper.Avatar.GetComponent<AnimIKController>();
            characterRoot.localScale = Vector3.one * 1.32f;
            characterRoot.localPosition = new Vector3(0, -0.5f, 0);

            avatarCameraController.RotateTarget = characterRoot;
            otherCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(AccountDataManager.Inst.UserInfo.avatarInfo, characterRoot);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherAnimationCtrlIK = otherCharacterWrap.Avatar.GetComponent<AnimIKController>();
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
            avatarWrapper = petWrapper;
            saveAvatarData = savePetData;
        }

        public void JumpTo(SendGiftMainTabs.Tab tab, int classType = -10000)
        {
            mainTabsUI.DefualtOn(tab);
            if (classType != -10000) classList.DefualtOn(classType);
        }
        public void OpenSendGift(GoodsData goodsData)
        {
            int giftType = (int)goodsData.GiftType;
            UIManager.Inst.OpenPanel(PanelId.SendGiftFriendPanel, goodsData,giftType);
        }

        public void OpenRequetsGift(GoodsData goodsData)
        {
            int giftType = (int)goodsData.GiftType;
            UIManager.Inst.OpenPanel(PanelId.RequestGiftFriendPanel,goodsData,giftType);
        }
        private void OnHeadClick()
        {
            // 点击详情停止乐谱播放
            playMusicScoreBev.StopPLay();
        }


        public override void OnWindowBeCovered(bool isCover)
        {
            if (isCover)
            {
                if (!this || !this.transform) return;
                DealBaseLayout2D(false);
                DealBaseLayout3D(false);
            }
        }

        public override void OnWindowShow()
        {
            if (!this || !this.transform) return;
            DealBaseLayout2D(true);
            DealBaseLayout3D(true);
        }

        private void OnMainTabs(SendGiftMainTabs.Tab tab)
        {
            // 动态修改 leftRoot 的 Right 值
            if (leftRoot != null)
            {
                float newRightValue = (tab == SendGiftMainTabs.Tab.Ugc || tab == SendGiftMainTabs.Tab.Tool)
                    ? 1210f
                    : 1349f;

                // 设置新的 Right 值
                Vector2 offsetMax = leftRoot.offsetMax;
                offsetMax.x = -newRightValue; // Right 值需要取负
                leftRoot.offsetMax = offsetMax;
            }

            this.currentTab = tab;
            classList.gameObject.SetActive(tab != SendGiftMainTabs.Tab.Ugc && tab != SendGiftMainTabs.Tab.Tool);
            searchUgcView.gameObject.SetActive(tab == SendGiftMainTabs.Tab.Ugc);
            accountWidgets.SetMainTabs(tab);
            scene?.Exit();
            scene = mainSceneDict[tab];
            scene.Enter();


        }

        private void OnClassSelected(ClassData classData)
        {
            avatarCameraController.SetCameraZoom(classData.Id);

            CancelEmote();
            CancelPreviewMusicScore();
            CancelPreviewMusicalInstrument();
        }

        private GoodsData CreateNewModel(int index)
        {
            return scene.CreateNewModel(index);
        }

        public void OnBackClick()
        {
            UIManager.Inst.ClosePanel(this);
        }

        public override void OnShow(params object[] args)
        {

            if (args.Length == 0) {
                isCharacterFittingRoom = true;
                InitCharacterWrapper();
            } else {
                if (args[0] is bool) {
                    isCharacterFittingRoom = !(bool)args[0];
                } else {
                    isCharacterFittingRoom = true;
                }
                if (isCharacterFittingRoom) {
                    InitCharacterWrapper();
                } else {
                    InitPetWrapper();
                }
            }
            // Oc初始化
            ocList.Init(isCharacterFittingRoom);
            MessageHelper.AddListener<string>(MessageName.ShowToast, ShowToast);
            MessageHelper.AddListener<string>(MessageName.SelectAssetItem, SelectAssetItem);

            _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
            _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
            _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
            _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();

            assetsList.gameObject.SetActive(true);
            assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
            assetsList.Init();

            mainSceneDict.Add(SendGiftMainTabs.Tab.BUD, new SendGiftBUDScene(this));
            mainSceneDict.Add(SendGiftMainTabs.Tab.Action, new SendGiftActionScene(this));
            mainSceneDict.Add(SendGiftMainTabs.Tab.Ugc, new SendGiftUGCScene(this));
            mainSceneDict.Add(SendGiftMainTabs.Tab.Tool, new SendGiftToolScene(this));
            mainTabsUI.DefualtOn(SendGiftMainTabs.Tab.BUD);

            Message.MessageHelper.AddListener(Message.MessageName.OnTryListenMIChange, OnTryListenMIChange);
            Message.MessageHelper.AddListener(Message.MessageName.OnTryListenMSChange, OnTryListenMSChange);
        }

        private void ShowToast(string toast)
        {
            TipPanel.ShowToast(toast);
        }

        private void SelectAssetItem(string ProductId)
        {
            for (int i = 0; i < assetsList.Data.List.Count; i++)
            {
                var item = assetsList.Data.List[i] as GoodsData;
                if (item.Id == ProductId)
                {
                    var holder = assetsList.GetCellViewsHolder(i) as SendGiftItemHolder;
                    holder.item.OnItemClick();
                    break;
                }
            }
            foreach (var item in assetsList.Data.List)
            {
                if ((item as GoodsData).Id == ProductId)
                {

                    break;
                }
            }
        }

        public override void OnHidden()
        {
            MessageHelper.RemoveListener<string>(MessageName.ShowToast, ShowToast);

            MessageHelper.RemoveListener<string>(MessageName.SelectAssetItem, SelectAssetItem);

            AmbientLightManager.Inst.CloseUILight(_srcLightSetting);
            AmbientLightManager.Inst.RevertHallLight(_srcHallLightVisible);
            AmbientLightManager.Inst.RevertGameSceneLight(_srcGameSceneLightVisible);
            AmbientLightManager.Inst.RevertPreviewLight(_srcPreviewSceneLightVisible);

            foreach (var kv in mainSceneDict)
            {
                kv.Value.Destory();
            }

            if (isCharacterFittingRoom) {
                animationCtrl.ResetEmoteForUICharacter();
                otherAnimationCtrl.gameObject.SetActive(false);
                otherAnimationCtrl.ResetEmoteForUICharacter();
            }
            ocList?.RemoveListener();

            Message.MessageHelper.RemoveListener(Message.MessageName.OnTryListenMIChange, OnTryListenMIChange);
            Message.MessageHelper.RemoveListener(Message.MessageName.OnTryListenMSChange, OnTryListenMSChange);
        }

        #region 换装

        internal void ChangeOc(OcServerData ocServerData)
        {
            if (avatarWrapper is CharacterWrap characterWrap) {
                characterWrap.SetCharacterData(CharacterData.DeserializeObject(ocServerData.ocInfo.avatarJson));
                saveAvatarData = characterWrap.ChaData.Clone();
            } else if (avatarWrapper is PetWrap petWrap)
            {
                petWrap.SetData(PetData.DeserializeObject(ocServerData.ocInfo.avatarJson));
                saveAvatarData = petWrap.Data.Clone();
            }
        }

        /// <summary>
        /// 试穿 重置参数
        /// </summary>
        /// <param name="goodsData"></param>
        internal ResourceType lastTryOnType = ResourceType.ErrResourceType;
        internal void ResetLastTryOn(ResourceType nowType)
        {
            switch (lastTryOnType)
            {
                case ResourceType.Avatar:
                case ResourceType.UgcAvatar:
                case ResourceType.PGCPetAvatar:
                case ResourceType.UGCPetAvatar:
                    CancelTryOn();
                    break;
                case ResourceType.MusicScore:
                    if (nowType != lastTryOnType)
                    {
                        CancelPreviewMusicScore();
                        CancelPreviewMusicalInstrument();
                        CancelTryOn();
                        CancelEmote();
                    }
                    break;
                case ResourceType.Emote:
                    CancelEmote();
                    break;
                case ResourceType.UgcEmote:
                    CancelEmote();
                    break;
                case ResourceType.UgcPose:
                    break;
            }

            lastTryOnType = nowType;
        }

        internal void ResetLastTryOn(GoodsData goodsData)
        {
            switch (goodsData.GoodsType)
            {
                case GoodsType.BundleUgc:
                    var previewType = ResourceType.ErrResourceType;
                    foreach (AvatarAssetsData bundleItem in goodsData.Assets)
                    {
                        if (previewType == ResourceType.ErrResourceType)
                        {
                            previewType = bundleItem.ResourceType;
                        }

                        if (bundleItem.AvatarSubType == AvatarSubType.MusicalInstrument)
                        {
                            ResetLastTryOn(ResourceType.ErrResourceType);
                            previewType = ResourceType.MusicScore;
                        }
                    }
                    ResetLastTryOn(previewType);
                    break;
                case GoodsType.SinglePgc:
                case GoodsType.SingleUgc:
                    var asset = goodsData.GetFirstAsset<AssetsData>();
                    if (asset == null)
                    {
                        ResetLastTryOn(ResourceType.ErrResourceType);
                    }
                    else if (asset.ResourceType == ResourceType.Avatar || asset.ResourceType == ResourceType.UgcAvatar)
                    {
                        var avatarAsset = asset as AvatarAssetsData;
                        if (avatarAsset.AvatarSubType == AvatarSubType.MusicalInstrument)
                        {
                            ResetLastTryOn(ResourceType.MusicScore);
                        }
                        else
                        {
                            ResetLastTryOn(asset.ResourceType);
                        }
                    }
                    else
                    {
                        ResetLastTryOn(asset.ResourceType);
                    }
                    break;
            }
        }

        internal void EnterPreviewMusicScore()
        {
            ResetLastTryOn(ResourceType.MusicScore);
            PreviewMusicScore();
        }

        internal void ShowToolView(GoodsData goodsData)
        {
            //todo
            LoggerUtils.Log("ShowToolView :" + goodsData);

        }

        internal void TryOn(GoodsData goodsData)
        {
            ResetLastTryOn(goodsData);
            for (int i = 0, C = goodsData.Assets.Count; i < C; i++)
            {
                var assets = goodsData.Assets[i];
                switch (assets.ResourceType)
                {
                    case ResourceType.PGCPetAvatar:
                        var petPgcAssets = assets as PGCAssetsData;
                        var petSubType = UniqueType.GetPGCPetAvatar(petPgcAssets.AvatarSubType);
                        var petConfig = DataTables.GetPetAvatarCommonData(goodsData.GetPgcId());
                        goodsData.Loading(true);
                        avatarWrapper.ChangePart(petSubType, petPgcAssets.Id, () => goodsData.Loading(false));
                        avatarWrapper.ChangeColor(petSubType, petConfig.defaultColor);
                        avatarWrapper.Move(petSubType, petConfig.pDef);
                        avatarWrapper.Rotate(petSubType, petConfig.rDef);
                        avatarWrapper.Scale(petSubType, petConfig.sDef);
                        avatarWrapper.HVScale(petSubType, petConfig.vhSDef);
                        avatarWrapper.SetLeftOrRight(petSubType, petConfig.leftRightType);
                        break;
                    case ResourceType.UGCPetAvatar:
                        var petUgcAssets = assets as UGCAssetsData;
                        goodsData.Loading(false);
                        avatarWrapper.ChangeUGCPart(petUgcAssets.UgcInfo.skinInfo, () => goodsData.Loading(false));
                        break;
                    case ResourceType.Avatar:
                        var pgcAssets = assets as PGCAssetsData;
                        var subType = UniqueType.GetAvatar(pgcAssets.AvatarSubType);
                        var config = DataTables.GetAvatarCommonData(goodsData.GetPgcId(i));
                        goodsData.Loading(true);
                        avatarWrapper.ChangePart(subType, pgcAssets.Id, () => goodsData.Loading(false));
                        avatarWrapper.ChangeColor(subType, config.defaultColor);
                        avatarWrapper.Move(subType, config.pDef);
                        avatarWrapper.Rotate(subType, config.rDef);
                        avatarWrapper.Scale(subType, config.sDef);
                        avatarWrapper.HVScale(subType, config.vhSDef);
                        avatarWrapper.SetLeftOrRight(subType, config.leftRightType);
                        if (pgcAssets.AvatarSubType == AvatarSubType.MusicalInstrument) PreviewMusicalInstrument(true, pgcAssets.Id);
                        animationCtrl.CheckAndOverrideSpecialAnim();
                        break;
                    case ResourceType.UgcAvatar:
                        var ugcAssets = assets as UGCAssetsData;
                        goodsData.Loading(false);
                        avatarWrapper.ChangeUGCPart(ugcAssets.UgcInfo.skinInfo, () => goodsData.Loading(false));
                        if (ugcAssets.UgcInfo.skinInfo.subType == (int)AvatarSubType.MusicalInstrument) PreviewMusicalInstrument(false, ugcAssets.UgcInfo.skinInfo.id);
                        break;
                    case ResourceType.Emote:
                        var emoteAssets = assets as EmoteAssetsData;
                        avatarCameraController.SetEmoteView(emoteAssets.Id);
                        switch (emoteAssets.EmoteSubType)
                        {
                            case EmoteSubType.Single:
                            case EmoteSubType.SingleLoop:
                                goodsData.Loading(true);
                                animationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id, OnDownloadOver: () => goodsData.Loading(false));
                                break;
                            case EmoteSubType.Double:
                            case EmoteSubType.DoubleLoop:
                                goodsData.Loading(true);
                                animationCtrl.PlayDoubleEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl, OnDownloadOver: () => goodsData.Loading(false));
                                break;
                            case EmoteSubType.PetSingle:
                            case EmoteSubType.PetSingleLoop:
                                goodsData.Loading(true);
                                petAnimationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id, OnDownloadOver: () => goodsData.Loading(false));
                                break;
                            case EmoteSubType.PetWithPlayer:
                            case EmoteSubType.PetWithPlayerLoop:
                                goodsData.Loading(true);
                                petAnimationCtrl.PlayPetWithPlayerEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl, OnDownloadOver: () => goodsData.Loading(false));
                                break;
                        }
                        break;
                    case ResourceType.MusicScore:
                        var msInfo = assets as MusicScoreAssetsData;
                        PreviewMusicScore(() =>
                        {
                            MusicalInstrumentManager.Inst.CheckInstrumentCanPlayMusicScore(playerHold.curToneInfo, msInfo.UgcInfo.musicScoreInfo, () =>
                             {
                                 TipPanel.ShowToast("这个乐谱是22音，你的乐器是15音，听起来可能会少音哦");
                             });
                            MusicalInstrumentManager.Inst.CheckInstrumentIsUgcToneAndShowToast(playerHold.curToneInfo);
                            playMusicScoreBev.ChangePlayType(PlayMusicScoreBev.PlayType.Loop);
                            playMusicScoreBev.StartPLay(msInfo.UgcInfo.musicScoreInfo, OnMusicScorePlaying);
                        });
                        break;
                    case ResourceType.UgcPose:
                        var poseInfo = assets as UgcPoseAssetsData;
                        avatarCameraController.SetEmoteView((UgcAnimSubType)poseInfo.UgcInfo.poseInfo.poseType);
                        switch ((UgcPoseSubType)poseInfo.UgcInfo.poseInfo.poseType)
                        {
                            case UgcPoseSubType.Single:
                            case UgcPoseSubType.Double:
                                animationCtrlIK.Pose(poseInfo.UgcInfo.poseInfo, otherAnimationCtrlIK);
                                break;
                            case UgcPoseSubType.PetSingle:
                            case UgcPoseSubType.PetWithPlayer:
                                petAnimationCtrlIK.Pose(poseInfo.UgcInfo.poseInfo, otherAnimationCtrlIK);
                                break;
                        }
                        break;
                    case ResourceType.UgcEmote:
                        var ueInfo = assets as UgcAnimAssetsData;
                        avatarCameraController.SetEmoteView((UgcAnimSubType)ueInfo.UgcInfo.animInfo.animType);
                        switch ((UgcAnimSubType)ueInfo.UgcInfo.animInfo.animType)
                        {
                            case UgcAnimSubType.Single:
                            case UgcAnimSubType.Double:
                                animationCtrlIK.Play(ueInfo.UgcInfo.animInfo, otherAnimationCtrlIK, CancelEmote);
                                break;
                            case UgcAnimSubType.PetSingle:
                            case UgcAnimSubType.PetWithPlayer:
                                petAnimationCtrlIK.Play(ueInfo.UgcInfo.animInfo, otherAnimationCtrlIK, CancelEmote);
                                break;
                        }
                        break;
                }
            }
        }

        internal void PreviewEmote(GoodsData goodsData)
        {
            CancelEmote();

            for (int i = 0, C = goodsData.Assets.Count; i < C; i++)
            {
                var assets = goodsData.Assets[i];
                if (assets.ResourceType == ResourceType.Emote)
                {
                    var emoteAssets = assets as EmoteAssetsData;
                    avatarCameraController.SetEmoteView(emoteAssets.Id);
                    switch (emoteAssets.EmoteSubType)
                    {
                        case EmoteSubType.Single:
                        case EmoteSubType.SingleLoop:
                            goodsData.Loading(true);
                            animationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id, OnDownloadOver: () => goodsData.Loading(false));
                            break;
                        case EmoteSubType.Double:
                        case EmoteSubType.DoubleLoop:
                            goodsData.Loading(true);
                            animationCtrl.PlayDoubleEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl, OnDownloadOver: () => goodsData.Loading(false));
                            break;
                        case EmoteSubType.PetSingle:
                        case EmoteSubType.PetSingleLoop:
                            goodsData.Loading(true);
                            petAnimationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id, OnDownloadOver: () => goodsData.Loading(false));
                            break;
                        case EmoteSubType.PetWithPlayer:
                        case EmoteSubType.PetWithPlayerLoop:
                            goodsData.Loading(true);
                            petAnimationCtrl.PlayPetWithPlayerEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl, OnDownloadOver: () => goodsData.Loading(false));
                            break;
                    }
                }
            }
        }

        internal void CancelTryOn()
        {
            if (avatarWrapper is CharacterWrap characterWrap) {
                characterWrap.RefreshAvatar(saveAvatarData as CharacterData);
            } else if (avatarWrapper is PetWrap petWrap) {
                petWrap.RefreshAvatar(saveAvatarData as PetData);
            }
        }

        internal void CancelEmote()
        {
            avatarCameraController.ResetEmoteView();
            avatarCameraController.SetCameraZoom(0);
            if (isCharacterFittingRoom) {
                animationCtrl.ResetEmoteForUICharacter();
                otherAnimationCtrl.ResetEmoteForUICharacter();
                animationCtrlIK.StopAnimAndResetJointNode();
                animationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
                otherAnimationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
                ResetIKPosition();
                otherAnimationCtrl.UpdateAnim(0);
                otherAnimationCtrl.gameObject.SetActive(false);
            }
            else
            {
                petAnimationCtrl.ResetEmoteForUICharacter();
                otherAnimationCtrl.ResetEmoteForUICharacter();
                petAnimationCtrlIK.StopAnimAndResetJointNode();
                petAnimationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
                otherAnimationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
                ResetPetIKPosition();
                otherAnimationCtrl.UpdateAnim(0);
                otherAnimationCtrl.gameObject.SetActive(false);
            }

        }

        internal void ResetIKPosition()
        {
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
            animationCtrlIK.transform.localPosition = poseModeData.RoleDefPos[0];
            animationCtrlIK.transform.localEulerAngles = Vector3.zero;
            animationCtrlIK.transform.localScale = Vector3.one;
            animationCtrlIK.transform.parent.localPosition = poseModeData.EditPos[0];
            animationCtrlIK.transform.parent.localEulerAngles = Vector3.zero;
            animationCtrlIK.transform.parent.localScale = Vector3.one;

            otherAnimationCtrlIK.transform.localPosition = poseModeData.RoleDefPos[0];
            otherAnimationCtrlIK.transform.localEulerAngles = Vector3.zero;
            otherAnimationCtrlIK.transform.localScale = Vector3.one;
            otherAnimationCtrlIK.transform.parent.localPosition = poseModeData.EditPos[0];
            otherAnimationCtrlIK.transform.parent.localEulerAngles = Vector3.zero;
            otherAnimationCtrlIK.transform.parent.localScale = Vector3.one;
        }

        internal void ResetPetIKPosition()
        {
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetSingle);
            petAnimationCtrlIK.transform.localPosition = poseModeData.RoleDefPos[0];
            petAnimationCtrlIK.transform.localEulerAngles = Vector3.zero;
            // petAnimationCtrlIK.transform.localScale = Vector3.one;
            petAnimationCtrlIK.transform.parent.localPosition = poseModeData.EditPos[0];
            petAnimationCtrlIK.transform.parent.localEulerAngles = Vector3.zero;
            petAnimationCtrlIK.transform.parent.localScale = Vector3.one;

            var poseModeData1 = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetWithPlayer);
            otherAnimationCtrlIK.transform.localPosition = poseModeData1.RoleDefPos[0];
            otherAnimationCtrlIK.transform.localEulerAngles = Vector3.zero;
            otherAnimationCtrlIK.transform.localScale = Vector3.one;
            otherAnimationCtrlIK.transform.parent.localPosition = poseModeData1.EditPos[0];
            otherAnimationCtrlIK.transform.parent.localEulerAngles = Vector3.zero;
            otherAnimationCtrlIK.transform.parent.localScale = Vector3.one;
        }

        internal void TakeOff(int resType)
        {
            avatarWrapper.TakeOff(avatarWrapper.GetMutexType(resType));
            avatarWrapper.TakeOff(resType);
            if (isCharacterFittingRoom) {
                saveAvatarData = avatarWrapper.GetData<CharacterData>().Clone();
            } else {
                saveAvatarData = avatarWrapper.GetData<PetData>().Clone();
            }

        }

        internal void ChangeColor(int resType, Color color)
        {
            avatarWrapper.ChangeColor(resType, "#" + ColorUtility.ToHtmlStringRGB(color));
            RefreshSaveData();
        }

        internal void ChangeSize(int resType, Vec3 size)
        {
            avatarWrapper.Scale(resType, size);
            RefreshSaveData();
        }

        internal void ChangeMove(int resType, Vec3 pos)
        {
            avatarWrapper.Move(resType, pos);
            RefreshSaveData();
        }

        internal void ChangeRotate(int resType, Vec3 rot)
        {
            avatarWrapper.Rotate(resType, rot);
            RefreshSaveData();
        }

        internal void ChangeHVSize(int resType, Vec3 size)
        {
            avatarWrapper.HVScale(resType, size);
            RefreshSaveData();
        }

        internal void ChangeLeftOrRight(int resType, int leftOrRight)
        {
            avatarWrapper.SetLeftOrRight(resType, leftOrRight);
            RefreshSaveData();
        }

        private void RefreshSaveData() {
            if (avatarWrapper is CharacterWrap characterWrap) {
                saveAvatarData = characterWrap.ChaData.Clone();
            } else if (avatarWrapper is PetWrap petWrap) {
                saveAvatarData = petWrap.Data.Clone();
            }
        }

        public void ShowTip(string str)
        {
            tipText.gameObject.SetActive(true);
            tipText.SetLocalText(str);
        }

        internal void OnMusicScorePlaying(List<SyllablePlayData> data)
        {
            playerHold.PlayMusicSyllable(data);
        }

        internal string previewMusicInstrumentId = "";
        internal bool isOk;
        internal Action previewMusicScoreAction;
        internal Dictionary<string, DetailRsp> detailCache = new Dictionary<string, DetailRsp>();
        internal void PreviewMusicScore(bool isPgc, string id, Action action = null)
        {
            previewMusicScoreAction = action;
            if (previewMusicInstrumentId == id)
            {
                if (isOk)
                {
                    action?.Invoke();
                }
                return;
            }
            previewMusicInstrumentId = id;
            if (isPgc)
            {
                ShowSwitch(switchMI);
                avatarWrapper.ChangePart(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), id, () =>
                {
                    if (previewMusicInstrumentId == id) playerHold.PreviewPGCInstrument(id);
                    switchMI.SetTarget(id, "");
                    isOk = true;
                    previewMusicScoreAction?.Invoke();
                });
                return;
            }

            GetMIDetailInfo(id, (rspData) =>
            {
                ShowSwitch(switchMI);
                avatarWrapper.ChangeUGCPart(rspData.skinInfo, () =>
                {
                    if (previewMusicInstrumentId == id) playerHold.PreviewUGCInstrument(rspData.skinActionInfo.instrumentInfo);
                    switchMI.SetTarget(id, rspData.skinInfo.cover);
                    isOk = true;
                    previewMusicScoreAction?.Invoke();
                });

            },
            () =>
            {
                // ugc乐器拿不到，回到默认的话筒
                AccountDataManager.Inst.TryListenMIData = new();
                if (!AccountDataManager.Inst.TryListenMIData.isPgc)
                {
                    ShowSwitch(switchMI.gameObject);
                    switchMI.gameObject.SetActive(true);
                    return;
                }
                previewMusicInstrumentId = "";
                PreviewMusicScore(action);
            });
        }

        internal void PreviewMusicScore(Action action = null)
        {
            var isPgc = AccountDataManager.Inst.TryListenMIData.isPgc;
            var id = AccountDataManager.Inst.TryListenMIData.id;
            PreviewMusicScore(isPgc, id, action);
        }

        internal void ShowSwitch(UnityEngine.Object go)
        {
            switchMI.gameObject.SetActive(go == switchMI);
            switchMS.gameObject.SetActive(go == switchMS);
        }

        internal void GetMIDetailInfo(string id, Action<DetailRsp> suc, Action fail)
        {
            if (detailCache.ContainsKey(id))
            {
                suc?.Invoke(detailCache[id]);
                return;
            }
            JObject req = new JObject()
            {
                ["idList"] = id,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetClothesBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
            {
                if (this == null || previewMusicInstrumentId != id || playerHold == null) return;
                BatchDetailRsp rspData = JsonConvert.DeserializeObject<BatchDetailRsp>(content);
                if (rspData.skinList == null || rspData.skinList.Count == 0 || rspData.skinList[0].skinActionInfo == null) return;
                detailCache[id] = rspData.skinList[0];
                suc?.Invoke(detailCache[id]);
            },
            (msg) =>
            {
                fail?.Invoke();
            });
        }

        internal void CancelPreviewMusicScore()
        {
            isOk = false;
            previewMusicInstrumentId = "";
            if (isCharacterFittingRoom) {
                playerHold.StopPreviewInstrument();
                playMusicScoreBev.StopPLay();
            }

            switchMI.gameObject.SetActive(false);
        }

        internal void OnTryListenMIChange()
        {
            if (!switchMI.gameObject.activeSelf) return;
            PreviewMusicScore();
        }

        internal string previewMusicScoreId;
        internal void PreviewMusicalInstrument(bool isPgc, string miId)
        {
            PreviewMusicScore(isPgc, miId, () =>
            {
                PreviewMusicalInstrument();
            });
        }

        internal void PreviewMusicalInstrument()
        {
            var id = AccountDataManager.Inst.TryListenMSData.id;
            previewMusicScoreId = id;
            if (string.IsNullOrEmpty(id))
            {
                ShowSwitch(switchMS);
                switchMS.SetTarget(id, "");
                playMusicScoreBev.StopPLay();
                playMusicScoreBev.ChangePlayType(PlayMusicScoreBev.PlayType.Once);
                playMusicScoreBev.StartPLay(playMusicScoreBev.DefaultMS, OnMusicScorePlaying);
                return;
            }

            ShowSwitch(null);
            AssetsDataManager.GetMusicScoreInfo(id, (isSuccess, RecommendItemData) =>
            {
                if (previewMusicScoreId != id) return;
                if (isSuccess)
                {
                    playMusicScoreBev.StopPLay();
                    playMusicScoreBev.ChangePlayType(PlayMusicScoreBev.PlayType.Once);
                    playMusicScoreBev.StartPLay(RecommendItemData.musicScoreInfo, OnMusicScorePlaying);
                    switchMS.gameObject.SetActive(true);
                    ShowSwitch(switchMS);
                    switchMS.SetTarget(id, RecommendItemData.musicScoreInfo.cover);
                }
                else
                {
                    // ugc乐谱拿不到，回到默认的
                    AccountDataManager.Inst.TryListenMSData = new();
                    previewMusicScoreId = "";
                    PreviewMusicalInstrument(true, "");
                }
            });
        }

        internal void CancelPreviewMusicalInstrument()
        {
            previewMusicScoreId = "";
            playMusicScoreBev.StopPLay();
            switchMS.gameObject.SetActive(false);
        }

        internal void OnTryListenMSChange()
        {
            if (!switchMS.gameObject.activeSelf) return;
            PreviewMusicalInstrument();
        }

        #endregion

        private Camera photoCamera;
        public void SaveOc()
        {
            saveOcButton.ShowLoading();
            saveOcButton.SetClickAble(false);
            OnClickSave();
        }

        private void OnClickSave()
        {
            if (!ocList.CanSave())
            {
                ShowOcButton();
                UIManager.Inst.OpenPanel<BuyOcPanel>(PanelId.BuyOcPanel,isCharacterFittingRoom);
                return;
            }

            PlayerAnimationCtrl playAnim = null;
            CharacterPartData partData = null;
            if (avatarWrapper is CharacterWrap characterWrap) {
                playAnim = characterWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
                playAnim.Init(characterWrap);
                playAnim.Play("idle");
                partData = avatarWrapper.GetPartData(UniqueType.GetAvatar(AvatarSubType.Eyes));
            } else {
                partData = avatarWrapper.GetPartData(UniqueType.GetPGCPetAvatar(AvatarSubType.Eyes));
            }

            void Save()
            {
                photoCamera = Loader.Load<GameObject>("Assets/Arts/Prefabs/CharacterUICamera.prefab")
                    .Instantiate(avatarWrapper.Avatar.transform).GetComponent<Camera>();
                photoCamera.transform.localPosition = new Vector3(0, isCharacterFittingRoom ? 0.5f : 0.35f, 1);
                photoCamera.transform.localEulerAngles = new Vector3(0, 180, 0);
                photoCamera.orthographicSize = photoCamera.orthographicSize * ResolutionAutoFit.CameraScale *
                                               avatarWrapper.Avatar.transform.localScale.x;

                StartCoroutine(TakeMatchPhoto());
            }

            if (partData == null || partData.IsNull())
            {
                Save();
                return;
            }

            AvatarCommonData bData = null;
            if (isCharacterFittingRoom)
            {
                bData = DataTables.GetAvatarCommonData(partData.Id);
            }
            else
            {
                bData = DataTables.GetPetAvatarCommonData(partData.Id);
            }
            Loader.LoadAsyncOrSync<AnimationClip>(bData.aniPath + ".anim", (isSuc, wrapper) =>
            {
                if (isSuc && wrapper != null)
                {
                    if (playAnim != null) {
                        var clip = wrapper.RetainAsset(playAnim.gameObject);
                        AnimationClip clipA = AnimationClip.Instantiate(clip);
                        playAnim.LoadPlay(clipA, 1);
                    }
                    Save();
                } else {
                    petAnimationCtrl.Play("idle");
                    Save();
                }
            });
        }

        private IEnumerator TakeMatchPhoto()
        {
            yield return new WaitForEndOfFrame();

            try
            {
                Rect rect = GetScreenShotRect();

                byte[] imgBytes = ScreenShotUtils.TakeShotGamma(photoCamera, rect);

                Destroy(photoCamera.gameObject);

                string fileName = LocalDataUtils.Inst.SaveImgRes(imgBytes);

                var uri = $"FittingRoom/characterInfo/{AccountDataManager.Inst.Uid}/{Path.GetFileName(fileName)}";
                CosXmlUploadManager.UploadFile(uri, fileName, (url, err) =>
                {
                    UploadImgCallback(url, err, fileName);
                });
            }
            catch (Exception e)
            {
                LoggerUtils.LogError(e.Message);
            }
        }

        private void UploadImgCallback(string url, string err, string fileName)
        {
            File.Delete(fileName);

            if (!string.IsNullOrEmpty(err))
            {
                LoggerUtils.LogError($"Upload Character Image Fail!!! Err : {err}");
                return;
            }
            else
            {
                LoggerUtils.Log("Upload Img Success url: " + url);

                SaveDressData(url);
            }
        }

        private void SaveDressData(string fileUrl) {
            string jsonContent = null;

            if (avatarWrapper is CharacterWrap characterWrap) {
                jsonContent = CharacterData.SerializeObject(characterWrap.ChaData);
            } else if (avatarWrapper is PetWrap petWrap) {
                jsonContent = PetData.SerializeObject(petWrap.Data);
            }

            ocList.SetOcInfo(fileUrl, jsonContent, resultHandler: isSuccess =>
            {
                LoggerUtils.Log($"[Oc] save result: {isSuccess}");
                if (isSuccess)
                {
                    TipPanel.ShowToast("成功保存到设子卡位");
                }
                ShowOcButton();
                EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
            });
        }

        private Rect GetScreenShotRect()
        {
            Rect rect = new Rect(0, 0, photoCamera.pixelWidth, photoCamera.pixelHeight);
            return rect;
        }

        public void ShowOcButton()
        {
            saveOcButton.gameObject.SetActive(true);
            saveOcButton.SetClickAble(true);
            saveOcButton.HideLoading();
        }

        protected override void OnDestroy()
        {
            // 清除背包Ugc数据
            AssetsDataManager.ClearBagUgcData();
            // TODO暂时调用，因为3D部件的mesh问题，需要释放一下
            Resources.UnloadUnusedAssets();
            // MessageHelper.RemoveListener<TaskListRsp>(MessageName.NewComerCommunityCoin, NewComerCommunityCoin);
#if UNITY_EDITOR
            // pc上主动GC
            GC.Collect();
#endif
        }

        public override void OnWindowPop()
        {

        }

        #region 细节调整
        public RoleDataAdjust GetRoleDataAdjust(AdjustViewItemType type, Es.AvatarCommonData configData, int classType) {

            CharacterPartData partData = saveAvatarData.GetPartData(classType);
            var avatarSubType = (AvatarSubType)configData.SubType;
            AdjustAxis axis;
            switch (type)
            {
                case AdjustViewItemType.Size:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.None
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.Size,
                        Getter = () => partData.Sca != null ? partData.Sca : configData.sDef,
                        Setter = (v) => partData.Sca = v,
                        Axis = () => axis,
                        Limit = () => configData.scaLimit,
                        Default = () => configData.sDef,
                        Apply = () => ChangeSize(classType, partData.Sca)
                    };
                case AdjustViewItemType.UpDown:
                    axis = avatarSubType switch
                    {
                        AvatarSubType.Hair => AdjustAxis.Y,
                        AvatarSubType.Earring => AdjustAxis.Y,
                        AvatarSubType.Hand => AdjustAxis.Z,
                        AvatarSubType.FacePaint => AdjustAxis.None,
                        _ => AdjustAxis.X
                    };

                    if (!isCharacterFittingRoom && avatarSubType == AvatarSubType.Hair)
                        axis = AdjustAxis.X;

                    return new()
                    {
                        AdjustType = AdjustViewItemType.UpDown,
                        Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
                        Setter = (v) => partData.Pos = v,
                        Axis = () => axis,
                        Limit = () => configData.vLimit,
                        Default = () => configData.pDef,
                        Apply = () => ChangeMove(classType, partData.Pos)
                    };
                case AdjustViewItemType.LeftRight:
                    axis = avatarSubType switch
                    {
                        AvatarSubType.Hair => AdjustAxis.X,
                        AvatarSubType.Hand => AdjustAxis.X,
                        _ => AdjustAxis.Z
                    };

                    if (!isCharacterFittingRoom && avatarSubType == AvatarSubType.Hair)
                        axis = AdjustAxis.Z;

                    return new()
                    {
                        AdjustType = AdjustViewItemType.LeftRight,
                        Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
                        Setter = (v) => partData.Pos = v,
                        Axis = () => axis,
                        Limit = () => configData.hLimit,
                        Default = () => configData.pDef,
                        Apply = () => ChangeMove(classType, partData.Pos)
                    };
                case AdjustViewItemType.FrontBack:
                    axis = avatarSubType switch
                    {
                        AvatarSubType.Hair => AdjustAxis.Z,
                        AvatarSubType.Earring => AdjustAxis.Z,
                        _ => AdjustAxis.Y
                    };

                    var fbAdjustData = new RoleDataAdjust()
                    {
                        AdjustType = AdjustViewItemType.FrontBack,
                        Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
                        Setter = (v) => partData.Pos = v,
                        Axis = () => axis,
                        Limit = () => avatarSubType == AvatarSubType.Hair ? configData.hLimit : configData.fLimit,
                        Default = () => configData.pDef,
                        Apply = () => ChangeMove(classType, partData.Pos)
                    };

                    if (!isCharacterFittingRoom && avatarSubType == AvatarSubType.Hair)
                    {
                        fbAdjustData.Axis = () => AdjustAxis.Y;
                        fbAdjustData.Limit = () => configData.fLimit;
                    }

                    return fbAdjustData;
                case AdjustViewItemType.Spacing:
                    axis = avatarSubType switch
                    {
                        AvatarSubType.Earring => AdjustAxis.X,
                        _ => AdjustAxis.Z
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.Spacing,
                        Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
                        Setter = (v) => partData.Pos = v,
                        Axis = () => axis,
                        Limit = () => configData.hLimit,
                        Default = () => configData.pDef,
                        Apply = () => ChangeMove(classType, partData.Pos)
                    };
                case AdjustViewItemType.Vertical:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.None
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.Vertical,
                        Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
                        Setter = (v) => partData.Pos = v,
                        Axis = () => axis,
                        Limit = () => configData.vLimit,
                        Default = () => configData.pDef,
                        Apply = () => ChangeMove(classType, partData.Pos)
                    };
                case AdjustViewItemType.HorizontalStretch:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.X
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.HorizontalStretch,
                        Getter = () => partData.CSca != null ? partData.CSca : configData.vhSDef,
                        Setter = (v) => partData.CSca = v,
                        Axis = () => axis,
                        Limit = () => configData.hScaLimit,
                        Default = () => configData.vhSDef,
                        Apply = () => ChangeHVSize(classType, partData.CSca)
                    };
                case AdjustViewItemType.VerticalStretch:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.Z
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.VerticalStretch,
                        Getter = () => partData.CSca != null ? partData.CSca : configData.vhSDef,
                        Setter = (v) => partData.CSca = v,
                        Axis = () => axis,
                        Limit = () => configData.vScaLimit,
                        Default = () => configData.vhSDef,
                        Apply = () => ChangeHVSize(classType, partData.CSca)
                    };
                case AdjustViewItemType.Rotation:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.None
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.Rotation,
                        Getter = () => partData.Rot != null ? partData.Rot : configData.rDef,
                        Setter = (v) => partData.Rot = v,
                        Axis = () => axis,
                        Limit = () => configData.rotateLimit,
                        Default = () => configData.rDef,
                        Apply = () => ChangeRotate(classType, partData.Rot)
                    };
                case AdjustViewItemType.XRotation:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.X
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.XRotation,
                        Getter = () => partData.Rot != null ? partData.Rot : configData.rDef,
                        Setter = (v) => partData.Rot = v,
                        Axis = () => axis,
                        Limit = () => configData.xrotLimit,
                        Default = () => configData.rDef,
                        Apply = () => ChangeRotate(classType, partData.Rot)
                    };
                case AdjustViewItemType.YRotation:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.Z
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.YRotation,
                        Getter = () => partData.Rot != null ? partData.Rot : configData.rDef,
                        Setter = (v) => partData.Rot = v,
                        Axis = () => axis,
                        Limit = () => avatarSubType == AvatarSubType.Hand || avatarSubType == AvatarSubType.Visor ? configData.yrotLimit : configData.zrotLimit,
                        Default = () => configData.rDef,
                        Apply = () => ChangeRotate(classType, partData.Rot)
                    };
                case AdjustViewItemType.ZRotation:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.Y
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.ZRotation,
                        Getter = () => partData.Rot != null ? partData.Rot : configData.rDef,
                        Setter = (v) => partData.Rot = v,
                        Axis = () => axis,
                        Limit = () => avatarSubType == AvatarSubType.Hand || avatarSubType == AvatarSubType.Visor ? configData.zrotLimit : configData.yrotLimit,
                        Default = () => configData.rDef,
                        Apply = () => ChangeRotate(classType, partData.Rot)
                    };
                case AdjustViewItemType.SwitchHand:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.None
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.SwitchHand,
                        Getter = () => new Vector3(partData.LRType, 0, 0),
                        Setter = (v) => partData.LRType = (int)v.x,
                        Axis = () => axis,
                        Default = () => new Vector3(configData.leftRightType, 0, 0),
                        Apply = () => ChangeLeftOrRight(classType, partData.LRType)
                    };
            }

            return new RoleDataAdjust();
        }



        public List<RoleDataAdjust> AdjustTypeAdjustItems(AvatarCommonData configData, int classType)
        {
            if (configData == null) return null;
            switch (configData.adjustType)
            {
                case (int)AdjustType.Skin:
                    var list = new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.LeftRight, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.XRotation, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.YRotation, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.ZRotation, configData, classType)
                    };
                    if ((AvatarSubType)configData.SubType == AvatarSubType.Hand && (configData.leftRightType == 1 || configData.leftRightType == 2))
                    {
                        list.Insert(0, GetRoleDataAdjust(AdjustViewItemType.SwitchHand, configData, classType));
                    }
                    return list;
                case (int)AdjustType.EyeBrow:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Spacing, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Rotation, configData, classType)
                    };
                case (int)AdjustType.Nose:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Vertical, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.HorizontalStretch, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.VerticalStretch, configData, classType)
                    };
                case (int)AdjustType.Mouth:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.LeftRight, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Rotation, configData, classType)
                    };
                case (int)AdjustType.Blush:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Spacing, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType)
                    };
                case (int)AdjustType.FacePaint:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType)
                    };
                case (int)AdjustType.EarRing:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Spacing, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Rotation, configData, classType)
                    };
                default:
                    return null;
            }
        }
        #endregion
    }

    public class SendGiftBaseScene
    {
        // 不属于avatar的分类枚举定义

        internal SendGiftPanel UI;

        protected List<MainTabs.Tab> mainTabs = new List<MainTabs.Tab>();

        protected Dictionary<MainTabs.Tab, List<ClassData>> classListDict = new Dictionary<MainTabs.Tab, List<ClassData>>();

        protected List<ClassData> classDatas;

        protected ClassData classSelected;

        protected GoodsDataClassifyList assetsDatas = new();

        protected bool Enable;

        internal SendGiftBaseScene(SendGiftPanel ui)
        {
            UI = ui;
            InitParams();
        }

        public virtual void InitParams()
        {

        }

        public virtual void Enter()
        {
            UI.CloseAllSubUI();
            UI.assetsList.ResetColor();
            UI.assetsList.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
            UI.assetsList.PullToRefreshBehaviour.HideGizmo();
            Enable = true;
        }

        public virtual void Exit()
        {
            Enable = false;
        }

        public virtual void OnDataRefresh()
        {

        }

        public virtual void Destory()
        {

        }

        public virtual GoodsData CreateNewModel(int index)
        {
            return default;
        }

        public virtual void Design()
        {
            var resourcesType = UniqueType.ResourceType(classSelected.Id);

            if (resourcesType == ResourceType.Avatar || resourcesType == ResourceType.UgcAvatar)
            {
                var avatarSubType = UniqueType.AvatarSubType(classSelected.Id);
                if (avatarSubType == AvatarSubType.MusicalInstrument)
                {
                    if (!GameController.IsInHallScene())
                    {
                        TipPanel.ShowToast("游玩过程中无法进行创作哦");
                        return;
                    }
                    UI.ResetLastTryOn(ResourceType.ErrResourceType);
                    UIManager.Inst.OpenPanel(PanelId.MusicalInstrumentStudioPanel);
                }
                else
                {
                    UI.ResetLastTryOn(ResourceType.ErrResourceType);
                    UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel,CharacterStyle.Avatar);
                }
            } else if (resourcesType == ResourceType.PGCPetAvatar || resourcesType == ResourceType.UGCPetAvatar) {
                UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel,CharacterStyle.Pet);
            } else if (resourcesType == ResourceType.MusicScore)
            {
                if (!GameController.IsInHallScene())
                {
                    TipPanel.ShowToast("游玩过程中无法进行创作哦");
                    return;
                }
                UI.ResetLastTryOn(ResourceType.ErrResourceType);
                UIManager.Inst.OpenPanel(PanelId.MusicScoreStudioPanel);
            } else if ( resourcesType == ResourceType.UgcPose)
            {
                if (!GameController.IsInHallScene())
                {
                    TipPanel.ShowToast("游玩过程中无法进行创作哦");
                    return;
                }
                UI.ResetLastTryOn(ResourceType.ErrResourceType);
                UIManager.Inst.OpenPanel(PanelId.AnimationStudioMainPanel, AnimationStudioType.Pose);
            } else if (resourcesType == ResourceType.UgcEmote)
            {
                if (!GameController.IsInHallScene())
                {
                    TipPanel.ShowToast("游玩过程中无法进行创作哦");
                    return;
                }
                UI.ResetLastTryOn(ResourceType.ErrResourceType);
                UIManager.Inst.OpenPanel(PanelId.AnimationStudioMainPanel, AnimationStudioType.Animation);
            }
            else
            {
                UI.ResetLastTryOn(ResourceType.ErrResourceType);

                UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel, UI.isCharacterFittingRoom ? CharacterStyle.Avatar : CharacterStyle.Pet);
            }
        }

    }

}
