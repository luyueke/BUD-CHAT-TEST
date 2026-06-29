using UnityEngine;

namespace Pointone.Sound
{
    public static class ClipResolver
    {
        // 避免每次解析随机容器都 new Random() 导致短时间内重复（相同种子/相近种子）
        private static readonly System.Random SharedRng = new System.Random();

        public struct ResolvedClip
        {
            public AudioClip clip;
            public LoopOverrideMode loopMode;
            public float volumeScale;
        }

        public static AudioClip Resolve(uint eventId, GameObject refObj, string pureEventName = null)
        {
            return ResolveWithMeta(eventId, refObj, pureEventName).clip;
        }

        public static ResolvedClip ResolveWithMeta(uint eventId, GameObject refObj, string pureEventName = null)
        {
            var mgr = PointoneAudioManager.Instance;

            var ctx = new ContainerContext
            {
                rng = SharedRng,
                switchLookup = mgr.GetSwitchSnapshot(),
                lastRandomKey = null,
                loopMode = LoopOverrideMode.Inherit,
                volumeScale = 1f
            };

            var db = mgr.GetCachedDatabase();
            if (db != null)
            {
                P1Event p1Event = null;
                if (!string.IsNullOrEmpty(pureEventName)) p1Event = db.Find(pureEventName);
                if (p1Event == null) p1Event = db.Find(eventId);

                if (p1Event != null)
                {
                    if (p1Event.root != null)
                    {
                        var clip = p1Event.root.PickClip(refObj, ctx);
                        if (clip != null) return new ResolvedClip { clip = clip, loopMode = ctx.loopMode, volumeScale = ctx.volumeScale };
                    }
                    else
                    {
                        P1AudioLogger.LogWarning($"事件没有根容器: {pureEventName ?? eventId.ToString()}");
                    }
                }
            }

            var path = !string.IsNullOrEmpty(pureEventName) ? pureEventName : eventId.ToString();
            P1AudioLogger.LogFlow("使用兜底路径", path);
            var fallback = XAssetAudioLoader.LoadClip(path, refObj);
            return new ResolvedClip { clip = fallback, loopMode = LoopOverrideMode.Inherit, volumeScale = 1f };
        }
    }
}


