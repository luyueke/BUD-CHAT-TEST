using Message;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 细节调整面板，通过三个 Slider 控制角色的缩放比例及上下/左右位置
///       Slider 值域均为 [0, 1]，内部映射到实际物理范围后直接广播事件
/// Date:26-04-01
/// </summary>
public class DraftBoxCharacterDetailPanel : MonoBehaviour
{
    [SerializeField] private Slider Slider_Scale;   // 角色缩放 Slider，映射范围 0.5 ~ 3.0
    [SerializeField] private Slider Slider_PosY;    // 上下位置 Slider，映射范围 -3.0 ~ 1.0
    [SerializeField] private Slider Slider_PosX;    // 左右位置 Slider，映射范围 -1.0 ~ 1.0

    private const float ScaleMin = 1.87f;
    private const float ScaleMax = 4.0f;
    private const float PosXMin = -1.0f;
    private const float PosXMax = 1.0f;
    private const float PosYMin = -2.5f;
    private const float PosYMax = 0f;

    /// <summary>初始化 Slider 默认值及值变更监听，变更时直接广播事件</summary>
    public void InitUI()
    {
        Slider_Scale.minValue = 0f;
        Slider_Scale.maxValue = 1f;
        Slider_Scale.value = Mathf.InverseLerp(ScaleMin, ScaleMax, 0.6f);
        Slider_Scale.onValueChanged.AddListener(v =>
            MessageHelper.Broadcast(MessageName.OnCabinEditorScaleChanged, Mathf.Lerp(ScaleMin, ScaleMax, v)));

        Slider_PosY.minValue = 0f;
        Slider_PosY.maxValue = 1f;
        Slider_PosY.value = 0.4f;
        Slider_PosY.onValueChanged.AddListener(v =>
            MessageHelper.Broadcast(MessageName.OnCabinEditorPosYChanged, Mathf.Lerp(PosYMin, PosYMax, v)));

        Slider_PosX.minValue = 0f;
        Slider_PosX.maxValue = 1f;
        Slider_PosX.value = 0.5f;
        Slider_PosX.onValueChanged.AddListener(v =>
            MessageHelper.Broadcast(MessageName.OnCabinEditorPosXChanged, Mathf.Lerp(PosXMin, PosXMax, v)));
    }

    /// <summary>根据服务器数据恢复 Slider 位置，不触发广播事件</summary>
    public void SetData(CabinCoverDetail detail)
    {
        Slider_Scale.SetValueWithoutNotify(Mathf.InverseLerp(ScaleMin, ScaleMax, detail.sizeVec3.x));
        Slider_PosY.SetValueWithoutNotify(Mathf.InverseLerp(PosYMin, PosYMax, detail.posVec3.y));
        Slider_PosX.SetValueWithoutNotify(Mathf.InverseLerp(PosXMin, PosXMax, detail.posVec3.x));
    }
}
