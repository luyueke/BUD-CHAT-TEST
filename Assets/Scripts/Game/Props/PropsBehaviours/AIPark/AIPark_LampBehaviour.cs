using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Scene.EnterModelController;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIPark_LampBehaviour : AIPark_BaseInteractiveBehaviour
    {
        public Light tableLight;
        private bool isPlay = true;

        // 可以在这里添加特定于灯的功能

        public override void OnInteractive()
        {
            base.OnInteractive();
            // 灯的交互逻辑
            isPlay = !isPlay;
            tableLight.enabled = isPlay;
            AIGameSoundUtils.Inst.PlaySound(isPlay ? AIParkConfig.SOUND_LIGHT_OPEN : AIParkConfig.SOUND_LIGHT_CLOSE, this.gameObject);
        }
    }
} 