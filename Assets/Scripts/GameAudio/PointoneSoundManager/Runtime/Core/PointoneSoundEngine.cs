using System;
using UnityEngine;

namespace Pointone.Sound
{
    public enum AKRESULT
    {
        AK_Fail = 0,
        AK_Success = 1
    }

    // 对外静态API，命名对齐AkSoundEngine
    public static class PointoneSoundManager
    {
        public const uint AK_INVALID_PLAYING_ID = 0;
        public static void AddBasePath(string path) => PointoneAudioManager.Instance.AddBasePath(path);
        public static void ClearBanks() => PointoneAudioManager.Instance.ClearBanks();
        public static AKRESULT LoadBank(string bankName, out uint bankId)
        {
            bankId = PointoneAudioManager.Instance.GetIdFromString(bankName);
            return PointoneAudioManager.Instance.MarkBankLoaded(bankName) ? AKRESULT.AK_Success : AKRESULT.AK_Fail;
        }
        public static uint GetIDFromString(string name) => PointoneAudioManager.Instance.GetIdFromString(name);
        public static uint PostEvent(string eventName, GameObject gameObject) => PointoneAudioManager.Instance.PostEventByName(eventName, gameObject);
        public static uint PostEvent(uint eventId, GameObject gameObject) => PointoneAudioManager.Instance.PostEventById(eventId, gameObject);
        // 兼容 Wwise 常见回调签名（此处忽略回调与标志，仅确保调用兼容与编译通过）
        public static uint PostEvent(string eventName, GameObject gameObject, uint in_uFlags, object in_pfnCallback, object in_pCookie)
            => PostEvent(eventName, gameObject);
        public static void StopAll() => PointoneAudioManager.Instance.StopAll();
        public static void StopAll(GameObject target) => PointoneAudioManager.Instance.StopAll(target);
        public static void SetRTPCValue(string parameterName, float value) => PointoneAudioManager.Instance.SetRTPC(parameterName, value);
        public static void SetSwitch(string group, string state, GameObject target) => PointoneAudioManager.Instance.SetSwitch(group, state, target);
        public static AKRESULT SeekOnEvent(uint eventId, GameObject target, float percent, bool fromBeginning, uint playingId)
            => PointoneAudioManager.Instance.SeekOnEvent(eventId, target, percent, fromBeginning, playingId) ? AKRESULT.AK_Success : AKRESULT.AK_Fail;
        // Resume 事件（用于恢复 Pause_ 暂停的音频）
        public static bool ResumeEvent(string eventName, GameObject gameObject) => PointoneAudioManager.Instance.ResumeEventByName(eventName, gameObject);
    }
}


