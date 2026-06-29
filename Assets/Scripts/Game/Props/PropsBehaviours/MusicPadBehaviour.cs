
using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using Game.Base;
using Game.Config;
using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class MusicPadBehaviour : NodeBaseBehaviour
    {
        private static MaterialPropertyBlock mpb;
        private List<Renderer> blockRenders = new List<Renderer>();
        private bool isPlaying = false;
        private float playTime = 0.8f;

		public override void OnInitByCreate()
		{
			base.OnInitByCreate();
            if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }

            var leftRd = this.transform.Find("MusicBoardSlice/left").GetComponent<Renderer>();
            var midRd = this.transform.Find("MusicBoardSlice/middle").GetComponent<Renderer>();
            var rightRd = this.transform.Find("MusicBoardSlice/right").GetComponent<Renderer>();

            blockRenders.Add(leftRd);
            blockRenders.Add(midRd);
            blockRenders.Add(rightRd);
		}

		public override void OnTrigEnter()
		{
			base.OnTrigEnter();
            // 高亮并且播放声音
            if (!isPlaying && CheckPlay())
                StartCoroutine(PlaySoundAndEffect());
		}

        IEnumerator PlaySoundAndEffect()
		{
            isPlaying = true;
            ResetEmissionColor();
            SetEmissionColor(); // 高亮
            PlaySound();
            yield return new WaitForSeconds(0.5f);
            ResetEmissionColor();
            yield return new WaitForSeconds(playTime - 0.5f);
            isPlaying = false;
        }

        void SetEmissionColor()
        {
            var musicPadComponent = entity.GetComp<MusicPadComponent>();
            for (int i = 0; i < musicPadComponent.KeyIds.Count; i++)
            {
                var keyId = musicPadComponent.KeyIds[i];
                if (i < blockRenders.Count)
                {
                    var colorStr = MusicPadConfig.GetMusicPadData(keyId)?.LightColor;
                    if (ColorUtility.TryParseHtmlString(colorStr, out Color dColor))
                    {
                        blockRenders[i].GetPropertyBlock(mpb);
                        mpb.SetColor("_EmissionColor", dColor);
                        blockRenders[i].SetPropertyBlock(mpb);
                    }
                }
            }
        }

        void PlaySound()
        {
            var musicPadComponent = entity.GetComp<MusicPadComponent>();
            for (int i = 0; i < musicPadComponent.KeyIds.Count; i++)
            {
                var keyId = musicPadComponent.KeyIds[i];
                var soundName = MusicPadConfig.GetMusicPadData(keyId)?.SoundName;
            }
        }

        bool CheckPlay()
        {
            var musicPadComponent = entity.GetComp<MusicPadComponent>();
            for (int i = 0; i < musicPadComponent.KeyIds.Count; i++)
            {
                if (musicPadComponent.KeyIds[i] != "0")
                {
                    return true;
                }
            }

            return false;
        }

        void ResetEmissionColor()
        {
            for (int i = 0; i < blockRenders.Count; i++)
            {
                blockRenders[i].GetPropertyBlock(mpb);
                mpb.SetColor("_EmissionColor", Color.clear);
                blockRenders[i].SetPropertyBlock(mpb);
            }
        }
        
        public void SetColor(int area, string keyId)
        {
            var colorStr = MusicPadConfig.GetMusicPadData(keyId)?.NormalColor;
            if (ColorUtility.TryParseHtmlString(colorStr, out Color dColor))
            {
                blockRenders[area].GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", dColor);
                blockRenders[area].SetPropertyBlock(mpb);
            }
        }    
    }
}
        
