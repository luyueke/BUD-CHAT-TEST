using System.Collections.Generic;
using UnityEngine;

namespace Pointone.Sound
{
    [DisallowMultipleComponent]
    public class VoiceController : MonoBehaviour
    {
        public uint EventId;
        public AudioSource Source;

        public P1Event boundEvent;
        private AudioLowPassFilter lp;
        private AudioHighPassFilter hp;
        private readonly List<P1Event.RtpcMapping> mappings = new List<P1Event.RtpcMapping>();
        private bool _hasDistanceMapping;

        private float _baseVolume = 1f;
        private float _globalVolumeScale = 1f;
        public float GlobalVolumeScale {
            get => _globalVolumeScale;
            set {
                _globalVolumeScale = value;
                if (Source != null) {
                    Source.volume = Mathf.Clamp01(_baseVolume * _globalVolumeScale);
                }
            }
        }
        
        private static Transform s_cachedListener;
        private static int s_listenerSearchFrame = -1;

        private static Transform GetListenerTransform()
        {
            if (s_cachedListener != null) return s_cachedListener;

            int frame = Time.frameCount;
            if (frame == s_listenerSearchFrame) return null;
            s_listenerSearchFrame = frame;

            var cam = Camera.main;
            if (cam != null)
            {
                var listener = cam.GetComponent<AudioListener>();
                if (listener != null)
                {
                    s_cachedListener = listener.transform;
                    return s_cachedListener;
                }
            }

            var found = Object.FindObjectOfType<AudioListener>();
            if (found != null) s_cachedListener = found.transform;
            return s_cachedListener;
        }

        private float GetRtpcInputValue(string rtpcName, Dictionary<string, float> globalRtpcs)
        {
            if (string.IsNullOrEmpty(rtpcName)) return float.NaN;
            
            if (rtpcName.Equals("Distance", System.StringComparison.OrdinalIgnoreCase))
            {
                var l = GetListenerTransform();
                if (l == null) return 0f;
                return Vector3.Distance(transform.position, l.position);
            }
            
            if (globalRtpcs != null && globalRtpcs.TryGetValue(rtpcName, out var v))
            {
                return v;
            }
            return float.NaN;
        }

        public void Initialize(uint eventId, P1Event p1Event, AudioSource source)
        {
            EventId = eventId;
            Source = source;
            _baseVolume = source.volume;
            boundEvent = p1Event;
            mappings.Clear();
            _hasDistanceMapping = false;
            if (boundEvent != null)
            {
                if (boundEvent.rtpcConfig != null && boundEvent.rtpcConfig.mappings != null && boundEvent.rtpcConfig.mappings.Count > 0)
                {
                    mappings.AddRange(boundEvent.rtpcConfig.mappings);
                }
            }
            for (int i = 0; i < mappings.Count; i++)
            {
                if (!string.IsNullOrEmpty(mappings[i].rtpcName) && 
                    mappings[i].rtpcName.Equals("Distance", System.StringComparison.OrdinalIgnoreCase))
                {
                    _hasDistanceMapping = true;
                    break;
                }
            }
            if (NeedLowPass()) lp = gameObject.GetComponent<AudioLowPassFilter>() ?? gameObject.AddComponent<AudioLowPassFilter>();
            if (NeedHighPass()) hp = gameObject.GetComponent<AudioHighPassFilter>() ?? gameObject.AddComponent<AudioHighPassFilter>();

            Source.volume = Mathf.Clamp01(_baseVolume * _globalVolumeScale);
        }

        private bool NeedLowPass()
        {
            for (int i = 0; i < mappings.Count; i++)
                if (mappings[i].target == RtpcTargetProperty.LowpassCutoff) return true;
            return false;
        }

        private bool NeedHighPass()
        {
            for (int i = 0; i < mappings.Count; i++)
                if (mappings[i].target == RtpcTargetProperty.HighpassCutoff) return true;
            return false;
        }

        private void Update()
        {
            if (Source == null || !Source.isPlaying || mappings.Count == 0) return;

            Dictionary<string, float> rtpcs = null;
            if (mappings.Count > 0)
            {
                rtpcs = PointoneAudioManager.Instance.GetRtpcSnapshot();
            }
            
            float currentVol = _baseVolume;

            for (int i = 0; i < mappings.Count; i++)
            {
                var m = mappings[i];
                if (string.IsNullOrEmpty(m.rtpcName)) continue;
                var v = GetRtpcInputValue(m.rtpcName, rtpcs);
                if (float.IsNaN(v)) continue;
                float eval = m.curve != null ? m.curve.Evaluate(v) * m.scale : v * m.scale;
                switch (m.target)
                {
                    case RtpcTargetProperty.Volume:
                        if (eval > 1.0f) eval /= 100f;
                        currentVol *= eval;
                        break;
                    case RtpcTargetProperty.Pitch:
                        Source.pitch = Mathf.Clamp(eval, 0.1f, 3f);
                        break;
                    case RtpcTargetProperty.LowpassCutoff:
                        if (lp != null) lp.cutoffFrequency = Mathf.Clamp(eval, 10f, 22000f);
                        break;
                    case RtpcTargetProperty.HighpassCutoff:
                        if (hp != null) hp.cutoffFrequency = Mathf.Clamp(eval, 10f, 22000f);
                        break;
                }
            }
            
            Source.volume = Mathf.Clamp01(currentVol * GlobalVolumeScale);
        }
    }
}


