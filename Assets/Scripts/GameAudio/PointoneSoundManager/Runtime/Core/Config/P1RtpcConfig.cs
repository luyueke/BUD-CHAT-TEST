using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pointone.Sound
{
    [CreateAssetMenu(menuName = "PointoneSoundMgr/Audio/RTPC Config")]
    public class P1RtpcConfig : ScriptableObject
    {
        public string configName;
        public List<P1Event.RtpcMapping> mappings = new List<P1Event.RtpcMapping>();
    }
}

