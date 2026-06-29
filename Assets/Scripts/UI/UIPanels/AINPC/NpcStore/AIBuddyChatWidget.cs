using System;
using System.Collections.Generic;
using Basic.Extensions;
using DG.Tweening;
using GameData.Account;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
public class AIBuddyChatWidget : MonoBehaviour {
        private AIBuddyInfo buddyInfo;
        private string aiBuddyCookie;
        private int aiBuddyIsEnd;
        private Text aiNameText;
        [SerializeField] private GameObject chatContainer;
        [SerializeField] private Text inputText;
        [SerializeField] private Button inputButton;
        [SerializeField] private GameObject inputArea;
        [SerializeField] private ChatScrollRect scrollRect;
        private ChatScrollRect.ChatItemProxy currentItemProxy;
        [SerializeField] private Button newMessageBtn;
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

        public void Init()
        {
            InitUI();
            OnChangeCameraBtnClick();
        }

        public void SetData(AIBuddyInfo info)
        {
            buddyInfo = info;
            RefreshAIBuddyInfo();
            inputArea.gameObject.SetActive(true);
        }

        private void InitUI() {
            aiNameText = GameObjectEx.FindComponentByName<Text>(transform, "FocusContainer/ChatContainer/AIName");
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

            var selfUIRoot = GameObjectEx.FindChildByName(transform, "NormalContainer/CameraContainer/SelfCharacterContainer");
            selfCharacterContainer = new CharacterContainer(selfUIRoot, true);

            var aiUIRoot = GameObjectEx.FindChildByName(transform, "NormalContainer/CameraContainer/AICharacterContainer");
            aiCharacterContainer = new CharacterContainer(aiUIRoot);
        }

        private void OnScrollTop() {
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
                    isRequestHistory = false;
                    if (rsp == null) {
                        aiBuddyCookie = null;
                        aiBuddyIsEnd = 1;
                        return;
                    }
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
                                SetNpcContent(buddyInfo.npc, chatMessage.content, true);
                            }
                        }
                    }

                    if (isFirstRequestHistory) {
                        if (isFocus) {
                            scrollRect.ProcMovement();
                        }
                    }
                    isFirstRequestHistory = false;


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
        }

        private void OnInputButtonClick() {
            if (isLockInput) {
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
                returnKeyType = (int)ReturnType.Send
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnShowKeyBoard);
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.hideKeyboard, OnHideKeyBoard);
            MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
        }

        private void OnNewMessageButtonClick() {
        }

        #region BuddyChat

        private void RefreshAIBuddyInfo() {
            aiNameText.SetText(buddyInfo.npc.npcName);
            OnScrollTop();
        }

        private void SendAIBuddyChat(string input = null) {
            
            GameAIBuddyManager.Inst.ChatToAIBuddyAndBroadcast(input);
            
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
            NetworkManager.Inst.SendHttpRequestOnStream(HttpUrlDefine.AIBuddyChat, HttpMethod.POST,
                JsonConvert.SerializeObject(reqData), (content) => {
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
                    OnReceiveChatRsp(msgDataRsp, isEnd);
                    allContent += msgDataRsp.content;
                    if (isEnd) {
                        isAllEnd = true;
                        isLockInput = false;
                        SetNpcContent(buddyInfo.npc, allContent);
                        aiCharacterContainer.HideWaiting();
                        aiCharacterContainer.DelayHideChat();
                        
                        GameAIBuddyManager.Inst.RcvAIBuddyChatAndBroadcast(buddyInfo, allContent);
                        GameAIBuddyChatManager.Inst.OnAIBuddyGuestSceneBuddyRsp(buddyInfo?.npc?.id);
                    }
                }, () => {
                    isLockInput = false;
                    aiCharacterContainer.HideWaiting();
                    if (!isAllEnd && !string.IsNullOrEmpty(allContent)) {
                        SetNpcContent(buddyInfo.npc, allContent);
                        aiCharacterContainer.HideWaiting();
                    }
                });
        }

        private void OnReceiveChatRsp(AIBuddyChatMessage chatRsp, bool isEnd) {
            if (chatRsp == null) {
                return;
            }
            
            if (string.IsNullOrEmpty((chatRsp.content))) {
                return;
            }

            if (isFocus) {
                return;
            }

            aiCharacterContainer.AddChat(chatRsp.content, false);
        }

        #endregion
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
            TextChatData textChatData = new TextChatData() {
                fromUid = AccountDataManager.Inst.Uid,
                toUid = buddyInfo.uid,
                message = str,
                nickName = AccountDataManager.Inst.UserInfo.nickname,
                portraitUrl = AccountDataManager.Inst.UserInfo.portraitUrl,
                chatBubbles = AccountDataManager.Inst.UserInfo.chatBubbles,
                avatarFrame = AccountDataManager.Inst.UserInfo.avatarFrame,
            };

            if (!isFocus) {
                selfCharacterContainer.SetChat(str);
            }

            SetSelfContent(JsonConvert.SerializeObject(textChatData));
            SendAIBuddyChat(str);
            isLockInput = false;
        }

        private void SetSelfContent(string str, bool isInsert = false) {
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
        }

        private class CharacterContainer {
            private Text chatText;
            private bool isShowChat;
            private string lastContent;
            private Camera camera;
            private bool isSelf;
            private bool isCameraFocus = false;
            private bool isShowWaiting = false;

            public CharacterContainer(Transform normalUIRoot, bool isSelf = false) {
                this.isSelf = isSelf;
                chatText = GameObjectEx.FindComponentByName<Text>(normalUIRoot, "Chat/Text");
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
                    chatText.rectTransform.sizeDelta = new Vector2(width, preferredHeight);
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
                        chatText.rectTransform.sizeDelta = new Vector2(width, preferredHeight);
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
                chatText.rectTransform.sizeDelta = new Vector2(width, preferredHeight);
                DelayHideChat();
            }


            public void DelayHideChat() {
                if (!isShowChat || isCameraFocus) {
                    return;
                }
                chatText.SetTimeCallBack(7, () => {
                    if (isShowChat && chatText != null) {
                        isShowChat = false;
                        chatText.transform.parent.gameObject.SetActive(false);
                    }
                });
            }

            private void ShowChat() {
                if (!isShowChat) {
                    isShowChat = true;
                    chatText.transform.parent.gameObject.SetActive(true);
                    chatText.transform.parent.localScale = Vector3.zero;
                    chatText.transform.parent.DOScale(Vector3.one, 0.5f);
                }
            }

            public void SetWaiting() {
                ShowChat();

                isShowWaiting = true;
                lastContent = "";
                chatText.SetText("");
                chatText.DOKill();
                chatText.DOText("......", 2).SetLoops(-1, LoopType.Restart);
                float width = 613;
                float minHeight = 54;
                chatText.rectTransform.sizeDelta = new Vector2(width, minHeight);
            }

            public void HideWaiting() {
                if (isShowWaiting) {
                    isShowWaiting = false;
                    chatText.DOKill();
                    isShowChat = false;
                    chatText.transform.parent.gameObject.SetActive(false);
                }
            }

            public void SetFocus(bool isFocus) {
                isCameraFocus = isFocus;
            }
        }
    }
