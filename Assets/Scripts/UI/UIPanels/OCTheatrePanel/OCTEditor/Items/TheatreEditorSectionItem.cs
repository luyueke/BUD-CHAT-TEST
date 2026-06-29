using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorSectionItem : MonoBehaviour
{
    [SerializeField] private Transform dialogueRoot;
    [SerializeField] private TheatreEditorDialogue dialoguePrefab;
    [SerializeField] private TheatreEditorOptions option;
    [SerializeField] private RemoteImageBehaviour avatar;
    [SerializeField] private Text[] currentIndexTexts;
    [SerializeField] private Text nextIndexText;
    [SerializeField] private GameObject nextRootGObject;
    [SerializeField] private GameObject nextInfoGObject;
    [SerializeField] private GameObject nextNoSetGObject;
    [SerializeField] private Image sectionBG;
    [SerializeField] private Sprite defaultBG;
    [SerializeField] private Sprite selectedBG;
    [SerializeField] private Button selectBtn;
    [SerializeField] private Button copyBtn;
    [SerializeField] private Button playBtn;

    private readonly List<TheatreEditorDialogue> dialogues = new();

    public POCTheatreSection SectionData { get; private set; }
    private TheatreEditorDataCenter dataCenter;
    private Action<TheatreEditorSectionItem> onClicked;
    private bool isSelected;

    public Action<int> onJumpWindowRequested;
    public Action<POCTheatreSection> onPlayClicked;

    public void Init(POCTheatreSection section, TheatreEditorDataCenter dc, Action<TheatreEditorSectionItem> onClick)
    {
        SectionData = section;
        dataCenter = dc;
        onClicked = onClick;

        selectBtn?.onClick.RemoveAllListeners();
        selectBtn?.onClick.AddListener(() => onClicked?.Invoke(this));

        copyBtn?.onClick.RemoveAllListeners();
        copyBtn?.onClick.AddListener(OnCopyClicked);

        playBtn?.onClick.RemoveAllListeners();
        playBtn?.onClick.AddListener(() => onPlayClicked?.Invoke(SectionData));

        // Wire option callbacks once — RefreshFromData reuses them via closures
        if (option != null)
        {
            option.OnCreate();

            // Options header text edit
            option.onSelected = _ =>
            {
                if (!isSelected) { onClicked?.Invoke(this); return; }
                POCTheatreDialogue optDialogue = null;
                if (SectionData != null)
                    foreach (var d in SectionData.Dialogues)
                        if (d.Type == 2) { optDialogue = d; break; }
                TheatreEditorTextInput.ShowQuickInput(
                    "输入选项问题",
                    optDialogue?.Text ?? "",
                    result => dataCenter?.SetOptionDialogueText(SectionData, result));
            };

            // Individual option text edit
            option.onOptionTextEditRequested = (optionIdx, currentText) =>
            {
                if (!isSelected) { onClicked?.Invoke(this); return; }
                TheatreEditorTextInput.ShowQuickInput("输入选项内容", currentText, 12, result =>
                {
                    dataCenter?.SetOptionText(SectionData, optionIdx, result);
                });
            };

            // Individual option jump button
            option.onOptionJumpRequested = jumpIndex =>
            {
                if (!isSelected) { onClicked?.Invoke(this); return; }
                if (jumpIndex <= 0)
                {
                    TipPanel.ShowToast("未配置跳转");
                    return;
                }
                onJumpWindowRequested?.Invoke(jumpIndex);
            };
        }

        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (SectionData == null) return;

        if (currentIndexTexts != null)
        {
            foreach (var t in currentIndexTexts)
            {
                if (t != null) t.text = SectionData.SectionIndex.ToString();
            }
        }

        POCTheatreDialogue optionDialogue = null;
        foreach (var d in SectionData.Dialogues)
        {
            if (d.Type == 2) { optionDialogue = d; break; }
        }
        bool hasOptions = optionDialogue != null && optionDialogue.Options.Count > 0;

        if (hasOptions)
        {
            nextRootGObject?.SetActive(false);
        }
        else if (SectionData.NextIndex > 0)
        {
            nextRootGObject?.SetActive(true);
            nextInfoGObject?.SetActive(true);
            nextNoSetGObject?.SetActive(false);
            if (nextIndexText != null) nextIndexText.text = SectionData.NextIndex.ToString();
        }
        else
        {
            nextRootGObject?.SetActive(true);
            nextInfoGObject?.SetActive(true);
            nextNoSetGObject?.SetActive(false);
            if (nextIndexText != null) nextIndexText.text = "?";
        }

        RefreshAvatarDisplay();
        RefreshDialoguePreview();
        RefreshOptionPreview();
    }

    private void RefreshAvatarDisplay()
    {
        if (avatar == null) return;

        string url = ResolveAvatarUrl();
        bool hasAvatar = !string.IsNullOrEmpty(url);
        avatar.gameObject.SetActive(hasAvatar);
        if (hasAvatar) avatar.Load(url);
    }

    private string ResolveAvatarUrl()
    {
        if (dataCenter == null || SectionData == null) return null;
        if (string.IsNullOrEmpty(SectionData.AvatarId)) return null;
        if (!dataCenter.AvatarInfoCache.TryGetValue(SectionData.AvatarId, out var avatarInfo)) return null;

        if (!string.IsNullOrEmpty(SectionData.AvatarType) && avatarInfo.expressions != null)
        {
            int sep = SectionData.AvatarType.IndexOf('|');
            string targetName = sep >= 0 ? SectionData.AvatarType.Substring(0, sep) : SectionData.AvatarType;
            int targetMType = (sep >= 0 && int.TryParse(SectionData.AvatarType.Substring(sep + 1), out int m)) ? m : -1;

            foreach (var expr in avatarInfo.expressions)
            {
                if (expr.expressionName == targetName
                    && (targetMType < 0 || expr.mType == targetMType)
                    && !string.IsNullOrEmpty(expr.expressionURL))
                    return expr.expressionURL;
            }
        }

        if (avatarInfo.expressions != null && avatarInfo.expressions.Count > 0)
            return avatarInfo.expressions[0].expressionURL;

        return avatarInfo.cover;
    }

    private void RefreshDialoguePreview()
    {
        foreach (var d in dialogues)
        {
            if (d != null) Destroy(d.gameObject);
        }
        dialogues.Clear();

        if (dialogueRoot == null || dialoguePrefab == null || SectionData == null) return;

        for (int i = 0; i < SectionData.Dialogues.Count; i++)
        {
            var dialogue = SectionData.Dialogues[i];
            if (dialogue.Type != 1) continue;

            var obj = Instantiate(dialoguePrefab.gameObject, dialogueRoot);
            var item = obj.GetComponent<TheatreEditorDialogue>();
            item.OnCreate();
            item.SetText(dialogue.Text ?? "");

            int capturedIndex = i;
            var capturedDialogue = dialogue;
            item.onSelected = _ =>
            {
                if (!isSelected) { onClicked?.Invoke(this); return; }
                TheatreEditorTextInput.ShowQuickInput(
                    "输入对话内容",
                    capturedDialogue.Text ?? "",
                    result => dataCenter?.SetDialogueText(SectionData, capturedIndex, result));
            };

            dialogues.Add(item);
            obj.SetActive(true);
        }
    }

    private void RefreshOptionPreview()
    {
        if (option == null || SectionData == null) return;

        POCTheatreDialogue optionDialogue = null;
        foreach (var d in SectionData.Dialogues)
        {
            if (d.Type == 2) { optionDialogue = d; break; }
        }

        bool hasOptions = optionDialogue != null && optionDialogue.Options.Count > 0;
        option.gameObject.SetActive(hasOptions);
        if (hasOptions)
            option.RefreshFromData(optionDialogue, jumpIdx =>
            {
                var target = dataCenter?.SectionList?.Find(s => s.SectionIndex == jumpIdx);
                if (target == null) return "未配置文本";
                foreach (var d in target.Dialogues)
                    if ((d.Type == 1 || d.Type == 2) && !string.IsNullOrEmpty(d.Text)) return d.Text;
                return "未配置文本";
            });
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (sectionBG != null)
            sectionBG.sprite = selected ? selectedBG : defaultBG;
        copyBtn?.gameObject.SetActive(selected);
        playBtn?.gameObject.SetActive(selected);
    }

    private void OnCopyClicked()
    {
        dataCenter?.DuplicateSection(SectionData);
    }
}
