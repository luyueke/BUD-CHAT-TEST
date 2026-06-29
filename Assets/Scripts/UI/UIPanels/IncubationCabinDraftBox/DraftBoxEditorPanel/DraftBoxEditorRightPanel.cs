using UI.BaseWidgets;
using UnityEngine;

/// <summary>
/// Author:
/// Desc: 草稿箱编辑器右侧面板，管理"角色"和"卡片"两个主 Tab 的切换
/// Date:26-04-01
/// </summary>
public class DraftBoxEditorRightPanel : MonoBehaviour
{
    [SerializeField] private CButton Btn_Character;         // 角色 Tab 按钮
    [SerializeField] private CButton Btn_Card;              // 卡片 Tab 按钮

    [SerializeField] private DraftBoxCharacterPanel CharacterPanel; // 角色编辑面板（姿势 + 细节）
    [SerializeField] private DraftBoxCardPanel CardPanel;           // 卡面编辑面板

    /// <summary>初始化按钮监听，并将回调透传给子面板</summary>
    public void InitUI()
    {
        Btn_Character.onClick.AddListener(() => SwitchTab(true));
        Btn_Card.onClick.AddListener(() => SwitchTab(false));

        CharacterPanel.InitUI();
        CardPanel.InitUI();

        SwitchTab(true);
    }

    /// <summary>传入角色数据引用，子面板自行处理数据读写和事件广播</summary>
    public void SetData(CabinCharacterBaseInfo info)
    {
        CardPanel.SetData(info);
        CharacterPanel.SetData(info);
    }

    /// <summary>切换主 Tab，isCharacter 为 true 显示角色面板，否则显示卡片面板</summary>
    private void SwitchTab(bool isCharacter)
    {
        CharacterPanel.gameObject.SetActive(isCharacter);
        CardPanel.gameObject.SetActive(!isCharacter);
    }
}
