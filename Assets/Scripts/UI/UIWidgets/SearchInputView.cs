using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 搜索输入框组件，封装键盘调起、文本显示、清除按钮。
/// 将 input_search / txt_search / btn_search_closs 三个子节点拖入对应字段即可复用。
/// </summary>
public class SearchInputView : MonoBehaviour
{
    [SerializeField] private Button inputBtn;
    [SerializeField] private Text txt;
    [SerializeField] private CButton clearBtn;
    [SerializeField] private CButton searchBtn;
    [SerializeField] private string placeholder = "搜索";
    [SerializeField] private int maxLength = 100;

    public string Text { get; private set; } = "";

    private Action<string> _onConfirm;
    private Action _onClear;

    private void Awake()
    {
        inputBtn.onClick.AddListener(OnInputClick);
        clearBtn.onClick.AddListener(OnClearClick);
        searchBtn.onClick.AddListener(OnSearchBtnClick);
        clearBtn.gameObject.SetActive(false);
    }

    private static readonly Color ColorPlaceholder = new Color(0xCC / 255f, 0xCC / 255f, 0xCC / 255f);
    private static readonly Color ColorInput = Color.white;

    private void Start()
    {
        SetTxt(placeholder, isPlaceholder: true);
    }

    /// <summary>键盘确认后触发，参数为输入文本</summary>
    public void SetOnConfirm(Action<string> callback)
    {
        _onConfirm = callback;
    }

    /// <summary>点击清除按钮时触发</summary>
    public void SetOnClear(Action callback)
    {
        _onClear = callback;
    }

    private void SetTxt(string content, bool isPlaceholder)
    {
        txt.text = content;
        txt.color = isPlaceholder ? ColorPlaceholder : ColorInput;
    }

    public void SetPlaceholder(string text)
    {
        placeholder = text;
        if (string.IsNullOrEmpty(Text))
            SetTxt(placeholder, isPlaceholder: true);
    }

    /// <summary>外部主动清空，不触发 OnClear 回调</summary>
    public void ClearWithoutNotify()
    {
        Text = "";
        SetTxt(placeholder, isPlaceholder: true);
        clearBtn.gameObject.SetActive(false);
    }

    private void OnInputClick()
    {
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = placeholder,
            inputMode = (int)KeyBoardInputMode.SingleLine,
            maxLength = maxLength,
            inputFlag = 0,
            textSecurity = 1,
            isFilterEmoji = 1,
            returnKeyType = (int)ReturnType.Done,
            defaultText = Text
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboardInput);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }

    private void OnKeyboardInput(string input)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        Text = input ?? "";
        SetTxt(string.IsNullOrEmpty(Text) ? placeholder : Text, isPlaceholder: string.IsNullOrEmpty(Text));
        clearBtn.gameObject.SetActive(!string.IsNullOrEmpty(Text));
        OnSearchBtnClick();
    }

    private void OnSearchBtnClick()
    {
        _onConfirm?.Invoke(Text);
    }

    private void OnClearClick()
    {
        Text = "";
        SetTxt(placeholder, isPlaceholder: true);
        clearBtn.gameObject.SetActive(false);
        _onClear?.Invoke();
    }
}
