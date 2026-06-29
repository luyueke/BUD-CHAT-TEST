using Game.Base;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc:
/// Date:23-08-10 11:11:38
/// </summary>
public class GameTestPublishPanel : BaseGamePlayPanel<GameTestPublishPanel>
{
    [SerializeField] private CButton onBackBtn;
    // 已迁移到BaseGamePlayPanel中
    // [SerializeField] private GameObject lifeUIGo;
    // [SerializeField] private CText lifeTxt;
    // [SerializeField] private GameObject starUIGo;
    // [SerializeField] private CText starTxt;
    // [SerializeField] private GameObject timeUIGo;
    // [SerializeField] private CText timeTxt;
    [SerializeField] private GameObject tipUIGo;
    [SerializeField] private CButton tipBtn;
    [SerializeField] private CText tipLine1Txt;
    [SerializeField] private CText tipLine2Txt;

    public override void OnCreate()
    {
        base.OnCreate();
        onBackBtn.onClick.AddListener(OnBackClick);
        tipBtn.onClick.AddListener(OnTipClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
    }

    void OnBackClick()
    {
        PassLevelManager.Inst.ExitCurrentMap();
    }

    void OnTipClick()
    {
        tipUIGo.SetActive(!tipUIGo.activeInHierarchy);
    }
}