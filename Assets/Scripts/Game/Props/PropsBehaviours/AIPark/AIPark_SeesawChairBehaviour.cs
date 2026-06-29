using AIGame.Base;
using Game.Avatar;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    /// <summary>
    /// s11交互道具：跷跷板
    /// 这个道具需要注意的是有左右两个交互点位，可以两人一起玩
    /// </summary>
    public class AIPark_SeesawChairBehaviour : AIPark_BasePropBehaviour
    {
  

        private Transform node2, node1;

        private Vector3 pos;

        private Vector3 rot;

        private bool isPlaying;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();

            var data = entity.GetComp<AIGameCommonComponent>();
            if (data != null)
            {
                pos = data.Pos;
                rot = data.Rot;
            }
            IsCanClick = false;
        }


        protected override void Play()
        {
          

        }

    


        protected override void PlayWithBuddy()
        {
            // 实现跷跷板特有的双人游玩逻辑
        }

        // 可以重写音效方法，使用特定的音效
        protected override void StartPlaySound(string soundName)
        {
        }

        protected override void EndPlaySound(string soundName)
        {
        }

        public override void OnPropReset()
        {
            if (isPlaying == false)
                return;
            isPlaying = false;

            // IsCanClick = true;

            EndPlaySound("seesaw_sound");
        }
    }
}