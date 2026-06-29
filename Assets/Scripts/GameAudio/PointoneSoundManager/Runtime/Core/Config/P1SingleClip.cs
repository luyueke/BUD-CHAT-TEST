using UnityEngine;

namespace Pointone.Sound
{
    [CreateAssetMenu(menuName = "PointoneSoundMgr/Audio/Single Clip")]
    public class P1SingleClip : P1Container
    {
        public string clipPath;

        public bool isStream;

        public override AudioClip PickClip(GameObject refObj, ContainerContext ctx)
        {
            ctx.volumeScale *= Mathf.Max(0f, volume);
            return XAssetAudioLoader.LoadClip(clipPath, refObj);
        }
    }
}


