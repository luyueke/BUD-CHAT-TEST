
using System;
using UnityEngine;
using UnityEngine.UI;

public enum TheatreContentType
{
    None = 0,
    Text = 1,
    Option = 2,
}

public class TheatreEditorContentInput : MonoBehaviour
{
    private const int maxDisplayText = 10;
    [SerializeField] private TheatreContentType type = TheatreContentType.None;
    [SerializeField] private SuperTextMesh text;
    [SerializeField] private Button textBtn;
    [SerializeField] private GameObject hintGObject;
    [SerializeField] private GameObject settingOptionBoard;
    [SerializeField] private Button openSettingBtn;
    [SerializeField] private Button goUpBtn;
    [SerializeField] private Button deleteBtn;
    [SerializeField] private Button jumpBtn; // 仅 Option 类型使用

    public Action onBoardOpen;
    public Action onBoardClose;

    private string _fullText = "";
    private Action onDelete;
    private Action onGoUp;
    private Action onJump;
    private Action<string> onTextChanged;

    public TheatreContentType ContentType => type;

    public void Init(TheatreContentType contentType, string displayText,
        Action<string> onChanged, Action onDeleteAction, Action onGoUpAction, Action onJumpAction = null)
    {
        type = contentType;
        onTextChanged = onChanged;
        onDelete = onDeleteAction;
        onGoUp = onGoUpAction;
        onJump = onJumpAction;

        settingOptionBoard?.SetActive(false);

        // jumpBtn 只有 Option 类型才用
        if (jumpBtn != null) jumpBtn.gameObject.SetActive(type == TheatreContentType.Option);

        textBtn?.onClick.RemoveAllListeners();
        textBtn?.onClick.AddListener(OnTextBtnClicked);

        openSettingBtn?.onClick.RemoveAllListeners();
        openSettingBtn?.onClick.AddListener(() =>
        {
            bool show = !(settingOptionBoard?.activeSelf ?? false);
            settingOptionBoard?.SetActive(show);
            if (show) onBoardOpen?.Invoke();
        });

        goUpBtn?.onClick.RemoveAllListeners();
        goUpBtn?.onClick.AddListener(() =>
        {
            settingOptionBoard?.SetActive(false);
            onBoardClose?.Invoke();
            onGoUp?.Invoke();
        });

        deleteBtn?.onClick.RemoveAllListeners();
        deleteBtn?.onClick.AddListener(() =>
        {
            settingOptionBoard?.SetActive(false);
            onBoardClose?.Invoke();
            onDelete?.Invoke();
        });

        if (jumpBtn != null)
        {
            jumpBtn.onClick.RemoveAllListeners();
            jumpBtn.onClick.AddListener(() =>
            {
                settingOptionBoard?.SetActive(false);
                onJump?.Invoke();
            });
        }

        SetText(displayText);
    }

    public void CloseSettingBoard() => settingOptionBoard?.SetActive(false);

    public void SetText(string displayText)
    {
        string val = displayText ?? "";
        _fullText = val;
        bool isEmpty = string.IsNullOrEmpty(val);

        if (text != null)
        {
            string shown = (type == TheatreContentType.Option && val.Length > maxDisplayText)
                ? val.Substring(0, maxDisplayText) + "..."
                : val;
            text.text = shown;
            text.gameObject.SetActive(!isEmpty);
        }
        hintGObject?.SetActive(isEmpty);
    }

    private void OnTextBtnClicked()
    {
        string placeholder = type == TheatreContentType.Option ? "输入选项内容" : "输入对话内容";
        int charLimit = type == TheatreContentType.Option ? 12 : 100;
        TheatreEditorTextInput.ShowQuickInput(placeholder, _fullText, charLimit, result =>
        {
            string val = result ?? "";
            SetText(val);
            onTextChanged?.Invoke(val);
        });
    }
}
