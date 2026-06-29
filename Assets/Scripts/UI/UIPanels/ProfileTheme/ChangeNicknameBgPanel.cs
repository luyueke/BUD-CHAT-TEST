using System;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

/// <summary>
/// 切换
/// </summary>
public class ChangeNicknameBgPanel : BasePanel<ChangeNicknameBgPanel>
{
    [SerializeField] private CButton BackBtn;
    [SerializeField] private LoadingButton SaveBtn;
    [SerializeField] private Transform Content;
    [SerializeField] private NicknameBgItem itemPrefab;

    private AccountUserInfo _accountUserInfo;
    private Action onSetSuccess;
    private Action onSetFail;

    private List<NicknameBgItem> items = new List<NicknameBgItem>();

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
        if (_accountUserInfo.ownedNicknameFrameList == null || _accountUserInfo.ownedNicknameFrameList.Count <= 0)
        {
            int defaultId = 0;
            var defaultItem = CreateItem(0);
            defaultItem.SetSelect(true);
            curSelectId = defaultId;
            return;
        }

        _accountUserInfo.ownedNicknameFrameList.Sort((a, b) => a.frameId.CompareTo(b.frameId));

        foreach (var chatBubble in _accountUserInfo.ownedNicknameFrameList)
        {
            var item = CreateItem(chatBubble.frameId);
            if (chatBubble.frameId == _accountUserInfo.nicknameFrame)
            {
                item.SetSelect(true);
                curSelectId = _accountUserInfo.nicknameFrame;
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
        AccountDataManager.Inst.SendSetNicknameBgRequest(curSelectId, OnChangedCallback);
    }


    private NicknameBgItem CreateItem(int themeId)
    {
        NicknameBgItem itemScript = Instantiate(itemPrefab, Content);
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

