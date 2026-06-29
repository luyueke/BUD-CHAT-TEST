using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;

namespace UI {
    public class UGCAnimEditData : BasePropEditData
    {
        public UgcAnimDraftInfo draftInfo;
        public override UgcBaseInfo GetInfo()
        {
            return draftInfo.baseInfo;
        }

        public AnimInfo GetAnimInfo()
        {
            return draftInfo.baseInfo;
        }
    }
}
