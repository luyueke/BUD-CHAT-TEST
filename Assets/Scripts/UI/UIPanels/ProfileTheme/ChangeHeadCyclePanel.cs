using System;
using System.Collections.Generic;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;


/// <summary>
/// 切换个人主页皮肤
/// </summary>
public class ChangeHeadCyclePanel : BasePanel<ChangeHeadCyclePanel>
{
    [SerializeField] private CButton BackBtn;
    [SerializeField] private LoadingButton SaveBtn;
    [SerializeField] private Transform Content;
    [SerializeField] private HeadCycleThemeItem itemPrefab;
    
    private AccountUserInfo _accountUserInfo;
    private Action onSetSuccess;
    private Action onSetFail;

    private List<HeadCycleThemeItem> items = new List<HeadCycleThemeItem>();

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
        if (_accountUserInfo.ownedAvatarFrameList == null || _accountUserInfo.ownedAvatarFrameList.Count <= 0)
        {
            int defaultId = (int)ProfileTheme.Default;
            var defaultItem = CreateItem(defaultId);
            defaultItem.SetSelect(true);
            curSelectId = defaultId;
            return;
        }
        _accountUserInfo.ownedAvatarFrameList.Sort((a, b) => a.frameId.CompareTo(b.frameId));
        foreach (var ownedAvatarFrame in _accountUserInfo.ownedAvatarFrameList)
        {
            var item = CreateItem(ownedAvatarFrame.frameId,ownedAvatarFrame.leftTime);
            if (ownedAvatarFrame.frameId == _accountUserInfo.avatarFrame)
            {
                item.SetSelect(true);
                curSelectId = _accountUserInfo.avatarFrame;
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
        AccountDataManager.Inst.SendSetAvatarFrameRequest(curSelectId, OnChangedCallback);
    }
    
    
    private HeadCycleThemeItem CreateItem(int themeId,string leftTime =null)
    {
        HeadCycleThemeItem itemScript = Instantiate(itemPrefab, Content);
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
            MessageHelper.Broadcast(MessageName.OnChangeHeadCycleSuccess);
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

