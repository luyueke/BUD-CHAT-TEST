using System.Collections.Generic;
using System.Reflection;
using Network;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TestAccountSwitch : MonoBehaviour
{
    public InputField uidInputField;
    public InputField tokenInputField;
    public CButton loginBtn;

    private void Awake()
    {
        loginBtn.onClick.AddListener(OnSwicthAccount);
    }

    void OnSwicthAccount()
    {
        var uid = uidInputField.text;
        var token = tokenInputField.text;
        AccountDataManager.Inst.Release();
        var tokenFieldInfo = AccountDataManager.Inst.GetType().GetField("token",
            BindingFlags.NonPublic | BindingFlags.Instance);
        tokenFieldInfo.SetValue(AccountDataManager.Inst, token);
        AccountDataManager.Inst.CreateUserInfo();
        AccountDataManager.Inst.UserInfo.uid = uid; 

        var tokenInfo = new Dictionary<string, string>();
        tokenInfo["uid"] = uid;
        tokenInfo["token"] = token;
        NetworkManager.Inst.SetHttpTokenInfo(tokenInfo);
        AccountDataManager.Inst.RefreshUserInfo();
        UIManager.Inst.ClosePanel(WindowId.TestWindow, PanelId.TestPanel1);
        AccountDataManager.Inst.AddUserInfoChangeListener((info) =>
        {
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.SignInWindow, true);
            UIManager.Inst.OpenPanel(PanelId.GameHallPanel);
            UIManager.Inst.ClosePanel(PanelId.SignInPanel);
        });
    }
}