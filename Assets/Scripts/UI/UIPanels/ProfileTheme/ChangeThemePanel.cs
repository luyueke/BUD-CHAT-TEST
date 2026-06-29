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
public class ChangeThemePanel : BasePanel<ChangeThemePanel>
{
    [SerializeField] private CButton BackBtn;
    [SerializeField] private LoadingButton SaveBtn;
    [SerializeField] private Transform Content;
    [SerializeField] private ProfileThemeItem itemPrefab;
    
    private AccountUserInfo _accountUserInfo;
    private Action onSetSuccess;
    private Action onSetFail;

    private List<ProfileThemeItem> items = new List<ProfileThemeItem>();

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
        _accountUserInfo.ownedHomepageSkinList.Sort((a, b) => a.skinId.CompareTo(b.skinId));
        if (_accountUserInfo.ownedHomepageSkinList == null || _accountUserInfo.ownedHomepageSkinList.Count <= 0)
        {
            int defaultId = (int)ProfileTheme.Default;
            var defaultItem = CreateItem(defaultId);
            defaultItem.SetSelect(true);
            curSelectId = defaultId;
            return;
        }
        foreach (var homepageSkinInfo in _accountUserInfo.ownedHomepageSkinList)
        {
            var item = CreateItem(homepageSkinInfo.skinId);
            if (homepageSkinInfo.skinId == _accountUserInfo.homepageSkin)
            {
                item.SetSelect(true);
                curSelectId = _accountUserInfo.homepageSkin;
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
        AccountDataManager.Inst.SendSetProfileThemeReqeust(curSelectId,OnChangedCallback);
    }
    
    
    private ProfileThemeItem CreateItem(int themeId)
    {
        ProfileThemeItem itemScript = Instantiate(itemPrefab, Content);
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
