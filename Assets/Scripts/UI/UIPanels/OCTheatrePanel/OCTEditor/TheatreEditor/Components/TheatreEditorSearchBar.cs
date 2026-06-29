using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TheatreEditorSearchBar : MonoBehaviour
{
    [SerializeField] private Button typingBth;
    [SerializeField] private Button searchBth;
    [SerializeField] private GameObject hintObj;
    [SerializeField] private Text text;

    private Action<string> onSearch;
    private string currentText = "";

    /// <summary>
    /// 初始化搜索栏。每次页面 OnShow 时调用，替换回调。
    /// </summary>
    public void Init(Action<string> onSearchCallback)
    {
        onSearch = onSearchCallback;

        typingBth?.onClick.RemoveAllListeners();
        typingBth?.onClick.AddListener(OnTypingClicked);

        searchBth?.onClick.RemoveAllListeners();
        searchBth?.onClick.AddListener(OnSearchClicked);

        SetText("");
    }

    public void SetText(string t)
    {
        currentText = t ?? "";
        if (text != null) text.text = currentText;
        RefreshHint();
    }

    private void OnTypingClicked()
    {
        var info = new KeyBoardInfo
        {
            type = 0,
            inputMode = (int)KeyBoardInputMode.All,
            maxLength = 250,
            inputFlag = 0,
            defaultText = currentText,
            returnKeyType = (int)ReturnType.Done,
            textSecurity = 1
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboardInput);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(info));
    }

    private void OnKeyboardInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return;
        currentText = input;
        if (text != null) text.text = currentText;
        RefreshHint();
    }

    private void OnSearchClicked()
    {
        if (string.IsNullOrEmpty(currentText)) return;
        onSearch?.Invoke(currentText);
    }

    private void RefreshHint()
    {
        hintObj?.SetActive(string.IsNullOrEmpty(currentText));
    }
}
