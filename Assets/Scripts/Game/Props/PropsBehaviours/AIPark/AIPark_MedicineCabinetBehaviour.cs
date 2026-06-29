using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using Es;
using Game.Avatar;
using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIPark_MedicineCabinetBehaviour : AIPark_BaseDoorBehaviour
    {
        public Vector3 pos;
        public Vector3 rot;
        private string curEmoteId = "40200466";

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

        // 可以在这里添加特定于配药的功能
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

        protected override void PlayOpenSound()
        {
            AIGameSoundUtils.Inst.PlaySound(AIParkConfig.SOUND_MED_OPEN_DOOR, this.gameObject);
        }
        protected override void PlayCloseSound()
        {
            AIGameSoundUtils.Inst.PlaySound(AIParkConfig.SOUND_MED_CLOSE_DOOR, this.gameObject);
        }
    }
}
