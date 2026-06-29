// @Author: YangJie
// @Description:
// @Date:  2023/07/21
// @Modify:

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Pointone.Sound;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.Networking;
using xasset;

namespace Game.Audio {

    public static class StandOnAudioType {
        public static string defaultAudio = "default";
    }

    // 大厅背景音
    public enum HomePageAudio {
        Bgm_Hall_S10,
        Bgm_Hall_S11,
        Bgm_Hall_S12,
        Bgm_Hall_S13,
        Bgm_Hall_S14,
        Bgm_Hall_S15,
    }

    public class AkSoundManager : GlobalInstance<AkSoundManager> {
        public delegate string MyDelegate(GameObject hitGO, GameObject playGO);
        public MyDelegate GetFootSwitchEvent;

        private GameObject uiSoundObj;
        private P1AudioDatabase audioDatabase;
        private readonly Dictionary<string, List<string>> eventClipPathCache = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, List<PendingEventRequest>> pendingEventRequests = new Dictionary<string, List<PendingEventRequest>>();
        private readonly HashSet<string> downloadingEvents = new HashSet<string>();
        private readonly List<string> _reusableMissingPaths = new List<string>();

        private Dictionary<GameObject, GameObject> nodeDict = new Dictionary<GameObject, GameObject>();
        private string curFootGroup = String.Empty;
        private string curFootstate = String.Empty;
        private const string nodeName = "soundNode";
        private GameObject curFootBindNode;

        private GameObject homePageNode;
        private GameObject gameMusicNode;

        public GameObject GameMusicNode {
            get {
                if (gameMusicNode == null) {
                    gameMusicNode = new GameObject("GameMusicNode");
                    GameObject.DontDestroyOnLoad(gameMusicNode);
                }
                return gameMusicNode;
            }
        }

#if PACKAGE_TYPE_US
        private string BusinessCdnUrl = "https://cdn-global.joinbudapp.com/";
        private string BusinessBaseUrl = "https://us-business-1318932159.cos.na-siliconvalley.myqcloud.com/";
#else
        private string BusinessCdnUrl = "https://cdn.budapp.cn/";
        private string BusinessBaseUrl = "https://u3d-business-data-1318932159.cos.ap-beijing.myqcloud.com/";
#endif

        private GameObject gameNoiseNode;
        private GameObject GameNoiseNode {
            get {
                if (gameNoiseNode == null) {
                    gameNoiseNode = new GameObject("GameNoiseNode");
                    GameObject.DontDestroyOnLoad(gameNoiseNode);
                }
                return gameNoiseNode;
            }
        }

        private bool isOpenFootSound = true;
        public bool IsOpenFootSound {
            get => isOpenFootSound;
            set => isOpenFootSound = value;
        }

        private static Dictionary<UISoundType, string> uiSoundTypeDict = new Dictionary<UISoundType, string>
        {
            { UISoundType.UI_EnterGame_A1, "Play_UI_EnterGame_A1" },
            { UISoundType.UI_ConfirmButton_A2, "Play_UI_ConfirmButton_A2" },
            { UISoundType.UI_GetRewards_A3, "Play_UI_GetRewards_A3" },
            { UISoundType.UI_RechargeCompleted_A4, "Play_UI_RechargeCompleted_A4" },
            { UISoundType.UI_OpenPage_A5, "Play_UI_OpenPage_A5" },
            { UISoundType.UI_ShiftTab_B1, "Play_UI_ShiftTab_B1" },
            { UISoundType.UI_ShiftItems_B2, "Play_UI_Shiftitems_B2" },
            { UISoundType.UI_SwitchOptions_B3, "Play_UI_SwitchOptions_B3" },
            { UISoundType.UI_ShiftFailed_B4, "Play_UI_ShiftFailed_B4" },
            { UISoundType.UI_Return_C1, "Play_UI_Return_C1" },
            { UISoundType.UI_Cancel_C2, "Play_UI_Cancel_C2" },
            { UISoundType.UI_Delete_C5, "Play_UI_Delete_C5" },
            { UISoundType.UI_Asset_D1, "Play_UI_Asset_D1" },
            { UISoundType.UI_Send_D2, "Play_UI_Send_D2"},
            { UISoundType.UI_XiaxiaCard_Click, "Play_UI_XiaxiaCard_Click" },
            { UISoundType.UI_XiaxiaCard_Ani, "Play_UI_XiaxiaCard_Ani" },
        };

        public AkSoundManager() {
#if UNITY_EDITOR
            if (Application.platform == RuntimePlatform.OSXEditor) {
                AkSoundEngine.AddBasePath("Audio/GeneratedSoundBanks/Mac/");
            } else if (Application.platform == RuntimePlatform.WindowsEditor) {
                AkSoundEngine.AddBasePath("Audio/GeneratedSoundBanks/Windows/");
            }
#endif
            AkSoundEngine.AddBasePath(Application.streamingAssetsPath + "/Bundles/wwise/");
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Bundles", "wwise"))) {
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Bundles", "wwise"));
            }
            AkSoundEngine.AddBasePath(Application.persistentDataPath + "/Bundles/wwise/");
        }

        private void InitAudioData() {
            audioDatabase = PointoneAudioManager.Instance.GetCachedDatabase();
            if (audioDatabase == null) {
                audioDatabase = PointoneAudioManager.Instance.GetDatabaseAsset() as P1AudioDatabase;
            }
            if (audioDatabase == null) {
                LoggerUtils.Log("未找到 P1AudioDatabase，音频事件将按需加载。");
            }
        }

        public void Init() {
            globalSoundObj = GameObject.Find("WwiseGlobal");
            if (globalSoundObj == null) {
                globalSoundObj = new GameObject("WwiseGlobal");
            }
            globalSoundObj.GetOrAddComponent<PointoneAudioManager>();
            GameObject.DontDestroyOnLoad(globalSoundObj);
            InitAudioData();
        }

        private GameObject globalSoundObj;

        #region UI音效

        public void PlayUIEffectSound(string in_pszEventName) {
            if (uiSoundObj == null) {
                uiSoundObj = new GameObject("uiSoundObj");
            }
            PostEvent(in_pszEventName, uiSoundObj);
        }

        public void PlayUIEffectSound(UISoundType soundType) {
            PlayUIEffectSound(uiSoundTypeDict[soundType]);
        }

        #endregion

        #region 背景音

        private string curPlayBgm;

        public HomePageAudio CurrentHomePageAudio { get; set; } = HomePageAudio.Bgm_Hall_S15;

        public void PlayBGSound() {
            string audioName = CurrentHomePageAudio.ToString();
            if (!string.IsNullOrEmpty(curPlayBgm) && curPlayBgm == audioName) {
                return;
            }
            PostEventAsync($"Stop_{audioName}", globalSoundObj);
            PostEventAsync($"Play_{audioName}", globalSoundObj);
            curPlayBgm = audioName;
        }

        public void PlayBGSound(string soundName) {
            if (!string.IsNullOrEmpty(curPlayBgm) && curPlayBgm == soundName) {
                return;
            }
            PostEventAsync($"Stop_{soundName}", globalSoundObj);
            PostEventAsync($"Play_{soundName}", globalSoundObj);
            curPlayBgm = soundName;
        }

        public void StopBGSound() {
            curPlayBgm = "";
            var audioName = CurrentHomePageAudio.ToString();
            if (string.IsNullOrEmpty(audioName)) {
                AkSoundEngine.StopAll(globalSoundObj);
                return;
            }
            PostEventAsync($"Stop_{audioName}", globalSoundObj);
        }

        private float musicVolume = 100;
        private float sfxVolume = 100;

        public void SetBGMAudioVolume(float value) {
            float v = Mathf.Min(value, 100);
            v = Mathf.Max(v, 0);
            musicVolume = v;
            AkSoundEngine.SetRTPCValue("Music_Volume", v);
            // 同步更新 UGC BGM AudioSource 音量（PGC 由 Wwise RTPC 控制，不受影响）
            var audioSource = GameMusicNode.GetComponent<AudioSource>();
            if (audioSource != null) {
                audioSource.volume = v / 100f;
            }
        }

        public float GetBGMAudioVolume() {
            return musicVolume / 100;
        }

        public void SetSFXAudioVolume(float value) {
            float v = Mathf.Min(value, 100);
            v = Mathf.Max(v, 0);
            sfxVolume = v;
            AkSoundEngine.SetRTPCValue("SFX_Volume", v);
        }

        public float GetSFXAudioVolume() {
            return sfxVolume / 100f;
        }

        public void ReduceMusic() {
            AkSoundEngine.SetRTPCValue("Music_Volume", musicVolume * 0.8f);
        }

        public void RestoreMusic() {
            AkSoundEngine.SetRTPCValue("Music_Volume", musicVolume);
        }

        #endregion

        #region 3D场景内背景音

        public void PlayGameMusic(string soundName) {
            StopGameMusic();
            PostEventAsync($"Play_{soundName}", GameMusicNode);
        }

        public void StopGameMusicNode() {
            var audioSource = GameMusicNode.GetComponent<AudioSource>();
            if (audioSource != null) {
                audioSource.Stop();
            }
        }

        public void PauseGameMusicNode() {
            var audioSource = GameMusicNode.GetComponent<AudioSource>();
            if (audioSource != null) {
                audioSource.Pause();
            }
        }

        public void ResumeGameMusicNode() {
            var audioSource = GameMusicNode.GetComponent<AudioSource>();
            if (audioSource != null) {
                audioSource.Play();
            }
        }

        public void StopGameMusic(string soundName = "") {
            var audioSource = GameMusicNode.GetComponent<AudioSource>();
            if (audioSource != null) {
                audioSource.Stop();
            }
            if (string.IsNullOrEmpty(soundName)) {
                AkSoundEngine.StopAll(GameMusicNode);
                return;
            }
            PostEventAsync($"Stop_{soundName}", GameMusicNode);
        }

        #endregion

        #region 白噪音

        public void PlayNoiseSound(string soundName) {
            StopNoiseSound();
            PostEventAsync($"Play_{soundName}", GameNoiseNode);
        }

        public void StopNoiseSound(string soundName = null) {
            if (string.IsNullOrEmpty(soundName)) {
                AkSoundEngine.StopAll(GameNoiseNode);
                return;
            }
            PostEventAsync($"Stop_{soundName}", GameNoiseNode);
        }

        #endregion

        #region 3D场景内可交互道具音效

        public void PlayInteractable3DSound(string playName, GameObject soundGameObject, bool isLoop = false) {
            if (string.IsNullOrEmpty(playName)) {
                LoggerUtils.LogError("PlayInteractable3DSound playName is null");
                return;
            }

            string eventName = "Play_" + playName;
            PostEvent(eventName, soundGameObject);
        }

        #endregion

        #region 主题皮肤预览音效

        public void PlayThemeSkinPreviewSound(string switchName, GameObject soundGameObject) {
        }

        public void StopThemeSkinPreviewSound(GameObject soundGameObject) {
        }

        #endregion

        public void StopAllSound() {
            AkSoundEngine.StopAll();
        }

        private void DownloadInitBank() {
        }

        public void PlaySound(string switchGroup, string switchStateName, string eventName, GameObject in_gameObjectID) {
            LoggerUtils.Log($"PlaySound: {switchGroup} {switchStateName} {eventName} {in_gameObjectID}");
            SetSwitch(switchGroup, switchStateName, in_gameObjectID);
            PostEventAsync(eventName, in_gameObjectID);
        }

        public void PlaySoundWithCb(string switchGroup, string switchStateName, string eventName, GameObject in_gameObjectID, float in_fPercent = 0, Action<uint> callBack = null) {
            SetSwitch(switchGroup, switchStateName, in_gameObjectID);
            PostEventAsyncWithSeek(eventName, in_gameObjectID, in_fPercent, callBack);
        }

        // UGC 音频独立子节点名，与 PGC（PointoneSoundManager）使用的根节点 AudioSource 隔离，
        // 避免 PGC Apply3DSettings 覆盖 UGC 的空间音频配置。
        private const string UGCAudioNodeName = "_UGCAudio";

        private static AudioSource GetOrCreateUGCAudioSource(GameObject parent) {
            var t = parent.transform.Find(UGCAudioNodeName);
            if (t != null) return t.GetComponent<AudioSource>();
            var child = new GameObject(UGCAudioNodeName);
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<AudioSource>();
        }

        // volumeScale：在 SFX 音量基础上额外增益（默认 1=不变；>1 放大，最终 clamp 到 1）。供 AI 伙伴语音等单独调大。
        public void PlayUGCAudioByUrl(string url, bool loop, GameObject gameObject, bool is3D = false, float volumeScale = 1f) {
            var audioSource = GetOrCreateUGCAudioSource(gameObject);
            audioSource.spatialBlend = is3D ? 1f : 0f;
            audioSource.volume = Mathf.Clamp01(GetSFXAudioVolume() * volumeScale);
            audioSource.maxDistance = 29;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.loop = loop;
            audioSource.Stop();
            audioSource.clip = null;
            CoroutineManager.Inst.StartCoroutine(LoadUGCAudio(url, clip => {
                audioSource.clip = clip;
                audioSource.loop = loop;
                audioSource.Play();
            }, err => {
                Debug.LogError($"Load UGC music failed: {err} {url}");
            }));
        }

        public void StopUGCAudio(GameObject gameObject) {
            if (gameObject == null) return;
            var t = gameObject.transform.Find(UGCAudioNodeName);
            if (t == null) return;
            var audioSource = t.GetComponent<AudioSource>();
            if (audioSource != null) {
                audioSource.Stop();
                audioSource.clip = null;
            }
        }

        /// <summary>
        /// 暂停指定对象上的 UGC URL 音频（保留播放位置，可通过 ResumeUGCAudio 恢复）。
        /// </summary>
        public void PauseUGCAudio(GameObject gameObject) {
            if (gameObject == null) return;
            var t = gameObject.transform.Find(UGCAudioNodeName);
            if (t == null) return;
            var audioSource = t.GetComponent<AudioSource>();
            if (audioSource != null) {
                audioSource.Pause();
            }
        }

        /// <summary>
        /// 恢复指定对象上已暂停的 UGC URL 音频（从暂停位置继续播放）。
        /// </summary>
        public void ResumeUGCAudio(GameObject gameObject) {
            if (gameObject == null) return;
            var t = gameObject.transform.Find(UGCAudioNodeName);
            if (t == null) return;
            var audioSource = t.GetComponent<AudioSource>();
            if (audioSource != null) {
                audioSource.UnPause();
            }
        }

        private IEnumerator LoadUGCAudio(string url, UnityAction<AudioClip> onSuccess, UnityAction<string> onFailure) {
            if (string.IsNullOrEmpty(url)) {
                onFailure?.Invoke("url == null");
                yield break;
            }

            url = url.Replace(BusinessBaseUrl, BusinessCdnUrl);
            Uri uri = null;
            try {
                uri = new Uri(url);
            } catch (Exception e) {
                onFailure?.Invoke(e.Message);
                yield break;
            }

            string extension = Path.GetExtension(uri.LocalPath);
            var request = UnityWebRequestMultimedia.GetAudioClip(uri, GetAudioTypeFromExtension(extension));
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success) {
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

        public void StopSound(string eventName, GameObject soundGameObject = null) {
            if (soundGameObject == null) {
                soundGameObject = globalSoundObj;
            }
            AkSoundEngine.PostEvent(eventName, soundGameObject);
        }

        private void SetSwitch(string switchGroup, string switchStateName, GameObject soundGameObject = null) {
            AkSoundEngine.SetSwitch(switchGroup, switchStateName, soundGameObject);
        }

        public uint PostEvent(string in_pszEventName, GameObject in_gameObjectID) {
            if (string.IsNullOrEmpty(in_pszEventName)) {
                return 0;
            }

            var eventDefinition = FindEvent(in_pszEventName);
            if (eventDefinition != null) {
                var clipPaths = GetEventClipPaths(eventDefinition);
                _reusableMissingPaths.Clear();
                if (!AreClipAssetsReady(clipPaths, _reusableMissingPaths) && _reusableMissingPaths.Count > 0) {
                    LoggerUtils.LogError($"音频事件[{in_pszEventName}] 缺少资源，已阻止播放: {string.Join(", ", _reusableMissingPaths)}");
                    return 0;
                }
            } else {
                LoggerUtils.Log($"音频事件[{in_pszEventName}] 未在 P1AudioDatabase 中配置，尝试直接播放。");
            }

            return AkSoundEngine.PostEvent(in_pszEventName, in_gameObjectID);
        }

        public void StopAll(GameObject target) {
            AkSoundEngine.StopAll(target);
        }

        public void PostEventAsync(string in_pszEventName, GameObject in_gameObjectID, Action<uint> callBack = null) {
            if (string.IsNullOrEmpty(in_pszEventName)) {
                callBack?.Invoke(0);
                return;
            }

            var eventDefinition = FindEvent(in_pszEventName);
            if (eventDefinition == null) {
                var playingId = AkSoundEngine.PostEvent(in_pszEventName, in_gameObjectID);
                callBack?.Invoke(playingId);
                return;
            }

            EnsureEventAssetsAsync(eventDefinition, () => {
                if (in_gameObjectID == null) {
                    callBack?.Invoke(0);
                    return;
                }
                var eventId = AkSoundEngine.PostEvent(in_pszEventName, in_gameObjectID);
                callBack?.Invoke(eventId);
            }, () => {
                LoggerUtils.LogError("音频事件资源加载失败: " + in_pszEventName);
                callBack?.Invoke(0);
            });
        }

        public void PostEventAsyncWithSeek(string in_pszEventName, GameObject in_gameObjectID, float in_fPercent = 0, Action<uint> callBack = null) {
            if (string.IsNullOrEmpty(in_pszEventName)) {
                callBack?.Invoke(0);
                return;
            }

            void PlayInternal() {
                if (in_gameObjectID == null) {
                    callBack?.Invoke(0);
                    return;
                }

                string pureName = in_pszEventName;
                if (!string.IsNullOrEmpty(pureName) && pureName.StartsWith("Play_")) {
                    pureName = pureName.Substring(5);
                }

                uint eventId = AkSoundEngine.GetIDFromString(pureName);
                var playingId = AkSoundEngine.PostEvent(eventId, in_gameObjectID);
                if (playingId == AkSoundEngine.AK_INVALID_PLAYING_ID) {
                    LoggerUtils.Log("播放事件失败: " + in_pszEventName);
                    callBack?.Invoke(0);
                    return;
                }

                if (in_fPercent >= 0) {
                    var result = AkSoundEngine.SeekOnEvent(eventId, in_gameObjectID, in_fPercent, false, playingId);
                    if (result != Pointone.Sound.AKRESULT.AK_Success) {
                        LoggerUtils.Log("SeekOnEvent 失败，错误码: " + result);
                    }
                }
                callBack?.Invoke(playingId);
            }

            var eventDefinition = FindEvent(in_pszEventName);
            if (eventDefinition == null) {
                PlayInternal();
                return;
            }

            EnsureEventAssetsAsync(eventDefinition, PlayInternal, () => {
                LoggerUtils.LogError("音频事件资源加载失败: " + in_pszEventName);
                callBack?.Invoke(0);
            });
        }

        public void LoadPgcToneAsync(string in_pszEventName, Action<string> callBack, Action onLoadFail) {
            if (string.IsNullOrEmpty(in_pszEventName)) {
                onLoadFail?.Invoke();
                return;
            }

            var eventDefinition = FindEvent(in_pszEventName);
            if (eventDefinition == null) {
                LoggerUtils.LogError("LoadPgcToneAsync 找不到对应事件的配置:" + in_pszEventName);
                onLoadFail?.Invoke();
                return;
            }

            EnsureEventAssetsAsync(eventDefinition, () => {
                callBack?.Invoke(in_pszEventName);
            }, () => {
                onLoadFail?.Invoke();
            });
        }

        private P1AudioDatabase AudioDatabase {
            get {
                if (audioDatabase == null) {
                    audioDatabase = PointoneAudioManager.Instance.GetCachedDatabase();
                    if (audioDatabase == null) {
                        audioDatabase = PointoneAudioManager.Instance.GetDatabaseAsset() as P1AudioDatabase;
                    }
                }
                return audioDatabase;
            }
        }

        private P1Event FindEvent(string eventName) {
            var db = AudioDatabase;
            if (db == null || string.IsNullOrEmpty(eventName)) {
                return null;
            }

            var evt = db.Find(eventName);
            if (evt != null) {
                return evt;
            }

            string pureName = eventName;
            if (eventName.StartsWith("Play_")) {
                pureName = eventName.Substring(5);
            } else if (eventName.StartsWith("Stop_")) {
                pureName = eventName.Substring(5);
            } else if (eventName.StartsWith("Pause_")) {
                pureName = eventName.Substring(6);
            }

            if (pureName != eventName) {
                evt = db.Find(pureName);
            }

            return evt;
        }

        private List<string> GetEventClipPaths(P1Event eventDefinition) {
            if (eventDefinition == null) {
                return emptyClipPathList;
            }

            if (!eventClipPathCache.TryGetValue(eventDefinition.eventName, out var cache) || cache == null) {
                var collector = new HashSet<string>();
                CollectClipPaths(eventDefinition.root, collector);
                cache = new List<string>(collector);
                eventClipPathCache[eventDefinition.eventName] = cache;
            }
            return cache;
        }

        private void CollectClipPaths(P1Container container, HashSet<string> collector) {
            if (container == null || collector == null) {
                return;
            }

            switch (container) {
                case P1SingleClip singleClip:
                    if (!string.IsNullOrEmpty(singleClip.clipPath)) {
                        collector.Add(singleClip.clipPath);
                    }
                    break;
                case P1RandomContainer randomContainer:
                    if (randomContainer.entries != null) {
                        foreach (var entry in randomContainer.entries) {
                            if (entry?.child != null) {
                                CollectClipPaths(entry.child, collector);
                            }
                        }
                    }
                    break;
                case P1SwitchContainer switchContainer:
                    if (switchContainer.mappings != null) {
                        foreach (var mapping in switchContainer.mappings) {
                            if (mapping?.child != null) {
                                CollectClipPaths(mapping.child, collector);
                            }
                        }
                    }
                    if (switchContainer.fallback != null) {
                        CollectClipPaths(switchContainer.fallback, collector);
                    }
                    break;
            }
        }

        private bool AreClipAssetsReady(List<string> clipPaths) {
            if (clipPaths == null || clipPaths.Count == 0) {
                return true;
            }
            for (int i = 0; i < clipPaths.Count; i++) {
                var path = clipPaths[i];
                if (string.IsNullOrEmpty(path)) continue;
                if (!Assets.IsDownloaded(path)) return false;
            }
            return true;
        }

        private bool AreClipAssetsReady(List<string> clipPaths, List<string> missingPaths) {
            missingPaths?.Clear();

            if (clipPaths == null || clipPaths.Count == 0) {
                return true;
            }

            var ready = true;
            for (int i = 0; i < clipPaths.Count; i++) {
                var path = clipPaths[i];
                if (string.IsNullOrEmpty(path)) {
                    continue;
                }
                if (Assets.IsDownloaded(path)) {
                    continue;
                }
                ready = false;
                missingPaths?.Add(path);
            }

            return ready;
        }

        public void ClearAudioClipCache() {
            XAssetAudioLoader.ClearCache();
            eventClipPathCache.Clear();
        }

        private void EnsureEventAssetsAsync(P1Event eventDefinition, Action onSuccess, Action onFail = null) {
            if (eventDefinition == null) {
                onSuccess?.Invoke();
                return;
            }

            var eventName = !string.IsNullOrEmpty(eventDefinition.eventName)
                ? eventDefinition.eventName
                : eventDefinition.GetInstanceID().ToString();
            var clipPaths = GetEventClipPaths(eventDefinition);

            if (clipPaths.Count == 0 || AreClipAssetsReady(clipPaths)) {
                onSuccess?.Invoke();
                return;
            }

            if (!pendingEventRequests.TryGetValue(eventName, out var requests)) {
                requests = new List<PendingEventRequest>();
                pendingEventRequests[eventName] = requests;
            }
            requests.Add(new PendingEventRequest(onSuccess, onFail));

            if (downloadingEvents.Contains(eventName)) {
                return;
            }

            var missingPaths = new List<string>();
            AreClipAssetsReady(clipPaths, missingPaths);

            if (missingPaths.Count > 0) {
                LoggerUtils.Log($"音频事件[{eventName}] 缺少资源，准备触发下载: {string.Join(", ", missingPaths)}");
            }

            downloadingEvents.Add(eventName);
            CoroutineManager.Inst.StartCoroutine(DownloadEventAssetsCoroutine(eventName, missingPaths));
        }

        private IEnumerator DownloadEventAssetsCoroutine(string eventName, List<string> clipPaths) {
            bool success = true;
            string error = null;

            if (clipPaths != null && clipPaths.Count > 0) {
                var request = Assets.GetDownloadSizeAsync(Assets.Versions, clipPaths.ToArray());
                yield return request;
                if (request.result != Request.Result.Success) {
                    success = false;
                    error = request.error;
                } else if (request.downloadSize > 0) {
                    var download = request.DownloadAsync();
                    yield return download;
                    if (download.result != DownloadRequest.Result.Success) {
                        success = false;
                        error = download.error;
                    }
                }
            }

            downloadingEvents.Remove(eventName);

            if (!pendingEventRequests.TryGetValue(eventName, out var callbacks)) {
                yield break;
            }
            pendingEventRequests.Remove(eventName);

            if (!success) {
                LoggerUtils.LogError($"音频事件资源下载失败: {eventName} {error}");
            }

            for (int i = 0; i < callbacks.Count; i++) {
                var callback = callbacks[i];
                if (success) {
                    callback.onSuccess?.Invoke();
                } else {
                    callback.onFail?.Invoke();
                }
            }
        }

        private readonly List<string> emptyClipPathList = new List<string>(0);

        private struct PendingEventRequest {
            public Action onSuccess;
            public Action onFail;

            public PendingEventRequest(Action onSuccess, Action onFail) {
                this.onSuccess = onSuccess;
                this.onFail = onFail;
            }
        }
    }
}
