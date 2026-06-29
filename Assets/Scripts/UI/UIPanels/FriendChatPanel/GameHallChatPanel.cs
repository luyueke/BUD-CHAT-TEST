using System;
using System.Collections;
using System.Collections.Generic;
using Game.AINPCStudio;
using Game.Avatar;
using Game.Config;
using Message;
using Network;
using Network.Http;
using Network.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using View.UI.PopupPanelSystem;

public class GameHallChatPanel : BasePanel<GameHallChatPanel>
{
    public Button closeBtn;
    public Transform itemContent;

    public Button inputButton;
    public Text inputText;
    public GameObject emptyTip;
    private string InputHandle = "点击输入";
    public Button newMessageBtn;
    public ChatFriendList chatFriendList;
    public ChatScrollRect scrollRect;
    public Text friendName;
    public GameObject clickTip;
    public GameObject inputArea;
    public GameObject ChatPanel;
    [HideInInspector]public AccountUserInfo otherInfo;
    public AIBuddyChatWidget BuddyChatWidget;
    
    private List<OfflineMessageItem> allChatDatas = new List<OfflineMessageItem>();
    private ConversationListItem curConversationListItem;
    private string curUid;
    private string _userId = "";

    public override void OnCreate()
    {
        base.OnCreate();
        MessageHelper.AddListener<TcpChatData>(MessageName.ChatMessage, ReceiveMessage);

        scrollRect.onValueChanged.AddListener((itemContent) =>
        {
            if (scrollRect.verticalNormalizedPosition <= 0)
            {
                newMessageBtn.gameObject.SetActive(false);
            }
        });
        closeBtn.onClick.AddListener(() =>
        {
            ReddotManagerUtils.Inst.RefreshRedDot();

            CloseSelf();
        });
        newMessageBtn.onClick.AddListener(OnNewMessageButtonClick);
        inputButton.onClick.AddListener(OnInputButtonClick);
        inputText.SetLocalText(InputHandle);
        inputArea.gameObject.SetActive(false);
        BuddyChatWidget.Init();
    }

    public override void OnShow(params object[] args)
    {
        if (args != null && args.Length > 0)
        {
            _userId = args[0] as string;
        }

        chatFriendList.gameObject.SetActive(true);
        chatFriendList.OnInitCreated(_userId, conversationListItem => { SetMessagePanel(conversationListItem); },
            hasFriend =>
            {
                if (_userId != null && !string.IsNullOrEmpty(_userId))
                {
                    chatFriendList?.ClickFirst();
                }
            }, isSearch =>
            {
                inputArea.gameObject.SetActive(false);
                scrollRect.gameObject.SetActive(false);
                clickTip.gameObject.SetActive(true);
                friendName.gameObject.SetActive(false);
            });
    }

    private void ReceiveMessage(TcpChatData chatData)
    {
        if (curConversationListItem == null)
        {
            return;
        }

        if (curConversationListItem.uid != chatData.toUid)
        {
            if (chatFriendList == null)
            {
                return;
            }

            TextChatData textChatData = JsonConvert.DeserializeObject<TextChatData>(chatData.data);
            chatFriendList.SetFirst(textChatData);
        }
        else
        {
            List<OfflineMessageItem> chatDatas = new List<OfflineMessageItem>();
            string showName = curConversationListItem.nickname;

            TextChatData textChatData = new TextChatData();
            if (!string.IsNullOrEmpty(chatData.data))
            {
                textChatData = JsonConvert.DeserializeObject<TextChatData>(chatData.data);
            }
            
            OfflineMessageItem friendMessage = new OfflineMessageItem()
            {
                uid = curConversationListItem.uid,
                nickname = showName,
                portraitUrl = curConversationListItem.portraitUrl,
                data = chatData.data,
                chatBubbles = textChatData.chatBubbles,
                avatarFrame = textChatData.avatarFrame,
                nicknameFrame = textChatData.nicknameFrame
            };
            chatDatas.Add(friendMessage);
            scrollRect.AddMsg(friendMessage);
            scrollRect.ProcMovement();
            allChatDatas.Add(friendMessage);

            ChatDataManager.Inst.SetReadMessage(curConversationListItem.uid);
            emptyTip.SetActive(false);
        }
    }

    private void SetSelfContent(string str)
    {
        List<OfflineMessageItem> chatDatas = new List<OfflineMessageItem>();
        string showName = AccountDataManager.Inst.UserInfo.nickname;
        OfflineMessageItem selfChatData = new OfflineMessageItem()
        {
            uid = AccountDataManager.Inst.Uid,
            nickname = showName,
            portraitUrl = AccountDataManager.Inst.UserInfo.portraitUrl,
            data = str,
            chatBubbles = AccountDataManager.Inst.UserInfo.chatBubbles,
            avatarFrame = AccountDataManager.Inst.UserInfo.avatarFrame,
            nicknameFrame = AccountDataManager.Inst.UserInfo.nicknameFrame
        };
        chatDatas.Add(selfChatData);
        scrollRect.AddMsg(selfChatData);
        scrollRect.ProcMovement();
        GameHallChatManager.Inst.SetSelfContents(chatDatas);
        allChatDatas.Add(selfChatData);

        if (chatFriendList != null)
        {
            chatFriendList.MoveItemToTop(curUid);
        }
    }

    public void OnNewMessageButtonClick()
    {
        newMessageBtn.gameObject.SetActive(false);
        StartCoroutine(SetContentToButtom());
    }

    public void OnInputButtonClick()
    {
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            source = Game.Base.GameController.IsInHallScene() ? (int)KeyboardSource.HallChat : (int)KeyboardSource.RoomChat,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入文字"),
            inputMode = 2,
            maxLength = 250,
            inputFlag = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("不能超过250字符"),
            defaultText = "",
            returnKeyType = (int)ReturnType.Send
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnShowKeyBoard);
        MobileInterface.Instance.AddClientFail(MobileInterfaceDefine.showKeyboard, OnKeyboardAuditFail);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }

    private void OnShowKeyBoard(string str)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        try
        {
            var response = JsonConvert.DeserializeObject<AuditResponse>(str);
            if (response != null && response.result == 0)
            {
                string textToSend = response.data?.filteredText ?? str;
                SendMessage(textToSend);
                inputText.SetLocalText(InputHandle);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析审核响应失败：{e.Message}");
            SendMessage(str);
            inputText.SetLocalText(InputHandle);
        }
    }

    private void OnKeyboardAuditFail(string failMsg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.showKeyboard);
        UIManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel_Style2, new CommonSingleConfirmPanel_Style2Data()
        {
            CanClose = true,
            ConfirmString = "确定",
            ContextString = failMsg,
            TopTitleString = "提示",
        });
    }

    private void SendMessage(string message)
    {
        emptyTip.SetActive(false);
        TextChatData textChatData = new TextChatData()
        {
            fromUid = AccountDataManager.Inst.Uid,
            toUid = curUid,
            message = message,
            nickName = curConversationListItem.nickname,
            portraitUrl = curConversationListItem.portraitUrl,
            chatBubbles = AccountDataManager.Inst.UserInfo.chatBubbles,
            avatarFrame = AccountDataManager.Inst.UserInfo.avatarFrame,
            nicknameFrame = AccountDataManager.Inst.UserInfo.nicknameFrame,
        };
        SetChatReq setChatReq = new SetChatReq()
        {
            setType = 1,
            toUid = curUid,
            msgType = 1,
            msgData = JsonConvert.SerializeObject(textChatData)
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetChat,
            HttpMethod.POST,
            JsonConvert.SerializeObject(setChatReq),
            onReceive: msg => { SetSelfContent(JsonConvert.SerializeObject(textChatData)); }, onFail: arg0 => { });
    }

    IEnumerator SetContentToButtom()
    {
        yield return new WaitForSeconds(0.1f);
        scrollRect.verticalNormalizedPosition = 0;
    }

    public void InitContents(List<OfflineMessageItem> contents)
    {
        emptyTip.SetActive(contents.Count <= 0);
        foreach (Transform child in itemContent)
        {
            Destroy(child.gameObject);
        }

        scrollRect.AddMsgOnInit(contents);
    }

    private void SetMessagePanel(ConversationListItem conversationListItem)
    {
        if (conversationListItem == null)
        {
            return;
        }

        scrollRect.gameObject.SetActive(false);
        clickTip.gameObject.SetActive(false);
        inputArea.gameObject.SetActive(true);
        friendName.gameObject.SetActive(true);
        curConversationListItem = conversationListItem;
        friendName.text = conversationListItem.nickname;
        string uid = conversationListItem.uid;
        curUid = uid;
        RequestMessageList(uid);
        if (chatFriendList != null)
        {
            chatFriendList.SetIsRead(uid);
            chatFriendList.SetSelect(uid);
        }

        if (conversationListItem.uid == GameConsts.AIBuddyTag)
        {
            BuddyChatWidget.gameObject.SetActive(true);
            BuddyChatWidget.SetData(AIBuddyAvatarController.Inst.SelfAIBuddyInfo);
            ChatPanel.SetActive(false);
        }
        else
        {
            BuddyChatWidget.gameObject.SetActive(false);
            otherInfo = conversationListItem.userInfo;
            ChatPanel.SetActive(true);
        }
    }

    private void RequestMessageList(string touid)
    {
        allChatDatas.Clear();
        JObject jobject = new JObject()
        {
            ["toUid"] = touid
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.OfflineMsg,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jobject),
            onReceive: msg =>
            {
                scrollRect.gameObject.SetActive(true);
                OfflineMessageResponse resourceInfo =
                    JsonConvert.DeserializeObject<OfflineMessageResponse>(msg);
                if (resourceInfo.list != null)
                {
                    List<OfflineMessageItem> offlineMessageItems = resourceInfo.list;
                    for (int i = 0; i < offlineMessageItems.Count; i++)
                    {
                        string uid = offlineMessageItems[i].uid;
                        if (AccountDataManager.Inst.IsSelf(uid))
                        {
                            offlineMessageItems[i].nickname = AccountDataManager.Inst.UserInfo.nickname;
                            offlineMessageItems[i].portraitUrl = AccountDataManager.Inst.UserInfo.portraitUrl;
                            offlineMessageItems[i].chatBubbles =  AccountDataManager.Inst.UserInfo.chatBubbles;
                            offlineMessageItems[i].avatarFrame =  AccountDataManager.Inst.UserInfo.avatarFrame;
                            offlineMessageItems[i].nicknameFrame = AccountDataManager.Inst.UserInfo.nicknameFrame;
                        }
                        else
                        {
                            offlineMessageItems[i].nickname = curConversationListItem.nickname;
                            offlineMessageItems[i].portraitUrl = curConversationListItem.portraitUrl;
                            offlineMessageItems[i].chatBubbles = resourceInfo.chatBubbles;
                            offlineMessageItems[i].avatarFrame = resourceInfo.avatarFrame;
                            offlineMessageItems[i].nicknameFrame = resourceInfo.nicknameFrame;
                        }
                    }

                    allChatDatas.AddRange(offlineMessageItems);
                    InitContents(allChatDatas);
                    scrollRect.verticalNormalizedPosition = 0;
                }
                else
                {
                    emptyTip.gameObject.SetActive(false);
                }
            }, onFail: arg0 => { });
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        MessageHelper.RemoveListener<TcpChatData>(MessageName.ChatMessage, ReceiveMessage);
    }

    private class AuditResponse
    {
        public int result { get; set; }
        public string rmsg { get; set; }
        public string requestId { get; set; }
        public AuditData data { get; set; }
    }

    private class AuditData
    {
        public int auditResult { get; set; }
        public List<string> relatedFilteredTextList { get; set; }
        public string filteredText { get; set; }
    }
}
