using System;
using System.Collections.Generic;
using Basic.Extensions;
using BUD.AnimPose;
using DG.Tweening;
using Es;
using EventTracking;
using Game.AIResData;
using Game.Avatar;
using GameData.Account;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.AINPCStudio {
    public class AINpcChatPanel : BasePanel<AINpcChatPanel> {
        private AINpcInfo npcInfo;
        private AIBuddyInfo buddyInfo;
        private string aiBuddyCookie;
        private int aiBuddyIsEnd;
        private string curConversationId;
        private Text aiNameText;
        [SerializeField] private GameObject chatContainer;


        [SerializeField] private Text inputText;
        [SerializeField] private Button inputButton;
        [SerializeField] private GameObject inputArea;
        [SerializeField] private ChatScrollRect scrollRect;
        private ChatScrollRect.ChatItemProxy currentItemProxy;
        [SerializeField] private Button newMessageBtn;
        [SerializeField] private CButton changeCameraBtn;
        [SerializeField] private GameObject[] normalObjList;
        [SerializeField] private GameObject[] focusObjList;
        [SerializeField] private CButton chatBtn;

        private CharacterContainer selfCharacterContainer;
        private CharacterContainer aiCharacterContainer;
        private bool isRequestHistory = false;
        private bool isFirstRequestHistory = true;

        private string InputHandle = "点击输入";
        private bool isLockInput = false;
        private bool isFocus = false;
        private ChatMode chatMode;
        private Action onCloseListener;
        private AIContentData aiResData;

        [SerializeField] private GameObject[] GameCardNodes;
        [SerializeField] private Text[] CardNumTexts;

        public override void OnCreate() {
            base.OnCreate();
            InitUI();
            LoadEvent.ReportTask(152, 0);
        }

        protected override void OnDestroy()
        {
            GameAIBuddyChatManager.Inst.LogAIBuddyChatInfo();
            MessageHelper.Broadcast(MessageName.OnAINpcChatPanelClose);
            onCloseListener?.Invoke();

        }

        public void SetGameCard()
        {
            if (aiResData == null)
            {
                return;
            }

            for (var i = 0; i < GameCardNodes.Length; i++)
            {
                GameCardNodes[i].SetActive( aiResData.free != (int) AIResFreeState.Free);
            }

            for (var i = 0; i < CardNumTexts.Length; i++)
            {
                CardNumTexts[i].SetLocalText("今日剩余次数：{0}", aiResData.remaining);
            }
        }

        public override void OnShow(params object[] args) {
            base.OnShow(args);
            if (args.Length > 0 && args[0] is AINpcInfo aiNpcInfo) {
                npcInfo = aiNpcInfo;
            } else if (args.Length > 0 && args[0] is AIBuddyInfo aiBuddyInfo) {
                buddyInfo = aiBuddyInfo;
            }

            if (npcInfo != null) {
                chatMode = ChatMode.Npc;
                RefreshNpcInfo();
            } else if (buddyInfo != null) {
                chatMode = ChatMode.AIBuddy;
                RefreshAIBuddyInfo();
            }

            inputArea.gameObject.SetActive(true);
            OnChangeCameraBtnClick();
            GetAIChat();
        }

        private void GetAIChat() {
            aiResData = AIResDataManager.Inst.GetAIGameData(AIResType.AIChat);
            if (aiResData == null) {
                LoggerUtils.LogError("无法获取AI聊天信息");
                return;
            }
            SetGameCard();
        }


        private void InitUI() {
            var backBtn = GameObjectEx.FindComponentByName<CButton>(transform, "BackButton");
            var closeBtn_01 =
                GameObjectEx.FindComponentByName<CButton>(transform, "BaseLayout2D/FocusContainer/CloseBtn");
            var closeBtn_02 =
                GameObjectEx.FindComponentByName<CButton>(transform,
                    "BaseLayout2D/NormalContainer/IcnContainer/CloseBtn");
            backBtn.onClick.AddListener(CloseSelf);
            closeBtn_01.onClick.AddListener(CloseSelf);
            closeBtn_02.onClick.AddListener(CloseSelf);
            aiNameText =
                GameObjectEx.FindComponentByName<Text>(transform, "BaseLayout2D/FocusContainer/ChatContainer/AIName");
            scrollRect.onValueChanged.AddListener((itemContent) => {
                if (scrollRect.verticalNormalizedPosition <= 0) {
                    newMessageBtn.gameObject.SetActive(false);
                }
            });
            scrollRect.SetScrollTopCallBack(OnScrollTop);

            newMessageBtn.onClick.AddListener(OnNewMessageButtonClick);
            inputButton.onClick.AddListener(OnInputButtonClick);
            inputText.SetLocalText(InputHandle);
            inputArea.gameObject.SetActive(false);
            chatBtn.onClick.AddListener(OnInputButtonClick);

            selfCharacterContainer = new CharacterContainer(
                GameObjectEx.FindChildByName(transform,
                    "BaseLayout2D/FocusContainer/CameraContainer/SelfCharacterContainer"),
                GameObjectEx.FindChildByName(transform,
                    "BaseLayout2D/NormalContainer/CameraContainer/SelfCharacterContainer"),
                GameObjectEx.FindChildByName(transform, "BaseLayout3D/SelfPreviewPlayer/CharacterRoot"), true);
            selfCharacterContainer.SetName(AccountDataManager.Inst.UserInfo.nickname);
            selfCharacterContainer.SetCharacterData(AccountDataManager.Inst.UserInfo.avatarInfo.Clone());
            aiCharacterContainer = new CharacterContainer(
                GameObjectEx.FindChildByName(transform,
                    "BaseLayout2D/FocusContainer/CameraContainer/AICharacterContainer"),
                GameObjectEx.FindChildByName(transform,
                    "BaseLayout2D/NormalContainer/CameraContainer/AICharacterContainer"),
                GameObjectEx.FindChildByName(transform, "BaseLayout3D/AIPreviewPlayer/CharacterRoot"));
            changeCameraBtn.onClick.AddListener(OnChangeCameraBtnClick);
        }

        public void AddCloseListener(Action callback) {
            onCloseListener += callback;
        }

        public void RemoveCloseListener(Action callback) {
            onCloseListener += callback;
        }


        private void OnScrollTop() {
            if (chatMode != ChatMode.AIBuddy) {
                return;
            }

            if (aiBuddyIsEnd == 1) {
                return;
            }

            if (isRequestHistory) {
                return;
            }

            isRequestHistory = true;

            var req = new AIBuddyChatHistoryReq() {
                conversationId = buddyInfo.conversationId,
                cookie = aiBuddyCookie,
            };

            NetworkManager.Inst.SendHttpRequest<AIBuddyChatHistoryRsp>(HttpUrlDefine.AIBuddyChatHistory, HttpMethod.GET,
                req,
                rsp => {
                    if (this == null || gameObject == null) {
                        return;
                    }



                    isRequestHistory = false;
                    // 服务器返回空，给默认值
                    rsp ??= new AIBuddyChatHistoryRsp()
                    {
                        cookie = null,
                        isEnd = 1,
                        messages = new List<AIBuddyChatMessage>()
                    };

                    aiBuddyCookie = rsp.cookie;
                    aiBuddyIsEnd = rsp.isEnd;
                    if (rsp.messages != null && rsp.messages.Count > 0) {
                        foreach (var chatMessage in rsp.messages) {
                            if (chatMessage.role == 1) {
                                TextChatData textChatData = new TextChatData() {
                                    fromUid = AccountDataManager.Inst.Uid,
                                    toUid = buddyInfo.npc.id,
                                    message = chatMessage.content,
                                    nickName = AccountDataManager.Inst.UserInfo.nickname,
                                    portraitUrl = AccountDataManager.Inst.UserInfo.portraitUrl,
                                    chatBubbles = AccountDataManager.Inst.UserInfo.chatBubbles,
                                    avatarFrame = AccountDataManager.Inst.UserInfo.avatarFrame,
                                };
                                SetSelfContent(JsonConvert.SerializeObject(textChatData), true);
                            } else {

                                if (!string.IsNullOrEmpty(chatMessage?.content)) {
                                    //939090
                                    chatMessage.content = chatMessage.content.Replace("（","<i><c=939090>（");
                                    chatMessage.content = chatMessage.content.Replace("）", "）</c></i>");
                                }
                                SetNpcContent(buddyInfo.npc, chatMessage.content, true);
                            }
                        }
                    } else if (isFirstRequestHistory) {
                        var petPhrase = LocalizationManager.Inst.GetLocalizedText("很高兴认识你");
                        if (buddyInfo.npc.npcPetPhrases != null && buddyInfo.npc.npcPetPhrases.Count > 0) {
                            petPhrase = buddyInfo.npc.npcPetPhrases[0];
                        }
                        SetNpcContent(buddyInfo.npc, petPhrase);
                    }

                    if (isFirstRequestHistory) {
                        if (isFocus) {
                            scrollRect.ProcMovement();
                        }
                    }


                }, errRsp => {
                    isRequestHistory = false;
                    LoggerUtils.LogError("获取数据失败:" + errRsp.rmsg);
                });
        }

        private void OnChangeCameraBtnClick() {
            isFocus = !isFocus;
            selfCharacterContainer.SetFocus(isFocus);
            aiCharacterContainer.SetFocus(isFocus);
            foreach (var normalObj in normalObjList) {
                var canvasGroup = normalObj.GetComponent<CanvasGroup>();
                if (canvasGroup != null) {
                    canvasGroup.alpha = isFocus ? 0 : 1;
                }

                normalObj.SetActive(!isFocus);
            }

            foreach (var focusObj in focusObjList) {
                var canvasGroup = focusObj.GetComponent<CanvasGroup>();
                if (canvasGroup != null) {
                    canvasGroup.alpha = isFocus ? 1 : 0;
                }
            }

            if (isFocus) {
                scrollRect.ProcMovement();
            }

        }

        private void UpdateAIResData(int buyCount) {
            if (aiResData != null) {
                aiResData.free = (int)AIResFreeState.Buy;
                aiResData.remaining = buyCount;
                SetGameCard();
            }
        }

        private void OnInputButtonClick() {
            if (isLockInput) {
                return;
            }

            if (aiResData == null) {
                LoggerUtils.LogError("无法获取AI聊天信息 OnInputButtonClick");
                return;
            }

            if (aiResData != null && aiResData.remaining <= 0) {
                var aiPanel =
                    UIManager.Inst.OpenPanel<AIBuyResourcePanel>(PanelId.AIBuyResourcePanel, (int)AIResType.AIChat);
                aiPanel.SetOnBuySuccessAct(UpdateAIResData,UpdateFreeByNewDay);
                return;
            }

            isLockInput = true;
            KeyBoardInfo keyBoardInfo = new KeyBoardInfo {
                type = 0,
                placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入文字"),
                inputMode = 2,
                maxLength = 250,
                inputFlag = 0,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("不能超过250字符"),
                defaultText = "",
                source = Base.GameController.IsInHallScene()? (int)KeyboardSource.HallChat:(int)KeyboardSource.RoomChat,
                returnKeyType = (int)ReturnType.Send
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnShowKeyBoard);
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.hideKeyboard, OnHideKeyBoard);
            MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
        }

        private void UpdateFreeByNewDay()
        {
            AIResDataManager.Inst.GetNetworkAIResData(GetAIChat);
        }


        private void OnNewMessageButtonClick() {
        }

        #region BuddyChat

        private void RefreshAIBuddyInfo() {
            curConversationId = buddyInfo.conversationId;
            aiNameText.SetText(buddyInfo.npc.npcName);
            aiCharacterContainer.SetName(buddyInfo.npc.npcName);
            aiCharacterContainer.SetCharacterData(buddyInfo.npc.npcAvatarJson);
            OnScrollTop();
        }

        private void SendAIBuddyChat(string input = null) {
            AIBuddyChatReq reqData = new AIBuddyChatReq() {
                id = buddyInfo.id,
                message = new AIBuddyChatMessage() {
                    role = 1,
                    content = input,
                }
            };

            currentItemProxy = null;
            aiCharacterContainer.SetWaiting();
            string allContent = null;
            bool isAllEnd = false;
            bool isFirstContent = true;
            isLockInput = true;
            NetworkManager.Inst.SendHttpRequestOnStream(HttpUrlDefine.AIBuddyChat, HttpMethod.POST,
                JsonConvert.SerializeObject(reqData), (content) => {
                    if (this == null || gameObject == null) {
                        return;
                    }
                    if (string.IsNullOrEmpty(content)) {
                        isLockInput = false;
                        return;
                    }

                    var responseData = JsonConvert.DeserializeObject<AIResposeData>(content);
                    if (string.IsNullOrEmpty(responseData?.message)) {
                        isLockInput = false;
                        return;
                    }


                    bool isEnd = responseData.isEnd == 1;
                    if (string.IsNullOrEmpty(responseData?.message)) {
                        isLockInput = false;
                        return;
                    }


                    var msgDataRsp = JsonConvert.DeserializeObject<AIBuddyChatMessage>(responseData.message);

                    if (isFirstContent) {
                        isFirstContent = false;
                        if (msgDataRsp != null) {
                            UpdateRemainintCardCount(msgDataRsp.remainingChatCnt);
                        }
                    }

                    if (!string.IsNullOrEmpty(msgDataRsp?.content)) {
                        msgDataRsp.content = msgDataRsp.content.Replace("（","<i><c=939090>（");
                        msgDataRsp.content = msgDataRsp.content.Replace("）", "）</c></i>");
                    }


                    OnReceiveChatRsp(msgDataRsp, isEnd);
                    allContent += msgDataRsp.content;
                    if (isEnd) {
                        GameAIBuddyChatManager.Inst.OnAIBuddyProfilePageBuddyRsp(buddyInfo?.npc?.id);
                        isAllEnd = true;
                        isLockInput = false;
                        SetNpcContent(buddyInfo.npc, allContent);
                        aiCharacterContainer.HideWaiting();
                        aiCharacterContainer.DelayHideChat();
                    }
                }, () => {
                    isLockInput = false;
                    aiCharacterContainer.HideWaiting();
                    if (!isAllEnd && !string.IsNullOrEmpty(allContent)) {
                        SetNpcContent(npcInfo, allContent);
                        aiCharacterContainer.HideWaiting();
                    }
                });
        }

        private void OnReceiveChatRsp(AIBuddyChatMessage chatRsp, bool isEnd) {
            if (chatRsp == null) {
                return;
            }


            if (!string.IsNullOrEmpty(chatRsp.pgcEmote)) {
                aiCharacterContainer.PlayPGCEmote(chatRsp.pgcEmote);
                return;
            }

            if (!string.IsNullOrEmpty(chatRsp.ugcEmote)) {
                aiCharacterContainer.PlayUGCEmote(chatRsp.ugcEmote);
                return;
            }


            if (string.IsNullOrEmpty((chatRsp.content))) {
                return;
            }

            aiCharacterContainer.AddChat(chatRsp.content, false);
            selfCharacterContainer.HideChat();
        }

        #endregion


        #region NPCChat

        private void RefreshNpcInfo() {
            if (npcInfo == null) {
                return;
            }

            aiNameText.SetText(npcInfo.npcName);
            aiCharacterContainer.SetName(npcInfo.npcName);
            aiCharacterContainer.SetCharacterData(npcInfo.npcAvatarJson);
            aiCharacterContainer.SetNpcInfo(npcInfo);
            var petPhrase = LocalizationManager.Inst.GetLocalizedText("很高兴认识你");
            if (npcInfo.npcPetPhrases != null && npcInfo.npcPetPhrases.Count > 0) {
                petPhrase = npcInfo.npcPetPhrases[0];
            }
            SetNpcContent(npcInfo, petPhrase);
        }

        private void UpdateRemainintCardCount(int remainingChatCnt) {
            if (aiResData != null) {
                aiResData.remaining = remainingChatCnt;
                SetGameCard();
            }
        }

        private void SendNpcChat(string input = null) {
            AINpcChatReq reqData = new AINpcChatReq() {
                npcId = npcInfo.id,
                conversationId = curConversationId,
                query = input,
            };
            currentItemProxy = null;
            aiCharacterContainer.SetWaiting();
            string allContent = null;
            bool isAllEnd = false;
            bool isFirstContent = true;
            isLockInput = true;
            NetworkManager.Inst.SendHttpRequestOnStream(HttpUrlDefine.AIUIChat, HttpMethod.POST,
                JsonConvert.SerializeObject(reqData), (content) => {
                    if (this == null || gameObject == null) {
                        return;
                    }
                    if (string.IsNullOrEmpty(content)) {
                        isLockInput = false;
                        return;
                    }

                    var responseData = JsonConvert.DeserializeObject<AIResposeData>(content);
                    if (string.IsNullOrEmpty(responseData?.message)) {
                        isLockInput = false;
                        return;
                    }

                    bool isEnd = responseData.isEnd == 1;
                    if (string.IsNullOrEmpty(responseData?.message)) {
                        isLockInput = false;
                        return;
                    }


                    var msgDataRsp = JsonConvert.DeserializeObject<AINpcChatRsp>(responseData.message);

                    if (isFirstContent) {
                        isFirstContent = false;
                        if (msgDataRsp != null) {
                            UpdateRemainintCardCount(msgDataRsp.remainingChatCnt);
                        }
                    }

                    if (!string.IsNullOrEmpty(msgDataRsp?.reply)) {
                        msgDataRsp.reply = msgDataRsp.reply.Replace("（","<i><c=939090>（");
                        msgDataRsp.reply = msgDataRsp.reply.Replace("）", "）</c></i>");
                    }
                    OnReceiveChatRsp(msgDataRsp, isEnd);
                    allContent += msgDataRsp.reply;
                    if (isEnd) {
                        GameAIBuddyChatManager.Inst.OnAIBuddyNpcStoreBuddyRsp(npcInfo.id);
                        isAllEnd = true;
                        isLockInput = false;
                        SetNpcContent(npcInfo, allContent);
                        aiCharacterContainer.DelayHideChat();
                    }
                }, () => {
                    isLockInput = false;
                    aiCharacterContainer.HideWaiting();
                    if (!isAllEnd && !string.IsNullOrEmpty(allContent)) {
                        SetNpcContent(npcInfo, allContent);
                        aiCharacterContainer.DelayHideChat();
                    }
                });
        }

        #endregion


        private void OnReceiveChatRsp(AINpcChatRsp chatRsp, bool isEnd) {
            if (string.IsNullOrEmpty(curConversationId)) {
                curConversationId = chatRsp.conversationId;
            }

            aiCharacterContainer.HideWaiting();

            if (!string.IsNullOrEmpty(chatRsp.pgcEmote)) {
                aiCharacterContainer.PlayPGCEmote(chatRsp.pgcEmote);
                return;
            }

            if (!string.IsNullOrEmpty(chatRsp.ugcEmote)) {
                aiCharacterContainer.PlayUGCEmote(chatRsp.ugcEmote);
                return;
            }


            if (string.IsNullOrEmpty((chatRsp.reply))) {
                return;
            }

            selfCharacterContainer.HideChat();
            aiCharacterContainer.AddChat(chatRsp.reply, false);
        }

        private void OnHideKeyBoard(string str) {
            isLockInput = false;
        }

        private void OnShowKeyBoard(string str) {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            if (string.IsNullOrEmpty(str)) {
                isLockInput = false;
                return;
            }

            inputText.SetLocalText(InputHandle);
            if (str.Contains("（") && str.Contains("）"))
            {
                string pattern = "（.*?）";
                str = System.Text.RegularExpressions.Regex.Replace(str, pattern,
                    match => $"<i><c=939090>{match.Value}</c></i>");
            }

            TextChatData textChatData = new TextChatData() {
                fromUid = AccountDataManager.Inst.Uid,
                toUid = chatMode == ChatMode.Npc ? npcInfo.id : buddyInfo.uid,
                message = str,
                nickName = AccountDataManager.Inst.UserInfo.nickname,
                portraitUrl = AccountDataManager.Inst.UserInfo.portraitUrl,
                chatBubbles = AccountDataManager.Inst.UserInfo.chatBubbles,
                avatarFrame = AccountDataManager.Inst.UserInfo.avatarFrame,
            };

            if (!isFocus) {
                selfCharacterContainer.SetChat(str);
                aiCharacterContainer.HideChat();
            }

            SetSelfContent(JsonConvert.SerializeObject(textChatData));
            if (chatMode == ChatMode.Npc) {
                SendNpcChat(str);
            } else if (chatMode == ChatMode.AIBuddy) {
                SendAIBuddyChat(str);
            }
        }

        private void SetSelfContent(string str, bool isInsert = false) {

            str = str.Replace("<i><c=939090>","<i><c=E0E0E0>");

            string showName = AccountDataManager.Inst.UserInfo.nickname;
            OfflineMessageItem selfChatData = new OfflineMessageItem() {
                uid = AccountDataManager.Inst.Uid,
                nickname = showName,
                portraitUrl = AccountDataManager.Inst.UserInfo.portraitUrl,
                data = str,
                chatBubbles = AccountDataManager.Inst.UserInfo.chatBubbles,
                avatarFrame = AccountDataManager.Inst.UserInfo.avatarFrame,
            };
            if (isInsert) {
                scrollRect.InsertMsg(selfChatData);
            } else {
                scrollRect.AddMsg(selfChatData);
                if (isFocus) {
                    scrollRect.ProcMovement();
                }
            }
        }

        private ChatScrollRect.ChatItemProxy SetNpcContent(AINpcInfo info, string str, bool isInsert = false) {
            if (string.IsNullOrEmpty(str)) {
                return null;
            }
            TextChatData textChatData = new TextChatData() {
                fromUid = info.id,
                toUid = AccountDataManager.Inst.UserInfo.uid,
                message = str,
                nickName = info.npcName,
                portraitUrl = info.npcPortraitUrl,
                chatBubbles = 0,
                avatarFrame = 0,
            };
            OfflineMessageItem aiChatData = new OfflineMessageItem() {
                uid = info.id,
                nickname = info.npcName,
                portraitUrl = info.npcPortraitUrl,
                data = JsonConvert.SerializeObject(textChatData),
                chatBubbles = 0,
                avatarFrame = 0,
            };
            ChatScrollRect.ChatItemProxy chatItemProxy;
            if (isInsert) {
                chatItemProxy = scrollRect.InsertMsg(aiChatData);
            } else {
                chatItemProxy = scrollRect.AddMsg(aiChatData);
                if (isFocus) {
                    scrollRect.ProcMovement();
                }
            }

            return chatItemProxy;
        }

        private class AIBuddyChatMessage {
            public string content;

            // 1 为自己， 2 为 aiBuddy
            public int role;
            public string pgcEmote;
            public string ugcEmote;
            public int remainingChatCnt;
        }

        private class AIBuddyChatReq {
            public string id;
            public AIBuddyChatMessage message;
        }


        private class AIBuddyChatRsp {
            public AIBuddyChatMessage message;
            public string cookie;
            public string sender;
            public int isEnd;
        }

        private class AIBuddyChatHistoryReq {
            public string conversationId;
            public string cookie;
        }

        private class AIBuddyChatHistoryRsp {
            public List<AIBuddyChatMessage> messages;
            public string cookie;
            public int isEnd;
        }


        private class AINpcChatReq {
            public string conversationId;
            public string npcId;
            public string query;
        }

        private class AINpcChatRsp {
            public string conversationId;
            public string reply;
            public string pgcEmote;
            public string ugcEmote;
            public int remainingChatCnt;
        }

        private enum ChatMode {
            ErrMode,
            Npc,
            AIBuddy,
        }

        private class CharacterContainer {
            private readonly Transform characterRoot;
            private CharacterWrap characterWrap;
            private PlayerAnimationCtrl animationCtrl;
            private AnimIKController animationCtrlIK;
            private PlayerHoldBehaviour playerHold;

            private SuperTextMesh chatText => isCameraFocus ? focusChatText : normalChatText;
            private SuperTextMesh focusChatText;
            private SuperTextMesh normalChatText;
            private bool isShowChat => chatText.transform.parent.gameObject.activeSelf;
            private string lastContent;
            private Camera camera;
            private bool isSelf;
            private bool isCameraFocus = false;
            private bool isShowWaiting = false;
            private PgcNpcIdleBehaviour pgcIdleBehaviour;
            private UgcNpcIdleBehaviour ugcIdleBehaviour;
            private AINpcInfo npcInfo;

            public CharacterContainer(Transform focusUIRoot, Transform normalUIRoot, Transform characterRoot,
                bool isSelf = false) {
                this.isSelf = isSelf;
                this.characterRoot = characterRoot;
                focusChatText = GameObjectEx.FindComponentByName<SuperTextMesh>(focusUIRoot, "Chat/Text");
                normalChatText = GameObjectEx.FindComponentByName<SuperTextMesh>(normalUIRoot, "Chat/Text");
                camera = characterRoot.parent.GetComponentInChildren<Camera>();
            }

            public void SetCharacterData(CharacterData characterData) {
                if (characterWrap == null) {
                    characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(characterData, characterRoot);
                } else {
                    characterWrap.SetCharacterData(characterData);
                }

                animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                animationCtrlIK = characterWrap.Avatar.GetComponent<AnimIKController>();
                playerHold = characterWrap.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();

                pgcIdleBehaviour = characterWrap.Avatar.AddComponent<PgcNpcIdleBehaviour>();
                pgcIdleBehaviour.Init(animationCtrl, true);

                ugcIdleBehaviour = characterWrap.Avatar.AddComponent<UgcNpcIdleBehaviour>();
                var playerIkController = characterWrap.Avatar.GetComponent<AnimIKController>();
                ugcIdleBehaviour.Init(playerIkController);
            }

            public void AddChat(string newContent, bool isEffect = true) {
                if (isCameraFocus && isSelf) {
                    return;
                }

                chatText.StopAllCoroutines();
                ShowChat();

                if (string.IsNullOrEmpty(lastContent)) {
                    chatText.DOKill();
                }

                isShowWaiting = false;
                lastContent += newContent;
                var content = lastContent;
                float width = 613;
                float minHeight = 54;
                if (!isEffect) {
                    chatText.DOKill(false);
                    chatText.SetText(content);
                    var preferredHeight = Mathf.Max(chatText.preferredHeight, minHeight);
                    chatText.GetComponent<RectTransform>().sizeDelta = new Vector2(width, preferredHeight);
                    return;
                }

                if (content == null) {
                    return;
                }

                content = content.Replace(" ", "\u00A0");
                chatText.SetPreferredSize();
                chatText.DOText(content, YandereDataManager.Inst.TextAnimDuration).SetEase(Ease.Linear).OnUpdate(
                    () => {
                        var preferredHeight = Mathf.Max(chatText.preferredHeight, minHeight);
                        chatText.GetComponent<RectTransform>().sizeDelta = new Vector2(width, preferredHeight);
                    });
            }

            public string GetContent() {
                return lastContent;
            }


            public void SetChat(string content) {
                if (isCameraFocus && isSelf) {
                    return;
                }

                chatText.StopAllCoroutines();
                ShowChat();

                isShowWaiting = false;
                lastContent = content;
                float width = 613;
                float minHeight = 54;
                chatText.DOKill(false);
                chatText.SetText(content);
                var preferredHeight = Mathf.Max(chatText.preferredHeight, minHeight);
                chatText.GetComponent<RectTransform>().sizeDelta = new Vector2(width, preferredHeight);
                DelayHideChat();
            }


            public void DelayHideChat() {
                if (!isShowChat) {
                    return;
                }

                chatText.SetTimeCallBack(7, () => {
                    if (isShowChat && chatText != null) {
                        chatText.transform.parent.gameObject.SetActive(false);
                    }
                });
            }

            public void HideChat() {
                if (!isShowChat) {
                    return;
                }

                if (chatText != null) {
                    chatText.transform.parent.gameObject.SetActive(false);
                }
            }


            private void ShowChat(float delay = 0) {
                if (!isShowChat) {
                    chatText.transform.parent.gameObject.SetActive(true);
                    chatText.transform.parent.localScale = Vector3.zero;
                    var tween = chatText.transform.parent.DOScale(Vector3.one, 0.5f);
                    if (delay > float.Epsilon) {
                        tween.SetDelay(delay);
                    }
                }
            }

            public void SetWaiting() {
                ShowChat(0.7f);
                isShowWaiting = true;
                lastContent = "";
                chatText.SetText("");
                chatText.DOKill();
                chatText.DOText("......", 2).SetLoops(-1, LoopType.Restart);
                float width = 613;
                float minHeight = 54;
                chatText.GetComponent<RectTransform>().sizeDelta = new Vector2(width, minHeight);
            }

            public void HideWaiting() {
                if (isShowWaiting) {
                    isShowWaiting = false;
                    chatText.DOKill();
                    chatText.transform.parent.gameObject.SetActive(false);
                }
            }


            public void PlayPGCEmote(string emoteId) {
                CancelEmote();
                animationCtrl.PlaySingleEmoteForUICharacter(emoteId);
            }

            public void PlayUGCEmote(string emoteId) {
                CancelEmote();


                JObject req = new JObject() {
                    ["id"] = emoteId,
                };
                NetworkManager.Inst.SendHttpRequest<DetailRsp>(HttpUrlDefine.getAnimInfo, HttpMethod.GET, req,
                    (rsp) => {
                        if (rsp != null && rsp.animInfo != null) {
                            animationCtrlIK.Play(rsp.animInfo, null);
                        }
                    }, null);
            }


            private void CancelEmote() {
                animationCtrl.ResetEmoteForUICharacter();
                animationCtrlIK.StopAnimAndResetJointNode();
                animationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
                ResetIKPosition();
            }


            private void ChangeAnim() {
                if (npcInfo == null) {
                    return;
                }

                bool isPgcRes = npcInfo.animResType == (int)AnimResType.PGC;
                var ikController = characterWrap.Avatar.GetComponent<AnimIKController>();
                ikController.ChangeAnimResType(isPgcRes ? AnimResType.PGC : AnimResType.UGC);
                ikController.RemovePropIks();
                if (isPgcRes) {
                    pgcIdleBehaviour.SetData(npcInfo.npcAnimations);
                    pgcIdleBehaviour.PlayMainAnim();
                } else {
                    ugcIdleBehaviour.SetData(npcInfo.npcAnimations);
                    ugcIdleBehaviour.PlayMainAnim();
                }

                SetNpcEmoType(AINpcAnimType.Idle);
            }

            private void SetNpcEmoType(AINpcAnimType npcType) {

            }


            private void ResetIKPosition() {
                var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
                animationCtrlIK.transform.localPosition = poseModeData.RoleDefPos[0];
                animationCtrlIK.transform.localEulerAngles = Vector3.zero;
                animationCtrlIK.transform.localScale = Vector3.one;
                animationCtrlIK.transform.parent.localPosition = poseModeData.EditPos[0];
                animationCtrlIK.transform.parent.localEulerAngles = Vector3.zero;
                animationCtrlIK.transform.parent.localScale = Vector3.one;
            }


            public void SetFocus(bool isFocus) {
                isCameraFocus = isFocus;
                if (isFocus) {
                    camera.transform.localPosition =
                        isSelf ? new Vector3(0, -2901.5f, -19) : new Vector3(0, -2902.5f, -19);
                    camera.fieldOfView = isSelf ? 46 : 32;
                } else {
                    camera.transform.localPosition = new Vector3(0, -2901.5f, -19);
                    camera.fieldOfView = 32;
                }
            }

            public void SetNpcInfo(AINpcInfo info) {
                npcInfo = info;
            }

            public void SetCharacterData(string avatarJson) {
                SetCharacterData(CharacterData.DeserializeObject(avatarJson));
            }


            public void SetName(string name) {
            }
        }
    }
}
