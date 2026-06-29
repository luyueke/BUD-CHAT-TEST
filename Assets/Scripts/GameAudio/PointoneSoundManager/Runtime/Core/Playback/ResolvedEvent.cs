using UnityEngine;
using UnityEngine.Audio;

namespace Pointone.Sound
{
    public class ResolvedEvent
    {
        public AudioClip Clip;
        public AudioMixerGroup Bus;
        public float Volume = 1f;
        public bool Loop = false;
        public float Pitch = 1f;
        public P1Event SourceEvent;
    }
}


