using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pointone.Sound
{
    [CreateAssetMenu(menuName = "PointoneSoundMgr/Audio/Switch Container")]
    public class P1SwitchContainer : P1Container
    {
        [Serializable]
        public class Mapping
        {
            public string group;
            public string state;
            public P1Container child;
            [Range(0f, 1f)] public float volume = 1f;
        }

        public List<Mapping> mappings = new List<Mapping>();

        [Header("Configs (推荐)")]
        [Tooltip("若不为空，将优先使用独立配置资产；为空时回退使用 mappings（向后兼容）。")]
        public List<P1SwitchMappingConfig> mappingConfigs = new List<P1SwitchMappingConfig>();
        public P1Container fallback;

        public override AudioClip PickClip(GameObject refObj, ContainerContext ctx)
        {
            var prevContainerVolume = ctx.volumeScale;
            ctx.volumeScale *= Mathf.Max(0f, volume);

            // 新结构：优先使用独立配置资产
            if (mappingConfigs != null && mappingConfigs.Count > 0)
            {
                foreach (var cfg in mappingConfigs)
                {
                    if (cfg == null) continue;
                    var keyPerGo = cfg.group + "@" + (refObj ? refObj.GetInstanceID().ToString() : "global");
                    var keyGlobal = cfg.group + "@global";
                    var matched = false;
                    if (ctx.switchLookup != null && refObj && ctx.switchLookup.TryGetValue(keyPerGo, out var stGo) && stGo == cfg.state) matched = true;
                    else if (ctx.switchLookup != null && ctx.switchLookup.TryGetValue(keyGlobal, out var stGl) && stGl == cfg.state) matched = true;
                    if (matched)
                    {
                        var prev = ctx.loopMode;
                        var prevEntryVolume = ctx.volumeScale;
                        ctx.volumeScale *= Mathf.Max(0f, cfg.volume);
                        if (cfg.loopOverride != LoopOverrideMode.Inherit)
                        {
                            // 更深层若再覆盖，会继续覆盖；失败回滚
                            ctx.loopMode = cfg.loopOverride;
                        }

                        var clip = cfg.child ? cfg.child.PickClip(refObj, ctx) : null;
                        if (clip != null) return clip;
                        ctx.loopMode = prev;
                        ctx.volumeScale = prevEntryVolume;
                    }
                }
            }

            // 旧结构：回退使用 mappings
            if (mappings != null)
            {
                foreach (var m in mappings)
                {
                    var keyPerGo = m.group + "@" + (refObj ? refObj.GetInstanceID().ToString() : "global");
                    var keyGlobal = m.group + "@global";
                    var matched = false;
                    if (ctx.switchLookup != null && refObj && ctx.switchLookup.TryGetValue(keyPerGo, out var stGo) && stGo == m.state) matched = true;
                    else if (ctx.switchLookup != null && ctx.switchLookup.TryGetValue(keyGlobal, out var stGl) && stGl == m.state) matched = true;
                    if (matched)
                    {
                        var prevEntryVolume = ctx.volumeScale;
                        ctx.volumeScale *= Mathf.Max(0f, m.volume);
                        var clip = m.child ? m.child.PickClip(refObj, ctx) : null;
                        if (clip != null) return clip;
                        ctx.volumeScale = prevEntryVolume;
                    }
                }
            }

            var fallbackClip = fallback ? fallback.PickClip(refObj, ctx) : null;
            if (fallbackClip != null) return fallbackClip;

            ctx.volumeScale = prevContainerVolume;
            return null;
        }
    }
}


