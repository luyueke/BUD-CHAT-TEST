using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Base;
using Game.Scene.EnterModelController;
using Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class LogOutPanel  : BasePanel<SettingPanel>
{
    private Text userName;
    private RemoteImageBehaviour headImg;
    private CButton logOutButton;
    private CButton logInButton;
    private Transform BG;
    public override void OnCreate()
    {
        base.OnCreate();
        BG = GameObjectEx.FindChildByName(transform, "Bg");
        userName = GameObjectEx.FindComponentByName<Text>(transform, "UserName");
        headImg= GameObjectEx.FindComponentByName<RemoteImageBehaviour>(transform, "UserHead");
        logOutButton= GameObjectEx.FindComponentByName<CButton>(transform, "LogOutButton");
        logInButton= GameObjectEx.FindComponentByName<CButton>(transform, "LogInButton");
        logOutButton.onClick.AddListener(OnLogOutClick);
        logInButton.onClick.AddListener(CloseSelf);

        OnInitBgUI();
        SetUserInfo();
    }
    
    private void OnInitBgUI()
    {
        if (BG == null)
        {
            LoggerUtils.LogError("[BG] Check GenderSelectPanel Bg Object");
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("FFFFFF", atlasPath, new List<string>()
        {
            "store_icon4", "store_icon5", "store_icon6"
        });
        item.gameObject.SetActive(true);
    }
    
    private void SetUserInfo()
    {
        AccountUserInfo userInfo = AccountDataManager.Inst.UserInfo;
        userName.text = userInfo.nickname;
        var path = userInfo.portraitUrl;
        if (!string.IsNullOrEmpty(path))
        {
            headImg.Load(path, true, (from, success) => { });
        }
    }
    private void OnLogOutClick()
    {
        CloseSelf();
        DeviceInfoManager.Inst.RemoveOldUserInfo();
        try
        {
            GameInstanceManager.Release();
        }catch(System.Exception e)
        {
            Debug.LogError("OnLogOutClick Exception e.message=" + e.Message);
        }
        AccountDataManager.Inst.DeleteCache();
        // 清掉记住的测试账号，登出后回到登录页显示账号选择（切换账号）；线上开关关闭时为空操作
        SignInPanel.ClearRememberedTestAccount();
        UIManager.Inst.ClosePanel(PanelId.GameHallPanel);
        UIManager.Inst.OpenPanel(PanelId.SignInPanel);
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.logout,"");
    }
   
  
}
