using System;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 切换个人主页皮肤
/// </summary>
public class ChangeChatBubblePanel : BasePanel<ChangeChatBubblePanel>
{
    [SerializeField] private CButton BackBtn;
    [SerializeField] private LoadingButton SaveBtn;
    [SerializeField] private Transform Content;
    [SerializeField] private ChatBubbleThemeItem itemPrefab;
    
    private AccountUserInfo _accountUserInfo;
    private Action onSetSuccess;
    private Action onSetFail;

    private List<ChatBubbleThemeItem> items = new List<ChatBubbleThemeItem>();

    private int curSelectId = 0;

    public override void OnCreate()
    {
        SaveBtn.onClick.AddListener(OnConfirmClick);
        BackBtn.onClick.AddListener(OnBackBtnClick);
    }
    
    public override void OnShow(params object[] args)
    {
        if (args != null && args.Length > 0)
        {
            _accountUserInfo = args[0] as AccountUserInfo;
        }
        else
        {
            _accountUserInfo = AccountDataManager.Inst.UserInfo;
        }
        
        if (_accountUserInfo == null)
        {
            return;
        }

        InitView();

    }

    private void InitView()
    {
        if (_accountUserInfo.ownedChatBubblesList == null || _accountUserInfo.ownedChatBubblesList.Count <= 0)
        {
            int defaultId = (int)ProfileTheme.Default;
            var defaultItem = CreateItem(defaultId);
            defaultItem.SetSelect(true);
            curSelectId = defaultId;
            return;
        }
        
        _accountUserInfo.ownedChatBubblesList.Sort((a, b) => a.bubbleId.CompareTo(b.bubbleId));
        
        foreach (var chatBubble in _accountUserInfo.ownedChatBubblesList)
        {
            var item = CreateItem(chatBubble.bubbleId);
            if (chatBubble.bubbleId == _accountUserInfo.chatBubbles)
            {
                item.SetSelect(true);
                curSelectId = _accountUserInfo.chatBubbles;
            }
        }
    }

    private void OnConfirmClick()
    {
        if (_accountUserInfo == null)
        {
            return;
        }

        SaveBtn.SetLoadingVisible(true);
        AccountDataManager.Inst.SendSetChatBubbleRequest(curSelectId,OnChangedCallback);
    }
    
    
    private ChatBubbleThemeItem CreateItem(int themeId)
    {
        ChatBubbleThemeItem itemScript = Instantiate(itemPrefab, Content);
        itemScript.Init(themeId, OnItemClick);
        items.Add(itemScript);
        return itemScript;
    }

    private void OnItemClick(int themeId)
    {
        curSelectId = themeId;
        foreach (var item in items)
        {
            item.SetSelect(false);
        }
    }
    
    private void OnBackBtnClick()
    {
        CloseSelf();
    }

    private void CloseSelf()
    {
        UIManager.Inst.ClosePanel(this);
    }

    private void OnChangedCallback(bool isSuccess)
    {
        SaveBtn.SetLoadingVisible(false);
        if (isSuccess)
        {
            this.onSetSuccess?.Invoke();
            CloseSelf();
        }
        else
        {
            this.onSetFail?.Invoke();
        }
    }

    public void SetAction(Action onSuccess = null, Action onFail = null)
    {
        this.onSetSuccess = onSuccess;
        this.onSetFail = onFail;
    }
    
}

