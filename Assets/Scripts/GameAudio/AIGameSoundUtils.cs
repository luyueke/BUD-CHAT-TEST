using System;
using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using UnityEngine;

namespace AIGame.Base
{
    public class AIGameSoundUtils : GlobalInstance<AIGameSoundUtils>
    {
        private const string TAG = "AIGameSoundUtils";
        private GameObject _bgmNode;
        public GameObject BgmNode
        {
            get
            {
                if (_bgmNode == null)
                {
                    _bgmNode = new GameObject("AIGameBgmNode");
                    GameObject.DontDestroyOnLoad(_bgmNode);
                }
                return _bgmNode;
            }
        }

        private GameObject _soundEffectNode;
        public GameObject SoundEffectNode
        {
            get
            {
                if (_soundEffectNode == null)
                {
                    _soundEffectNode = new GameObject("AIGameSoundtNode");
                    GameObject.DontDestroyOnLoad(_soundEffectNode);
                }
                return _soundEffectNode;
            }
        }

        public string CurBgmName = "";
        public void PlayBgm(string audioName, Action<uint> ac = null)
        {
            LoggerUtils.Log($"{TAG} PlayBgm: {audioName}");
            StopBgm(CurBgmName);
            var playEvent = $"Play_{audioName}";
            // Pointone 兼容层目前不依赖 AkCallbackManager，这里仅保留上层参数兼容。
            AkSoundManager.Inst.PostEventAsync(playEvent, BgmNode);
            CurBgmName = audioName;
        }

        public void PlayBgmWithNoCb(string audioName)
        {
            LoggerUtils.Log($"{TAG} PlayBgmWithNoCb: {audioName}");
            StopBgm(CurBgmName);
            var playEvent = $"Play_{audioName}";
            AkSoundManager.Inst.PostEventAsync(playEvent, BgmNode);
            CurBgmName = audioName;
        }

        public void StopBgm(string audioName)
        {
            if (string.IsNullOrEmpty(audioName)) return;
            var stopEvent = $"Stop_{audioName}";
            AkSoundManager.Inst.PostEvent(stopEvent, BgmNode);
            CurBgmName = "";
        }

        public void StopCurrentBgm()
        {
            if (string.IsNullOrEmpty(CurBgmName)) return;
            var stopEvent = $"Stop_{CurBgmName}";
            AkSoundManager.Inst.PostEvent(stopEvent, BgmNode);
        }

        public void StopAllBgm()
        {
            AkSoundEngine.StopAll(BgmNode);
        }

        public void PostEvent(string playEvent)
        {
            AkSoundManager.Inst.PostEventAsync(playEvent, SoundEffectNode);
        }

        public void PlaySound(string soundName)
        {
            LoggerUtils.Log($"{TAG} PlaySound: {soundName}");
            var playEvent = $"Play_{soundName}";
            AkSoundManager.Inst.PostEventAsync(playEvent, SoundEffectNode);
        }

        public void PlaySound(string soundName,GameObject in_gameObjectID)
        {
            LoggerUtils.Log($"{TAG} PlaySound: {soundName}");
            var playEvent = $"Play_{soundName}";
            AkSoundManager.Inst.PostEventAsync(playEvent, in_gameObjectID);
        }

        public void PlaySound(string group,string switchState,string soundName,GameObject in_gameObjectID)
        {
            LoggerUtils.Log($"{TAG} PlayEmoSound: group:{group}  switchState:{switchState }  eventName:{soundName}");
            var playEvent = $"Play_{soundName}";
            AkSoundManager.Inst.PlaySound(group, switchState,playEvent,in_gameObjectID); 
        }

        //注意，Sound不一定有Stop，得看wwise有没有对应Event
        public void StopSound(string soundName)
        {
            if (string.IsNullOrEmpty(soundName)) return;
            var stopEvent = $"Stop_{soundName}";
            AkSoundManager.Inst.PostEventAsync(stopEvent, SoundEffectNode);
        }

        public void StopSound(string soundName,GameObject in_gameObjectID)
        {
            if (string.IsNullOrEmpty(soundName)) return;
            var stopEvent = $"Stop_{soundName}";
            AkSoundManager.Inst.PostEventAsync(stopEvent, in_gameObjectID);
        }

        public void StopAllSound()
        {
            AkSoundEngine.StopAll(SoundEffectNode);
            AkSoundEngine.StopAll(BgmNode);
            AkSoundManager.Inst.StopGameMusic();
        }

        public override void Release()
        {
            base.Release();
            GameObject.Destroy(BgmNode);
            GameObject.Destroy(SoundEffectNode);
        }
    }
}
