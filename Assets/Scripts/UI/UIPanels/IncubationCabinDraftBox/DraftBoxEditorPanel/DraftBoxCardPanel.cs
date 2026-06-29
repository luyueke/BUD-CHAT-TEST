using ChocDino.UIFX;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 卡面编辑面板，包含"卡片颜色"和"台词显示"两个子 Tab
/// Date:26-04-01
/// </summary>
public class DraftBoxCardPanel : MonoBehaviour
{
    [SerializeField] private CButton Btn_Color;            // 卡片颜色 Tab 按钮

    [SerializeField] private CButton Btn_Lable;            // 台词显示 Tab 按钮

    [SerializeField] private DraftBoxCardColorPanel ColorPanel; // 卡片颜色子面板
    [SerializeField] private DraftBoxCardLablePanel LablePanel; // 台词显示子面板

    /// <summary>初始化按钮监听及子面板，默认展示卡片颜色 Tab</summary>
    public void InitUI()
    {
        Btn_Color.onClick.AddListener(() => SwitchTab(true));
        Btn_Lable.onClick.AddListener(() => SwitchTab(false));

        ColorPanel.InitUI();
        LablePanel.InitUI();

        SwitchTab(true);
    }

    /// <summary>传入角色数据引用，子面板自行读取初始值并在变更时直接写入数据+广播事件</summary>
    public void SetData(CabinCharacterBaseInfo info)
    {
        ColorPanel.SetData(info);
        LablePanel.SetData(info);
    }

    /// <summary>切换子 Tab，isColor 为 true 显示颜色面板，否则显示台词面板，同步更新按钮样式</summary>
    private void SwitchTab(bool isColor)
    {
        ColorPanel.gameObject.SetActive(isColor);
        Btn_Color.interactable = !isColor;

        LablePanel.gameObject.SetActive(!isColor);
        Btn_Lable.interactable = isColor;
    }
}
