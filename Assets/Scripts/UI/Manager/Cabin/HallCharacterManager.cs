using System;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;

/// <summary>
/// 大厅 AI 伙伴角色的 UI 侧业务入口。
/// 数据由 AccountDataManager.HallCharacterInfo（GameData 复刻类型 HallCharacterInfo，可见性 isHidden 内置）持有；
/// 持久化走 /image/character/set（请求体 { characterInfo }，characterInfo 即 HallCharacterInfo，含 isHidden）。
/// SettingPanel 选伙伴/换皮肤时传入 UI 的 CabinCharacterUgcInfo + 所选皮肤，由本类收窄复刻为 HallCharacterInfo 后下发。
/// 取代旧的大厅 AIBuddyInfo（身份/形象/可见性；idle 暂仍走 AIBuddyInfo.idleData）。
/// </summary>
public static class HallCharacterManager
{
    /// <summary>当前大厅伙伴角色（可能为 null：未配置）。</summary>
    public static HallCharacterInfo Current => AccountDataManager.Inst.HallCharacterInfo;

    /// <summary>大厅是否隐藏伙伴：无角色或 isHidden==1 视为隐藏。</summary>
    public static bool IsHidden
    {
        get
        {
            var info = Current;
            return info == null || info.isHidden == 1;
        }
    }

    /// <summary>当前是否已配置大厅伙伴（有有效角色 id）。</summary>
    public static bool HasCharacter
    {
        get
        {
            var info = Current;
            return info != null && !string.IsNullOrEmpty(info.id);
        }
    }

    /// <summary>当前装备皮肤的 avatarJson（大厅渲染用）。</summary>
    public static string CurrentSkinAvatarJson()
    {
        return Current?.GetEquippedAvatarJson();
    }

    /// <summary>
    /// 设置大厅伙伴（选伙伴/换皮肤）：用 UI 完整角色 + 所选皮肤收窄复刻为 HallCharacterInfo 持久化。
    /// 先乐观更新本地缓存（UI 立即一致），再 POST /image/character/set。
    /// </summary>
    public static void SetHallCharacter(CabinCharacterUgcInfo full, SkinPackInfo equippedSkin, bool isHidden, Action<bool> onDone = null)
    {
        if (full == null)
        {
            onDone?.Invoke(false);
            return;
        }
        var hall = BuildHallInfo(full, equippedSkin, isHidden);
        Persist(hall, onDone);
    }

    /// <summary>仅改可见性（沿用当前角色），用于大厅展示/隐藏切换。</summary>
    public static void SetVisibility(bool isHidden, Action<bool> onDone = null)
    {
        var cur = Current;
        if (cur == null)
        {
            onDone?.Invoke(false);
            return;
        }
        cur.isHidden = isHidden ? 1 : 0;
        Persist(cur, onDone);
    }

    private static void Persist(HallCharacterInfo hall, Action<bool> onDone)
    {
        // 乐观更新本地缓存（避免后续可见性判断读到旧值导致重复请求/覆盖）
        AccountDataManager.Inst.UpdateHallCharacter(hall);

        var req = new SetCharacterImageReq { characterInfo = hall };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.SetCharacterImage,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: _ => { onDone?.Invoke(true); },
            onFail: err =>
            {
                LoggerUtils.LogError("[HallCharacter] SetHallCharacter 失败:" + err);
                onDone?.Invoke(false);
            },
            retryCount: 3);
    }

    // UI 完整角色 + 所选皮肤 -> GameData 复刻类型（skinPack 收窄为当前装备皮肤）
    private static HallCharacterInfo BuildHallInfo(CabinCharacterUgcInfo full, SkinPackInfo skin, bool isHidden)
    {
        var hall = new HallCharacterInfo
        {
            id = full.id,
            name = full.GetName(),
            pubName = full.pubName,
            targetUgcId = full.targetUgcId,
            isHidden = isHidden ? 1 : 0,
            skinPack = new List<HallSkinPack>(),
        };
        if (skin != null)
        {
            hall.skinPack.Add(new HallSkinPack
            {
                packId = skin.packId,
                avatarJson = skin.avatarJson,
                isDefault = skin.isDefault,
                cover = skin.cover,
            });
        }
        return hall;
    }
}

/// <summary>/image/character/set 请求体（可见性 isHidden 在 characterInfo 内）。</summary>
public class SetCharacterImageReq
{
    public HallCharacterInfo characterInfo;
}
