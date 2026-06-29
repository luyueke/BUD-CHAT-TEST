using GameData.Base;
using GameData.BaseInfo;

namespace UI {
    public class ActorEditData : UGCBaseEditData
    {
        public OCTheatreAvatarInfo actorInfo;

        public override UgcBaseInfo GetInfo()
        {
            return actorInfo;
        }
    }
}
