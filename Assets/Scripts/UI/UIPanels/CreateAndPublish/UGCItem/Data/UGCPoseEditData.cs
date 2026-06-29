using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;

namespace UI {
    public class UGCPoseEditData : BasePropEditData
    {
        public PoseDraftInfo draftInfo;
        public override UgcBaseInfo GetInfo()
        {
            return draftInfo.baseInfo;
        }

        public PoseInfo GetPoseInfo()
        {
            return draftInfo.baseInfo;
        }
    }
}

