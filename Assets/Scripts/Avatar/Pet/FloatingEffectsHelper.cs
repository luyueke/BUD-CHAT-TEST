using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Pet
{
    /// <summary>
    /// 漂浮视觉效果辅助工具
    /// </summary>
    public static class FloatingEffectsHelper
    {
        /// <summary>
        /// 为漂浮宠物添加粒子效果
        /// </summary>
        public static void AddFloatingParticles(GameObject targetObject)
        {
            if (targetObject == null) return;
            
            // 如果已有粒子系统，先移除
            ParticleSystem[] existingParticles = targetObject.GetComponentsInChildren<ParticleSystem>();
            foreach (var particle in existingParticles)
            {
                if (particle.gameObject.name.Contains("Floating"))
                {
                    GameObject.Destroy(particle.gameObject);
                }
            }
            
            // 创建一个粒子效果子对象
            GameObject particleObj = new GameObject("FloatingParticles");
            particleObj.transform.SetParent(targetObject.transform);
            particleObj.transform.localPosition = Vector3.zero;
            particleObj.transform.localRotation = Quaternion.identity;
            
            // 添加粒子系统组件
            ParticleSystem particleSys = particleObj.AddComponent<ParticleSystem>();
            
            // 配置粒子系统 - 主模块
            var main = particleSys.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 0.2f;
            main.startSize = 0.05f;
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            // 发射模块
            var emission = particleSys.emission;
            emission.rateOverTime = 15;
            emission.enabled = true;
            
            // 形状模块
            var shape = particleSys.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            shape.radiusThickness = 0.2f;
            
            // 颜色模块
            var colorOverLifetime = particleSys.colorOverLifetime;
            colorOverLifetime.enabled = true;
            
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.8f, 0.9f, 1f, 1f), 0.0f),
                    new GradientColorKey(new Color(0.5f, 0.8f, 1f, 1f), 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(0.7f, 0.3f),
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            colorOverLifetime.color = gradient;
            
            // 大小模块
            var sizeOverLifetime = particleSys.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, new AnimationCurve(
                new Keyframe(0, 0.2f),
                new Keyframe(0.5f, 1.0f),
                new Keyframe(1, 0)
            ));
            
            // 添加粒子系统渲染器
            ParticleSystemRenderer renderer = particleSys.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.SetColor("_Color", new Color(0.7f, 0.9f, 1f, 0.5f));
            renderer.sortingOrder = 1;
        }
        
        /// <summary>
        /// 为漂浮宠物添加光源效果
        /// </summary>
        public static void AddFloatingLight(GameObject targetObject, Color? lightColor = null)
        {
            if (targetObject == null) return;
            
            
            // 创建一个光源子对象
            GameObject lightObj = new GameObject("FloatingLight");
            lightObj.transform.SetParent(targetObject.transform);
            lightObj.transform.localPosition = Vector3.zero;
            
            // 添加光源组件
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = lightColor ?? new Color(0.7f, 0.9f, 1f);
            light.intensity = 0.7f;
            light.range = 2f;
            
            // 设置光照衰减
            light.shadows = LightShadows.None; // 禁用阴影以提高性能
            
            // 添加呼吸动画脚本
            PulsatingLight pulsating = lightObj.AddComponent<PulsatingLight>();
            pulsating.minIntensity = 0.5f;
            pulsating.maxIntensity = 1.0f;
            pulsating.pulseSpeed = 1.0f;
        }
        
        /// <summary>
        /// 为漂浮宠物添加拖尾效果
        /// </summary>
        public static void AddFloatingTrail(GameObject targetObject)
        {
            if (targetObject == null) return;
            
            // 如果已有拖尾，先移除
            
            // 创建一个拖尾子对象
            GameObject trailObj = new GameObject("FloatingTrail");
            trailObj.transform.SetParent(targetObject.transform);
            trailObj.transform.localPosition = new Vector3(0, 0, -0.2f);
            
            // 添加拖尾渲染器
            TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
            trail.time = 0.5f; // 拖尾持续时间
            trail.widthMultiplier = 0.2f; // 拖尾宽度
            trail.startWidth = 0.2f;
            trail.endWidth = 0.0f;
            trail.material = new Material(Shader.Find("Particles/Standard Unlit"));
            trail.material.SetColor("_Color", new Color(0.7f, 0.9f, 1f, 0.3f));
            
            // 设置渐变颜色
            Gradient trailGradient = new Gradient();
            trailGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.7f, 0.9f, 1f), 0.0f),
                    new GradientColorKey(new Color(0.5f, 0.8f, 1f), 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.5f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            trail.colorGradient = trailGradient;
            
            // 拖尾默认不激活
            trail.emitting = false;
        }
        
        /// <summary>
        /// 应用所有漂浮效果
        /// </summary>
        public static void ApplyAllFloatingEffects(GameObject targetObject)
        {
            AddFloatingParticles(targetObject);
            AddFloatingLight(targetObject);
            AddFloatingTrail(targetObject);
        }
    }
    
    /// <summary>
    /// 光源呼吸效果组件
    /// </summary>
    public class PulsatingLight : MonoBehaviour
    {
        public float minIntensity = 0.5f;
        public float maxIntensity = 1.5f;
        public float pulseSpeed = 1.0f;
        
        private Light lightComponent;
        private float targetIntensity;
        
        private void Start()
        {
            lightComponent = GetComponent<Light>();
            if (lightComponent == null)
            {
                Destroy(this);
                return;
            }
            
            targetIntensity = lightComponent.intensity;
        }
        
        private void Update()
        {
            if (lightComponent != null)
            {
                // 使用正弦波计算强度
                float intensity = Mathf.Lerp(minIntensity, maxIntensity, 
                    (Mathf.Sin(Time.time * pulseSpeed) + 1) / 2);
                
                lightComponent.intensity = intensity;
            }
        }
    }
} 