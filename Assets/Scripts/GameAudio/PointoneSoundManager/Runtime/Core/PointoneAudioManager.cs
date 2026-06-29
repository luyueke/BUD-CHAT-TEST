using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using xasset;

namespace Pointone.Sound
{
    [DefaultExecutionOrder(-10000)]
    public class PointoneAudioManager : MonoBehaviour
    {
        private static PointoneAudioManager instance;
        public static PointoneAudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<PointoneAudioManager>();
                    
                    if (instance == null)
                    {
                        var go = new GameObject("PointoneAudioManager");
                        DontDestroyOnLoad(go);
                        instance = go.AddComponent<PointoneAudioManager>();
                        wasAutoCreated = true;
                        instance.Initialize();
                    }
                    else if (!instance.wasInitialized)
                    {
                        instance.Initialize();
                        DontDestroyOnLoad(instance.gameObject);
                        wasAutoCreated = false;
                    }
                }
                // Awake() handles replacement when a scene instance appears
                return instance;
            }
        }

        private bool wasInitialized = false;
        private bool _isDbLoading = false;
        private static bool wasAutoCreated = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this && wasAutoCreated)
            {
                if (instance.gameObject != null)
                {
                    Destroy(instance.gameObject);
                }
            }
            
            instance = this;
            wasAutoCreated = false;
            
            if (!wasInitialized)
            {
                Initialize();
                DontDestroyOnLoad(gameObject);
            }
        }

        private readonly HashSet<string> basePaths = new HashSet<string>();
        private readonly HashSet<string> loadedBanks = new HashSet<string>();
        private readonly Dictionary<string, string> switchStatesPerObject = new Dictionary<string, string>();
        private readonly Dictionary<string, float> globalRtpc = new Dictionary<string, float>();
        
        private float _musicVolume = 1.0f;
        private float _sfxVolume = 1.0f;

        private readonly Dictionary<uint, VoiceInstance> playingVoices = new Dictionary<uint, VoiceInstance>();
        private readonly Dictionary<string, List<uint>> pausedVoicesByEventName = new Dictionary<string, List<uint>>();
        private uint nextId = 1;

        private int _cleanupCounter;
        private const int CLEANUP_INTERVAL = 120;
        private readonly List<uint> _voiceCleanupBuffer = new List<uint>();
        private readonly List<string> _pausedKeyCleanupBuffer = new List<string>();

        private struct DeferredPlayRequest
        {
            public uint eventId;
            public GameObject go;
            public string pureEventName;
            public int frameCounter;
        }
        private const int DEFERRED_MAX_FRAMES = 180;
        private const int DEFERRED_MAX_COUNT = 32;
        private readonly List<DeferredPlayRequest> _deferredPlays = new List<DeferredPlayRequest>();

        private P1AudioDatabase _cachedDb;
        private AssetRequest _dbLoadRequest;

        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup defaultBus;
        [SerializeField] private ScriptableObject databaseAsset;

        private void Initialize()
        {
            if (wasInitialized) return;

            if (databaseAsset == null && !_isDbLoading)
            {
                // 用 LoadAsync 避免 WaitForCompletion 的主线程忙等阻塞
                // _isDbLoading 防止 Bootstrap→Awake 多次调用时重复启动请求（覆盖 _dbLoadRequest 会泄露旧请求）
                _isDbLoading = true;
                _dbLoadRequest = Asset.LoadAsync("Assets/Sounds/Config/Database.asset", typeof(ScriptableObject));
                if (_dbLoadRequest != null && _dbLoadRequest.isDone)
                {
                    databaseAsset = _dbLoadRequest.asset as ScriptableObject;
                    _dbLoadRequest = null;
                    _isDbLoading = false;
                }
                else if (_dbLoadRequest == null)
                {
                    _isDbLoading = false;
                    Debug.LogError("[PointoneAudioManager] Failed to start loading Database.asset");
                }
                // 否则等 Update() 中检测 isDone 后再完成初始化
            }

            if (databaseAsset != null)
                CompleteDbInit();
            // 若 databaseAsset 仍为 null（异步加载中），Update() 会在加载完成后调用 CompleteDbInit()
        }

        private void CompleteDbInit()
        {
            _cachedDb = databaseAsset as P1AudioDatabase;
            if (_cachedDb != null)
            {
                _cachedDb.BuildIndex(this);
                if (defaultBus == null)
                    defaultBus = _cachedDb.sfx != null ? _cachedDb.sfx : _cachedDb.master;
                if (audioMixer == null)
                    audioMixer = _cachedDb.mixer;
                if (!string.IsNullOrEmpty(_cachedDb.duckAmountParameter) && audioMixer != null)
                    audioMixer.SetFloat(_cachedDb.duckAmountParameter, _cachedDb.defaultDuckAmount);
            }
            wasInitialized = true;
        }

        private void Update()
        {
            // 异步加载 Database.asset 完成后补全初始化
            if (_dbLoadRequest != null && _dbLoadRequest.isDone)
            {
                databaseAsset = _dbLoadRequest.asset as ScriptableObject;
                _dbLoadRequest = null;
                _isDbLoading = false;
                if (databaseAsset != null)
                    CompleteDbInit();
                else
                    Debug.LogError("[PointoneAudioManager] Database.asset 异步加载完成但 asset 为 null");
            }

            if (++_cleanupCounter >= CLEANUP_INTERVAL)
            {
                _cleanupCounter = 0;
                CleanupFinishedVoices();
            }

            if (_deferredPlays.Count > 0)
                ProcessDeferredPlays();
        }

        private void ProcessDeferredPlays()
        {
            for (int i = _deferredPlays.Count - 1; i >= 0; i--)
            {
                var req = _deferredPlays[i];

                if (req.go == null || req.frameCounter > DEFERRED_MAX_FRAMES)
                {
                    _deferredPlays.RemoveAt(i);
                    continue;
                }

                var result = PostEventById(req.eventId, req.go, req.pureEventName, true);
                if (result > 0)
                {
                    _deferredPlays.RemoveAt(i);
                }
                else
                {
                    var updated = req;
                    updated.frameCounter++;
                    _deferredPlays[i] = updated;
                }
            }
        }

        private void CleanupFinishedVoices()
        {
            _voiceCleanupBuffer.Clear();
            foreach (var kv in playingVoices)
            {
                var voice = kv.Value;
                if (voice.Source == null)
                {
                    _voiceCleanupBuffer.Add(kv.Key);
                    continue;
                }
                if (!voice.Source.isPlaying && !voice.IsPaused)
                {
                    _voiceCleanupBuffer.Add(kv.Key);
                }
            }
            for (int i = 0; i < _voiceCleanupBuffer.Count; i++)
            {
                playingVoices.Remove(_voiceCleanupBuffer[i]);
            }

            if (_voiceCleanupBuffer.Count > 0 && pausedVoicesByEventName.Count > 0)
            {
                _pausedKeyCleanupBuffer.Clear();
                foreach (var kv in pausedVoicesByEventName)
                {
                    var list = kv.Value;
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (!playingVoices.ContainsKey(list[i]))
                            list.RemoveAt(i);
                    }
                    if (list.Count == 0)
                        _pausedKeyCleanupBuffer.Add(kv.Key);
                }
                for (int i = 0; i < _pausedKeyCleanupBuffer.Count; i++)
                    pausedVoicesByEventName.Remove(_pausedKeyCleanupBuffer[i]);
            }
        }

        public void AddBasePath(string path)
        {
            if (!string.IsNullOrEmpty(path)) basePaths.Add(path);
        }

        public void ClearBanks()
        {
            loadedBanks.Clear();
        }

        public bool MarkBankLoaded(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            loadedBanks.Add(name);
            return true;
        }

        public uint GetIdFromString(string name)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < name.Length; i++)
                {
                    hash ^= name[i];
                    hash *= 16777619;
                }
                if (hash == 0) hash = 1;
                return hash;
            }
        }

        public uint PostEventByName(string eventName, GameObject go)
        {
            if (string.IsNullOrEmpty(eventName))
                return 0;

            string pureEventName;
            string action = null;

            if (eventName.StartsWith("Play_"))
            {
                action = "Play";
                pureEventName = eventName.Substring(5);
            }
            else if (eventName.StartsWith("Stop_"))
            {
                action = "Stop";
                pureEventName = eventName.Substring(5);
            }
            else if (eventName.StartsWith("Pause_"))
            {
                action = "Pause";
                pureEventName = eventName.Substring(6);
            }
            else if (eventName.StartsWith("Resume_"))
            {
                action = "Resume";
                pureEventName = eventName.Substring(7);
            }
            else
            {
                action = "Play";
                pureEventName = eventName;
            }

            P1AudioLogger.LogEvent(pureEventName, action, go);

            switch (action)
            {
                case "Play":
                    var playEventId = GetIdFromString(pureEventName);
                    return PostEventById(playEventId, go, pureEventName);

                case "Stop":
                    return StopEventByName(pureEventName, go) ? 1u : 0u;

                case "Pause":
                    return PauseEventByName(pureEventName, go) ? 1u : 0u;
                
                case "Resume":
                    return ResumeEventByName(pureEventName, go) ? 1u : 0u;

                default:
                    return 0;
            }
        }

        private bool StopEventByName(string eventName, GameObject go)
        {
            var eventId = GetIdFromString(eventName);
            _voiceCleanupBuffer.Clear();

            foreach (var kv in playingVoices)
            {
                var voice = kv.Value;
                bool isMatch = voice.EventId == eventId;
                if (!isMatch && !string.IsNullOrEmpty(voice.GroupName))
                    isMatch = voice.GroupName == eventName;

                if (isMatch && (go == null || voice.Source == null || voice.Source.gameObject == go))
                    _voiceCleanupBuffer.Add(kv.Key);
            }

            for (int i = 0; i < _voiceCleanupBuffer.Count; i++)
            {
                if (playingVoices.TryGetValue(_voiceCleanupBuffer[i], out var voice))
                {
                    voice.Stop();
                    playingVoices.Remove(_voiceCleanupBuffer[i]);
                }
            }

            if (pausedVoicesByEventName.ContainsKey(eventName))
                pausedVoicesByEventName.Remove(eventName);

            int stopped = _voiceCleanupBuffer.Count;
            P1AudioLogger.LogStop(eventName, stopped, go);
            return stopped > 0;
        }

        private bool PauseEventByName(string eventName, GameObject go)
        {
            var eventId = GetIdFromString(eventName);
            _voiceCleanupBuffer.Clear();

            foreach (var kv in playingVoices)
            {
                var voice = kv.Value;
                bool isMatch = voice.EventId == eventId;
                if (!isMatch && !string.IsNullOrEmpty(voice.GroupName))
                    isMatch = voice.GroupName == eventName;

                if (isMatch && voice.Source != null && voice.Source.isPlaying
                    && (go == null || voice.Source.gameObject == go))
                    _voiceCleanupBuffer.Add(kv.Key);
            }

            if (!pausedVoicesByEventName.TryGetValue(eventName, out var pausedList))
            {
                pausedList = new List<uint>();
                pausedVoicesByEventName[eventName] = pausedList;
            }

            for (int i = 0; i < _voiceCleanupBuffer.Count; i++)
            {
                var id = _voiceCleanupBuffer[i];
                if (playingVoices.TryGetValue(id, out var voice))
                {
                    voice.Pause();
                    if (!pausedList.Contains(id))
                        pausedList.Add(id);
                }
            }

            int paused = _voiceCleanupBuffer.Count;
            P1AudioLogger.LogPause(eventName, paused, go);
            return paused > 0;
        }

        public bool ResumeEventByName(string eventName, GameObject go)
        {
            if (!pausedVoicesByEventName.TryGetValue(eventName, out var pausedList) || pausedList.Count == 0)
                return false;

            var eventId = GetIdFromString(eventName);
            _voiceCleanupBuffer.Clear();

            foreach (var id in pausedList)
            {
                if (playingVoices.TryGetValue(id, out var voice))
                {
                    bool isMatch = voice.EventId == eventId;
                    if (!isMatch && !string.IsNullOrEmpty(voice.GroupName))
                        isMatch = voice.GroupName == eventName;

                    if (isMatch && (go == null || voice.Source == null || voice.Source.gameObject == go))
                        _voiceCleanupBuffer.Add(id);
                }
            }

            for (int i = 0; i < _voiceCleanupBuffer.Count; i++)
            {
                var id = _voiceCleanupBuffer[i];
                if (playingVoices.TryGetValue(id, out var voice))
                {
                    voice.Resume();
                    pausedList.Remove(id);
                }
            }

            if (pausedList.Count == 0)
                pausedVoicesByEventName.Remove(eventName);

            int resumed = _voiceCleanupBuffer.Count;
            P1AudioLogger.LogResume(eventName, resumed, go);
            return resumed > 0;
        }

        public uint PostEventById(uint eventId, GameObject go, string pureEventName = null, bool isDeferredRetry = false)
        {
            if (go == null) go = gameObject;
            
            uint actualEventId = eventId;
            if (!string.IsNullOrEmpty(pureEventName))
            {
                actualEventId = GetIdFromString(pureEventName);
            }

            // Look up P1Event once, reuse across all sub-methods
            P1Event p1Event = null;
            if (_cachedDb != null)
            {
                if (!string.IsNullOrEmpty(pureEventName)) p1Event = _cachedDb.Find(pureEventName);
                if (p1Event == null) p1Event = _cachedDb.Find(actualEventId);
            }
            
            P1AudioLogger.LogFlow("解析音频", $"事件: {pureEventName ?? eventId.ToString()}");
            var resolved = ClipResolver.ResolveWithMeta(actualEventId, go, pureEventName);
            var clip = resolved.clip;
            if (clip == null)
            {
                if (!isDeferredRetry && _deferredPlays.Count < DEFERRED_MAX_COUNT)
                {
                    _deferredPlays.Add(new DeferredPlayRequest
                    {
                        eventId = eventId,
                        go = go,
                        pureEventName = pureEventName,
                        frameCounter = 0
                    });
                    P1AudioLogger.LogFlow("音频异步加载中，已加入延迟播放队列", pureEventName ?? eventId.ToString());
                }
                P1AudioLogger.LogPlayResult(pureEventName ?? eventId.ToString(), false, 0,
                    isDeferredRetry ? "音频仍在加载中" : "音频解析失败，已加入延迟队列");
                return 0;
            }

            // 通过 VoiceController 追踪 PGC 专属的 AudioSource，
            // 避免 GetComponent<AudioSource>() 抢到其他系统（如 UgcToneLoaderBehaviour）创建的 AudioSource。
            var vc = go.GetComponent<VoiceController>();
            if (vc == null) vc = go.AddComponent<VoiceController>();
            AudioSource source = vc.Source;
            if (source == null)
                source = go.AddComponent<AudioSource>();
            
            P1AudioLogger.LogFlow("应用事件设置");
            ApplyEventSettings(p1Event, source);
            source.volume = Mathf.Max(0f, source.volume * resolved.volumeScale);

            source.clip = clip;
            source.playOnAwake = false;

            if (resolved.loopMode != LoopOverrideMode.Inherit)
            {
                source.loop = resolved.loopMode == LoopOverrideMode.ForceLoop;
            }

            EnforceInstanceLimit(p1Event, actualEventId, go, source);
            if (source.clip == null)
            {
                P1AudioLogger.LogPlayResult(pureEventName ?? eventId.ToString(), false, 0, "被实例限制阻止");
                return 0;
            }

            string groupName = p1Event != null ? p1Event.groupName : null;
            AttachOrUpdateVoiceController(vc, actualEventId, p1Event, source, groupName, pureEventName);

            string clipPath = clip.name;
            string clipName = clip.name;
            bool loop = source.loop;
            float volume = source.volume;
            float pitch = source.pitch;
            P1AudioLogger.LogPlayStart(pureEventName ?? eventId.ToString(), clipPath, clipName, go, loop, volume, pitch);

            source.Play();

            var id = nextId++;
            var voice = new VoiceInstance(id, source, actualEventId, BuildInstanceKey(actualEventId, go), groupName);
            playingVoices[id] = voice;
            
            P1AudioLogger.LogPlayResult(pureEventName ?? eventId.ToString(), true, id);
            return id;
        }

        private string BuildInstanceKey(uint eventId, GameObject go)
        {
            return eventId.ToString() + "@" + (go ? go.GetInstanceID().ToString() : "global");
        }

        private void ApplyEventSettings(P1Event p1Event, AudioSource source)
        {
            if (p1Event == null)
            {
                if (defaultBus != null) source.outputAudioMixerGroup = defaultBus;
                source.loop = false;
                source.volume = 1f;
                source.pitch = 1f;
                Apply3DSettings(source, null);
                P1AudioLogger.LogWarning("未找到事件配置，使用默认设置");
                return;
            }

            P1AudioLogger.LogEventResolve(p1Event.eventName, true, GetIdFromString(p1Event.eventName));

            var busGroup = (p1Event.bus != null) ? p1Event.bus.group : null;
            source.outputAudioMixerGroup = busGroup != null ? busGroup : defaultBus;
            source.volume = p1Event.volume;
            source.loop = p1Event.loop;
            source.pitch = p1Event.pitch;

            Apply3DSettings(source, p1Event.spatial3DConfig);
        }

        private void Apply3DSettings(AudioSource source, P1Spatial3DSoundConfig cfg)
        {
            if (source == null) return;
            if (cfg == null)
            {
                source.spatialBlend = 0f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                return;
            }

            source.spatialBlend = Mathf.Clamp01(cfg.spatialBlend);
            source.rolloffMode = cfg.rolloffMode;
            source.minDistance = Mathf.Max(0f, cfg.minDistance);
            source.maxDistance = Mathf.Max(source.minDistance, cfg.maxDistance);
            source.dopplerLevel = cfg.dopplerLevel;
            source.spread = cfg.spread;
            source.panStereo = cfg.panLevel;
            source.reverbZoneMix = cfg.reverbZoneMix;

            if (cfg.rolloffMode == AudioRolloffMode.Custom && cfg.customRolloff != null)
            {
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, cfg.customRolloff);
            }
        }

        private void EnforceInstanceLimit(P1Event p1Event, uint eventId, GameObject go, AudioSource pendingSource)
        {
            if (p1Event == null || p1Event.maxInstances <= 0) return;

            int max = p1Event.maxInstances;
            bool perGo = p1Event.perGameObject;
            var policy = p1Event.stealPolicy;

            string scope = perGo ? (go ? go.GetInstanceID().ToString() : "global") : "global";
            string key = eventId.ToString() + "@" + scope;
            int count = 0;
            uint oldestId = 0;
            float oldestTime = float.MaxValue;
            uint newestId = 0;
            float newestTime = float.MinValue;

            foreach (var kv in playingVoices)
            {
                var v = kv.Value;
                if (v.EventId != eventId) continue;
                if (v.InstanceKey != key) continue;
                count++;
                if (v.StartTime < oldestTime) { oldestTime = v.StartTime; oldestId = v.Id; }
                if (v.StartTime > newestTime) { newestTime = v.StartTime; newestId = v.Id; }
            }

            if (count < max) return;

            string pName = policy.ToString();
            
            if (policy == VoiceStealPolicy.None)
            {
                pendingSource.clip = null;
                P1AudioLogger.LogInstanceLimit(p1Event.eventName, count, max, pName, true);
                return;
            }
            
            P1AudioLogger.LogInstanceLimit(p1Event.eventName, count, max, pName, false);
            if (policy == VoiceStealPolicy.Oldest && oldestId != 0 && playingVoices.TryGetValue(oldestId, out var ov))
            {
                ov.Stop();
                playingVoices.Remove(oldestId);
            }
            else if (policy == VoiceStealPolicy.Newest && newestId != 0 && playingVoices.TryGetValue(newestId, out var nv))
            {
                nv.Stop();
                playingVoices.Remove(newestId);
            }
        }

        private void AttachOrUpdateVoiceController(VoiceController vc, uint eventId, P1Event p1Event, AudioSource source, string groupName, string eventName)
        {
            bool isMusic = IsMusicEvent(groupName, !string.IsNullOrEmpty(eventName) ? eventName : (p1Event != null ? p1Event.eventName : ""));
            vc.Initialize(eventId, p1Event, source);
            vc.GlobalVolumeScale = isMusic ? _musicVolume : _sfxVolume;
        }

        public void StopAll()
        {
            foreach (var v in playingVoices.Values) v.Stop();
            playingVoices.Clear();
            pausedVoicesByEventName.Clear();
        }

        public void StopAll(GameObject target)
        {
            var sources = target != null ? target.GetComponents<AudioSource>() : Array.Empty<AudioSource>();
            foreach (var s in sources) s.Stop();

            _voiceCleanupBuffer.Clear();
            foreach (var kv in playingVoices)
            {
                if (kv.Value.Source == null || kv.Value.Source.gameObject == target)
                    _voiceCleanupBuffer.Add(kv.Key);
            }
            for (int i = 0; i < _voiceCleanupBuffer.Count; i++)
                playingVoices.Remove(_voiceCleanupBuffer[i]);

            _pausedKeyCleanupBuffer.Clear();
            foreach (var kv in pausedVoicesByEventName)
            {
                var list = kv.Value;
                for (int j = list.Count - 1; j >= 0; j--)
                {
                    if (!playingVoices.ContainsKey(list[j]))
                        list.RemoveAt(j);
                }
                if (list.Count == 0)
                    _pausedKeyCleanupBuffer.Add(kv.Key);
            }
            for (int i = 0; i < _pausedKeyCleanupBuffer.Count; i++)
                pausedVoicesByEventName.Remove(_pausedKeyCleanupBuffer[i]);
        }

        public void SetRTPC(string parameterName, float value)
        {
            globalRtpc[parameterName] = value;
            
            bool isMusicVol = parameterName.IndexOf("Music_Volume", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isSfxVol = parameterName.IndexOf("SFX_Volume", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isMusicVol || isSfxVol)
            {
                float vol01 = value;
                if (value > 1.0f) vol01 = value / 100f;
                vol01 = Mathf.Clamp01(vol01);

                if (isMusicVol) _musicVolume = vol01;
                if (isSfxVol) _sfxVolume = vol01;

                foreach (var kv in playingVoices)
                {
                    var voice = kv.Value;
                    if (voice.Source != null)
                    {
                        var vc = voice.Source.GetComponent<VoiceController>();
                        if (vc != null)
                        {
                            bool isMusic = IsMusicEvent(voice.GroupName, vc.boundEvent != null ? vc.boundEvent.eventName : "");
                            vc.GlobalVolumeScale = isMusic ? _musicVolume : _sfxVolume;
                        }
                    }
                }
            }

            if (audioMixer != null)
            {
                float mixerValue = value;
                if (parameterName.IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0 && value >= 0)
                {
                    mixerValue = value <= 0.001f ? -80f : 20f * Mathf.Log10(Mathf.Clamp(value, 0.0001f, 100f) / 100f);
                }
                audioMixer.SetFloat(parameterName, mixerValue);
            }
        }

        private bool IsMusicEvent(string groupName, string eventName)
        {
            if (!string.IsNullOrEmpty(eventName) && eventName.IndexOf("Bgm", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(groupName) && (groupName.IndexOf("Bgm", StringComparison.OrdinalIgnoreCase) >= 0 || groupName.IndexOf("Music", StringComparison.OrdinalIgnoreCase) >= 0)) return true;
            return false;
        }

        public void SetSwitch(string group, string state, GameObject target)
        {
            var key = group + "@" + (target ? target.GetInstanceID().ToString() : "global");
            switchStatesPerObject[key] = state;
        }

        public bool SeekOnEvent(uint eventId, GameObject target, float percent, bool fromBeginning, uint playingId)
        {
            if (!playingVoices.TryGetValue(playingId, out var voice) || voice.Source == null || voice.Source.clip == null)
                return false;
            percent = Mathf.Clamp01(percent);
            var samples = voice.Source.clip.samples;
            voice.Source.timeSamples = (int)(samples * percent);
            return true;
        }

        public Dictionary<string, string> GetSwitchSnapshot()
        {
            return switchStatesPerObject;
        }

        public ScriptableObject GetDatabaseAsset()
        {
            return databaseAsset;
        }

        public P1AudioDatabase GetCachedDatabase()
        {
            return _cachedDb;
        }

        public bool TryGetRtpcValue(string name, out float value)
        {
            return globalRtpc.TryGetValue(name, out value);
        }

        public Dictionary<string, float> GetRtpcSnapshot()
        {
            return globalRtpc;
        }

        public void ClearAudioClipCache()
        {
            XAssetAudioLoader.ClearCache();
        }

        public void SetDuckAmount(float value)
        {
            if (_cachedDb == null || audioMixer == null) return;
            if (string.IsNullOrEmpty(_cachedDb.duckAmountParameter)) return;
            audioMixer.SetFloat(_cachedDb.duckAmountParameter, Mathf.Clamp01(value));
        }
    }
}
