using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Serialization;

public class LoginViewUS : MonoBehaviour
{
    public CButton CloseBtn;
    public LoadingButton GuestBtn;
    public LoadingButton AppleBtn;
    public LoadingButton FacebookBtn;
    public LoadingButton GoogleBtn;
    public CButton SnapBtn;
    public CButton TictokBtn;

    [SerializeField] private List<LoadingButton> loadingButtons;

    private Action<AccountPlatform> LoginBtnListener; 
    private void Start()
    {
        ResetAllLoader();
        CloseBtn.onClick.AddListener(OnClickClose);
        GuestBtn.onClick.AddListener(() => OnSelectLogin(GuestBtn,AccountPlatform.Tourists));
        AppleBtn.onClick.AddListener(() => OnSelectLogin(AppleBtn,AccountPlatform.Apple));
        FacebookBtn.onClick.AddListener(() => OnSelectLogin(FacebookBtn,AccountPlatform.Facebook));
        GoogleBtn.onClick.AddListener(() => OnSelectLogin(GoogleBtn,AccountPlatform.Google));
        SnapBtn.onClick.AddListener(() => OnSelectLogin(SnapBtn,AccountPlatform.Snapchat));
        TictokBtn.onClick.AddListener(() => OnSelectLogin(TictokBtn,AccountPlatform.TikTok));
    }

    public void Show()
    {
    
#if UNITY_IOS
        SetAppleLoginActive(true);
#else
        SetAppleLoginActive(false);
#endif
        ResetAllLoader();
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        ResetAllLoader();
        this.gameObject.SetActive(false);
    }

    public void AddLoginBtnListener(Action<AccountPlatform> callback)
    {
        LoginBtnListener += callback;
    }

    public void ClearLoginBtnListener()
    {
        LoginBtnListener = null;
    }

    private void OnClickClose()
    {
        Hide();
    }

    private void OnSelectLogin(CButton selectBtn,AccountPlatform platform)
    {
        if (selectBtn is LoadingButton)
        {
            var loading = (LoadingButton)selectBtn;
            loading.SetLoadingVisible(true);
        }
        //TODO:@Jaywill 调用SignInPanel 登录
        LoginBtnListener?.Invoke(platform);
    }

    public void SetAppleLoginActive(bool isActive)
    {
        AppleBtn.gameObject.SetActive(isActive);
    }

    public void ResetAllLoader()
    { 
        foreach (var loadingButton in loadingButtons)
        {
            loadingButton.SetLoadingVisible(false);
        }
    }
}
