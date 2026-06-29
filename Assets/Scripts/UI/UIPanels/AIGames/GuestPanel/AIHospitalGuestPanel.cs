using System;
using Game.Base;
using GameData;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using AIGame.Base;
using Basic.Utils;
using Game.AIResData;
using Game.Avatar;
using Game.Utils;
using Network;
using Network.Http;
using UI.UIPanels.FittingRoom;
using UnityEngine.Events;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;
using Game.Props;
using Game.Props.PropsManagers;
using UI.Catalog;
using UnityEngine.TextCore.Text;

public class AIHospitalGuestPanel : BaseGamePlayPanel<AIHospitalGuestPanel>
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CButton emoBtn;
    [SerializeField] private CButton editAvatarBtn;
    [SerializeField] private CButton chatMessageBtn;
    [SerializeField] private CButton roomMenuBtn;
    [SerializeField] private CButton hideUIBtn;
    [SerializeField] private CButton showUIBtn;
    [SerializeField] private CButton roomChatBtn;
    [SerializeField] private CButton retryBtn;
    [SerializeField] private CButton dialogBtn;
    //[SerializeField] private CButton fpsBtn;
    //[SerializeField] private CButton tpsBtn;
    [SerializeField] private CButton screenShotBtn;
    [SerializeField] private CButton screenModeBtn;
    [SerializeField] private GameObject rightHideRoot;
    [SerializeField] private GameObject leftTopRoot;
    [SerializeField] private GameObject centerBottomRoot;
    [SerializeField] private PlayGoalView playGoalView;
    [SerializeField] private CButton changeOcBtn;
    [SerializeField] private AIYangereChatView chatView;
    [SerializeField] private Transform dialogRoot;
    [SerializeField] private CButton Btn_Exit;
    [SerializeField] private GameObject screenEffect;
    [SerializeField] private GameObject defTarget;
    [SerializeField] private GameObject openDoorTarget;
    [SerializeField] private GameObject progressNode;
    [SerializeField] private List<GameObject> heartImg;
    [SerializeField] private Text txt_HpNum;
    [SerializeField] private FPSViewWidget fpsViewWidget;
    [SerializeField] private AIHospitalHUDPanel hudPanel;
    [SerializeField] private GameObject _hpContainer_1; 
    [SerializeField] private GameObject _hpContainer_2;
    [SerializeField] private GameObject _cataButton;
    [SerializeField] private GameObject _ugCcataButton;
    [SerializeField] private CButton unLinkEmoteBtnA;
    [SerializeField] private CButton unLinkEmoteBtnB;

    //[SerializeField] private
    //[SerializeField] private Slider doorProgress;

    public Text TargetText1;
    public Text TargetText2;

    public bool IsPgcEnter { set; private get; }
    private GameObject selfDialogNode;
    private NpcDialogBox npcDialogBox;
    private SelfNpcDialogBox selfDialogBox;
        
    private List<string> cameraNodeNames = new() { "LeftTopRoot","RightHideRoot/BottomRightGroup","RightHideRoot/EditAvatarBtn","RightHideRoot/GoalGroup"};
    private List<GameObject> camereNodes = new List<GameObject>();
    private GameMode curGameMode = GameMode.Play;
    private CameraModePanel cameraPanel;
    
    private float inputDeltaTime = 15;
    private bool tempInput = false;
    private int currencyState = 0;
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
    private AIHospital_CharacterBehaviour npcBehaviour;
    private bool _bChangeTalkTo; //交谈对象改变
    private bool _aiReqIsEnd = false;
    private bool isShowDialog = true;
    public bool IsCreateDialog { get; set; }
    
    private int currentNpcLocation = (int)YandereAreaType.LivingRoom;

    private AIContentData aiResData;
    public int ChatCount { get;private set; } = 0; //对话次数

    private HospitalNpcRoleType _curNpcRoleType;
    private string _curNpcID;

    private AIHospitalGame aiGame;

    private string _linkBuddyName = "";
    private string _linkEmoteID = "";

    private AIHospitalDialogMgr _dialogManager;

    private bool _ignoreGuide = false;

    public override void OnCreate()
    {
        base.OnCreate();
        fpsViewWidget.InitCharacterNode(kinematicCharacter);
        emoBtn.onClick.AddListener(OnEmoBtnClick);
        editAvatarBtn.onClick.AddListener(OnEditAvatarBtnClick);
        roomMenuBtn.onClick.AddListener(OnRoomMenuBtnClick);
        chatMessageBtn.onClick.AddListener(OnChatBtnClick);
        retryBtn.onClick.AddListener(RetryBtnClick);
        dialogBtn.onClick.AddListener(SetDialogVisible);
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
        MessageHelper.AddListener<string,string>(MessageName.OnS9ChatEmoteClick,OnKeyboardEmoteClick);
        MessageHelper.AddListener<bool>(MessageName.BuddyLinkEmoteStateChange,OnBuddyLinkEmoteStateChange);
        MessageHelper.AddListener<string, string,bool>(MessageName.OnS9EmoteWithMsg,SendInputByEmoteEvent);
        MessageHelper.AddListener<AIGameHospitalConfig.EGuideAction>(MessageName.OnS9GuideStepAction, OnPgcGuideAction);
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
        _dialogManager = new AIHospitalDialogMgr();
        _dialogManager.Init(dialogRoot, selfDialogNode);
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

    private void SetDialogVisible()
    {
        isShowDialog = !isShowDialog;
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

        aiGame = AIGameController.Inst.GetCurAIGame<AIHospitalGame>();
    }

    public void ChangeTarget()
    {
        defTarget.SetActive(false);
        openDoorTarget.SetActive(true);
    }

    public void SetTarget(int stepID)
    {
        openDoorTarget.SetActive(true);
        if (!IsPgcEnter)
        {
            TargetText2.text = string.Format(AIGameHospitalConfig.ugcTargetDesc, aiGame.GameSetting.aIGameConfig.winRegulatorAmount,stepID) ;

            if (aiGame.GameSetting.aIGameConfig.winRegulatorAmount == stepID)
            {
                TargetText2.text = "离开医院：从医院大门中离开";
            }
        }
        else
        {
            if (stepID >= 0 && stepID < AIGameHospitalConfig.taskTarget.Count)
            {
                TargetText1.text = AIGameHospitalConfig.taskTarget[stepID].targetTitle;
                TargetText2.text = AIGameHospitalConfig.taskTarget[stepID].targetDesc;
            }
        }
        if (!IsPgcEnter)
        {
            _ugCcataButton.SetActive(true);
            AIHospitalCataData.Inst.GetCataData(aiGame.CurMapID);
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
        chatView.SetRecChatWithoutName("欢迎来到AI模拟器-逃离废弃医院，完成所有游戏目标并从医院中逃脱吧！",true,false);
        if (IsPgcEnter)
        {
            chatView.SetRecChat("当前目标", "53FF5A", $"和医生对话，说服医生打开病房门让你离开", true, false);
        }
        else
        {
            string target = string.Format(AIGameHospitalConfig.ugcTargetDesc, aiGame.GameSetting.aIGameConfig.winRegulatorAmount,0);
            chatView.SetRecChat("当前目标", "53FF5A", target, true, false);
        }
    }




    public void HideHeart()
    {
        _hpContainer_1.SetActive(AIGameHospitalConfig.defaultHp != AIGameHospitalConfig.maxHp);
        _hpContainer_2.SetActive(AIGameHospitalConfig.defaultHp != AIGameHospitalConfig.maxHp);
    }

    /// <summary>
    /// 设置当前交互的目标并且唤起输入框
    /// </summary>
    public void PopKeyBoardByNpcInteract(AIHospital_CharacterBehaviour characterBehaviour,string npcID)
    {
        _bChangeTalkTo = npcBehaviour != characterBehaviour;
        npcBehaviour = characterBehaviour;
        _curNpcRoleType = npcBehaviour.GetNpcRoleType();
        _curNpcID = npcBehaviour.GetNpcID();
        LoggerUtils.Log($"交谈对象改变 = {_bChangeTalkTo} 初始化当前交互Npc 信息 roleType = {npcBehaviour.GetNpcRoleType()}, npcID = {npcBehaviour.GetNpcID()}");
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
        MobileInterface.Instance.ShowEmoteKeyboard(JsonUtility.ToJson(keyBoardInfo),_curNpcID);
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

    private void OnKeyboardEmoteClick(string emoteId,string msg=null)
    {
        if (string.IsNullOrEmpty(emoteId))
        {
            DirectUnlockInput();
            return;
        }
        lockInput = true;
        WaitUnLockInput();
        SendInput(msg,emoteId);
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
    public void SendInput(string input,string emoteId = "")
    {
        if (String.IsNullOrEmpty(input)&&String.IsNullOrEmpty(emoteId))
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

        var chatData = new S9ChatReq()
        {
            content = input,
            emote = emoteId,
            hospitalNPCRole = (int)_curNpcRoleType
        };
        string gameData = JsonConvert.SerializeObject(chatData);
        if (npcDialogBox != null)
        {
            npcDialogBox.ForceHide();
        }
        SendMsgToSever(gameData);
        npcBehaviour.IsNpcTalking = true;
    }

    public void SendInputByEmoteEvent(string emoteID,string npcName,bool isEnter)
    {
        string input = AIGameHospitalConfig.GetEmoteName(emoteID,isEnter);
        if (string.IsNullOrEmpty(input)) return;
        _linkBuddyName = npcName;
        _linkEmoteID = emoteID;
        var curNpcName = "<c=E477FF>@" + npcName + "</c> ";
        chatView.SetRecChat(AccountDataManager.Inst.UserInfo.nickname, "FFD15B", $"{curNpcName} {input}", true, false);
    }

    private void OnRecvChatRsp(S9AIMessageRsp msgData, S9ChatRsp gameData, bool isEnd)
    {
        GameAINpcChatManager.Inst.SetNpcConversitionInfo(_curNpcID,msgData.conversationId);
        if (gameData != null && !string.IsNullOrEmpty(gameData.reply))
        {
            var content = gameData.reply.Replace("（","<i><c=c4c4c4>（");
            content = content.Replace("）", "）</c></i>");
            currentReply += content;
        }

        if (!string.IsNullOrEmpty(currentReply))
        {
            string str = DataUtil.ReplaceEmojiForSTM(currentReply);
            str = DataUtil.FilterNonStandardText(str);
            str = DataUtil.ReplaceRichText(str);
            chatView.SetRecChat(npcBehaviour.GetNpcName(), "6551FF", str,  IsCreateDialog, true);
            SetNpcTalk(str, isEnd, true, false, () =>
            {
                UpdateNpcData(gameData);
                if (isEnd)
                {
                    npcBehaviour.IsNpcTalking = false;
                    npcBehaviour.RecalculateChapter(3f);
                }
            });
        }

        if (!npcBehaviour._npcStateController.IsMainState(PlayerState.DoubleEmote))
        {
            var stepID =  PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + AIGameHospitalConfig.pgcGuideId, -1);
            var oldGuideFlag = PlayerPrefs.GetInt(AIGameHospitalConfig.guideKeyName);
            if (stepID < (int)AIGameHospitalConfig.EPgcGuideID.ProgressTips && oldGuideFlag == 0)
            {
                //如果没过第一步引导则不处理
            }
            //播放表情动画
            else if (!string.IsNullOrEmpty(gameData?.emote))
            {
                var curState = npcBehaviour.GetCurrentState();
                if (curState is TalkWithPlayerState)
                {
                    var talkState = curState as TalkWithPlayerState;
                    talkState.StartPlayNpcEmote(gameData?.emote);
                }
            }
        }

        if (isEnd && !string.IsNullOrEmpty(gameData.taskAnswer))
        {
            TimerManager.Inst.RunOnce("S9code", 5, () =>
            {
                CreateTaskCodeDialog(gameData.taskAnswer, isEnd);
            });
        }

        IsCreateDialog = false;
    }

    private NpcDialogBox CreateDialogBox(GameObject node)
    {
        var dialogBox =
            NpcDialogBox.Create(node,
                new Vector3(0, 1.8f, 0));
        var cam = GameCameraUtils.Inst.GetMainCamera();
        dialogBox.SetCamera(cam);
        dialogBox.transform.SetParent(dialogRoot);
        return dialogBox;
    }

    private void SetDialogBoxParent(GameObject node)
    {
        if (npcDialogBox==null)
        {
            LoggerUtils.LogError($"异常 Npc对话框不存在  挂载node = {node}");
        }  
        var cam = GameCameraUtils.Inst.GetMainCamera();
        npcDialogBox.Axis.Target = node.transform;
        npcDialogBox.SetCamera(cam);
        npcDialogBox.transform.SetParent(dialogRoot);
    }

    /// <summary>
    /// 创建一个显示密码的dialog
    /// </summary>
    private void CreateTaskCodeDialog(string content,bool isEnd)
    {
        // 强制隐藏当前对话气泡
        if (npcDialogBox != null)
        {
            npcDialogBox.ForceHide();
        }

        // 创建新的对话气泡
        if (npcBehaviour != null)
        {
            var npcWrap = npcBehaviour._npcKccCtr.PlayerAnimCtrl.Wrap;
            var npcNode = GameUtils.FindChildByName(npcWrap.Avatar.transform, "dialogpos").gameObject;

            // 创建新的对话框实例
            var newDialogBox = NpcDialogBox.Create(npcNode, new Vector3(0, 2.4f, 0));
            var cam = GameCameraUtils.Inst.GetMainCamera();
            newDialogBox.SetCamera(cam);
            newDialogBox.transform.SetParent(dialogRoot);

            // 显示文本内容
            newDialogBox.SetTextAndSpeak(
                YandereDataManager.Inst.TextAnimDuration,
                content,
                true,   // 需要动画
                true,   // 是新对话的第一句
                isEnd,  // 是否是结束语
                () => {
                    // 文本显示完成后的回调
                    DirectUnlockInput();
                    currentReply = string.Empty;
                    // 设置定时销毁
                    TimerManager.Inst.RunOnce("DestroyTaskDialog", 7f, () => {
                        if (newDialogBox != null)
                        {
                            newDialogBox.Disappear();
                            newDialogBox = null;
                        }
                    });
                }
            );

            // 在聊天界面也显示这段文本
            chatView.SetRecChat(npcBehaviour.GetNpcName(), "6551FF", content, true, true);
        }
    }

    public void WaitNpcTalk(string text)
    {
        _dialogManager.ShowNpcDialog(npcBehaviour, text, false, true, true);
    }

    public void SetNpcTalk(string text, bool isEnd, bool needAni, bool isFirstContent, Action action)
    {
        _dialogManager.ShowNpcDialog(npcBehaviour, text, isEnd, needAni, isFirstContent, action);
    }

    public void SetNpcDialogLocalPos(Vector3 offset)
    {
        if (npcDialogBox != null)
        {
            npcDialogBox.SetLocalPos(offset);
        }
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

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener<bool>(MessageName.UICameraMode, OnCameraMode);
        MessageHelper.RemoveListener<string, string>(MessageName.OnS9ChatEmoteClick, OnKeyboardEmoteClick);
        MessageHelper.RemoveListener<bool>(MessageName.BuddyLinkEmoteStateChange, OnBuddyLinkEmoteStateChange);
        MessageHelper.RemoveListener<string, string, bool>(MessageName.OnS9EmoteWithMsg, SendInputByEmoteEvent);
        MessageHelper.RemoveListener<AIGameHospitalConfig.EGuideAction>(MessageName.OnS9GuideStepAction, OnPgcGuideAction);

        emoBtn.onClick.RemoveAllListeners();
        editAvatarBtn.onClick.RemoveAllListeners();
        roomMenuBtn.onClick.RemoveAllListeners();
        chatMessageBtn.onClick.RemoveAllListeners();
        retryBtn.onClick.RemoveAllListeners();
        dialogBtn.onClick.RemoveAllListeners();
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
        AIHospitalAvatarManager.Inst.Release();
        
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
        AIHospitalAvatarManager.Inst.OnEnterFittingRoom();
    }

    private void OnCloseFittingRoom(BaseAvatarData baseAvatarData)
    {
        if (baseAvatarData == null) 
        {
            baseAvatarData = AvatarDataManager.Inst.SelfCharacterData;
        }

        if (baseAvatarData is CharacterData characterData)
        {
            AIHospitalAvatarManager.Inst.OnExitFittingRoom(characterData);
            // 确保UI状态正确恢复
            UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestChangeOcBtn, AbilityKey.CameraMode);
        }
        else if (baseAvatarData is PetData petData)
        {
            AIHospitalAvatarManager.Inst.OnExitPetFittingRoom(petData);
        }
    }

    private void OnChangeOcClick()
    {
        UIManager.Inst.OpenPanelTakeAni<OcChangePanel>(PanelId.OcChangePanel, OcChangeScene.Play).OnCloseAction = OnCloseFittingRoom;
        AIHospitalAvatarManager.Inst.OnEnterFittingRoom();
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

    #region UI相关方法
    private void OnBtnExitClick()
    {
        CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetThemeColor("#68CCBE", "#905CFF", "#68D896");
        commonConfirmPanel.SetLocalText("退出游戏", "你确定要中途退出游戏吗？\n当前的游戏进度会被清空哦！", "退出游戏", "继续游玩");
        commonConfirmPanel.SetOnClickAction(() =>
        {
            aiGame.OnStepChange(S9GameState.Exit);
            ReportGameResult();
            GameController.ExitGame(() =>
            {
                AIGameSoundUtils.Inst.StopAllSound();
                SimpleGameDurationManager.Inst.Release();
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
                UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
                UIManager.Inst.BackToLastWindow();
            });
        }, () =>
        {
            commonConfirmPanel.CloseSelf();
        });
    }


    private void ReportGameResult()
    {
        S9GameReport req = new()
        {
            npcId = string.Empty,
            gameId = (int)PGCGameType.AIHospital,
            duration = (int)(GameUtils.GetTimeStamp() - aiGame.GetGameStartTime()),
            result = (int)0,
            conversationId = string.Empty,
            mapId = aiGame.CurMapID
        };
        var paramStr = JsonConvert.SerializeObject(req);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AIResult, HttpMethod.POST, paramStr, (content) =>
        {
            LoggerUtils.Log("s9游戏结果上报成功");
        }, (err) => { LoggerUtils.Log("s9游戏结果上报失败", err); }, null, 0, 3);
        string localKey = AccountDataManager.Inst.UserInfo.uid + AIGameHospitalConfig.firstPlayMapIDKey;
        if (!IsPgcEnter && string.IsNullOrEmpty(PlayerPrefs.GetString(localKey,string.Empty)))
        {
            PlayerPrefs.SetString(localKey,aiGame.CurMapID);
        }
    }
    #endregion

    #region AI请求
    private void SendMsgToSever(string data, UnityAction<S9AIMessageRsp, S9ChatRsp, bool> onSuccess = null, UnityAction onFail = null)
    {
        

        S9AIMessageReq reqData = new()
        {
            gameId = (int)PGCGameType.AIHospital,
            conversationId = GameAINpcChatManager.Inst.GetNpcConversitionID(_curNpcID),
            gameData = data,
            npcId = IsPgcEnter ? string.Empty : _curNpcID,
            mapId = IsPgcEnter ? string.Empty : aiGame.CurMapID,
            sessionId = GameAINpcChatManager.Inst.GetSessionID()
        };
        var paramStr = JsonConvert.SerializeObject(reqData);
        WaitNpcTalk("......");
        bool isFirst = true;
        //npcBehaviour.StartTalkWithPlayer();
        LoggerUtils.Log($"当前交互对象 {_curNpcID},会话ID = {GameAINpcChatManager.Inst.GetNpcConversitionID(_curNpcID)}");
        NetworkManager.Inst.SendHttpRequestOnStream(HttpUrlDefine.AIChat, HttpMethod.POST, paramStr, (content) =>
        {
            LoggerUtils.Log(content);
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
            var msgDataRsp = JsonConvert.DeserializeObject<S9AIMessageRsp>(resposeData.message);
            var gameDataRsp = JsonConvert.DeserializeObject<S9ChatRsp>(msgDataRsp.gameData);

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
            OnRecvChatRsp(msgDataRsp, gameDataRsp, isEnd);
            
            if (isEnd)
            {
                WaitNpcSpeakByEndStream(gameDataRsp);
            }
            onSuccess?.Invoke(msgDataRsp, gameDataRsp, isEnd);
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

    private void WaitNpcSpeakByEndStream(S9ChatRsp gameDataRsp)
    {
        if (string.IsNullOrEmpty(currentReply))
        {
            DirectUnlockInput();
            if (npcDialogBox != null)
            {
                npcDialogBox.Disappear();
            }
            CompleteDialog(gameDataRsp);
        }
        else
        {
            TimerManager.Inst.RunOnce("FinalContent",YandereDataManager.Inst.TextAnimDuration , () =>
            {
                DirectUnlockInput();
                CompleteDialog(gameDataRsp);
            });
        }
    }

    public void SetDoorProgressVisible(bool isVisible)
    {
        progressNode.SetActive(isVisible);
    }

    private void UpdateNpcData(S9ChatRsp gameData)
    {
        if (gameData.replyIsEnd == 1)
        {

        }
        else if (gameData.replyIsEnd == 2 )
        {
            if (PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + AIGameHospitalConfig.pgcGuideId) < (int)AIGameHospitalConfig.EPgcGuideID.ProgressTips
&& IsPgcEnter)
            {
                UIManager.Inst.OpenPanel<AIHospitalStrongGuide>(PanelId.AIHospitalStrongGuidePanel, AIGameHospitalConfig.EPgcGuideID.ProgressTips);
            }
            AIHospital_CharacterManager.Inst.UpdateNpcData(_curNpcID, gameData.decisionRate);

            if (gameData.code!=0)
            {
                aiGame.Pwd = gameData.code;
            }

            //当前任务完成后防止后面的存在tempConversitionID，强制清理一下
            LoggerUtils.Log($"{_curNpcRoleType} update decisionRate :{gameData.decisionRate} update alert: {gameData.npcAlertness}");
            if (gameData.decisionRate >= AIGameHospitalConfig.maxDecisionNum)
            {
                if (IsPgcEnter)
                {
                    aiGame.UpdatePgcNpcState(_curNpcRoleType,true);
                }
                else
                {
                    aiGame.UpdateUgcNpcState(_curNpcID,true);
                }
            }
            if (gameData.npcAlertness >= AIGameHospitalConfig.maxAlertNum)
            {
                aiGame.OnNpcAlert(_curNpcID);
            }
        }

    }

    private void CompleteDialog(S9ChatRsp gameData)
    {
        if (!gameData.Equals(new S9ChatRsp()))
        {
            YandereDataManager.Inst.IsFirstReply = false;
            var aiGame = AIGameController.Inst.GetCurAIGame<AIHospitalGame>();
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
                heartImg[i].SetActive(hp>i);
            }  
        }
    }
    
    private void OnRoomChatBtnClick() {
        chatView.gameObject.SetActive(!chatView.gameObject.activeSelf);
    }
    #endregion

    #region 退出游戏时处理倒计时
    public void StopTimer()
    {
        LoggerUtils.Log("S9 GameEnd StopAllSound");
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

    private void OnPgcGuideAction(AIGameHospitalConfig.EGuideAction eAction)
    {
        if (IsPgcEnter && eAction == AIGameHospitalConfig.EGuideAction.ShowHeartTips)
        {
            UIManager.Inst.OpenPanel<AIHospitalStrongGuide>(PanelId.AIHospitalStrongGuidePanel, AIGameHospitalConfig.EPgcGuideID.HeartTips);
        }
    }
}


