using UnityEngine;

namespace Pointone.Sound
{
    /// <summary>
    /// 类似 Unity AudioSource 的 3D Sound Settings，可被多个 P1Event 复用。
    /// 用于配置 3D 空间化、距离衰减（含 Custom Rolloff 曲线）、多普勒等。
    /// </summary>
    [CreateAssetMenu(menuName = "PointoneSoundMgr/Audio/3D Sound Config")]
    public class P1Spatial3DSoundConfig : ScriptableObject
    {
        [Header("Spatial Blend")]
        [Range(0f, 1f)] public float spatialBlend = 1f; // 0=2D, 1=3D

        [Header("Distance")]
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        [Min(0f)] public float minDistance = 1f;
        [Min(0f)] public float maxDistance = 50f;

        [Header("Custom Rolloff (only when RolloffMode=Custom)")]
        public AnimationCurve customRolloff = AnimationCurve.Linear(0f, 1f, 50f, 0f);

        [Header("Other 3D Settings")]
        [Range(0f, 5f)] public float dopplerLevel = 0f;
        [Range(0f, 360f)] public float spread = 0f;
        [Range(0f, 1f)] public float panLevel = 1f;
        [Range(0f, 1.1f)] public float reverbZoneMix = 1f;
    }
}


