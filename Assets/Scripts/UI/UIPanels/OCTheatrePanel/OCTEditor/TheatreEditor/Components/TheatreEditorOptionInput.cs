using System;
using System.Collections.Generic;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 管理 Section 的选项内容（dialogue.Type==2，options 列表）。
/// 最多 4 个选项，最少 0 个。
/// optionSectionInput 对应 option dialogue 的 Text 字段（对话文本）。
/// </summary>
public class TheatreEditorOptionInput : MonoBehaviour
{
    private const int MaxOptionNumber = 4;

    [SerializeField] private TheatreEditorContentInput contentPrefab;
    [SerializeField] private TheatreEditorTextInput optionSectionInput;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Button addNewBtn;
    [SerializeField] private Text btnText;

    public Action onBoardOpen;
    public Action onBoardClose;

    private readonly List<TheatreEditorContentInput> contentItems = new();
    private POCTheatreSection currentSection;
    private TheatreEditorDataCenter dataCenter;
    private Action<int> onJumpToOptionEdit; // 参数为 optionIndex

    public void CloseAllBoards()
    {
        foreach (var item in contentItems)
            item?.CloseSettingBoard();
    }

    public void Init(TheatreEditorDataCenter dc, Action<int> onJumpOption)
    {
        dataCenter = dc;
        onJumpToOptionEdit = onJumpOption;

        addNewBtn?.onClick.RemoveAllListeners();
        addNewBtn?.onClick.AddListener(OnAddNewClicked);
    }

    public void Refresh(POCTheatreSection section)
    {
        currentSection = section;

        // 刷新 option dialogue 的 Text（optionSectionInput）
        optionSectionInput?.Init("输入对话/旁白", 100, text =>
        {
            if (dataCenter == null || currentSection == null) return;
            bool hadNoOptions = (dataCenter.GetOptionDialogue(currentSection)?.Options.Count ?? 0) == 0;
            dataCenter.SetOptionDialogueText(currentSection, text);
            // 有内容时首次自动创建了空选项，需同步刷新编辑器内的选项列表
            if (!string.IsNullOrEmpty(text) && hadNoOptions)
                RebuildItems();
        });
        optionSectionInput?.SetDisplayText(dataCenter?.GetOptionDialogue(section)?.Text ?? "");

        RebuildItems();
    }

    private void RebuildItems()
    {
        ClearItems();
        if (currentSection == null || contentPrefab == null || contentRoot == null) return;

        var optionDialogue = dataCenter?.GetOptionDialogue(currentSection);
        if (optionDialogue == null)
        {
            RefreshBtnText(0);
            return;
        }

        for (int i = 0; i < optionDialogue.Options.Count; i++)
        {
            var option = optionDialogue.Options[i];
            int capturedIndex = i;

            var obj = Instantiate(contentPrefab.gameObject, contentRoot);
            var item = obj.GetComponent<TheatreEditorContentInput>();
            item.onBoardOpen = onBoardOpen;
            item.onBoardClose = onBoardClose;
            item.Init(
                TheatreContentType.Option,
                option.Text,
                text => dataCenter?.SetOptionText(currentSection, capturedIndex, text),
                () => OnDeleteOption(capturedIndex),
                () => OnGoUpOption(capturedIndex),
                () => onJumpToOptionEdit?.Invoke(capturedIndex)
            );
            contentItems.Add(item);
            obj.SetActive(true);
        }

        RefreshBtnText(optionDialogue.Options.Count);
    }

    private void OnAddNewClicked()
    {
        if (currentSection == null || dataCenter == null) return;
        var optionDialogue = dataCenter.GetOptionDialogue(currentSection);
        int count = optionDialogue?.Options.Count ?? 0;
        if (count >= MaxOptionNumber)
        {
            TipPanel.ShowToast($"最多添加 {MaxOptionNumber} 个选项");
            return;
        }
        dataCenter.AddOption(currentSection);
        RebuildItems();
    }

    private void OnDeleteOption(int index)
    {
        dataCenter?.RemoveOption(currentSection, index);
        RebuildItems();
    }

    private void OnGoUpOption(int index)
    {
        dataCenter?.MoveOptionUp(currentSection, index);
        RebuildItems();
    }

    private void RefreshBtnText(int count)
    {
        if (btnText != null)
            btnText.text = $"新增选项 ({count}/{MaxOptionNumber})";
    }

    private void ClearItems()
    {
        foreach (var item in contentItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        contentItems.Clear();
    }
}
