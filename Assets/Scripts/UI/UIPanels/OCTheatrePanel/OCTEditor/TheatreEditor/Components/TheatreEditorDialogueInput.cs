using System;
using System.Collections.Generic;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 管理 Section 的普通对话内容（Type==1 的 POCTheatreDialogue）。
/// 最多 MaxDialogueNumber 条，最少 0 条（DataCenter 保证至少 1 条）。
/// </summary>
public class TheatreEditorDialogueInput : MonoBehaviour
{
    private const int MaxDialogueNumber = 30;

    [SerializeField] private TheatreEditorContentInput contentPrefab;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Button addNewBtn;
    [SerializeField] private Text btnText;

    public Action onBoardOpen;
    public Action onBoardClose;

    private readonly List<TheatreEditorContentInput> contentItems = new();
    private POCTheatreSection currentSection;
    private TheatreEditorDataCenter dataCenter;
    private Action onJumpToOption; // 跳转到 OptionEdit 页面

    public void CloseAllBoards()
    {
        foreach (var item in contentItems)
            item?.CloseSettingBoard();
    }

    public void Init(TheatreEditorDataCenter dc, Action onJumpOption)
    {
        dataCenter = dc;
        onJumpToOption = onJumpOption;

        addNewBtn?.onClick.RemoveAllListeners();
        addNewBtn?.onClick.AddListener(OnAddNewClicked);
    }

    public void Refresh(POCTheatreSection section)
    {
        currentSection = section;
        RebuildItems();
    }

    private void RebuildItems()
    {
        ClearItems();
        if (currentSection == null || contentPrefab == null || contentRoot == null) return;

        int dialogueCount = 0;
        for (int i = 0; i < currentSection.Dialogues.Count; i++)
        {
            var dialogue = currentSection.Dialogues[i];
            if (dialogue.Type != 1) continue;

            int capturedIndex = i;
            var obj = Instantiate(contentPrefab.gameObject, contentRoot);
            var item = obj.GetComponent<TheatreEditorContentInput>();
            item.onBoardOpen = onBoardOpen;
            item.onBoardClose = onBoardClose;
            item.Init(
                TheatreContentType.Text,
                dialogue.Text,
                text => dataCenter?.SetDialogueText(currentSection, capturedIndex, text),
                () => OnDeleteDialogue(capturedIndex),
                () => OnGoUpDialogue(capturedIndex),
                null
            );
            contentItems.Add(item);
            obj.SetActive(true);
            dialogueCount++;
        }

        RefreshBtnText(dialogueCount);
    }

    private void OnAddNewClicked()
    {
        if (currentSection == null || dataCenter == null) return;
        int count = dataCenter.GetNormalDialogueCount(currentSection);
        if (count >= MaxDialogueNumber)
        {
            TipPanel.ShowToast($"最多添加 {MaxDialogueNumber} 条对话");
            return;
        }
        dataCenter.AddDialogue(currentSection);
        RebuildItems();
    }

    private void OnDeleteDialogue(int index)
    {
        dataCenter?.RemoveDialogue(currentSection, index);
        RebuildItems();
    }

    private void OnGoUpDialogue(int index)
    {
        dataCenter?.MoveDialogueUp(currentSection, index);
        RebuildItems();
    }

    private void RefreshBtnText(int count)
    {
        if (btnText != null)
            btnText.text = $"新增对话内容 ({count}/{MaxDialogueNumber})";
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
