
using System;
using Game.GameHall.View;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.UIPanels.FriendList;
using UnityEngine;

public class ChatFriendList : MonoBehaviour
{
    public ChatFriendListEntry friendListEntry;

    public GameObject noFriendObj;
    public GameObject noSearchObj;
    public GameObject loadingObj;
    public CButton addFriendBtn;
    public CText searchTxt;
    public CButton searchInputBtn;
    public CButton closeSearchBtn;

    private bool isInit = false;
    private string searchContent;

    private Action<ConversationListItem> _action;
    private Action<bool> _onHasFriends;
    private Action<bool> _onSearch;
    private string userId = "";


    private void Start()
    {
        searchContent = "";
        searchTxt.SetLocalText("搜索");
        addFriendBtn.onClick.AddListener(() =>
        {
            AddFriendPanel friendInfoPanel = UIManager.Inst.OpenPanel<AddFriendPanel>(PanelId.AddFriendPanel);
        });


        searchInputBtn.onClick.AddListener(() =>
        {
            OnSearchBtnClick();
        });

        closeSearchBtn.onClick.AddListener(() =>
        {
            closeSearchBtn.gameObject.SetActive(false);
            searchTxt.SetLocalText("搜索");
            searchContent = "";
            searchTxt.color = DataUtil.DeSerializeColorCheckHash("#6B6B6B");

            loadingObj.gameObject.SetActive(true);
            GetFirstPage("",searchContent);
            _onSearch?.Invoke(true);
        });
    }

    void OnSearchBtnClick()
    {
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("搜索"),
            inputMode = 2,
            maxLength = 60,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips =  LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            defaultText = "",
            returnKeyType = (int)ReturnType.Done
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
        this.searchContent = str;
        this.searchTxt.SetText(str);
        searchTxt.color = DataUtil.DeSerializeColorCheckHash("#121212");

        Search();
    }

    private void Search()
    {
        _onSearch.Invoke(true);
        closeSearchBtn.gameObject.SetActive(true);
        loadingObj.gameObject.SetActive(true);
        GetFirstPage("",searchContent);
    }

    public void OnInitCreated(string userId,Action<ConversationListItem> action, Action<bool> OnHasFriends, Action<bool> OnSearch)
    {
        if (isInit)
        {
            return;
        }

        isInit = true;

        this._onHasFriends = OnHasFriends;
        this._onSearch = OnSearch;
        this._action = action;
        this.userId = userId;
        GetFirstPage(this.userId, "");
    }

    private void GetFirstPage(string toUid, string searchWord)
    {
        loadingObj.gameObject.SetActive(true);
        friendListEntry.GetFirstPageFriendDatas(b =>
        {
            _onHasFriends.Invoke(b);
            this.OnHasFriends(b);
        }, toUid, searchWord);
        friendListEntry.SetAction(_action);
    }

    public void ClickFirst()
    {
        friendListEntry?.ClickFirst();
    }

    private void OnHasFriends(bool hasFriends)
    {
        loadingObj.gameObject.SetActive(false);
        if (string.IsNullOrEmpty(searchContent))
        {
            noSearchObj.gameObject.SetActive(false);
            noFriendObj.gameObject.SetActive(!hasFriends);
        }
        else
        {
            noFriendObj.gameObject.SetActive(false);
            noSearchObj.gameObject.SetActive(!hasFriends);
        }
    }

    public void SetFirst(TextChatData textChatData)
    {
        if (friendListEntry == null)
        {
            return;
        }
        friendListEntry.SetFirst(textChatData);
    }

    public void SetIsRead(string fromUid)
    {
        if (friendListEntry == null)
        {
            return;
        }
        friendListEntry.SetIsRead(fromUid);
    }

    public void SetSelect(string fromUid)
    {
        if (friendListEntry == null)
        {
            return;
        }
        friendListEntry.SetSelect(fromUid);
    }

    public void MoveItemToTop(string uid)
    {
        if (friendListEntry != null)
        {
            friendListEntry.MoveItemToTop(uid);
        }
    }

}
