using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;

namespace UI {
    public class MapEditData : UGCBaseEditData
    {
        public MapDraftInfo draftInfo;
        public bool isPublish;
        public string overwriteId;
        public bool isCondition = true;
        public override UgcBaseInfo GetInfo()
        {
            return draftInfo.baseInfo;
        }

        public MapInfo GetMapInfo()
        {
            return draftInfo.baseInfo;
        }
    }
}
