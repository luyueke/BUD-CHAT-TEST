using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorOptionEdit : TheatreEditorUIBase<POCTheatreSection>
{
    [SerializeField] private Text currentOptIndexText;
    [SerializeField] private TheatreEditorTextInput optionContentInput;
    [SerializeField] private TheatreEditorJumpSectionInput jumpToInput;
    [SerializeField] private Text jumpSectionText;
    [SerializeField] private Button openJumpSectionBtn;
    [SerializeField] private Button openJumpSectionBtn2;

    [SerializeField] private Button addSectionBtn;
    [SerializeField] private Button deleteBtn;

    private POCTheatreSection sectionData;
    private int currentOptionIndex;

    public override void OnInit(POCTheatreSection param)
    {
        base.OnInit(param);

        addSectionBtn?.onClick.RemoveAllListeners();
        addSectionBtn?.onClick.AddListener(OnAddSectionClicked);

        deleteBtn?.onClick.RemoveAllListeners();
        deleteBtn?.onClick.AddListener(OnDeleteClicked);

        openJumpSectionBtn?.onClick.RemoveAllListeners();
        openJumpSectionBtn?.onClick.AddListener(OpenJumpSection);

        openJumpSectionBtn2?.onClick.RemoveAllListeners();
        openJumpSectionBtn2?.onClick.AddListener(OpenJumpSection);

        optionContentInput?.Init("输入选项内容", 12, text =>
        {
            Panel?.DataCenter?.SetOptionText(sectionData, currentOptionIndex, text);
        });
    }

    public override void OnShow(POCTheatreSection param)
    {
        base.OnShow(param);
        sectionData = param;
        jumpToInput?.gameObject.SetActive(false);
        RefreshAll();
    }

    private void OpenJumpSection()
    {
        if (jumpToInput == null) return;
        jumpToInput.gameObject.SetActive(true);

        var dc = Panel?.DataCenter;
        var optionDialogue = dc?.GetOptionDialogue(sectionData);
        if (optionDialogue == null || currentOptionIndex < 0 || currentOptionIndex >= optionDialogue.Options.Count) return;

        var option = optionDialogue.Options[currentOptionIndex];
        var allSections = dc?.SectionList;
        if (allSections != null)
        {
            jumpToInput.Init(allSections, sectionData.SectionIndex, option.JumpIndex, jumpIndex =>
            {
                dc?.SetOptionJumpIndex(sectionData, currentOptionIndex, jumpIndex);
                RefreshJumpSectionText(jumpIndex);
                jumpToInput.gameObject.SetActive(false);
            });
        }
    }

    public override void OnHide() { base.OnHide(); }

    public void SetCurrentOptionIndex(int index)
    {
        currentOptionIndex = index;
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (sectionData == null) return;

        var dc = Panel?.DataCenter;
        var optionDialogue = dc?.GetOptionDialogue(sectionData);
        if (optionDialogue == null || currentOptionIndex < 0 || currentOptionIndex >= optionDialogue.Options.Count) return;

        var option = optionDialogue.Options[currentOptionIndex];

        if (currentOptIndexText != null)
            currentOptIndexText.text = $"第 {currentOptionIndex + 1} 个";

        optionContentInput?.SetDisplayText(option.Text);

        RefreshJumpSectionText(option.JumpIndex);
    }

    private void RefreshJumpSectionText(int jumpIndex)
    {
        if (jumpSectionText == null) return;

        if (jumpIndex <= 0)
        {
            jumpSectionText.text = "[未设置跳转]";
            return;
        }

        var dc = Panel?.DataCenter;
        var targetSection = dc?.SectionList?.Find(s => s.SectionIndex == jumpIndex);
        if (targetSection == null) { jumpSectionText.text = "[无效节点]"; return; }
        string raw = TheatreEditorJumpSectionInput.GetSectionFirstText(targetSection);
        jumpSectionText.text = raw.Length > 8 ? raw.Substring(0, 8) + "..." : raw;
    }

    private void OnAddSectionClicked()
    {
        var dc = Panel?.DataCenter;
        if (dc == null) return;

        var newSection = dc.AddNewSection();
        if (newSection == null) return;

        dc.SetOptionJumpIndex(sectionData, currentOptionIndex, newSection.SectionIndex);
        // sectionData's item won't auto-refresh because currentSelected moved to newSection;
        // force refresh so its OptionItem shows the updated jump index.
        Panel?.RefreshSectionItemDisplay(sectionData);
    }

    private void OnDeleteClicked()
    {
        Panel?.DataCenter?.RemoveOption(sectionData, currentOptionIndex);
        Panel?.BackToSectionEdit();
    }
}
