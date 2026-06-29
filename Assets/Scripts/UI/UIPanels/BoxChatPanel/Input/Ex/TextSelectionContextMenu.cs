using UnityEngine;
using UnityEngine.UI;

public class TextSelectionContextMenu : MonoBehaviour
{
    public GameObject menuPanel;
    public Button copyButton;
    public Button cutButton;
    public Button pasteButton;
    public Button selectAllButton;

    [HideInInspector] public AdvancedInputField currentInputField;
    [HideInInspector] public MultiLineInputFieldSelection multiLineInputFieldSelection;

    void Start()
    {
        // 绑定按钮事件
        copyButton.onClick.AddListener(OnCopyClicked);
        cutButton.onClick.AddListener(OnCutClicked);
        pasteButton.onClick.AddListener(OnPasteClicked);
        selectAllButton?.onClick?.AddListener(OnSelectAllClicked);

        currentInputField.inputField.onValueChanged.AddListener(OnValueChanged);
        // 初始隐藏菜单
        menuPanel.SetActive(false);
    }

    void OnValueChanged(string text)
    {
        Hide();
        // UpdateButtonStates();
    }

    public bool CheckCanShow()
    {
        // 根据是否有文本选择和剪贴板内容更新按钮状态
        bool hasSelection = currentInputField.HasSelection();
        bool canHighlight = multiLineInputFieldSelection.canHighlight;
        bool hasClipboardContent = !string.IsNullOrEmpty(GUIUtility.systemCopyBuffer);

        return (hasSelection && canHighlight) || hasClipboardContent;
    }

    public void ShowAtPosition(Vector2 position)
    {
        if (!CheckCanShow())
        {
            Hide();
            return;
        }
        // 更新菜单位置
        RectTransform rectTransform = menuPanel.GetComponent<RectTransform>();
        // rectTransform.position = position;  //暂时不用

        // 根据选择状态更新按钮可用性
        UpdateButtonStates();

        menuPanel.SetActive(true);
    }

    public void Hide()
    {
        menuPanel.SetActive(false);
    }

    private void UpdateButtonStates()
    {
        // 根据是否有文本选择和剪贴板内容更新按钮状态
        bool hasSelection = currentInputField.HasSelection();
        bool canHighlight = multiLineInputFieldSelection.canHighlight;
        bool hasClipboardContent = !string.IsNullOrEmpty(GUIUtility.systemCopyBuffer);

        // if (currentInputField.inputField.isFocused)
        // {
        //     if (!hasSelection && hasClipboardContent || hasSelection)
        //     {
        //         this.transform.localScale = Vector3.one;
        //     }
        //     else
        //     {
        //         this.transform.localScale = Vector3.zero;
        //     }
        // }
        // else
        // {
        //     this.transform.localScale = Vector3.zero;
        // }

        copyButton.interactable = hasSelection && canHighlight;
        cutButton.interactable = hasSelection && canHighlight;
        pasteButton.interactable = hasClipboardContent;
    }

    private void OnCopyClicked()
    {
        currentInputField?.Copy();
    }

    private void OnCutClicked()
    {
        currentInputField?.Cut();
    }

    private void OnPasteClicked()
    {
        currentInputField?.Paste();
    }

    private void OnSelectAllClicked()
    {
        currentInputField?.SelectAll();
    }

    // 点击菜单外部关闭
    public void OnBackdropClicked()
    {
        Hide();
    }
}