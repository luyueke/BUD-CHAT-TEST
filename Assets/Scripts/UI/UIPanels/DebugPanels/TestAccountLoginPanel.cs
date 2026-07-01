using System;
using UnityEngine;

/// <summary>
/// 测试账号登录面板
/// 使用 IMGUI 实现，无需 Prefab，所有包体（含正式包）均可用。
/// 在登录界面显示一个可见的「测试登录」按钮，点击后列出 10 个固定的游客测试账号。
/// 选中任一账号后，通过游客(Tourists)身份 + 固定 openId 直接登录后端，
/// 完全绕过原生 U8SDK 渠道登录与实名认证流程。
/// 同一个 openId 始终对应后端同一个账号，因此 10 个测试账号数据是持久的。
/// </summary>
public class TestAccountLoginPanel : MonoBehaviour
{
    /// <summary>测试账号数量</summary>
    public const int AccountCount = 10;

    /// <summary>固定 openId 前缀，保证与真实游客 openId 不冲突</summary>
    private const string OpenIdPrefix = "qa_test_account_";

    private static TestAccountLoginPanel _instance;
    public static bool InstExists => _instance != null;

    public static TestAccountLoginPanel Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("TestAccountLoginPanel");
                _instance = go.AddComponent<TestAccountLoginPanel>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 登录回调：参数为 (openId, 默认昵称)
    private Action<string, string> _onSelectAccount;

    private bool _entryVisible = false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 在登录界面直接显示 10 个测试账号按钮。
    /// </summary>
    /// <param name="onSelectAccount">选中账号后的登录回调 (openId, 昵称)</param>
    public void Show(Action<string, string> onSelectAccount)
    {
        _onSelectAccount = onSelectAccount;
        _entryVisible = true;
    }

    /// <summary>
    /// 隐藏测试登录按钮（登录成功或离开登录界面时调用）。
    /// </summary>
    public void Hide()
    {
        _entryVisible = false;
    }

    /// <summary>第 index(1 起) 个测试账号的固定 openId。</summary>
    public static string GetOpenId(int index)
    {
        return OpenIdPrefix + index.ToString("00");
    }

    /// <summary>第 index(1 起) 个测试账号的默认昵称。</summary>
    public static string GetNickname(int index)
    {
        return "测试账号" + index.ToString("00");
    }

    private void OnGUI()
    {
        if (!_entryVisible)
        {
            return;
        }

        // 依据屏幕高度做简单缩放，保证移动端高分屏下可读
        int fontSize = Mathf.Max(16, Mathf.RoundToInt(Screen.height / 38f));
        GUI.skin.button.fontSize = fontSize;
        GUI.skin.label.fontSize = fontSize;

        // 10 个账号按钮：两列 5 行，固定在右上角，避免遮挡中间主登录区域
        const int columns = 2;
        int rows = Mathf.CeilToInt(AccountCount / (float)columns);

        float btnW = Mathf.Max(150f, Screen.width * 0.12f);
        float btnH = Mathf.Max(56f, Screen.height * 0.085f);
        float gap = btnH * 0.18f;
        float margin = btnH * 0.35f;

        float titleH = btnH * 0.8f;
        float panelW = columns * btnW + (columns + 1) * gap;
        float panelH = titleH + rows * (btnH + gap) + gap;
        float panelX = Screen.width - panelW - margin;
        float panelY = margin;

        GUI.Box(new Rect(panelX, panelY, panelW, panelH), GUIContent.none);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(panelX, panelY + gap, panelW, titleH), "测试账号登录(免实名)", titleStyle);

        float gridTop = panelY + titleH + gap;
        for (int i = 0; i < AccountCount; i++)
        {
            int col = i % columns;
            int row = i / columns;
            float x = panelX + gap + col * (btnW + gap);
            float y = gridTop + row * (btnH + gap);

            int accountIndex = i + 1;
            if (GUI.Button(new Rect(x, y, btnW, btnH), GetNickname(accountIndex)))
            {
                OnAccountClicked(accountIndex);
            }
        }
    }

    private void OnAccountClicked(int accountIndex)
    {
        string openId = GetOpenId(accountIndex);
        string nickname = GetNickname(accountIndex);
        LoggerUtils.Log($"TestAccountLoginPanel: 选择测试账号 {nickname}, openId={openId}");
        _onSelectAccount?.Invoke(openId, nickname);
    }
}
