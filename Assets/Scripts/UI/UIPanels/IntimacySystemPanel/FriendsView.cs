
using Game.GameHall.View;
using Message;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.UIPanels.FriendList;
using UnityEngine;
using UnityEngine.EventSystems;

public class FriendsView : MonoBehaviour
{
    public FriendListEntry friendListEntry;
    public SearchFriendEntry SearchFriendEntry;
    public GameObject friendScrollView;
    public GameObject searchScrollView;

    public CButton addFriendBtn;
    public CText onlineTxt;
    public CText searchTxt;
    public CButton searchBtn;
    public CButton searchInputBtn;
    public CButton closeSearchBtn;

    public GameObject noFriendObj;
    public GameObject loadingObj;
    public GameObject noSearchObj;

    private bool isInit = false;

    private string searchContent;

    private void Start()
    {
        friendListEntry.AddClickListener(OnScrollViewClick);
        addFriendBtn.onClick.AddListener(() =>
        {
            AddFriendPanel friendInfoPanel = UIManager.Inst.OpenPanel<AddFriendPanel>(PanelId.AddFriendPanel);

        });

        searchBtn.onClick.AddListener(() =>
        {
            Search();
        });

        searchInputBtn.onClick.AddListener(() =>
        {
            OnSearchBtnClick();
        });

        closeSearchBtn.onClick.AddListener(() =>
        {
            searchScrollView.gameObject.SetActive(false);
            friendScrollView.gameObject.SetActive(true);
            closeSearchBtn.gameObject.SetActive(false);
            // searchTxt.text = "";
            searchTxt.SetLocalText("搜索");
            noSearchObj.gameObject.SetActive(false);
        });

        MessageHelper.AddListener(MessageName.AddFriendSuccess, AddFriendSuccess);
        searchTxt.SetLocalText("搜索");
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.AddFriendSuccess, AddFriendSuccess);
    }

    private void AddFriendSuccess()
    {
        if (friendListEntry != null)
        {
            friendListEntry.GetFirstPageFriendDatas(OnHasFriends, OnUpdateOnline);
        }
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
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
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
        this.searchTxt.text = str;

        Search();
    }

    private void Search()
    {
        if (string.IsNullOrEmpty(this.searchContent))
        {
            return;
        }
        closeSearchBtn.gameObject.SetActive(true);
        searchScrollView.gameObject.SetActive(true);
        friendScrollView.gameObject.SetActive(false);
        loadingObj.gameObject.SetActive(true);
        SearchFriendEntry.GetFirstPageFriendDatas(RelationShipType.Friend,this.searchContent,(b =>
        {
            noFriendObj.gameObject.SetActive(false);
            noSearchObj.gameObject.SetActive(!b);
            loadingObj.gameObject.SetActive(false);
        }));
    }


    public void OnInitCreated()
    {
        if (isInit)
        {
            return;
        }

        isInit = true;
        loadingObj.gameObject.SetActive(true);
        onlineTxt.SetLocalText("{0} 在线",0 + "/" + 0);
        friendListEntry.GetFirstPageFriendDatas(OnHasFriends, OnUpdateOnline);
    }


    private void OnHasFriends(bool hasFriends)
    {
        loadingObj.gameObject.SetActive(false);
        noFriendObj.gameObject.SetActive(!hasFriends);
        noSearchObj.gameObject.SetActive(false);
    }

    private void OnUpdateOnline(RelationAmountInfo info)
    {
        if (info != null)
        {
            int onlineNum = info.onlineRelationAmount;
            int totalNum = info.relationAmount;
            onlineTxt.SetLocalText("{0} 在线",onlineNum + "/" + totalNum);
        }
    }

    private void OnScrollViewClick(PointerEventData data){

    }
}
