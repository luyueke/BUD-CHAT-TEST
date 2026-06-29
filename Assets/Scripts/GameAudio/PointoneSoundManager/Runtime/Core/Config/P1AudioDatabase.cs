using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Pointone.Sound
{
    [CreateAssetMenu(menuName = "PointoneSoundMgr/Audio/Database")]
    public partial class P1AudioDatabase : ScriptableObject
    {
        [Header("Mixer & Buses")]
        public AudioMixer mixer;
        public AudioMixerGroup master;
        public AudioMixerGroup music;
        public AudioMixerGroup sfx;
        public AudioMixerGroup ambient;
        public AudioMixerGroup voice;

        [Header("Ducking (Sidechain)")]
        [Tooltip("被压低的目标分组（例如 Music）")]
        public AudioMixerGroup duckTarget;
        [Tooltip("由该分组触发压低（例如 SFX）")]
        public AudioMixerGroup duckSidechainSource;
        [Tooltip("用于控制鸭嘴深度的暴露参数名，建议绑定到目标组的压缩器阈值/增益")]
        public string duckAmountParameter = "Duck_Amount";
        [Range(0f, 1f)] public float defaultDuckAmount = 0.0f;

        [Header("Events")]
        public List<P1Event> events = new List<P1Event>();

        private readonly Dictionary<string, P1Event> nameToEvent = new Dictionary<string, P1Event>();
        private readonly Dictionary<uint, P1Event> idToEvent = new Dictionary<uint, P1Event>();

        public void BuildIndex(PointoneAudioManager mgr)
        {
            nameToEvent.Clear();
            idToEvent.Clear();
            foreach (var e in events)
            {
                if (e == null || string.IsNullOrEmpty(e.eventName)) continue;
                nameToEvent[e.eventName] = e;
                idToEvent[mgr.GetIdFromString(e.eventName)] = e;
            }
        }

        public P1Event Find(uint id)
        {
            idToEvent.TryGetValue(id, out var e);
            return e;
        }

        public P1Event Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            nameToEvent.TryGetValue(name, out var e);
            return e;
        }
    }
}


