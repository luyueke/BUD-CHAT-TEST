using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 结算面板 - 关于BOX子面板，显示设备 SN 码与型号信息
/// Date: 26-04-09
/// </summary>
public class CabinControllExplainPanel : CabinControllSettleSubPanel
{
    [SerializeField] private Text SnText;           // 设备 SN 码文本
    [SerializeField] private Text productModelText;     // 设备型号文本（简洁显示）
    [SerializeField] private Text productModelTextEx;   // 设备型号文本（带"型号:"前缀的扩展显示）

    // 每次面板激活时刷新设备信息
    private void OnEnable()
    {
        OnUpdateData();
    }

    // 从 CabinBoxManager 获取当前 Box 的 SN 码与型号，更新 UI 文本
    private void OnUpdateData()
    {
        var boxData = CabinBoxManager.Inst.GetCabinBudBoxData();
        if (boxData == null)
        {
            return;
        }
        SnText.text = boxData.deviceState.snCode;
        productModelText.text = boxData.deviceState.productModel;
        productModelTextEx.text = $"型号:{boxData.deviceState.productModel}";
    }
}
