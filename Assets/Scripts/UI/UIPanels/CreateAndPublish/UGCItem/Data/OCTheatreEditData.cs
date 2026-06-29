using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;

namespace UI {
    public class OCTheatreEditData : UGCBaseEditData
    {
        public OCTheatreDraftInfo draftInfo;

        public override UgcBaseInfo GetInfo()
        {
            return draftInfo.baseInfo;
        }

        public OCTheatreInfo GetOCTheatreInfo()
        {
            return draftInfo.baseInfo;
        }
    }
}