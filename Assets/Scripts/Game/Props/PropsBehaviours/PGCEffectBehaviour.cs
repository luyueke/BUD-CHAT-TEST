
using System;
using Game.Audio;
using Game.Base;
using Game.ECS;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Scene.ModeController;
using Game.Utils;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class PGCEffectBehaviour : ActorNodeBehaviour
    {
        private Color[] originColor;
        private static MaterialPropertyBlock mpb;
        
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }
        }

        private void OnEnable()
        {
            if (GlobalNodeManager.Inst.Get<PGCEffectManager>().IsGuest() || GlobalNodeManager.Inst.Get<PGCEffectManager>().IsPlay())
            {
                //TODO:Loading加载时，不播放音效
                // if(GameManager.Inst.loadingPageIsClosed)
                PlaySound(true);
            }
            
        }

        private void OnDisable()
        {
            PlaySound(false);
        }

        private void OnDestroy()
        {
            AkSoundManager.Inst.StopAll(gameObject);
        }

        public override void HighLight(bool isHigh)
        {
            base.HighLight(isHigh);
            Color srcColor = entity.GetComp<PGCEffectComponent>().Color;
            if (isHigh)
            {
                var hightColor = GamePropUtils.GetHighlightColor(srcColor);
                SetColor(hightColor);
            }
            else
            {
                SetColor(srcColor);
            }
        }
        
        
        /// <summary>
        /// 设置颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetColor(Color color)
        {
            var Renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < Renderers.Length; i++)
            {
                var render = Renderers[i];
                render.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", color);
                render.SetPropertyBlock(mpb);
            }
        }

        public void PlaySound(bool isPlay)
        {
            if (entity == null) return;
            if (!GlobalNodeManager.HasInstance) return;
            var propId = entity.GetComp<GameObjectComponent>()?.PropId;
            if (string.IsNullOrEmpty(propId))
            {
                return;
            }

            var soundName = GlobalNodeManager.Inst.Get<PGCEffectManager>()?.GetConfigDataById(propId)?.soundName;
            var playSound = entity.GetComp<PGCEffectComponent>()?.PlaySound ?? 0;
            
            if (string.IsNullOrEmpty(soundName))
            {
                return;
            }
            
            if (!isPlay)
            {
                StopEffectSound(soundName);
            }
            else if (playSound == 1)
            {
                PlayEffectSound(soundName);
            }
        }

        private void StopEffectSound(string configName)
        {
            if (string.IsNullOrEmpty(configName)) return;
            string eventName = "Stop_" + configName;
            AkSoundManager.Inst.PostEvent(eventName, gameObject);
        }

        private void PlayEffectSound(string configName)
        {
            if (string.IsNullOrEmpty(configName)) return;
            string eventName = "Play_" + configName;
            AkSoundManager.Inst.PostEvent(eventName, gameObject);
        }

        public void PlayParticleEffect()
        {
            var particles = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                particle.Clear();
                particle.Play();
            }
        }
    }
}
        
