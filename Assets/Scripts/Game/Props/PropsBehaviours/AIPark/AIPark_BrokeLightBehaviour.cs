using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIPark_BrokeLightBehaviour : AIPropBaseBehaviour
    {
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();

            //InvokeRepeating("PlaySound", 6f, 10);
            PlaySound();
        }

        private void PlaySound()
        {
            AIGameSoundUtils.Inst.PlaySound(AIParkConfig.BROKE_LIGHT_LOOP, this.gameObject);
        }

        public override void OnReset()
        {
            base.OnReset();
            AIGameSoundUtils.Inst.StopSound(AIParkConfig.BROKE_LIGHT_LOOP, this.gameObject);
        }

        public void OnDestroy()
        {
            AIGameSoundUtils.Inst.StopSound(AIParkConfig.BROKE_LIGHT_LOOP, this.gameObject);
        }
    }
}
