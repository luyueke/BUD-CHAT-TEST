using UnityEngine;

namespace Pointone.Sound
{
    /// <summary>
    /// RandomContainer 的单条 Entry 独立配置（建议每条生成一个资产文件）
    /// </summary>
    [CreateAssetMenu(menuName = "PointoneSoundMgr/Audio/Random Entry Config")]
    public class P1RandomEntryConfig : ScriptableObject
    {
        public P1Container child;
        public float weight = 1f;
        [Range(0f, 1f)] public float volume = 1f;

        [Header("Loop Override")]
        public LoopOverrideMode loopOverride = LoopOverrideMode.Inherit;
    }
}





