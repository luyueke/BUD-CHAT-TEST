using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AIGame.Base;
using Basic;
using Basic.Utils;
using BUD.AnimPose;
using BUD.MailBox;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.AIResData;
using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.Database;
using Game.Event;
using Game.GameSetting;
using Game.Pet;
using Game.Store;
using GameData;
using GameData.Account;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Base;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.LobbyCharacterIdlePanel;
using UI.UIPanels.ProfilePanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;
using View.UI.PopupPanelSystem;
using EventTracking;
using UI.UIPanels.FittingRoom;
using Newbie;
using DG.Tweening;
using GameUI;
using System.Reflection;
using Game.Vehicle.PGCVehicle;
using UI.Manager;
using System.IO;
using Sirenix.OdinInspector;
/// <summary>
/// Author:
/// Desc:
/// Date:23-07-17 17:13:59
/// </summary>
public class GameHallPanel : BasePanel<GameHallPanel>
{
    [SerializeField] private Transform AvatarParent;
    [SerializeField] private Camera AvatarCamera;
    // AvatarCamera 的原始本地位置（OnCreate 时记录一次，相机尚未被任何动作移动）。
    // 用于在切换动作时还原相机，避免被上一次动作移动后的位置污染。
    private Vector3 _avatarCameraDefaultLocalPos;
    [SerializeField] private CButton seasonPassBtn;
    [SerializeField] private UIDragUtil DragUtil;

    [SerializeField] private CButton npcStudioBtn;
    [SerializeField] private CButton npcStoreBtn;
    [SerializeField] private CButton communityGameBtn;
    [SerializeField] private CButton changeOutfitBtn;
    [SerializeField] private CButton changePetOutfitBtn;
    [SerializeField] private CButton profileInfoBtn;
    [SerializeField] private CButton activityBtn;
    [SerializeField] private CButton anniversaryCelebrationBtn;
    [SerializeField] private CButton newbieTaskBtn;
    [SerializeField] private CButton newbieTaskV2Btn;
    [SerializeField] private CButton newbieTaskV3Btn;
    [SerializeField] private CButton rechargeBtn;
    [SerializeField] private CButton storeBtn;
    [SerializeField] private Image vipRewardIcon;
    [SerializeField] private CButton creatorBtn;
    // [SerializeField] private CButton pinkCoinBtn;
    [SerializeField] private GameHallPinkCoin gameHallPinkCoin;
    [SerializeField] private FittingRoomNewBieTaskBtn newBieSevenDay;
    [HideInInspector]
    private GameHallFriendView FriendView;
    [SerializeField] private CButton roomCodeBtn;
    [SerializeField] private CButton mailboxBtn;
    [SerializeField] private CButton chatBtn;
    [SerializeField] private Transform topLine;
    [SerializeField] private CButton settingBtn;
    [SerializeField] private CButton aiCompanionBtn;
    [SerializeField] private CButton IncubationCabinBLEWifiBtn;


    private Text userName;
    private Image chatReddot;
    private Text chatReddotText;

    private Camera bgCamera;
    public CharacterWrap CharacterWrap { get { return _characterWrap; } }
    private CharacterWrap _characterWrap;
    private PlayerIdleBehaviour idleBehaviour;
    private PetWrap _petWrap;
    private PetPlayerIdleBehaviour petIdleBehaviour;
    private CharacterWrap _npcWrap;
    private NpcPlayerIdleBehaviour npcIdleBehaviour;

    [SerializeField] private RedDotItem mailboxRedDot;
    [SerializeField] private RedDotItem activityCenterRedDot;
    [SerializeField] private RedDotItem gashaponRedDot;
    [SerializeField] private RedDotItem avatarRedDot;
    [SerializeField] private RedDotItem petAvatarRedDot;
    [SerializeField] private RedDotItem creatorRedDot;
    [SerializeField] private ContestBanner contestBanner;
    [SerializeField] private CButton GMBtn;
    [SerializeField] private CButton Btn_LimitedPack;
    [SerializeField] private GameObject Reddot_LimitedPack;
    [SerializeField] private CButton Btn_SendGift;


    [SerializeField] private CButton changeIdleBtn;
    [SerializeField] private CButton changeOcBtn;
    [SerializeField] private CButton Btn_AnimStudio;
    [SerializeField] private CButton Btn_Studios;
    [SerializeField] private HeadViewWidget HeadViewWidget;
    [SerializeField] private RedDotItem newbieRedDot;
    [SerializeField] private RedDotItem newbieV2RedDot;
    [SerializeField] private RedDotItem newbieV3RedDot;
    [SerializeField] private CButton Btn_AIYandere;
    [SerializeField] private CButton BtnAIBuddy;
    [SerializeField] private CButton BtnContent;
    [SerializeField] private CButton BtnBuyPinkCoin;
    [SerializeField] private CButton BtnFirstRecharge;
    [SerializeField] public CButton newBieBreakIceBtn;
    [SerializeField] public GameObject newBieBreakIceRedDot;
    [SerializeField] private CButton Btn_S9MainEntry;
    [SerializeField] private CButton Btn_Season;
    [SerializeField] private GameObject v2TaskView;
    [SerializeField] private GameObject v3TaskView; 
    [SerializeField] private CButton TheatreBtn;
    string key = "firsOpenGame" + AccountDataManager.Inst.UserInfo.uid;
    public bool _isTopBtnClose;

    private bool isSetBodyPos = false;

    private Vector3 originalAvatarParentPos;

    private bool blanceInfoBack = false;
    private bool iapListBack = false;
    private bool hasReport = false;

    private enum PreRequestType
    {
        None = 0,
        Pre = 1,
        Next = 2
    }

    public override void OnCreate()
    {
        _ = SeasonCumulativeSystem.Inst;
        if (DeviceInfoManager.Inst.CheckVersion_is_1_0_14())
        {
            var cgVideo = GameObject.Find("UIRoot/Canvas/CGVedio");
            if (cgVideo != null)
            {
                Debug.Log("Have CgVideo");
                GameObject.Destroy(cgVideo);
                // cgVideo.gameObject.SetActive(false);
            }
        }
        aiCompanionBtn.gameObject.SetActive(false);
        isSetBodyPos = false;
        if(AvatarParent != null){
            originalAvatarParentPos = AvatarParent.localPosition;
        }
        if(AvatarCamera != null){
            _avatarCameraDefaultLocalPos = AvatarCamera.transform.localPosition;
        }

        communityGameBtn.gameObject.SetActive(false);
        DestroyGlobalAudioListener();
        // 检查设备磁盘剩余大小
        DickSpaceCheck();
        var userInfo = AccountDataManager.Inst.UserInfo;
        // 初始化背包数据 目前没有登录成功后的地方，所以暂时放这里
        BagDatabase.Inst.Initialize(userInfo.uid);
        AssetsDataManager.Init();
        AssetPropNodeManager.Inst.Init();
        AccountDataManager.Inst.BalanceInfo.Refresh(()=> {
            blanceInfoBack = true;
            //ReportLoginHall();
        });
        TokenDataManager.Inst.GetTokenData((_tokenData) => {
        }, (fil) => {
            Debug.LogError("获取Token数据失败：" + fil);
        });
        IAPDataManager.Inst.Refresh((res)=> {
            iapListBack = true;
            //ReportLoginHall();
        });
        ReportLoginHall();
        MessageHelper.AddListener<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, OnOverBuildMap);
        MessageHelper.AddListener<EnterGameModel>(MessageName.OverBuildMap, OnOverBuildMap);
        MessageHelper.AddListener<EnterGameModel, string>(MessageName.OverExitGame, OnOverExitGame);
        MessageHelper.AddListener(MessageName.BatchCreatePlayers, CloseGuestUgcLoading);
        MessageHelper.AddListener(MessageName.ReddotNotice, UpdateNewIcon);
        MessageHelper.AddListener(MessageName.FirstShapeOpenNotice, FirstShapeOpen);
        userName = GameObjectEx.FindComponentByName<Text>(transform, "UserName");
        chatReddot = GameObjectEx.FindComponentByName<Image>(transform, "ChatReddot");
        chatReddotText = GameObjectEx.FindComponentByName<Text>(transform, "ChatReddotText");

        AccountDataManager.Inst.AddUserInfoChangeListener(OnUserInfoChange);
        AccountDataManager.Inst.AddAvatarChangeListener(OnAvatarChange);
        AccountDataManager.Inst.AddIdleChangeListener(OnIdleChange);
        AccountDataManager.Inst.AddPetAvatarChangeListener(OnPetAvatarChange);
        AccountDataManager.Inst.AddHallCharacterChangeListener(OnHallCharacterChange);
        _ = MarketReviewManager.Inst;

        //var endTime = new DateTime(2025, 9, 1);
        //bool showAnniversaryBtn = TcpTimeSystem.Inst.ServerDataTime <= endTime;
        anniversaryCelebrationBtn.gameObject.SetActive(false);
        profileInfoBtn.onClick.AddListener(OnClickProfile);

        var dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        changeOutfitBtn.onClick.AddListener(OnClickEditAvatar);
        changePetOutfitBtn.onClick.AddListener(OnClickEditPetAvatar);

        avatarRedDot.SetRedDotNum(dataHandler.RedDot);
        petAvatarRedDot.SetRedDotNum(dataHandler.PetRedDot);
        dataHandler.AddDataChange(gameObject, (_) =>
        {
            avatarRedDot.SetRedDotNum(dataHandler.RedDot);
            petAvatarRedDot.SetRedDotNum(dataHandler.PetRedDot);
        });

        communityGameBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.CommunityGamesPanel);
            LoadEvent.ReportPopupStatus("ClickedMap", "GameHall");
        });

        Btn_AnimStudio.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel,CharacterStyle.Avatar);
            LoadEvent.ReportPopupStatus("ClickedAnimStudio", "GameHall");
        });

        Btn_Studios.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanelTakeAni(PanelId.GameHallStudiosPanel);
            LoadEvent.ReportPopupStatus("ClickedStudios", "GameHall");
        });
        anniversaryCelebrationBtn.onClick.AddListener(() =>
        {
            var endTime1 = new DateTime(2025, 9, 1);
            bool isShowAnniversaryBtn = TcpTimeSystem.Inst.ServerDataTime <= endTime1;
            if(isShowAnniversaryBtn == false)
            {
                TipPanel.ShowToast("周年庆活动已结束，感谢您的参与！");
                anniversaryCelebrationBtn.gameObject.SetActive(false);
                return;
            }
            UIManager.Inst.OpenPanel(PanelId.AnniversaryPanel);
            LoadEvent.ReportPopupStatus("ClickedAnniversary", "Anniversary");
        });
        seasonPassBtn.onClick.AddListener(() =>
        {
            var panel = UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel);
            LoadEvent.ReportPopupStatus("ClickedSeasonpass", "GameHall");
            panel.BackAction = () =>
            {
                if (this == null)
                {
                    return;
                }
                RefreshSeasonPassIfNeed();
            };
        });

        roomCodeBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel<EnterRoomCodePanel>(PanelId.EnterRoomCodePanel);
        });
        chatBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel<GameHallChatPanel>(PanelId.GameHallChatPanel);
        });
        mailboxBtn.onClick.AddListener(() =>
        {
            //ReturnToPortrait();
            UIManager.Inst.OpenPanel<MailboxPanel>(PanelId.MailboxPanel);
            LoadEvent.ReportPopupStatus("ClickedMailBox", "GameHall");
        });
        settingBtn.onClick.AddListener(() =>
        {
            var settingPanel = UIManager.Inst.OpenPanelTakeAni<SettingPanel>(PanelId.SettingPanel);
            settingPanel.PetAndNpcVisible = SetPetAndNpcVisible;
            settingPanel.RenderHallBuddyLocal = RenderHallAIBuddyLocal;
        });
        activityBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.ActivityCenterPanel);
            LoadEvent.ReportPopupStatus("Clickedctivity", "GameHall");
        });
        newbieTaskBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.BudNewbieTaskPanel);
        });
        newbieTaskV2Btn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.BudNewbieTaskV2Panel);
        });
        newbieTaskV3Btn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.NewBieSevenDayV3TaskPanel);
        });
        creatorBtn.onClick.AddListener(() =>
        {
            //UIManager.Inst.OpenPanel(PanelId.CreatorCenterPanel);
            UIManager.Inst.OpenPanel(PanelId.CreatorSeasonPanel);

        });
        changeIdleBtn.onClick.AddListener(() => {

            if (!HallCharacterManager.IsHidden) {
                var idleAnimPanel = UIManager.Inst.OpenPanel<LobbyNpcIdlePanel>(PanelId.LobbyNpcIdlePanel);
                idleAnimPanel.UpdateHallAnim = OnIdleChange;
            } else {
                var idleAnimPanel = UIManager.Inst.OpenPanel<LobbyCharacterIdlePanel>(PanelId.LobbyCharacterIdlePanel);
                idleAnimPanel.UpdateHallAnim = OnIdleChange;
            }

            // idleAnimPanel.UpdateHallAnim = OnIdleChange;


            //TODO: 区分NPC 还是宠物
            // var idleAnimPanel = UIManager.Inst.OpenPanel<LobbyCharacterIdlePanel>(PanelId.LobbyCharacterIdlePanel);
            // idleAnimPanel.UpdateHallAnim = OnIdleChange;
        });
        changeOcBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanelTakeAni(PanelId.OcChangePanel, UI.UIPanels.FittingRoom.OcChangeScene.Lobby);
        });

        Btn_LimitedPack.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.LimitedRechargeGiftPack);
        });

        Btn_SendGift.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.SendGiftPanel);
        });

        BtnAIBuddy.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AIBuddyListPanel);
        });

        npcStudioBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AINpcStudioMainPanel);
        });

        npcStoreBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AINpcStorePanel);
            LoadEvent.ReportPopupStatus("ClickedNPCStore", "GameHall");
        });

        Btn_AIYandere.onClick.AddListener(() =>
        {
            // UIManager.Inst.OpenPanel<AIYandereStartPanel>(PanelId.AIYandereStartPanel);
        });

        Btn_S9MainEntry.onClick.AddListener(() =>
        {
            //UIManager.Inst.OpenPanel(PanelId.AIHospitalMainEntryPanel, WindowId.RecommendWindow);
            GameEntrySystem.Inst.OpenMainPanel();
            LoadEvent.ReportPopupStatus("ClickedPlay", "GameHall");
        });

        BtnContent.onClick.AddListener(() =>
        {
            ContestEventManager.Inst.OpenContestPage();
        });

        BtnBuyPinkCoin.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanelTakeAni<GetMorePinkCoinPanel>(PanelId.GetMorePinkCoinPanel);
            LoadEvent.ReportPopupStatus("ClickedHotBuy", "GameHall");
        });
        newBieBreakIceBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.BreakIcePanel);
        });
        BtnFirstRecharge.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.FirstChargeOneYuan);
        });
        
        Btn_Season.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AIHospitalSeasonPanel);
            LoadEvent.ReportPopupStatus("ClickedS9Season", "GameHall");
        });
        aiCompanionBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AICompanionPanel);
        });
        // 通过反射读取底包 GameStart.boxBudUser 控制按钮显示
        var gameStartType = Type.GetType("GameStart, Assembly-CSharp");
        if (gameStartType != null)
        {
            var boxBudUserField = gameStartType.GetField("isBoxWhiteUser",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (boxBudUserField != null)
            {
                var isBoxBudUser = (bool)boxBudUserField.GetValue(null);
                bool openBox = isBoxBudUser && DeviceInfoManager.Inst.CheckVersion_1_0_19();
                openBox = openBox || DeviceInfoManager.Inst.CheckVersion_1_0_20_Update();
#if UNITY_EDITOR
                openBox = true;
#endif
                Debug.Log($"[isBoxWhiteUser] isBoxBudUser={isBoxBudUser}  CheckVersion={DeviceInfoManager.Inst.CheckVersion_1_0_19()} ,HotUpdateVersion={xasset.Assets.HotUpdateVersion},1_0_20_Update={DeviceInfoManager.Inst.CheckVersion_1_0_20_Update()}");
                aiCompanionBtn.gameObject.SetActive(openBox);
                if (openBox)
                {
                    CabinNetManager.Inst.Init();
                }
            }
            else
            {
                Debug.Log("[isBoxWhiteUser] 找不到 isBoxWhiteUser字段");
            }
        }
        else
        {
            Debug.Log("[isBoxWhiteUser] 找不到 GameStart类型");
        }
        IncubationCabinBLEWifiBtn.onClick.AddListener(() =>
        {
            var isActive = IncubationCabinBLEWifiBtn.transform.Find("soft").gameObject.activeInHierarchy;
            IncubationCabinBLEWifiBtn.transform.Find("soft").gameObject.SetActive(!isActive);


            var contentTrans = transform.Find("BaseLayout2D/Content");
            var go = new GameObject("softParent");
            var trans = go.AddComponent<RectTransform>();
            go.transform.SetParent(contentTrans);
            trans.localScale = Vector3.one;
            trans.sizeDelta = new(4000, 4000);
            trans.localPosition = Vector3.zero;
            go.AddComponent<Image>();
        });


        PlayerAnimationCtrl avatarAnimCtrl = null;
        CharacterData avatarInfo = userInfo.avatarInfo;
        if (avatarInfo != null)
        {
            AvatarDataManager.Inst.SelfCharacterData = avatarInfo;
            // 同一帧调用两次会有问题 待解决
            var wrap = AvatarController.Inst.CreateUIAvatarWithIKController(avatarInfo,AvatarParent);
            _characterWrap = wrap;
            avatarAnimCtrl = wrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarAnimCtrl.CheckAndOverrideSpecialAnim();
            avatarAnimCtrl?.ApplyUIPreviewIdleOverride(); // 大厅特殊皮肤 idle 用 preview（须在 CheckAndOverrideSpecialAnim 之后）
            idleBehaviour = wrap.Avatar.AddComponent<PlayerIdleBehaviour>();
            idleBehaviour.Init(avatarAnimCtrl);

            var ugcBehaivour = wrap.Avatar.AddComponent<UgcIdleBehaviour>();
            var ikController = wrap.Avatar.GetComponent<AnimIKController>();
            ugcBehaivour.Init(ikController);
        }

        var petInfo = AccountDataManager.Inst.PetInfo;
        PetData petData = petInfo?.avatarInfo;
        if (petData != null)
        {
            _petWrap = PetAvatarController.Inst.CreateUIAvatarWithIKController(petData,AvatarParent);
            var animationCtrl = _petWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            petIdleBehaviour = _petWrap.Avatar.AddComponent<PetPlayerIdleBehaviour>();
            petIdleBehaviour.Init(animationCtrl);

            var petUgcIdleBehaivour = _petWrap.Avatar.AddComponent<UgcIdleBehaviour>();
            var ikController = _petWrap.Avatar.GetComponent<AnimIKController>();
            petUgcIdleBehaivour.Init(ikController);
            petIdleBehaviour.avatarAnimCtr = avatarAnimCtrl;
            _petWrap.Avatar.SetActive(false);
        }


        var npcAvatarJson = HallCharacterManager.CurrentSkinAvatarJson();

        if (string.IsNullOrEmpty(npcAvatarJson)) {
            // 特殊逻辑，确保创建出来了NPC，后续只是做换装处理；
            npcAvatarJson = AccountDataManager.Inst.UserInfo.avatarJson;
        }

        if (!string.IsNullOrEmpty(npcAvatarJson)) {
            var npcData = CharacterData.DeserializeObject(npcAvatarJson);
            _npcWrap = AvatarController.Inst.CreateUIAvatarWithIKController(npcData, AvatarParent);
            var animationCtrl = _npcWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            npcIdleBehaviour = _npcWrap.Avatar.AddComponent<NpcPlayerIdleBehaviour>();
            npcIdleBehaviour.Init(animationCtrl);
            npcIdleBehaviour.avatarAnimCtr = avatarAnimCtrl;
            var npcUgcIdleBehaivour = _npcWrap.Avatar.AddComponent<UgcIdleBehaviour>();
            var ikController = _npcWrap.Avatar.GetComponent<AnimIKController>();
            npcUgcIdleBehaivour.Init(ikController);
            _npcWrap.Avatar.SetActive(false);
        }

        DragUtil.RotateTarget = AvatarParent.transform;
        ReddotManagerUtils.Inst.Init();
        OnPreHttpRequest();
        RefreshFriendView();

        SetUserInfo();

        FriendView = new GameHallFriendView().Bind(this.gameObject);

        EventCenterDataManager.Inst.AddTaskDataCallBack("NewbieState", (rsp) =>
        {
        
            newbieTaskBtn.gameObject.SetActive(EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieSevenDayTask));
            newbieTaskV2Btn.gameObject.SetActive(EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieSevenDayTaskV2));
            newbieTaskV3Btn.gameObject.SetActive(EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieSevenDayAccumulativeTask));

            List<string> tasks = new List<string>();
            tasks.Add(TASK_ID.NewbieDressUpTask.ToString());
            tasks.Add(TASK_ID.NewbieDressUpTask18.ToString());
            tasks.Add(TASK_ID.NewbieDressUpTask30.ToString());
            IAPDataManager.Inst.GetTaskList(tasks, (b, taskInfoResponse) =>
            {
                if (this == null || gameObject == null)
                {
                    return;
                }

                if (taskInfoResponse?.list == null || taskInfoResponse.list.Count <= 2) return;
                TaskInfoData tsTaskInfoData = taskInfoResponse.list[0];
                TaskInfoData tsTaskInfoData1 = taskInfoResponse.list[1];
                TaskInfoData tsTaskInfoData2 = taskInfoResponse.list[2];
                if (tsTaskInfoData?.eventList == null) return;
                var eventList = tsTaskInfoData.eventList;
                foreach(var even in eventList)
                {
                    if(even.eventStatus== (int)EventStatus.Claim)
                    {
                        newBieBreakIceRedDot.gameObject.SetActive(true);
                    }
                }
                if (tsTaskInfoData.taskStatus!=0)
                {
                    bool task1 = tsTaskInfoData.taskStatus == 1; //EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieDressUpTask);
                    bool task2 = tsTaskInfoData1.taskStatus == 1;//  EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieDressUpTask18);
                    bool task3 = tsTaskInfoData2.taskStatus == 1;//  EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieDressUpTask30);
                    Debug.Log($"taskInfoResponse task ={task1},{task2}，{task3}");
                    newBieBreakIceBtn.gameObject.SetActive(task1 || task2 || task3);
                }
                else
                {
                    newBieBreakIceBtn.gameObject.SetActive(PlayerPrefs.HasKey("FirstOpenBreakIceNew" + AccountDataManager.Inst.Uid));
                }
        });
            
            v2TaskView.SetActive(EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieSevenDayTaskV2));
            v3TaskView.SetActive(EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieSevenDayAccumulativeTask));
        });

        EventCenterDataManager.Inst.AddTaskDataCallBack("NewComerCommunityCoin", (taskListRsp) =>
        {
            SetPinkCoin(taskListRsp);
        });

        EventCenterDataManager.Inst.AddTaskDataCallBack("NewComerCommunityCoinV2", (taskListRsp) =>
        {
            SetPinkCoin(taskListRsp);
        });

        AddListeners();
        CloseLoadingSound();

        VipDataManager.Inst.UpdateVipStatus();
        DiscountCardManager.Inst.UpdateStatus();
        GlobalSettingManager.Inst.Init();
        //AIBuddyDataManager.Inst.RequestAIBuddyList();
        OpenLogReport();
        AIResDataManager.Inst.GetNetworkAIResData();
        ExternalLinkSkipManager.Inst.RegisterUniversalLinkListener(handleUniversalLinkSkip);

#if UNITY_ANDROID
    MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.getChannelId, GetChannelId);
    MobileInterface.Instance.SendMessage(MobileInterfaceDefine.getChannelId,
        "");
    MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.forceLogout, ForceLogout);
#endif


        if (GMBtn != null)
        {
            GMBtn.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        GMBtn.gameObject.SetActive(true);
        GMBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.GMPanel);
        });

#endif  
        
        if (PlayerPrefs.HasKey(key))
        {
            PopUpMobileQualityPanel();
        }
        MessageHelper.AddListener<TcpChatData>(MessageName.ChatMessage, ReceiveMessage);

        ActivitySkipSystem.Inst.RequestInfo(null);

        AnniversaryMgr.Inst.PreGetAnniversaryInfo();


    }

    private void ReportLoginHall()
    {
        if (/*iapListBack && blanceInfoBack && */hasReport == false)
        {
            hasReport = true;
            //var balanceInfo = AccountDataManager.Inst.BalanceInfo;
            //var rechargeBenefits = IAPDataManager.Inst.GetCumulativeRechargeData();
            //var userInfo = AccountDataManager.Inst.UserInfo;

            //Dictionary<string, object> superProperties = AnalyticsManager.Inst.GetSuperProperties();
            //superProperties["role_id"]               = userInfo.uid;
            //superProperties["role_name"]             = userInfo.nickname;
            //superProperties["diamond_amount"]        = balanceInfo.GetAccountCount(CurrencyType.Gem);
            //superProperties["bluecoin_amount"]       = balanceInfo.GetAccountCount(CurrencyType.Badge);
            //superProperties["pinkcoin_amount"]       = balanceInfo.GetAccountCount(CurrencyType.PinkCoin);
            //superProperties["coin_amount"]           = balanceInfo.GetAccountCount(CurrencyType.Coin);
            //superProperties["yoyocoin_amount"]       = balanceInfo.GetAccountCount(CurrencyType.YouYouCoin);
            //superProperties["purplecoin_amount"]     = balanceInfo.GetAccountCount(CurrencyType.PurpleDreamCoin);
            //superProperties["luckycoin_amount"]      = balanceInfo.GetAccountCount(CurrencyType.LuckyCoin);
            //superProperties["creatorcoin_amount"]    = balanceInfo.GetAccountCount(CurrencyType.GreenCoin);
            //superProperties["creator_energy_amount"] = balanceInfo.GetAccountCount(CurrencyType.EnergyCoin);
            //superProperties["passcoin_amount"]       = balanceInfo.GetAccountCount(CurrencyType.SeasonPassCoin);
            //superProperties["charge_amount"]  = rechargeBenefits?.totalRecharged ?? 0;
            //AnalyticsManager.Inst.SetSuperProperties(superProperties);

            //Dictionary<string, object> trackData = new Dictionary<string, object>(superProperties);
            AnalyticsManager.Inst.Track(AnalyticsEventName.LOGIN_GAMEHALL_Enter);
            //Debug.Log($"ReportLoginHall role_id={userInfo.uid} diamond={superProperties["diamond_amount"]} coin={superProperties["coin_amount"]} season_charge={superProperties["season_charge_amount"]}");
        }
    }

    /// <summary>
    /// 设置为横屏（常用）
    /// </summary>
    public void SetLandscape()
    {

        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;

    }

    /// <summary>
    /// 返回到之前的横屏方向
    /// </summary>
    public void ReturnToLandscape()
    {
        SetLandscape();

        // 如果需要重新布局UI
        StartCoroutine(RefreshLayout());
    }

    private IEnumerator RefreshLayout()
    {
        // 强制Canvas更新
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// 返回到之前的横屏方向
    /// </summary>
    public void ReturnToPortrait()
    {
#if UNITY_IOS
        // iOS上延迟设置，避免在UI操作过程中立即设置方向导致崩溃
        StartCoroutine(SetPortraitSafely());
#else
        SetPortrait();
#endif

        // 如果需要重新布局UI
        StartCoroutine(RefreshLayout());
    }




    /// <summary>
    /// 设置为竖屏
    /// </summary>
    public void SetPortrait()
    {
#if UNITY_IOS
        // iOS上最安全的处理方式：只设置autorotate，完全不设置orientation
        // 让系统根据autorotate设置自动旋转，避免直接设置orientation导致的崩溃
        // 由于Xcode已经设置了所有方向支持，系统会自动旋转到竖屏
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        
        // 不设置Screen.orientation，让系统根据autorotate自动处理
        // 如果需要强制旋转，可以通过协程延迟处理，但通常autorotate就足够了
#else
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
#endif
    }

#if UNITY_IOS
    /// <summary>
    /// iOS上安全地设置竖屏方向（带延迟和异常保护）
    /// </summary>
    private IEnumerator SetPortraitSafely()
    {
        // 先设置autorotate
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        
        // 等待多帧，确保UI操作完成
        yield return new WaitForEndOfFrame();
        yield return null;
        yield return new WaitForSeconds(0.1f);
        
        // 尝试设置orientation，使用try-catch保护
        try
        {
            // 只有在当前不是竖屏时才设置
            if (Screen.orientation != ScreenOrientation.Portrait && 
                Screen.orientation != ScreenOrientation.PortraitUpsideDown)
            {
                Screen.orientation = ScreenOrientation.Portrait;
            }
        }
        catch (System.Exception e)
        {
            // 如果设置失败，至少autorotate已经设置，系统会自动旋转
            Debug.LogWarning($"设置屏幕方向失败，将使用autorotate自动旋转: {e.Message}");
        }
    }
#endif
    /*
    private void OnEnable()
    {
        string keyFittingRoom = "FirstOpenFittingRoomPanel" + AccountDataManager.Inst.UserInfo.uid;
        if(PlayerPrefs.HasKey(keyFittingRoom) && PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            UIManager.Inst.OpenPanel(PanelId.BootPanel, 6);
            ChangBtAlpha(0, "BtnNewbieV2");
        }
    }*/
    private void GetDataByHttp(bool isClaimSuccess = false)
    {
        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string>
        {
            ActivityId.FirstChargeOneYuan.ToString()
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (content) =>
            {
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (activityResponse.list != null)
                {
                    OnGetActivityListSuccess(activityResponse.list, isClaimSuccess);
                }
            },
            (error) =>
            {
            });
    }

    private void OnGetActivityListSuccess(List<ActivityInfo> activityList, bool isClaimSuccess)
    {
        var activityInfo = activityList.Find(x => x.activityId == ActivityId.FirstChargeOneYuan.ToString());
        if (activityInfo == null)
        {
            return;
        }

    }

    private void RefreshFirstRechargeBtn()
    {
        GetDataByHttp();
    }

    private void OpenLogReport()
    {
        GameObject rep = GameObject.Find("Reporter");
        if (rep == null)
        {
            Debug.LogError("Reporter is not exist");
        }
        if (rep.TryGetComponent<Reporter>(out var report))
        {
            if (DeviceInfoManager.Inst.Environment != GameEnvironment.MASTER)
            {
                string playerId = AccountDataManager.Inst.Uid;
                List<string> whiteListReport = new List<string>()
                {
                    "1815352869529292800", "1815332352537423872", "1815301222400552960", "1815301324317945856",
                    "1815304631610535936", "1818630289367588864", "1839490166541856768"
                };
                if (whiteListReport.Contains(playerId))
                {
                    report.enabled = true;
                }
            }
        }
    }

    // 大厅展示载具：双人载具且有 AI 伙伴时，让伙伴(_npcWrap)坐乘客位；否则隐藏伙伴(维持现状)。
    private void ShowHallVehicle(VehicleInfo vehicleInfo)
    {
        bool hasBuddy = !HallCharacterManager.IsHidden && HallCharacterManager.HasCharacter;
        bool isDouble = vehicleInfo.vehicleType == (int)VehicleType.Double;
        bool buddyAsPassenger = hasBuddy && isDouble && _npcWrap != null;

        var passenger = buddyAsPassenger ? _npcWrap : null;
        AvatarAndVehicleAnimView vehicleAnimView = new(_characterWrap, vehicleInfo, AvatarParent, passenger);
        vehicleAnimView.StartVehicleAnim(true);

        if (buddyAsPassenger)
        {
            _npcWrap.Avatar.SetActive(true);
            var avatarJson = HallCharacterManager.CurrentSkinAvatarJson();
            if (!string.IsNullOrEmpty(avatarJson))
            {
                _npcWrap.RefreshAvatar(CharacterData.DeserializeObject(avatarJson));
            }
        }
        else if (_npcWrap != null)
        {
            _npcWrap.Avatar.SetActive(false);
        }

        if (_petWrap != null)
        {
            _petWrap.Avatar.SetActive(false);
        }
    }

    private void ResetAvatarParentPos()
    {
        var vehicleInfo = AccountDataManager.Inst.VehicleInfo;
        if (vehicleInfo != null && vehicleInfo.isHidden == 0)
        {
            ShowHallVehicle(vehicleInfo);
            return;
        }
        if(AvatarParent != null){
            AvatarParent.localPosition = originalAvatarParentPos;
        }
    }


    private void SetPinkCoin(TaskListRsp taskListRsp)
    {
        var taskInfoData = taskListRsp.list.Find(x => x.taskId == TASK_ID.NewbieCheckIn.ToString());
        bool isTaskEnable = EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieCheckIn);
        bool isAllComplete = EventCenterDataManager.Inst.CheckTaskIsAllComplete(taskInfoData);
        gameHallPinkCoin.gameObject.SetActive(taskInfoData != null && isTaskEnable && !isAllComplete);
        if (taskInfoData == null )
        {
            return;
        }
        gameHallPinkCoin.OnInitCreate(taskInfoData);
        MessageHelper.Broadcast(MessageName.NewComerCommunityCoin, taskListRsp);


        
        var taskInfoDataV2 = taskListRsp.list.Find(x => x.taskId == TASK_ID.NewbieCheckInV2.ToString());
        bool isTaskEnableV2 = EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieCheckInV2);
        bool isAllCompleteV2 = EventCenterDataManager.Inst.CheckTaskIsAllComplete(taskInfoData);
        newBieSevenDay.gameObject.SetActive(taskInfoDataV2 != null && isTaskEnableV2 && !isAllCompleteV2);
        if (taskInfoDataV2 == null)
        {
            return;
        }
        newBieSevenDay.OnInitCreate(taskInfoDataV2);
    }

    /// <summary>
    /// 本地渲染大厅 AI 伙伴模型（SettingPanel 选伙伴/换皮肤的即时预览），仅刷新模型，不做服务器同步。
    /// 后端伙伴皮肤持久化未就绪，先本地展示；持久化补齐后由对应同步接口接管。
    /// </summary>
    public void RenderHallAIBuddyLocal(string avatarJson)
    {
        if (_npcWrap == null || string.IsNullOrEmpty(avatarJson))
        {
            return;
        }
        _npcWrap.Avatar.SetActive(true);
        _npcWrap.RefreshAvatar(CharacterData.DeserializeObject(avatarJson));
    }

    private void SetPetAndNpcVisible(bool isHiddenPet, bool isHiddenNpc, bool isHiddenVehicle, AIBuddyInfo buddyInfo)
    {
        if (_petWrap != null)
        {
            _petWrap.Avatar.SetActive(!isHiddenPet);
        }

        if (_npcWrap != null) {
            _npcWrap.Avatar.SetActive(!isHiddenNpc);
        }

        if (!isHiddenVehicle) {
            _petWrap.Avatar.SetActive(false);
            _npcWrap.Avatar.SetActive(false);
        }

        var avatarIdleData = new IdleData() {mainIdle = BaseIdleBehaviour.IdleLeisureName, subIdle = new()};
        var petIdleData = new IdleData() {mainIdle = BaseIdleBehaviour.IdleDefaultName, subIdle = new()};
        var npcIdleData = new IdleData() {mainIdle = BaseIdleBehaviour.IdleLeisureName, subIdle = new()};
        AccountDataManager.Inst.UserInfo.idleData = avatarIdleData;
        AccountDataManager.Inst.PetInfo.idleData = petIdleData;
        AccountDataManager.Inst.AIBuddyInfo.idleData = npcIdleData;

        // 大厅摆位（AvatarAndPetAnimView.GetHallAnimatoionType）仍据 AIBuddyInfo.isHidden 判定，
        // 需本地同步该标志，否则展示伙伴时玩家与伙伴会重叠（不偏移）。服务器持久化另由 HallCharacterManager 负责。
        AccountDataManager.Inst.AIBuddyInfo.isHidden = isHiddenNpc ? 1 : 0;

        // 选伙伴/换皮肤由 SettingPanel 经 HallCharacterManager.SetHallCharacter 持久化；此处仅在展示/隐藏变化时回写可见性
        if (isHiddenNpc != HallCharacterManager.IsHidden) {
            HallCharacterManager.SetVisibility(isHiddenNpc);
        }

        if (isHiddenPet != (AccountDataManager.Inst.PetInfo.isHidden == 1)) {
            AccountDataManager.Inst.PetInfo.isHidden = isHiddenPet ? 1 : 0;
            AccountDataManager.Inst.SyncPetData(isHiddenPet);
        }

        if(AccountDataManager.Inst.VehicleInfo == null)
        {
            if(!isHiddenVehicle)
            {
                TipPanel.ShowToast("请到试衣间中设置你的载具");
            }
        }
        else
        {
            if(isHiddenVehicle)
            {
                _characterWrap.GetOutSelfVehicle(AccountDataManager.Inst.UserInfo.uid);
                PGCVehicleManager.Inst.RemoveUIPGCVehicle(PGCVehicleUIUsageType.Hall);
            }
            AccountDataManager.Inst.VehicleInfo.isHidden = isHiddenVehicle ? 1 : 0;
            AccountDataManager.Inst.SyncVehicleData(isHiddenVehicle);
        }

        OnIdleChange(AccountDataManager.Inst.UserInfo,AccountDataManager.Inst.PetInfo, AccountDataManager.Inst.AIBuddyInfo, AccountDataManager.Inst.VehicleInfo);
    }

    private void PopUpMobileQualityPanel()
    {
        if (GameDataManager.Inst.GetMobileQuality() == ModelClassificationType.Low && !SaveGameUtil.Inst.HasKeyByPlayerPrefs(SaveGameUtil.UIQuailityTipPopUp))
        {
            var qualityPanel = UIManager.Inst.OpenPanel<UIQualityTipPanel>(PanelId.UIQualityTipPanel);
            qualityPanel.CloseSelfAction =PopupPanelManager.Inst.RequestPopupDataOnColdStart;
            SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveGameUtil.UIQuailityTipPopUp, 1);
        }
        else
        {
            PopupPanelManager.Inst.RequestPopupDataOnColdStart();
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
            chatReddotText.text = totalUnReadCount > 99 ? "99+" : totalUnReadCount.ToString();
        }
    }

    private void CloseLoadingSound()
    {
        var loadSound = GameObject.Find("loadSound");
        if (loadSound != null)
        {
            GameObject.Destroy(loadSound);
        }
    }

    public void DickSpaceCheck()
    {
#if UNITY_ANDROID || UNITY_IOS
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.getAvailableFreeSpace, GetAvailableFreeSpace);
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.getAvailableFreeSpace, "");
#endif
    }

    private void GetAvailableFreeSpace(string message)
    {
        LoggerUtils.Log("磁盘空间剩余大小:" + message);
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.getAvailableFreeSpace);
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
        GetAvailableFreeSpaceResponse resp = JsonConvert.DeserializeObject<GetAvailableFreeSpaceResponse>(message);
#if UNITY_ANDROID
        if (resp.size > 0 && resp.size < 52428800)
        {
            UIManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel_Style2, new CommonSingleConfirmPanel_Style2Data()
            {
                CanClose = true,
                ConfirmString = "知道了",
                ContextString = "手机内存已不足50MB，请及时前往设置清理缓存。 这将有助于提升游戏的流畅度，确保您在游戏中的正常体验。",
                TopTitleString = "手机内存不足",
            });
        }
#endif

#if UNITY_IOS
        // IOS给的数是除以1000
        if (resp.size > 0 && resp.size < 50000000)
        {
            UIManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel_Style2, new CommonSingleConfirmPanel_Style2Data()
            {
                CanClose = true,
                ConfirmString = "知道了",
                ContextString = "手机内存已不足50MB，请及时前往设置清理缓存。 这将有助于提升游戏的流畅度，确保您在游戏中的正常体验。",
                TopTitleString = "手机内存不足",
            });
        }
#endif
    }

    private void GetChannelId(string message)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.getChannelId);
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
        GetChannelIdResponse getchannelIdResponse = JsonConvert.DeserializeObject<GetChannelIdResponse>(message);
        IAPDataManager.Inst.channelId = getchannelIdResponse.channelId;
    }

    private void SetUserInfo()
    {
        AccountUserInfo userInfo = AccountDataManager.Inst.UserInfo;
        userName.SetText(userInfo.nickname);
        var path = userInfo.portraitUrl;
        HeadViewWidget.InitHeadCycle(userInfo);
    }

    private void AddListeners()
    {
        rechargeBtn?.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.RechargePanel);
            LoadEvent.ReportPopupStatus("ClickedRecharge", "GameHall");
        });

        storeBtn?.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.StoreMallPanel);
            LoadEvent.ReportPopupStatus("ClickedGashapon", "GameHall");
        });

        MessageHelper.AddListener(MessageName.OnTcpNotifyRefreshFriendList, RefreshFriendView);
    }

    private void OnPreHttpRequest()
    {
        var httpRequests = Es.DataTables.GetNetHttpBusinessList();
        for (var i = 0; i < httpRequests.Count; i++)
        {
            var netData = httpRequests[i];
            if (netData.IsPreRequest == (int)PreRequestType.Pre)
            {
                NetworkProxy.SendHttpRequest(netData.HttpUrl, HttpMethod.GET, netData.ParamStr,
                    arg => OnNextHttpRequest(netData, arg), null, new NetCacheEvent(null));
            }
        }
    }

    private void OnNextHttpRequest(NetHttpBusiness data, string content)
    {
        string nextCmd = data.NextCmd;
        if (!string.IsNullOrEmpty(nextCmd))
        {
            var nextData = Es.DataTables.GetNetHttpBusiness(nextCmd);
            if (nextData.IsPreRequest == (int)PreRequestType.Next)
            {
                JObject rObject = JsonConvert.DeserializeObject<JObject>(content);
                JArray rArray = rObject[data.Arg0] as JArray;
                JObject firstObject = rArray.First() as JObject;
                JObject jObject = JsonConvert.DeserializeObject<JObject>(nextData.ParamStr);
                Dictionary<string, string> maps =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(data.Arg1);
                foreach (var keyValue in maps)
                {
                    jObject[keyValue.Value] = firstObject[keyValue.Key];
                }

                NetworkProxy.SendHttpRequest(nextData.HttpUrl, HttpMethod.GET, JsonConvert.SerializeObject(jObject),
                    null, null, new NetCacheEvent(null));
            }
        }
    }

    private void OnUserInfoChange(AccountUserInfo userInfo)
    {
        if (userInfo != null)
        {
            SetUserInfo();
            
            if (UIManager.Inst.TryFindPanel(WindowId.GuestWindow, PanelId.GameGuestPanel, out GameGuestPanel _))
            {
                var isHiddenPet = AccountDataManager.Inst.PetInfo == null ? true : AccountDataManager.Inst.PetInfo.isHidden == 1;
                var isHiddenNpc = HallCharacterManager.IsHidden;
                var isHiddenVehicle = AccountDataManager.Inst.VehicleInfo == null ? true : AccountDataManager.Inst.VehicleInfo.isHidden == 1;
                SetPetAndNpcVisible(isHiddenPet, isHiddenNpc, isHiddenVehicle, null);
                if (AvatarController.Inst.SelfController != null && AvatarController.Inst.SelfController.Motor != null)
                {
                    AvatarController.Inst.SelfWrap.GetOutSelfVehicle(AccountDataManager.Inst.UserInfo.uid);//避免在试衣间影响到游戏中人物状态,大厅等展示界面没有motor状态
                }
            }
        }
    }

    private void OnAvatarChange(AccountUserInfo userInfo)
    {
        if (userInfo != null && userInfo.avatarInfo != null)
        {
            _characterWrap?.RefreshAvatar(userInfo.avatarInfo);
            var avatarAnimCtrl = _characterWrap?.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarAnimCtrl?.CheckAndOverrideSpecialAnim();
            avatarAnimCtrl?.ApplyUIPreviewIdleOverride(); // 大厅特殊皮肤 idle 用 preview
        }
    }

    public void OnIdleChange(AccountUserInfo userInfo,AccountPetInfo petInfo, AIBuddyInfo buddyInfo, VehicleInfo vehicleInfo = null)
    {
        //当前的载具显示优先级最高，后续有冲突和策划确认
        if (vehicleInfo != null && vehicleInfo.isHidden == 0) {
            ShowHallVehicle(vehicleInfo);
            //_characterWrap.CustomAvatar.transform.localPosition = Vector3.zero;
            isSetBodyPos = false;
            //StartSetPosByBodyType(userInfo.avatarInfo);
            return;
        }

        #if UNITY_EDITOR
        if (DebugSetting.Inst != null && DebugSetting.Inst.isShowTempVehicle && TempVehicleDataSave.GetTempVehicleInfo() != null) {
            AvatarAndVehicleAnimView vehicleAnimView = new AvatarAndVehicleAnimView(_characterWrap, TempVehicleDataSave.GetTempVehicleInfo(), AvatarParent);
            vehicleAnimView.StartVehicleAnim(true);
            return;
        }
        #endif
        
        AvatarAndPetAnimView animView = new AvatarAndPetAnimView(_characterWrap,_petWrap, _npcWrap, idleBehaviour,petIdleBehaviour, npcIdleBehaviour, AvatarCamera, _avatarCameraDefaultLocalPos);
        var animType = animView.GetHallAnimatoionType(petInfo, buddyInfo);
        animView.OnAnimChange(true,animType,userInfo,petInfo, buddyInfo);
        var npcSkinJson = HallCharacterManager.CurrentSkinAvatarJson();
        if (!string.IsNullOrEmpty(npcSkinJson)) {
            _npcWrap.RefreshAvatar(CharacterData.DeserializeObject(npcSkinJson));
        }
        isSetBodyPos = false;
        StartSetPosByBodyType(userInfo.avatarInfo);
        ResetAvatarParentPos();
    }

    public void OnIdleChange(AccountUserInfo userInfo, AccountPetInfo petInfo) {
        if (AccountDataManager.Inst.VehicleInfo != null && AccountDataManager.Inst.VehicleInfo.isHidden == 0) {
            AvatarAndVehicleAnimView vehicleAnimView = new AvatarAndVehicleAnimView(_characterWrap, AccountDataManager.Inst.VehicleInfo, AvatarParent);
            vehicleAnimView.StartVehicleAnim(true);
            return;
        }

        #if UNITY_EDITOR
        if (DebugSetting.Inst != null && DebugSetting.Inst.isShowTempVehicle && TempVehicleDataSave.GetTempVehicleInfo() != null) {
            AvatarAndVehicleAnimView vehicleAnimView = new AvatarAndVehicleAnimView(_characterWrap, TempVehicleDataSave.GetTempVehicleInfo(), AvatarParent);
            vehicleAnimView.StartVehicleAnim(true);
            return;
        }
        #endif
        
        AvatarAndPetAnimView animView = new AvatarAndPetAnimView(_characterWrap,_petWrap, _npcWrap, idleBehaviour,petIdleBehaviour, npcIdleBehaviour,AvatarCamera, _avatarCameraDefaultLocalPos);
        var animType = animView.GetHallAnimatoionType(petInfo, null);
        animView.OnAnimChange(true,animType,userInfo,petInfo, null);
    }

    public void OnIdleChange(AccountUserInfo userInfo, AIBuddyInfo buddyInfo) {
        if (AccountDataManager.Inst.VehicleInfo != null && AccountDataManager.Inst.VehicleInfo.isHidden == 0) {
            AvatarAndVehicleAnimView vehicleAnimView = new AvatarAndVehicleAnimView(_characterWrap, AccountDataManager.Inst.VehicleInfo, AvatarParent);
            vehicleAnimView.StartVehicleAnim(true);
            return;
        }

        #if UNITY_EDITOR
        if (DebugSetting.Inst != null && DebugSetting.Inst.isShowTempVehicle && TempVehicleDataSave.GetTempVehicleInfo() != null) {
            AvatarAndVehicleAnimView vehicleAnimView = new AvatarAndVehicleAnimView(_characterWrap, TempVehicleDataSave.GetTempVehicleInfo(), AvatarParent);
            vehicleAnimView.StartVehicleAnim(true);
            return;
        }
        #endif

        AvatarAndPetAnimView animView = new AvatarAndPetAnimView(_characterWrap,_petWrap, _npcWrap, idleBehaviour,petIdleBehaviour, npcIdleBehaviour,AvatarCamera, _avatarCameraDefaultLocalPos);
        var animType = animView.GetHallAnimatoionType(null, buddyInfo);
        animView.OnAnimChange(true,animType,userInfo,null, buddyInfo);
        var npcSkinJson = HallCharacterManager.CurrentSkinAvatarJson();
        if (!string.IsNullOrEmpty(npcSkinJson)) {
            _npcWrap.RefreshAvatar(CharacterData.DeserializeObject(npcSkinJson));
        }
        isSetBodyPos = false;
        StartSetPosByBodyType(userInfo.avatarInfo);
    }



    private void OnPetAvatarChange(AccountPetInfo petInfo) {
        if (petInfo != null && petInfo.avatarInfo != null)
        {
            _petWrap?.RefreshAvatar(petInfo.avatarInfo);
        }
    }

    // 大厅伙伴角色变更（HallCharacterManager 持久化/服务端刷新后）：用当前皮肤刷新 npc 形象
    // 大厅伙伴角色变更（服务端加载 characterInfo / SettingPanel 设置后触发）。
    // 同步布局可见性标志（摆位据 AIBuddyInfo.isHidden），并全量刷新大厅展示（激活+摆位+换装）。
    // 重启时服务端不再返回 aibuddyInfo（为 null），仅靠 OnRefreshHallAnim 无法显示伙伴，必须经此用 characterInfo 驱动。
    private void OnHallCharacterChange(HallCharacterInfo info) {
        if (_npcWrap == null) return; // 角色尚未创建（极少数早到事件）；OnCreate 创建时会读取最新 characterInfo
        bool hidden = info == null || info.isHidden == 1;
        AccountDataManager.Inst.AIBuddyInfo.isHidden = hidden ? 1 : 0;
        OnIdleChange(AccountDataManager.Inst.UserInfo, AccountDataManager.Inst.PetInfo,
            AccountDataManager.Inst.AIBuddyInfo, AccountDataManager.Inst.VehicleInfo);
    }


    private void OnClickProfile()
    {
        AccountDataManager.Inst.RefreshUserInfo();
        ProfilePanel profilePanel = UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, AccountDataManager.Inst.Uid);
    }

    public override void OnShow(params object[] args)
    {
        if (bgCamera == null)
        {
            bgCamera = UIManager.Inst.CreateBgCanvas(this.gameObject);
            bgCamera.gameObject.DontDestroy();
        }

        GlobalCameraManager.Inst.InsertFirst(AvatarCamera);
        GlobalCameraManager.Inst.InsertFirst(bgCamera);

        AkSoundManager.Inst.CurrentHomePageAudio = SeasonPassDataManager.Inst.CurrentSeasonPassType == SeasonPassType.S14SeasonPass
            ? HomePageAudio.Bgm_Hall_S14
            : HomePageAudio.Bgm_Hall_S15;
        AkSoundManager.Inst.PlayBGSound();
        RefreshBannerIfNeed();
        
        if (!PlayerPrefs.HasKey(key))
        {
            UIManager.Inst.OpenPanel(PanelId.FittingRoomPanel);
        }
        ExternalLinkSkipManager.Inst.GetCurrentUniversalLinkInfo();
        if (SignInPanel.isNewPlayer)
        {
            LoadEvent.ReportPopupStatus("1", "load_done");
        }
    }

    public override void OnHidden()
    {
        // var uiCamera =  GameCameraUtils.Inst.GetUICamera();
        // var uiCameraData = uiCamera.GetUniversalAdditionalCameraData();
        // uiCameraData.renderType = CameraRenderType.Base;
        if (bgCamera != null)
        {
            // var cameraData = bgCamera.GetUniversalAdditionalCameraData();
            // cameraData.cameraStack.Remove(uiCamera);
            // cameraData.cameraStack.Remove(AvatarCamera);

            GlobalCameraManager.Inst.Remove(bgCamera);
        }

    }

    private void CloseGuestUgcLoading()
    {
        if (UIManager.Inst.FindPanel(WindowId.CommonWindow, PanelId.UgcLoadingPanel))
        {
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            var panel = UIManager.Inst.FindPanel<GameGuestPanel>(PanelId.GameGuestPanel);
            panel.PlaySelfTitle();
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, false);
        }
    }

    private void OnOverBuildMap(EnterGameModel enterGameModel)
    {
        OnOverBuildMap(enterGameModel, new UgcBaseInfo());
    }

    private void OnOverBuildMap(EnterGameModel enterGameModel, UgcBaseInfo info)
    {
        if (bgCamera)
        {
            bgCamera.gameObject.SetActive(false);
            GlobalCameraManager.Inst.Remove(AvatarCamera);
            GlobalCameraManager.Inst.Remove(bgCamera);
        }

        if (enterGameModel == EnterGameModel.CreateEmptyScene || enterGameModel == EnterGameModel.ContinueEditScene)
        {
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            UIManager.Inst.OpenPanel(PanelId.GameEditModePanel);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GameEditModeWindow, false);
        }
        else if (enterGameModel == EnterGameModel.GuestScene)
        {
            TimerManager.Inst.RunOnce("closeUgcLoading", 3, CloseGuestUgcLoading);
            UIManager.Inst.OpenPanel(PanelId.GameGuestPanel);
            UIManager.Inst.OpenPanel(PanelId.UIOperationOnWorldPanel);
            UIManager.Inst.SwitchWindow(WindowId.CommonWindow);
        }
        else if (enterGameModel is EnterGameModel.PublishTest or EnterGameModel.UpdatePublishTest)
        {
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            UIManager.Inst.OpenPanel(PanelId.GameTestPublishPanel);
            UIManager.Inst.OpenPanel(PanelId.UIOperationOnWorldPanel);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, false);
        }
        else if (enterGameModel == EnterGameModel.UgcPropEmpty || enterGameModel == EnterGameModel.UgcPropContinueEdit)
        {
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            UIManager.Inst.OpenPanel(PanelId.UGCItemEditPanel);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, false);
        }
        else if ((enterGameModel == EnterGameModel.UgcSkinEmpty || enterGameModel == EnterGameModel.UgcSkinContinueEdit) && info is SkinInfo { isProp: true })
        {
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            var curInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
            switch ((SkinType)curInfo.skinType)
            {
                case SkinType.Pet:
                    UIManager.Inst.OpenPanel(PanelId.UGCPetEditPanel);
                    break;

                default:
                case SkinType.Avatar:
                    UIManager.Inst.OpenPanel(PanelId.UGCItemEditPanel);
                    break;
            }

            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, false);

        }
        else if ((enterGameModel == EnterGameModel.UgcMusicalInstrumentEmpty || enterGameModel == EnterGameModel.UgcMusicalInstrumentContinueEdit) && info is SkinInfo { isProp: true })
        {
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            UIManager.Inst.ClosePanel(PanelId.CreateMusicalInstrumentPanel);
            UIManager.Inst.OpenPanel(PanelId.UgcMusicalInstrumentEditPanel);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, false);
        }
        else if (enterGameModel == EnterGameModel.UgcMusicScoreEmpty || enterGameModel == EnterGameModel.UgcMusicScoreContinueEdit)
        {
            // UIManager.Inst.OpenPanel(PanelId.ug);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, false);
        }
        else if (enterGameModel == EnterGameModel.UgcAnimEmpty || enterGameModel == EnterGameModel.UgcAnimContinueEdit)
        {
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            UIManager.Inst.CloseCommonPanel(PanelId.BlackPanel);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, false);
        }
        else if(enterGameModel == EnterGameModel.AnimPoseEmpty || enterGameModel == EnterGameModel.AnimPoseContinueEdit)
        {
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            UIManager.Inst.CloseCommonPanel(PanelId.BlackPanel);
            UIManager.Inst.OpenPanel(PanelId.AnimPoseEditPanel);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.AnimWindow, false);
        }
        else if (enterGameModel == EnterGameModel.AIYandere)
        {
            var blackPanel = UIManager.Inst.FindPanel(WindowId.CommonWindow,PanelId.BlackPanel);
            if (blackPanel)
            {
                blackPanel.CloseSelf();
            }
            UIManager.Inst.ClosePanel(PanelId.BlackPanel);
            UIManager.Inst.OpenPanel(PanelId.AIGameGuestPanel);
            UIManager.Inst.OpenPanel(PanelId.UIOperationOnWorldPanel);
            UIManager.Inst.SwitchWindow(WindowId.CommonWindow);
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, false);
        }
        else if (enterGameModel == EnterGameModel.AIHospital)
        {
            var blackPanel = UIManager.Inst.FindPanel(WindowId.CommonWindow,PanelId.BlackPanel);
            if (blackPanel)
            {
                blackPanel.CloseSelf();
            }
            UIManager.Inst.ClosePanel(PanelId.BlackPanel);
            UIManager.Inst.OpenPanel(PanelId.AIHospitalGuestPanel);
            UIManager.Inst.OpenPanel(PanelId.UIOperationOnWorldPanel);
            UIManager.Inst.SwitchWindow(WindowId.CommonWindow);
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, false);
        }
        else if (enterGameModel == EnterGameModel.AIPark)
        {
            var blackPanel = UIManager.Inst.FindPanel(WindowId.CommonWindow, PanelId.BlackPanel);
            if (blackPanel)
            {
                blackPanel.CloseSelf();
            }
            UIManager.Inst.ClosePanel(PanelId.BlackPanel);
            UIManager.Inst.OpenPanel(PanelId.AIParkGuestPanel);
            UIManager.Inst.OpenPanel(PanelId.UIOperationOnWorldPanel);
            UIManager.Inst.SwitchWindow(WindowId.CommonWindow);
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, false);
        }
        else if(enterGameModel == EnterGameModel.UgcVehicleEmpty || enterGameModel == EnterGameModel.UgcVehicleEmptyContinueEdit)
        {
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            UIManager.Inst.ClosePanel(PanelId.CreateVehiclePanel);
            UIManager.Inst.OpenPanel(PanelId.UGCVehicleEditPanel);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, false);
        }
        else if (enterGameModel == EnterGameModel.UgcVehicleTryPlay)
        {
            UIManager.Inst.CloseCommonPanel(PanelId.UgcLoadingPanel);
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, false);

            GameController.ChangeMode(GameMode.Play, () => {
                VehicleTryPlayInfo vehicleTryPlayInfo = new VehicleTryPlayInfo()
                {
                    vehicleInfo = info as VehicleInfo,
                    isPlay = false
                };
                UIManager.Inst.OpenPanel(PanelId.VehiclePlayPanel, vehicleTryPlayInfo);
            });
        }
        AkSoundManager.Inst.StopBGSound();
    }


    /// <summary>
    /// 退出Game场景
    /// </summary>
    /// <param name="enterGameModel"></param>
    /// <param name="err"></param>
    private void OnOverExitGame(EnterGameModel enterGameModel, string err)
    {
        if (!string.IsNullOrEmpty(err))
        {
            TipPanel.ShowToast(err);
        }

        if (UIManager.Inst.TryFindPanel(WindowId.CommonWindow, PanelId.UgcLoadingPanel, out var panel))
        {
            UIManager.Inst.ClosePanel(panel);
        }

        if (bgCamera)
        {
            bgCamera.gameObject.SetActive(true);
            GlobalCameraManager.Inst.InsertFirst(AvatarCamera);
            GlobalCameraManager.Inst.InsertFirst(bgCamera);
        }

        AkSoundManager.Inst.CurrentHomePageAudio = SeasonPassDataManager.Inst.CurrentSeasonPassType == SeasonPassType.S14SeasonPass
            ? HomePageAudio.Bgm_Hall_S14
            : HomePageAudio.Bgm_Hall_S15;
        AkSoundManager.Inst.PlayBGSound();
        isSetBodyPos = false;
        StartSetPosByBodyType(AccountDataManager.Inst.UserInfo.avatarInfo);
        ResetAvatarParentPos();
    }

    protected override void OnEnable()
    {
        UpdateNewIcon();
        base.OnEnable();
    }

    protected override void OnDestroy()
    {
        AccountDataManager.Inst.RemoveUserInfoChangeListener(OnUserInfoChange);
        AccountDataManager.Inst.RemoveAvatarChangeListener(OnAvatarChange);
        AccountDataManager.Inst.RemoveIdleChangeListener(OnIdleChange);
        AccountDataManager.Inst.RemovePetAvatarChangeListener(OnPetAvatarChange);
        MessageHelper.RemoveListener(MessageName.BatchCreatePlayers, CloseGuestUgcLoading);
        MessageHelper.RemoveListener<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, OnOverBuildMap);
        MessageHelper.RemoveListener<EnterGameModel>(MessageName.OverBuildMap, OnOverBuildMap);
        MessageHelper.RemoveListener<EnterGameModel, string>(MessageName.OverExitGame, OnOverExitGame);
        MessageHelper.RemoveListener(MessageName.OnTcpNotifyRefreshFriendList, RefreshFriendView);
        MessageHelper.RemoveListener(MessageName.ReddotNotice, UpdateNewIcon);
        MessageHelper.RemoveListener(MessageName.FirstShapeOpenNotice, FirstShapeOpen);

        if (bgCamera != null && bgCamera.gameObject != null)
        {
            Destroy(bgCamera.gameObject);
            bgCamera = null;
        }
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }
        MessageHelper.RemoveListener<TcpChatData>(MessageName.ChatMessage, ReceiveMessage);
        EventCenterDataManager.Inst.RemoveTaskDataCallBack("NewComerCommunityCoin");
        ResetAvatarParentPos();
    }

    public override void OnWindowBeCovered(bool isCover)
    {
        if (isCover)
        {
            if (!this || !this.transform) return;
            DealBaseLayout2D(false);
            DealBaseLayout3D(false);
            isSetBodyPos = false;
        }
    }

    public override void OnWindowShow()
    {
        if (!this || !this.transform) return;
        DealBaseLayout2D(true);
        DealBaseLayout3D(true);
    }

    public override void OnWindowBeFocused()
    {
        base.OnWindowBeFocused();
        LoggerUtils.LogFormat("[GameHall] page OnWindowBeFocused");
        CharacterData avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (avatarInfo == null)
            avatarInfo = AvatarDataManager.Inst.SelfCharacterData;

        //_characterWrap?.RefreshAvatar(avatarInfo);
        var avatarAnimCtrl = _characterWrap?.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();

        avatarAnimCtrl?.CheckAndOverrideSpecialAnim();
        avatarAnimCtrl?.ApplyUIPreviewIdleOverride(); // 大厅特殊皮肤 idle 用 preview
        ReddotManagerUtils.Inst.RefreshRedDot();
        LimitTimePropManager.Inst.RefashLimitTimeProps();
        AIBuddyDataManager.Inst.RequestAIBuddyList();
        RefreshSeasonPassIfNeed();
        RefreshBannerIfNeed();
        RefreshFirstRechargeBtn();
        if(AccountDataManager.Inst.VehicleInfo != null && AccountDataManager.Inst.VehicleInfo.isHidden == 0){
            ResetAvatarParentPos();
            return;
        }
        StartSetPosByBodyType(avatarInfo);
        ResetAvatarParentPos();
    }

    public override void OnWindowPop()
    {
    }

    private void OnClickEditAvatar()
    {
        FittingRoomPanel panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        panel.OnCloseAction = (data) =>
        {
            StartSetPosByBodyType(data);
            if(AccountDataManager.Inst.VehicleInfo != null){
                OnIdleChange(AccountDataManager.Inst.UserInfo, AccountDataManager.Inst.PetInfo, AccountDataManager.Inst.AIBuddyInfo, AccountDataManager.Inst.VehicleInfo);
            }
       };
        LoadEvent.ReportPopupStatus("ClickedEditImage", "GameHall");

    }

    private void OnClickEditPetAvatar()
    {
        UIManager.Inst.OpenPanel(PanelId.FittingRoomPanel, true);
    }

    private void StartSetPosByBodyType(CharacterData userAvatarData){
        if(isSetBodyPos) return;
        SetPosByBodyType(_characterWrap,(CustomBodyTypeController.BodyType)userAvatarData.bodyType);
        var npcSkinJson = HallCharacterManager.CurrentSkinAvatarJson();
        if (!string.IsNullOrEmpty(npcSkinJson))
        {
            var npcData = CharacterData.DeserializeObject(npcSkinJson);
            SetPosByBodyType(_npcWrap,(CustomBodyTypeController.BodyType)npcData.bodyType);
        }
        isSetBodyPos  = true;
    }

    private void SetPosByBodyType(CharacterWrap wrap, CustomBodyTypeController.BodyType bodyType)
    {
        if(wrap == null) return;
        if(wrap.Avatar == null) return;


        if(AccountDataManager.Inst.VehicleInfo != null 
        && AccountDataManager.Inst.VehicleInfo.isHidden == 0)
        {
            if(!int.TryParse(AccountDataManager.Inst.VehicleInfo.id, out int pgcId))
            {
                ResetAvatarParentPos();
                // 基于初始位置做绝对偏移，避免来回进出试衣间时 += 不断累加导致角色/镜头越来越远
                AvatarParent.localPosition = originalAvatarParentPos + new Vector3(0, 12, 285);
            }
        }else{
            ResetAvatarParentPos();
        }

        switch(bodyType)
        {
            case CustomBodyTypeController.BodyType.Type1:
            case CustomBodyTypeController.BodyType.Type2:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.6f, 0);
                break;
            case CustomBodyTypeController.BodyType.Type3:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.43f, 0);
                break;
            case CustomBodyTypeController.BodyType.Type4:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.26f, 0);
                break;
            case CustomBodyTypeController.BodyType.Type5:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.49f, 0);
                break;
            case CustomBodyTypeController.BodyType.Type6:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.57f, 0);
                break;
            default:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.5f, 0);
                break;
        }
    }

    /// <summary>
    /// 更新New图标
    /// </summary>
    private void UpdateNewIcon()
    {   
        int applyFriendNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.ApplyingFriend);
        if (FriendView != null)
        {
            FriendView.SetRedDotNum(applyFriendNum);
        }
        int mailBoxNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.Mail)
                       + ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.InteractNotification)
                       + ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.AuditNotification)
                       + ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.GiftNotification);
        if (mailboxRedDot != null)
        {
            mailboxRedDot.SetRedDotNum(mailBoxNum);
        }
        //int activityNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.ActivityCenter);
        int activityNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.ActivityCenter) + ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.TreePlantingDayTask);

        if (activityCenterRedDot != null)
        {
            activityCenterRedDot.SetRedDotNum(activityNum);
        }

        int gashaponNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.Gashapon);

        if (gashaponRedDot != null)
        {
            gashaponRedDot.SetRedDotNum(gashaponNum);
        }
        // int taskNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotManagerUtils.ReddotType.Task);
        // if (taskRedDot != null)
        // {
        //     taskRedDot.SetRedDotNum(taskNum);
        // }
        int limitedPackRedDotCount = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.LimitPackage);
        Reddot_LimitedPack.SetActive(limitedPackRedDotCount != 0);

        int creatorNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.CreatorCenter);
        if (creatorRedDot != null)
        {
            creatorRedDot.SetRedDotNum(creatorNum);
        }

        if (gameHallPinkCoin != null)
        {
            gameHallPinkCoin.SetRedDot();
        }

        if (newbieRedDot != null)
        {
            int newbieTaskCount =  ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.NewbieTask);
            newbieRedDot.SetRedDotNum(newbieTaskCount);
        }
        if (newbieV2RedDot != null)
        {
            int newbieTaskCount = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.NewbieTask);
            newbieV2RedDot.SetRedDotNum(newbieTaskCount);
        }
        if (newbieV3RedDot != null)
        {
            int newbieTaskCount = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.NewbieTask);
            newbieV3RedDot.SetRedDotNum(newbieTaskCount);
        }
        if (vipRewardIcon != null)
        {
            int chargeNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.Charge);
            //vipRewardIcon.gameObject.SetActive(chargeNum > 0 || AnniversaryMonthCardMgr.Inst.IsEntryRedDot(""));
            vipRewardIcon.gameObject.SetActive(chargeNum > 0 || AnniversaryMonthCardMgr.Inst.IsEntryRedDot("") || SeasonCumulativeSystem.Inst.RedDot(""));

        }

        if (chatReddot != null)
        {
            int chatNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.Chat);
            chatReddot.gameObject.SetActive(chatNum > 0);
            chatReddotText.text = chatNum > 99 ? "99+" : chatNum.ToString();
        }

        if (!avatarRedDot.gameObject.activeSelf)//体型红点数据存在本地，特殊处理一下
        {
            avatarRedDot.SetRedDotNum(!PlayerPrefs.HasKey(ShapeDataMgr.Inst.shapeKey) ? 1 : 0);
        }
    }

    private void FirstShapeOpen()
    {
        var dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        avatarRedDot.SetRedDotNum(dataHandler.RedDot);
    }

    private void RefreshSeasonPassIfNeed()
    {
        int CurDay = 0;
        bool isCanClaim = false;
        var heightObj = GameObjectEx.FindChildByName(seasonPassBtn.transform, "Highlight").gameObject;
        heightObj.SetActive(false);
        var numText = GameObjectEx.FindChildByName(heightObj, "NUM").GetComponent<Text>();
        numText.text = "";
        
        SeasonPassDataManager.Inst.GetSeasonPassLists(resultAction: (b, rsp) =>
        {
            if (this == null)
            {
                return;
            }

            if (b && rsp != null)
            {
                bool isHideSeasonPass = rsp.seasonLobbyStatus == SeasonLobbyStatus.Offline || rsp.seasonLobbyStatus == SeasonLobbyStatus.ErrLobbyStatus;
                //seasonPassBtn.gameObject.SetActive(!isHideSeasonPass);
                if (!isCanClaim)
                {
                    isCanClaim = rsp.seasonLobbyStatus == SeasonLobbyStatus.Claimable;
                    heightObj.SetActive(isCanClaim);
                    if (isCanClaim)
                    {
                        CurDay = SeasonPassDataManager.Inst.GetCurDay(rsp.rewardList);
                        numText.text = CurDay.ToString();
                    }
                }
            }
            else
            {
                //seasonPassBtn.gameObject.SetActive(false);
            }
        });
    }

    private void RefreshBannerIfNeed()
    {
        ContestDataManager.Inst.RefreshLobbyInfo((result, res) =>
        {
            if (this == null)
            {
                return;
            }
            if (res == null)
            {
                return;
            }

            if(res.showSeasonLuckyPack == 1)
            {
                ShowSeasonReward();
            }
            if (contestBanner == null)
            {
                Debug.Log("contestBanner is Null");
                return;
            }
            var list = res?.contestList;
            if (list == null)
            {
                Debug.Log("list is Null");
                return;
            }

            bool isShow = result && list.Count > 0;
            contestBanner.gameObject.SetActive(isShow);
            if (isShow)
            {
                contestBanner.SetBanner(list);
            }
        });
    }
    private void ShowSeasonReward()
    {
        var rewardItemDatas = new List<CommonRewardItemData>();
        List<RewardItemData> rewards = new List<RewardItemData>();
        rewards.Add(new RewardItemData() { pgcId = (int)BUDRewardType.RewardYouYouCoin, rewardNum = 12 });
        rewards.Add(new RewardItemData() { pgcId = (int)BUDRewardType.RewardLuckyCoin, rewardNum = 12 });
        rewards.Add(new RewardItemData() { pgcId = (int)BUDRewardType.RewardPurpleDreamCoin, rewardNum = 6 });
        foreach (var rewardData in rewards)
        {
            var itemData = new CommonRewardItemData()
            {
                IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)rewardData.pgcId, gameObject),
                RewardAmount = rewardData.rewardNum,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardData.pgcId)
            };
            rewardItemDatas.Add(itemData);
        }
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardItemDatas);
    }
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
#if UNITY_IPHONE || UNITY_IOS
            //用于appicon显示红点
            JObject obj = new JObject()
            {
                ["number"] = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.Mail)
                             + ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.InteractNotification)
                             + ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.AuditNotification)
                             + ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.ApplyingFriend)
                             + ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.Chat)
            };
            MobileInterface.Instance.RefreshIconBadgeNumber(JsonConvert.SerializeObject(obj));
#endif
        }
        else
        {
            ReddotManagerUtils.Inst.RefreshRedDot();
            if (IsInGameHall() && !BootPanel.isPlaying) //新手引导界面打开时，不拍脸弹出
            {
                PopupPanelManager.Inst.RequestPopupDataOnWarmStart();
            }
        }
    }

    public bool IsInGameHall()
    {
        var curWin = UIManager.Inst.GetCurWindow(); //只有当前界面是大厅时，才拍脸
        if (curWin == null)
            return false;
        return curWin == BelongWindow && BelongWindow != null && BelongWindow.panels != null && BelongWindow.panels.Count == 1;
    }

    public void RefreshFriendView()
    {
        if (FriendView != null)
        {
            FriendView.Refresh();
        }
    }

    private void ForceLogout(string message)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.forceLogout);
        GameInstanceManager.Release();
        AccountDataManager.Inst.DeleteCache();
        UIManager.Inst.ClosePanel(PanelId.GameHallPanel);
        UIManager.Inst.OpenPanel(PanelId.SignInPanel);
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.logout,
            "");
    }

    private void DestroyGlobalAudioListener()
    {
        var audioListnerTrans = GameObject.Find("AudioListener");
        if (audioListnerTrans != null && audioListnerTrans.gameObject != null)
        {
            try
            {
                GameObject.Destroy(audioListnerTrans.gameObject);
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("DestroyGlobalAudioListener = " + e.StackTrace);
            }
        }
    }

    public void PlayChangeOcAni()
    {
        var userInfo = AccountDataManager.Inst.UserInfo;
        if (userInfo != null && userInfo.idleData != null)
        {
            if ((AnimResType)userInfo.idleData.animResType == AnimResType.PGC)
            {
                var emoData = Es.DataTables.GetEmoUIConfig(userInfo.idleData.mainIdle);
                if (emoData != null)
                {
                    var emoAniType = (EmoteType) emoData.emoAniType;
                    if (emoAniType == EmoteType.SingleOnce || emoAniType == EmoteType.SingleLoop)
                    {
                        idleBehaviour.OnlyPlayChangeOcAni(idleBehaviour.PlayMain);
                        return;
                    }
                }
                idleBehaviour.ChangeOcAni();
            }
            else
            {
                TemporarySwitchAnimRes(idleBehaviour.animationCtrl,false);
            }
        }

        // var emoteId = AccountDataManager.Inst.PetInfo?.idleData?.mainIdle;
        // if(!string.IsNullOrEmpty(emoteId) && petIdleBehaviour != null)
        // {
        //     var emoData = Es.DataTables.GetEmoUIConfig(emoteId);
        //     if (emoData != null)
        //     {
        //         var emoAniType = (EmoteType) emoData.emoAniType;
        //         if (emoAniType == EmoteType.PetWithPlayer || emoAniType == EmoteType.PetWithPlayerLoop)
        //         {
        //             idleBehaviour.OnlyPlayChangeOcAni(petIdleBehaviour.PlayMain);
        //             return;
        //         }
        //     }
        // }
        // idleBehaviour.ChangeOcAni();
    }

    private void TemporarySwitchAnimRes(PlayerAnimationCtrl animCtrl,bool isPet)
    {
        var ugcIdleBehaviour = idleBehaviour.GetComponent<UgcIdleBehaviour>();
        ugcIdleBehaviour.enabled = false;
        var playerIkController = idleBehaviour.GetComponent<AnimIKController>();
        playerIkController.ChangeAnimResType(AnimResType.PGC);

        var petUgcIdleBehaviour = this.petIdleBehaviour.GetComponent<UgcIdleBehaviour>();
        petUgcIdleBehaviour.enabled = false;
        var petIkController = petIdleBehaviour.GetComponent<AnimIKController>();
        petIkController.ChangeAnimResType(AnimResType.PGC);

        void RestoreState()
        {
            playerIkController.ChangeAnimResType(AnimResType.UGC);
            petIkController.ChangeAnimResType(AnimResType.UGC);
            ugcIdleBehaviour.enabled = true;
            petUgcIdleBehaviour.enabled = true;
            animCtrl.ResetEmoteForUICharacter();
        }

        if (isPet)
        {
            var petAnimCtrl = animCtrl as PetAnimationCtrl;
            petAnimCtrl.PlayChangeClothAni(null, true);
            TimerManager.Inst.RunOnce("RestoreState", 2.5f, RestoreState);
        }
        else
        {
            animCtrl.PlayerChangeClothesForUICharacer(null, true);
            TimerManager.Inst.RunOnce("RestoreState", 2.5f, RestoreState);
        }
    }

    public void PlayPetChangeOcAni()
    {
        var petInfo = AccountDataManager.Inst.PetInfo;
        if (petInfo != null && petInfo.idleData != null)
        {
            if ((AnimResType) petInfo.idleData.animResType == AnimResType.PGC)
            {
                petIdleBehaviour.ChangeOcAni();
            }
            else
            {
                TemporarySwitchAnimRes(petIdleBehaviour.animationCtrl,true);
            }
        }

    }

    private void AdjustHalloweenBg()
    {
        if (bgCamera == null)
        {
            return;
        }
        var bgImage = GameObjectEx.FindChildByName(bgCamera.transform, "Image")?.GetComponent<RawImage>();
        if (bgImage == null)
        {
            return;
        }
        var ImgPath = "Assets/Loadable/UI/UIPanel/GameHall/Texture/BG_Halloween.png";
        var texture = XAssetLoaderMgr.Inst.LoadResource<Texture>(ImgPath, gameObject);
        if (texture != null)
        {
            bgImage.texture = texture;
        }
    }

    private void handleUniversalLinkSkip(ExternalLinkSkipManager.UniversalLinkType type)
    {
        switch (type)
        {
            case ExternalLinkSkipManager.UniversalLinkType.SeasonBlindBox:
                UIManager.Inst.SwapPanel(PanelId.StoreMallPanel);
                break;
            case ExternalLinkSkipManager.UniversalLinkType.YandereGame:
                UIManager.Inst.SwapPanel(PanelId.AIYandereStartPanel);
                break;
        }
    }

    //提供外部调用的表现方法
    public void ChangBtAlpha(float targetAlpha,string OnlyName)
    {
        _isTopBtnClose = targetAlpha == 0;
        foreach (Transform child in topLine)
        {
            if (child.name == OnlyName)
            {
                continue;
            }

            CanvasGroup canvasGroup = child.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = child.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.DOFade(targetAlpha, 0.5f).SetEase(Ease.OutQuad);
        }
        if(targetAlpha == 1)
        {
            newBieBreakIceBtn.gameObject.SetActive(true);
            UIManager.Inst.OpenPanel(PanelId.BreakIcePanel);
            CanvasGroup canvasGroup = newBieBreakIceBtn.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = newBieBreakIceBtn.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.DOFade(0, 0.01f).SetEase(Ease.OutQuad);
            canvasGroup.DOFade(1, 0.5f).SetEase(Ease.OutQuad);
            PlayerPrefs.SetInt("FirstOpenBreakIceNew" + AccountDataManager.Inst.Uid, 1);
            PlayerPrefs.Save();
        }
        
    }

    [Button("测试ai币")]
    void testAI()
    {
        AccountDataManager.Inst.BalanceInfo.AiCredit ??=new();
        AccountDataManager.Inst.BalanceInfo.AiCredit.permanentAmount=0;
        AccountDataManager.Inst.BalanceInfo.AiCredit.expiringAmount=0;
        AccountDataManager.Inst.BalanceInfo.AiCredit.dailyAmount=0;
        AccountDataManager.Inst.BalanceInfo.AiCredit.cloneAmount=2;
            MessageHelper.Broadcast(MessageName.OnAICreditChange);
        
    }
}
