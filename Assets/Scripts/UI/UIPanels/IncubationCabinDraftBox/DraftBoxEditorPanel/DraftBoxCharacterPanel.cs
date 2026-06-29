using ChocDino.UIFX;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 角色编辑面板，管理"角色姿势"和"细节调整"两个子 Tab 的切换及选中态样式
/// Date:26-04-01
/// </summary>
public class DraftBoxCharacterPanel : MonoBehaviour
{
    [SerializeField] private CButton Btn_Pose;              // 角色姿势 Tab 按钮

    [SerializeField] private CButton Btn_Detail;            // 细节调整 Tab 按钮

    [SerializeField] private DraftBoxCharacterPosePanel PosePanel;      // 角色姿势子面板
    [SerializeField] private DraftBoxCharacterDetailPanel DetailPanel;  // 细节调整子面板

    /// <summary>初始化按钮监听及子面板</summary>
    public void InitUI()
    {
        Btn_Pose.onClick.AddListener(() => SwitchTab(true));
        Btn_Detail.onClick.AddListener(() => SwitchTab(false));

        PosePanel.InitUI();
        DetailPanel.InitUI();

        SwitchTab(true);
    }

    /// <summary>根据服务器角色数据初始化姿势默认选中页签及细节 Slider 值</summary>
    public void SetData(CabinCharacterBaseInfo info)
    {
        if (info == null) return;
        PosePanel.SetData(info);
        DetailPanel.SetData(info.coverInfo?.GetDetail() ?? new CabinCoverDetail());
    }

    /// <summary>切换子 Tab，isPose 为 true 显示姿势面板，否则显示细节面板，同步更新按钮样式</summary>
    private void SwitchTab(bool isPose)
    {
        PosePanel.gameObject.SetActive(isPose);
        Btn_Pose.interactable = !isPose;
        DetailPanel.gameObject.SetActive(!isPose);
        Btn_Detail.interactable = isPose;
    }
}
