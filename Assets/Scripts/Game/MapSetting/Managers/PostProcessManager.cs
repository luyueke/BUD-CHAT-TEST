using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsComponents;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.MapSetting
{
    
    public class PostProcessManager : BaseMapSettingManager<PostProcessManager>
    {
        private GameObject postProcessGo;
        private PostProcessComponent postComp;
        
        private Volume _volume;
        private Bloom _bloom;
        private DepthOfField _dof;
        private ColorAdjustments _colorAdjustments;

        // 记录默认 Volume（用于“无/还原”）
        private VolumeProfile _defaultSharedProfile;
        private VolumeProfile _defaultProfile;
        private bool _hasCachedDefault;

        // Lens 调参专用：基于“默认 Volume”克隆出来的运行时 Profile（避免修改资产）
        private bool _lensAdjustActive;
        private VolumeProfile _lensRuntimeProfile;

        private float _ratio = 0.2f;
        private float defaultIntensity = 5f;
        
        public PostProcessManager()
        {
            postProcessGo = GameObject.Find("PostProcessVolume");
            if (postProcessGo)
            {
                _volume = postProcessGo.GetComponent<Volume>();
                if (_volume != null)
                {
                    CacheDefaultVolumeProfileIfNeeded();
                    RefreshCachedVolumeComponents();
                }
            }
            
            //TODO:@jaywill 默认打开，后续需要跟进设置来决定是否打开
            SetPostProcessActive(true);
        }

        //地图还原数据
        public override void OnCreateByData()
        {
            base.OnCreateByData();
            postComp = GameMapSettingManager.Inst.settingEntity.GetComp<PostProcessComponent>();
            if (postComp == null)
            {
                postComp = GameMapSettingManager.Inst.settingEntity.AddComp<PostProcessComponent>();
                SetDefault();
            }
            //还原表现层
            SetBloomActive(postComp.BloomState);
            SetBloomIntensity(postComp.BloomIntensity);
        }

        public void SetDefault(bool isActive = true)
        {
            SetBloomActive(isActive ? 1 : 0);
            SetBloomIntensity(defaultIntensity);
        }

        public PostProcessComponent GetSettingComp()
        {
            return postComp;
        }
        
        public void SetBloomActive(int isActive)
        {
            postComp.BloomState = isActive;
            // if (_bloom != null)
            // {
            //     _bloom.active = isActive == 1;
            // }
        }
        
        public void SetBloomIntensity(float inte)
        {
            postComp.BloomIntensity = inte;
            // if (_bloom != null)
            //     _bloom.intensity.value = inte * _ratio;
        }
        
        public void SetPostProcessActive(bool isActive)
        {
            // GlobalFieldController.isOpenPostProcess = isActive;
            if (_volume != null)
            {
                _volume.enabled = isActive;
            }
        }

        /// <summary>
        /// 根据配置替换后处理 Volume。
        /// 注意：不影响原有 Bloom 开关/强度数据结构，仅替换 Volume 的 Profile 来源。
        /// </summary>
        /// <param name="resourcePath">配置里的资源路径，可能是 VolumeProfile(.asset) 或带 Volume 的 Prefab</param>
        /// <param name="user">资源使用者，用于自动释放缓存</param>
        /// <returns>是否替换成功</returns>
        public bool TryOverrideVolume(string resourcePath, GameObject user)
        {
            if (_volume == null) return false;
            if (string.IsNullOrEmpty(resourcePath)) return false;

            // Filter 等“替换 Profile”优先级更高：一旦覆盖，Lens 调参模式就必须退出
            EndLensAdjustMode();
            CacheDefaultVolumeProfileIfNeeded();

            VolumeProfile profile = null;

            // 1) 尝试直接加载 VolumeProfile
            try
            {
                profile = Loader.Load<VolumeProfile>(resourcePath, user);
            }
            catch
            {
                profile = null;
            }

            // 2) 若不是 Profile，则尝试当作 Prefab GameObject 加载，再取 Volume
            if (profile == null)
            {
                try
                {
                    var go = Loader.Load<GameObject>(resourcePath, user);
                    if (go != null)
                    {
                        var vol = go.GetComponent<Volume>();
                        if (vol != null)
                        {
                            profile = vol.sharedProfile != null ? vol.sharedProfile : vol.profile;
                        }
                    }
                }
                catch
                {
                    profile = null;
                }
            }

            if (profile == null) return false;

            // 使用 sharedProfile 更符合“替换配置资产”的预期
            _volume.sharedProfile = profile;
            // 同时刷新一下 runtime profile 引用（避免某些场景读取 profile 时仍指向旧对象）
            _volume.profile = profile;

            // 刷新缓存引用（保持原逻辑可用）
            RefreshCachedVolumeComponents();
            return true;
        }

        /// <summary>
        /// 还原为场景里原始的 Volume（用于选择“无”）。
        /// </summary>
        public void RestoreDefaultVolume()
        {
            if (_volume == null) return;
            EndLensAdjustMode();
            CacheDefaultVolumeProfileIfNeeded();

            if (!_hasCachedDefault) return;

            // 优先还原 sharedProfile（若有）
            if (_defaultSharedProfile != null)
            {
                _volume.sharedProfile = _defaultSharedProfile;
                _volume.profile = _defaultSharedProfile;
            }
            else if (_defaultProfile != null)
            {
                _volume.sharedProfile = null;
                _volume.profile = _defaultProfile;
            }

            RefreshCachedVolumeComponents();
        }

        /// <summary>
        /// 进入 Lens 调参模式：
        /// - 基于“默认 Volume”克隆 runtime profile
        /// - 之后 SetLensXXX 都在该 runtime profile 上改数值
        /// 
        /// 注意：它与 FilterMenu 的 TryOverrideVolume 冲突，互斥。
        /// </summary>
        public void BeginLensAdjustMode()
        {
            if (_volume == null) return;
            CacheDefaultVolumeProfileIfNeeded();
            if (!_hasCachedDefault) return;

            if (_lensAdjustActive && _lensRuntimeProfile != null && _volume.profile == _lensRuntimeProfile)
            {
                // 已在 Lens 调参模式
                return;
            }

            // 先确保回到“默认 Volume”，避免在 filter profile 上克隆（否则会把滤镜带进 Lens 调参）
            RestoreDefaultVolume();

            var baseProfile = _volume.sharedProfile != null ? _volume.sharedProfile : _volume.profile;
            if (baseProfile == null) return;

            // 克隆一个 runtime profile（避免修改 asset）
            // 注意：某些 URP 版本对 VolumeProfile 直接 Instantiate 会复用内部 VolumeComponent 引用，
            // 从而把“基础 Profile”也改掉。这里用“逐组件深拷贝”确保不会污染默认数据。
            _lensRuntimeProfile = DeepCloneVolumeProfile(baseProfile);
            if (_lensRuntimeProfile == null) return;

            _volume.sharedProfile = null;
            _volume.profile = _lensRuntimeProfile;
            _lensAdjustActive = true;

            RefreshCachedVolumeComponents();
        }

        /// <summary>
        /// 退出 Lens 调参模式（不负责恢复默认 Volume；需要由 RestoreDefaultVolume/Override 来做）。
        /// </summary>
        public void EndLensAdjustMode()
        {
            _lensAdjustActive = false;
            if (_lensRuntimeProfile != null)
            {
                try
                {
                    Object.Destroy(_lensRuntimeProfile);
                }
                catch
                {
                    // ignore
                }
                _lensRuntimeProfile = null;
            }
        }

        /// <summary>
        /// Lens：柔光（Bloom Intensity），建议范围 0~5
        /// </summary>
        public void SetLensBloom(float intensity)
        {
            BeginLensAdjustMode();
            if (_bloom == null) return;
            _bloom.active = true;
            _bloom.intensity.overrideState = true;
            _bloom.intensity.value = intensity;
        }

        /// <summary>
        /// Lens：对焦（DepthOfField FocusDistance），建议范围 0~20
        /// </summary>
        public void SetLensFocus(float focusDistance)
        {
            BeginLensAdjustMode();
            if (_dof == null) return;
            _dof.active = true;
            _dof.mode.overrideState = true;
            _dof.mode.value = DepthOfFieldMode.Bokeh;
            _dof.focusDistance.overrideState = true;
            _dof.focusDistance.value = focusDistance;
        }

        /// <summary>
        /// Lens：景深强度（DepthOfField GaussianMaxRadius），建议范围 0~1
        /// </summary>
        public void SetLensDoF(float strength01)
        {
            BeginLensAdjustMode();
            if (_dof == null) return;
            _dof.active = strength01 > 0.0001f;
            _dof.mode.overrideState = true;
            _dof.mode.value = DepthOfFieldMode.Bokeh;
            _dof.focalLength.overrideState = true;
            _dof.focalLength.value = strength01 * 200f;
        }

        /// <summary>
        /// Lens：对比度（ColorAdjustments.contrast），输入 0~3（1=默认）
        /// </summary>
        public void SetLensContrast(float contrast01To3)
        {
            BeginLensAdjustMode();
            if (_colorAdjustments == null) return;
            _colorAdjustments.active = true;
            _colorAdjustments.contrast.overrideState = true;
            // URP contrast 通常是 [-100,100]：这里做一个相对温和的映射（1->0）
            _colorAdjustments.contrast.value = (contrast01To3 - 1f) * 50f;
        }

        /// <summary>
        /// Lens：饱和度（ColorAdjustments.saturation），输入 0~3（1=默认）
        /// </summary>
        public void SetLensSaturation(float saturation01To3)
        {
            BeginLensAdjustMode();
            if (_colorAdjustments == null) return;
            _colorAdjustments.active = true;
            _colorAdjustments.saturation.overrideState = true;
            _colorAdjustments.saturation.value = (saturation01To3 - 1f) * 50f;
        }

        /// <summary>
        /// Lens：曝光（ColorAdjustments.postExposure），输入 0~2（1=默认）
        /// </summary>
        public void SetLensExposure(float exposure01To2)
        {
            BeginLensAdjustMode();
            if (_colorAdjustments == null) return;
            _colorAdjustments.active = true;
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.postExposure.value = (exposure01To2 - 1f) * 2f;
        }

        private void RefreshCachedVolumeComponents()
        {
            if (_volume == null) return;
            if (_volume.profile == null) return;

            _volume.profile.TryGet(out _bloom);
            _volume.profile.TryGet(out _dof);
            _volume.profile.TryGet(out _colorAdjustments);
        }

        /// <summary>
        /// 深拷贝 VolumeProfile（运行时安全版本）：复制 profile 以及其内部所有 VolumeComponent。
        /// 避免 shallow copy 导致修改默认/滤镜资产。
        /// </summary>
        private static VolumeProfile DeepCloneVolumeProfile(VolumeProfile src)
        {
            if (src == null) return null;

            var dst = ScriptableObject.CreateInstance<VolumeProfile>();
            dst.name = $"{src.name}_LensRuntime";

            // 尽量把 src 的组件逐个 Add 到 dst，并复制序列化字段
            // 这里用 JsonUtility 做运行时拷贝（无需 EditorUtility.CopySerialized）
            var comps = src.components;
            for (int i = 0; i < comps.Count; i++)
            {
                var c = comps[i];
                if (c == null) continue;

                var added = dst.Add(c.GetType(), overrides: false);
                if (added == null) continue;

                try
                {
                    var json = JsonUtility.ToJson(c);
                    JsonUtility.FromJsonOverwrite(json, added);
                    added.active = c.active;
                }
                catch
                {
                    // ignore
                }
            }

            return dst;
        }

        private void CacheDefaultVolumeProfileIfNeeded()
        {
            if (_hasCachedDefault) return;
            if (_volume == null) return;

            // sharedProfile 是“原始资产引用”，profile 可能是 runtime 实例
            _defaultSharedProfile = _volume.sharedProfile;
            _defaultProfile = _volume.profile;
            _hasCachedDefault = true;
        }
        
     
        
        //TODO:根据模版设置参数
        // public void SetTemplateConfig(MapTemplateConfig config)
        // {
        //     if (_bloom != null && config != null)
        //     {
        //         _bloom.scatter.value = config.scatter;
        //         _bloom.threshold.value = config.threshold;
        //         _bloom.intensity.value = config.intensity;
        //         _ratio = config.intensity * 0.2f;
        //     }
        // }

    }
}
