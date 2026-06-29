namespace Pointone.Sound
{
    public enum VoiceStealPolicy
    {
        None,
        Oldest,
        Newest
    }

    public enum RandomSelectMode
    {
        Weighted,
        WeightedNoImmediateRepeat
    }

    public enum RtpcTargetProperty
    {
        Volume,
        Pitch,
        LowpassCutoff,
        HighpassCutoff
    }

    /// <summary>
    /// Loop 覆盖三态：
    /// - Inherit: 继承事件级/上层设置（不修改 AudioSource.loop）
    /// - ForceLoop: 强制 loop = true
    /// - ForceNoLoop: 强制 loop = false
    /// </summary>
    public enum LoopOverrideMode
    {
        Inherit,
        ForceLoop,
        ForceNoLoop
    }
}


