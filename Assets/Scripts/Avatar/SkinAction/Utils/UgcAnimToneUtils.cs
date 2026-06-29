using System.Collections;
using System.Collections.Generic;
using Es;
using GameData.BaseInfo;
using UnityEngine;

public static class UgcAnimToneUtils
{
    public static AnimMusicInfo CovertPgcConfigToUgcInfo(string pgcId)
    {
        AnimMusicInfo info = new AnimMusicInfo();
        var config = GetPgcToneConfig(pgcId);

        if (config != null)
        {
            info.id = pgcId;
            info.name = config.toneName;
            info.frameLen = config.bgmLength;
            info.isPgc = 1;
        }

        return info;
    }

    public static UgcAnimBgmConfig GetPgcToneConfig(string pgcId)
    {
        var pgcToneConfig = Es.DataTables.GetUgcAnimBgmConfig(pgcId);
        if (pgcToneConfig == null)
        {
            Debug.LogError("GetPgcToneConfig 没找到");
            return null;
        }
        return pgcToneConfig;
    }

    public static string GetPgcTonePlayEventName(string pgcId)
    {
        var config = GetPgcToneConfig(pgcId);
        return "Play" + config?.eventName;
    }
    
    public static string GetPgcToneStopEventName(string pgcId)
    {
        var config = GetPgcToneConfig(pgcId);
        return "Stop" + config?.eventName;
    }
}
