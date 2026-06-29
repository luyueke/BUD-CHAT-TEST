using System;
using Es;
using Game.Avatar;
using Game.Props.PropsComponents;
using GameData.PgcData;
using Message;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIYandereComputerBehaivour: AIPropBaseBehaviour
    {
        private string emoteId = "40200403";
        private Vector3 pos;
        private Vector3 rot;
        private string nodeName;
        private Func<bool, bool> _checkStartGame;
        private Action onPlayComputer;
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            var dataComp = this.entity.GetComp<AIYandereCommonComponent>();
            pos = dataComp.Pos;
            rot = dataComp.Rot;
            MessageHelper.AddListener<string>(MessageName.AICancelEmote, CancelEmote);
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener<string>(MessageName.AICancelEmote, CancelEmote);
        }


        public void CancelEmoteOnUI()
        {
            CancelEmote("");
        }
        public void CancelEmote(string emoId)
        {
            IsCanClick = true;
            AvatarController.Inst.SelfStateController.ExitState(PlayerState.SingleEmote);
        }


        public void SetFunctions(Func<bool, bool> func)
        {
            this._checkStartGame = func;
        }

        public void SetPlayComputer(Action play)
        {
            onPlayComputer = play;
        }

        public override void OnTouchClick()
        {
            if (IsCanClick && _checkStartGame(false))
            {
                IsCanClick = false;
                var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteId);
                if (emoAniDataList.Count > 0)
                {
                    if (AvatarController.Inst.SelfStateController.CanEnterState(PlayerState.SingleEmote))
                    {
                        int random = emoAniDataList[0].randomCount;
                        int randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
                        AvatarController.Inst.SelfController.Motor.SetPositionAndRotation(pos,Quaternion.Euler(rot));
                        AvatarController.Inst.SelfStateController.EnterState(PlayerState.SingleEmote, emoteId, randomResult);
                        TimerManager.Inst.RunOnce("PlayComputer", 2, PlayComputer);
                    }
                }
            }
        }

        private void PlayComputer()
        {
            onPlayComputer?.Invoke();
        }

    }
}