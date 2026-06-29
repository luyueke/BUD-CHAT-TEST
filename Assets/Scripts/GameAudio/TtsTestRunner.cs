#if false // TTS disabled
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// TTS test UI. All TtsOrchestrator/TtsService calls are routed through
/// TtsReflection so this file can hot-update into old binaries that do
/// not have the PonyuDev.SherpaOnnx package.
///
/// Inspector: assign _orchestrator to the TtsOrchestrator MonoBehaviour
/// (typed as MonoBehaviour to avoid a static assembly reference).
/// </summary>
public class TtsTestRunner : MonoBehaviour
{
    // Local mirror of KeyBoardInfo (defined in GameUI assembly which cannot be referenced
    // from Game.Audio without creating a circular dependency).
    [System.Serializable]
    private struct TtsKeyboardPayload
    {
        public int    type;
        public string placeHolder;
        public int    maxLength;
        public int    inputFlag;
        public int    textSecurity;
        public string lengthTips;
        public string defaultText;
        public int    returnKeyType;
        public int    source;
        public int    isFilterEmoji;
    }

    [SerializeField] private MonoBehaviour _orchestrator;   // TtsOrchestrator — no static type dep
    [SerializeField] private Button        _playButton;
    [SerializeField] private AudioSource   _audioSource;
    [SerializeField] private string        _text = "你好，这是一个语音合成测试。";

    private UnityAction<string> _onKeyboardResult;

    private static readonly char[] _sentenceDelimiters = { '。', '！', '？', '；', '…', '\n' };

    private void Start()
    {
        _playButton.onClick.AddListener(OnPlayClicked);
        _playButton.interactable = false;

        if (!TtsReflection.IsAvailable)
        {
            UnityEngine.Debug.LogWarning("[TtsTestRunner] TTS package not available on this binary.");
            return;
        }

        if (TtsReflection.IsInitialized(_orchestrator))
            _playButton.interactable = true;
        else
            TtsReflection.AddInitializedListener(_orchestrator, OnOrchestratorReady);
    }

    private void OnOrchestratorReady()
    {
        _playButton.interactable = true;
    }

    private void OnPlayClicked()
    {
        if (_onKeyboardResult != null)
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

        _onKeyboardResult = OnKeyboardConfirmed;
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, _onKeyboardResult);

        var payload = new TtsKeyboardPayload
        {
            type          = 0,
            placeHolder   = "输入要朗读的文字",
            maxLength     = 500,
            defaultText   = _text,
            returnKeyType = 0,
            textSecurity  = 1,
            isFilterEmoji = 0,
        };
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(payload));
    }

    private void OnKeyboardConfirmed(string input)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        _onKeyboardResult = null;

        if (string.IsNullOrWhiteSpace(input)) return;
        _text = input;

        _playButton.interactable = false;
        StopAllCoroutines();
        _audioSource.Stop();
        StartCoroutine(SpeakStreaming(_text));
    }

    private IEnumerator SpeakStreaming(string text)
    {
        if (!TtsReflection.IsAvailable)
        {
            _playButton.interactable = true;
            yield break;
        }

        var segments = SplitSentences(text);
        if (segments.Count == 0)
        {
            _playButton.interactable = true;
            yield break;
        }

        object service = TtsReflection.GetService(_orchestrator);
        if (service == null)
        {
            _playButton.interactable = true;
            yield break;
        }

        var sw = Stopwatch.StartNew();
        Task<object> currentTask = TtsReflection.GenerateAsync(service, segments[0]);

        for (int i = 0; i < segments.Count; i++)
        {
            Task<object> nextTask = (i + 1 < segments.Count)
                ? TtsReflection.GenerateAsync(service, segments[i + 1])
                : null;

            yield return new WaitUntil(() => currentTask.IsCompleted);

            object result = currentTask.Result;
            if (result != null && TtsReflection.IsValid(result))
            {
                long ms = sw.ElapsedMilliseconds;
                AudioClip clip = TtsReflection.ToAudioClip(result, $"seg_{i}");
                _audioSource.clip = clip;
                _audioSource.Play();
                UnityEngine.Debug.Log(
                    $"[TTS] seg[{i}] \"{segments[i]}\" 生成耗时 {ms}ms，音频时长 {clip.length * 1000f:F0}ms");
                sw.Restart();

                yield return new WaitWhile(() => _audioSource.isPlaying);
            }

            currentTask = nextTask;
        }

        _playButton.interactable = true;
    }

    private List<string> SplitSentences(string text)
    {
        var result = new List<string>();
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (System.Array.IndexOf(_sentenceDelimiters, text[i]) >= 0)
            {
                string seg = text.Substring(start, i - start + 1).Trim();
                if (seg.Length > 0) result.Add(seg);
                start = i + 1;
            }
        }
        if (start < text.Length)
        {
            string seg = text.Substring(start).Trim();
            if (seg.Length > 0) result.Add(seg);
        }
        return result;
    }

    private void OnDestroy()
    {
        TtsReflection.RemoveInitializedListener(_orchestrator, OnOrchestratorReady);
        _playButton.onClick.RemoveListener(OnPlayClicked);
    }
}
#endif // TTS disabled
