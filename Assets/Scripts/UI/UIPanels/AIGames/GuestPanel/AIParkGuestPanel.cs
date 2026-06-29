using AIGame.Base;
using Basic.Utils;
using Game.AIResData;
using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using Game.Props;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Game.Utils;
using Google.Protobuf;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Base;
using Pb.Game;
using System;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.Catalog;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AIParkGuestPanel : BaseGamePlayPanel<AIParkGuestPanel>
{
    [SerializeField] public AIParkGuestGroup guestGroup;
    [SerializeField] public CanvasGroup canvasGroup;
    [SerializeField] private CButton emoBtn;
    [SerializeField] private CButton editAvatarBtn;
    [SerializeField] private CButton chatMessageBtn;
    [SerializeField] private CButton roomMenuBtn;
    [SerializeField] private CButton hideUIBtn;
    [SerializeField] private CButton showUIBtn;
    [SerializeField] private CButton roomChatBtn;
    [SerializeField] private CButton retryBtn;
    //[SerializeField] private CButton fpsBtn;
    //[SerializeField] private CButton tpsBtn;
    [SerializeField] private CButton screenShotBtn;
    [SerializeField] private CButton screenModeBtn;
    [SerializeField] private GameObject rightHideRoot;
    [SerializeField] private GameObject leftTopRoot;
    [SerializeField] private GameObject centerBottomRoot;
    [SerializeField] private PlayGoalView playGoalView;
    [SerializeField] private CButton changeOcBtn;
    [SerializeField] private AIParkGuestChatNode chatView;
    [SerializeField] public AIParkChatView chatPopView;
    [SerializeField] private Transform dialogRoot;
    [SerializeField] private CButton Btn_Exit;
    [SerializeField] private GameObject screenEffect;
    [SerializeField] private GameObject defTarget;
    [SerializeField] private GameObject openDoorTarget;
    [SerializeField] private GameObject progressNode;
    [SerializeField] private List<GameObject> heartImg;
    [SerializeField] private Text txt_HpNum;
    [SerializeField] private FPSViewWidget fpsViewWidget;
    [SerializeField] public AIParkHUDPanel hudPanel;
    [SerializeField] private GameObject _hpContainer_1;
    [SerializeField] private GameObject _hpContainer_2;
    [SerializeField] private GameObject _cataButton;
    [SerializeField] private GameObject _ugCcataButton;
    [SerializeField] private CButton unLinkEmoteBtnA;
    [SerializeField] private CButton unLinkEmoteBtnB;
    public GameObject chatBanUINode;

    public Text TargetText1;
    public Text TargetText2;

    public bool IsPgcEnter { set; private get; }
    private GameObject selfDialogNode;
    private NpcDialogBox npcDialogBox;
    private SelfNpcDialogBox selfDialogBox;

    private List<string> cameraNodeNames = new() { "LeftTopRoot", "RightHideRoot/BottomRightGroup", "RightHideRoot/EditAvatarBtn", "RightHideRoot/GoalGroup" };
    private List<GameObject> camereNodes = new List<GameObject>();
    private CameraModePanel cameraPanel;

    private float inputDeltaTime = 15;
    private bool tempInput = false;
    private bool lockInput
    {
        get
        {
            return tempInput;
        }
        set
        {
            tempInput = value;
        }
    }
    private string currentReply = string.Empty;
    public string curConversationId { get; set; } //当前会话
    private AIPark_CharacterBehaviour npcBehaviour;
    private bool _bChangeTalkTo; //交谈对象改变
    private bool _aiReqIsEnd = false;
    private bool isShowDialog = true;
    public bool IsCreateDialog { get; set; }

    private int currentNpcLocation = (int)YandereAreaType.LivingRoom;

    private AIContentData aiResData;
    public int ChatCount { get; private set; } = 0; //对话次数

    private string _curNpcRoleId;
    private string _curNpcID;

    private AIParkGame aiGame;

    private string _linkBuddyName = "";
    private string _linkEmoteID = "";

    private AIParkDialogMgr _dialogManager;

    private bool _ignoreGuide = false;

    /// <summary>
    /// 获取对话管理器
    /// </summary>
    public AIParkDialogMgr GetDialogManager()
    {
        return _dialogManager;
    }

    public override void OnCreate()
    {
        base.OnCreate();

        // kinematicCharacter.gameObject.AddComponent<AIPark_CharacterBehaviour>();

        fpsViewWidget.InitCharacterNode(kinematicCharacter);
        emoBtn.onClick.AddListener(OnEmoBtnClick);
        editAvatarBtn.onClick.AddListener(OnEditAvatarBtnClick);
        roomMenuBtn.onClick.AddListener(OnRoomMenuBtnClick);
        chatMessageBtn.onClick.AddListener(OnChatBtnClick);
        retryBtn.onClick.AddListener(ParkRetryBtnClick);
        screenShotBtn.onClick.AddListener(ScreenShotBtnClick);
        screenModeBtn.onClick.AddListener(ScreenModeBtnClick);
        hideUIBtn.onClick.AddListener(HideUIBtnClick);
        showUIBtn.onClick.AddListener(ShowUIBtnClick);
        changeOcBtn.onClick.AddListener(OnChangeOcClick);
        Btn_Exit.onClick.AddListener(OnBtnExitClick);
        roomChatBtn.onClick.AddListener(OnRoomChatBtnClick);
        unLinkEmoteBtnA.onClick.AddListener(OnUnlinkEmoteBtnClick);
        unLinkEmoteBtnB.onClick.AddListener(OnUnlinkEmoteBtnClick);
        MessageHelper.AddListener<bool>(MessageName.UICameraMode, OnCameraMode);
        MessageHelper.AddListener<string, string>(MessageName.OnS11ChatEmoteClick, OnKeyboardEmoteClick);
        MessageHelper.AddListener<bool>(MessageName.BuddyLinkEmoteStateChange, OnBuddyLinkEmoteStateChange);
        MessageHelper.AddListener<string, string, bool>(MessageName.OnS11EmoteWithMsg, SendInputByEmoteEvent);
        MessageHelper.AddListener<AIGameParkConfig.EGuideAction>(MessageName.OnS11GuideStepAction, OnPgcGuideAction);
        MessageHelper.AddListener<string, string, Action>(MessageName.OnParkBeginNpcTalk, OnAIParkGameDialog);
        MessageHelper.AddListener(MessageName.OnJoystickChange, OnJoystickChangeHandler);
        MessageHelper.AddListener(MessageName.OnJoystickChange_new, OnJoystickChangeHandler);

        MessageHelper.AddListener<EmoteNetData>(MessageName.SelfCancelEmote, OnSelfCancelEmote);

        chatPopView.SetBanUINode(chatBanUINode);
        InitCameraBindComponent();
        if (kinematicCharacter.CurIKCController != null)
        {
            var rigidbody = kinematicCharacter.GetComponent<Rigidbody>();
            rigidbody.isKinematic = false;
            kinematicCharacter.CurIKCController.StableMovementData.MaxStableMoveSpeed = 2;
            kinematicCharacter.CurIKCController.JumpingData.JumpUpSpeed = 8;
            selfDialogNode = new GameObject("dialogpos");
            selfDialogNode.transform.SetParent(kinematicCharacter.PlayerAnimCtrl.transform);
            selfDialogNode.transform.localPosition = Vector3.zero;
            selfDialogNode.transform.localEulerAngles = Vector3.zero;
            selfDialogNode.transform.localScale = Vector3.one;

            var headView = kinematicCharacter.GetComponentInChildren<UserInfoHeadView>(true);
            if (headView != null)
            {
                headView.gameObject.SetActive(false);
            }
        }
        SetDialogVisible(false);
        if (petCharacter != null)
        {
            petCharacter.gameObject.SetActive(false);
        }
        hudPanel.Init();

        // 初始化对话管理器
        _dialogManager = new();
        _dialogManager.Init(dialogRoot, selfDialogNode);
    }

    void ParkRetryBtnClick()
    {
        AIParkPropsManager.Inst.ExitAction(((int)ParkNpcRoleType.self).ToString());
        RetryBtnClick();
    }

    public void LockInput()
    {
        lockInput = true;
    }

    private void SetDialogVisible(bool isVisible)
    {
        isShowDialog = isVisible;
        chatView.gameObject.SetActive(isShowDialog);
    }

    private void ChangeCamera(bool enterCameraMode)
    {
        _cataButton.SetActive(!enterCameraMode);
        leftTopRoot.SetActive(!enterCameraMode);
        rightHideRoot.SetActive(!enterCameraMode);
        chatView.gameObject.SetActive(!enterCameraMode);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        // var mpManager = GlobalNodeManager.Inst.Get<AIYandereCharacterManager>();
        // npcBehaviour = mpManager.GetNpcBev();
        UIManager.Inst.AddClosePanelAction(OnClosePanelAction);

        aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();

        _cataButton.SetActive(true);
        _ugCcataButton.SetActive(false);
    }

    public void ChangeTarget()
    {
        defTarget.SetActive(false);
        openDoorTarget.SetActive(true);
    }

    public void SetTarget(int stepID)
    {
        guestGroup.MiniMap.gameObject.SetActiveValid(true);
        return;  //乐园不需要事件目标 先注释了
        openDoorTarget.SetActive(true);
        if (!IsPgcEnter)
        {
            TargetText2.text = string.Format(AIGameParkConfig.ugcTargetDesc, aiGame.GameSetting.aIGameConfig.winRegulatorAmount, stepID);

            if (aiGame.GameSetting.aIGameConfig.winRegulatorAmount == stepID)
            {
                TargetText2.text = "离开医院：从医院大门中离开";
            }
        }
        else
        {
            if (stepID >= 0 && stepID < AIGameParkConfig.taskTarget.Count)
            {
                TargetText1.text = AIGameParkConfig.taskTarget[stepID].targetTitle;
                TargetText2.text = AIGameParkConfig.taskTarget[stepID].targetDesc;
            }
        }
        if (!IsPgcEnter)
        {
            _ugCcataButton.SetActive(true);
            AIParkCataData.Inst.GetCataData(aiGame.CurMapID);
            _cataButton.SetActive(false);
        }
        else
        {
            _cataButton.SetActive(true);
            _ugCcataButton.SetActive(false);
        }
    }

    public void StartNpcFirstTalk()
    {
        LoggerUtils.Log("设置目标 StartNpcFirstTalk");
        SetDialogVisible(true);
        DirectUnlockInput();

        //注释首次的对话
        // chatView.SetRecChatWithoutName("欢迎来到AI模拟器-逃离废弃医院，完成所有游戏目标并从医院中逃脱吧！",true,false);
        // if (IsPgcEnter)
        // {
        //     chatView.SetRecChat("当前目标", "53FF5A", $"和医生对话，说服医生打开病房门让你离开", true, false);
        // }
        // else
        // {
        //     string target = string.Format(AIGameParkConfig.ugcTargetDesc, aiGame.GameSetting.aIGameConfig.winRegulatorAmount,0);
        //     chatView.SetRecChat("当前目标", "53FF5A", target, true, false);
        // }
    }

    public void SendNpcTalk(string speaker, string content)
    {
        // AddNpcChat(speaker,content);
        content = content.Replace("（", "<i><c=c4c4c4>（");
        content = content.Replace("）", "）</c></i>");

        content = DataUtil.ReplaceEmojiForSTM(content);
        content = DataUtil.FilterNonStandardText(content);
        content = DataUtil.ReplaceRichText(content);
        chatView.SetRecChat(speaker, "6551FF", content, true, false);
    }

    public void UpdateChatViewLayout()
    {
        chatView.UpdateLayout();
    }



    public void HideHeart()
    {
        _hpContainer_1.SetActive(AIGameParkConfig.defaultHp != AIGameParkConfig.maxHp);
        _hpContainer_2.SetActive(AIGameParkConfig.defaultHp != AIGameParkConfig.maxHp);
    }

    /// <summary>
    /// 设置当前交互的目标并且唤起输入框
    /// </summary>
    public void PopKeyBoardByNpcInteract(AIPark_CharacterBehaviour characterBehaviour, string npcID)
    {
        _bChangeTalkTo = npcBehaviour != characterBehaviour;
        npcBehaviour = characterBehaviour;
        _curNpcRoleId = npcBehaviour.GetNpcID();
        _curNpcID = npcBehaviour.GetNpcID();
        LoggerUtils.Log($"交谈对象改变 = {_bChangeTalkTo} 初始化当前交互Npc 信息, npcID = {npcBehaviour.GetNpcID()}");
    }

    //TODO；输入聊天内容
    private void OnChatBtnClick()
    {
        if (lockInput)
        {
            return;
        }

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
        SetKeyBoardStatus(true);
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.hideKeyboard, OnHideKeyBoard);
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard);
        MobileInterface.Instance.ShowEmoteKeyboard(JsonUtility.ToJson(keyBoardInfo), _curNpcID);
    }
    private void OnKeyboard(string content)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(content))
        {
            DirectUnlockInput();
            return;
        }
        lockInput = true;
        WaitUnLockInput();
        SendInput(content);
    }

    private void OnKeyboardEmoteClick(string emoteId, string msg = null)
    {
        Debug.LogError("OnKeyboardEmoteClick:" + emoteId + " msg:" + msg);
        if (string.IsNullOrEmpty(emoteId))
        {
            DirectUnlockInput();
            return;
        }
        lockInput = true;
        WaitUnLockInput();
        SendInput(msg, emoteId);
    }

    private BudTimer lockTimer;

    private void WaitUnLockInput()
    {
        if (lockTimer != null)
        {
            TimerManager.Inst.Stop(lockTimer);
        }
        lockTimer = TimerManager.Inst.RunOnce("aiInput", inputDeltaTime, () =>
        {
            lockInput = false;
        });
    }

    public void DirectUnlockInput()
    {
        if (lockTimer != null)
        {
            TimerManager.Inst.Stop(lockTimer);
        }
        lockInput = false;
    }


    private void OnHideKeyBoard(string str)
    {
        SetKeyBoardStatus(false);
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.hideKeyboard);
    }

    private void SetKeyBoardStatus(bool status)
    {
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
        if (playerStateCtrl == null)
        {
            return;
        }
        MessageHelper.Broadcast(MessageName.TypeData, status);
    }

    //对话交互
    public void SendInput(string input, string emoteId = "")
    {
        Debug.Log("input:" + input + " emoteId:" + emoteId);
        if (String.IsNullOrEmpty(input) && String.IsNullOrEmpty(emoteId))
        {
            return;
        }
        //这里可以给每个npc都创建一个对话框
        IsCreateDialog = true;
        currentReply = string.Empty;

        if (input.Contains("（") && input.Contains("）"))
        {
            string pattern = "（.*?）";
            input = System.Text.RegularExpressions.Regex.Replace(input, pattern,
                match => $"<i><c=c4c4c4>{match.Value}</c></i>");
        }

        _dialogManager.ShowSelfDialog(input);
        var curNpcName = "<c=E477FF>@" + npcBehaviour?.GetNpcName() + "</c> ";
        chatView.SetRecChat(AccountDataManager.Inst.UserInfo.nickname, "FFD15B", $"{curNpcName} {input}", true, false);

        var chatData = new S11ChatReq()
        {
            content = input,
            emote = emoteId,
            npcId = _curNpcRoleId
        };
        string gameData = JsonConvert.SerializeObject(chatData);
        if (npcDialogBox != null)
        {
            npcDialogBox.ForceHide();
        }
        SendMsgToSever(gameData);
        npcBehaviour.IsNpcTalking = true;
    }

    public void SendInputByEmoteEvent(string emoteID, string npcName, bool isEnter)
    {
        string input = AIGameParkConfig.GetEmoteName(emoteID, isEnter);
        if (string.IsNullOrEmpty(input)) return;
        _linkBuddyName = npcName;
        _linkEmoteID = emoteID;
        var curNpcName = "<c=E477FF>@" + npcName + "</c> ";
        chatView.SetRecChat(AccountDataManager.Inst.UserInfo.nickname, "FFD15B", $"{curNpcName} {input}", true, false);
    }

    // public void AddNpcChat(string npcId, string content)
    // {
    //     AIPark_CharacterBehaviour npcBev = AIPark_CharacterManager.Inst.GetNpc(npcId);
    //     if (npcBev != null)
    //     {
    //         content = content.Replace("（", "<i><c=c4c4c4>（");
    //         content = content.Replace("）", "）</c></i>");

    //         string str = DataUtil.ReplaceEmojiForSTM(content);
    //         str = DataUtil.FilterNonStandardText(str);
    //         str = DataUtil.ReplaceRichText(str);
    //         chatView.SetRecChat(npcBev.GetNpcName(), "6551FF", str, IsCreateDialog, true);
    //         SetNpcTalk(str, true, true, false, () =>
    //         {
    //             npcBev.IsNpcTalking = false;
    //             npcBev.RecalculateChapter(3f);
    //         });
    //     }
    // }

    private void OnRecvChatRsp(S11AIMessageRsp msgData, bool isEnd)
    {
        GameAINpcChatManager_Park.Inst.SetNpcConversitionInfo(_curNpcID, msgData.conversationId);
        if (!string.IsNullOrEmpty(msgData.reply))
        {
            var content = msgData.reply.Replace("（", "<i><c=c4c4c4>（");
            content = content.Replace("）", "）</c></i>");
            currentReply += content;
        }

        if (!string.IsNullOrEmpty(currentReply))
        {
            // string str = DataUtil.ReplaceEmojiForSTM(currentReply);
            // str = DataUtil.FilterNonStandardText(str);
            // str = DataUtil.ReplaceRichText(str);
            // chatView.SetRecChat(npcBehaviour.GetNpcName(), "6551FF", str, IsCreateDialog, true);
            string str = "...";
            if (isEnd)
            {
                 str = DataUtil.ReplaceEmojiForSTM(currentReply);
                str = DataUtil.FilterNonStandardText(str);
                str = DataUtil.ReplaceRichText(str);
                chatView.SetRecChat(npcBehaviour.GetNpcName(), "6551FF", str, true, false);
            }
            SetNpcTalk(str, isEnd, false, false, () =>
            {
                UpdateNpcData(msgData);
                if (isEnd)
                {
                    npcBehaviour.IsNpcTalking = false;
                    npcBehaviour.RecalculateChapter(3f);
                }
            });
        }

        if (!npcBehaviour._npcStateController.IsMainState(PlayerState.DoubleEmote))
        {
            var stepID = PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + AIGameParkConfig.pgcGuideId, -1);
            var oldGuideFlag = PlayerPrefs.GetInt(AIGameParkConfig.guideKeyName);
            if (stepID < (int)AIGameParkConfig.EPgcGuideID.ProgressTips && oldGuideFlag == 0)
            {
                //如果没过第一步引导则不处理
            }
            //播放表情动画
            else if (!string.IsNullOrEmpty(msgData?.emoteId))
            {
                if (AIParkGuideMgr.Inst.IsDuringGuideFirstInGame())
                {
                    return;
                }
                var curState = npcBehaviour.GetCurrentState();
                LoggerUtils.Log("乐园" + npcBehaviour.GetNpcName() + " 播放表情动画:" + msgData?.emoteId);

                if (curState is IdleState)
                {
                    var talkState = npcBehaviour.StartTalkWithPlayer();
                    talkState.StartPlayNpcEmote(msgData?.emoteId);
                }

                // if (curState is TalkWithPlayerState)
                // {
                //     var talkState = curState as TalkWithPlayerState;
                //     talkState.StartPlayNpcEmote(msgData?.emoteId);
                // }
            }
        }

        IsCreateDialog = false;
    }

 
    public void WaitNpcTalk(string text)
    {
        _dialogManager.ShowNpcDialog(npcBehaviour, text, false, true, true);
    }

    public void SetNpcTalk(string text, bool isEnd, bool needAni, bool isFirstContent, Action action)
    {
        _dialogManager.ShowNpcDialog(npcBehaviour, text, isEnd, needAni, isFirstContent, action);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        AIGameSoundUtils.Inst.StopBgm(YandereConfig.BGM_PLAY);
        UIManager.Inst.RemoveClosePanelAction(OnClosePanelAction);
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

    void OnAIParkGameDialog(string speaker, string content, Action ac)
    {
        AIParkGame aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();

        var title = aiGame.GetNpcName(speaker);

        content = content.Replace("（", "<i><c=c4c4c4>（");
        content = content.Replace("）", "）</c></i>");

        content = DataUtil.ReplaceEmojiForSTM(content);
        content = DataUtil.FilterNonStandardText(content);
        content = DataUtil.ReplaceRichText(content);
        chatView.SetRecChat(title, "6551FF", content, true, false);
    }
    void OnJoystickChangeHandler()
    {
        if (MobileJoystick.Inst != null)
        {
            // MobileJoystick.Inst.OnResetJoystick();
        }
        AIParkPropsManager.Inst.SelfEnterIdle();
    }

    void OnSelfCancelEmote(EmoteNetData emoteNetData)
    {
        // AIParkPropsManager.Inst.SelfExitProp();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener<bool>(MessageName.UICameraMode, OnCameraMode);
        MessageHelper.RemoveListener<string, string>(MessageName.OnS11ChatEmoteClick, OnKeyboardEmoteClick);
        MessageHelper.RemoveListener<bool>(MessageName.BuddyLinkEmoteStateChange, OnBuddyLinkEmoteStateChange);
        MessageHelper.RemoveListener<string, string, bool>(MessageName.OnS11EmoteWithMsg, SendInputByEmoteEvent);
        MessageHelper.RemoveListener<AIGameParkConfig.EGuideAction>(MessageName.OnS11GuideStepAction, OnPgcGuideAction);
        MessageHelper.RemoveListener<string, string, Action>(MessageName.OnParkBeginNpcTalk, OnAIParkGameDialog);
        MessageHelper.RemoveListener(MessageName.OnJoystickChange, OnJoystickChangeHandler);
        MessageHelper.RemoveListener(MessageName.OnJoystickChange_new, OnJoystickChangeHandler);

        MessageHelper.RemoveListener<EmoteNetData>(MessageName.SelfCancelEmote, OnSelfCancelEmote);
        emoBtn.onClick.RemoveAllListeners();
        editAvatarBtn.onClick.RemoveAllListeners();
        roomMenuBtn.onClick.RemoveAllListeners();
        chatMessageBtn.onClick.RemoveAllListeners();
        retryBtn.onClick.RemoveAllListeners();
        screenShotBtn.onClick.RemoveAllListeners();
        screenModeBtn.onClick.RemoveAllListeners();
        hideUIBtn.onClick.RemoveAllListeners();
        showUIBtn.onClick.RemoveAllListeners();
        changeOcBtn.onClick.RemoveAllListeners();
        Btn_Exit.onClick.RemoveAllListeners();
        roomChatBtn.onClick.RemoveAllListeners();
        unLinkEmoteBtnA.onClick.RemoveAllListeners();
        unLinkEmoteBtnB.onClick.RemoveAllListeners();
        DirectUnlockInput();
        AIParkAvatarManager.Inst.Release();

        _dialogManager?.Release();
        _dialogManager = null;
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
        _ugCcataButton.gameObject.SetActive(false);
        _cataButton.gameObject.SetActive(false);
        bool bHideTab = true;
        var emotePanel = UIManager.Inst.OpenPanel<EmoMenuPanel_AIGame>(PanelId.EmoMenuPanel_AIGame, bHideTab);
        //var emotePanel = UIManager.Inst.OpenPanel<AIBuddyOptionPanel>(PanelId.AIBuddyOptionPanel);
        emotePanel.CloseAction = () =>
        {
            ChangeCameraPanel(true);
            _ugCcataButton.gameObject.SetActive(true);
            _cataButton.gameObject.SetActive(true);
        };
        ChangeCameraPanel(false);
    }

    private void OnClosePanelAction(BasePanel panel)
    {
        if (panel is EmoMenuPanel_AIGame)
        {
            rightHideRoot.SetActive(true);
        }
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
        AIParkAvatarManager.Inst.OnEnterFittingRoom();
    }

    private void OnCloseFittingRoom(BaseAvatarData baseAvatarData)
    {
        if (baseAvatarData == null)
        {
            baseAvatarData = AvatarDataManager.Inst.SelfCharacterData;
        }

        if (baseAvatarData is CharacterData characterData)
        {
            AIParkAvatarManager.Inst.OnExitFittingRoom(characterData);
            // 确保UI状态正确恢复
            UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestChangeOcBtn, AbilityKey.CameraMode);
        }
        else if (baseAvatarData is PetData petData)
        {
            AIParkAvatarManager.Inst.OnExitPetFittingRoom(petData);
        }
    }

    private void OnChangeOcClick()
    {
        UIManager.Inst.OpenPanel<OcChangePanel>(PanelId.OcChangePanel, OcChangeScene.Play).OnCloseAction = OnCloseFittingRoom;
        AIParkAvatarManager.Inst.OnEnterFittingRoom();
    }

    private void OnRoomMenuBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.RoomMenuPanel);
    }


    private void ScreenModeBtnClick()
    {
        ChangeCamera(true);
        cameraPanel = UIManager.Inst.OpenPanel<CameraModePanel>(PanelId.CameraModePanel);
        cameraPanel.CloseCameraModeBtn.onClick.AddListener(() =>
        {
            ChangeCamera(false);
        });
    }

    public void HideUIBtnClick()
    {
        //todo
        showUIBtn.gameObject.SetActive(true);
        hideUIBtn.gameObject.SetActive(false);
        rightHideRoot.SetActive(false);
        leftTopRoot.SetActive(false);
        centerBottomRoot.SetActive(false);
        chatView.gameObject.SetActive(false);
    }

    public void ShowUIBtnClick()
    {
        //todo
        showUIBtn.gameObject.SetActive(false);
        hideUIBtn.gameObject.SetActive(true);
        rightHideRoot.SetActive(true);
        leftTopRoot.SetActive(true);
        centerBottomRoot.SetActive(true);
        chatView.gameObject.SetActive(true);
    }


    public AIParkHUDPanel GetHubPanel()
    {
        return hudPanel;
    }

    public void GuideHidePanel()
    {
        canvasGroup.alpha = 1;
        chatView.transform.localScale = Vector3.one;
        canvasGroup.interactable = false;

        var trans = canvasGroup.transform;
        for (int i = 0; i < trans.childCount; i++)
        {
            trans.GetChild(i).localScale = Vector3.zero;
        }
        dialogRoot.localScale = Vector3.one;

        var c = guestGroup.GetComponent<CanvasGroup>();
        c.alpha = 0;
        c.interactable = false;
        hudPanel.gameObject.SetActive(true);
    }

    public void GuideRevertPanel()
    {
        canvasGroup.interactable = true;
        // guestGroup.GetComponent<CanvasGroup>().interactable = true;

        var trans = canvasGroup.transform;
        for (int i = 0; i < trans.childCount; i++)
        {
            trans.GetChild(i).localScale = Vector3.one;
        }

        trans = guestGroup.transform;
        for (int i = 0; i < trans.childCount; i++)
        {
            trans.GetChild(i).localScale = Vector3.one;
        }
        var leftTogGroup = trans.Find("LeftTogGroup");
        if (leftTogGroup != null)
        {
            leftTogGroup.localScale = Vector3.one;
            for (int j = 0; j < leftTogGroup.childCount; j++)
            {
                leftTogGroup.GetChild(j).localScale = Vector3.one;
            }
        }
        //
        var rightGroup = trans.Find("RightGroup");
        if (rightGroup != null)
        {
            rightGroup.localScale = Vector3.one;
            for (int j = 0; j < rightGroup.childCount; j++)
            {
                rightGroup.GetChild(j).localScale = Vector3.one;
            }
        }
        GuideCameraJoyStickRevert();
    }

    public void GuideTarget2SwingTxt()
    {
        guestGroup.GetComponent<CanvasGroup>().alpha = 1;
        var trans = guestGroup.transform;
        for (int i = 0; i < trans.childCount; i++)
        {
            trans.GetChild(i).localScale = Vector3.zero;
        }
        var rightGroup = trans.Find("RightGroup");
        if (rightGroup != null)
        {
            rightGroup.localScale = Vector3.one;
            for (int j = 0; j < rightGroup.childCount; j++)
            {
                rightGroup.GetChild(j).localScale = Vector3.zero;
            }
            rightGroup.Find("EventTargetBg").localScale = Vector3.one;
        }
    }

    public void GuideCameraJoyStick()
    {
        var trans = canvasGroup.transform;
        var cantHideRoot = trans.Find("CantHideRoot");
        cantHideRoot.localScale = Vector3.one;
        for (int i = 0; i < cantHideRoot.childCount; i++)
        {
            cantHideRoot.GetChild(i).localScale = Vector3.zero;
        }
        cantHideRoot.Find("CamereJoyStick").localScale = Vector3.one;
    }

    public void DisableJoyStick()
    {
        var trans = canvasGroup.transform;
        var cantHideRoot = trans.Find("CantHideRoot");
        cantHideRoot.localScale = Vector3.zero;
    }

    public void GuideCameraJoyStickRevert()
    {
        var trans = canvasGroup.transform;
        var cantHideRoot = trans.Find("CantHideRoot");
        cantHideRoot.localScale = Vector3.one;
        for (int i = 0; i < cantHideRoot.childCount; i++)
        {
            cantHideRoot.GetChild(i).localScale = Vector3.one;
        }

    }

    public void GuideMapPanel()
    {
        var trans = canvasGroup.transform;
        for (int i = 0; i < trans.childCount; i++)
        {
            trans.GetChild(i).localScale = Vector3.one;
        }
        var c = guestGroup.GetComponent<CanvasGroup>();
        c.alpha = 1;
        trans = guestGroup.transform;
        for (int i = 0; i < trans.childCount; i++)
        {
            trans.GetChild(i).localScale = Vector3.zero;
        }
        var leftTogGroup = trans.Find("LeftTogGroup");
        if (leftTogGroup != null)
        {
            leftTogGroup.localScale = Vector3.one;
            for (int j = 0; j < leftTogGroup.childCount; j++)
            {
                leftTogGroup.GetChild(j).localScale = Vector3.zero;
            }
            var miniMapBg = leftTogGroup.Find("MiniMapBg");
            if (miniMapBg != null)
            {
                miniMapBg.localScale = Vector3.one;
                miniMapBg.gameObject.SetActive(true);
            }
        }
    }

    public void GuideEventTargetPanel()
    {
        var trans = guestGroup.transform;
        var rightGroup = trans.Find("RightGroup");
        if (rightGroup != null)
        {
            rightGroup.localScale = Vector3.one;
            for (int j = 0; j < rightGroup.childCount; j++)
            {
                rightGroup.GetChild(j).localScale = Vector3.zero;
            }
            rightGroup.Find("EventTargetBg").localScale = Vector3.one;
        }

        trans = canvasGroup.transform;
        // var leftTop = trans.Find("LeftTopRoot");
        // if (leftTop != null)
        // {
        //     // leftTop.localScale = Vector3.one;
        //     // for (int j = 0; j < leftTop.childCount; j++)
        //     // {
        //     //     leftTop.GetChild(j).localScale = Vector3.zero;
        //     // }
        //     // leftTop.Find("Btn_Exit").localScale = Vector3.one;
        // }

        trans.Find("LeftTopRoot").localScale = Vector3.zero;
        // trans.Find("RightHideRoot").localScale = Vector3.one;
        // trans.Find("AIYangereChatView").localScale = Vector3.one;
    }

    public void GuideEvent2TargetBg()
    {

    }

    public void GuideEventTargetBgShow(string eventName)
    {
        guestGroup.ShowCustomEvent(eventName);
    }

    public void HidePanel()
    {
        canvasGroup.alpha = 0;
        chatView.transform.localScale = Vector3.zero;
        canvasGroup.interactable = false;
        var c = guestGroup.GetComponent<CanvasGroup>();
        c.alpha = 0;
        c.interactable = false;

        hudPanel.gameObject.SetActive(false);
    }

    public void ShowPanel()
    {
        canvasGroup.alpha = 1;
        chatView.transform.localScale = Vector3.one;
        canvasGroup.interactable = true;
        var c = guestGroup.GetComponent<CanvasGroup>();
        c.alpha = 1;
        c.interactable = true;

        hudPanel.gameObject.SetActive(true);
    }

    #region UI相关方法
    private void OnBtnExitClick()
    {
        // AIPark_CharacterManager.Inst.EnableSelfCharacterDetection();
        // AIPark_CharacterManager.Inst.EnableCharacterDetection("106");

        // return;


        // AIParkTcpNetMgr.Instance.TestSendAIParkSyncReq_NpcNpc();
        // return;

        //     ParkNpcTransData currentPoint = new ParkNpcTransData()
        //     { 
        //          pos = new Vector3(1.75f, 0, -4.2f),
        //         rot =  new Vector3(0,90,0),  
        //     };

        // AIPark_CharacterManager.Inst.GetNpc("106").SetPositionAndRotation(currentPoint.pos, Quaternion.Euler(currentPoint.rot));
        // // return;
        // AIParkTcpNetMgr.Instance.TestNpcHistory();
        // return;

        //         AIParkGame aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
        // aiGame.EnterBlackPanel();
        // var npc1= AIPark_CharacterManager.Inst.GetNpc("npc1");
        // npc1._npcAnimController.SetPlayerAniState(PlayerAniState.GashaponOneTime,true);
        // npc1.PlayAnim("40100009");
        // npc1.PlayAnim("40100011");
        // return;
        var panel = UIManager.Inst.OpenPanel<AIParkConfirmPanel>(PanelId.AIParkConfirmPanel);
        panel.SetData("退出游戏", "你确定要中途退出游戏吗？\n当前的游戏进度会被清空哦！", "退出游戏", "继续游玩",
            () =>
            {
                aiGame.ExitGame();
            },
            () =>
            {

            });
    }

    void OnApplicationFocus(bool focusStatus)
    {
        Debug.Log("乐园AIParkGuestPanel OnApplicationFocus 检查状态:" + focusStatus);
        if (focusStatus)
        {
            AIParkTcpNetMgr.Instance.CheckReconnectState();
        }
        var aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
        if (aiGame != null)
        {
            aiGame.OnApplicationFocus(focusStatus);
        }
    }



    #endregion

    #region AI请求
    private void SendMsgToSever(string data, UnityAction<S11AIMessageRsp, bool> onSuccess = null, UnityAction onFail = null)
    {
        S11AIMessageReq reqData = new()
        {
            gameId = (int)PGCGameType.AIPark,
            // conversationId = GameAINpcChatManager_Park.Inst.GetNpcConversitionID(_curNpcID),
            gameData = data,
            // npcId = IsPgcEnter ? string.Empty : _curNpcID,
            // npcId = _curNpcID,
            mapId = IsPgcEnter ? string.Empty : aiGame.CurMapID,
            // sessionId = GameAINpcChatManager_Park.Inst.GetSessionID()
            sessionId = AIParkUtils.Inst.ParkGameData.sessionId
        };
        var paramStr = JsonConvert.SerializeObject(reqData);
        WaitNpcTalk("......");
        bool isFirst = true;
        //npcBehaviour.StartTalkWithPlayer();
        AIParkGuideMgr.Inst.SpeakBeginTriggerNext();

        LoggerUtils.Log($"当前交互对象 {_curNpcID},会话ID = {GameAINpcChatManager_Park.Inst.GetNpcConversitionID(_curNpcID)}");
        NetworkManager.Inst.SendHttpRequestOnStream(HttpUrlDefine.AIChat, HttpMethod.POST, paramStr, (content) =>
        {
            LoggerUtils.Log("内容:" + content);
            if (string.IsNullOrEmpty(content))
            {
                DirectUnlockInput();
                LoggerUtils.LogError($"服务端数据流异常 content is null");
                onFail?.Invoke();
                return;
            }
            var resposeData = JsonConvert.DeserializeObject<AIResposeData>(content);
            if (resposeData == null)
            {
                DirectUnlockInput();
                LoggerUtils.LogError($"服务端数据流无法解析 resposeData is null");
                onFail?.Invoke();
                return;
            }

            if (string.IsNullOrEmpty(resposeData.message))
            {
                DirectUnlockInput();
                LoggerUtils.LogError($"服务端数据流消息为null resposeData.message is null");
                onFail?.Invoke();
                return;
            }

            bool isEnd = resposeData.isEnd == 1;

            SetNpcLookAtByFirstStream();

            // var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
            var msgDataRsp = JsonConvert.DeserializeObject<S11AIMessageRsp>(resposeData.message);
            msgDataRsp.ParseGameData();
            JObject jObject = JObject.Parse(msgDataRsp.gameData);
            jObject.TryGetValue("triggerAction", out JToken historyToken);
            if (historyToken != null)
            {
                try
                {
                    // 预处理JSON，确保字段名正确且处理null值
                    var processedToken = new JObject();

                    // 处理participants字段
                    if (historyToken["participants"] != null && historyToken["participants"].Type != JTokenType.Null)
                    {
                        processedToken["participants"] = historyToken["participants"];
                    }
                    else
                    {
                        processedToken["participants"] = new JArray();
                    }

                    // 处理location字段
                    if (historyToken["location"] != null)
                    {
                        processedToken["location"] = historyToken["location"];
                    }
                    else
                    {
                        processedToken["location"] = 0;
                    }

                    // 处理action字段
                    if (historyToken["action"] != null)
                    {
                        processedToken["action"] = historyToken["action"];
                    }
                    else
                    {
                        processedToken["action"] = 0;
                    }

                    // 处理quotes字段
                    if (historyToken["quotes"] != null && historyToken["quotes"].Type != JTokenType.Null)
                    {
                        processedToken["quotes"] = historyToken["quotes"];
                    }
                    else
                    {
                        processedToken["quotes"] = new JArray();
                    }

                    LoggerUtils.Log($"预处理后的JSON: {processedToken}");
                    var history = History.Parser.ParseJson(processedToken.ToString());
                    // 确保当前NPC在参与者列表中
                    if (!history.Participants.Contains(_curNpcID))
                    {
                        history.Participants.Add(_curNpcID);
                    }
                    Debug.Log("乐园消息讲话结尾插入剧情:" + JsonConvert.SerializeObject(history));
                    //todo 触发新的行为
                    AIPark_CharacterManager.Inst.InsertHistoryImmediate(_curNpcID, history);
                }
                catch (System.Exception ex)
                {
                    LoggerUtils.LogError($"解析事件数据失败: {ex.Message}");
                    LoggerUtils.LogError($"原始JSON: {historyToken.ToString()}");
                    throw;
                }

            }
            // var gameDataRsp = JsonConvert.DeserializeObject<S11ChatRsp>(msgDataRsp.gameData);

            if (isFirst)
            {
                ChatCount++;
                isFirst = false;
                if (aiResData != null)
                {
                    //aiResData.remaining = msgDataRsp.remainingChatCnt;
                }
            }

            //UpdateNpcData(gameDataRsp);
            OnRecvChatRsp(msgDataRsp, isEnd);

            if (isEnd)
            {
                WaitNpcSpeakByEndStream(msgDataRsp);
                AIParkGuideMgr.Inst.SpeakOverTriggerNext();
            }
            onSuccess?.Invoke(msgDataRsp, isEnd);
        }, () =>
        {
            YandereDataManager.Inst.IsFirstReply = false;
            LoggerUtils.Log("AIContent Stream is Close");
        });
    }

    private void SetNpcLookAtByFirstStream()
    {
        if (IsCreateDialog)
        {
            if (selfDialogBox != null)
            {
                selfDialogBox.ForceHide();
            }
            //npcBehaviour.LookAtPlayer();
        }
    }

    private void WaitNpcSpeakByEndStream(S11AIMessageRsp msgDataRsp)
    {
        if (string.IsNullOrEmpty(currentReply))
        {
            DirectUnlockInput();
            if (npcDialogBox != null)
            {
                npcDialogBox.Disappear();
            }
            CompleteDialog(msgDataRsp);
        }
        else
        {
            TimerManager.Inst.RunOnce("FinalContent", YandereDataManager.Inst.TextAnimDuration, () =>
            {
                DirectUnlockInput();
                CompleteDialog(msgDataRsp);
            });
        }
    }

    public void SetDoorProgressVisible(bool isVisible)
    {
        progressNode.SetActive(isVisible);
    }

    private void UpdateNpcData(S11AIMessageRsp msgData)
    {
        if (msgData.replyIsEnd == 1)
        {

        }
        else if (msgData.replyIsEnd == 2)
        {
            if (PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + AIGameParkConfig.pgcGuideId) < (int)AIGameParkConfig.EPgcGuideID.ProgressTips
&& IsPgcEnter)
            {
                //引导未做 先注释
                // UIManager.Inst.OpenPanel<AIParkStrongGuide>(PanelId.AIParkStrongGuidePanel, AIGameParkConfig.EPgcGuideID.ProgressTips);
            }
        }
    }

    private void CompleteDialog(S11AIMessageRsp msgDataRsp)
    {
        if (!msgDataRsp.Equals(new S11AIMessageRsp()))
        {
            YandereDataManager.Inst.IsFirstReply = false;
            var aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
            if (aiGame != null)
            {
                aiGame.GameStepCheck();
            }
        }
    }

    public void OnHpChange(int hp)
    {
        HideHeart();

        if (hp > 3)
        {
            _hpContainer_1.SetActive(false);
            _hpContainer_2.SetActive(true);
            txt_HpNum.text = "x" + hp;
        }
        else
        {
            _hpContainer_1.SetActive(true);
            _hpContainer_2.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                heartImg[i].SetActive(hp > i);
            }
        }
    }

    private void OnRoomChatBtnClick()
    {
        chatPopView.ShowChatView();
    }
    #endregion

    #region 退出游戏时处理倒计时
    public void StopTimer()
    {
        LoggerUtils.Log("S11 GameEnd StopAllSound");
        AIGameSoundUtils.Inst.StopAllSound();
        SimpleGameDurationManager.Inst.Release();
    }

    #endregion

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

    private void OnBuddyLinkEmoteStateChange(bool isLink)
    {
        unLinkEmoteBtnA.gameObject.SetActive(isLink);
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

    private void OnPgcGuideAction(AIGameParkConfig.EGuideAction eAction)
    {
        if (IsPgcEnter && eAction == AIGameParkConfig.EGuideAction.ShowHeartTips)
        {
            //引导未做 先注释
            // UIManager.Inst.OpenPanel<AIParkStrongGuide>(PanelId.AIParkStrongGuidePanel, AIGameParkConfig.EPgcGuideID.HeartTips);
        }
    }
}


