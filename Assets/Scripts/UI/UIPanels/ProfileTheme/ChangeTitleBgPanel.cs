using System;
using System.Collections.Generic;
using Game.Avatar;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;


/// <summary>
/// 切换个人主页皮肤
/// </summary>
public class ChangeTitleBgPanel : BasePanel<ChangeTitleBgPanel>
{
    [SerializeField] private CButton BackBtn;
    [SerializeField] private LoadingButton SaveBtn;
    [SerializeField] private Transform Content;
    [SerializeField] private TitleBgThemeItem itemPrefab;
    
    private AccountUserInfo _accountUserInfo;
    private Action onSetSuccess;
    private Action onSetFail;

    private List<TitleBgThemeItem> items = new List<TitleBgThemeItem>();

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
        //Debug.LogError("ownedTitleList =" + Newtonsoft.Json.JsonConvert.SerializeObject(_accountUserInfo.ownedTitleList));
        if (_accountUserInfo == null)
        {
            return;
        }

        InitView();

    }

    private void InitView()
    {
        if (_accountUserInfo.ownedTitleList == null || _accountUserInfo.ownedTitleList.Count <= 0)
        {
            int defaultId = (int)ProfileTheme.Default;
            var defaultItem = CreateItem(defaultId);
            defaultItem.SetSelect(true);
            curSelectId = defaultId;
            return;
        }

        _accountUserInfo.ownedTitleList.Sort((a, b) => a.titleId.CompareTo(b.titleId));
        foreach (var ownedAvatarFrame in _accountUserInfo.ownedTitleList)
        {
            var item = CreateItem(ownedAvatarFrame.titleId, ownedAvatarFrame.leftTime);
            if (ownedAvatarFrame.titleId == _accountUserInfo.titleId)
            {
                item.SetSelect(true);
                curSelectId = _accountUserInfo.titleId;
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
        AccountDataManager.Inst.SendSetTitleIdRequest(curSelectId, OnChangedCallback);
    }
    
    
    private TitleBgThemeItem CreateItem(int themeId,string leftTime =null)
    {
        TitleBgThemeItem itemScript = Instantiate(itemPrefab, Content);
        itemScript.Init(themeId,leftTime, OnItemClick);
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

            //AccountDataManager.Inst.UserInfo.avatarInfo.titleId = curSelectId;
            //AccountDataManager.Inst.UserInfo.avatarJson = CharacterData.SerializeObject(AccountDataManager.Inst.UserInfo.avatarInfo);

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

