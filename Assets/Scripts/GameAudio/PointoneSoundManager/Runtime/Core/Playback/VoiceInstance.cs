using UnityEngine;

namespace Pointone.Sound
{
    public class VoiceInstance
    {
        public uint Id { get; }
        public AudioSource Source { get; }
        public uint EventId { get; }
        public string GroupName { get; }
        public float StartTime { get; }
        public string InstanceKey { get; }

        public VoiceInstance(uint id, AudioSource source, uint eventId, string instanceKey, string groupName)
        {
            Id = id;
            Source = source;
            EventId = eventId;
            InstanceKey = instanceKey;
            GroupName = groupName;
            StartTime = Time.time;
        }

        public void Stop()
        {
            if (Source != null)
            {
                Source.Stop();
            }
        }

        public void Pause()
        {
            if (Source != null && Source.isPlaying)
            {
                Source.Pause();
            }
        }

        public void Resume()
        {
            if (Source != null)
            {
                Source.UnPause();
            }
        }

        public bool IsPaused => Source != null && !Source.isPlaying && Source.time > 0f;
    }
}


