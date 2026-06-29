using System;
using UI.Base;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc:
/// Date:24-08-21 14:15:07
/// </summary>
public class UIQualityTipPanel : BasePanel<UIQualityTipPanel>
{
    public Button CloseBtn;
    public Button GetBtn;
    public Action CloseSelfAction;
    public override void OnCreate()
    {
        CloseBtn.onClick.AddListener(CloseSelfPanel);
        GetBtn.onClick.AddListener(CloseSelfPanel);
    }

    private void CloseSelfPanel()
    {
        CloseSelfAction?.Invoke();
        CloseSelf();
    }
}