using System;
using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using Es;
using Game.Avatar;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Scene.EnterModelController;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIPark_MedicineBehaviour : AIPark_BaseInteractiveBehaviour
    {
        public Vector3 pos;
        public Vector3 rot;
        private string curEmoteId = "40100463";
        private Action<YandereAreaType, int> _sendPosAct;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            var comData = entity.GetComp<AIGameCommonComponent>();
            if (comData != null)
            {
                pos = comData.Pos;
                rot = comData.Rot;
            }
        }

        public override void OnTouchClick()
        {
            base.OnTouchClick();
        }

        public override void OnInteractive()
        {
            base.OnInteractive();
            // 药品的交互逻辑

            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == curEmoteId);
            if (emoAniDataList.Count > 0)
            {
                if (AvatarController.Inst.SelfStateController.CanEnterState(PlayerState.SingleEmote))
                {
                    int random = emoAniDataList[0].randomCount;
                    int randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
                    AvatarController.Inst.SelfController.Motor.SetPositionAndRotation(pos, Quaternion.Euler(rot));
                    AvatarController.Inst.SelfStateController.EnterState(PlayerState.SingleEmote, curEmoteId, randomResult);
                }
            }
        }
    }
} 