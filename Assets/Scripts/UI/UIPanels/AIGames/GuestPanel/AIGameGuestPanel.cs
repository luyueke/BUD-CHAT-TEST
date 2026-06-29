using System;
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
using AIGame.Base;
using Basic.Utils;
using DG.Tweening;
using Game.AIResData;
using GameData.BaseInfo;
using Game.Avatar;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using Game.Utils;
using GameData.Manager;
using Network;
using Network.Http;
using UI.Avatar;
using UI.UIPanels.FittingRoom;
using UnityEngine.Events;

public class AIGameGuestPanel : BaseGamePlayPanel<AIGameGuestPanel>
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
    [SerializeField] private CButton fpsBtn;
    [SerializeField] private CButton tpsBtn;
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
    [SerializeField] private Slider doorProgress;
    public CButton Btn_BadEnd_1;
    public CButton Btn_BadEnd_2;
    public CButton Btn_BadEnd_3;
    public CButton Btn_GoodEnd_1;
    public CButton Btn_GoodEnd_2;
    public CButton Btn_OpenMainDoor;

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
    private AIYandereCharacterBehaviour npcBehaviour;
    private bool _aiReqIsEnd = false;
    private bool isShowDialog = true;
    public bool IsCreateDialog { get; set; }
    
    private int currentNpcLocation = (int)YandereAreaType.LivingRoom;

    private AIContentData aiResData;
    public int ChatCount { get;private set; } = 0; //对话次数

    public override void OnCreate()
    {
        base.OnCreate();
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
        MessageHelper.AddListener<bool>(MessageName.UICameraMode, OnCameraMode);
        InitCameraBindComponent();
        AddTestButtonListener();
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

        GetAIGameData();
    }

    private void GetAIGameData()
    {
        aiResData = AIResDataManager.Inst.GetAIGameData(AIResType.AIYandere);
        if (aiResData == null)
        {
            LoggerUtils.LogError("无法获取AI游戏对局信息");
            return;
        }
    }

    public void LockInput()
    {
        lockInput = true;
    }

    public void SetScreenEffect(bool visible)
    {
        screenEffect.SetActive(visible);
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
        leftTopRoot.SetActive(!enterCameraMode);
        rightHideRoot.SetActive(!enterCameraMode);
        chatView.gameObject.SetActive(!enterCameraMode);
    }

    private void AddTestButtonListener()
    {
        Btn_BadEnd_1.onClick.AddListener(() =>
        {
            // npcBehaviour.PlayAnim(MoodOption.Happy);
            YandereDataManager.Inst.SetResult((int)YandereEndState.OpenDoor);
            var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
            aiGame.GameStepCheck();
            
            // var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
            // aiGame.OnStepChange(YandereStep.BadEnd_1);
        });
        Btn_BadEnd_2.onClick.AddListener(() =>
        {
            YandereDataManager.Inst.SetResult((int)YandereEndState.GoodEnd_2);
            var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
            aiGame.GameStepCheck();
            // npcBehaviour.PlayAnim(MoodOption.Sad);
            // var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
            // aiGame.OnStepChange(YandereStep.BadEnd_2);
        });
        Btn_BadEnd_3.onClick.AddListener(() =>
        {
            // npcBehaviour.PlayAnim(MoodOption.Angry);
            // var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
            // aiGame.OnStepChange(YandereStep.BadEnd_3);
        });
        Btn_GoodEnd_1.onClick.AddListener(() =>
        {
            npcBehaviour.PlayAnimAndStopSpeak(MoodOption.Exasperated);
            // var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
            // aiGame.OnStepChange(YandereStep.GoodEnd_1);
        });
        Btn_GoodEnd_2.onClick.AddListener(() =>
        {
            npcBehaviour.PlayAnimAndStopSpeak(MoodOption.Surprised);
            // var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
            // aiGame.OnStepChange(YandereStep.GoodEnd_2);
        });
        Btn_OpenMainDoor.onClick.AddListener(() =>
        {
            npcBehaviour.PlayAnimAndStopSpeak(MoodOption.Nock);
            // var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
            // aiGame.OpenMainDoor();
        });
    }
    
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        var mpManager = GlobalNodeManager.Inst.Get<AIYandereCharacterManager>();
        npcBehaviour = mpManager.GetNpcBev();
        UIManager.Inst.AddClosePanelAction(OnClosePanelAction);
    }


    public void ChangeTarget()
    {
        defTarget.SetActive(false);
        openDoorTarget.SetActive(true);
    }



    public void StartNpcFirstTalk()
    {
        SetDialogVisible(true);
        npcBehaviour.PlayIdleAnim();
        var npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
        //官方NPC
        string firstTalk = string.Empty;
        if (IsPgcEnter)
        {
            firstTalk = "谁给你打电话？是不是喊你出去玩？我跟你说我现在心情不好，你今天哪也不许去必须在家陪我！";
        }
        else
        {
            firstTalk = "你终于醒啦，外面很危险，我们千万不要出去。";
        }
        lockInput = true;
        npcBehaviour.PlaySpeakAnim();
        SetNpcTalk(firstTalk, true, true, true);
        chatView.SetRecChat(npcInfo.name, "6551FF", firstTalk, true, true);
        TimerManager.Inst.RunOnce("ShowTarget", 2.5f, () =>
        {
            if (defTarget == null)
            {
                return;
            }
            DirectUnlockInput();
            defTarget.SetActive(true);
            TargetText1.text = $"游戏目标：说服{npcInfo.name}开门，逃离公寓！";
            TargetText2.text = $"但要先哄{npcInfo.name}开心不然会被暴揍！";
            SetDoorProgressVisible(true);
            chatView.SetRecChat("当前目标", "53FF5A", $"通过聊天说服{npcInfo.name}打开公寓大门", true, false);
        });
    }

    public void OnRestart()
    {
        ShowPanel();
        chatView.ClearAllMessage();
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
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
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
    public void SendInput(string input)
    {
        IsCreateDialog = true;
        YandereDataManager.Inst.IsFirstReply = true; 
        currentReply = string.Empty;

        if (selfDialogBox == null)
        {
            if(AIGameController.Inst.GetCurAIGame<AIParkGame>() != null)
            {
                selfDialogBox = CreateParkSelfDialogBox(selfDialogNode);
            }
            else
            {
                selfDialogBox = CreateSelfDialogBox(selfDialogNode);
            }
        }

        if (input.Contains("（") && input.Contains("）"))
        {
            string pattern = "（.*?）";
            input = System.Text.RegularExpressions.Regex.Replace(input, pattern,
                match => $"<i><c=c4c4c4>{match.Value}</c></i>");
        }

        selfDialogBox.SetText(input);
        chatView.SetRecChat(AccountDataManager.Inst.UserInfo.nickname, "FFD15B", input, true, false);
        
        var chatData = new AIRequestChatData()
        {
            content = input
        };
        string gameData = JsonConvert.SerializeObject(chatData);
        if (npcDialogBox != null)
        {
            npcDialogBox.ForceHide();
        }
        SendMsgToSever(gameData,null, null);
    }

    private void WaitStartFollowPlayer()
    {
        if (currentNpcLocation == (int) YandereAreaType.SitSofa)
        {
            return;
        }

        if (npcBehaviour != null)
        {
            npcBehaviour.WaitStartFollowPlayer();
        }
    }
    private void PlayNpcAnim()
    {
        var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
        if (aiGame == null)
        {
            return;
        }
        var isGameOver = aiGame.IsGameOver();
        if (!isGameOver)
        {
            npcBehaviour.StopFollowPlayer();
            var rep = YandereDataManager.Inst.GetCurData();
            
            if (rep.npcLocation > 0 && currentNpcLocation != rep.npcLocation)
            {
                currentNpcLocation = rep.npcLocation;
                HandleNpcMoveCheck((YandereAreaType)rep.npcLocation);
                return;
            }

            if (currentNpcLocation == (int)YandereAreaType.SitSofa)
            {
                return;
            }

            if (rep.moodOption == 0)
            {
                npcBehaviour.PlaySpeakAnim();
            }
            else if (rep.moodOption > 0)
            {
                npcBehaviour.PlayAnimAndStopSpeak((MoodOption) rep.moodOption);
            }
        }
    }

    private void OnRecvChatRsp(AIResposeMessageData msgData, AIResposeChatData gameData, bool isEnd)
    {
        curConversationId = msgData.conversationId;
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
            var npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
            chatView.SetRecChat(npcInfo.name, "6551FF", str,  IsCreateDialog, true);
            SetNpcTalk(str, isEnd, true,true);
        }
        IsCreateDialog = false;
    }

    public void HandleNpcMoveCheck(YandereAreaType moveArea = YandereAreaType.Unknown)
    {
        if (moveArea != YandereAreaType.Unknown)
        {
            switch (moveArea)
            {
               case YandereAreaType.SitSofa:
                   var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
                   npcBehaviour.NpcGotoDestinationAndAnim(YandereAreaType.SitSofa,aiGame.CurAreaIndex);
                   break;
               default:
                   npcBehaviour.GoToYanderArea(moveArea);
                   break;
            }
        }
    }
    
    private NpcDialogBox CreateDialogBox(GameObject node)
    {
        var dialogBox =
            NpcDialogBox.Create(node,
                new Vector3(0, 2.4f, 0));
        var cam = GameCameraUtils.Inst.GetMainCamera();
        dialogBox.SetCamera(cam);
        dialogBox.transform.SetParent(dialogRoot);
        return dialogBox;
    }

    private SelfNpcDialogBox CreateParkSelfDialogBox(GameObject node)
    {
         var dialogBox =
            SelfNpcDialogBox.CreatePark(node,
                new Vector3(0, 2.2f, 0));
        var cam = GameCameraUtils.Inst.GetMainCamera();
        dialogBox.SetCamera(cam);
        dialogBox.transform.SetParent(dialogRoot);
        return dialogBox;
    }
    private SelfNpcDialogBox CreateSelfDialogBox(GameObject node)
    {
        var dialogBox =
            SelfNpcDialogBox.Create(node,
                new Vector3(0, 2.2f, 0));
        var cam = GameCameraUtils.Inst.GetMainCamera();
        dialogBox.SetCamera(cam);
        dialogBox.transform.SetParent(dialogRoot);
        return dialogBox;
    }

    public void WaitNpcTalk(string text)
    {
        if (npcDialogBox == null)
        {
            var npcWrap = npcBehaviour.NpcWrap;
            var npcNode = GameUtils.FindChildByName(npcWrap.Avatar.transform, "dialogpos").gameObject;
            npcDialogBox = CreateDialogBox(npcNode);
        }
        npcDialogBox.WaitNpcSpeak(text);
    }


    public void SetNpcTalk(string text,bool isEnd, bool needAni,bool isFirstContent)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        if (npcDialogBox == null)
        {
            var npcWrap = npcBehaviour.NpcWrap;
            var npcNode = GameUtils.FindChildByName(npcWrap.Avatar.transform, "dialogpos").gameObject;
            npcDialogBox = CreateDialogBox(npcNode);
        }
        
        if (IsCreateDialog)
        {
            npcDialogBox.ResetContent();
        }
        npcDialogBox.SetTextAndSpeak(YandereDataManager.Inst.TextAnimDuration, text, needAni,isFirstContent,isEnd, () =>
        {
            npcBehaviour.StopSpeakAnim();
            currentReply = string.Empty;
        });
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
        commonConfirmPanel.SetLocalText("退出游戏", "你确定要中途退出游戏吗？\n当前的游戏进度会被清空哦！", "退出游戏", "继续游玩");
        commonConfirmPanel.SetOnClickAction(() =>
        {
            var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
            aiGame.ReportThinkingData(YandereStep.None);

            GameController.ExitGame(() =>
            {
                AIGameSoundUtils.Inst.StopAllSound();
                YandereDataManager.Inst.IsTryAgain = false;
                SimpleGameDurationManager.Inst.Release();
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
                UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
                UIManager.Inst.ClosePanel(PanelId.AIYandereGameOverPanel);
                UIManager.Inst.BackToLastWindow();
            });
        }, () =>
        {
            commonConfirmPanel.CloseSelf();
        });
    }

    #endregion

    #region AI请求
    public void SendMsgToSeverForOut(string data, UnityAction<AIResposeMessageData, AIResposeChatData, bool> onSuccess = null, UnityAction onFail = null)
    {
        if(lockInput)
            return;

        lockInput = true;
        YandereDataManager.Inst.IsFirstReply = true;
        IsCreateDialog = true;
        currentReply = string.Empty;
        WaitUnLockInput();
        SendMsgToSever(data, onSuccess, onFail);
    }

    private void SendMsgToSever(string data, UnityAction<AIResposeMessageData, AIResposeChatData, bool> onSuccess = null, UnityAction onFail = null)
    {
        var npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
        AIRequestMessageData reqData = new AIRequestMessageData()
        {
            npcId = npcInfo.id,
            gameId = (int)PGCGameType.AIYandere,
            conversationId = curConversationId,
            gameData = data
        };
        npcBehaviour.isNpcSpeak = false;
        var paramStr = JsonConvert.SerializeObject(reqData);
        WaitNpcTalk("......");
        bool isFirst = true;
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
            var msgDataRsp = JsonConvert.DeserializeObject<AIResposeMessageData>(resposeData.message);
            var gameDataRsp = JsonConvert.DeserializeObject<AIResposeChatData>(msgDataRsp.gameData);

            if (isFirst)
            {
                ChatCount++;
                isFirst = false;
                if (aiResData != null)
                {
                    aiResData.remaining = msgDataRsp.remainingChatCnt;
                }
            }

            UpdateNpcData(gameDataRsp);
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
            npcBehaviour.LookAtPlayer();
        }
    }

    private void WaitNpcSpeakByEndStream(AIResposeChatData gameDataRsp)
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

    private void UpdateNpcData(AIResposeChatData gameData)
    {
        if (gameData.replyIsEnd == 1)
        {
            float curValue = gameData.decisionRate / 100.0f;
            doorProgress.DOValue(curValue, 1);
            
            if (currencyState != gameData.currencyState)
            {
                currencyState = gameData.currencyState;
                // var content = YandereDataManager.Inst.GetStateContent(currencyState);
                // if (!string.IsNullOrEmpty(content))
                // {
                //     TipPanel.ShowToast(content);
                // }
            }
            YandereDataManager.Inst.SetFollowPlayer(gameData.followPlayer);
            WaitStartFollowPlayer();
            
            YandereDataManager.Inst.SetResult(gameData.result);
        }
        else if(gameData.replyIsEnd == 2 && YandereDataManager.Inst.IsFirstReply)
        {
            YandereDataManager.Inst.IsFirstReply = false;
            YandereDataManager.Inst.SetNpcOpData(gameData.npcLocation,gameData.moodOption);
            PlayNpcAnim();
        }
       
    }

    private void CompleteDialog(AIResposeChatData gameData)
    {
        if (!gameData.Equals(new AIResposeChatData()))
        {
            YandereDataManager.Inst.IsFirstReply = false;
            var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
            if (aiGame != null)
            {
                aiGame.GameStepCheck();
            }
        }
    }
    #endregion
}
