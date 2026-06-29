using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorMainEdit : TheatreEditorUIBase<TheatreEditorDataCenter>
{
    [SerializeField] private TheatreEditorTextInput theatreName;
    [SerializeField] private TheatreEditorTextInput theatreDescription;
    [SerializeField] private TheatreEditorBGUploader coverUploader;
    [SerializeField] private TheatreEditorMusicUploader theatreBGMusic;
    [SerializeField] private TheatreEditorBGLibrary theatreBGLibrary;
    [SerializeField] private Text sectionNumber;
    [SerializeField] private Text dialogueNumber;


    public override void OnInit(TheatreEditorDataCenter param)
    {
        base.OnInit(param);

        theatreName?.Init("输入剧本名称", 16, text => DataRoot.SetTheatreName(text));
        theatreDescription?.Init("输入剧本描述", 200, text => DataRoot.SetTheatreDescription(text));
        coverUploader?.Init($"UgcOCTheatreCover/{AccountDataManager.Inst.Uid}", url => DataRoot.SetTheatreCover(url), getDataCenter: () => DataRoot);
        theatreBGMusic?.Init(url => DataRoot.SetBgMusic(url));
    }

    public override void OnShow(TheatreEditorDataCenter param)
    {
        if (DataRoot != null) DataRoot.OnDataChanged -= RefreshDisplay;
        base.OnShow(param);
        DataRoot = param;
        theatreBGLibrary?.Init(DataRoot);
        if (DataRoot != null) DataRoot.OnDataChanged += RefreshDisplay;
        RefreshDisplay();
    }

    public override void OnHide() { base.OnHide(); }

    private void RefreshDisplay()
    {
        var info = DataRoot?.TheatreInfo;
        if (info == null) return;

        theatreName?.SetDisplayText(info.name);
        theatreDescription?.SetDisplayText(info.desc);
        if (sectionNumber != null) sectionNumber.text = DataRoot.SectionList.Count.ToString();
        if (dialogueNumber != null)
        {
            int total = 0;
            foreach (var s in DataRoot.SectionList)
                total += DataRoot.GetNormalDialogueCount(s);
            dialogueNumber.text = total.ToString();
        }
        theatreBGMusic?.SetDisplayUrl(DataRoot.BgMusic);
        coverUploader?.SetDisplayUrl(info.cover);
    }

    public override void Destroy()
    {
        base.Destroy();
        if (DataRoot != null) DataRoot.OnDataChanged -= RefreshDisplay;
    }
}
