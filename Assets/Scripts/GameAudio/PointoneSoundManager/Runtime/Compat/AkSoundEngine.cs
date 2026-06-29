using UnityEngine;

// 零改动兼容入口：对齐常用 AkSoundEngine API，内部转调 Pointone.Sound.PointoneSoundManager
public static class AkSoundEngine
{
    public const uint AK_INVALID_PLAYING_ID = Pointone.Sound.PointoneSoundManager.AK_INVALID_PLAYING_ID;

    public static void AddBasePath(string path) => Pointone.Sound.PointoneSoundManager.AddBasePath(path);
    public static void ClearBanks() => Pointone.Sound.PointoneSoundManager.ClearBanks();
    public static Pointone.Sound.AKRESULT LoadBank(string bankName, out uint bankId) => Pointone.Sound.PointoneSoundManager.LoadBank(bankName, out bankId);
    public static uint GetIDFromString(string name) => Pointone.Sound.PointoneSoundManager.GetIDFromString(name);

    public static uint PostEvent(string eventName, GameObject gameObject) => Pointone.Sound.PointoneSoundManager.PostEvent(eventName, gameObject);
    public static uint PostEvent(uint eventId, GameObject gameObject) => Pointone.Sound.PointoneSoundManager.PostEvent(eventId, gameObject);
    public static uint PostEvent(string eventName, GameObject gameObject, uint in_uFlags, object in_pfnCallback, object in_pCookie)
        => Pointone.Sound.PointoneSoundManager.PostEvent(eventName, gameObject, in_uFlags, in_pfnCallback, in_pCookie);

    public static void StopAll() => Pointone.Sound.PointoneSoundManager.StopAll();
    public static void StopAll(GameObject target) => Pointone.Sound.PointoneSoundManager.StopAll(target);

    public static void SetRTPCValue(string parameterName, float value) => Pointone.Sound.PointoneSoundManager.SetRTPCValue(parameterName, value);
    public static void SetSwitch(string group, string state, GameObject target) => Pointone.Sound.PointoneSoundManager.SetSwitch(group, state, target);
    public static Pointone.Sound.AKRESULT SeekOnEvent(uint eventId, GameObject target, float percent, bool fromBeginning, uint playingId)
        => Pointone.Sound.PointoneSoundManager.SeekOnEvent(eventId, target, percent, fromBeginning, playingId);
}


