using System;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorTextInput : MonoBehaviour
{
    [SerializeField] private SuperTextMesh text;
    [SerializeField] private Button button;
    [SerializeField] private Text placeholderText;
    [SerializeField] private Text charCountText;
    [SerializeField] private int maxLength = 100;
    [SerializeField] private bool multiLine;

    private string currentText = "";
    private string placeholder = "";
    private Action<string> onTextChanged;

    private bool _numericOnly;

    public void Init(string placeholderStr, int charLimit, Action<string> onChange, bool numericOnly = false)
    {
        placeholder = placeholderStr;
        maxLength = charLimit;
        onTextChanged = onChange;
        _numericOnly = numericOnly;

        if (button == null) button = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
        button?.onClick.RemoveAllListeners();
        button?.onClick.AddListener(OnButtonClicked);
        RefreshDisplay();
    }

    public void SetDisplayText(string displayText)
    {
        currentText = displayText ?? "";
        RefreshDisplay();
    }

    public void SetInteractable(bool interactable)
    {
        if (button == null) button = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
        if (button != null) button.interactable = interactable;
    }

    private void RefreshDisplay()
    {
        bool isEmpty = string.IsNullOrEmpty(currentText);

        if (text != null)
        {
            text.text = isEmpty ? "" : currentText;
            text.gameObject.SetActive(!isEmpty);
        }
        if (placeholderText != null)
        {
            placeholderText.text = placeholder;
            placeholderText.gameObject.SetActive(isEmpty);
        }
        if (charCountText != null)
            charCountText.text = $"{currentText.Length}/{maxLength}";
    }

    private void OnButtonClicked()
    {
        ShowKeyboardInput(placeholder, currentText, maxLength, multiLine, _numericOnly, result =>
        {
            currentText = result ?? "";
            RefreshDisplay();
            onTextChanged?.Invoke(currentText);
        });
    }

    public static void ShowQuickInput(string placeholder, string defaultText, Action<string> onResult)
    {
        ShowKeyboardInput(placeholder, defaultText, 100, false, false, onResult);
    }

    public static void ShowQuickInput(string placeholder, string defaultText, int charLimit, Action<string> onResult)
    {
        ShowKeyboardInput(placeholder, defaultText, charLimit, false, false, onResult);
    }

    private static void ShowKeyboardInput(string placeholder, string defaultText, int charLimit, bool multiLine, bool numericOnly, Action<string> onResult)
    {
        var keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = placeholder,
            inputMode = numericOnly ? (int)KeyBoardInputMode.Number : multiLine ? (int)KeyBoardInputMode.All : (int)KeyBoardInputMode.SingleLine,
            maxLength = charLimit,
            inputFlag = 0,
            lengthTips = "字数超出限制",
            defaultText = defaultText ?? "",
            returnKeyType = (int)ReturnType.Send,
            textSecurity = 0,
            isFilterEmoji = 0,
        };

        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, str =>
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            onResult?.Invoke(str);
        });
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }
}
