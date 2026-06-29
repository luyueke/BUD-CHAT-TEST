// @Author: YangJie
// @Description:
// @Date:  2023/08/08
// @Modify:

using System;
using System.Collections;
using System.Reflection;
using Es;
using Game.Audio;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsComponents;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using UIAgent;
using UnityEngine;

using UnityEngine.Audio;

using UnityEngine.Events;
using UnityEngine.Networking;
using xasset;
using Version = System.Version;

namespace Game.MapSetting {
    public class BgMusicManager : BaseMapSettingManager<BgMusicManager>, IModeManager {

        public bool IsPlaying = true;
        BgMusicComponent bgMusicComp;
        private UnityEngine.Object bgAudioMixer;
        private Type audioMixerType;
        private AssetRequest audioMixerRequest;
        public override void OnCreateByData() {
            base.OnCreateByData();
            bgMusicComp = GameMapSettingManager.Inst.settingEntity.GetOrAddComp<BgMusicComponent>();

            if (CompareVersion(DeviceInfoManager.Inst.DeviceBaseData.version, "1.0.6") >= 0) {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies) {
                    audioMixerType = assembly.GetType("UnityEngine.Audio.AudioMixer");
                    if (audioMixerType != null) {
                        break;
                    }
                }
                if (audioMixerType != null) {
                    audioMixerRequest = xasset.Asset.Load("Assets/Arts/Config/BGAudioMixer.mixer", audioMixerType);
                    bgAudioMixer = audioMixerRequest.asset;
                }
            } else {
                LoggerUtils.Log("当前版本:" + DeviceInfoManager.Inst.DeviceBaseData.version + " 版本过低，不支持背景音乐");
            }

        }

        public void OnEdit() {
            AkSoundManager.Inst.StopNoiseSound();
            AkSoundManager.Inst.StopGameMusic();
        }
        public void OnPlay() {
            PlayAllMusic();
        }
        public void OnGuest() {
            PlayAllMusic();
        }


        public override void Init() {
            base.Init();
            MessageHelper.AddListener<bool>(MessageName.ReduceMusic, ReduceMusic);
            MessageHelper.AddListener<bool>(MessageName.RestoreMusic, RestoreMusic);
        }

        public override void Release() {
            base.Release();
            if (AkSoundManager.HasInstance) {
                AkSoundManager.Inst.StopGameMusic();
                AkSoundManager.Inst.StopNoiseSound();
                var audioSource = AkSoundManager.Inst.GameMusicNode.GetComponent<AudioSource>();
                if (audioSource != null) {
                    audioSource.clip = null;
                }
            }
            MessageHelper.RemoveListener<bool>(MessageName.ReduceMusic, ReduceMusic);
            MessageHelper.RemoveListener<bool>(MessageName.RestoreMusic, RestoreMusic);
            audioMixerRequest = null;
        }


        public void PlayAllMusic() {
			if (bgMusicComp == null || !IsPlaying)
                return;
            if (!string.IsNullOrEmpty(bgMusicComp.ugcMusicUrl)) {
                PlayUGCMusic();
            } else if (!string.IsNullOrEmpty(bgMusicComp.bgId)) {
                PlayPGCMusic();
            }

            if (!string.IsNullOrEmpty(bgMusicComp.noiseId)) {
                var audioData = Es.DataTables.GetGameNoiseAudioData(bgMusicComp.noiseId);
                if (audioData != null && !string.IsNullOrEmpty(audioData.State)) {
                    AkSoundManager.Inst.PlayNoiseSound(audioData.State);
                }
            }
        }


        /// <summary>
        /// 播放UGC 音乐
        /// </summary>
        /// <param name="calculateLoudness"> 是否计算音频响度</param>
        /// <param name="onCallBack"></param>
        public void PlayUGCMusic(Action<AudioSource> callback = null) {
            if (bgMusicComp == null || !IsPlaying)
                return;
            var url = bgMusicComp.ugcMusicUrl;
            var audioSource = AkSoundManager.Inst.GameMusicNode.GetOrAddComponent<AudioSource>();
            audioSource.volume = AkSoundManager.Inst.GetBGMAudioVolume();
            audioSource.Stop();
            audioSource.clip = null;
            CoroutineManager.Inst.StartCoroutine(LoadUGCAudio(url, clip => {
                if (Mathf.Abs(bgMusicComp.loudness - 0) > float.Epsilon) {
                    // 新上传的背景音乐才有 loudness 值，默认值为 0
                    SetLoudness(audioSource, bgAudioMixer, GameConsts.BGMusicLoudness - bgMusicComp.loudness);
                }

                if (url == bgMusicComp.ugcMusicUrl && audioSource != null) {
                    audioSource.clip = clip;
                    audioSource.loop = true;
                    audioSource.Play();
                    callback?.Invoke(audioSource);
                }
            }, err => {
                Debug.LogError($"Load UGC music failed: {err} {url}");
            }));
        }

        public void PlayURlMusic(string url)
        {
            var audioSource = AkSoundManager.Inst.GameMusicNode.GetOrAddComponent<AudioSource>();
            audioSource.volume = AkSoundManager.Inst.GetBGMAudioVolume();
            audioSource.Stop();
            audioSource.clip = null;
            CoroutineManager.Inst.StartCoroutine(LoadUGCAudio(url, clip => {
                if (url == bgMusicComp.ugcMusicUrl && audioSource != null)
                {
                    audioSource.clip = clip;
                    audioSource.loop = true;
                    audioSource.Play();
                }
            }, err => {
                Debug.LogError($"Load UGC music failed: {err} {url}");
            }));
        }

        public void ReduceMusic(bool onlyUGCMusic = false) {
            if (bgMusicComp == null)
                return;
            if (string.IsNullOrEmpty(bgMusicComp.bgId) && !string.IsNullOrEmpty(bgMusicComp.ugcMusicUrl)) {
                var audioSource = AkSoundManager.Inst.GameMusicNode.GetOrAddComponent<AudioSource>();
                audioSource.volume = AkSoundManager.Inst.GetBGMAudioVolume() * 0.4f;
            } else if (!onlyUGCMusic) {
                AkSoundManager.Inst.ReduceMusic();
            }
        }

        public void RestoreMusic(bool onlyUGCMusic = false)
        {
            if (bgMusicComp == null)
                return;
            if (string.IsNullOrEmpty(bgMusicComp.bgId) && !string.IsNullOrEmpty(bgMusicComp.ugcMusicUrl)) {
                var audioSource = AkSoundManager.Inst.GameMusicNode.GetOrAddComponent<AudioSource>();
                audioSource.volume = AkSoundManager.Inst.GetBGMAudioVolume();
            } else if (!onlyUGCMusic) {
                AkSoundManager.Inst.RestoreMusic();
            }
        }




        public void PlayPGCMusic() {
            if (!IsPlaying)
                return;
            var audioData = Es.DataTables.GetGameBgAudioData(bgMusicComp.bgId);
            if (audioData != null && !string.IsNullOrEmpty(audioData.State)) {
                AkSoundManager.Inst.PlayGameMusic(audioData.State);
            } else {
                AkSoundManager.Inst.StopGameMusic();
            }
        }

        public void StopGameMusicNode()
        { 
            AkSoundManager.Inst.StopGameMusicNode();
        }

        public void PauseGameMusicNode()
        { 
            AkSoundManager.Inst.PauseGameMusicNode();
        }
        public void ResumeGameMusicNode()
        { 
            AkSoundManager.Inst.ResumeGameMusicNode();
        }

        public void SetUGCMusic(string url, int loudness = 0)
        {
            if (!string.IsNullOrEmpty(url))
            {
                bgMusicComp.ugcMusicUrl = url;
                bgMusicComp.bgId = "";
                bgMusicComp.loudness = loudness;
                GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().gameSetting.bgMusicUrl = url;
            }
            else
            {
                bgMusicComp.ugcMusicUrl = url;
                bgMusicComp.loudness = 0;
                GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().gameSetting.bgMusicUrl = url;
            }
        }


        public bool HasNoiseSetting() {
            if (bgMusicComp == null || string.IsNullOrEmpty(bgMusicComp.noiseId)) {
                return false;
            }
            return bgMusicComp.noiseId != "20000";
        }

        private IEnumerator LoadUGCAudio(string url, UnityAction<AudioClip> onSuccess, UnityAction<string> onFailure) {
            if (string.IsNullOrEmpty(url)) {
                onFailure?.Invoke("url == null");
                yield break;
            }
            if(url.Contains(GameConsts.BusinessBaseUrl))
            {
                url = url.Replace(GameConsts.BusinessBaseUrl, GameConsts.BusinessCdnUrl);
            }
            else if(url.Contains(GameConsts.AccBusinessBaseUrl))
            {
                url = url.Replace(GameConsts.AccBusinessBaseUrl, GameConsts.BusinessCdnUrl);
            }
            Uri uri = null;
            try {
                uri = new Uri(url);
            } catch (Exception e) {
                onFailure?.Invoke(e.Message);
                yield break;
            }

            string extension = System.IO.Path.GetExtension(uri.LocalPath);
            var request = UnityWebRequestMultimedia.GetAudioClip(uri, GetAudioTypeFromExtension(extension));
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success) {
                var clip = DownloadHandlerAudioClip.GetContent(request);
                onSuccess?.Invoke(DownloadHandlerAudioClip.GetContent(request));
            } else {
                onFailure?.Invoke(request.error);
            }

        }
        public AudioType GetAudioTypeFromExtension(string extension) {
            switch (extension.ToLower()) {
                case ".mp3":
                    return AudioType.MPEG;
                case ".wav":
                    return AudioType.WAV;
                case ".ogg":
                    return AudioType.OGGVORBIS;
                case ".aiff":
                case ".aif":
                    return AudioType.AIFF;
                case ".xm":
                    return AudioType.XM;
                case ".mod":
                    return AudioType.MOD;
                case ".it":
                    return AudioType.IT;
                case ".s3m":
                    return AudioType.S3M;
                default:
                    return AudioType.UNKNOWN;
            }
        }

        public void SetLoudness(AudioSource audioSource, UnityEngine.Object audioMixer, float loudness) {
            if (audioMixerType == null || audioMixer == null) {
                return;
            }
            var setFloatMethodInfo = audioMixerType.GetMethod("SetFloat");
            if (setFloatMethodInfo != null) {
                setFloatMethodInfo.Invoke(bgAudioMixer, new object[] { "MasterVolume",  loudness});
            }
            var findGroupsMethodInfo = audioMixerType.GetMethod("FindMatchingGroups");
            if (findGroupsMethodInfo != null) {
                var groups = (AudioMixerGroup[]) findGroupsMethodInfo.Invoke(bgAudioMixer, new object[] { "Master"});
                if (groups!= null && groups.Length > 0) {
                     audioSource.outputAudioMixerGroup = groups[0];
                }
            }
        }


        private int CompareVersion(string version1, string version2) {
            version1 = version1.Trim('v');
            version2 = version2.Trim('v');
            var versionValue1 = new Version(version1);
            var versionValue2 = new Version(version2);
            return versionValue1.CompareTo(versionValue2);
        }

    }
}
