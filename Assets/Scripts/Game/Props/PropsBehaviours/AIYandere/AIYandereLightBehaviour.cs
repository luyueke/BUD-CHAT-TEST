using System;
using System.Collections;
using AIGame.Base;
using Game.Audio;
using Game.Props.PropsComponents;
using Pb.Base;
using UnityEngine;
using UnityEngine.Video;

namespace Game.Props.PropsBehaviours
{
    public class AIYandereLightBehaviour: AIPropBaseBehaviour
    {
        private string nodeName;
        private Light tabelLight;
        private bool isPlay = true;
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            var dataComp = this.entity.GetComp<AIYandereNodeComponent>();
            nodeName = dataComp.nodeName;
            var node = GameObject.Find(nodeName);
            var nodeLight = GameObject.Find(nodeName + "_light");
            tabelLight =nodeLight.GetComponent<Light>();
        }

        protected Func<bool, bool> _checkStartGame;

        public void SetFunctions(Func<bool, bool> func)
        {
            this._checkStartGame = func;
        }
        
        public override void OnTouchClick()
        {
            if (_checkStartGame(false))
            {
                isPlay = !isPlay;
                tabelLight.enabled = isPlay;
                AIGameSoundUtils.Inst.PlaySound(isPlay?YandereConfig.PlayOnLight:YandereConfig.PlayOffLight);
            }
        }
    }
}