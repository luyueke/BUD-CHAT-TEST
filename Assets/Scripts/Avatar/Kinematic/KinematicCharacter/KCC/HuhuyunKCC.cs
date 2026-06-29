namespace Game.Avatar.Kinematic.KinematicCharacter.KCC {
    public class HuhuyunKCC : BaseSpecialKCC {
        public HuhuyunKCC(string uid, bool isSelf) : base(uid, isSelf) {
        }

        protected override void AddPGCRunStateEvents(AnimationEventHandler handler) {
            if (handler == null) {
                return;
            }
            handler.AddStateEvent("run",$"{GetType().Name}_playFootSound1", 0.4f, OnAnimationEvent);
            handler.AddStateEvent("run",$"{GetType().Name}_playFootSound2", 1.3f, OnAnimationEvent);
            handler.AddStateEvent("fast_run",$"{GetType().Name}_playFootSound1", 0.1f, OnAnimationEvent);
            handler.AddStateEvent("fast_run",$"{GetType().Name}_playFootSound2", 0.2f, OnAnimationEvent);
        }

        protected override void RemovePGCRunStateEvents(AnimationEventHandler handler) {
            if (handler == null) {
                return;
            }
            handler.RemoveStateEvent("run",$"{GetType().Name}_playFootSound1");
            handler.RemoveStateEvent("run",$"{GetType().Name}_playFootSound2");
            handler.RemoveStateEvent("fast_run",$"{GetType().Name}_playFootSound1");
            handler.RemoveStateEvent("fast_run",$"{GetType().Name}_playFootSound2");
        }
    }
}
