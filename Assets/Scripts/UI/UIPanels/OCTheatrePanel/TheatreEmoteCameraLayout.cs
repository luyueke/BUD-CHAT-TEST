using UnityEngine;

/// <summary>
/// 剧场动作镜头基准值与计算的唯一来源。
///
/// 编辑器预览 <see cref="TheatreEditorEmoteShowcase"/> 与正式游玩 <see cref="TheatreGamePanel"/>
/// 共用本类，保证编辑器里设定并保存的 customPosition / customRotation / scale 偏移，
/// 在游玩时呈现的镜头效果与编辑器完全一致。
///
/// 约定（两边必须一致）：
///   * 相机 prefab 的 localPosition 即为基准位，其 Y/Z 作为基准（单人、双人都用同一基准 Z）。
///   * 保存的 scale 作为相机 Z 偏移、customPosition(x,y) 作为相机 X/Y 偏移，叠加在基准位之上。
///   * 双人时镜头额外在 X 轴偏移 <see cref="DoubleCameraXOffset"/>。
///   * 注意：avatarCamera 是透视相机，缩放完全靠 Z 距离实现，
///     不要再用 orthographicSize 去做单/双人取景（对透视相机无效，历史上曾导致两边不一致）。
/// </summary>
public static class TheatreEmoteCameraLayout
{
    /// 双人动作时镜头在 X 轴的基准偏移（让两名演员同时入镜）。
    public const float DoubleCameraXOffset = -70f;

    /// PGC（官方）双人动作时，演员根节点 characterRoot 在 X 轴的位移。
    /// 与相机 X(<see cref="DoubleCameraXOffset"/>) 配合构成官方双人动作的取景；仅官方双人需要，UGC/单人为 0。
    public const float DoublePgcCharacterRootX = 50f;

    /// <summary>
    /// 演员根节点 characterRoot 的 X 位移。两边（编辑器/游玩）必须一致，
    /// 否则编辑器设定的镜头到游玩时会因角色整体平移而看起来 X 不一致。
    /// </summary>
    public static float GetCharacterRootX(bool isPgc, bool isDouble)
        => (isPgc && isDouble) ? DoublePgcCharacterRootX : 0f;

    /// <summary>
    /// 演员根节点的基准旋转（叠加 customRotation 之前）。
    /// 仅 PGC 双人动作需要转 -90°，使两名演员侧身同框。
    /// </summary>
    public static Vector3 GetBaseCharacterRotation(bool isPgc, bool isDouble)
        => (isPgc && isDouble) ? new Vector3(0f, -90f, 0f) : Vector3.zero;

    /// <summary>
    /// 相机基准 localPosition（叠加保存/手势偏移之前）。
    /// </summary>
    /// <param name="cameraDefaultPos">相机 prefab 默认 localPosition，取其 Y/Z 作为基准。</param>
    /// <param name="isPgc">是否 PGC 动作。</param>
    /// <param name="isDouble">是否双人动作。</param>
    /// <param name="pgcConfigCameraX">PGC 动作配置表 cameraPos.x（非 PGC 传 0）。</param>
    public static Vector3 GetBaseCameraPosition(
        Vector3 cameraDefaultPos, bool isPgc, bool isDouble, float pgcConfigCameraX)
    {
        float baseX = (isDouble ? DoubleCameraXOffset : 0f) + (isPgc ? pgcConfigCameraX : 0f);
        return new Vector3(baseX, cameraDefaultPos.y, cameraDefaultPos.z);
    }

    /// <summary>
    /// 在基准位上叠加偏移，得到相机最终 localPosition。
    /// posOffset = customPosition(x,y)，scaleOffset = scale。
    /// </summary>
    public static Vector3 GetCameraLocalPosition(
        Vector3 cameraDefaultPos, bool isPgc, bool isDouble, float pgcConfigCameraX,
        Vector2 posOffset, float scaleOffset)
    {
        Vector3 basePos = GetBaseCameraPosition(cameraDefaultPos, isPgc, isDouble, pgcConfigCameraX);
        return new Vector3(
            basePos.x + posOffset.x,
            basePos.y + posOffset.y,
            basePos.z + scaleOffset);
    }
}
