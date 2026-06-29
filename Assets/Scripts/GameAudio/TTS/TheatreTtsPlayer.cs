#if false // TTS disabled
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Per-dialogue TTS player for TheatreGamePanel.
///
/// Setup (prefab):
///   - Assign _orchestrator to the TtsOrchestrator MonoBehaviour (inactive child GO).
///   - Assign _audioSource to the AudioSource used for TTS playback.
///   - TtsModelLoader on the same/sibling GO handles model download and activates the orchestrator.
///
/// Flow: TheatreGamePanel calls Speak(text) on each dialogue trigger.
/// If a speak is in progress, it is interrupted immediately before the next one starts.
/// </summary>
public class TheatreTtsPlayer : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _orchestrator;  // TtsOrchestrator — no static dep on SherpaOnnx
    [SerializeField] private AudioSource _audioSource;

    // Sentence-end chars always split; comma-class chars split only when chunk >= soft min
    private static readonly char[] HardSplitChars = { '。', '！', '？', '…', '\n' };
    private static readonly char[] SoftSplitChars = { '，', '；', '：' };
    // Progressive max lengths: chunk0=8, chunk1=12, chunk2=16, chunk3+=20
    // Keeps first chunk short so first audio starts within ~1 s on device.
    private static readonly int[] ChunkMaxLengths = { 8, 12, 16, 20 };
    // Dependent suffixes — never let a split leave these dangling on a new chunk.
    // All are neutral-tone enclitics: split causes wrong citation tone or broken erhua.
    // Excludes interjections (哦/哇/嗯) which are utterance-initial, and 来/去/子/头
    // which are too polysemous to safely absorb without POS context.
    private static readonly char[] SuffixChars = {
        '么', '们',                         // plural / question suffix
        '了', '着', '过',                   // aspect markers
        '的', '地', '得',                   // structural particles (de)
        '吗', '呢', '啊', '吧', '嘛', '啦', '呀',  // sentence-final / modal particles
        '儿'                                // erhua — must merge into preceding rime
    };

    private bool _isEnabled = true;
    private int _speakVersion;
    private Coroutine _speakCoroutine;

    public bool IsReady => TtsReflection.IsAvailable && TtsReflection.IsInitialized(_orchestrator) && TtsReflection.IsEngineLoaded;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            if (!value) StopSpeaking();
        }
    }

    public float Volume
    {
        get => _audioSource != null ? _audioSource.volume : 1f;
        set { if (_audioSource != null) _audioSource.volume = Mathf.Clamp01(value); }
    }

    public void Speak(string text)
    {
        if (!_isEnabled || !IsReady || string.IsNullOrEmpty(text)) return;
        StopSpeaking();
        _speakVersion++;
        _speakCoroutine = StartCoroutine(SpeakCoroutine(text, _speakVersion));
    }

    public void StopSpeaking()
    {
        _speakVersion++;
        if (_speakCoroutine != null)
        {
            StopCoroutine(_speakCoroutine);
            _speakCoroutine = null;
        }
        _audioSource?.Stop();
    }

    private void OnDestroy()
    {
        StopSpeaking();
    }

    // Pipeline: generates chunk[i+1] while chunk[i] is playing → near-zero gap between sentences
    private IEnumerator SpeakCoroutine(string text, int version)
    {
        object service = TtsReflection.GetService(_orchestrator);
        if (service == null) { _speakCoroutine = null; yield break; }

        string[] chunks = SplitIntoChunks(text);
        if (chunks.Length == 0) { _speakCoroutine = null; yield break; }

        // Kick off first chunk generation immediately
        Task<object> pending = TtsReflection.GenerateAsync(service, chunks[0]);

        for (int i = 0; i < chunks.Length; i++)
        {
            if (version != _speakVersion) { _speakCoroutine = null; yield break; }

            Task<object> current = pending;
            yield return new WaitUntil(() => current.IsCompleted);

            if (version != _speakVersion) { _speakCoroutine = null; yield break; }

            // Start next chunk generation while we play current — overlaps with playback
            if (i + 1 < chunks.Length)
                pending = TtsReflection.GenerateAsync(service, chunks[i + 1]);

            if (current.IsFaulted || current.Result == null) continue;
            object result = current.Result;
            if (!TtsReflection.IsValid(result)) continue;

            AudioClip clip = TtsReflection.ToAudioClip(result, $"tts_{version}_{i}");
            if (clip == null || version != _speakVersion) { _speakCoroutine = null; yield break; }

            if (_audioSource != null)
            {
                _audioSource.clip = clip;
                _audioSource.Play();
                yield return new WaitWhile(() => _audioSource.isPlaying && version == _speakVersion);
            }
        }
        _speakCoroutine = null;
    }

    private static string[] SplitIntoChunks(string text)
    {
        var result = new List<string>();
        var sb = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            sb.Append(c);

            int chunkIdx = result.Count;
            int maxLen = chunkIdx < ChunkMaxLengths.Length ? ChunkMaxLengths[chunkIdx] : ChunkMaxLengths[ChunkMaxLengths.Length - 1];
            int softMin = chunkIdx == 0 ? 4 : 8;

            bool hard = System.Array.IndexOf(HardSplitChars, c) >= 0;
            bool soft = System.Array.IndexOf(SoftSplitChars, c) >= 0 && sb.Length >= softMin;
            bool overMax = sb.Length >= maxLen;

            if (hard || soft || overMax)
            {
                // Protect dependent suffix: if the very next char is a suffix, absorb it first
                if (!hard && i + 1 < text.Length && System.Array.IndexOf(SuffixChars, text[i + 1]) >= 0)
                {
                    sb.Append(text[i + 1]);
                    i++;
                }

                string chunk = sb.ToString().Trim();
                if (chunk.Length > 0) result.Add(chunk);
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            string chunk = sb.ToString().Trim();
            if (chunk.Length > 0) result.Add(chunk);
        }

        return result.ToArray();
    }
}
#endif // TTS disabled
