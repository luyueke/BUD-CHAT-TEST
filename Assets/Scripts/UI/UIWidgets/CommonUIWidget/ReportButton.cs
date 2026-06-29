using System;
using Basic.Utils;
using UI.BaseWidgets;
using UnityEngine;

public class ReportButton : CommonUIWidget
{
    public CButton Btn_Report;

    private ErrReportReq _reportReq = new ErrReportReq();

    private void Awake()
    {
        Btn_Report.onClick.AddListener(OnBtnReportClick);
    }

    /// <summary>
    /// 所需要的参数
    /// args[0] bizId = ""; //(string)作品或用户id
    /// args[1] scenesType = ""; //(int)举报场景类型， ErrReportScenesType = 0; UgcMap = 1; UgcProp = 2; UgcMaterial = 3; UgcSkin = 4; User = 5;
    /// </summary>
    /// <param name="args"></param>
    public override void SetData(params object[] args)
    {
        base.SetData(args);
        if(args.Length < 2)
            return;
        
        _reportReq = new ErrReportReq()
        {
            Uid = AccountDataManager.Inst.Uid,
            bizId = (string)args[0],
            scenesType = (int)args[1],
        };
    }

    private void OnBtnReportClick()
    {
        UIManager.Inst.OpenPanel(PanelId.ReportAssetPanel, _reportReq);
    }
}

public enum ErrReportType
{
    ErrReportType = 0,
    Porn = 1, //涉黄
    Political = 2, //涉政
    Violent = 3, //暴力
    Terrorism = 4, //仇恨,恐怖,极端
    DrugsAndGuns = 5, //毒品枪支
    Suicide = 6, //自杀
    Misinformation = 7, //虚假信息
    IPViolation = 8, //侵犯IP
    DontWantToSee = 9, //单纯不想看见
    Harassment = 10, //骚扰
    Others = 11, //其他
    Plagiarize = 12,//抄袭他人作品
}

public class ErrReportReq
{
    public string Uid;
    public int reportType; //举报理由类型： ErrReportType = 0; Porn = 1; //涉黄 Political = 2; //涉政 Violent = 3; //暴力 Terrorism = 4; //仇恨,恐怖,极端 DrugsAndGuns = 5; //毒品枪支 Suicide = 6; //自杀 Misinformation = 7; //虚假信息 IPViolation = 8; //侵犯IP DontWantToSee = 9; //单纯不想看见 Harassment = 10; //骚扰 Others = 11; //其他
    public string bizId = ""; //作品或用户id
    public string reason = ""; //举报原因
    public string imageUrl = ""; //截图
    public int scenesType; //举报场景类型， ErrReportScenesType = 0; UgcMap = 1; UgcProp = 2; UgcMaterial = 3; UgcSkin = 4; User = 5;
    public string contestId;
}

public enum ErrReportSceneType
{
    ErrReportScenesType = 0,
    UgcMap = 1,
    UgcProp = 2,
    UgcMaterial = 3,
    UgcSkin = 4,
    User = 5,
    UgcMusicScore = 6,
    UgcMusicTone = 7,
    UgcAnimation = 8,
    UgcPose = 9,
    UgcAnimationMusic = 10,
    AINpc = 11,
    OC = 12,
    UgcVehicle = 13,
    Photo = 14,
    Character = 15,
    CharacterTone = 16,
    Actor = 17,
    Theater = 18,
    UGCSence = 19,
}