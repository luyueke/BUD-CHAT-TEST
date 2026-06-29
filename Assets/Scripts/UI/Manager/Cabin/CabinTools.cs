using Es;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using UI.Manager;

/// <summary>
/// Author:
/// Desc: BOX通用工具方法
/// Date: 26-04-09
/// </summary>
public static class CabinTools
{
    /// <summary>
    /// 从皮肤包列表中取出当前默认使用的皮肤（isDefault == 1）
    /// </summary>
    public static SkinPackInfo GetDefaultSkin(List<SkinPackInfo> skinPack)
    {
        if (skinPack == null) return null;
        foreach (var skin in skinPack)
        {
            if (skin.isDefault == 1)
                return skin;
        }
        return null;
    }

    /// <summary>
    /// 根据默认配置构建 PendingEmoteData（待机循环 + 单次随机动作）
    /// </summary>
    public static PendingEmoteData BuildDefaultPendingEmote(CabinDefCharacterConfig defConfig)
    {
        var result = new PendingEmoteData()
        {
            emoteList = new List<pEmoteData>(),
            // 默认预置内置待机动画：待机（leisure）和站立（default）
            loopEmoteList = new List<pEmoteData>
            {
                new pEmoteData { emoteId = "leisure" },
                //new pEmoteData { emoteId = "default" },
            },
        };
        if (defConfig == null)
        {
            return result;
        }
        if (!string.IsNullOrEmpty(defConfig.StandLoop))
        {
            foreach (var id in defConfig.StandLoop.Split(','))
            {
                var trimId = id.Trim();
                if (!string.IsNullOrEmpty(trimId))
                {
                    result.loopEmoteList.Add(new pEmoteData { emoteId = trimId });
                }
            }
        }
        if (!string.IsNullOrEmpty(defConfig.StandSole))
        {
            foreach (var id in defConfig.StandSole.Split(','))
            {
                var trimId = id.Trim();
                if (!string.IsNullOrEmpty(trimId))
                {
                    result.emoteList.Add(new pEmoteData { emoteId = trimId });
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 根据默认配置构建初始卡面信息
    /// </summary>
    public static CabinCoverInfo BuildDefaultCoverInfo(CabinDefCharacterConfig defConfig)
    {
        var detail = new CabinCoverDetail()
        {
            colorId = defConfig != null ? defConfig.CardColorID : 1,
            sizeVec3 = defConfig != null ? defConfig.CardSize : Vector3.one,
            posVec3 = defConfig != null ? defConfig.CardPos : new Vector3(0f, -1f, 0f),
        };
        if (defConfig != null && !string.IsNullOrEmpty(defConfig.PoseAnimID))
        {
            detail.poseId = defConfig.PoseAnimID;
            detail.poseResourceType = (int)ResourceType.Pose;
        }
        return new CabinCoverInfo()
        {
            desc = "",
            detail = JsonConvert.SerializeObject(detail),
        };
    }

    /// <summary>
    /// 超过字数上限就显示前 maxLength 个字加...
    /// </summary>
    public static string TruncateName(string name, int maxLength)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }
        if (name.Length > maxLength)
            return name.Substring(0, maxLength) + "...";

        return name;
    }

    /// <summary>
    /// 将互动配置的动画名称设置到指定 Text 组件。
    /// PGC 动画直接从配置表取名（leisure→"待机"，default→"站立"）；
    /// UGC 动画通过服务端接口异步获取名称。名称超过 6 个字截断为 5 字 + "…"。
    /// </summary>
    /// <param name="txtName">目标 Text 组件</param>
    /// <param name="emoteId">PGC 动画 ID（为空时视为 UGC）</param>
    /// <param name="ugcId">UGC 动画 ID（emoteId 为空时使用）</param>
    /// <param name="ugcCover">UGC 封面 URL（非空才视为有效 UGC）</param>
    public static void SetInteractEmoteNameText(Text txtName, string emoteId, string ugcId, string ugcCover)
    {
        txtName.text = string.Empty;
        if (!string.IsNullOrEmpty(emoteId) && UniqueType.IsPgc(emoteId))
        {
            string emoteName;
            if (emoteId == "leisure" || emoteId == "default")
            {
                emoteName = emoteId == "leisure" ? "待机" : "站立";
            }
            else
            {
                emoteName = PgcUtils.GetEmoteName(emoteId) ?? emoteId;
            }
            txtName.text = emoteName.Length > 6 ? emoteName.Substring(0, 5) + "…" : emoteName;
        }
        else if (!string.IsNullOrEmpty(ugcId) && !string.IsNullOrEmpty(ugcCover))
        {
            AssetsDataManager.GetUgcAnimInfo(ugcId, (isSuccess, serverData) =>
            {
                if (!isSuccess || serverData == null)
                    return;

                if (txtName == null)
                    return;

                var name = serverData.animInfo?.name;
                if (!string.IsNullOrEmpty(name))
                {
                    txtName.text = name.Length > 6 ? name.Substring(0, 5) + "…" : name;
                }
            });
        }
    }

    /// <summary>
    /// 将逗号分隔的 ID 字符串拆分为列表，忽略空项
    /// </summary>
    public static List<string> SplitIds(string idString)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(idString))
        {
            return result;
        }
        foreach (var id in idString.Split(','))
        {
            var trimId = id.Trim();
            if (!string.IsNullOrEmpty(trimId))
            {
                result.Add(trimId);
            }
        }
        return result;
    }

    /// <summary>
    /// 为新建扩展包（皮肤包）应用默认动画/唤醒/口令数据，完成后打开 IncubationCabinPanel。
    /// packInfo 需预先填好 characterId / name / skinPack / coverInfo 等基础字段。
    /// </summary>
    /// <param name="onTakePhoto">拍照委托（signature 与 TakePhotoForNewRole 一致），传 null 时跳过拍照直接打开面板</param>
    public static void ApplyDefaultDataToExtPackAndOpenPanel(
        CabinCharacterPackInfo packInfo,
        string toneId,
        Action<CabinCharacterUgcInfo, Action<string>> onTakePhoto = null,
        Action onOpened = null)
    {
        var defConfigList = DataTables.GetCabinDefCharacterConfigList();
        var defConfig = defConfigList != null && defConfigList.Count > 0 ? defConfigList[0] : null;

        // 扩展包不使用配置表默认动画，仅预置内置待机选项
        packInfo.pendingEmote = new PendingEmoteData()
        {
            emoteList = new List<pEmoteData>(),
            loopEmoteList = new List<pEmoteData>(),
        };
        packInfo.activation = new List<characterInteraction>();
        packInfo.voiceCommands = new List<voiceCommands>();
        void TryOpen()
        {
            UIManager.Inst.OpenPanel(PanelId.IncubationCabinPanel, packInfo, CabinEntryType.Create, toneId);
            onOpened?.Invoke();
        }

        if (onTakePhoto != null)
        {
            var tempInfo = new CabinCharacterUgcInfo()
            {
                skinPack = packInfo.skinPack,
                coverInfo = packInfo.coverInfo,
            };
            onTakePhoto(tempInfo, url =>
            {
                if (!string.IsNullOrEmpty(url))
                {
                    packInfo.cover = url;
                    var defaultSkin = GetDefaultSkin(packInfo.skinPack);
                    if (defaultSkin != null)
                    {
                        defaultSkin.cover = url;
                    }
                }
                TryOpen();
            });
        }
        else
        {
            TryOpen();
        }
    }


    public static bool isLiuHe = File.Exists("C:/liuhe.txt");
    public static bool isYueKe = File.Exists("C:/yueke.txt");
}
