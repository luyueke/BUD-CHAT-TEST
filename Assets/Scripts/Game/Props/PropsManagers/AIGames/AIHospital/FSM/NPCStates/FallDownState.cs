using Game.Avatar;
using Game.Props;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    public class FallDownState : AIChapterState
    {
        public FallDownState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data = null) : base(character, data)
        {
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[FallDownState] {_character.GetNpcName()} 进入到底状态");

            var selfStateCtr = AvatarController.Inst.SelfStateController;
            selfStateCtr.EnterState(PlayerState.DoubleEmote, "40300468", true, AccountDataManager.Inst.Uid);
            _character._npcStateController.EnterState(PlayerState.DoubleEmote, "40300468", false, AccountDataManager.Inst.Uid);
            _character._npcStateController.EnterState(PlayerState.SingleEmote, "40200488", 0);

            _character.LookAtPlayer();
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}