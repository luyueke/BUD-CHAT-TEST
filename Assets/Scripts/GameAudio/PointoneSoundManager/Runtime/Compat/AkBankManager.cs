using System.Collections.Generic;
using Pointone.Sound;

public static class AkBankManager
{
    private static readonly HashSet<string> s_loaded = new HashSet<string>();

    public static void LoadInitBank()
    {
        // 标记 Init 已加载，保持与旧项目调用兼容
        PointoneAudioManager.Instance.MarkBankLoaded("Init");
        s_loaded.Add("Init");
    }

    public static void LoadBank(string bankName, bool decodeBank, bool saveDecodedBank)
    {
        if (string.IsNullOrEmpty(bankName)) return;
        PointoneAudioManager.Instance.MarkBankLoaded(bankName);
        s_loaded.Add(bankName);
    }

    public static bool IsLoaded(string bankName)
    {
        if (string.IsNullOrEmpty(bankName)) return false;
        return s_loaded.Contains(bankName);
    }
}


