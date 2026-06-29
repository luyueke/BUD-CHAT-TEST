using System.Collections.Generic;

/// <summary>
/// 大厅 AI 伙伴角色（characterInfo）的 GameData 侧数据模型。
/// 复刻自 UI 程序集的 CabinCharacterUgcInfo 中大厅所需的最小字段（CabinNetData.cs 由其他人维护，不动它）。
/// 用于 /image/info、/image/publicProfile 响应解析与 /image/character/set 持久化（JSON 键与服务端契约一致）。
/// 可见性 isHidden 内置于本类型（对标 AIBuddyInfo.isHidden）。
/// </summary>
public class HallCharacterInfo
{
    public string id;                 // 角色发布记录 id（SettingPanel 据此拉完整角色取全部皮肤）
    public string name;
    public string pubName;
    public string targetUgcId;        // 商品原始 UGC id
    public int isHidden;              // 1=隐藏 0=展示
    public List<HallSkinPack> skinPack = new List<HallSkinPack>(); // 大厅一般只存当前装备皮肤（收窄为单个）

    /// <summary>当前装备皮肤：优先 isDefault，缺省取首个。</summary>
    public HallSkinPack GetEquippedSkin()
    {
        if (skinPack == null || skinPack.Count == 0) return null;
        foreach (var s in skinPack)
        {
            if (s != null && s.isDefault == 1) return s;
        }
        return skinPack[0];
    }

    /// <summary>当前装备皮肤的 avatarJson（大厅渲染用）。</summary>
    public string GetEquippedAvatarJson()
    {
        return GetEquippedSkin()?.avatarJson;
    }
}

/// <summary>皮肤包（复刻 SkinPackInfo 大厅所需字段，JSON 键一致）。</summary>
public class HallSkinPack
{
    public string packId;
    public string avatarJson;
    public int isDefault;
    public string cover;
}
