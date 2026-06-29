
using System;
using Game.GameHall.View;
using Message;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;

public class FollowView : MonoBehaviour
{

    public FollowListEntry FollowListEntry;
    public SearchFriendEntry SearchFriendEntry;
    
    public CText searchTxt;
    public CButton searchBtn;
    public CButton searchInputBtn;
    public CButton closeSearchBtn;
    
    public GameObject followScrollView;
    public GameObject searchScrollView;
    public GameObject noFriendObj;
    public GameObject loadingObj;
    public GameObject noSearchObj;

    private bool isInit = false;
    private string searchContent = "";

    private void Start()
    {
        FollowListEntry.AddClickListener(OnScrollViewClick);
                
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
            followScrollView.gameObject.SetActive(true);
            closeSearchBtn.gameObject.SetActive(false);
            // searchTxt.text = "";
            searchTxt.SetLocalText("搜索");
            noSearchObj.gameObject.SetActive(false);
        });
        
        MessageHelper.AddListener(MessageName.FollowSuccess, FollowSuccess);
        searchTxt.SetLocalText("搜索");
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.FollowSuccess, FollowSuccess);
    }

    private void FollowSuccess()
    {
        if (FollowListEntry != null)
        {
            FollowListEntry.GetFirstPageFriendDatas(OnHasFriends);
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
        followScrollView.gameObject.SetActive(false);
        loadingObj.gameObject.SetActive(true);
        SearchFriendEntry.GetFirstPageFriendDatas(RelationShipType.Follow,this.searchContent,(b =>
        {

            loadingObj.gameObject.SetActive(false);
            noFriendObj.gameObject.SetActive(false);
            noSearchObj.gameObject.SetActive(!b);
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
        FollowListEntry.GetFirstPageFriendDatas(OnHasFriends);
        
    }
    
    private void OnHasFriends(bool hasFriends)
    {
        noSearchObj.gameObject.SetActive(false);
        loadingObj.gameObject.SetActive(false);
        noFriendObj.gameObject.SetActive(!hasFriends);
    }
    
    private void OnScrollViewClick(PointerEventData data){
     
    }
}
