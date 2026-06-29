


using System;
using Game.ECS;
using UI.UIPanels.GameEdit;
/**
* @ Author: Jun Zhou
* @ Create Time: 2023-08-25 14:56:12
* @ Modified by: Jun Zhou
* @ Modified time: 2023-08-25 14:57:25
* @ Description: UI打开帮助类
*/
public static class UIOpenUntils
{
    public static void CloseGamePropertyEditPanel()
    {
        if (UIManager.Inst.TryFindPanel(PanelId.GamePropertyEditPanel, out var oldPanel))
        {
            UIManager.Inst.ClosePanel(oldPanel);
        }
    }

    public static GamePropertyEditPanel OpenGamePropertyEditPanel(Type adapterType, SceneEntity entity)
    {
        GamePropertyEditPanel propertyPanel = UIManager.Inst.OpenPanel(PanelId.GamePropertyEditPanel) as GamePropertyEditPanel;
        if (adapterType!=null && propertyPanel.gameObject.GetComponent(adapterType) == null)
            (propertyPanel.gameObject.AddComponent(adapterType) as BasePropertyAdapter).SetSelectEntity(entity);

        return propertyPanel;
    }

    /// <summary>
    ///  刷新一下属性面板。如果不存在就创建
    /// </summary>
    public static GamePropertyEditPanel RefreshGamePropertyEditPanel(Type adapterType, SceneEntity entity)
    {
        GamePropertyEditPanel propertyPanel;
        if (UIManager.Inst.TryFindPanel(PanelId.GamePropertyEditPanel, out var panel))
        {
            propertyPanel = panel as GamePropertyEditPanel;
        } else {
            propertyPanel = OpenGamePropertyEditPanel(adapterType, entity);
        }
        return propertyPanel;
    }

    /// <summary>
    /// 重写大概一下属性面板
    /// </summary>
    public static GamePropertyEditPanel ReOpenGamePropertyEditPanel(string adapterTypeStr, SceneEntity entity)
    {
        CloseGamePropertyEditPanel();

        Type adapterType = null;
        if (!string.IsNullOrEmpty(adapterTypeStr))
        {
            adapterType = Type.GetType($"UI.UIPanels.GameEdit.{adapterTypeStr}");
        }
        return OpenGamePropertyEditPanel(adapterType, entity);
    }
}
