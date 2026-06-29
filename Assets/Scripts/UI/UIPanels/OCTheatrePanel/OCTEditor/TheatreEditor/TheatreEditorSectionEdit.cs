using System;
using System.Collections.Generic;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorSectionEdit : TheatreEditorUIBase<TheatreEditorDataCenter>
{
    [SerializeField] private TheatreEditorTextInput currentIndexInput;
    [SerializeField] private TheatreEditorTextInput jumpTextInput;
    [SerializeField] private TheatreEditorAvatarSelectInput avatarSelectInput;
    [SerializeField] private TheatreEditorBGInput bgInput;
    [SerializeField] private TheatreEditorDialogueInput dialogueInput;
    [SerializeField] private TheatreEditorOptionInput optionInput;
    [SerializeField] private TheatreEditorEmoteInput emoteInput;
    // [SerializeField] private TheatreEditorMusicInput musicInput;   // 已移除：MusicEdit 音效选择
    // [SerializeField] private TheatreEditorMusicInput dampingInput; // 已移除：配音上传
    [SerializeField] private TheatreEditorMusicInput sectionBGMusicInput;

    [SerializeField] private Button deleteBtn;
    [SerializeField] private Button newBtn;
    [SerializeField] private Button closeBoardBtn;
    [SerializeField] private Button addOneJumpBtn;

    private POCTheatreSection currentSection;

    public override void OnInit(TheatreEditorDataCenter param)
    {
        base.OnInit(param);

        deleteBtn?.onClick.RemoveAllListeners();
        deleteBtn?.onClick.AddListener(OnDeleteClicked);

        newBtn?.onClick.RemoveAllListeners();
        newBtn?.onClick.AddListener(OnNewClicked);

        closeBoardBtn?.gameObject.SetActive(false);
        closeBoardBtn?.onClick.RemoveAllListeners();
        closeBoardBtn?.onClick.AddListener(OnCloseBoardClicked);

        addOneJumpBtn?.onClick.RemoveAllListeners();
        addOneJumpBtn?.onClick.AddListener(OnAddOneJumpClicked);

        // currentIndexInput：修改段落编号（移动排序）
        currentIndexInput?.Init("段落编号", 4, text =>
        {
            if (currentSection == null || DataRoot == null) return;
            if (!int.TryParse(text, out int newIndex) || newIndex < 1) return;
            if (newIndex > DataRoot.SectionList.Count)
            {
                TipPanel.ShowToast($"段落编号不能超过 {DataRoot.SectionList.Count}");
                currentIndexInput.SetDisplayText(currentSection.SectionIndex.ToString());
                return;
            }
            DataRoot.MoveSectionToIndex(currentSection, newIndex);
        }, numericOnly: true);

        // jumpTextInput：管理 NextIndex
        jumpTextInput?.Init("输入跳转段落编号", 4, text =>
        {
            if (currentSection == null || DataRoot == null) return;
            if (string.IsNullOrEmpty(text)) { DataRoot.SetSectionNextIndex(currentSection, 0); return; }
            if (!int.TryParse(text, out int index) || index <= 0) return;
            var targetSection = currentSection; // capture before AddNewSection triggers OnShow and changes currentSection
            var existing = DataRoot.SectionList?.Find(s => s.SectionIndex == index);
            if (existing == null)
            {
                var newSection = DataRoot.AddNewSection(index);
                if (newSection == null) return;
                index = newSection.SectionIndex;
                jumpTextInput?.SetDisplayText(index.ToString());
            }
            DataRoot.SetSectionNextIndex(targetSection, index);
        }, numericOnly: true);

        // avatarSelectInput：点击打开 AvatarExpressEdit
        avatarSelectInput?.Init(() => Panel?.ShowAvatarExpressEdit(currentSection));

        Action onBoardOpen = () => closeBoardBtn?.gameObject.SetActive(true);
        Action onBoardClose = () => closeBoardBtn?.gameObject.SetActive(false);

        // bgInput：选择背景图
        if (bgInput != null) { bgInput.onBoardOpen = onBoardOpen; bgInput.onBoardClose = onBoardClose; }
        bgInput?.Init(
            onSelect: () =>
            {
                closeBoardBtn?.gameObject.SetActive(false);
                Panel?.ShowBGSelector(bgIndex =>
                {
                    if (currentSection == null) return;
                    // bgIndex 是 AllBackgrounds 的 0-based index，
                    // 存到 BackgroundUrl 时用 index+1 区分"未配置"(0)
                    DataRoot?.SetSectionBackground(currentSection, bgIndex + 1);
                    bgInput.Refresh(bgIndex + 1);
                });
            },
            onDelete: () =>
            {
                DataRoot?.SetSectionBackground(currentSection, 0);
                bgInput?.Refresh(0);
            },
            dc: param
        );

        // dialogueInput
        if (dialogueInput != null) { dialogueInput.onBoardOpen = onBoardOpen; dialogueInput.onBoardClose = onBoardClose; }
        dialogueInput?.Init(param, null);

        // optionInput：跳转到 OptionEdit
        if (optionInput != null) { optionInput.onBoardOpen = onBoardOpen; optionInput.onBoardClose = onBoardClose; }
        optionInput?.Init(param, optionIndex =>
        {
            closeBoardBtn?.gameObject.SetActive(false);
            if (currentSection != null)
            {
                Panel?.ShowOptionEdit(currentSection);
                Panel?.DataCenter?.GetOptionDialogue(currentSection); // 确保存在
                // OptionEdit 需要知道当前操作哪个 option
                var optionEdit = Panel?.GetComponentInChildren<TheatreEditorOptionEdit>(true);
                optionEdit?.SetCurrentOptionIndex(optionIndex);
            }
        });

        // emoteInput：打开 EmoteEdit（占位，EmoteEdit 尚未实现）
        emoteInput?.Init(() => Panel?.ShowEmoteEdit(currentSection));
        if (emoteInput != null)
        {
            emoteInput.onEmoteUpdated = emote =>
            {
                DataRoot?.SetSectionEmote(currentSection, emote);
            };
        }

        // sectionBGMusicInput（Section BGM，URL 上传）
        if (sectionBGMusicInput != null) { sectionBGMusicInput.onBoardOpen = onBoardOpen; sectionBGMusicInput.onBoardClose = onBoardClose; }
        sectionBGMusicInput?.Init();
        if (sectionBGMusicInput != null)
        {
            sectionBGMusicInput.onAudioUpdated = audio =>
            {
                DataRoot?.SetSectionAudio(currentSection, audio);
            };
        }

        // emoteInput onBoardOpen
        if (emoteInput != null) emoteInput.onBoardOpen = onBoardOpen;
    }

    public override void OnShow(TheatreEditorDataCenter param)
    {
        base.OnShow(param);
        // DataRoot 由 base.OnShow → OnInit 时已赋值；但每次 Show 需更新 dc 引用
        DataRoot = param;

        currentSection = param?.currentSelected;

        RefreshAll();
    }

    private void RefreshAll()
    {
        if (currentSection == null) return;

        // 当前 index 显示（可编辑）
        currentIndexInput?.SetDisplayText(currentSection.SectionIndex.ToString());

        // 跳转 index
        bool hasOptions = DataRoot?.HasOptionsConfigured(currentSection) ?? false;
        if (hasOptions)
        {
            jumpTextInput?.SetDisplayText("已配置选项");
            jumpTextInput?.SetInteractable(false);
        }
        else
        {
            int nextIdx = currentSection.NextIndex;
            jumpTextInput?.SetDisplayText(nextIdx > 0 ? nextIdx.ToString() : "");
            jumpTextInput?.SetInteractable(true);
        }

        bool hasJump = currentSection.NextIndex > 0;
        addOneJumpBtn?.gameObject.SetActive(!hasJump && !hasOptions);

        // 角色立绘
        avatarSelectInput?.Refresh(currentSection.AvatarId, currentSection.AvatarType, DataRoot);

        // 背景图
        bgInput?.Refresh(currentSection.BackgroundUrl);

        // 对话内容
        dialogueInput?.Refresh(currentSection);

        // 选项内容
        optionInput?.Refresh(currentSection);

        // Emote
        emoteInput?.Refresh(currentSection, DataRoot);

        // Section BGM
        sectionBGMusicInput?.Refresh(currentSection);
    }

    private void OnDeleteClicked()
    {
        if (DataRoot == null || currentSection == null) return;

        var list = DataRoot.SectionList;
        if (list.Count <= 1)
        {
            TipPanel.ShowToast("至少保留一个段落");
            return;
        }

        int idx = list.IndexOf(currentSection);
        DataRoot.RemoveSection(currentSection);

        // 选择下一个，没有则选上一个
        if (DataRoot.SectionList.Count > 0)
        {
            int nextIdx = Mathf.Clamp(idx, 0, DataRoot.SectionList.Count - 1);
            var nextSection = DataRoot.SectionList[nextIdx];
            DataRoot.SelectSection(nextSection);
            currentSection = nextSection;
            RefreshAll();
        }
    }

    private void OnNewClicked()
    {
        var newSection = DataRoot?.AddNewSection();
        if (newSection != null)
        {
            currentSection = newSection;
            RefreshAll();
        }
    }

    private void OnAddOneJumpClicked()
    {
        if (currentSection == null || DataRoot == null) return;
        int targetIndex = currentSection.SectionIndex + 1;
        var targetSection = currentSection; // capture before AddNewSection may trigger OnShow
        var existing = DataRoot.SectionList?.Find(s => s.SectionIndex == targetIndex);
        if (existing == null)
        {
            var newSection = DataRoot.AddNewSection(targetIndex);
            if (newSection == null) return;
            targetIndex = newSection.SectionIndex;
        }
        DataRoot.SetSectionNextIndex(targetSection, targetIndex);
        // AddNewSection switches currentSelected to the new section;
        // restore selection and display back to the original section.
        currentSection = targetSection;
        DataRoot.SelectSection(targetSection);
        RefreshAll();
        Panel?.RefreshSectionItemDisplay(targetSection);
    }

    private void OnCloseBoardClicked()
    {
        bgInput?.CloseOptionPanel();
        dialogueInput?.CloseAllBoards();
        optionInput?.CloseAllBoards();
        sectionBGMusicInput?.CloseAllBoards();
        emoteInput?.CloseTriggerBoard();
        emoteInput?.CloseSelectionPanel();
        closeBoardBtn?.gameObject.SetActive(false);
    }

    public override void OnHide() { base.OnHide(); }
}
