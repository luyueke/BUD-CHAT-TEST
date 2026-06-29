using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pointone.Sound
{
    [CreateAssetMenu(menuName = "PointoneSoundMgr/Audio/Random Container")]
    public class P1RandomContainer : P1Container
    {
        [Serializable]
        public class Entry
        {
            public P1Container child;
            public float weight = 1f;
            [Range(0f, 1f)] public float volume = 1f;
            [HideInInspector] public string cacheKey;
        }

        public RandomSelectMode mode = RandomSelectMode.WeightedNoImmediateRepeat;
        public List<Entry> entries = new List<Entry>();

        [Header("Configs (推荐)")]
        [Tooltip("若不为空，将优先使用独立配置资产；为空时回退使用 entries（向后兼容）。")]
        public List<P1RandomEntryConfig> entryConfigs = new List<P1RandomEntryConfig>();

        public override AudioClip PickClip(GameObject refObj, ContainerContext ctx)
        {
            var prevContainerVolume = ctx.volumeScale;
            ctx.volumeScale *= Mathf.Max(0f, volume);

            // 新结构：优先使用独立配置资产
            if (entryConfigs != null && entryConfigs.Count > 0)
            {
                float totalCfg = 0f;
                foreach (var e in entryConfigs)
                {
                    if (e == null) continue;
                    totalCfg += Mathf.Max(0f, e.weight);
                }
                if (totalCfg <= 0f)
                {
                    ctx.volumeScale = prevContainerVolume;
                    return null;
                }

                AudioClip ResolveEntryCfg(P1RandomEntryConfig e)
                {
                    if (e == null || e.child == null) return null;
                    var prev = ctx.loopMode;
                    var prevEntryVolume = ctx.volumeScale;
                    ctx.volumeScale *= Mathf.Max(0f, e.volume);
                    if (e.loopOverride != LoopOverrideMode.Inherit)
                    {
                        ctx.loopMode = e.loopOverride;
                    }
                    var clip = e.child.PickClip(refObj, ctx);
                    if (clip != null) return clip;
                    ctx.loopMode = prev;
                    ctx.volumeScale = prevEntryVolume;
                    return null;
                }

                for (int safety = 0; safety < 8; safety++)
                {
                    float r = (float)(ctx.rng.NextDouble() * totalCfg);
                    float acc = 0f;
                    foreach (var e in entryConfigs)
                    {
                        if (e == null) continue;
                        acc += Mathf.Max(0f, e.weight);
                        if (r <= acc)
                        {
                            var key = e.child ? e.child.name : "";
                            if (mode == RandomSelectMode.WeightedNoImmediateRepeat && key == ctx.lastRandomKey)
                            {
                                break; // 重抽
                            }
                            ctx.lastRandomKey = key;
                            var clip = ResolveEntryCfg(e);
                            if (clip != null) return clip;
                            continue;
                        }
                    }
                }

                // 兜底返回第一个有效 entry
                for (int i = 0; i < entryConfigs.Count; i++)
                {
                    var e = entryConfigs[i];
                    if (e == null || e.child == null) continue;
                    ctx.lastRandomKey = e.child.name;
                    var clip = ResolveEntryCfg(e);
                    if (clip != null) return clip;
                }
                ctx.volumeScale = prevContainerVolume;
                return null;
            }

            if (entries == null || entries.Count == 0)
            {
                ctx.volumeScale = prevContainerVolume;
                return null;
            }

            float total = 0f;
            foreach (var e in entries) total += Mathf.Max(0f, e.weight);
            if (total <= 0f)
            {
                ctx.volumeScale = prevContainerVolume;
                return null;
            }

            AudioClip ResolveEntry(Entry e)
            {
                if (e == null || e.child == null) return null;
                var prevEntryVolume = ctx.volumeScale;
                ctx.volumeScale *= Mathf.Max(0f, e.volume);
                var clip = e.child.PickClip(refObj, ctx);
                if (clip != null) return clip;
                ctx.volumeScale = prevEntryVolume;
                return null;
            }

            for (int safety = 0; safety < 8; safety++)
            {
                float r = (float)(ctx.rng.NextDouble() * total);
                float acc = 0f;
                foreach (var e in entries)
                {
                    acc += Mathf.Max(0f, e.weight);
                    if (r <= acc)
                    {
                        var key = e.child ? e.child.name : "";
                        if (mode == RandomSelectMode.WeightedNoImmediateRepeat && key == ctx.lastRandomKey)
                        {
                            break; // 重抽
                        }
                        ctx.lastRandomKey = key;
                        var clip = ResolveEntry(e);
                        if (clip != null) return clip;
                        continue;
                    }
                }
            }

            // 兜底返回第一个
            ctx.lastRandomKey = entries[0].child ? entries[0].child.name : "";
            var fallbackClip = ResolveEntry(entries[0]);
            if (fallbackClip != null) return fallbackClip;

            ctx.volumeScale = prevContainerVolume;
            return null;
        }
    }
}


