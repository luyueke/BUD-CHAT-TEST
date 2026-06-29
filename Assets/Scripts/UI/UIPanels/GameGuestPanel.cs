using Game.Base;
using GameData;
using Message;
using Newtonsoft.Json;
using Pb.Game;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cinemachine;
using GameData.BaseInfo;
using Game.Avatar;
using Game.MusicalInstrument;
using Game.Pet;
using Game.Utils;
using GameData.GameSync;
using Pb.Base;
using UI.UIPanels;
using UI.UIPanels.FittingRoom;
using MapTitle;
using UnityEngine.EventSystems;
using Game.Vehicle.PGCVehicle;
using Game.Audio;
using Game.KinematicCharacter;
using DG.Tweening;

public class GameGuestPanel : BaseGamePlayPanel<GameGuestPanel>
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CButton emoBtn;
    [SerializeField] private CButton editAvatarBtn;
    [SerializeField] private CButton aiBuddyOPBtn;
    [SerializeField] private CButton chatMessageBtn;
    [SerializeField] private CButton roomMenuBtn;
    [SerializeField] private CButton hideUIBtn;
    [SerializeField] private CButton showUIBtn;
    [SerializeField] private CButton roomChatBtn;
    [SerializeField] private CButton retryBtn;
    [SerializeField] private CButton fpsBtn;
    [SerializeField] private CButton tpsBtn;
    [SerializeField] private CButton screenShotBtn;
    [SerializeField] private CButton screenModeBtn;
    [SerializeField] private GameObject rightHideRoot;
    [SerializeField] private GameObject leftTopRoot;
    [SerializeField] private GameObject centerBottomRoot;
    [SerializeField] private PlayGoalView playGoalView;
    [SerializeField] private CButton privateChatBtn;
    [SerializeField] private CButton changeOcBtn;
    [SerializeField] private GameChatWindow gameChatWindow;
    [SerializeField] private CButton unLinkEmoteBtnA;
    [SerializeField] private CButton unLinkEmoteBtnB;
    [SerializeField] private CButton getOutBtn;
    [SerializeField] private Button struggleBtn;          // 被娃娃机抓住时的挣脱按钮
    [SerializeField] private Image struggleFillImage;      // 挣脱QTE进度（circle fill，0~1）
    [SerializeField] private Animation struggleBtnAnim;    // 挣脱按钮点击反馈动画（点一下重头播一下）
    [SerializeField] private ChargeGearView gearUI;
    [SerializeField] private PlayInstrumentButton instrumentButton;


    #region 载具相关

    private bool isSoundBtnDown = false;
    private float soundTimer = 0f;
    private float soundInterval = 1f; // 短鸣冷却时间
    private readonly HashSet<string> _audioBannedUids = new HashSet<string>();

    #endregion
    private Image chatReddot;
    private Text chatReddotText;

    private List<string> cameraNodeNames = new() { "LeftTopRoot","RightHideRoot/BottomRightGroup","RightHideRoot/GoalGroup", "RightHideRoot/PrivateChat"};
    private List<GameObject> camereNodes = new List<GameObject>();
    private GameMode curGameMode = GameMode.Play;
    private CameraModePanel cameraPanel;
    private bool isInCameraMode = false;
    private bool isInVehicleMode = false;
    public MapTitleView mapTitleView;

    private Vector3 originalCameraOffset;
    private Vector3 vehicleCameraOffset;

    public override void OnCreate()
    {
        base.OnCreate();
        emoBtn.onClick.AddListener(OnEmoBtnClick);
        editAvatarBtn.onClick.AddListener(OnEditAvatarBtnClick);
        aiBuddyOPBtn.onClick.AddListener(OnBtnBuddyOPClick);
        roomMenuBtn.onClick.AddListener(OnRoomMenuBtnClick);
        chatMessageBtn.onClick.AddListener(OnChatMessageClick);
        retryBtn.onClick.AddListener(()=>{
            if(AvatarController.Inst.SelfController.Motor.CurUGCVehicleStatus == KinematicCharacterMotor.UGCVehicleStatus.TakeCar)
            {
                TipPanel.ShowToast("乘坐载具中不能返回出生点");
                return;
            }
            else{
                Debug.LogError("RetryBtnClick");
                RetryBtnClick();
            }
        });
        fpsBtn.onClick.AddListener(FpsBtnClick);
        tpsBtn.onClick.AddListener(TpsBtnClick);
        screenShotBtn.onClick.AddListener(ScreenShotBtnClick);
        screenModeBtn.onClick.AddListener(ScreenModeBtnClick);
        hideUIBtn.onClick.AddListener(HideUIBtnClick);
        showUIBtn.onClick.AddListener(ShowUIBtnClick);
        changeOcBtn.onClick.AddListener(OnChangeOcClick);
        roomChatBtn.onClick.AddListener(OnRoomChatBtnClick);
        unLinkEmoteBtnA.onClick.AddListener(OnUnlinkEmoteBtnClick);
        unLinkEmoteBtnB.onClick.AddListener(OnUnlinkEmoteBtnClick);
        getOutBtn.onClick.AddListener(OnGetOutBtnClick);
        getOutBtn.gameObject.SetActive(false);
        struggleBtn.onClick.AddListener(OnStruggleBtnClick);
        struggleBtn.gameObject.SetActive(false);
        privateChatBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel<GameHallChatPanel>(PanelId.GameHallChatPanel);
        });
        chatReddot= GameObjectEx.FindComponentByName<Image>(transform, "ChatReddot");
        chatReddotText = GameObjectEx.FindComponentByName<Text>(transform, "ChatReddotText");
        MessageHelper.AddListener<bool>(MessageName.UICameraMode, OnCameraMode);
        MessageHelper.AddListener<TcpChatData>(MessageName.ChatMessage, ReceiveMessage);
        MessageHelper.AddListener(MessageName.ReddotNotice, UpdateNewIcon);
        MessageHelper.AddListener<bool>(MessageName.LinkEmoteStateChange,OnLinkEmoteStateChange);
        MessageHelper.AddListener<bool>(MessageName.BuddyLinkEmoteStateChange,OnBuddyLinkEmoteStateChange);
        MessageHelper.AddListener(MessageName.LinkEmoteShowUnlink,OnLinkEmoteShowUnlink);
        MessageHelper.AddListener<bool,bool,Vector3>(MessageName.OnSelfGetInVehicle, OnSelfGetInVehicle);
        MessageHelper.AddListener<string, string>(MessageName.OnSelfVehicleCreated, OnSelfVehicleCreated);
        MessageHelper.AddListener<string, string, Vector3>(MessageName.OnSelfPgcVehicleCreated, OnSelfPgcVehicleCreated);
        MessageHelper.AddListener<string>(MessageName.OnPlayerGetOutVehicle, OnPlayerGetOutVehicle);
        MessageHelper.AddListener<string>(MessageName.OnVehicleHonkingStart, PlayHonkAudio);
        MessageHelper.AddListener<string>(MessageName.OnVehicleMovingSoundStart, PlayVehicleStarAudio);
        MessageHelper.AddListener<string>(MessageName.OnVehicleMovingSoundStop, PlayVehicleEndAudio);
        MessageHelper.AddListener<bool>(MessageName.UICameraModeHideUI, OnUICameraModeHideUI);
        MessageHelper.AddListener<string, bool>(MessageName.OnPlayerAudioBanChanged, OnPlayerAudioBanChanged);
        MessageHelper.AddListener<bool>(MessageName.CameraLandMarkTrigEnter, OnCameraLandMarkTrigEnter);
        MessageHelper.AddListener<bool, string>(MessageName.OnSelfBoundStateChange, OnSelfBoundStateChange);
        MessageHelper.AddListener<bool>(MessageName.OnSelfCapturedStateChange, OnSelfCapturedStateChange);
        NetSyncManager.Inst.AddBroadcastListener(SubCmdType.HiddenPet, OnPetHidden);
        NetSyncManager.Inst.AddBroadcastListener(SubCmdType.SetBan, OnSetBanRecv);
        GameVehicleManager.Inst.Init();

        UIShowAbilityManager.Inst.AddBanChangeListener(OnUIAbilityBanChange);
        InitCameraBindComponent();


        int chatNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.Chat);
        chatReddot.gameObject.SetActive(chatNum > 0);
        chatReddotText.text = chatNum>99?"99+":chatNum.ToString();

    }

    private void OnRoomChatBtnClick() {
        gameChatWindow.gameObject.SetActive(!gameChatWindow.gameObject.activeSelf);
    }

    public void PlaySelfTitle()
    {
        TimerManager.Inst.RunOnce("title", 0.4f, () => {
            mapTitleView.isPlaying = false;
        });
    }

    private void OnUnlinkEmoteBtnClick()
    {
        if (AvatarController.Inst.SelfStateController.linkEmoteData != null)
        {
            if (BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(AccountDataManager.Inst.Uid))
            {
                BuddyLinkEmoteManager.Inst.SendExitLinkReq(AvatarController.Inst.SelfStateController.linkEmoteData);
            }
            else
            {
                LinkEmoteManager.Inst.SendExitLinkReq(AvatarController.Inst.SelfStateController.linkEmoteData);
            }
        }
    }


    #region 娃娃机被抓 - 挣脱QTE

    private const int StruggleNeedCount = 5;     // 挣脱所需连按次数
    private const float StruggleQteWindow = 3.5f;  // QTE 窗口（秒）：对齐 skill1End 收回动画时长
    private const string StruggleBanKey = "WawajiRestrain";
    private string _struggleCaptorUid;
    private int _struggleCount;
    private bool _isStruggling;
    private bool _isCapturedEscapable; // 被劫持(Captured)态：按钮显示但无 progress，单击直接挣脱掉落

    // Skill5(娃娃机抓取)按钮可用性轮询：没可抓目标时置灰，有目标立即恢复
    private float _skill5GrabPollTimer;
    private const float Skill5GrabPollInterval = 0.15f;
    private bool _skill5GrabEnabled = true; // 上次下发的可用态，避免每帧重复 SetButtonEnabled
    private BudTimer _struggleQteTimer;
    private BudTimer _captureFallbackTimer;
    private const float CaptureFallbackWait = 3f; // 发出被劫持请求后，等待车主分配锚点的兜底时间

    // 本人进入/退出被束缚（挣扎中）状态
    private void OnSelfBoundStateChange(bool isBound, string captorUid)
    {
        if (isBound)
        {
            _isStruggling = true;
            _struggleCaptorUid = captorUid;
            _struggleCount = 0;
            if (struggleFillImage != null) struggleFillImage.fillAmount = 0f;
            struggleBtn.gameObject.SetActive(true);
            SetRestrainBan(true);
            m_joyStick?.SetJoyStickVisible(false);

            StopStruggleQte();
            _struggleQteTimer = TimerManager.Inst.RunOnce("WawajiStruggleQte", StruggleQteWindow, OnStruggleTimeout);
        }
        else
        {
            _isStruggling = false;
            StopStruggleQte();
            StopCaptureFallback(); // Bound 已退出(去Captured或彻底释放)，兜底使命完成
            struggleBtn.gameObject.SetActive(false);
            if (struggleFillImage != null) struggleFillImage.fillAmount = 0f;
            SetRestrainBan(false);
            m_joyStick?.SetJoyStickVisible(true);
        }
    }

    // 本人进入/退出被劫持（完全束缚）状态
    private void OnSelfCapturedStateChange(bool isCaptured)
    {
        if (isCaptured)
        {
            // 被劫持：仍显示挣脱按钮，但无 progress（不连点），单击直接挣脱掉落。
            // captorUid 复用进 Bound 时记录的 _struggleCaptorUid（Captured 必经 Bound→超时，故有效）。
            _isStruggling = false;
            _isCapturedEscapable = true;
            StopStruggleQte();
            struggleBtn.gameObject.SetActive(true);
            if (struggleFillImage != null) struggleFillImage.fillAmount = 0f; // 无进度显示
            SetRestrainBan(true);
            m_joyStick?.SetJoyStickVisible(false);
        }
        else
        {
            // 被释放（车主放车 / 单击挣脱成功）：恢复
            _isCapturedEscapable = false;
            struggleBtn.gameObject.SetActive(false);
            if (struggleFillImage != null) struggleFillImage.fillAmount = 0f;
            SetRestrainBan(false);
            m_joyStick?.SetJoyStickVisible(true);
        }
    }

    private void OnStruggleBtnClick()
    {
        // 被劫持(Captured)态：单击直接挣脱掉落，无需 progress/连点
        if (_isCapturedEscapable)
        {
            _isCapturedEscapable = false; // 防重复点击；按钮隐藏由 Captured 退出回调驱动
            if (struggleBtnAnim != null)
            {
                struggleBtnAnim.Stop();
                struggleBtnAnim.Play();
            }
            GameVehicleManager.Inst.SendStruggleEscape(_struggleCaptorUid);
            return;
        }

        if (!_isStruggling) return;

        // 点击反馈：每点一次重头播一次按钮动画（Stop 复位再 Play，保证快速连点也能重播）
        if (struggleBtnAnim != null)
        {
            struggleBtnAnim.Stop();
            struggleBtnAnim.Play();
        }

        // 每点一次播一次 struggle 动作：本地即时(本人) + 广播给其他端(本人副本)
        MessageHelper.Broadcast(MessageName.OnSelfStruggleTap);
        GameVehicleManager.Inst.SendStruggleTap(_struggleCaptorUid);

        _struggleCount++;
        if (struggleFillImage != null)
            struggleFillImage.fillAmount = Mathf.Clamp01((float)_struggleCount / StruggleNeedCount);

        if (_struggleCount >= StruggleNeedCount)
        {
            _isStruggling = false;
            StopStruggleQte();
            // 状态退出由 op=21 回包驱动 OnSelfBoundStateChange(false)
            GameVehicleManager.Inst.SendStruggleEscape(_struggleCaptorUid);
        }
    }

    private void OnStruggleTimeout()
    {
        if (!_isStruggling) return;
        _isStruggling = false;
        // 后续 op=22 → 被劫持；状态切换由网络回包驱动
        GameVehicleManager.Inst.SendStruggleFailCaptured(_struggleCaptorUid);
        // 兜底：若车主已离场/未响应，超时后仍卡在Bound，则自救挣脱让全房统一释放
        StopCaptureFallback();
        _captureFallbackTimer = TimerManager.Inst.RunOnce("WawajiCaptureFallback", CaptureFallbackWait, OnCaptureFallbackTimeout);
    }

    private void OnCaptureFallbackTimeout()
    {
        _captureFallbackTimer = null;
        var self = AvatarController.Inst.SelfStateController;
        // 仅当确实仍卡在被束缚态(车主未分配锚点)才自救，避免与正常被劫持流程冲突
        if (self != null && self.ContainsCurrentState(PlayerState.Bound))
        {
            GameVehicleManager.Inst.SendStruggleEscape(_struggleCaptorUid);
        }
    }

    private void StopCaptureFallback()
    {
        if (_captureFallbackTimer != null)
        {
            TimerManager.Inst.Stop(_captureFallbackTimer);
            _captureFallbackTimer = null;
        }
    }

    private void StopStruggleQte()
    {
        if (_struggleQteTimer != null)
        {
            TimerManager.Inst.Stop(_struggleQteTimer);
            _struggleQteTimer = null;
        }
    }

    // 束缚/劫持期间整体隐藏主UI，只留挣脱按钮（struggleBtn 须放在下列3个根节点之外）
    private void SetRestrainBan(bool ban)
    {
        // 1) 整体隐藏主UI分组（含摇杆/聊天/按钮等，复用 HideUI 的根节点）
        if (rightHideRoot != null) rightHideRoot.SetActive(!ban);
        if (leftTopRoot != null) leftTopRoot.SetActive(!ban);
        if (centerBottomRoot != null) centerBottomRoot.SetActive(!ban);

        // 2) 双保险：对注册过的能力按钮做引用计数禁用（覆盖根节点之外的按钮，且多系统叠加安全）
        UIAbility[] all =
        {
            UIAbility.GuestEmoteBtn, UIAbility.GuestJumpBtn, UIAbility.GuestBackToSpawnBtn,
            UIAbility.GuestChangeOcBtn, UIAbility.GuestInstrumentBtn, UIAbility.EnterSelfieBtn,
            UIAbility.GuestVehicleControlBtn, UIAbility.GuestUgcVehicleBtn, UIAbility.GuestCameraModeBtn
        };
        for (int i = 0; i < all.Length; i++)
        {
            if (ban) UIShowAbilityManager.Inst.AddBanBility(all[i], StruggleBanKey);
            else UIShowAbilityManager.Inst.RemoveBanBility(all[i], StruggleBanKey);
        }
    }

    #endregion

    private void UpdateNewIcon()
    {
        if (chatReddot != null)
        {
            int chatNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.Chat);
            chatReddot.gameObject.SetActive(chatNum > 0);
            chatReddotText.text = chatNum>99?"99+":chatNum.ToString();
        }
    }


    private void ReceiveMessage(TcpChatData chatData)
    {
        if (chatData == null)
        {
            return;
        }

        long totalUnReadCount = chatData.totalUnReadCount;
        if (chatReddot != null)
        {
            chatReddot.gameObject.SetActive(totalUnReadCount > 0);
            chatReddotText.text = totalUnReadCount>99?"99+":totalUnReadCount.ToString();
        }
    }

    public override void OnHidden()
    {
        base.OnHidden();
        // 注意：这里可能由 Window Pop 流程触发。
        // BaseWindow.PopFromStack 正在 foreach window.panels 时，
        // 不能再在 OnHidden 里主动 Close 同窗体下其他 panel（会修改同一 List 导致枚举异常）。
        // CameraModePanel 会在 window 出栈时由统一流程处理，这里只清引用避免后续误用。
        cameraPanel = null;
        UIManager.Inst.RemoveClosePanelAction(OnClosePanelAction);
        UIManager.Inst.RemoveOpenPanelAction(OnOpenPanelAction);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        playGoalView.InitGoalView();
        UIManager.Inst.AddClosePanelAction(OnClosePanelAction);
        UIManager.Inst.AddOpenPanelAction(OnOpenPanelAction);
        var camera = GameCameraUtils.Inst.GetPlayVirtualCamera();
        var body = camera.GetCinemachineComponent<CinemachineTransposer>();
        if(body != null){
            originalCameraOffset = body.m_FollowOffset;
        }

    }


    private void InitCameraBindComponent()
    {
        camereNodes.Clear();
        for (var i = 0; i < cameraNodeNames.Count; i++)
        {
            var node = this.transform.Find("Panel/" + cameraNodeNames[i]).gameObject;
            camereNodes.Add(node);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener<bool>(MessageName.UICameraMode, OnCameraMode);
        MessageHelper.RemoveListener<TcpChatData>(MessageName.ChatMessage, ReceiveMessage);
        MessageHelper.RemoveListener(MessageName.ReddotNotice, UpdateNewIcon);
        MessageHelper.RemoveListener<bool>(MessageName.LinkEmoteStateChange,OnLinkEmoteStateChange);
        MessageHelper.RemoveListener<bool>(MessageName.BuddyLinkEmoteStateChange,OnBuddyLinkEmoteStateChange);
        MessageHelper.RemoveListener(MessageName.LinkEmoteShowUnlink,OnLinkEmoteShowUnlink);
        MessageHelper.RemoveListener<bool,bool,Vector3>(MessageName.OnSelfGetInVehicle, OnSelfGetInVehicle);
        MessageHelper.RemoveListener<string, string>(MessageName.OnSelfVehicleCreated, OnSelfVehicleCreated);
        MessageHelper.RemoveListener<string, string, Vector3>(MessageName.OnSelfPgcVehicleCreated, OnSelfPgcVehicleCreated);
        MessageHelper.RemoveListener<string>(MessageName.OnPlayerGetOutVehicle, OnPlayerGetOutVehicle);
        MessageHelper.RemoveListener<string>(MessageName.OnVehicleHonkingStart, PlayHonkAudio);
        MessageHelper.RemoveListener<string>(MessageName.OnVehicleMovingSoundStart, PlayVehicleStarAudio);
        MessageHelper.RemoveListener<string>(MessageName.OnVehicleMovingSoundStop, PlayVehicleEndAudio);
        MessageHelper.RemoveListener<bool>(MessageName.CameraLandMarkTrigEnter, OnCameraLandMarkTrigEnter);
        MessageHelper.RemoveListener<bool>(MessageName.UICameraModeHideUI, OnUICameraModeHideUI);
        MessageHelper.RemoveListener<bool, string>(MessageName.OnSelfBoundStateChange, OnSelfBoundStateChange);
        MessageHelper.RemoveListener<bool>(MessageName.OnSelfCapturedStateChange, OnSelfCapturedStateChange);
        StopStruggleQte();
        StopCaptureFallback();

        MessageHelper.RemoveListener<string, bool>(MessageName.OnPlayerAudioBanChanged, OnPlayerAudioBanChanged);

        NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.HiddenPet, OnPetHidden);
        NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.SetBan, OnSetBanRecv);

        if (UIShowAbilityManager.HasInstance)
        {
            UIShowAbilityManager.Inst.RemoveBanChangeListener(OnUIAbilityBanChange);
            UIShowAbilityManager.Inst.ClearGameGuest();
        }
    }
    
    private void OnPetHidden(CommonSyncClientData netData)
    {
        //排除自己
        if(netData.PalyerId == AccountDataManager.Inst.Uid)
            return;

        var hiddenNetData = (HiddenPetNetData)netData.Body;
        if (hiddenNetData != null)
        {
            var stateCtr = PetAvatarController.Inst.GetPetKCCtrl(netData.PalyerId);
            stateCtr.kinematicCharacterController.gameObject.SetActive(hiddenNetData.Op == 0);
        }
    }

    private void OnCameraMode(bool enterCameraMode)
    {
        for (var i = 0; i < camereNodes.Count; i++)
        {
            camereNodes[i].SetActive(!enterCameraMode);
        }

        if (cameraStick != null)
        {
            cameraStick.GetComponent<Image>().enabled = !enterCameraMode;
        }

        if (enterCameraMode)
        {
            UIShowAbilityManager.Inst.AddBanBility(UIAbility.GuestCameraModeBtn,AbilityKey.CameraMode);
            isInCameraMode = true;
        }
        else
        {
            UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestCameraModeBtn,AbilityKey.CameraMode);
            isInCameraMode = false;
        }
    }


    // private void AdapterLinkUI(bool isLink)
    // {
    //     if (isLink)
    //     {
    //         UIShowAbilityManager.Inst.AddBanBility(UIAbility.GuestEmoteBtn,AbilityKey.LinkEmote);
    //         UIShowAbilityManager.Inst.AddBanBility(UIAbility.GuestChangeOcBtn,AbilityKey.LinkEmote);
    //         UIShowAbilityManager.Inst.AddBanBility(UIAbility.GuestInstrumentBtn,AbilityKey.LinkEmote);
    //         if (LinkEmoteManager.Inst.IsPlayerB(AccountDataManager.Inst.Uid))
    //         {
    //             UIShowAbilityManager.Inst.AddBanBility(UIAbility.GuestBackToSpawnBtn, AbilityKey.LinkEmote);
    //             UIShowAbilityManager.Inst.AddBanBility(UIAbility.GuestJumpBtn,AbilityKey.LinkEmote);
    //         }
    //         
    //     }
    //     else
    //     {
    //         UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestEmoteBtn,AbilityKey.LinkEmote);
    //         UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestJumpBtn,AbilityKey.LinkEmote);
    //         UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestBackToSpawnBtn,AbilityKey.LinkEmote);
    //         UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestChangeOcBtn,AbilityKey.LinkEmote);
    //         UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestInstrumentBtn,AbilityKey.LinkEmote);
    //     }
    // }

    private void OnUIAbilityBanChange(UIAbility ability, bool isBan)
    {
        switch (ability)
        {
            case UIAbility.GuestEmoteBtn:
                if(isInCameraMode){
                    return;
                }
                emoBtn.gameObject.SetActive(!isBan);
                break;
            case UIAbility.GuestJumpBtn:
                m_joyStick?.SetJumpVisible(!isBan);
                break;
            case UIAbility.GuestBackToSpawnBtn:
                retryBtn.gameObject.SetActive(!isBan);
                break;
            case UIAbility.GuestChangeOcBtn:
                if(isInCameraMode){
                    return;
                }
                changeOcBtn.gameObject.SetActive(!isBan);
                editAvatarBtn.gameObject.SetActive(!isBan);
                break;
            case UIAbility.GuestVehicleControlBtn:
                if(isInCameraMode){
                    return;
                }
                m_joyStick?.SetJumpVisible(!isBan);
                changeOcBtn.gameObject.SetActive(!isBan);
                editAvatarBtn.gameObject.SetActive(!isBan);
                emoBtn.gameObject.SetActive(!isBan);
                aiBuddyOPBtn.gameObject.SetActive(!isBan);
                instrumentButton.ChangeBtnStatus(!isBan);
                break;
            case UIAbility.GuestUgcVehicleBtn:
                if(isInCameraMode){
                    return;
                }
                changeOcBtn.gameObject.SetActive(!isBan);
                editAvatarBtn.gameObject.SetActive(!isBan);
                emoBtn.gameObject.SetActive(!isBan);
                aiBuddyOPBtn.gameObject.SetActive(!isBan);
                instrumentButton.ChangeBtnStatus(!isBan);
                break;
            case UIAbility.GuestCameraModeBtn:
                if(isInVehicleMode){
                    return;
                }
                changeOcBtn.gameObject.SetActive(!isBan);
                editAvatarBtn.gameObject.SetActive(!isBan);
                aiBuddyOPBtn.gameObject.SetActive(!isBan);
                emoBtn.gameObject.SetActive(!isBan);
                break;
        }
    }

    private void OnLinkEmoteStateChange(bool isLink)
    {
        if (!isLink || !LinkEmoteManager.Inst.IsPlayerLinking(AccountDataManager.Inst.Uid))
        {
            unLinkEmoteBtnA.gameObject.SetActive(false);
            unLinkEmoteBtnB.gameObject.SetActive(false);
            GameCameraUtils.Inst.SetPlayVirtualDamping(Vector3.zero);
        }
        else
        {
            if (LinkEmoteManager.Inst.IsPlayerA(AccountDataManager.Inst.Uid))
            {
                unLinkEmoteBtnA.gameObject.SetActive(true);
            }
            else if (LinkEmoteManager.Inst.IsPlayerB(AccountDataManager.Inst.Uid))
            {
                GameCameraUtils.Inst.SetPlayVirtualDamping(new Vector3(0.6f,0.6f,0.6f));
            }
        }

        // AdapterLinkUI(isLink);
    }
    
    private void OnBuddyLinkEmoteStateChange(bool isLink)
    {
        if (!isLink || !BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(AccountDataManager.Inst.Uid))
        {
            unLinkEmoteBtnA.gameObject.SetActive(false);
            unLinkEmoteBtnB.gameObject.SetActive(false);
            GameCameraUtils.Inst.SetPlayVirtualDamping(Vector3.zero);
        }
        else
        {
            if (BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(AccountDataManager.Inst.Uid))
            {
                unLinkEmoteBtnA.gameObject.SetActive(true);
            }
        }
    }

    private BudTimer showUnlinkTimer = null;
    private float hideBtnTime = 5; 
    private void OnLinkEmoteShowUnlink()
    {
        if (LinkEmoteManager.Inst.IsPlayerB(AccountDataManager.Inst.Uid))
        {
            unLinkEmoteBtnB.gameObject.SetActive(true);
            
            TimerManager.Inst.Stop(showUnlinkTimer);
            showUnlinkTimer = TimerManager.Inst.RunOnce("showUnLinkBtn", hideBtnTime, () =>
            {
                if (!this) return;
                if (unLinkEmoteBtnB.gameObject.activeSelf)
                {
                    unLinkEmoteBtnB.gameObject.SetActive(false);
                }
            });
        }
    }
    
    private void ExitGuest()
    {
        GameController.ExitGame(() =>
        {
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
            UIManager.Inst.BackToLastWindow();
        });
    }

    private void OnEmoBtnClick()
    {
        rightHideRoot.SetActive(false);
        var emotePanel = UIManager.Inst.OpenPanel<EmoMenuPanel>(PanelId.EmoMenuPanel);
        emotePanel.CloseAction = ()=>ChangeCameraPanel(true);
        ChangeCameraPanel(false);
    }

    private void OnClosePanelAction(BasePanel panel)
    {
        if (panel is EmoMenuPanel)
        {
            rightHideRoot.SetActive(true);
        }

        if (panel is AIBoxBuddyCallPanel)
        {
            rightHideRoot.SetActive(true);
        }

        if (panel is AIBoxBuddyCommandPanel)
        {
            SetBuddyOperateButtonsVisible(true);
        }
    }

    private void OnOpenPanelAction(BasePanel panel)
    {
        if (panel is AIBoxBuddyCommandPanel)
        {
            SetBuddyOperateButtonsVisible(false);
        }
    }

    // 口令面板打开期间隐藏主界面操作按钮，关闭后恢复
    private void SetBuddyOperateButtonsVisible(bool visible)
    {
        chatMessageBtn.gameObject.SetActive(visible);
        changeOcBtn.gameObject.SetActive(visible);
        editAvatarBtn.gameObject.SetActive(visible);
        emoBtn.gameObject.SetActive(visible);
        aiBuddyOPBtn.gameObject.SetActive(visible);
    }

    private void ChangeCameraPanel(bool isShow)
    {
        if (cameraPanel != null)
        {
            cameraPanel.OnEmoPanelShow(isShow);
        }
    }

    private void OnEditAvatarBtnClick()
    {
        UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel).OnCloseAction = OnCloseFittingRoom;
        RoomEditAvatarManager.Inst.OnEnterFittingRoom();
    }

    private void OnBtnBuddyOPClick()
    {
        rightHideRoot.SetActive(false);
        var callPanel = UIManager.Inst.OpenPanel<AIBoxBuddyCallPanel>(PanelId.AIBoxBuddyCallPanel);
        callPanel.CloseAction = ()=>ChangeCameraPanel(true);
        ChangeCameraPanel(false);
    }

    private void OnCloseFittingRoom(BaseAvatarData baseAvatarData)
    {
        if (baseAvatarData == null) {
            baseAvatarData = AvatarDataManager.Inst.SelfCharacterData;
        }

        if (baseAvatarData is CharacterData)
        {
            RoomEditAvatarManager.Inst.OnExitFittingRoom((CharacterData)baseAvatarData);
        }
        else if (baseAvatarData is PetData)
        {
            RoomEditAvatarManager.Inst.OnExitPetFittingRoom((PetData)baseAvatarData);
        }

    }

    private void OnChangeOcClick()
    {
        UIManager.Inst.OpenPanelTakeAni<OcChangePanel>(PanelId.OcChangePanel, OcChangeScene.Play).OnCloseAction = OnCloseFittingRoom;
        RoomEditAvatarManager.Inst.OnEnterFittingRoom();
    }

    private void OnChooseClothBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.ChooseClothePanel);
    }

    private void OnRoomMenuBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.RoomMenuPanel);
    }



    private void FpsBtnClick()
    {
        //todo
    }

    private void TpsBtnClick()
    {
        //todo
    }


    private void ScreenModeBtnClick()
    {
        // if (AvatarController.Inst.SelfController.Motor.IsDriveVehicle)
        // {
        //     TipPanel.ShowToast("驾驶载具中不允许自拍");
        //     return;
        // }
        cameraPanel = UIManager.Inst.OpenPanel<CameraModePanel>(PanelId.CameraModePanel);
        if(cameraModeBtnTweener != null){
            cameraModeBtnTweener.Kill();
            screenModeBtn.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }



    private void HideUIBtnClick()
    {
        //todo
        showUIBtn.gameObject.SetActive(true);
        hideUIBtn.gameObject.SetActive(false);
        rightHideRoot.SetActive(false);
        leftTopRoot.SetActive(false);
        centerBottomRoot.SetActive(false);
    }

    private void ShowUIBtnClick()
    {
        //todo
        showUIBtn.gameObject.SetActive(false);
        hideUIBtn.gameObject.SetActive(true);
        rightHideRoot.SetActive(true);
        leftTopRoot.SetActive(true);
        centerBottomRoot.SetActive(true);
    }

    void OnChatMessageClick()
    {
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("输入消息..."),
            inputMode = 2,
            maxLength = 60,
            inputFlag = 0,
            textSecurity = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            defaultText = "",
            returnKeyType = (int)ReturnType.Send,
            source = (int)KeyboardSource.RoomChat
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, KeyboardReturn);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(keyBoardInfo));
    }

    void KeyboardReturn(string str)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        // 发送消息
        var selfChatBubble = AccountDataManager.Inst.UserInfo.chatBubbles;
        ChatNetData netData = new ChatNetData();
        netData.Content = str;
        netData.ChatBubbles = selfChatBubble;
        NetSyncManager.Inst.SendAllRoom(SubCmdType.Chat, netData);
    }

    public void HidePanel()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
    }

    public void ShowPanel()
    {
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
    }

    public void SetAIBuddyChatReddot(bool isShowAIBuddy)
    {
        int chatNum = 0;
        if (isShowAIBuddy)
        {
            chatNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.Chat) + 1;

        }
        else
        {
            chatNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.Chat) - 1;
        }
        chatReddot.gameObject.SetActive(chatNum > 0);
        chatReddotText.text = chatNum>99?"99+":chatNum.ToString();
    }

    /// <summary>
    /// 设置冲突按钮的交互状态, 防止一些按钮在长按状态下切换到别的全屏页面无法调起取消长按
    /// </summary>
    /// <param name="isInteractable"></param>
    private void SetConflictBtnInteract(bool isInteractable){
        screenModeBtn.interactable = isInteractable;
        privateChatBtn.interactable = isInteractable;
        roomChatBtn.interactable = isInteractable;
        retryBtn.interactable = isInteractable;
    }

    private void OnSkill1BtnDown()
    {
        SetConflictBtnInteract(false);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill1, true);
    }

    private void OnSkill2BtnDown()
    {
        SetConflictBtnInteract(false);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill2, true);
    }

    private void OnSkill1BtnUp()
    {
        SetConflictBtnInteract(true);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill1, false);
    }

    private void OnSkill2BtnUp()
    {
        SetConflictBtnInteract(true);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill2, false);
    }

    private void OnSkill3BtnDown()
    {
        SetConflictBtnInteract(false);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill3, true);
    }

    private void OnSkill4BtnDown()
    {
        SetConflictBtnInteract(false);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill4, true);
    }

    private void OnSkill3BtnUp()
    {
        SetConflictBtnInteract(true);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill3, false);
    }

    private void OnSkill4BtnUp()
    {
        SetConflictBtnInteract(true);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill4, false);
    }

    private void OnSkill5BtnDown()
    {
        SetConflictBtnInteract(false);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill5, true);
    }

    private void OnSkill5BtnUp()
    {
        SetConflictBtnInteract(true);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill5, false);
    }

    private void OnSkill6BtnDown()
    {
        SetConflictBtnInteract(false);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill6, true);
    }

    private void OnSkill6BtnUp()
    {
        SetConflictBtnInteract(true);
        MessageHelper.Broadcast(MessageName.OnPlayerUseSkill, (int)SkillType.Skill6, false);
    }

    private void OnControlControlBtnDown()
    {
        if(AvatarController.Inst.SelfStateController != null){
            if(AvatarController.Inst.SelfStateController.stateMachine.ContainsCurrentState(PlayerState.PGCVehicle)){
                PGCVehicleManager.Inst.CancelDrive(AccountDataManager.Inst.UserInfo.uid, false);
                GameVehicleManager.Inst.SendControlPGCVehicle(AccountDataManager.Inst.UserInfo.uid, false);
                OnUIAbilityBanChange(UIAbility.GuestVehicleControlBtn, false);
                instrumentButton.ChangeBtnStatus(false);
                isInVehicleMode = false;
                mobileInputStrategy.HideAllButtons(new List<InputStrategyType>(){InputStrategyType.Control});
            }else{
                PGCVehicleManager.Inst.EnterDrive(AccountDataManager.Inst.UserInfo.uid);
                GameVehicleManager.Inst.SendControlPGCVehicle(AccountDataManager.Inst.UserInfo.uid, true);
                OnUIAbilityBanChange(UIAbility.GuestVehicleControlBtn, true);
                isInVehicleMode = true;
                mobileInputStrategy.ShowAllButtons(new List<InputStrategyType>(){InputStrategyType.Control});
            }
        }
    }

    private void OnControlControlBtnUp()
    {
        MessageHelper.Broadcast(MessageName.OnPlayerControlVehicle, false);
    }

    private void OnJumpBtnDown()
    {
        m_joyStick.SimulatorJump();
    }
    private int OpIndex = 16;
    private void OnShowBtnDown()
    {
        OpIndex = OpIndex == 15 ? 16 : 15;
        // 经 GameVehicleManager 单一真源合并发送（保留娃娃机 capturedSlots，避免覆盖抓取持久化）
        GameVehicleManager.Inst.SyncSelfBannerShow(OpIndex == 15);
        MessageHelper.Broadcast(MessageName.OnPlayerVehicleShow,OpIndex==15);
    }
    private void OnShowNameBtnDown()
    {
        UIManager.Inst.OpenPanel(PanelId.ChangeVehicleBannerPanel);
    }
    //长鸣笛改短鸣笛了
    private void OnSoundBtnDown()
    {

    }

    private void OnSoundBtnUp()
    {
        if(isSoundBtnDown){
            return;
        }
        isSoundBtnDown = true;
        soundTimer = 0f;
        MessageHelper.Broadcast(MessageName.OnVehicleTryHonking, AccountDataManager.Inst.UserInfo.uid);
        MessageHelper.Broadcast(MessageName.OnHonkingCoolDown, soundInterval);
    }

    private void OnGetOutBtnClick()
    {
        getOutBtn.gameObject.SetActive(false);
        OnUIAbilityBanChange(UIAbility.GuestVehicleControlBtn, false);
        isInVehicleMode = false;
        MessageHelper.Broadcast(MessageName.OnPlayerTryGetOutVehicle, AccountDataManager.Inst.UserInfo.uid);
    }

    private void OnSelfGetInVehicle(bool isSelf,bool isPgc, Vector3 cameraOffset)
    {
        gearUI.gameObject.SetActive(!isSelf && !isPgc); //自己不是乘客，才显示换档

        if(!isSelf){
            //自己不是乘客不显示乘客UI
            return;
        }
        getOutBtn.gameObject.SetActive(true);
        OnUIAbilityBanChange(UIAbility.GuestVehicleControlBtn, true);
        isInVehicleMode = true;
        if(isPgc){
            SetPlayerVehicleCameraOffset(cameraOffset);
        }
    }

    private MobileInputStrategy mobileInputStrategy;
    private string defaultInput = "Assets/Loadable/UI/UIPanel/GamePlayPanel/Input/DefaultInput.prefab";

    private void OnSelfPgcVehicleCreated(string uid, string inputPrefabPath, Vector3 cameraOffset)
    {
        if (!AccountDataManager.Inst.IsMySelf(uid))
        {
            return;
        }
        getOutBtn.gameObject.SetActive(true);

        OnUIAbilityBanChange(UIAbility.GuestVehicleControlBtn, true);
        isInVehicleMode = true;
        VehicleCreated(uid, inputPrefabPath);
        SetPlayerVehicleCameraOffset(cameraOffset);
        gearUI.gameObject.SetActive(false);
        //通知载具UI初始化
        PGCVehicleManager.Inst.OnUIInit(uid);
    }

    private void OnSelfVehicleCreated(string uid, string inputPrefabPath)
    {
        if(!AccountDataManager.Inst.IsMySelf(uid))
        {
            return;
        }
        OnUIAbilityBanChange(UIAbility.GuestVehicleControlBtn, true);
        isInVehicleMode = true;
        VehicleCreated(uid, inputPrefabPath);
        gearUI.gameObject.SetActive(true);
        gearUI.SetVehicleGear(VehicleGear.Gear2);
        PlayStartAudio(uid);
    }

    private void VehicleCreated(string uid, string inputPrefabPath)
    {
        getOutBtn.gameObject.SetActive(true);

        string path = inputPrefabPath;
        if (string.IsNullOrEmpty(path))
        {
            path = defaultInput;
        }
        TipPanel.HideToast("载具召唤中");
        var inputPrefab = Loader.Load<GameObject>(path, gameObject);
        var inputStrategy = GameObject.Instantiate(inputPrefab, gameObject.transform);
        if (inputStrategy.TryGetComponent(out mobileInputStrategy))
        {
            mobileInputStrategy.Init();
            
            if (mobileInputStrategy.IsButtonExist(InputStrategyType.Honking))
            {
                mobileInputStrategy.AddButtonDownEvent(InputStrategyType.Honking, OnSoundBtnDown);
                mobileInputStrategy.AddButtonUpEvent(InputStrategyType.Honking, OnSoundBtnUp);
            }
            if (mobileInputStrategy.IsButtonExist(InputStrategyType.Jump))
            {
                mobileInputStrategy.AddButtonDownEvent(InputStrategyType.Jump, OnJumpBtnDown);
            }
            if (mobileInputStrategy.IsButtonExist(InputStrategyType.Skill1))
            {
                mobileInputStrategy.AddButtonDownEvent(InputStrategyType.Skill1, OnSkill1BtnDown);
                mobileInputStrategy.AddButtonUpEvent(InputStrategyType.Skill1, OnSkill1BtnUp);
            }
            if (mobileInputStrategy.IsButtonExist(InputStrategyType.Skill2))
            {
                mobileInputStrategy.AddButtonDownEvent(InputStrategyType.Skill2, OnSkill2BtnDown);
                mobileInputStrategy.AddButtonUpEvent(InputStrategyType.Skill2, OnSkill2BtnUp);
            }
            if (mobileInputStrategy.IsButtonExist(InputStrategyType.Control))
            {
                mobileInputStrategy.AddButtonDownEvent(InputStrategyType.Control, OnControlControlBtnDown);
                mobileInputStrategy.AddButtonUpEvent(InputStrategyType.Control, OnControlControlBtnUp);
            }
            if (mobileInputStrategy.IsButtonExist(InputStrategyType.Skill3))
            {
                mobileInputStrategy.AddButtonDownEvent(InputStrategyType.Skill3, OnSkill3BtnDown);
                mobileInputStrategy.AddButtonUpEvent(InputStrategyType.Skill3, OnSkill3BtnUp);
            }
            if (mobileInputStrategy.IsButtonExist(InputStrategyType.Skill4))
            {
                mobileInputStrategy.AddButtonDownEvent(InputStrategyType.Skill4, OnSkill4BtnDown);
                mobileInputStrategy.AddButtonUpEvent(InputStrategyType.Skill4, OnSkill4BtnUp);
            }
            if (mobileInputStrategy.IsButtonExist(InputStrategyType.Skill5))
            {
                mobileInputStrategy.AddButtonDownEvent(InputStrategyType.Skill5, OnSkill5BtnDown);
                mobileInputStrategy.AddButtonUpEvent(InputStrategyType.Skill5, OnSkill5BtnUp);
                // 复位可用态基线为"明亮"，并强制下一帧立刻轮询：保证刚上车若无目标会被置灰
                _skill5GrabEnabled = true;
                _skill5GrabPollTimer = Skill5GrabPollInterval;
            }
            if (mobileInputStrategy.IsButtonExist(InputStrategyType.Skill6))
            {
                mobileInputStrategy.AddButtonDownEvent(InputStrategyType.Skill6, OnSkill6BtnDown);
                mobileInputStrategy.AddButtonUpEvent(InputStrategyType.Skill6, OnSkill6BtnUp);
            }
            if (mobileInputStrategy.IsButtonExist(InputStrategyType.ShowBanner))
            {
                mobileInputStrategy.AddButtonDownEvent(InputStrategyType.ShowBanner, OnShowBtnDown);
            }
            if (mobileInputStrategy.IsButtonExist(InputStrategyType.ShowBannerName))
            {
                mobileInputStrategy.AddButtonDownEvent(InputStrategyType.ShowBannerName, OnShowNameBtnDown);
            }
        }

    }



    private void SetPlayerVehicleCameraOffset(Vector3 cameraOffset)
    {
        vehicleCameraOffset = cameraOffset;
        var camera = GameCameraUtils.Inst.GetPlayVirtualCamera();
        var body = camera.GetCinemachineComponent<CinemachineTransposer>();
        if(body != null){
            originalCameraOffset = body.m_FollowOffset;
            body.m_FollowOffset = cameraOffset;
        }
    }

    private void ResetPlayerVehicleCameraOffset()
    {
        var camera = GameCameraUtils.Inst.GetPlayVirtualCamera();
        var body = camera.GetCinemachineComponent<CinemachineTransposer>();
        if(body != null){
            body.m_FollowOffset = originalCameraOffset;
        }
    }

    private void OnPlayerGetOutVehicle(string uid)
    {
        if(!AccountDataManager.Inst.IsMySelf(uid))
        {
            return;
        }
        getOutBtn.gameObject.SetActive(false);
        if(mobileInputStrategy != null){
            GameObject.Destroy(mobileInputStrategy.gameObject);
            mobileInputStrategy = null;
        }

        ResetPlayerVehicleCameraOffset();

        OnUIAbilityBanChange(UIAbility.GuestVehicleControlBtn, false);
        isInVehicleMode = false;
    }

    protected override void Update() {
        base.Update();
        if(isSoundBtnDown){
            // 玩家一直按着喇叭的情况，每隔一段时间广播一次鸣笛事件
            soundTimer += Time.deltaTime;
            if(soundTimer >= soundInterval){
                soundTimer = 0f;
                isSoundBtnDown = false;
            }
        }

        PollSkill5GrabAvailability();
    }

    // 轮询自己娃娃机是否有可抓目标，无则把 Skill5 按钮置灰、有则立即恢复（仅驾驶娃娃机时生效）
    private void PollSkill5GrabAvailability()
    {
        // Skill5 绑定只存在于娃娃机输入预制体，借此判断"是否在开娃娃机"
        if(mobileInputStrategy == null || !mobileInputStrategy.IsButtonExist(InputStrategyType.Skill5)) return;

        _skill5GrabPollTimer += Time.deltaTime;
        if(_skill5GrabPollTimer < Skill5GrabPollInterval) return;
        _skill5GrabPollTimer = 0f;

        var ctrl = PGCVehicleManager.Inst.GetPGCVehicleController(AccountDataManager.Inst.Uid);
        bool canGrab = ctrl != null && ctrl.VehicleKinematic != null && ctrl.VehicleKinematic.HasGrabbableTarget();
        if(canGrab != _skill5GrabEnabled)
        {
            _skill5GrabEnabled = canGrab;
            mobileInputStrategy.SetButtonEnabled(InputStrategyType.Skill5, canGrab);
        }
    }

    private void PlayStartAudio(string DriverUid)
    {
        if (IsDriverAudioBanned(DriverUid))
        {
            StopVehicleAudioOnly(DriverUid);
            return;
        }
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(DriverUid);
        if(playerStateCtrl == null)
        {
            return;
        }
        var avatar = playerStateCtrl.Wrap.Avatar;
        var _chaData = playerStateCtrl.Wrap.ChaData;

        if(_chaData.vehicleData== null || _chaData.vehicleData.vehicleAudio == null)
        {
            return;
        }

        var vehicleAudio = _chaData.vehicleData.vehicleAudio;

        if (string.IsNullOrEmpty(vehicleAudio.starUrl))
        {
            var audio = vehicleAudio.starWwise;
            if(audio == null)
            {
                Debug.LogWarning("PlayHonkAudio: audio is null");
                return;
            }
            AkSoundManager.Inst.PlaySound(audio.group, audio.switchs, audio.wwise3P, avatar);
        }
        else
        {
            var audioPos = avatar.transform.Find("dialogpos").GetChild(0);
            if(audioPos == null)
            {
                Debug.LogWarning("没有找到对应创建的载具");
                return;
            }
            AkSoundManager.Inst.PlayUGCAudioByUrl(vehicleAudio.starUrl, loop: false, audioPos.gameObject,true);
        }
    }

    private void PlayHonkAudio(string DriverUid)
    {
        if (IsDriverAudioBanned(DriverUid))
        {
            StopVehicleAudioOnly(DriverUid);
            return;
        }
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(DriverUid);
        if(playerStateCtrl == null)
        {
            return;
        }
        var avatar = playerStateCtrl.Wrap.Avatar;
        var _chaData = playerStateCtrl.Wrap.ChaData;

        if(_chaData.vehicleData== null || _chaData.vehicleData.vehicleAudio == null)
        {
            return;
        }

        var vehicleAudio = _chaData.vehicleData.vehicleAudio;

        if (string.IsNullOrEmpty(vehicleAudio.hornUrl))
        {
            var audio = vehicleAudio.hornWwise;
            if(audio == null)
            {
                Debug.LogWarning("PlayHonkAudio: audio is null");
                return;
            }
            AkSoundManager.Inst.PlaySound(audio.group, audio.switchs, audio.wwise3P, avatar);
        }
        else
        {
            var audioPos = avatar.transform.Find("dialogpos").GetChild(0);
            if(audioPos == null)
            {
                Debug.LogWarning("没有找到对应创建的载具");
                return;
            }
            AkSoundManager.Inst.PlayUGCAudioByUrl(vehicleAudio.hornUrl, loop: false, audioPos.gameObject,true);
        }
    }

    private void PlayVehicleStarAudio(string DriverUid)
    {
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(DriverUid);
        if(playerStateCtrl == null)
        {
            return;
        }
        var avatar = playerStateCtrl.Wrap.Avatar;
        var _chaData = playerStateCtrl.Wrap.ChaData;

        if(_chaData.vehicleData == null || _chaData.vehicleData.vehicleAudio == null)
        {
            return;
        }

        var vehicleAudio = _chaData.vehicleData.vehicleAudio;
        
        if (IsDriverAudioBanned(DriverUid))
        {
            // 只禁用音频，不影响载具动画表现
            StopVehicleAudioOnly(DriverUid);
        }
        else
        {
            // 进入行驶音效前：停止“启动音效”（star），避免启动音效与行驶循环音效叠加或残留
            if (vehicleAudio != null)
            {
                if (string.IsNullOrEmpty(vehicleAudio.starUrl))
                {
                    var star = vehicleAudio.starWwise;
                    if (star != null && !string.IsNullOrEmpty(star.stopWwise3P))
                    {
                        AkSoundManager.Inst.StopSound(star.stopWwise3P, avatar);
                    }
                }
                else
                {
                    var startAudioPosRoot = avatar != null ? avatar.transform.Find("dialogpos") : null;
                    var startAudioPos = startAudioPosRoot != null && startAudioPosRoot.childCount > 0 ? startAudioPosRoot.GetChild(0) : null;
                    if (startAudioPos != null)
                    {
                        AkSoundManager.Inst.StopUGCAudio(startAudioPos.gameObject);
                    }
                }
            }

            if(string.IsNullOrEmpty(vehicleAudio.driveUrl))
            {
                var audio = vehicleAudio.driveWwise;
                if (audio != null)
                {
                    AkSoundManager.Inst.PlaySound(audio.group, audio.switchs, audio.wwise3P, avatar);
                }
            }
            else
            {
                var audioPos = avatar.transform.Find("AvatarNode");
                if(audioPos == null)
                {
                    Debug.LogWarning("没有找到对应创建的载具");
                    return;
                }
                AkSoundManager.Inst.PlayUGCAudioByUrl(vehicleAudio.driveUrl, loop: true, audioPos.gameObject,true);
            }
        }

        var animator = avatar.transform.parent.parent.GetComponent<Animator>();
        if(animator != null)
        {
            if(_chaData.vehicleData.runAniType != 0)
            {
                animator.enabled = true;
                animator.Play(string.Format("ugc_car0{0}_run", _chaData.vehicleData.runAniType));
            }
            else
            {
                animator.enabled = false;
            }
        }
    }

    private void PlayVehicleEndAudio(string DriverUid)
    {
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(DriverUid);
        if(playerStateCtrl == null)
        {
            return;
        }
        var avatar = playerStateCtrl.Wrap.Avatar;
        var _chaData = playerStateCtrl.Wrap.ChaData;

        if (_chaData.vehicleData == null ||_chaData.vehicleData.vehicleAudio == null)
        {
            return;
        }
        var vehicleAudio = _chaData.vehicleData.vehicleAudio;

        if (vehicleAudio != null)
        {
            if (string.IsNullOrEmpty(vehicleAudio.starUrl))
            {
                var star = vehicleAudio.starWwise;
                if (star != null && !string.IsNullOrEmpty(star.stopWwise3P))
                {
                    AkSoundManager.Inst.StopSound(star.stopWwise3P, avatar);
                }
            }
            else
            {
                var startAudioPosRoot = avatar != null ? avatar.transform.Find("dialogpos") : null;
                var startAudioPos = startAudioPosRoot != null && startAudioPosRoot.childCount > 0 ? startAudioPosRoot.GetChild(0) : null;
                if (startAudioPos != null)
                {
                    AkSoundManager.Inst.StopUGCAudio(startAudioPos.gameObject);
                }
            }
        }

        if(string.IsNullOrEmpty(vehicleAudio.driveUrl))
        {
            var audio = vehicleAudio.driveWwise;
            if (audio != null)
            {
                AkSoundManager.Inst.StopSound(audio.stopWwise3P, avatar);
            }
        }
        else
        {
            var audioPos = avatar.transform.Find("AvatarNode");
            if(audioPos == null)
            {
                Debug.LogWarning("没有找到对应创建的载具");
                return;
            }
            AkSoundManager.Inst.StopUGCAudio(audioPos.gameObject);
        }
        var animator = avatar.transform.parent.parent.GetComponent<Animator>();
        if(animator != null)
        {
            if(_chaData.vehicleData.endAniType != 0)
            {
                animator.enabled = true;
                animator.Play(string.Format("ugc_car0{0}_Idle", _chaData.vehicleData.endAniType));
            }
            else
            {
                animator.enabled = false;
            }
        }
    }

    private void OnUICameraModeHideUI(bool isHide)
    {
        if(isHide){
            chatMessageBtn.gameObject.SetActive(false);
            m_joyStick?.SetJoyStickVisible(false);
        }
        else{
            chatMessageBtn.gameObject.SetActive(true);
            m_joyStick?.SetJoyStickVisible(true);
        }
    }

    private bool IsDriverAudioBanned(string uid)
    {
        return !string.IsNullOrEmpty(uid) && _audioBannedUids.Contains(uid);
    }

    private void OnPlayerAudioBanChanged(string uid, bool isAudioBanned)
    {
        if (string.IsNullOrEmpty(uid)) return;
        if (isAudioBanned)
        {
            _audioBannedUids.Add(uid);
            StopVehicleAudioOnly(uid);
        }
        else
        {
            _audioBannedUids.Remove(uid);
        }
    }

    private void OnSetBanRecv(CommonSyncClientData netData)
    {
        var banNetData = netData?.Body as SetBanNetData;
        if (banNetData == null) return;
        if (banNetData.BanType != 2) return; // 2 = 闭麦
        if (string.IsNullOrEmpty(banNetData.ToUid)) return;

        // SetType: 1=设置禁用, 2=取消禁用（根据 RoomMenuPanel 现有用法约定）
        var isBan = banNetData.SetType == 1;
        OnPlayerAudioBanChanged(banNetData.ToUid, isBan);
    }

    private void StopVehicleAudioOnly(string driverUid)
    {
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(driverUid);
        if (playerStateCtrl == null) return;

        var avatar = playerStateCtrl.Wrap.Avatar;
        var chaData = playerStateCtrl.Wrap.ChaData;
        var vehicleAudio = chaData?.vehicleData?.vehicleAudio;
        if (avatar == null) return;

        // Stop UGC vehicle audio (if any)
        var avatarNode = avatar.transform.Find("AvatarNode");
        if (avatarNode != null)
        {
            AkSoundManager.Inst.StopUGCAudio(avatarNode.gameObject);
        }

        var dialogPosRoot = avatar.transform.Find("dialogpos");
        var dialogPos = dialogPosRoot != null && dialogPosRoot.childCount > 0 ? dialogPosRoot.GetChild(0) : null;
        if (dialogPos != null)
        {
            AkSoundManager.Inst.StopUGCAudio(dialogPos.gameObject);
        }

        if (vehicleAudio == null) return;

        if (string.IsNullOrEmpty(vehicleAudio.starUrl))
        {
            var star = vehicleAudio.starWwise;
            if (star != null && !string.IsNullOrEmpty(star.stopWwise3P))
            {
                AkSoundManager.Inst.StopSound(star.stopWwise3P, avatar);
            }
        }

        if (string.IsNullOrEmpty(vehicleAudio.driveUrl))
        {
            var drive = vehicleAudio.driveWwise;
            if (drive != null && !string.IsNullOrEmpty(drive.stopWwise3P))
            {
                AkSoundManager.Inst.StopSound(drive.stopWwise3P, avatar);
            }
        }
    }

    Tweener cameraModeBtnTweener;

    private void OnCameraLandMarkTrigEnter(bool isEnter)
    {
        //收到是True的时候，让screenModeBtn像iosapp需要删除的时候那种左右角度摇摆，从-10到10来回摇摆
        if(isEnter){
            cameraModeBtnTweener?.Kill();
            screenModeBtn.transform.localRotation = Quaternion.Euler(0, 0, -10);
            cameraModeBtnTweener = screenModeBtn.transform.DOLocalRotate(new Vector3(0, 0, 10), 0.12f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }else{
            cameraModeBtnTweener?.Kill();
            screenModeBtn.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }
}

