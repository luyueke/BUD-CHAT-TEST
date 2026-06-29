using System;
using System.Collections;
using System.Collections.Generic;
using BestHTTP.WebSocket;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 豆包流式语音识别（ASR）管理器。
///
/// 使用方式：
///   1. 赋值 Config（ASRConfigAsset）
///   2. 调用 StartRecognition(uid, onResult, onError) 开始
///   3. 调用 StopRecognition() 结束，等待 onResult(IsFinal=true) 回调
///
/// 状态机：
///   Idle → Connecting → Recognizing → Stopping → Idle
/// </summary>
public class ASRManager
{
    private static ASRManager _inst;
    private static readonly object _lock = new object();

    public static ASRManager Inst
    {
        get
        {
            if (_inst == null)
            {
                lock (_lock)
                {
                    if (_inst == null)
                        _inst = new ASRManager();
                }
            }
            return _inst;
        }
    }

    /// <summary>在使用前赋值，包含 AppId/Token/Cluster 等凭证。</summary>
    public ASRConfigAsset Config;

    /// <summary>日志开关，上线前可设为 false 关闭所有 ASR 调试日志（错误日志始终输出）。</summary>
    public bool EnableLog = true;

    /// <summary>
    /// 软件增益倍数（默认 1.0 = 不放大）。麦克风硬件音量过低时可调高，
    /// 超出 [-1,1] 的采样会被 Clamp 截断。Android 真机通常不需要调整。
    /// </summary>
    [Range(1f, 50f)]
    public float MicGain = 1f;

    private enum State { Idle, Connecting, Recognizing, Stopping }

    private State     _state = State.Idle;
    private WebSocket _ws;
    private string    _reqId;

    // 麦克风录音相关
    private AudioClip _recordClip;   // Microphone.Start 返回的循环 AudioClip
    private string    _deviceName;   // 当前使用的麦克风设备名
    private int       _lastReadPos;  // 上一次读取到的 AudioClip 采样位置（用于增量读取）
    private Coroutine _streamCoroutine;
    private bool      _pendingStop;    // Connecting 阶段调用 StopRecognition 时置 true，连接成功后立即停止
    private Coroutine _timeoutCoroutine; // 发送 last 包后的超时兜底

#if UNITY_EDITOR
    // Editor 下无音频输入设备时自动置 true，发送静音帧验证协议流程，不影响打包
    private bool _mockMicMode;
#endif

    private Action<ASRResult> _onResult;
    private Action<string>    _onError;
    private string            _uid;

    private ASRCoroutineRunner _runner;

    private ASRCoroutineRunner Runner
    {
        get
        {
            if (_runner == null)
            {
                var go = new GameObject("[ASRCoroutineRunner]");
                Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<ASRCoroutineRunner>();
            }
            return _runner;
        }
    }

    private const int SampleRate    = 16000; // ASR 服务要求 16kHz
    private const int ChunkMs       = 100;   // 每次推送 100ms 的音频
    private const int MaxRecordSecs = 300;   // AudioClip 循环缓冲区长度（秒）
    private const int FinalTimeoutMs = 5000; // 发送 last 包后等待最终结果的超时时间（ms）

    // -------------------------------------------------------
    // Public API
    // -------------------------------------------------------

    /// <summary>
    /// 开始流式语音识别。内部依次执行：建立 WebSocket → 发送首包 → 启动麦克风 → 流式推送音频。
    /// </summary>
    /// <param name="uid">用户唯一标识，用于服务端日志追踪，传入账号 ID 即可</param>
    /// <param name="onResult">识别结果回调，IsFinal=true 时为最终结果</param>
    /// <param name="onError">错误回调，触发后识别已自动停止</param>
    public void StartRecognition(string uid, Action<ASRResult> onResult, Action<string> onError)
    {
        if (_state != State.Idle)
        {
            onError?.Invoke("ASRManager is already running");
            return;
        }
        if (Config == null)
        {
            onError?.Invoke("ASRManager.Config is not set");
            return;
        }
        if (Microphone.devices.Length == 0)
        {
#if UNITY_EDITOR
            Log("Editor 无麦克风设备，进入 MockMicMode");
#else
            onError?.Invoke("No microphone device found");
            return;
#endif
        }

        if (EnableLog) Log($"使用麦克风: {(Microphone.devices.Length > 0 ? Microphone.devices[0] : "MockMicMode（静音帧）")}");
        _uid      = string.IsNullOrEmpty(uid) ? SystemInfo.deviceUniqueIdentifier : uid;
        _onResult = onResult;
        _onError  = onError;
        _reqId    = Guid.NewGuid().ToString("N");
        _state    = State.Connecting;
        MicGain   = Config.MicGain;
        ASRProtocol.EnableLog = EnableLog;

        ConnectWebSocket();
    }

    /// <summary>
    /// 停止识别。发送最后一包音频（isLast=true），服务端收到后返回最终结果，
    /// 最终结果通过 onResult(IsFinal=true) 回调后自动 Cleanup。
    /// </summary>
    public void StopRecognition()
    {
        if (_state == State.Connecting)
        {
            // 连接尚未建立，记录挂起标志，OnWsOpen 后立即停止
            _pendingStop = true;
            return;
        }
        if (_state != State.Recognizing) return;
        _state = State.Stopping;
        SendRemainingAudioAndFinish();
    }

    public void Release()
    {
        Cleanup();
        if (_runner != null)
        {
            Object.Destroy(_runner.gameObject);
            _runner = null;
        }
        _inst = null;
    }

    // -------------------------------------------------------
    // WebSocket
    // -------------------------------------------------------

    private void ConnectWebSocket()
    {
        _ws = new WebSocket(new Uri("wss://openspeech.bytedance.com/api/v2/asr"));
        _ws.OnOpen   += OnWsOpen;
        _ws.OnBinary += OnWsBinary;
        _ws.OnError  += OnWsError;
        _ws.OnClosed += OnWsClosed;

        // 火山引擎 ASR WebSocket 握手鉴权：Bearer Token
        _ws.OnInternalRequestCreated = (w, req) =>
        {
            req.AddHeader("Authorization", $"Bearer; {Config.Token}");
        };
        _ws.Open();
    }

    private void OnWsOpen(WebSocket ws)
    {
        // 连接建立后立即发送首包（携带 appid/cluster/音频格式等参数），再启动麦克风
        byte[] frame = ASRProtocol.BuildFullClientRequest(Config, _reqId, _uid);
        _ws.Send(frame);
        StartMicrophone();
        _state = State.Recognizing;

        if (_pendingStop)
        {
            _pendingStop = false;
            StopRecognition();
        }
    }

    private void OnWsBinary(WebSocket ws, byte[] data)
    {
        var resp = ASRProtocol.ParseServerResponse(data);

        if (resp.code == 1013 || (resp.code == 1020 && _state == State.Stopping))
        {
            // 1013 = 静音；1020 = 停止时服务端未收到音频包（刚启动即停止，last 包为空）→ 均视为无语音正常结束
            Log($"未识别到语音 (code={resp.code})");
            var silentResult = new ASRResult { FullText = "", IsFinal = true, Utterances = new List<ASRUtterance>() };
            var cb = _onResult;
            Cleanup(); // Cleanup 先于回调，使 _state 回到 Idle，防止回调中 BeginRecord 时报 "already running"
            cb?.Invoke(silentResult);
            return;
        }

        if (resp.code != 1000)
        {
            string msg = $"ASR Error {resp.code}: {resp.message}";
            LogError(msg);
            _onError?.Invoke(msg);
            Cleanup();
            return;
        }

        var result = new ASRResult
        {
            FullText   = resp.result != null && resp.result.Length > 0 ? resp.result[0].text ?? "" : "",
            IsFinal    = resp.sequence < 0, // 服务端用负数 sequence 标记最终响应
            Utterances = MapUtterances(resp.result)
        };

        // 最终结果：先 Cleanup（_state → Idle），再回调，避免回调里调 BeginRecord 时状态仍是 Stopping
        if (result.IsFinal && _state == State.Stopping)
        {
            var cb = _onResult;
            Cleanup();
            cb?.Invoke(result);
        }
        else
        {
            _onResult?.Invoke(result);
        }
    }

    private void OnWsError(WebSocket ws, string error)
    {
        LogError($"WebSocket error: {error}");
        _onError?.Invoke(error);
        Cleanup();
    }

    private void OnWsClosed(WebSocket ws, ushort code, string message)
    {
        if (_state != State.Idle)
            Cleanup();
    }

    // -------------------------------------------------------
    // 麦克风 & 音频流式推送
    // -------------------------------------------------------

    private void StartMicrophone()
    {
#if UNITY_EDITOR
        _mockMicMode = Microphone.devices.Length == 0;
        if (_mockMicMode)
        {
            _streamCoroutine = Runner.StartCoroutine(SilenceStreamCoroutine());
            return;
        }
#endif
        _deviceName  = Microphone.devices[0];
        _lastReadPos = 0;
        // loop=true：AudioClip 作为环形缓冲区循环写入，避免长时间录音超出长度限制
        _recordClip  = Microphone.Start(_deviceName, loop: true, lengthSec: MaxRecordSecs, frequency: SampleRate);
        if (EnableLog) Log($"AudioClip 实际频率={_recordClip.frequency}Hz 声道={_recordClip.channels}（期望 {SampleRate}Hz 单声道）");
        _streamCoroutine = Runner.StartCoroutine(AudioStreamCoroutine());
    }

    /// <summary>每 100ms 读取一次麦克风新增采样并推送给服务端。</summary>
    private IEnumerator AudioStreamCoroutine()
    {
        var wait = new WaitForSeconds(ChunkMs / 1000f);
        while (_state == State.Recognizing)
        {
            yield return wait;
            SendAudioChunk(isLast: false);
        }
    }

#if UNITY_EDITOR
    /// <summary>MockMicMode：每 100ms 发一包静音 PCM16，用于在无麦克风的 Editor 下验证协议流程。</summary>
    private IEnumerator SilenceStreamCoroutine()
    {
        int silenceSamples = SampleRate * ChunkMs / 1000;
        byte[] silence = new byte[silenceSamples * 2]; // 全零 PCM16
        var wait = new WaitForSeconds(ChunkMs / 1000f);
        while (_state == State.Recognizing)
        {
            yield return wait;
            if (_ws != null && _ws.IsOpen)
                _ws.Send(ASRProtocol.BuildAudioOnlyRequest(silence, isLast: false));
        }
    }

    private const int MockFlushFrames = 12; // 停止时额外补发的静音帧数（12×100ms = 1.2s）

    /// <summary>
    /// MockMicMode 停止流程：逐帧发送静音帧直到 VAD 超时，再发 last 包。
    /// 必须异步执行，不能在同一帧内同步发送所有帧，否则服务端 VAD 无法处理。
    /// </summary>
    private IEnumerator MockFlushAndFinishCoroutine()
    {
        int silenceSamples = SampleRate * ChunkMs / 1000;
        byte[] silence = new byte[silenceSamples * 2];
        var wait = new WaitForSeconds(ChunkMs / 1000f);
        for (int i = 0; i < MockFlushFrames; i++)
        {
            yield return wait;
            if (_ws == null || !_ws.IsOpen) yield break;
            _ws.Send(ASRProtocol.BuildAudioOnlyRequest(silence, isLast: false));
        }
        if (_ws != null && _ws.IsOpen)
            _ws.Send(ASRProtocol.BuildAudioOnlyRequest(new byte[0], isLast: true));
        _mockMicMode = false;
        StartFinalTimeout();
    }
#endif

    /// <summary>
    /// 停止时调用：读取最后一段音频，停止麦克风，发送 isLast=true 的帧告知服务端结束。
    /// </summary>
    private void SendRemainingAudioAndFinish()
    {
        if (_streamCoroutine != null)
        {
            Runner.StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
        }

#if UNITY_EDITOR
        if (_mockMicMode)
        {
            // 异步逐帧发送静音帧 + last 包，不能同步发送否则服务端 VAD 无法处理
            _streamCoroutine = Runner.StartCoroutine(MockFlushAndFinishCoroutine());
            return;
        }
#endif

        int finalPos = Microphone.GetPosition(_deviceName);
        Microphone.End(_deviceName);

        byte[] pcm16 = ReadSamplesAsPCM16(_lastReadPos, finalPos);
        if (EnableLog) LogPcmAmplitude(pcm16, "last");
        // 即使没有剩余音频也要发 last 包，否则服务端不会返回最终结果
        byte[] frame = ASRProtocol.BuildAudioOnlyRequest(
            pcm16 != null && pcm16.Length > 0 ? pcm16 : new byte[0], isLast: true);
        _ws?.Send(frame);

        _recordClip  = null;
        _deviceName  = null;
        _lastReadPos = 0;
        StartFinalTimeout();
    }

    private void StartFinalTimeout()
    {
        _timeoutCoroutine = Runner.StartCoroutine(FinalTimeoutCoroutine());
    }

    private IEnumerator FinalTimeoutCoroutine()
    {
        yield return new WaitForSeconds(FinalTimeoutMs / 1000f);
        if (_state != State.Stopping) yield break;

#if UNITY_EDITOR
        // Editor MockMicMode：全零 PCM 无法触发服务端 VAD 结束检测，超时后模拟空结果，让流程正常完成
        Log($"MockMicMode 超时，模拟无语音结果");
        var emptyResult = new ASRResult { FullText = "", IsFinal = true, Utterances = new List<ASRUtterance>() };
        var cbTimeout = _onResult;
        Cleanup();
        cbTimeout?.Invoke(emptyResult);
#else
        LogError($"等待最终结果超时（{FinalTimeoutMs}ms），强制结束");
        var errCb = _onError;
        Cleanup();
        errCb?.Invoke("ASR final result timeout");
#endif
    }

    private void SendAudioChunk(bool isLast)
    {
        if (_recordClip == null || _ws == null || !_ws.IsOpen) return;

        int currentPos = Microphone.GetPosition(_deviceName);
        if (currentPos == _lastReadPos) return; // 本帧没有新采样

        byte[] pcm16 = ReadSamplesAsPCM16(_lastReadPos, currentPos);
        _lastReadPos = currentPos;

        if (pcm16 == null || pcm16.Length == 0) return;

        _ws.Send(ASRProtocol.BuildAudioOnlyRequest(pcm16, isLast));
    }

    /// <summary>
    /// 从 AudioClip 增量读取 [fromPos, toPos) 范围的采样并转为 PCM16 字节。
    /// 因 AudioClip 是环形缓冲区，需处理 toPos 小于 fromPos 时的回绕情况。
    /// </summary>
    private byte[] ReadSamplesAsPCM16(int fromPos, int toPos)
    {
        if (_recordClip == null) return null;

        float[] samples;
        if (toPos > fromPos)
        {
            // 正常情况：直接读取 [fromPos, toPos)
            int count = toPos - fromPos;
            samples = new float[count];
            _recordClip.GetData(samples, fromPos);
        }
        else if (toPos < fromPos)
        {
            // 环绕：AudioClip 写指针回到头部，分两段读取：[fromPos, end) + [0, toPos)
            int tailCount = _recordClip.samples - fromPos;
            int headCount = toPos;
            float[] tail = new float[tailCount];
            float[] head = new float[headCount];
            _recordClip.GetData(tail, fromPos);
            if (headCount > 0)
                _recordClip.GetData(head, 0);
            samples = new float[tailCount + headCount];
            Array.Copy(tail, 0, samples, 0, tailCount);
            Array.Copy(head, 0, samples, tailCount, headCount);
        }
        else
        {
            return null; // 位置未变，无新数据
        }

        if (MicGain != 1f)
        {
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Clamp(samples[i] * MicGain, -1f, 1f);
        }
        return ASRProtocol.FloatsToPCM16(samples);
    }

    // -------------------------------------------------------
    // 辅助方法
    // -------------------------------------------------------

    private static List<ASRUtterance> MapUtterances(ASRResultItem[] resultItems)
    {
        var list = new List<ASRUtterance>();
        if (resultItems == null || resultItems.Length == 0) return list;
        var utterances = resultItems[0].utterances;
        if (utterances == null) return list;
        foreach (var u in utterances)
        {
            list.Add(new ASRUtterance
            {
                Text      = u.text ?? "",
                StartTime = u.start_time,
                EndTime   = u.end_time,
                Definite  = u.definite
            });
        }
        return list;
    }

    private void LogPcmAmplitude(byte[] pcm16, string label)
    {
        if (!EnableLog || pcm16 == null || pcm16.Length < 2) return;
        short maxAmp = 0;
        for (int i = 0; i + 1 < pcm16.Length; i += 2)
        {
            short s = (short)(pcm16[i] | (pcm16[i + 1] << 8));
            if (s < 0) s = (short)-s;
            if (s > maxAmp) maxAmp = s;
        }
        Log($"PCM 幅度诊断[{label}] samples={pcm16.Length / 2} maxAmp={maxAmp} ({maxAmp / 327.67f:F1}% 满量程)");
    }

    private void Log(string msg)
    {
        if (EnableLog) Debug.Log($"[ASRManager] {msg}");
    }

    private void LogError(string msg)
    {
        Debug.LogError($"[ASRManager] {msg}");
    }

    private void Cleanup()
    {
        if (_streamCoroutine != null)
        {
            Runner.StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
        }

        if (_timeoutCoroutine != null)
        {
            Runner.StopCoroutine(_timeoutCoroutine);
            _timeoutCoroutine = null;
        }

        if (!string.IsNullOrEmpty(_deviceName) && Microphone.IsRecording(_deviceName))
            Microphone.End(_deviceName);

        if (_ws != null)
        {
            _ws.OnOpen   -= OnWsOpen;
            _ws.OnBinary -= OnWsBinary;
            _ws.OnError  -= OnWsError;
            _ws.OnClosed -= OnWsClosed;
            if (_ws.IsOpen)
                _ws.Close();
            _ws = null;
        }

        _recordClip  = null;
        _deviceName  = null;
        _lastReadPos = 0;
        _pendingStop = false;
        _onResult    = null;
        _onError     = null;
        _uid         = null;
        _state       = State.Idle;
    }
}

// -------------------------------------------------------
// 对外暴露的结果类型
// -------------------------------------------------------

/// <summary>每次收到服务端响应时回调的识别结果。</summary>
public class ASRResult
{
    /// <summary>整段音频的识别文本（累计）。</summary>
    public string FullText;

    /// <summary>true = 最终结果（服务端 sequence 为负数），false = 中间结果。</summary>
    public bool IsFinal;

    /// <summary>分句列表，仅在 show_utterances=true 时有值。</summary>
    public List<ASRUtterance> Utterances;
}

internal class ASRCoroutineRunner : MonoBehaviour { }

/// <summary>单条分句信息。</summary>
public class ASRUtterance
{
    public string Text;
    public int    StartTime;  // 分句起始时间（ms）
    public int    EndTime;    // 分句结束时间（ms）
    public bool   Definite;   // true = 分句已确定，false = 仍为中间结果
}
