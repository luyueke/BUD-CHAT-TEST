#if false // TTS disabled
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// One-time reflection cache for PonyuDev.SherpaOnnx TTS types.
/// Designed for two-binary hot-update: old binaries without the TTS package
/// load normally and all calls silently no-op. New binaries with the package
/// get full TTS functionality.
///
/// Resolution runs once on first access. If the assembly is absent,
/// _available stays false and is NEVER retried — no repeated reflection cost.
/// </summary>
public static class TtsReflection
{
    // ── Assembly / type names ──────────────────────────────────────────────
    private const string AssemblyName       = "PonyuDev.SherpaOnnx";
    private const string OrchestratorFQN    = "PonyuDev.SherpaOnnx.Tts.TtsOrchestrator, "        + AssemblyName;
    private const string ServiceFQN         = "PonyuDev.SherpaOnnx.Tts.ITtsService, "            + AssemblyName;
    private const string ServiceImplFQN     = "PonyuDev.SherpaOnnx.Tts.TtsService, "             + AssemblyName;
    private const string EngineIfaceFQN     = "PonyuDev.SherpaOnnx.Tts.Engine.ITtsEngine, "      + AssemblyName;
    private const string SettingsLoaderFQN  = "PonyuDev.SherpaOnnx.Tts.Config.TtsSettingsLoader, " + AssemblyName;
    private const string ResultFQN          = "PonyuDev.SherpaOnnx.Tts.Engine.TtsResult, "       + AssemblyName;

    // ── Cached state ───────────────────────────────────────────────────────
    private static bool _checked;
    private static bool _available;
    // True once the native ONNX engine has finished loading.
    // Defaults to true so non-iOS platforms (Android, macOS, Editor) are unaffected.
    // ActivateWithCustomPath resets it to false at entry and sets it back to true
    // from the background thread once Load() completes.
    private static volatile bool _engineLoaded = true;
    public static bool IsEngineLoaded => _engineLoaded;

    // Used externally (TtsTestRunner)
    private static PropertyInfo _isInitializedProp;
    private static EventInfo    _initializedEvent;
    private static PropertyInfo _serviceProp;
    private static MethodInfo   _generateAsyncMethod;
    private static PropertyInfo _isValidProp;
    private static MethodInfo   _toAudioClipMethod;

    // Used by ActivateWithCustomPath (iOS path override)
    private static Type       _orchestratorType;
    private static FieldInfo  _initializeOnAwakeFI;
    private static FieldInfo  _innerServiceFI;
    private static FieldInfo  _engineFI;
    private static FieldInfo  _settingsFI;
    private static FieldInfo  _activeProfileFI;
    private static MethodInfo _ensureEngineMI;
    private static MethodInfo _settingsLoaderLoadMI;
    private static MethodInfo _settingsLoaderGetActiveProfileMI;
    private static MethodInfo _engineLoadMI;

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>True if the TTS package is present and all required members found.</summary>
    public static bool IsAvailable
    {
        get { EnsureResolved(); return _available; }
    }

    public static bool IsInitialized(Component orchestrator)
    {
        if (!IsAvailable || orchestrator == null) return false;
        return (bool)_isInitializedProp.GetValue(orchestrator);
    }

    public static void AddInitializedListener(Component orchestrator, Action callback)
    {
        if (!IsAvailable || orchestrator == null) return;
        _initializedEvent.AddEventHandler(orchestrator, callback);
    }

    public static void RemoveInitializedListener(Component orchestrator, Action callback)
    {
        if (!IsAvailable || orchestrator == null) return;
        _initializedEvent.RemoveEventHandler(orchestrator, callback);
    }

    /// <summary>Returns the ITtsService object from the orchestrator, or null.</summary>
    public static object GetService(Component orchestrator)
    {
        if (!IsAvailable || orchestrator == null) return null;
        return _serviceProp.GetValue(orchestrator);
    }

    /// <summary>
    /// Calls service.GenerateAsync(text) and wraps the Task&lt;TtsResult&gt;
    /// into a Task&lt;object&gt; so callers have no static dependency on TtsResult.
    /// Returns a completed null task if TTS is unavailable.
    /// </summary>
    public static Task<object> GenerateAsync(object service, string text)
    {
        if (!IsAvailable || service == null)
            return Task.FromResult<object>(null);

        var rawTask = (Task)_generateAsyncMethod.Invoke(service, new object[] { text });
        var tcs = new TaskCompletionSource<object>();

        rawTask.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                tcs.SetException(t.Exception.InnerExceptions);
            }
            else if (t.IsCanceled)
            {
                tcs.SetCanceled();
            }
            else
            {
                // Unwrap Task<TtsResult>.Result via the cached generic Result property.
                var resultProp = t.GetType().GetProperty("Result");
                tcs.SetResult(resultProp?.GetValue(t));
            }
        }, TaskContinuationOptions.ExecuteSynchronously);

        return tcs.Task;
    }

    public static bool IsValid(object ttsResult)
    {
        if (!IsAvailable || ttsResult == null) return false;
        return (bool)_isValidProp.GetValue(ttsResult);
    }

    /// <summary>Must be called on the main thread (Unity AudioClip creation constraint).</summary>
    public static AudioClip ToAudioClip(object ttsResult, string clipName)
    {
        if (!IsAvailable || ttsResult == null) return null;
        return (AudioClip)_toAudioClipMethod.Invoke(ttsResult, new object[] { clipName });
    }

    // ── Public: iOS path-override activation ──────────────────────────────

    /// <summary>
    /// For iOS: disables TtsOrchestrator auto-init before SetActive so sherpa-onnx
    /// never touches streamingAssetsPath (read-only on iOS), then manually loads the
    /// engine from <paramref name="modelDir"/> (persistentDataPath-based).
    /// Falls back to a plain SetActive when reflection members are unavailable.
    /// </summary>
    public static void ActivateWithCustomPath(GameObject go, string modelDir, string espeakOverrideDir = null)
    {
        _engineLoaded = false;
        EnsureResolved();

        Component orchComp = null;
        if (_available && _orchestratorType != null)
        {
            foreach (var mb in go.GetComponents<MonoBehaviour>())
            {
                if (mb != null && mb.GetType() == _orchestratorType)
                {
                    orchComp = mb;
                    break;
                }
            }
        }

        if (orchComp == null           || _initializeOnAwakeFI == null ||
            _innerServiceFI == null    || _ensureEngineMI      == null ||
            _engineFI       == null    || _engineLoadMI        == null ||
            _settingsLoaderLoadMI == null || _settingsLoaderGetActiveProfileMI == null)
        {
            Debug.LogWarning("[TtsReflection] ActivateWithCustomPath: reflection incomplete, using plain SetActive.");
            go.SetActive(true);
            _engineLoaded = true; // no real engine — skip the wait in LoadingBar
            return;
        }

        // Prevent Awake from calling InitializeAsync (which uses wrong path on iOS)
        _initializeOnAwakeFI.SetValue(orchComp, false);

        // Awake runs synchronously (no await since _initializeOnAwake = false).
        // Creates TtsService, sets IsInitialized = true, fires Initialized event.
        // NOTE: IsInitialized = true fires here but engine is NOT loaded yet;
        //       TheatreTtsPlayer.IsReady also gates on IsEngineLoaded.
        go.SetActive(true);

        object innerService = _innerServiceFI.GetValue(orchComp);
        if (innerService == null)
        {
            Debug.LogError("[TtsReflection] ActivateWithCustomPath: _innerService null after SetActive.");
            _engineLoaded = true;
            return;
        }

        // Load settings JSON from StreamingAssets (the JSON stays there; only model binaries move)
        object settings = _settingsLoaderLoadMI.Invoke(null, null);
        object profile  = _settingsLoaderGetActiveProfileMI.Invoke(null, new[] { settings });
        if (profile == null)
        {
            Debug.LogError("[TtsReflection] ActivateWithCustomPath: no active TTS profile in settings.");
            _engineLoaded = true;
            return;
        }

        // Create native engine instance (requires SHERPA_ONNX define in the build)
        _ensureEngineMI.Invoke(innerService, null);
        object engine = _engineFI.GetValue(innerService);
        if (engine == null)
        {
            Debug.LogError("[TtsReflection] ActivateWithCustomPath: engine null — SHERPA_ONNX define missing?");
            _engineLoaded = true;
            return;
        }

        // Read pool size from settings with safe fallback
        int poolSize = 1;
        if (settings != null)
        {
            try
            {
                var cacheFI = settings.GetType().GetField("cache");
                object cacheObj = cacheFI?.GetValue(settings);
                if (cacheObj != null)
                {
                    var psField = cacheObj.GetType().GetField("offlineTtsPoolSize");
                    if (psField != null) poolSize = (int)psField.GetValue(cacheObj);
                }
            }
            catch { /* keep default 1 */ }
        }

        // Store settings so TtsService.Settings is populated after init
        _settingsFI?.SetValue(innerService, settings);

        // Override vitsDataDir to absolute path when espeak lives outside modelDir (e.g. Editor SideFiles)
        if (!string.IsNullOrEmpty(espeakOverrideDir))
        {
            var profileType = profile.GetType();
            var fi = profileType.GetField("vitsDataDir",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var pi = profileType.GetProperty("vitsDataDir",
                BindingFlags.Public | BindingFlags.Instance);
            fi?.SetValue(profile, espeakOverrideDir);
            pi?.SetValue(profile, espeakOverrideDir);
        }

        // Load ONNX model on a background thread so the main thread (and LoadingBar) stays responsive.
        // TheatreTtsPlayer.IsReady gates on _engineLoaded, so TheatreGamePanel's loading coroutine
        // will display "加载语音合成..." and wait here instead of freezing at panel open.
        var capturedEngine      = engine;
        var capturedProfile     = profile;
        var capturedModelDir    = modelDir;
        var capturedPoolSize    = poolSize;
        var capturedInnerService = innerService;
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                _engineLoadMI.Invoke(capturedEngine, new object[] { capturedProfile, capturedModelDir, capturedPoolSize });
                _activeProfileFI?.SetValue(capturedInnerService, capturedProfile);
                Debug.Log($"[TtsReflection] ActivateWithCustomPath: engine loaded from {capturedModelDir}, pool={capturedPoolSize}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TtsReflection] ActivateWithCustomPath: engine load failed — {ex.InnerException?.Message ?? ex.Message}");
            }
            finally
            {
                _engineLoaded = true;
            }
        });
    }

    // ── One-time resolution ────────────────────────────────────────────────

    private static void EnsureResolved()
    {
        if (_checked) return;
        _checked = true;

        var orchType   = Type.GetType(OrchestratorFQN);
        var svcType    = Type.GetType(ServiceFQN);
        var resultType = Type.GetType(ResultFQN);

        if (orchType == null || svcType == null || resultType == null)
        {
            Debug.LogWarning(
                "[TtsReflection] PonyuDev.SherpaOnnx assembly not found — TTS disabled. " +
                "This is expected on builds without the TTS package.");
            _available = false;
            return;
        }

        // ── Members used by TtsTestRunner ──
        _isInitializedProp   = orchType.GetProperty("IsInitialized",   BindingFlags.Public | BindingFlags.Instance);
        _initializedEvent    = orchType.GetEvent("Initialized",         BindingFlags.Public | BindingFlags.Instance);
        _serviceProp         = orchType.GetProperty("Service",          BindingFlags.Public | BindingFlags.Instance);
        _generateAsyncMethod = svcType.GetMethod("GenerateAsync",       new[] { typeof(string) });
        _isValidProp         = resultType.GetProperty("IsValid",        BindingFlags.Public | BindingFlags.Instance);
        _toAudioClipMethod   = resultType.GetMethod("ToAudioClip",      new[] { typeof(string) });

        if (_isInitializedProp  == null || _initializedEvent   == null ||
            _serviceProp        == null || _generateAsyncMethod == null ||
            _isValidProp        == null || _toAudioClipMethod   == null)
        {
            Debug.LogError("[TtsReflection] TTS assembly found but required members missing — version mismatch?");
            _available = false;
            return;
        }

        _available = true;

        // ── Extra members for ActivateWithCustomPath (iOS path override) ──
        // These are best-effort: null members cause a graceful fallback, not a failure.
        _orchestratorType = orchType;

        var svcImplType  = Type.GetType(ServiceImplFQN);
        var engineIfType = Type.GetType(EngineIfaceFQN);
        var settingsType = Type.GetType(SettingsLoaderFQN);

        _initializeOnAwakeFI = orchType.GetField("_initializeOnAwake",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _innerServiceFI = orchType.GetField("_innerService",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _engineFI = svcImplType?.GetField("_engine",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _settingsFI = svcImplType?.GetField("_settings",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _activeProfileFI = svcImplType?.GetField("_activeProfile",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _ensureEngineMI = svcImplType?.GetMethod("EnsureEngine",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _settingsLoaderLoadMI = settingsType?.GetMethod("Load",
            BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
        _settingsLoaderGetActiveProfileMI = settingsType?.GetMethod("GetActiveProfile",
            BindingFlags.Public | BindingFlags.Static);
        _engineLoadMI = engineIfType?.GetMethod("Load",
            BindingFlags.Public | BindingFlags.Instance);

        Debug.Log("[TtsReflection] TTS reflection resolved successfully.");
    }
}
#endif // TTS disabled
