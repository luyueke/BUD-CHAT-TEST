using UnityEngine;

namespace Pointone.Sound
{
    /// <summary>
    /// SwitchContainer 的单条映射独立配置（建议每条生成一个资产文件）
    /// </summary>
    [CreateAssetMenu(menuName = "PointoneSoundMgr/Audio/Switch Mapping Config")]
    public class P1SwitchMappingConfig : ScriptableObject
    {
        public string group;
        public string state;
        public P1Container child;
        [Range(0f, 1f)] public float volume = 1f;

        [Header("Loop Override")]
        public LoopOverrideMode loopOverride = LoopOverrideMode.Inherit;
    }
}





