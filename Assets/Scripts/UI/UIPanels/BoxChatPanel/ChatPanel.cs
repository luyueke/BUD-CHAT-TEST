using Basic;
using DG.Tweening;
using Fsbm.Runtime;
using Message;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 消息界面
    /// </summary>
    public class ChatPanel : MonoBehaviour
    {

        public RectTransform Content;

        public RectTransform ScrollView;

        public ChatPanelItem Item;

        public ChatResultItem chatResultItem;
        public ChatCreatingResultItem chatCreatingResultItem;
        public GameObject titleItem;
        public GameObject timeItem;




        public CButton CloseBtn;
        public Button SendBtn;

        public GameObject NoSend;

        #region  普通聊天的组件
        public InputField InputField;

        public Text InputTxtTem;

        public ScrollRect TxtScroll;

        public RectTransform TxtScrollContent;

        public ContentSizeFitter InputTxtTemSize;

        public RectTransform InputTxtTemRect;

        public RectTransform InputFieldRect;

        public RectTransform InputBgRect;


        public Button InputFieldBtn;
        public KeyBoardTest keyBoardTest;


        #endregion

        public GameObject normalChatGo;

        public RectTransform panel;

        public ImagePointTrigger ImagePointTrigger;

        public ImagePointTrigger ImagePointTrigger2;

        public GameObject btnTempObj; //当收到botProfile时，添加这个

        public Text titleText;

        public ChatSelectionParentCom chatSelectionParentCom; // AI选择题面板

        public GameObject chatNode;
        public ChatVoiceCom chatVoiceCom;
        public ChatBuyVoiceCom chatBuyVoiceCom;
        public ChatChoiceAvatarCom chatChoiceAvatarCom;
        string _robotPortraitUrl;

        List<(bool, string)> data = new List<(bool, string)>();
        List<ChatPanelItem> ItemLs = new List<ChatPanelItem>();

        public Action onCloseCb;
        BudTimer budTimer;

        // ── CreateBotStream 聊天状态 ──
        private readonly List<CabinChatCreateBotRoleContent> _chatHistory = new();
        private string _currentSessionId = "";
        private string _characterId = "";
        private readonly StringBuilder _currentAiContent = new();
        private bool _botProfileReceived = false;
        private bool _waitingForResponse = false;
        private ChatPanelItem _currentStreamItem;
        private ChatCreatingResultItem _currentCreatingResultItem;
        private bool _isExtracting = false;
        private readonly StringBuilder _extractAiContent = new();

        // ── 时间分割线状态 ──
        private int _lastMsgTimestamp = 0;       // 最后一条消息的 unix 时间戳（秒）
        private bool _isFirstSendOfSession = true; // 进入聊天页后是否还未发送过消息

        // ── 流式语音 ──
        private TextChatOptions _lastStreamOptions;

        // ── Bot 流（chatType==1）专用状态 ──
        private enum BotStreamMode { Unknown, PlainText, SelectionJson }
        private BotStreamMode _botStreamMode = BotStreamMode.Unknown;
        private readonly StringBuilder _botAccumulated = new();
        private int _botReplyCharStreamed = 0;
        private bool _botSelectionInitialized = false;
        private int _botChipsAdded = 0;
        private string _botDetectedSelMode = "single";
        private int _botDetectedMultiMax = 1;

        ChatMainPanel _chatMainPanel;

        private ChatPanelItem CreateChatItem()
        {
            var item = Instantiate(Item, Content).GetComponent<ChatPanelItem>();
            item.chatType = chatType;
            item.SetPortrait(_robotPortraitUrl, 0);
            return item;
        }

        int chatType = 0; //1:创建角色聊天 2:正常角色聊天

        public float key_h;
        float txt_h;
        public void Awake()
        {
            // InputField.placeholder.GetComponent<Text>().text = "Talk to " + "";

            NoSend.gameObject.SetActive(true);
            // normalChatGo?.SetActive(true);
            chatSelectionParentCom?.gameObject.SetActive(false);


            SendBtn.onClick.AddListener(OnSendBtn);

            ImagePointTrigger.OnPointDown = () => { CloseKeyboard(); };
            ImagePointTrigger2.OnPointDown = () => { CloseKeyboard(); };
            InputField.onEndEdit.AddListener(OnInput);
            InputFieldBtn.onClick.AddListener(OnInputBtn);

            // Selection_InputField.onEndEdit.AddListener(Selection_OnInput);
            // Selection_InputFieldBtn.onClick.AddListener(Selection_OnInputBtn);


            CloseBtn.onClick.AddListener(OnCloseBtn);

            InputFieldBtn.gameObject.SetActive(true);
            // Selection_InputFieldBtn.gameObject.SetActive(true);



            keyBoardTest.offsetAction = OnOffsetAction;
            keyBoardTest.inputAction = OnInputAction;

            InputField.onValueChanged.AddListener((str) => { OnInputAction(InputField.text); });


        }

        void OnCloseBtn()
        {
            for (int i = 0; i < Content.childCount; i++)
            {
                GameObject.Destroy(Content.GetChild(i).gameObject);
            }
            _chatMainPanel?.EndRoleChat();
            gameObject.SetActive(false);
            onCloseCb?.Invoke();

            if (chatType == 1)
            {
                ScreenOrientationHelper.Inst.Switch(ScreenOrientation.LandscapeLeft,
                    () =>
                    {
                        UIManager.Inst.ClosePanel(PanelId.AICompanionChatPanel);
                        UIManager.Inst.OpenPanel(PanelId.IncubationCabinDraftBox);
                    });
            }
        }

        protected void OnDestroy()
        {
            CloseKeyboard();
            TimerManager.Inst.Stop(budTimer);
        }

        public void SetChatMainPanel(ChatMainPanel chatMainPanel)
        {
            _chatMainPanel = chatMainPanel;
        }



        ChatPanelItem waitAnim;
        void OnNewAddChatMessage(CabinChatCreateBotChoiceData cabinChatCreateBotChoiceData, bool bo)
        {
            string str = cabinChatCreateBotChoiceData.delta.content;

            if (!string.IsNullOrEmpty(str))
            {
                data.Add((bo, str));

                if (waitAnim != null && !bo)
                {
                    waitAnim.SetData((bo, str));
                    waitAnim = null;
                }
                else
                {
                    var tem = CreateChatItem();
                    tem.SetData((bo, str));
                    // tem.SetPortrait(_robotPortraitUrl, 0);
                    ItemLs.Add(tem);
                    if (waitAnim != null)
                    {
                        waitAnim.transform.SetAsLastSibling();
                    }
                }

                if (bo && waitAnim == null)
                {
                    waitAnim = CreateChatItem();
                    waitAnim.SetWaitAnim();
                    ItemLs.Add(waitAnim);
                }

                // LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
                if (Content.sizeDelta.y > ScrollView.rect.height)
                {
                    // Content.DOLocalMoveY(Content.sizeDelta.y - ScrollView.rect.height, 0.2f);
                }
            }
        }

        void OnSendBtn()
        {
            if (_waitingForResponse) return;

            string inputText = InputField.text;
            inputText = RemoveEmoji(inputText);
            if (string.IsNullOrEmpty(inputText)) return;


            InputField.text = "";

            int nowTs = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // 满足任一条件插入时间分割线：首次发送 / 间隔≥5分钟 / 跨天
            if (_isFirstSendOfSession
                || (_lastMsgTimestamp > 0 && nowTs - _lastMsgTimestamp >= 5 * 60)
                || (_lastMsgTimestamp > 0 && DateTimeOffset.FromUnixTimeSeconds(nowTs).LocalDateTime.Date
                    != DateTimeOffset.FromUnixTimeSeconds(_lastMsgTimestamp).LocalDateTime.Date))
            {
                InsertTimeItem(nowTs);
            }
            _isFirstSendOfSession = false;
            _lastMsgTimestamp = nowTs;

            // 先创建 bot 回复占位，BeginProducing 状态
            // OnNewAddChatMessage 检测到 waitAnim != null 会将其 SetAsLastSibling（移到用户消息之后）
            waitAnim = CreateChatItem();
            waitAnim.SetBotProducing();
            ItemLs.Add(waitAnim);

            OnNewAddChatMessage(new CabinChatCreateBotChoiceData
            {
                delta = new CabinChatCreateBotRoleContent { role = "user", content = inputText, timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            }, true);

            OnInputAction("");
            if (keyBoardTest?.keyboard != null)
                keyBoardTest.keyboard.text = "";

            _chatHistory.Add(new CabinChatCreateBotRoleContent { role = "user", content = inputText, timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
            _waitingForResponse = true;
            _currentStreamItem = null;
            NoSend.SetActive(true);

            if (chatType == 2)
            {
                CabinChatManager.Inst.BoxchatStream(
                    new boxchatStreamReq
                    {
                        messages = new() { new() { role = "user", content = inputText, timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() } },
                        characterId = _characterId
                    },
                    OnStreamReceive
                );
            }
            else if (chatType == 1)
            {
                _botStreamMode = BotStreamMode.Unknown;
                _botAccumulated.Clear();
                _botReplyCharStreamed = 0;
                _botSelectionInitialized = false;
                _botChipsAdded = 0;
                _botDetectedSelMode = "single";
                _botDetectedMultiMax = 1;
                CabinChatManager.Inst.CreateBotStream(
                    new createBotStreamReq
                    {
                        messages = new() { new CabinChatCreateBotRoleContent { role = "user", content = inputText, timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() } },
                        sessionId = _currentSessionId
                    },
                    OnBotStreamReceive
                );
            }
        }


        void OnInput(string str)
        {

        }

        void OnInputBtn()
        {
#if UNITY_EDITOR
            OnOffsetAction(0);
#endif
            if (keyBoardTest.keyboard == null)
            {
                keyBoardTest.OpenKeyboard("");
                InputFieldBtn.gameObject.SetActive(false);
            }
        }

        void CloseKeyboard()
        {
            OnOffsetAction(0);
            keyBoardTest.CloseOpenKeyboard();
            InputFieldBtn.gameObject.SetActive(true);
        }

        float barY;
        void OnInputAction(string str)
        {
#if UNITY_EDITOR
            // OnOffsetAction(400);
#endif
            // Debug.LogError("2当前内容：" + str);
            if (InputTxtTem.text != str)
            {
                bool needsSync = InputField.text != str;
                int prevCaret = InputField.caretPosition;
                int prevLen = InputField.text.Length;

                InputField.SetTextWithoutNotify(str);

                if (needsSync && str.Length > prevLen)
                {
                    int newCaret = Mathf.Min(prevCaret + (str.Length - prevLen), str.Length);
                    InputField.caretPosition = newCaret;
                    InputField.selectionAnchorPosition = newCaret;
                }

                InputTxtTem.text = str;
                InputTxtTemSize.SetLayoutVertical();
                txt_h = InputTxtTemRect.sizeDelta.y;

                // Debug.LogError("txt_h=" + txt_h);
                //大于四行展示放入滑动条控制
                if (txt_h >= 151)
                {
                    TxtScroll.gameObject.SetActive(true);
                    if (InputFieldRect.transform.parent != TxtScrollContent.transform)
                    {
                        InputFieldRect.transform.parent = TxtScrollContent.transform;
                        barY = 0;
                    }
                    else
                    {
                        barY = TxtScroll.verticalScrollbar.value;
                    }
                    InputBgRect.sizeDelta = new Vector2(InputBgRect.sizeDelta.x, 214);
                    InputFieldRect.anchorMin = new Vector2(0, 0);
                    InputFieldRect.anchorMax = new Vector2(0, 0);
                    InputFieldRect.pivot = new Vector2(0, 0);
                    InputFieldRect.anchoredPosition = new Vector2(0f, 0f);


                    LayoutRebuilder.ForceRebuildLayoutImmediate(TxtScrollContent);
                    TxtScrollContent.sizeDelta = new(TxtScrollContent.sizeDelta.x, 178.6f);
                    InputFieldRect.sizeDelta = new(InputFieldRect.sizeDelta.x, 178.6f);
                    // TxtScrollContent.anchoredPosition = new Vector2(TxtScrollContent.anchoredPosition.x,
                    //     (TxtScrollContent.sizeDelta.y - 155) * (1 - barY));
                }
                else
                {
                    TxtScroll.gameObject.SetActive(false);

                    InputFieldRect.transform.parent = InputBgRect.transform;
                    // InputFieldRect.anchorMin = new Vector2(0.5f, 0);
                    // InputFieldRect.anchorMax = new Vector2(0.5f, 1);
                    // InputFieldRect.pivot = new Vector2(1, 0);

                    InputFieldRect.sizeDelta = new Vector2(787, Math.Max(102, txt_h + 12));
                    // InputFieldRect.anchoredPosition = new Vector2(344f, InputFieldRect.anchoredPosition.y);
                    // InputFieldRect.offsetMin = new Vector2(InputFieldRect.offsetMin.x, 22.3f);
                    // InputFieldRect.offsetMax = new Vector2(InputFieldRect.offsetMax.x, -8.9f);

                    InputBgRect.sizeDelta = new Vector2(InputBgRect.sizeDelta.x, 134 + Math.Max(0, txt_h - 72));
                    ScrollView.offsetMin = new Vector2(ScrollView.offsetMin.x, 134 + key_h + txt_h);


                    // InputFieldRect.anchoredPosition = new Vector2(334, 124);
                    // InputFieldBtn.transform.SetAsLastSibling();
                    // SendBtn.transform.SetAsLastSibling();
                    // InputBgRect.sizeDelta = new Vector2(InputBgRect.sizeDelta.x, 200 + txt_h);
                    // //InputBg2Rect.sizeDelta = new Vector2(InputBg2Rect.sizeDelta.x, 142 + txt_h);
                    // ScrollView.offsetMin = new Vector2(ScrollView.offsetMin.x, 200 + key_h + txt_h);
                }
            }
            NoSend.SetActive(string.IsNullOrEmpty(RemoveEmoji(str)));
        }

        public string RemoveEmoji(string text)
        {
            return Regex.Replace(
                text,
                @"[\uD800-\uDBFF\uDC00-\uDFFF]",
                ""
            );
        }

        void OnOffsetAction(float h)
        {
            key_h = h;
            if (key_h != panel.anchoredPosition.y)
            {
                panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, key_h);
                ScrollView.offsetMin = new Vector2(ScrollView.offsetMin.x, 200 + key_h + txt_h);
                if (Content.sizeDelta.y > ScrollView.rect.height)
                {
                    Content.DOLocalMoveY(Content.sizeDelta.y - ScrollView.rect.height, 0.2f);
                }
            }

            RefreshScrollviewLayout();
        }



        public void InitWithHistory(string robotPortraitUrl, CabinChatTextHistoryData historyData)
        {
            gameObject.SetActive(true);
            foreach (var item in ItemLs)
                if (item != null) Destroy(item.gameObject);
            ItemLs.Clear();
            data.Clear();
            _chatHistory.Clear();
            _currentAiContent.Clear();
            waitAnim = null;
            _currentStreamItem = null;
            _currentCreatingResultItem = null;
            _isExtracting = false;
            _extractAiContent.Clear();
            _waitingForResponse = false;
            _lastMsgTimestamp = 0;
            _isFirstSendOfSession = true;
            _lastStreamOptions = null;

            if (historyData?.history == null || historyData.history.Count == 0)
            {
                return;
            }

            historyData.history.Reverse();

            for (int i = 0; i < Content.childCount; i++)
            {
                GameObject.Destroy(Content.GetChild(i).gameObject);
            }

            // 取对方头像及卡片底色
            // string robotPortraitUrl = cabinChatSessionData?.portraitUrl;
            // var charData = CabinBoxManager.Inst.GetBoxCharacterData();
            // int colorId = charData?.coverInfo?.GetDetail()?.colorId ?? 0;

            foreach (var entry in historyData.history)
            {
                if (string.IsNullOrEmpty(entry.content))
                {
                    Instantiate(titleItem, Content);
                    continue;
                }

                // 相邻消息间隔 ≥ 5 分钟，插入时间分割线
                if (_lastMsgTimestamp > 0 && entry.timestamp - _lastMsgTimestamp >= 5 * 60)
                    InsertTimeItem(entry.timestamp);

                _lastMsgTimestamp = entry.timestamp;

                bool isUser = entry.role == "user";
                data.Add((isUser, entry.content));
                _chatHistory.Add(new CabinChatCreateBotRoleContent
                {
                    role = entry.role,
                    content = entry.content,
                    timestamp = entry.timestamp,
                    audioUrl = entry.audioUrl,
                    audioDuration = entry.audioDuration,
                    msgId = entry.msgId,
                });
                var tem = CreateChatItem();
                tem.SetData((isUser, entry.content));
                // tem.SetPortrait(_robotPortraitUrl, 0);
                if (!isUser && chatType == 2)
                    tem.SetVoiceBotData(_characterId, entry.msgId, entry.content, entry.audioUrl, entry.audioDuration);
                ItemLs.Add(tem);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
            if (Content.sizeDelta.y > ScrollView.rect.height)
            {
                Content.DOLocalMoveY(Content.sizeDelta.y - ScrollView.rect.height, 0.2f);
            }

            // isEnd == 0：AI 回复未结束，禁用发送，等待外部重连流
            bool isWaiting = historyData.isEnd == 0;
            _waitingForResponse = isWaiting;
            // NoSend.SetActive(isWaiting);
        }




        /// <summary>
        /// 进入创建聊天：发起空 body 的初始流，等待 AI 开场白
        /// </summary>
        public void BeginCreateRoleChat()
        {
            normalChatGo.SetActive(false);
            titleText.text = "AI创作助手";
            chatType = 1;

            _chatHistory.Clear();
            _currentSessionId = "";
            _currentAiContent.Clear();
            _botProfileReceived = false;
            _waitingForResponse = true;
            _currentStreamItem = null;
            _currentCreatingResultItem = null;
            _isExtracting = false;
            _extractAiContent.Clear();
            _lastMsgTimestamp = 0;
            _isFirstSendOfSession = true;
            _botStreamMode = BotStreamMode.Unknown;
            _botAccumulated.Clear();
            _botReplyCharStreamed = 0;
            _botSelectionInitialized = false;
            _botChipsAdded = 0;
            _botDetectedSelMode = "single";
            _botDetectedMultiMax = 1;

            NoSend.SetActive(true);

            _robotPortraitUrl = "Assets/Loadable/UI/UIPanel/AICompanionChatPanel/aiCreatRobotHead.png";


            waitAnim = CreateChatItem();
            // waitAnim.SetPortrait(_robotPortraitUrl, 0);
            waitAnim.SetBotProducing();
            ItemLs.Add(waitAnim);
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

            InputField.placeholder.GetComponent<Text>().text = "";

            CabinChatManager.Inst.CreateBotStream(
                new createBotStreamReq { messages = new(), sessionId = "" },
                OnBotStreamReceive
            );
        }

        public void BeginCharacterChat(string sessionId, string portraitUrl, string name)
        {
            normalChatGo.SetActive(true);
            titleText.text = name;
            InputField.placeholder.GetComponent<Text>().text = "发送消息给" + name + "..";

            chatType = 2;
            _robotPortraitUrl = portraitUrl;
            _characterId = sessionId;

            _chatHistory.Clear();
            _currentSessionId = "";
            _currentAiContent.Clear();
            _botProfileReceived = false;
            _waitingForResponse = false;
            _currentStreamItem = null;
            _currentCreatingResultItem = null;
            _isExtracting = false;
            _lastStreamOptions = null;
            _extractAiContent.Clear();
            _lastMsgTimestamp = 0;
            _isFirstSendOfSession = true;

            NoSend.SetActive(true);
        }


        //{"id":"044d92a5-951e-4afe-ad3b-8c2600575e64","object":"","created":1778480673,"model":"","choices":[{"index":0,"delta":{"role":"assistant","content":"嗨"},"finish_reason":null}],"stream_options":{"include_usage":false}}
        //{"id":"044d92a5-951e-4afe-ad3b-8c2600575e64","object":"","created":1778480673,"model":"","choices":[{"index":0,"delta":{"role":"assistant","content":"，"},"finish_reason":null}],"stream_options":{"include_usage":false}}
        //{"id":"044d92a5-951e-4afe-ad3b-8c2600575e64","object":"","created":1778480673,"model":"","choices":[{"index":0,"delta":{"role":"assistant","content":"想要"},"finish_reason":null}],"stream_options":{"include_usage":false}}
        //{"id":"044d92a5-951e-4afe-ad3b-8c2600575e64","object":"","created":1778480673,"model":"","choices":[{"index":0,"delta":{"role":"assistant","content":"创建"},"finish_reason":null}],"stream_options":{"include_usage":false}}
        //{"id":"044d92a5-951e-4afe-ad3b-8c2600575e64","object":"","created":1778480673,"model":"","choices":[{"index":0,"delta":{"role":"assistant","content":"什么"},"finish_reason":null}],"stream_options":{"include_usage":false}}
        //{"id":"044d92a5-951e-4afe-ad3b-8c2600575e64","object":"","created":1778480673,"model":"","choices":[{"index":0,"delta":{},"finish_reason":"stop"}],"stream_options":{"include_usage":false}}
        private void OnStreamReceive(CabinChatCreateBotData streamData, bool isEnd)
        {
            if (streamData != null)
            {
                if (!string.IsNullOrEmpty(streamData.id))
                    _currentSessionId = streamData.id;

                // 普通聊天模式：缓存最新一帧携带的语音信息
                if (chatType == 2 && streamData.textChatOptions != null
                    && !string.IsNullOrEmpty(streamData.textChatOptions.msgId))
                    _lastStreamOptions = streamData.textChatOptions;

                if (streamData.choices != null)
                {
                    foreach (var choice in streamData.choices)
                    {
                        if (choice?.delta == null) continue;
                        if (chatType == 2 && !string.IsNullOrEmpty(choice.delta.msgId))
                        {
                            _lastStreamOptions ??= new TextChatOptions();
                            _lastStreamOptions.msgId = choice.delta.msgId;
                            _lastStreamOptions.audioDuration = choice.delta.audioDuration;
                        }
                        if (!string.IsNullOrEmpty(choice.delta.content))
                        {
                            if (_isExtracting)
                                _extractAiContent.Append(choice.delta.content);
                            else
                            {
                                bool isFirstWord = false;
                                string content = "";
                                (isFirstWord, content) = TryParseCreateRoleFirstWord(choice.delta.content);
                                AppendAiStreamChunk(content);
                                if (isFirstWord)
                                {
                                    normalChatGo.SetActive(true);
                                    SendBtn.onClick.RemoveAllListeners();
                                    SendBtn.onClick.AddListener(() => SendBySelection(InputField.text));
                                }
                            }
                        }
                    }
                }

                // stage: startExtract → 移除 "..." 气泡，显示进度条，开始静默累积 aiReply
                if (streamData.stage == "startExtract" && _currentCreatingResultItem == null)
                {
                    _isExtracting = true;
                    _extractAiContent.Clear();

                    if (waitAnim != null)
                    {
                        Destroy(waitAnim.gameObject);
                        ItemLs.Remove(waitAnim);
                        waitAnim = null;
                    }
                    if (chatCreatingResultItem != null)
                    {
                        _currentCreatingResultItem = Instantiate(chatCreatingResultItem, Content);
                        _currentCreatingResultItem.BeginProgress();
                    }

                    Invoke("waitSetVerticalNormalizedPosition", 0.1f);
                }

                // stage: outputExtract → 把 aiReply 句子插在进度条前，再结束进度条，显示结果
                if (streamData.stage == "outputExtract" && streamData.botProfile != null && !_botProfileReceived)
                {
                    _isExtracting = false;
                    _botProfileReceived = true;
                    NoSend.SetActive(true);

                    Action endAc = () =>
                    {

                        string extractedReply = _extractAiContent.ToString();
                        _extractAiContent.Clear();
                        if (!string.IsNullOrEmpty(extractedReply) && _currentCreatingResultItem != null)
                        {
                            int insertIdx = _currentCreatingResultItem.transform.GetSiblingIndex();
                            var replyItem = CreateChatItem();
                            replyItem.SetData((false, extractedReply));
                            replyItem.transform.SetSiblingIndex(insertIdx);
                            ItemLs.Add(replyItem);
                            _chatHistory.Add(new CabinChatCreateBotRoleContent { role = "assistant", content = extractedReply });
                        }

                        if (_currentCreatingResultItem != null)
                        {
                            Destroy(_currentCreatingResultItem.gameObject);
                        }

                        if (chatResultItem != null)
                        {
                            var resultItem = Instantiate(chatResultItem, Content);
                            resultItem.SetData(streamData.botProfile);
                        }
                        if (btnTempObj != null)
                        {
                            var go = Instantiate(btnTempObj, Content);
                            go.GetComponentInChildren<Button>().onClick.AddListener(() =>
                            {
                                //
                                chatNode?.SetActive(false);
                                chatVoiceCom.SetData(streamData.botProfile);
                                // UIManager.Inst.ClosePanel(PanelId.AICompanionChatPanel);
                                // CoroutineManager.Inst.StartCoroutine(WaitForLandscapeAndCreateRole(streamData.botProfile));
                            });
                        }
                        Invoke("waitSetVerticalNormalizedPosition", 0.1f);
                    };


                    if (_currentCreatingResultItem != null)
                    {
                        _currentCreatingResultItem.EndProgress(() =>
                        {
                            endAc();
                            _currentCreatingResultItem = null;
                        });
                    }
                    else
                    {
                        endAc();
                    }
                }
            }

            if (isEnd)
            {
                _waitingForResponse = false;
                bool wasInsufficient = _lastStreamOptions?.insufficient ?? false;

                // 若整条流没有内容，waitAnim 还悬在那里，清掉
                if (waitAnim != null)
                {
                    Destroy(waitAnim.gameObject);
                    ItemLs.Remove(waitAnim);
                    waitAnim = null;
                }

                var finishedStreamItem = _currentStreamItem;
                _currentStreamItem = null;

                string aiContent = _currentAiContent.ToString();
                _currentAiContent.Clear();

                if (!string.IsNullOrEmpty(aiContent))
                {
                    bool isSelectionUI;
                    (isSelectionUI, aiContent) = TryShowSelectionUI(aiContent);
                    if (isSelectionUI)
                    {
                        // 移除显示原始JSON的气泡
                        if (finishedStreamItem != null)
                        {
                            Destroy(finishedStreamItem.gameObject);
                            ItemLs.Remove(finishedStreamItem);
                        }
                        // 选择题面板处于激活状态，等待用户选择，不隐藏 NoSend
                    }
                    else
                    {
                        string streamMsgId = _lastStreamOptions?.msgId;
                        int streamAudioDuration = _lastStreamOptions?.audioDuration ?? 0;
                        _chatHistory.Add(new CabinChatCreateBotRoleContent
                        {
                            role = "assistant",
                            content = aiContent,
                            audioDuration = streamAudioDuration,
                            msgId = streamMsgId,
                        });
                        if (!_botProfileReceived)
                            NoSend.SetActive(true);

                        // 普通聊天模式：流结束后给气泡绑定语音节点（audioUrl 为空，走 GetBoxTextAudio 拉取）
                        // chatType==2 使用 _characterId 作为 sessionId（BoxchatStream 的 streamData.id 是消息ID）
                        if (chatType == 2 && finishedStreamItem != null)
                        {
                            finishedStreamItem.SetVoiceBotData(
                                _characterId,
                                streamMsgId,
                                aiContent,
                                null,
                                streamAudioDuration);
                            _lastStreamOptions = null;
                        }
                    }
                }
                else if (!_botProfileReceived)
                {
                    NoSend.SetActive(true);
                }
                Invoke("waitSetVerticalNormalizedPosition", 0.1f);

                if (wasInsufficient)
                    UIManager.Inst.OpenPanel(PanelId.AICreditOverPanel);
            }
        }

        // ────────────────────────────────────────────────────────────────────────
        // Bot 创角流式回调（chatType==1 专用）
        // ────────────────────────────────────────────────────────────────────────
        private void OnBotStreamReceive(CabinChatCreateBotData streamData, bool isEnd)
        {
            if (streamData != null)
            {
                if (!string.IsNullOrEmpty(streamData.id))
                    _currentSessionId = streamData.id;

                if (streamData.choices != null)
                {
                    foreach (var choice in streamData.choices)
                    {
                        if (choice?.delta == null) continue;
                        if (string.IsNullOrEmpty(choice.delta.content)) continue;

                        if (_isExtracting)
                        {
                            _extractAiContent.Append(choice.delta.content);
                            continue;
                        }

                        _botAccumulated.Append(choice.delta.content);
                        var acc = _botAccumulated.ToString();

                        // ── 模式检测 ──
                        if (_botStreamMode == BotStreamMode.Unknown)
                        {
                            const string PREFIX = "{\"reply\":";
                            bool enoughToDecide = acc.Length >= PREFIX.Length || (acc.Length > 0 && acc[0] != '{');
                            if (enoughToDecide)
                            {
                                _botStreamMode = acc.TrimStart().StartsWith(PREFIX, StringComparison.Ordinal)
                                    ? BotStreamMode.SelectionJson
                                    : BotStreamMode.PlainText;

                                if (_botStreamMode == BotStreamMode.PlainText)
                                {
                                    // 把已积累的所有内容一次性送入气泡
                                    AppendAiStreamChunk(acc);
                                    continue;
                                }
                                // SelectionJson：下面继续走 SelectionJson 逻辑
                            }
                            else
                            {
                                continue; // 还没到可判断长度，先等
                            }
                        }

                        if (_botStreamMode == BotStreamMode.PlainText)
                        {
                            // 后续逐 chunk 追加（第一个 chunk 已在上面 flush）
                            AppendAiStreamChunk(choice.delta.content);
                        }
                        else // SelectionJson
                        {
                            // ── 流式输出 reply 文字 ──
                            string replyText = ExtractReplyText(acc);
                            if (replyText.Length > _botReplyCharStreamed)
                            {
                                string newChars = replyText.Substring(_botReplyCharStreamed);
                                _botReplyCharStreamed = replyText.Length;
                                if (chatSelectionParentCom != null)
                                    chatSelectionParentCom.txt_question.text = replyText;

                                if (_currentStreamItem == null)
                                {
                                    if (waitAnim != null)
                                    {
                                        waitAnim.SetData((false, newChars));
                                        _currentStreamItem = waitAnim;
                                        waitAnim = null;
                                    }
                                    else
                                    {
                                        _currentStreamItem = CreateChatItem();
                                        _currentStreamItem.SetData((false, newChars));
                                        // _currentStreamItem.SetPortrait(_robotPortraitUrl, 0);
                                        ItemLs.Add(_currentStreamItem);
                                    }
                                }
                                else
                                {
                                    _currentStreamItem.AppendText(newChars);
                                }
                            }

                            // ── selection_mode 检测（可能在 chips 前后任意位置出现）──
                            var (detectedMode, detectedMax) = ExtractSelectionModeFromPartial(acc);
                            if (_botSelectionInitialized
                                && (detectedMode != _botDetectedSelMode || detectedMax != _botDetectedMultiMax))
                            {
                                _botDetectedSelMode = detectedMode;
                                _botDetectedMultiMax = detectedMax;
                                chatSelectionParentCom.UpdateSelectionMode(detectedMode, detectedMax);
                            }

                            // ── 逐个解析并追加 chip ──
                            var chips = ExtractChipsFromPartial(acc);
                            for (int ci = _botChipsAdded; ci < chips.Count; ci++)
                            {
                                if (!_botSelectionInitialized)
                                {
                                    _botSelectionInitialized = true;
                                    _botDetectedSelMode = detectedMode;
                                    _botDetectedMultiMax = detectedMax;
                                    normalChatGo?.SetActive(false);
                                    chatSelectionParentCom.gameObject.SetActive(true);
                                    chatSelectionParentCom.InitStream(detectedMode, detectedMax, SendBySelection);

                                }
                                chatSelectionParentCom.AppendChip(chips[ci]);
                                _botChipsAdded++;
                            }
                        }
                        CloseKeyboard();
                        RefreshScrollviewLayout();
                    }
                }

                // stage: startExtract → 移除 "..." 气泡，显示进度条，开始静默累积
                if (streamData.stage == "startExtract" && _currentCreatingResultItem == null)
                {
                    _isExtracting = true;
                    _extractAiContent.Clear();

                    if (waitAnim != null)
                    {
                        Destroy(waitAnim.gameObject);
                        ItemLs.Remove(waitAnim);
                        waitAnim = null;
                    }
                    if (chatCreatingResultItem != null)
                    {
                        _currentCreatingResultItem = Instantiate(chatCreatingResultItem, Content);
                        _currentCreatingResultItem.BeginProgress();
                    }
                    RefreshScrollviewLayout();
                    Invoke("waitSetVerticalNormalizedPosition", 0.1f);
                }

                // stage: outputExtract → 进度条结束，插入 aiReply 气泡，显示创角结果
                if (streamData.stage == "outputExtract" && streamData.botProfile != null && !_botProfileReceived)
                {
                    _isExtracting = false;
                    _botProfileReceived = true;
                    NoSend.SetActive(true);

                    Action endAc = () =>
                    {
                        string extractedReply = _extractAiContent.ToString();
                        _extractAiContent.Clear();
                        if (!string.IsNullOrEmpty(extractedReply) && _currentCreatingResultItem != null)
                        {
                            int insertIdx = _currentCreatingResultItem.transform.GetSiblingIndex();
                            var replyItem = CreateChatItem();
                            replyItem.SetData((false, extractedReply));
                            replyItem.transform.SetSiblingIndex(insertIdx);
                            ItemLs.Add(replyItem);
                            _chatHistory.Add(new CabinChatCreateBotRoleContent { role = "assistant", content = extractedReply });
                        }
                        if (_currentCreatingResultItem != null)
                            Destroy(_currentCreatingResultItem.gameObject);
                        if (chatResultItem != null)
                        {
                            var resultItem = Instantiate(chatResultItem, Content);
                            resultItem.SetData(streamData.botProfile);
                        }
                        if (btnTempObj != null)
                        {
                            var go = Instantiate(btnTempObj, Content);
                            go.GetComponentInChildren<Button>().onClick.AddListener(() =>
                            {
                                chatNode?.SetActive(false);
                                chatVoiceCom.SetData(streamData.botProfile);
                            });
                        }
                        Invoke("waitSetVerticalNormalizedPosition", 0.1f);
                    };

                    if (_currentCreatingResultItem != null)
                        _currentCreatingResultItem.EndProgress(() => { endAc(); _currentCreatingResultItem = null; });
                    else
                        endAc();
                }
            }

            if (isEnd)
            {
                _waitingForResponse = false;

                if (waitAnim != null)
                {
                    Destroy(waitAnim.gameObject);
                    ItemLs.Remove(waitAnim);
                    waitAnim = null;
                }

                if (_botStreamMode == BotStreamMode.SelectionJson)
                {
                    _currentStreamItem = null;
                    var acc = _botAccumulated.ToString();

                    // 补齐未及时追加的 chip
                    var chips = ExtractChipsFromPartial(acc);
                    for (int ci = _botChipsAdded; ci < chips.Count; ci++)
                    {
                        if (!_botSelectionInitialized)
                        {
                            _botSelectionInitialized = true;
                            normalChatGo?.SetActive(false);
                            chatSelectionParentCom.gameObject.SetActive(true);
                            chatSelectionParentCom.InitStream("single", 1, SendBySelection);
                            UICommonUtils.RefreshLayout(panel.transform);
                            ScrollView.offsetMin = new Vector2(ScrollView.offsetMin.x, panel.rect.height);
                        }
                        chatSelectionParentCom.AppendChip(chips[ci]);
                        _botChipsAdded++;
                    }

                    // 流结束，开放点击
                    if (_botSelectionInitialized)
                        chatSelectionParentCom.SetInteractable(true);

                    string replyForHistory = "";
                    try
                    {
                        var selData = JsonConvert.DeserializeObject<ChatSelectionData>(acc);
                        replyForHistory = selData?.reply ?? "";

                        // 仅"自己写"单chip：切回普通输入框
                        bool isSelfWriteOnly = selData?.chips != null
                            && selData.chips.Count == 1
                            && selData.chips[0].label == "自己写";
                        if (isSelfWriteOnly)
                        {
                            chatSelectionParentCom.gameObject.SetActive(false);
                            normalChatGo.SetActive(true);
                            SendBtn.onClick.RemoveAllListeners();
                            SendBtn.onClick.AddListener(() => SendBySelection(InputField.text));
                        }
                    }
                    catch { }

                    if (!string.IsNullOrEmpty(replyForHistory))
                        _chatHistory.Add(new CabinChatCreateBotRoleContent { role = "assistant", content = replyForHistory });

                    if (!_botProfileReceived)
                        NoSend.SetActive(true);
                }
                else // PlainText / Unknown
                {
                    var finishedStreamItem = _currentStreamItem;
                    _currentStreamItem = null;
                    string aiContent = _currentAiContent.ToString();
                    _currentAiContent.Clear();

                    if (!string.IsNullOrEmpty(aiContent))
                    {
                        bool isSelectionUI;
                        (isSelectionUI, aiContent) = TryShowSelectionUI(aiContent);
                        if (isSelectionUI)
                        {
                            if (finishedStreamItem != null)
                            {
                                Destroy(finishedStreamItem.gameObject);
                                ItemLs.Remove(finishedStreamItem);
                            }
                        }
                        else
                        {
                            _chatHistory.Add(new CabinChatCreateBotRoleContent { role = "assistant", content = aiContent });
                            if (!_botProfileReceived)
                                NoSend.SetActive(true);
                        }
                    }
                    else if (!_botProfileReceived)
                    {
                        NoSend.SetActive(true);
                    }
                }


                RefreshScrollviewLayout();
                // 重置 bot stream 状态，为下一轮准备
                _botStreamMode = BotStreamMode.Unknown;
                _botAccumulated.Clear();
                _botReplyCharStreamed = 0;
                _botSelectionInitialized = false;
                _botChipsAdded = 0;
                _botDetectedSelMode = "single";
                _botDetectedMultiMax = 1;

                Invoke("waitSetVerticalNormalizedPosition", 0.1f);
            }
        }

        public void RefreshScrollviewLayout()
        {
            UICommonUtils.RefreshLayout(panel.transform);
            ScrollRect scrollRect = ScrollView.GetComponent<ScrollRect>();
            UICommonUtils.RefreshLayout(scrollRect.content);
            ScrollView.offsetMin = new Vector2(ScrollView.offsetMin.x, panel.rect.height + key_h);
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }

        // ── 辅助：从部分 JSON 中提取 reply 字符串 ──────────────────────────────
        private string ExtractReplyText(string json)
        {
            const string MARKER = "{\"reply\":\"";
            int start = json.IndexOf(MARKER, StringComparison.Ordinal);
            if (start < 0) return "";
            int textStart = start + MARKER.Length;
            if (textStart >= json.Length) return "";
            var sb = new StringBuilder();
            for (int i = textStart; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    i++;
                    switch (json[i])
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        default: sb.Append('\\'); sb.Append(json[i]); break;
                    }
                    continue;
                }
                if (c == '"') break;
                sb.Append(c);
            }
            return sb.ToString();
        }

        // ── 辅助：从部分 JSON 中提取所有完整的 chip 对象 ──────────────────────
        private List<ChatSelectionChip> ExtractChipsFromPartial(string json)
        {
            var result = new List<ChatSelectionChip>();
            const string CHIPS_MARKER = "\"chips\":[";
            int arrayStart = json.IndexOf(CHIPS_MARKER, StringComparison.Ordinal);
            if (arrayStart < 0) return result;
            int pos = arrayStart + CHIPS_MARKER.Length;
            while (pos < json.Length)
            {
                while (pos < json.Length && (json[pos] == ' ' || json[pos] == '\n' || json[pos] == '\r' || json[pos] == '\t')) pos++;
                if (pos >= json.Length || json[pos] != '{') break;
                int end = FindMatchingBrace(json, pos);
                if (end < 0) break; // chip 尚不完整，等待后续数据
                try
                {
                    var chip = JsonConvert.DeserializeObject<ChatSelectionChip>(json.Substring(pos, end - pos + 1));
                    if (chip != null) result.Add(chip);
                }
                catch { }
                pos = end + 1;
                while (pos < json.Length && (json[pos] == ',' || json[pos] == ' ' || json[pos] == '\n' || json[pos] == '\r')) pos++;
            }
            return result;
        }

        // ── 辅助：在 JSON 字符串中找到与 openPos 处 '{' 匹配的 '}' 位置 ───────
        private int FindMatchingBrace(string json, int openPos)
        {
            int depth = 0;
            bool inStr = false;
            for (int i = openPos; i < json.Length; i++)
            {
                char c = json[i];
                if (inStr)
                {
                    if (c == '\\') { i++; continue; }
                    if (c == '"') inStr = false;
                    continue;
                }
                if (c == '"') { inStr = true; continue; }
                if (c == '{') depth++;
                else if (c == '}') { if (--depth == 0) return i; }
            }
            return -1;
        }

        // ── 辅助：从部分 JSON 中提取 selection_mode 和 multi_max ──────────────
        private (string selMode, int multiMax) ExtractSelectionModeFromPartial(string json)
        {
            string mode = "single";
            int max = 1;

            const string MODE_KEY = "\"selection_mode\":\"";
            int mi = json.IndexOf(MODE_KEY, StringComparison.Ordinal);
            if (mi >= 0)
            {
                int s = mi + MODE_KEY.Length;
                int e = json.IndexOf('"', s);
                if (e > s) mode = json.Substring(s, e - s);
            }

            const string MAX_KEY = "\"multi_max\":";
            int xi = json.IndexOf(MAX_KEY, StringComparison.Ordinal);
            if (xi >= 0)
            {
                int s = xi + MAX_KEY.Length;
                while (s < json.Length && json[s] == ' ') s++;
                int e = s;
                while (e < json.Length && char.IsDigit(json[e])) e++;
                if (e > s && int.TryParse(json.Substring(s, e - s), out int parsed))
                    max = parsed;
            }

            return (mode, max);
        }

        /// <summary>
        /// 尝试将 aiContent 解析为创建角色第一句话
        /// </summary>
        private (bool, string) TryParseCreateRoleFirstWord(string aiContent)
        {
            if (chatType != 1)
            {
                return (false, aiContent);
            }
            if (chatSelectionParentCom == null) return (false, aiContent);
            if (string.IsNullOrEmpty(aiContent) || !aiContent.TrimStart().StartsWith("{")) return (false, aiContent);
            try
            {
                var selData = JsonConvert.DeserializeObject<ChatSelectionData>(aiContent);
                if (selData?.chips == null || selData.chips.Count == 0) return (false, aiContent);
                if (selData.chips.Count == 1 && selData.chips[0].label == "自己写")
                {
                    return (true, selData.reply);
                }
                return (false, aiContent);
            }
            catch
            {
                return (false, aiContent);
            }
        }

        /// <summary>
        /// 尝试将 aiContent 解析为选择题 JSON，成功则显示选择面板并返回 true
        /// </summary>
        private (bool, string) TryShowSelectionUI(string aiContent)
        {
            if (chatSelectionParentCom == null) return (false, aiContent);
            if (string.IsNullOrEmpty(aiContent) || !aiContent.TrimStart().StartsWith("{")) return (false, aiContent);
            try
            {
                var selData = JsonConvert.DeserializeObject<ChatSelectionData>(aiContent);
                if (selData?.chips == null || selData.chips.Count == 0) return (false, aiContent);
                if (selData.chips.Count == 1 && selData.chips[0].label == "自己写")
                {
                    return (false, selData.reply);
                }

                if (!string.IsNullOrEmpty(selData.reply))
                {
                    var questionItem = CreateChatItem();
                    questionItem.SetData((false, selData.reply));
                    ItemLs.Add(questionItem);
                }

                normalChatGo?.SetActive(false);
                chatSelectionParentCom.gameObject.SetActive(true);
                chatSelectionParentCom.SetData(aiContent, SendBySelection);

                UICommonUtils.RefreshLayout(panel.transform);
                var height = panel.rect.height;
                ScrollView.offsetMin = new Vector2(ScrollView.offsetMin.x, height);

                // LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
                // if (Content.sizeDelta.y > ScrollView.rect.height)
                // Content.DOLocalMoveY(Content.sizeDelta.y - ScrollView.rect.height, 0.2f);
                return (true, aiContent);
            }
            catch
            {
                return (false, aiContent);
            }
        }

        /// <summary>
        /// <summary>
        /// 选择题确认回调：将选中的 value 作为用户消息发给服务器
        /// </summary>
        private void SendBySelection(string value)
        {
            Debug.Log("发送:" + value);
            value = RemoveEmoji(value);
            SendBtn.onClick.RemoveAllListeners();
            SendBtn.onClick.AddListener(OnSendBtn);
            InputField.text = "";
            normalChatGo.SetActive(false);

            if (chatSelectionParentCom != null)
                chatSelectionParentCom.gameObject.SetActive(false);

            if (string.IsNullOrEmpty(value)) return;

            int nowTs = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (_isFirstSendOfSession
                || (_lastMsgTimestamp > 0 && nowTs - _lastMsgTimestamp >= 5 * 60)
                || (_lastMsgTimestamp > 0 && DateTimeOffset.FromUnixTimeSeconds(nowTs).LocalDateTime.Date
                    != DateTimeOffset.FromUnixTimeSeconds(_lastMsgTimestamp).LocalDateTime.Date))
            {
                InsertTimeItem(nowTs);
            }
            _isFirstSendOfSession = false;
            _lastMsgTimestamp = nowTs;

            waitAnim = CreateChatItem();
            waitAnim.SetBotProducing();
            ItemLs.Add(waitAnim);

            OnNewAddChatMessage(new CabinChatCreateBotChoiceData
            {
                delta = new CabinChatCreateBotRoleContent { role = "user", content = value, timestamp = nowTs }
            }, true);

            _chatHistory.Add(new CabinChatCreateBotRoleContent { role = "user", content = value, timestamp = nowTs });
            _waitingForResponse = true;
            _currentStreamItem = null;
            NoSend.SetActive(true);

            _botStreamMode = BotStreamMode.Unknown;
            _botAccumulated.Clear();
            _botReplyCharStreamed = 0;
            _botSelectionInitialized = false;
            _botChipsAdded = 0;
            _botDetectedSelMode = "single";
            _botDetectedMultiMax = 1;
            CabinChatManager.Inst.CreateBotStream(
                new createBotStreamReq
                {
                    messages = new() { new CabinChatCreateBotRoleContent { role = "user", content = value, timestamp = nowTs } },
                    sessionId = _currentSessionId
                },
                OnBotStreamReceive
            );

            // if (Content.sizeDelta.y > ScrollView.rect.height)
            // {
            //     Content.DOLocalMoveY(Content.sizeDelta.y - ScrollView.rect.height, 0.2f);
            // }

            Invoke("waitSetVerticalNormalizedPosition", 0.1f);
            RefreshScrollviewLayout();
        }

        void waitSetVerticalNormalizedPosition()
        {
            ScrollView.GetComponent<ScrollRect>().verticalNormalizedPosition = 0;
        }
        private IEnumerator WaitForLandscapeAndCreateRole(CabinChatCreateBotProfileData botProfile)
        {
            // 等待屏幕真正恢复横屏尺寸（最多 2 秒），镜像 SwitchToPortrait 的等待逻辑
            float elapsed = 0f;
            const float timeout = 2f;
            while (Screen.width < Screen.height && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            // 再等一帧让 Canvas 完成布局重计算
            yield return null;
            //创建角色
            var panel = UIManager.Inst.FindPanel(PanelId.IncubationCabinDraftBox);
            (panel as IncubationCabinDraftBox).CreateRole(botProfile);
        }

        private void AppendAiStreamChunk(string chunk)
        {
            _currentAiContent.Append(chunk);
            if (_currentStreamItem == null)
            {
                // 第一个 chunk：把 waitAnim 转成流式气泡，或新建一个
                if (waitAnim != null)
                {
                    waitAnim.SetData((false, chunk));
                    _currentStreamItem = waitAnim;
                    waitAnim = null;
                }
                else
                {
                    _currentStreamItem = CreateChatItem();
                    _currentStreamItem.SetData((false, chunk));
                    ItemLs.Add(_currentStreamItem);
                }
            }
            else
            {
                // 后续 chunk：追加到同一气泡
                _currentStreamItem.AppendText(chunk);
            }
            // _currentStreamItem.SetPortrait(_robotPortraitUrl, 0);

            // LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
            // if (Content.sizeDelta.y > ScrollView.rect.height)
            // Content.DOLocalMoveY(Content.sizeDelta.y - ScrollView.rect.height, 0.2f);
        }



        [Button("测试吐字表现")]
        void testOnStreamReceive()
        {
            // 模拟用户发一条消息，触发 waitAnim
            _waitingForResponse = false;
            OnNewAddChatMessage(new CabinChatCreateBotChoiceData
            {
                delta = new CabinChatCreateBotRoleContent { role = "user", content = "你好！" }
            }, true);
            _waitingForResponse = true;
            _currentStreamItem = null;
            NoSend.SetActive(true);

            var chunks = new List<string> { "嗨", "，", "想要", "创建", "什么", "样的", "角色", "呢？" };
            StartCoroutine(PlayTestStreamChunks(chunks));
        }

        private System.Collections.IEnumerator PlayTestStreamChunks(List<string> chunks)
        {
            const string sid = "test-session-id";
            foreach (var content in chunks)
            {
                OnStreamReceive(new CabinChatCreateBotData
                {
                    id = sid,
                    choices = new List<CabinChatCreateBotChoiceData>
                    {
                        new() { delta = new CabinChatCreateBotRoleContent { role = "assistant", content = content }, finish_reason = null }
                    }
                }, false);
                yield return new UnityEngine.WaitForSeconds(0.15f);
            }

            // 结束帧：finish_reason = "stop"，delta 为空
            OnStreamReceive(new CabinChatCreateBotData
            {
                id = sid,
                choices = new List<CabinChatCreateBotChoiceData>
                {
                    new() { delta = new CabinChatCreateBotRoleContent(), finish_reason = "stop" }
                }
            }, true);
        }




        private void InsertTimeItem(int unixTimestamp)
        {
            var go = Instantiate(timeItem, Content);
            go.GetComponentInChildren<Text>().text = FormatMsgTime(unixTimestamp);
        }

        private string FormatMsgTime(int unixTimestamp)
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).LocalDateTime;
            var now = DateTime.Now;
            if (dt.Date == now.Date)
                return dt.ToString("HH:mm");
            if (dt.Date == now.Date.AddDays(-1))
                return $"昨天 {dt:HH:mm}";
            if (dt.Year == now.Year)
                return $"{dt.Month}月{dt.Day}日 {dt:HH:mm}";
            return $"{dt.Year}年{dt.Month}月{dt.Day}日 {dt:HH:mm}";
        }


        [Button("测试timeItem逻辑")]
        void testTimeItem()
        {
            var now = DateTimeOffset.UtcNow;
            // 基准时间点：今天 10:00
            var baseToday = new DateTimeOffset(now.LocalDateTime.Date.AddHours(10), TimeSpan.FromHours(8));
            // 昨天 23:58
            var baseYesterday = new DateTimeOffset(now.LocalDateTime.Date.AddDays(-1).AddHours(23).AddMinutes(58), TimeSpan.FromHours(8));
            // 今年年初 1月1日 09:00
            var baseThisYear = new DateTimeOffset(new DateTime(now.Year, 1, 1, 9, 0, 0), TimeSpan.FromHours(8));

            int T(DateTimeOffset t) => (int)t.ToUnixTimeSeconds();

            var historyData = new CabinChatTextHistoryData
            {
                isEnd = 1,
                history = new List<CabinChatTextHistory>
                {
                    new() { role = "assistant", content = "今天10:08的回复",            timestamp = T(baseToday.AddMinutes(8)) },
                    new() { role = "user",      content = "今天10:02发的",             timestamp = T(baseToday.AddMinutes(2)) },
                    new() { role = "assistant", content = "今天上午的回复",             timestamp = T(baseToday) },
                    new() { role = "user",      content = "昨天23:59发的",             timestamp = T(baseYesterday.AddMinutes(1)) },
                    new() { role = "assistant", content = "昨天深夜的回复",             timestamp = T(baseYesterday) },
                    new() { role = "user",      content = "10分钟后又发了一条",         timestamp = T(baseThisYear.AddMinutes(10)) },
                    new() { role = "assistant", content = "AI回复（不到5分钟后）",      timestamp = T(baseThisYear.AddMinutes(3)) },
                    new() { role = "user",      content = "今年年初发的消息",          timestamp = T(baseThisYear) },
                }
            };

            // history 由服务端返回的是倒序，Reverse后才是正序，这里直接给正序模拟已Reverse后的状态
            InitWithHistory(null, historyData);
        }

        [Button("测试添加我聊天的内容")]
        void testAddMyChatItem()
        {
            // OnNewAddChatMessage("This is a test message from me.", true);
        }

        [Button("测试添加ai聊天的内容")]
        void testAddAIChatItem()
        {
            // OnNewAddChatMessage("This is a test reply from AI.", false);
        }



        [Button("offset300")]
        void testOffset300()
        {
            OnOffsetAction(300);

        }
        [Button("offset0")]
        void testOffset0()
        {
            OnOffsetAction(0);
        }
        [Button("inputa")]
        void testinput()
        {
            // OnInputAction("aaaa");
            // ScrollView.GetComponent<ScrollRect>().verticalNormalizedPosition = 0;



            string value = "aa";

            //  if (chatSelectionParentCom != null)
            //     chatSelectionParentCom.gameObject.SetActive(false);
            // // normalChatGo?.SetActive(true);

            // if (string.IsNullOrEmpty(value)) return;

            int nowTs = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // if (_isFirstSendOfSession
            //     || (_lastMsgTimestamp > 0 && nowTs - _lastMsgTimestamp >= 5 * 60)
            //     || (_lastMsgTimestamp > 0 && DateTimeOffset.FromUnixTimeSeconds(nowTs).LocalDateTime.Date
            //         != DateTimeOffset.FromUnixTimeSeconds(_lastMsgTimestamp).LocalDateTime.Date))
            // {
            //     InsertTimeItem(nowTs);
            // }
            // _isFirstSendOfSession = false;
            // _lastMsgTimestamp = nowTs;

            waitAnim = CreateChatItem();
            waitAnim.SetBotProducing();
            ItemLs.Add(waitAnim);

            OnNewAddChatMessage(new CabinChatCreateBotChoiceData
            {
                delta = new CabinChatCreateBotRoleContent { role = "user", content = value, timestamp = nowTs }
            }, true);

            _chatHistory.Add(new CabinChatCreateBotRoleContent { role = "user", content = value, timestamp = nowTs });
            _waitingForResponse = true;
            _currentStreamItem = null;
            NoSend.SetActive(true);

            Invoke("waitSetVerticalNormalizedPosition", 0.1f);
        }


        [Button("测试botProfile")]
        void testBotProfile()
        {
            chatNode?.SetActive(false);

            var botProfile = new CabinChatCreateBotProfileData
            {
                name = "苏晚",
                characterName = "苏晚",
                ip_source = "黑月光拿稳BE剧本",
                inspired_by_ip = "黑月光拿稳BE剧本·苏晚",
                characterDesc = "你是女性，十七岁，人类，是仙界灵植峰的弟子，日常负责培育各类珍稀灵草，能精准分辨上千种草药的药性，是峰主最倚重的晚辈。你性格外柔内刚，看似温吞好说话，实则极有原则，触及底线时寸步不让。",
                persona = "你是女性，十七岁，人类，是仙界灵植峰的弟子，日常负责培育各类珍稀灵草，能精准分辨上千种草药的药性，是峰主最倚重的晚辈。你性格外柔内刚，看似温吞好说话，实则极有原则，触及底线时寸步不让。说话总是温声细语，提及灵草时语速会不自觉变快，偶尔会蹦出几句只有灵植峰弟子才懂的行话，遇到旁人认错草药时会下意识纠正，哪怕对方身份尊贵也不会怯场。你最大的执念是培育出能逆转神魂损伤的还魂草，治好当年为救你被魔物打至魂飞魄散的师姐。",
                world = "这是一个仙魔对立的修真世界，灵气遍布山川河岳，修士以修炼提升修为，追求飞升大道，同时要抵御边境蠢蠢欲动的魔族入侵。修真界分为五大仙门，各有所长，灵植峰作为仙界最重要的药材供给地，掌握着所有疗伤丹药的原料来源，地位特殊却也常年被各方势力觊觎。",
                one_line_note = "看起来软乎乎好说话，实则敢抱着灵草在乱葬岗躲三天的灵植峰小弟子",
                toneId = "",
                greeting_candidates = new List<string>
                {
                    "蹲在药田里直起腰，指尖还沾着新鲜的泥土，抬头冲你弯了弯眼，你是来求药的？先跟我说清楚症状，不对症的草药我可不会随便给。",
                    "正拿着小剪刀修剪灵草的枯叶，听见脚步声头也没抬，脚步放轻些，刚浇过的灵根脆弱得很，踩坏了就算你是掌门亲传也赔不起。",
                    "抬手擦了擦额角的汗，把刚晒好的草药捆成小堆，见你站在门口犹豫，主动扬了扬手，站在那做什么？是找峰主还是找我？有话直说就行。",
                },
                botMatchTags = new List<BotMatchTags>
                {
                    new BotMatchTags { categoryName = "语言", matchedTag = "中文", reason = "角色是东方修真世界观下的人物，适配中文表达。" },
                    new BotMatchTags { categoryName = "性别", matchedTag = "女声", reason = "设定为十七岁女性，女声契合性别属性。" },
                    new BotMatchTags { categoryName = "年龄", matchedTag = "少年", reason = "十七岁的修仙小弟子，符合少年年龄区间。" },
                    new BotMatchTags { categoryName = "特质", matchedTag = "清亮", reason = "气质干净如带露兰草，温软又有韧性，适配清亮音色。" },
                },
            };
            chatVoiceCom.SetData(botProfile);
        }


        [Button("测试奖励")]
        void testReward()
        {
            var pkg = AICreditPurchaseHelper.Packages[0];

            var items = new List<BoxRewardItemData>
            {
                new()
                {
                itemType    = BoxRewardItemData.ItemType.Common,
                specialIcon = specialIconType.aiCredit,
                commonData  = new CommonRewardItemData
                {
                    RewardAmount = pkg.energyAmount,
                    rewardName   = "AI能量",
                }
                }
            };

            if (pkg.bonusAmount > 0)
            {
                items.Add(new BoxRewardItemData
                {
                    itemType = BoxRewardItemData.ItemType.Common,
                    specialIcon = specialIconType.aiCredit,
                    tagType = TagType.zengsong,
                    commonData = new CommonRewardItemData
                    {
                        RewardAmount = pkg.bonusAmount,
                        rewardName = "AI能量",
                    }
                });
            }

            var rewardPanel = UIManager.Inst.OpenPanel<CommonBoxRewardPanel>(PanelId.CommonBoxRewardPanel);
            rewardPanel.ShowReward(items);
        }
    }
}