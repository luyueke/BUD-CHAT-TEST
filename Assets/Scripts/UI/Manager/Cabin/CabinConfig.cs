using System.Collections.Generic;

/// <summary>
/// 孵化舱角色互动动作数量上限配置。
/// 本体（CabinCharacterUgcInfo）与皮肤（CabinCharacterPackInfo）的上限不同，统一在此处维护。
/// </summary>
public static class CabinConfig
{
    // ────────────────────────────────────────────────────────────
    // 孵化舱编辑面板（IncubationCabinPanel）：本体 (CabinCharacterUgcInfo) 上限
    // ────────────────────────────────────────────────────────────

    /// <summary>本体：待机主动作（循环）最多可添加数量</summary>
    public const int UgcLoopEmoteMax = 2;

    /// <summary>本体：待机表演动作（非循环）最多可添加数量</summary>
    public const int UgcNonLoopEmoteMax = 4;

    /// <summary>本体：唤醒动作最多可添加数量</summary>
    public const int UgcActivationMax = 2;

    /// <summary>本体：口令互动最多可添加数量</summary>
    public const int UgcVoiceCommandMax = 2;

    // ────────────────────────────────────────────────────────────
    // 孵化舱编辑面板（IncubationCabinPanel）：皮肤 (CabinCharacterPackInfo) 上限
    // ────────────────────────────────────────────────────────────

    /// <summary>皮肤：待机主动作（循环）最多可添加数量</summary>
    public const int PackLoopEmoteMax = 1;

    /// <summary>皮肤：待机表演动作（非循环）最多可添加数量</summary>
    public const int PackNonLoopEmoteMax = 2;

    /// <summary>皮肤：唤醒动作最多可添加数量</summary>
    public const int PackActivationMax = 1;

    /// <summary>皮肤：口令互动最多可添加数量</summary>
    public const int PackVoiceCommandMax = 1;

    // ────────────────────────────────────────────────────────────
    // 角色互动编辑面板（EditRoleInteractionView）上限
    // ────────────────────────────────────────────────────────────

    /// <summary>EditRoleInteractionView：待机主动作（循环）最多可添加数量</summary>
    public const int EditViewLoopEmoteMax = 8;

    /// <summary>EditRoleInteractionView：待机表演动作（非循环）最多可添加数量</summary>
    public const int EditViewNonLoopEmoteMax = 12;

    /// <summary>EditRoleInteractionView：唤醒动作最多可启用数量（disabled=0 的条目数）</summary>
    public const int EditViewActivationEnabledMax = 5;

    /// <summary>EditRoleInteractionView：指令互动最多可启用数量（disabled=0 的条目数）</summary>
    public const int EditViewVoiceCommandEnabledMax = 8;

    // ────────────────────────────────────────────────────────────
    // 辅助方法：根据 info 实际类型返回对应上限（IncubationCabinPanel 用）
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// 获取指定角色信息对应的待机主动作（循环）上限。
    /// </summary>
    /// <param name="info">角色信息，实际可为 CabinCharacterUgcInfo（本体）或 CabinCharacterPackInfo（皮肤）</param>
    /// <returns>可添加的最大数量</returns>
    public static int GetLoopEmoteMax(CabinCharacterBaseInfo info)
    {
        return info is CabinCharacterUgcInfo ? UgcLoopEmoteMax : PackLoopEmoteMax;
    }

    /// <summary>
    /// 获取指定角色信息对应的待机表演动作（非循环）上限。
    /// </summary>
    /// <param name="info">角色信息，实际可为 CabinCharacterUgcInfo（本体）或 CabinCharacterPackInfo（皮肤）</param>
    /// <returns>可添加的最大数量</returns>
    public static int GetNonLoopEmoteMax(CabinCharacterBaseInfo info)
    {
        return info is CabinCharacterUgcInfo ? UgcNonLoopEmoteMax : PackNonLoopEmoteMax;
    }

    /// <summary>
    /// 获取指定角色信息对应的唤醒动作上限。
    /// </summary>
    /// <param name="info">角色信息，实际可为 CabinCharacterUgcInfo（本体）或 CabinCharacterPackInfo（皮肤）</param>
    /// <returns>可添加的最大数量</returns>
    public static int GetActivationMax(CabinCharacterBaseInfo info)
    {
        return info is CabinCharacterUgcInfo ? UgcActivationMax : PackActivationMax;
    }

    /// <summary>
    /// 获取指定角色信息对应的口令互动上限。
    /// </summary>
    /// <param name="info">角色信息，实际可为 CabinCharacterUgcInfo（本体）或 CabinCharacterPackInfo（皮肤）</param>
    /// <returns>可添加的最大数量</returns>
    public static int GetVoiceCommandMax(CabinCharacterBaseInfo info)
    {
        return info is CabinCharacterUgcInfo ? UgcVoiceCommandMax : PackVoiceCommandMax;
    }
}
