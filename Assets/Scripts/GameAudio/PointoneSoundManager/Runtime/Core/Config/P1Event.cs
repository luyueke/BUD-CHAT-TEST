using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pointone.Sound
{
    [CreateAssetMenu(menuName = "PointoneSoundMgr/Audio/Event")]
    public class P1Event : ScriptableObject
    {
        [Header("Identity")]
        public string eventName;
        public string groupName;

        [Header("Routing")]
        public AudioMixerGroupRef bus;

        [Header("Playback")]
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = false;
        [Range(0.1f, 3f)] public float pitch = 1f;

        [Header("Limits")]
        public int maxInstances = 0; // 0=无限
        public bool perGameObject = false;
        public VoiceStealPolicy stealPolicy = VoiceStealPolicy.None;

        [Header("Container")]
        public P1Container root;

        [Header("3D Sound")]
        public P1Spatial3DSoundConfig spatial3DConfig;

        [Header("RTPC Mappings")]
        public P1RtpcConfig rtpcConfig; // Optional shared config

        [Serializable]
        public class RtpcMapping
        {
            public string rtpcName = "";
            public AnimationCurve curve = AnimationCurve.Linear(0, 1, 100, 1);
            public RtpcTargetProperty target = RtpcTargetProperty.Volume;
            public float scale = 1f;
        }
    }
}


