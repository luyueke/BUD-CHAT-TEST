#if false // TTS disabled
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 挂到 TTS AudioSource 所在 GameObject（或其父节点）。
/// 通过 SetTtsPlayer 传入 player 后，只要 TTS 功能开启就持续驱动
/// AudioMixer Flange Depth 做 sin 波动，与 TTS 是否正在朗读无关。
/// </summary>
public class TheatreTTSEffector : MonoBehaviour
{
    [Header("AudioMixer 配置")]
    [SerializeField] private AudioMixer mixer;
    [Tooltip("第一个 Flange 效果器暴露的 Depth 参数名")]
    [SerializeField] private string flangeDepthParam1 = "FlangeDepth1";
    [Tooltip("第二个 Flange 效果器暴露的 Depth 参数名")]
    [SerializeField] private string flangeDepthParam2 = "FlangeDepth2";

    [Header("波动参数")]
    [Tooltip("振荡频率，单位 Hz（每秒完成几个完整周期）。0.3 ≈ 约 3 秒一轮")]
    [SerializeField, Range(0.05f, 2f)] private float speed = 0.3f;
    [SerializeField] private float minDepth = 0.01f;
    [SerializeField] private float maxDepth = 0.8f;

    [Header("运行时预览（只读）")]
    [SerializeField, Range(0f, 1f)] private float _currentDepth;

    private TheatreTtsPlayer _ttsPlayer;
    private float _phase;

    public void SetTtsPlayer(TheatreTtsPlayer player)
    {
        _ttsPlayer = player;
    }

    private void Update()
    {
        if (_ttsPlayer == null || !_ttsPlayer.IsEnabled)
        {
            ApplyDepth(minDepth);
            return;
        }

        _phase += Time.deltaTime * speed * (Mathf.PI * 2f);
        if (_phase >= Mathf.PI * 2f) _phase -= Mathf.PI * 2f;

        float t = (Mathf.Sin(_phase) + 1f) * 0.5f;          // 0~1
        ApplyDepth(Mathf.LerpUnclamped(minDepth, maxDepth, t));
    }

    private void ApplyDepth(float depth)
    {
        _currentDepth = depth;
        mixer?.SetFloat(flangeDepthParam1, depth);
        mixer?.SetFloat(flangeDepthParam2, depth);
    }
}
#endif // TTS disabled
