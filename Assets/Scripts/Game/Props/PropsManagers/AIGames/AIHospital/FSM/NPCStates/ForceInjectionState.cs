using Es;
using Game.Avatar;
using GameData.PgcData;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    public class ForceInjectionState : AIChapterState
    {
        private bool isPgcEnter = true;
        private string _curPlayEmoteId;
        private HospitalNpcTransData _transData = new HospitalNpcTransData()
        {
            pos = new Vector3(-16.9f, 1.41f, -26.2f),
            rot =  new Vector3(0,90,0),
        };
        public ForceInjectionState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(
            character, data)
        {
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[ForceInjectionState] {_character.GetNpcName()} 进入强制打针的状态");
            AvatarController.Inst.SelfController.Motor.SetPositionAndRotation(_transData.pos, Quaternion.Euler(_transData.rot));
            StartPlayNpcEmote("40300487", true);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[ForceInjectionState] {_character.GetNpcName()} 退出强制打针的状态");

        }

        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="emoteId"></param>
        /// <param name="bSelfActive">是否玩家主动进入状态</param>
        public void StartPlayNpcEmote(string emoteId, bool bSelfActive = false)
        {
            _curPlayEmoteId = emoteId;
            var emoteData = DataTables.GetEmoUIConfigList().Find((emoData) => emoData.pgcId == emoteId);
            if (emoteData.emoType == (int)EmoteSubType.Double || emoteData.emoType == (int)EmoteSubType.DoubleLoop)
            {
                var selfStateCtr = AvatarController.Inst.SelfStateController;
                var npcStateCtr = _character._npcStateController;
                npcStateCtr.EnterState(PlayerState.DoubleEmote, emoteId, !bSelfActive, AccountDataManager.Inst.Uid);
                selfStateCtr.EnterState(PlayerState.DoubleEmote, emoteId, bSelfActive, npcStateCtr.PlayerID);
            }
            else
            {
                if (bSelfActive)
                {

                    AvatarController.Inst.SelfStateController.EnterState(PlayerState.SingleEmote, emoteId, 0);
                }
                else
                    _character.PlayAnim(emoteId);
            }
        }
    }
}
