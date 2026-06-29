#if false // TTS disabled
using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using xasset;

/// <summary>
/// Downloads TTS model files via xasset and activates TtsOrchestrator
/// only after all files are available on the local filesystem.
///
/// No static dependency on PonyuDev.SherpaOnnx — safe to hot-update into
/// old binaries that do not have the TTS package.
///
/// Setup:
///   1. Place TtsOrchestrator on a disabled child GameObject (_orchestratorGO).
///   2. Add this component to an active GameObject in the same scene.
///   3. Assign _orchestratorGO in Inspector.
///   4. Adjust _profileName and _modelFiles to match xasset bundle paths.
///
/// Android: files are downloaded from CDN via xasset RawAsset, then copied to
///          persistentDataPath/SherpaOnnx/tts-models/{profile}/ — exactly where
///          TtsModelPathResolver looks on Android.
///
/// For vits-piper models: set _espeakXassetDir to the espeak-ng-data xasset
///          folder path. All files under that directory are downloaded and placed
///          at {modelDir}/espeak-ng-data/ (required by vitsDataDir setting).
///
/// Editor:  model files must already be in StreamingAssets; orchestrator is
///          activated immediately with no download.
/// </summary>
public class TtsModelLoader : MonoBehaviour
{
    [Serializable]
    public struct ModelFileEntry
    {
        [Tooltip("xasset logical path registered in the manifest, e.g. Assets/Loadable/SherpaOnnx/...")]
        public string xassetPath;
        [Tooltip("Destination filename inside the model directory")]
        public string fileName;
    }

    [SerializeField] private GameObject _orchestratorGO;

    [SerializeField] private string _profileName = "vits-piper-zh_CN-chaowen-medium-int8";

    [SerializeField] private ModelFileEntry[] _modelFiles =
    {
        new ModelFileEntry
        {
            xassetPath = "Assets/Loadable/SherpaOnnx/tts-models/vits-piper-zh_CN-chaowen-medium-int8/zh_CN-chaowen-medium.onnx",
            fileName   = "zh_CN-chaowen-medium.onnx"
        },
        new ModelFileEntry
        {
            xassetPath = "Assets/Loadable/SherpaOnnx/tts-models/vits-piper-zh_CN-chaowen-medium-int8/zh_CN-chaowen-medium.onnx.json",
            fileName   = "zh_CN-chaowen-medium.onnx.json"
        },
        new ModelFileEntry
        {
            xassetPath = "Assets/Loadable/SherpaOnnx/tts-models/vits-piper-zh_CN-chaowen-medium-int8/lexicon.txt",
            fileName   = "lexicon.txt"
        },
        new ModelFileEntry
        {
            xassetPath = "Assets/Loadable/SherpaOnnx/tts-models/vits-piper-zh_CN-chaowen-medium-int8/tokens.txt",
            fileName   = "tokens.txt"
        },
    };

    [Tooltip("xasset folder path for espeak-ng-data (Editor only). Leave empty for non-piper models.")]
    [SerializeField] private string _espeakXassetDir =
        "Assets/Loadable/SherpaOnnxSideFiles/tts-models/vits-piper-zh_CN-chaowen-medium-int8/espeak-ng-data";

    [Tooltip("xasset path to espeak-ng-data.zip (runtime download). Leave empty for non-piper models.")]
    [SerializeField] private string _espeakXassetZipPath =
        "Assets/Loadable/SherpaOnnxSideFiles/tts-models/vits-piper-zh_CN-chaowen-medium-int8/espeak-ng-data.zip";

    private IEnumerator Start()
    {
#if UNITY_EDITOR
        // Editor: load directly from Assets/Loadable (no download needed, avoids APK bloat from StreamingAssets)
        string editorModelDir = Path.Combine(
            Application.dataPath, "Loadable", "SherpaOnnx", "tts-models", _profileName);

        // Derive local espeak path from _espeakXassetDir (may live in a separate SideFiles folder)
        string editorEspeakDir = null;
        if (!string.IsNullOrEmpty(_espeakXassetDir))
        {
            // "Assets/Loadable/..." → Application.dataPath + "/Loadable/..."
            string rel = _espeakXassetDir.StartsWith("Assets/")
                ? _espeakXassetDir.Substring("Assets/".Length)
                : _espeakXassetDir;
            editorEspeakDir = Path.Combine(Application.dataPath,
                rel.Replace('/', Path.DirectorySeparatorChar));
        }

        bool espeakOk = string.IsNullOrEmpty(_espeakXassetDir)
            || IsEspeakReady(editorModelDir)                                    // co-located
            || (editorEspeakDir != null && Directory.Exists(editorEspeakDir)   // SideFiles path
                && Directory.GetFiles(editorEspeakDir).Length > 0);

        if (!AreFilesReady(editorModelDir) || !espeakOk)
        {
            Debug.LogWarning(
                $"[TtsModelLoader] Editor: model files not found at {editorModelDir}. " +
                $"TTS disabled. To test in Editor, ensure model files are in Assets/Loadable/SherpaOnnx/tts-models/{_profileName}/");
            yield break;
        }

        // Pass absolute espeak path only when it differs from the model dir
        string espeakOverride = (editorEspeakDir != null && !IsEspeakReady(editorModelDir))
            ? editorEspeakDir
            : null;
        TtsReflection.ActivateWithCustomPath(_orchestratorGO, editorModelDir, espeakOverride);
        yield break;
#else
        yield return StartCoroutine(DownloadThenActivate());
#endif
    }

    private IEnumerator DownloadThenActivate()
    {
        string modelDir = Path.Combine(
            Application.persistentDataPath, "SherpaOnnx", "tts-models", _profileName);

        if (!AreFilesReady(modelDir) || !IsEspeakReady(modelDir))
        {
            Directory.CreateDirectory(modelDir);

            foreach (var entry in _modelFiles)
            {
                string dest = Path.Combine(modelDir, entry.fileName);
                if (File.Exists(dest))
                    continue;

                var req = RawAsset.LoadAsync(entry.xassetPath);
                if (req == null)
                {
                    Debug.LogError(
                        $"[TtsModelLoader] Not in xasset manifest: {entry.xassetPath}\n" +
                        "Add the file to Assets/Loadable/SherpaOnnx/... and rebuild the xasset bundle.");
                    yield break;
                }

                yield return new WaitUntil(() => req.isDone);

                if (req.result != Request.Result.Success)
                {
                    Debug.LogError(
                        $"[TtsModelLoader] Download failed: {entry.xassetPath} — {req.error}");
                    req.Release();
                    yield break;
                }

                var destDir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                File.Copy(req.path, dest, overwrite: true);
                req.Release();
                Debug.Log($"[TtsModelLoader] {entry.fileName} → {dest}");
            }

            if (!string.IsNullOrEmpty(_espeakXassetZipPath))
                yield return StartCoroutine(DownloadEspeakData(modelDir));
        }
        else
        {
            Debug.Log($"[TtsModelLoader] All model files present at {modelDir}");
        }

#if UNITY_IOS
        TtsReflection.ActivateWithCustomPath(_orchestratorGO, modelDir);
#else
        _orchestratorGO.SetActive(true);
#endif
    }

    private IEnumerator DownloadEspeakData(string modelDir)
    {
        var req = RawAsset.LoadAsync(_espeakXassetZipPath);
        if (req == null)
        {
            Debug.LogError(
                $"[TtsModelLoader] espeak-ng-data.zip not in xasset manifest: {_espeakXassetZipPath}\n" +
                "Add the file to Assets/Loadable/... and rebuild the xasset bundle.");
            yield break;
        }

        yield return new WaitUntil(() => req.isDone);

        if (req.result != Request.Result.Success)
        {
            Debug.LogError($"[TtsModelLoader] espeak zip download failed: {_espeakXassetZipPath} — {req.error}");
            req.Release();
            yield break;
        }

        string espeakDestDir = Path.Combine(modelDir, "espeak-ng-data");
        if (Directory.Exists(espeakDestDir))
            Directory.Delete(espeakDestDir, true);

        ZipFile.ExtractToDirectory(req.path, modelDir);
        req.Release();
        Debug.Log($"[TtsModelLoader] espeak-ng-data extracted to {espeakDestDir}");
    }

    private bool AreFilesReady(string dir)
    {
        foreach (var entry in _modelFiles)
        {
            if (!File.Exists(Path.Combine(dir, entry.fileName)))
                return false;
        }
        return true;
    }

    private bool IsEspeakReady(string modelDir)
    {
        if (string.IsNullOrEmpty(_espeakXassetZipPath))
            return true;
        string espeakDir = Path.Combine(modelDir, "espeak-ng-data");
        return Directory.Exists(espeakDir) && Directory.GetFiles(espeakDir).Length > 0;
    }
}
#endif // TTS disabled
