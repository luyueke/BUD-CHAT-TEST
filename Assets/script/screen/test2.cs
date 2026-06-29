using System.Collections;
using System.Collections.Generic;
using System.Runtime.Versioning;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Network;

public class test2 : MonoBehaviour
{
    public AICompanionChatPanel aICompanionChatPanel;
    public Button btn;
    public Button btnChat;
    void Start()
    {
        Debug.Log("[test2] Start begin");
        DeviceInfoManager.Inst.Init();
        UIManager.Inst.Init();
        Debug.Log("[test2] UIManager.Init done");

        if (btn == null) { Debug.LogError("[test2] btn is NULL — 请检查 Inspector 是否赋值"); return; }
        if (aICompanionChatPanel == null) Debug.LogError("[test2] aICompanionChatPanel is NULL — 请检查 Inspector 是否赋值");

        btn.onClick.AddListener(aaa);
        btnChat.onClick.AddListener(bbb);

        Debug.Log("[test2] btn.onClick listener added, btn.interactable=" + btn.interactable);

        // var panel = UIManager.Inst.OpenPanel<AICompanionChatPanel>(PanelId.AICompanionChatPanel, gameObject);
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        _ = ScreenOrientationHelper.Inst;
        NetworkManager.Inst.Init();
        NetworkManager.Inst.SetHttpUrl(GameEnvironment.PROD);
        SetHttpTokenInfo();
        Debug.Log("[test2] Start done");
    }

    void bbb()
    {
        ScreenOrientationHelper.Inst.Switch(
        ScreenOrientation.Portrait,
        onComplete: () =>
        {
            Debug.Log("[test2] onComplete — activating panel");
            aICompanionChatPanel.gameObject.SetActive(true);
            aICompanionChatPanel.OnShow(null);
        },
        onPreRotate: () => gameObject.SetActive(false));
    }

    void aaa()
    {
        Debug.Log("[test2] aaa() called — btn clicked");
        if (aICompanionChatPanel == null) { Debug.LogError("[test2] aICompanionChatPanel is NULL"); return; }

        Debug.Log("[test2] calling ScreenOrientationHelper.Switch to Portrait");
        ScreenOrientationHelper.Inst.Switch(
             ScreenOrientation.Portrait,
             onComplete: () =>
             {
                 Debug.Log("[test2] onComplete — activating panel");
                 aICompanionChatPanel.gameObject.SetActive(true);
                 aICompanionChatPanel.OnShow(null);
                 aICompanionChatPanel.BeginCreateRoleChat();
                 Debug.Log("[test2] BeginCreateRoleChat done");
             },
             onPreRotate: () =>
             {
                 Debug.Log("[test2] onPreRotate — hiding gameObject");
                 gameObject.SetActive(false);
             });
    }

    public void SetHttpTokenInfo()
    {
        string uid = "1815301222400552960";
        string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjIwNjA3NjMzNTYsImlhdCI6MTc0NTQwMzM1NiwiaXNzIjoiUlZGZWZYSGFOSnpHN21sdFhVc1NOZDY4dEJ6dzcxeEYiLCJuYmYiOjE3NDU0MDMzNTYsInBsIjoiZXlKMWNuWWlPaUlpTENKeWJDSTZNQ3dpWTJraU9pSWlMQ0oyYVhBaU9qQXNJbmQwSWpvd2ZRPT0iLCJ1aWQiOiIxODE1MzAxMjIyNDAwNTUyOTYwIn0.DQcsOFai-aMsQK8Zj2QISgkT2B1s44XO0aQeVnYzBcc";
        var tokenInfo = new Dictionary<string, string>();
        tokenInfo["uid"] = uid;
        tokenInfo["token"] = token;
        NetworkManager.Inst.SetHttpTokenInfo(tokenInfo);
    }
}
