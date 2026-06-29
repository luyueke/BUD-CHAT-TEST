using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pointone.Sound
{
    public abstract class P1Container : ScriptableObject
    {
        [Tooltip("可选：若配置了事件级 Bus，则以事件为准；否则用此容器上的 Bus")] public AudioMixerGroupRef bus;
        [Range(0f, 1f)] public float volume = 1f;
        public abstract AudioClip PickClip(GameObject refObj, ContainerContext ctx);
    }
}


