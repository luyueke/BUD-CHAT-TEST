using UI.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:fsc
/// Desc:测试地图加载
/// Date:23-07-19 14:19:13
/// </summary>
public class TestSceneBuildPanel : BasePanel<TestSceneBuildPanel>
{

    public Button loadMapBtn;
    
    public override void OnCreate()
    {
        loadMapBtn.onClick.AddListener(DoLoadMap);
    }

    private void DoLoadMap()
    {
        Debug.Log("Load map test Start ~~~");
        
        CloseSelf();
        UIManager.Inst.OpenPanel(PanelId.GameEditModePanel);
    }

    public override void OnShow(params object[] args)
    {
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
    }

    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }
}