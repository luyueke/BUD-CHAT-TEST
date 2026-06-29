using Game.Props.PropsComponents;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;

namespace Game.Props.PropsBehaviours
{
    public class AIHospital_RoomDoorBehaviour : AIHospital_BaseDoorBehaviour
    {
        public LocationType DoorLocation = LocationType.None;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            var comData = entity.GetComp<AIGameCommonComponent>();
            if (comData != null)
            {
                DoorLocation = (LocationType)comData.Tag;
            }
        }
    }
}
