using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ASR 接入测试脚本，仅用于开发验证，不用于正式业务。
/// 挂到场景任意 GameObject，在 Inspector 中指定 Config 后运行。
///
/// 操作方式：
///   Editor / PC：按住空格开始录音，松开停止
///   移动端：触摸屏幕开始录音，松开停止
/// </summary>
public class ASRTester : MonoBehaviour
{
    public static ASRTester Instance { get; private set; }

    [SerializeField] private ASRConfigAsset _config;

    private bool _isRecording;
    private bool _isLooping;
    private Coroutine _loopCoroutine;
    private int _loopCycleId;
    private const float LoopIntervalSeconds = 5f;

    private void Awake()
    {
        Instance = this;
    }

    public void SetConfig(ASRConfigAsset config)
    {
        _config = config;
    }

    /// <summary>ASR 识别到最终文本时触发，参数为识别文本（空字符串表示静音/无结果）。</summary>
    public Action<string> OnFinalText;

//     private void Update()
//     {
// #if UNITY_EDITOR || UNITY_STANDALONE
//         if (Input.GetKeyDown(KeyCode.Space))
//             BeginRecord();

//         if (Input.GetKeyUp(KeyCode.Space))
//             EndRecord();
// #else
//         if (Input.touchCount > 0)
//         {
//             var touch = Input.GetTouch(0);
//             if (touch.phase == TouchPhase.Began)
//                 BeginRecord();
//             else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
//                 EndRecord();
//         }
// #endif
//     }

    public void BeginRecord()
    {
        if (_isRecording) return;

        if (_config == null)
        {
            Debug.LogError("[ASRTester] Config 未设置，请在 Inspector 中指定 ASRConfig.asset");
            return;
        }

        Debug.Log($"[ASRTester] 当前麦克风列表: [{string.Join(", ", Microphone.devices)}]");
        ASRManager.Inst.Config = _config;
        // uid 从 PlayerPrefs 读取，与项目其他模块保持一致
        string uid = PlayerPrefs.GetString("Cabin_uid", "None");
        _isRecording = true;
        _isLooping = true;
        ASRManager.Inst.StartRecognition(uid, OnResult, OnError);
        // StartRecognition 同步失败时 OnError 会将 _isRecording 置为 false，所以此处判断后再打日志
        if (_isRecording)
        {
            Debug.Log($"[ASRTester] 开始录音（每 {LoopIntervalSeconds}s 自动转录）...");
            ResetLoopTimer();
        }
    }

    public void EndRecord()
    {
        _isLooping = false;
        StopLoopTimer();
        if (!_isRecording) return;

        _isRecording = false;
        ASRManager.Inst.StopRecognition();
        Debug.Log("[ASRTester] 停止录音，等待最终结果...");
    }

    private void ResetLoopTimer()
    {
        StopLoopTimer();
        int myId = ++_loopCycleId;
        _loopCoroutine = StartCoroutine(LoopTimerCoroutine(myId));
    }

    private void StopLoopTimer()
    {
        if (_loopCoroutine != null)
        {
            StopCoroutine(_loopCoroutine);
            _loopCoroutine = null;
        }
    }

    private IEnumerator LoopTimerCoroutine(int cycleId)
    {
        yield return new WaitForSeconds(LoopIntervalSeconds);
        // cycleId 不匹配说明新一轮已经启动，旧 coroutine 直接退出，避免误停新轮
        if (!_isLooping || !_isRecording || cycleId != _loopCycleId) yield break;
        Debug.Log("[ASRTester] 5s 到，停止本轮识别，等待结果后重启...");
        _isRecording = false;
        ASRManager.Inst.StopRecognition();
        // OnResult(IsFinal=true) 回调后自动重启下一轮
    }

    /// <summary>每次收到服务端响应时触发，IsFinal=true 时为最终识别结果。</summary>
    private void OnResult(ASRResult result)
    {
        string tag = result.IsFinal ? "[最终]" : "[中间]";
        Debug.Log($"[ASRTester] {tag} {result.FullText}");

        // 最终结果时打印每条分句的时间戳，方便验证分句是否正确
        if (result.IsFinal && result.Utterances != null)
        {
            foreach (var u in result.Utterances)
                Debug.Log($"  分句 [{u.StartTime}ms~{u.EndTime}ms] {u.Text}");
        }

        if (result.IsFinal)
        {
            _isRecording = false;   // ASRManager 已 Cleanup，重置标志允许下次 BeginRecord
            OnFinalText?.Invoke(result.FullText ?? "");
            if (_isLooping)
                BeginRecord();      // 自动重启下一轮
        }
    }

    private void OnError(string error)
    {
        _isRecording = false;
        StopLoopTimer();
        Debug.LogError($"[ASRTester] 错误: {error}");
        // 出错时不自动重试，由外部（状态系统）决定是否重启
    }

    private void OnDestroy()
    {
        _isLooping = false;
        StopLoopTimer();
        // 场景卸载时确保麦克风和连接正常关闭
        if (_isRecording)
            ASRManager.Inst.StopRecognition();
    }
}
