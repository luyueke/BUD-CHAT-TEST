using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Pointone.Sound
{
    [System.Serializable]
    public class AudioMixerGroupRef
    {
        public AudioMixerGroup group;
    }

    public partial class ContainerContext
    {
        public System.Random rng = new System.Random();
        public Dictionary<string, string> switchLookup;
        public string lastRandomKey;

        // 最终命中的条目可覆盖 loop（默认继承事件级设置）
        public LoopOverrideMode loopMode = LoopOverrideMode.Inherit;

        // 从 Event.root 开始沿命中路径累计的音量倍率（Event 自身 volume 不在这里）
        public float volumeScale = 1f;
    }
}


