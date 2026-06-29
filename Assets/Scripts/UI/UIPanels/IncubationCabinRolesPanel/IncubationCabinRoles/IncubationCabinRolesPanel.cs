using BUD.AnimPose;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Avatar;
using Game.Database;
using Game.COSXML;
using Game.MusicalInstrument;
using Game.Utils;
using GameData;
using GameData.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;
using Message;

namespace UI.UIPanels.IncubationCabin
{
    public partial class IncubationCabinRolesPanel : BasePanel<IncubationCabinRolesPanel>
    {
        internal Button btn_show;
        internal Text nameText;
        internal Button btn_all_anim;

        internal Button backBtn;

        internal CharacterWrap otherCharacterWrap;
        internal BaseAvatarWrapper avatarWrapper;
        internal Transform characterRoot;
        internal AvatarCameraController avatarCameraController;

        #region 人物相关

        internal GameObject skinContentGo;
        internal GameObject actionContentGo;
        internal GameObject voiceContentGo;
        internal GameObject interactContentGo;
        internal GameObject avatarRoot;

        internal GameObject bg_show;

        internal PlayerAnimationCtrl animationCtrl;
        internal AnimIKController animationCtrlIK;
        internal PlayerHoldBehaviour playerHold;
        internal PlayerAnimationCtrl otherAnimationCtrl;
        internal AnimIKController otherAnimationCtrlIK;
        internal PlayMusicScoreBev playMusicScoreBev;

        internal CharacterWrap characterWrapper;

        string curSkinPackId = "";
        string curSkinUgcId = "";
        string curSkinCreator = "";
        /// <summary>当前选中皮肤所属角色的完整数据（本体为 CabinCharacterUgcInfo，拓展包为 CabinCharacterPackInfo）</summary>
        CabinCharacterBaseInfo _currentSkinData;

        #endregion

        // ── skin ──
        internal ScrollRect scrollRect;
        internal GameObject skinRoleItemPrefab;


        // ── baseMsg ──
        internal RemoteImageBehaviour remoteImg;
        internal Button remoteImgBtn;
        internal LikeButton LikeBtn;
        internal Text txt_user_name;
        internal Text txt_like;

        // ── interact ──
        internal CabinBtnToggleParent characterInteractToggleParent;
        internal CabinBtnToggleParent characterInteractNode1ToggleParent;
        internal GameObject interactContent1Go;
        internal GameObject interactContent2Go;
        internal GameObject interactContent3Go;
        internal GameObject interactContent4Go;
        internal GameObject interact_mainContentGo;
        internal GameObject interact_bottomContentGo;
        internal GameObject interactRoleItemPrefab;
        internal GameObject interactBottomRoleItemPrefab;
        internal CabinBtnToggleParent interact_node1_toggleParent;
        internal ScrollRect interact_node1_scrollRect;
        internal GameObject interact_node1_itemPrefab;
        internal GameObject interact_node1_emptyTipGo;
        internal ScrollRect interact_node2_scrollRect;
        internal GameObject interact_node2_emptyTipGo;
        internal GameObject interact_node2_itemPrefab;
        internal GameObject interact_node2_btnPrefab;
        internal ScrollRect interact_node3_scrollRect;
        internal GameObject interact_node3_emptyTipGo;
        internal GameObject interact_node3_itemPrefab;
        internal GameObject interact_node3_btnPrefab;
        internal ScrollRect interact_node4_scrollRect;
        internal Text txt_buy_count;
        internal CButton btn_edit;
        internal CButton btn_getskin;
        internal Text btn_buy_text;
        internal CButton btn_buy;
        internal Text txt_price;


        // ── action ──
        internal CabinBtnToggleParent mainActionBtnToggleParent;
        internal CabinBtnToggleParent subActionBtnToggleParent;
        internal ScrollRect actionScrollRect;
        internal Text tipTxt;
        internal Transform poolTransRoot;
        internal GameObject actionCreateItemPrefab;
        internal GameObject actionGetMoreItemPrefab;
        internal GameObject actionRoleItemPrefab;
        internal GameObject actionRoleItem2Prefab;
        internal FittingRoomAdapter assetsList;
        public IncubationCabinDataLoader DataLoader;

        // ── voice ──
        internal CabinBtnToggleParent characterVoiceToggleParent;
        internal CabinBtnToggleParent voiceTypeToggleParent;
        internal GameObject RoleSoundGo;
        internal ScrollRect RoleSoundScrollRect;
        internal Text txt_selectVoiceName;
        internal GameObject RoleVoicePackGo;
        internal GameObject voiceItemPrefab;
        internal GameObject voiceCreateItemPrefab;
        internal GameObject voiceGetMoreItemPrefab;
        internal ScrollRect RoleVoicePackScrollRect;
        internal GameObject voicePackAddItemPrefab;
        internal GameObject voicePackDetailItemPrefab;

        private Text creatorName;
        private Text creatorValue;
        private Text creatorLevel;

        // ── controller ──
        CabinPgcUgcPlayController cabinPgcUgcPlayController = new();
        private BudTimer _previewDelayTimer;
        // 动画播完但语音仍在播放时，等待语音结束的协程
        private Coroutine _audioWaitCoroutine;
        private CabinStandbyAnimController _standbyAnimCtrl = new CabinStandbyAnimController();

        public override void OnCreate()
        {
            base.OnCreate();
            bg_show = transform.Find("BaseLayout2D/LeftRoot/bg_show")?.gameObject;
            //setDefaultBtn = transform.Find("BaseLayout2D/LeftRoot/setDefaultBtn")?.GetComponent<CButton>();
            backBtn = transform.Find("BaseLayout2D/LeftRoot/back/BackBtn")?.GetComponent<CButton>();
            characterRoot = transform.Find("BaseLayout3D/CharacterRoot");
            avatarCameraController = transform.Find("BaseLayout2D/LeftRoot/ClickArea")?.GetComponentInChildren<AvatarCameraController>();
            avatarRoot = transform.Find("BaseLayout2D/LeftRoot/AvatarRoot")?.gameObject;
            var contentRoot = transform.Find("BaseLayout2D/RightRoot/CabinRoot/ContentRoot");
            btn_show = GameObjectEx.FindComponentByName<CButton>(contentRoot, "btn_show");
            nameText = contentRoot.Find("nameText").GetComponent<Text>();
            skinContentGo = contentRoot?.Find("skinContent")?.gameObject;
            actionContentGo = contentRoot?.Find("actionContent")?.gameObject;
            voiceContentGo = contentRoot?.Find("voiceContent")?.gameObject;
            interactContentGo = contentRoot?.Find("interactContent")?.gameObject;

            // skin
            var skinContent = contentRoot?.Find("skinContent");
            scrollRect = skinContent?.Find("Scroll View")?.GetComponent<ScrollRect>();
            skinRoleItemPrefab = skinContent?.Find("CabinSkinCardItem")?.gameObject;

            // baseMsg
            var RoleVoice = contentRoot?.Find("RoleVoice");
            remoteImg = RoleVoice.Find("remoteImg").GetComponent<RemoteImageBehaviour>();
            remoteImgBtn = RoleVoice.Find("remoteImg")?.GetComponent<Button>();
            txt_user_name = RoleVoice.Find("name").GetComponent<Text>();
            txt_like = RoleVoice.Find("LikeBtn/value").GetComponent<Text>();
            LikeBtn = GameObjectEx.FindChildByName(RoleVoice, "LikeBtn")?.GetComponent<LikeButton>();

            // interact
            var interactContent = contentRoot?.Find("interactContent");
            characterInteractToggleParent = interactContent?.GetComponent<CabinBtnToggleParent>()
                ?? interactContent?.Find("mainTabs")?.GetComponent<CabinBtnToggleParent>();
            var node1 = interactContent?.Find("node1");
            btn_all_anim = GameObjectEx.FindComponentByName<CButton>(node1, "btn_all_anim");
            //txt_buy_count = GameObjectEx.FindComponentByName<Text>(interactContent,"txt_buy_count");
            btn_edit = GameObjectEx.FindComponentByName<CButton>(interactContent, "btn_edit");
            btn_buy_text = GameObjectEx.FindComponentByName<Text>(btn_edit.transform, "btn_buy_text");
            btn_getskin = GameObjectEx.FindComponentByName<CButton>(interactContent, "btn_getskin");
            btn_buy = GameObjectEx.FindComponentByName<CButton>(interactContent, "btn_buy");
            txt_price = GameObjectEx.FindComponentByName<Text>(btn_buy.transform, "Price");
            characterInteractNode1ToggleParent = node1?.Find("secondTabs/hori")?.GetComponent<CabinBtnToggleParent>();
            interactContent1Go = node1?.gameObject;
            interactContent2Go = interactContent?.Find("node2")?.gameObject;
            interactContent3Go = interactContent?.Find("node3")?.gameObject;
            interactContent4Go = interactContent?.Find("node4")?.gameObject;
            interactRoleItemPrefab = node1?.Find("interactRoleItem")?.gameObject;
            interactBottomRoleItemPrefab = node1?.Find("roleItem1")?.gameObject;
            interact_node1_toggleParent = node1?.Find("secondTabs")?.GetComponent<CabinBtnToggleParent>();
            interact_node1_scrollRect = node1?.Find("Scroll View")?.GetComponent<ScrollRect>();
            interact_node1_itemPrefab = node1?.Find("interactRoleItem")?.gameObject;
            interact_node1_emptyTipGo = node1?.Find("emptyTipNode")?.gameObject;
            var node2 = interactContent?.Find("node2");
            interact_node2_scrollRect = node2?.Find("Scroll View")?.GetComponent<ScrollRect>();
            interact_node2_emptyTipGo = node2?.Find("emptyTipNode")?.gameObject;
            interact_node2_itemPrefab = node2?.Find("roleItem")?.gameObject;
            interact_node2_btnPrefab = node2?.Find("btn")?.gameObject;
            var node3 = interactContent?.Find("node3");
            interact_node3_scrollRect = node3?.Find("Scroll View")?.GetComponent<ScrollRect>();
            interact_node3_emptyTipGo = node3?.Find("emptyTipNode")?.gameObject;
            interact_node3_itemPrefab = node3?.Find("roleItem")?.gameObject;
            interact_node3_btnPrefab = node3?.Find("btn")?.gameObject;
            interact_node4_scrollRect = interactContent?.Find("node4/Scroll View")?.GetComponent<ScrollRect>();

            // action
            var actionContent = contentRoot?.Find("actionContent");
            mainActionBtnToggleParent = actionContent?.Find("mainTabs")?.GetComponent<CabinBtnToggleParent>();
            subActionBtnToggleParent = actionContent?.Find("secondTabs/hori")?.GetComponent<CabinBtnToggleParent>();
            actionScrollRect = actionContent?.Find("Scroll View")?.GetComponent<ScrollRect>();
            tipTxt = actionContent?.Find("bottomNode/tip_txt")?.GetComponent<Text>();
            poolTransRoot = actionContent?.Find("pool");
            actionCreateItemPrefab = actionContent?.Find("pool/createItem")?.gameObject;
            actionGetMoreItemPrefab = actionContent?.Find("pool/getMoreItem")?.gameObject;
            actionRoleItemPrefab = actionContent?.Find("pool/roleItem")?.gameObject;
            actionRoleItem2Prefab = actionContent?.Find("pool/roleItem2")?.gameObject;
            DataLoader = actionContent?.Find("dataLoader")?.GetComponent<IncubationCabinDataLoader>();

            // voice
            var voiceContent = contentRoot?.Find("voiceContent");
            characterVoiceToggleParent = voiceContent?.Find("mainTabs")?.GetComponent<CabinBtnToggleParent>();
            var roleSound = voiceContent?.Find("roleSound");
            RoleSoundGo = roleSound?.gameObject;
            voiceTypeToggleParent = roleSound?.Find("secondTabs/hori")?.GetComponent<CabinBtnToggleParent>();
            RoleSoundScrollRect = roleSound?.Find("Scroll View")?.GetComponent<ScrollRect>();
            txt_selectVoiceName = roleSound?.Find("bottomNode/txt")?.GetComponent<Text>();
            voiceItemPrefab = roleSound?.Find("roleItem")?.gameObject;
            voiceCreateItemPrefab = roleSound?.Find("createItem")?.gameObject;
            voiceGetMoreItemPrefab = roleSound?.Find("getMoreItem")?.gameObject;
            var roleVoicePack = voiceContent?.Find("roleVoicePack");
            RoleVoicePackGo = roleVoicePack?.gameObject;
            RoleVoicePackScrollRect = roleVoicePack?.Find("Scroll View")?.GetComponent<ScrollRect>();
            voicePackAddItemPrefab = roleVoicePack?.Find("addItem")?.gameObject;
            voicePackDetailItemPrefab = roleVoicePack?.Find("detailItem")?.gameObject;
            MessageHelper.AddListener<int>(MessageName.OnCabinCharacterLikeChange, OnCabinCharacterLikeChange);
        }

        internal CabinPublishData currentPublishData;

        // 面板打开时传入的独立参数，与角色数据无关
        private string _skinId;
        private Action _onClose;

        // 是否从商城入口打开；为 true 时各 Tab 均只显示当前选中皮肤的 CabinCharacterBaseInfo 数据
        internal bool _isFromShop;

        public override void OnShow(params object[] args)
        {
            currentPublishData = args != null && args.Length > 0 ? args[0] as CabinPublishData : null;
            _skinId     = args != null && args.Length > 1 ? args[1] as string : null;
            _onClose    = args != null && args.Length > 2 ? args[2] as Action : null;
            _isFromShop = args != null && args.Length > 3 && args[3] is bool b && b;
            InitUpdateUI(currentPublishData);
        }

        void InitUpdateUI(CabinPublishData publishData = null)
        {
            CabinRolesNetManager.Inst.RegisterPanel(this);
            InitUI();

            if (publishData?.characterInfo != null)
                CabinRolesNetManager.Inst.SetCurrentCharacterUgcInfo(publishData.characterInfo);
            else
                CabinRolesNetManager.Inst.SelectDefaultCabinCharacterUgcInfo();

            skinContentGo.SetActive(true);
            actionContentGo.SetActive(false);
            voiceContentGo.SetActive(true);
            interactContentGo.SetActive(true);

            var netCabinCharacterUgcInfo = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();
            if (netCabinCharacterUgcInfo != null)
            {
                SelectCabinCharacterUgcInfo(netCabinCharacterUgcInfo, _skinId);
            }
        }

        /// <summary>
        /// 选择养成舱角色
        /// </summary>
        public void SelectCabinCharacterUgcInfo(CabinCharacterUgcInfo cabinCharacterUgcInfo, string skinId = null)
        {
            if (cabinCharacterUgcInfo == null)
            {
                LoggerUtils.LogError("选择养成舱角色失败，角色信息为空");
                return;
            }
            // 切换角色前停止当前预览语音、协程和定时器
            StopCurrentPreviewAudioAndTimers();
            cabinPgcUgcPlayController.CancelAnim();
            _standbyAnimCtrl.Stop();
            foreach (var skinPack in cabinCharacterUgcInfo.skinPack)
            {
                if (skinPack.isDefault == 1)
                {
                    curSkinPackId = skinPack.packId;
                    InitCharacterWrapper(CharacterData.DeserializeObject(skinPack.avatarJson));
                    //IsDefautSkinPack = true;
                    break;
                }
            }

            photoCamera = avatarCameraController.roleCamera;
            cabinPgcUgcPlayController.Init(animationCtrl, characterWrapper, null, avatarCameraController);
            _standbyAnimCtrl.Start(cabinPgcUgcPlayController, cabinCharacterUgcInfo.usingEmote);
            CreatRoleSkinDataList(skinId);
            RefreshBaseMsg();
            RefreshInteract();
            RefreshBuyEditButtons(cabinCharacterUgcInfo);
        }

        private void RefreshBuyEditButtons(CabinCharacterUgcInfo info, BaseInteractInfo interactInfo = null)
        {
            if (info == null)
                return;

            bool isOwned = interactInfo?.consumed == 1;
            bool isCreator = info.creator == AccountDataManager.Inst.Uid;
            bool canEdit = isOwned || isCreator;

            btn_buy.gameObject.SetActive(!canEdit);
            btn_edit.gameObject.SetActive(canEdit && !_isFromShop);
            if (!canEdit && txt_price != null)
                txt_price.text = info.paymentInfo?.price.ToString() ?? "";

            RefreshGetSkinBtn(info);
        }

        private void RefreshGetSkinBtn(CabinCharacterUgcInfo info)
        {
            if (btn_getskin == null || info == null) return;
            bool isCreator = info.creator == AccountDataManager.Inst.Uid;
            if (isCreator)
            {
                btn_getskin.gameObject.SetActive(false);
                return;
            }
            var charInventory = BagDatabase.Inst.Select(info.id);
            bool charOwned = charInventory != null && charInventory.OwnedNum > 0;
            if (!charOwned)
            {
                btn_getskin.gameObject.SetActive(true);
                return;
            }
            bool allPacksOwned = true;
            if (info.extensionPackList != null)
            {
                foreach (var packId in info.extensionPackList)
                {
                    var inv = BagDatabase.Inst.Select(packId);
                    if (inv == null || inv.OwnedNum <= 0)
                    {
                        allPacksOwned = false;
                        break;
                    }
                }
            }
            btn_getskin.gameObject.SetActive(!allPacksOwned);
        }

        private void InitCharacterWrapper(CharacterData characterData)
        {
            if (characterWrapper != null)
            {
                characterWrapper.RefreshAvatar(characterData);
                return;
            }

            characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(characterData, characterRoot);
            animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            animationCtrlIK = characterWrapper.Avatar.GetComponent<AnimIKController>();
            playerHold = characterWrapper.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
            avatarCameraController.RotateTarget = characterRoot;
        }

        void InitUI()
        {
            //setDefaultBtn.onClick.AddListener(OnSetDefaultBtnClick);
            backBtn.onClick.AddListener(OnBackBtnClick);

            btn_all_anim.onClick.AddListener(OnShowStandbyPreview);
            btn_show.onClick.AddListener(() =>
            {
                bg_show.GetComponent<CButton>().onClick.RemoveAllListeners();
                bg_show.GetComponent<CButton>().onClick.AddListener(() =>
                {
                    ExitShowPreview();
                });
                GameObjectEx.FindChildByName(transform, "RightRoot").gameObject.SetActive(false);
                bg_show.gameObject.SetActive(true);
            });
            btn_edit.onClick.AddListener(() =>
            {
                // 打开编辑界面前停止当前预览语音
                StopCurrentPreviewAudioAndTimers();
                UIManager.Inst.OpenPanel(PanelId.EditRoleInteractionView);
            });
            btn_getskin.onClick.AddListener(() =>
            {
                var info = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();
                if (info == null) return;
                UIManager.Inst.OpenPanel<PartnerBuyItemsPanel>(PanelId.PartnerBuyItemsPanel, info, (Action)(() =>
                {
                    RefreshSkinItemsLock();
                    RefreshInteractItemsLock();
                    RefreshBaseMsg();
                }));
            });
            btn_buy.onClick.AddListener(() =>
            {
                var info = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();

                if (info == null)
                    return;

                UIManager.Inst.OpenPanel<PartnerBuyItemsPanel>(PanelId.PartnerBuyItemsPanel, info, (Action)(() =>
                {
                    RefreshSkinItemsLock();
                    RefreshInteractItemsLock();
                    RefreshBaseMsg();
                    // 购买完成后检测伙伴是否在 BOX 中，若是则提示前往控制台同步
                    CheckIsInBoxThenDo(null);
                }));
            });
            var netCabinCharacterUgcInfo = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();

            remoteImgBtn?.onClick.AddListener(() =>
            {
                var info = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();

                if (info == null)
                    return;

                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.CabinCharacter, info.id);
            });

            InitActionUI();
            InitVoiceUI(netCabinCharacterUgcInfo);
        }

        public override void OnHidden()
        {
            StopCurrentPreviewAudioAndTimers();
            cabinPgcUgcPlayController.CancelAnim();
            _standbyAnimCtrl.Stop();
            base.OnHidden();
            CabinRolesNetManager.Inst.UnregisterPanel();
        }

        protected override void OnDestroy()
        {
            MessageHelper.RemoveListener<int>(MessageName.OnCabinCharacterLikeChange, OnCabinCharacterLikeChange);
            base.OnDestroy();
        }

        public override void CloseSelf()
        {
            StopCurrentPreviewAudioAndTimers();
            cabinPgcUgcPlayController.CancelAnim();
            _standbyAnimCtrl.Stop();
            _onClose?.Invoke();
            base.CloseSelf();
        }

        /// <summary>
        /// 点击返回按钮，直接关闭面板。
        /// </summary>
        void OnBackBtnClick()
        {
            CloseSelf();
        }

        /// <summary>
        /// 检测当前伙伴是否在线上 BOX 中展示。
        /// 若不在，直接执行 proceedAction；
        /// 若在，则弹出「前往控制台同步」二确弹窗：
        ///   - 确认（前往控制台）→ 打开 BOX 控制台入口
        ///   - 取消（继续）→ 执行 proceedAction
        /// </summary>
        /// <param name="proceedAction">BOX 检测通过或用户选择继续时执行的原始操作</param>
        internal void CheckIsInBoxThenDo(Action proceedAction)
        {
            // 获取账号最新的 BOX 角色绑定数据
            CabinBoxManager.Inst.InitBudBoxDic(success =>
            {
                // 面板可能在网络回调到达前已被销毁（如切换场景），需提前返回
                if (gameObject == null)
                    return;

                if (!success)
                {
                    proceedAction?.Invoke();
                    return;
                }

                var currentCharInfo = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();

                if (currentCharInfo == null)
                {
                    proceedAction?.Invoke();
                    return;
                }

                // 遍历所有绑定的 BOX，判断当前伙伴是否正在某台 BOX 中展示
                bool isInBox = false;
                var boxList = CabinBoxManager.Inst.GetBudBoxList();

                foreach (var box in boxList)
                {
                    if (box.characterInfo?.id == currentCharInfo.id)
                    {
                        isInBox = true;
                        break;
                    }
                }

                if (!isInBox)
                {
                    proceedAction?.Invoke();
                    return;
                }

                // 当前伙伴正在 BUD BOX 中展示，弹出二级确认框
                var confirmPanel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(
                    PanelId.CommonBoxConfirmWithTitlePanel);

                confirmPanel.SetTextAndAction(
                    "前往BUD BOX控制台",
                    "当前伙伴正在BUD BOX中展示，可前往控制台同步最新配置。",
                    "前往控制台",
                    "不了",
                    confirmClick: () =>
                    {
                        CabinBoxManager.Inst.OpenBoxEntrance();
                    },
                    cancelClick: () =>
                    {
                        proceedAction?.Invoke();
                    }
                );
            });
        }

        void ExitShowPreview()
        {
            GameObjectEx.FindChildByName(transform, "RightRoot").gameObject.SetActive(true);
            bg_show.gameObject.SetActive(false);
        }

        Camera photoCamera;

        #region 上传cover
        public IEnumerator TakeMatchPhoto(Action<bool, string> callback = null)
        {
            yield return new WaitForEndOfFrame();
            yield return null;

            try
            {
                Rect rect = GetScreenShotRect();
                byte[] imgBytes = ScreenShotUtils.TakeShotGamma(photoCamera, rect);
                string fileName = LocalDataUtils.Inst.SaveImgRes(imgBytes);
                var uri = $"IncubationCabin/characterInfo/{AccountDataManager.Inst.Uid}/{Path.GetFileName(fileName)}";
                CosXmlUploadManager.UploadFile(uri, fileName, (url, err) =>
                {
                    UploadImgCallback(url, err, fileName, callback);
                });
            }
            catch (Exception e)
            {
                LoggerUtils.LogError(e.Message);
                callback?.Invoke(false, null);
            }
        }

        private Rect GetScreenShotRect()
        {
            return new Rect(0, 0, photoCamera.pixelWidth, photoCamera.pixelHeight);
        }

        private void UploadImgCallback(string url, string err, string fileName, Action<bool, string> callback = null)
        {
            File.Delete(fileName);
            if (!string.IsNullOrEmpty(err))
            {
                LoggerUtils.LogError($"Upload Character Image Fail!!! Err : {err}");
                callback?.Invoke(false, null);
                return;
            }
            LoggerUtils.Log("Upload Img Success url: " + url);
            callback?.Invoke(true, url);
        }
        #endregion

        #region 预览激活动作
        public void BeginPreviewActivation(characterInteraction Interaction)
        {
            // 先停止上一次预览的语音、定时器和等待协程，防止多次点击叠加播放语音
            StopCurrentPreviewAudioAndTimers();
            _standbyAnimCtrl.Stop();
            cabinPgcUgcPlayController.InitData(Interaction);
            cabinPgcUgcPlayController.SetPlayEndCallback(EndPreviewActivationAnimOnly);

            if (Interaction.delaySecond >= 0)
            {
                // delaySecond >= 0：立即播动画，延迟 delaySecond 秒后播语音
                // isMute == 1 时动画音效静音；语音 audioUrl 始终播放，不受 isMute 控制
                cabinPgcUgcPlayController.PlayAnim(Interaction.isMute != 1);
                _previewDelayTimer = TimerManager.Inst.RunOnce("preview_activation_delay", Interaction.delaySecond, () =>
                {
                    if (!string.IsNullOrEmpty(Interaction.audioUrl))
                    {
                        var soundObj = GameObject.Find("GlobalMainCamera");
                        var ugcToneLoader = soundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
                        ugcToneLoader.LoadAudioClipAndPlay(PreviewAudioType.TwoD, Interaction.audioUrl);
                    }
                });
            }
            else
            {
                // delaySecond < 0：立即播语音，延迟 abs(delaySecond) 秒后播动画
                if (!string.IsNullOrEmpty(Interaction.audioUrl))
                {
                    var soundObj = GameObject.Find("GlobalMainCamera");
                    var ugcToneLoader = soundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
                    ugcToneLoader.LoadAudioClipAndPlay(PreviewAudioType.TwoD, Interaction.audioUrl);
                }
                _previewDelayTimer = TimerManager.Inst.RunOnce("preview_activation_delay", Mathf.Abs(Interaction.delaySecond), () =>
                {
                    // isMute == 1 时动画音效静音
                    cabinPgcUgcPlayController.PlayAnim(Interaction.isMute != 1);
                });
            }
        }

        public void BeginPreviewVoiceCommands(voiceCommands voiceCommands)
        {
            // 先停止上一次预览的语音、定时器和等待协程，防止多次点击叠加播放语音
            StopCurrentPreviewAudioAndTimers();
            _standbyAnimCtrl.Stop();
            cabinPgcUgcPlayController.InitData(voiceCommands);
            cabinPgcUgcPlayController.SetPlayEndCallback(EndPreviewActivationAnimOnly);

            if (voiceCommands.delaySecond >= 0)
            {
                // delaySecond >= 0：立即播动画，延迟 delaySecond 秒后播语音
                // isMute == 1 时动画音效静音；语音 audioUrl 始终播放，不受 isMute 控制
                cabinPgcUgcPlayController.PlayAnim(voiceCommands.isMute != 1);
                _previewDelayTimer = TimerManager.Inst.RunOnce("preview_voice_commands_delay", voiceCommands.delaySecond, () =>
                {
                    if (!string.IsNullOrEmpty(voiceCommands.audioUrl))
                    {
                        var soundObj = GameObject.Find("GlobalMainCamera");
                        var ugcToneLoader = soundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
                        ugcToneLoader.LoadAudioClipAndPlay(PreviewAudioType.TwoD, voiceCommands.audioUrl);
                    }
                });
            }
            else
            {
                // delaySecond < 0：立即播语音，延迟 abs(delaySecond) 秒后播动画
                if (!string.IsNullOrEmpty(voiceCommands.audioUrl))
                {
                    var soundObj = GameObject.Find("GlobalMainCamera");
                    var ugcToneLoader = soundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
                    ugcToneLoader.LoadAudioClipAndPlay(PreviewAudioType.TwoD, voiceCommands.audioUrl);
                }
                _previewDelayTimer = TimerManager.Inst.RunOnce("preview_voice_commands_delay", Mathf.Abs(voiceCommands.delaySecond), () =>
                {
                    // isMute == 1 时动画音效静音
                    cabinPgcUgcPlayController.PlayAnim(voiceCommands.isMute != 1);
                });
            }
        }

        void OnShowStandbyPreview()
        {
            var cabinInfo = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();
            var loopList = cabinInfo?.usingEmote?.loopEmoteList;
            if (loopList == null || loopList.Count == 0)
            {
                TipPanel.ShowToast("暂无待机动作");
                return;
            }
            BeginPreviewStandby(loopList[0]);
        }

        void BeginPreviewStandby(pEmoteData pEmoteData)
        {
            cabinPgcUgcPlayController.InitData(pEmoteData);
            cabinPgcUgcPlayController.SetPlayEndCallback(EndPreviewActivation);
            // 待机动画不播放音效
            cabinPgcUgcPlayController.PlayAnim(false);
        }

        /// <summary>
        /// 停止当前预览的语音、延迟定时器和等待协程，防止多次点击叠加播放语音。
        /// 不恢复待机循环。
        /// </summary>
        private void StopCurrentPreviewAudioAndTimers()
        {
            TimerManager.Inst.Stop(_previewDelayTimer);
            _previewDelayTimer = null;

            if (_audioWaitCoroutine != null)
            {
                StopCoroutine(_audioWaitCoroutine);
                _audioWaitCoroutine = null;
            }

            GameObject.Find("GlobalMainCamera")?.GetComponent<UgcToneLoaderBehaviour>()?.Stop();
        }

        public void EndPreviewActivation()
        {
            StopCurrentPreviewAudioAndTimers();
            cabinPgcUgcPlayController.CancelAnim();
            // 预览结束后恢复待机动画循环
            _standbyAnimCtrl.Resume();
        }

        private void EndPreviewActivationAnimOnly()
        {
            TimerManager.Inst.Stop(_previewDelayTimer);
            _previewDelayTimer = null;

            // 检查语音是否仍在播放
            var soundObj = GameObject.Find("GlobalMainCamera");
            var ugcToneLoader = soundObj?.GetComponent<UgcToneLoaderBehaviour>();

            if (ugcToneLoader != null && ugcToneLoader.IsAudioPlaying())
            {
                // 动画结束但语音仍在：切换到 leisure 待机，等语音结束再做完整清理
                cabinPgcUgcPlayController.PlayLeisureIdle();
                _audioWaitCoroutine = StartCoroutine(WaitForAudioAndCleanup());
            }
            else
            {
                // 无语音或语音已结束：直接完整清理
                DoEndPreviewActivationCleanup();
            }
        }

        /// <summary>
        /// 预览完全结束时的清理操作：停止动画、恢复待机循环。
        /// </summary>
        private void DoEndPreviewActivationCleanup()
        {
            cabinPgcUgcPlayController.CancelAnim();
            // 预览结束后恢复待机动画循环
            _standbyAnimCtrl.Resume();
        }

        /// <summary>
        /// 等待语音播放完成后执行完整清理，每 0.1 秒轮询一次 AudioSource.isPlaying。
        /// </summary>
        private IEnumerator WaitForAudioAndCleanup()
        {
            var soundObj = GameObject.Find("GlobalMainCamera");
            var ugcToneLoader = soundObj?.GetComponent<UgcToneLoaderBehaviour>();

            while (ugcToneLoader != null && ugcToneLoader.IsAudioPlaying())
            {
                yield return new WaitForSeconds(0.1f);
            }

            _audioWaitCoroutine = null;
            DoEndPreviewActivationCleanup();
        }
        #endregion

    }
}