using System;
using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using Es;
using Game.Avatar;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Scene.EnterModelController;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIHospital_FileCabinetBehaviour : AIHospital_BaseDoorBehaviour
    {
        public Vector3 pos;
        public Vector3 rot;
        private string curEmoteId = "40200465";

        public override bool AllowClickInEmoteLink => false;
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

        // 可以在这里添加特定于文件柜的功能
        public override void OnTouchClick()
        {
            base.OnTouchClick();

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
