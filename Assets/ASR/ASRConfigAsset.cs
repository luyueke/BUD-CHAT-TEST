using UnityEngine;

/// <summary>
/// 豆包流式语音识别（ASR）配置。
/// 右键 Project 窗口 → Create → Config → ASRConfig 创建实例，
/// 在 Inspector 中填写控制台下发的凭证后，赋值给 ASRManager.Inst.Config。
/// </summary>
[CreateAssetMenu(fileName = "ASRConfig", menuName = "Config/ASRConfig")]
public class ASRConfigAsset : ScriptableObject
{
    [Header("应用凭证（豆包语音控制台 → 流式识别 → 应用管理）")]
    [Tooltip("控制台应用 ID")]
    public string AppId;

    [Tooltip("控制台访问令牌")]
    public string Token;

    [Tooltip("控制台 Cluster ID，根据场景选择对应集群")]
    public string Cluster;

    [Header("识别参数")]
    [Tooltip("VAD 尾部静音时长(ms)，检测到该时长静音后认为说话结束，范围 500-6000，推荐 800-1000")]
    public string VadSilenceTime = "800";

    [Tooltip("软件增益倍数（默认 1）。麦克风硬件音量过低时调高，超出 [-1,1] 的采样会被截断。" +
             "幅度诊断 maxAmp < 1000 时建议从 10 开始调，目标 5000~20000")]
    [Range(1f, 50f)]
    public float MicGain = 1f;

    [Tooltip("开启 ITN：将口语数字转为阿拉伯数字，如「三十八」→「38」")]
    public bool EnableITN = false;

    [Tooltip("开启标点：识别结果自动添加逗号、句号等标点符号")]
    public bool EnablePunctuation = false;
}
