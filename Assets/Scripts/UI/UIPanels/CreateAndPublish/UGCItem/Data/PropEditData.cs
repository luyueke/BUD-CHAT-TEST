using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;

namespace UI {
    public class PropEditData : BasePropEditData
    {
        public PropDraftInfo draftInfo;

        public override UgcBaseInfo GetInfo()
        {
            return draftInfo.baseInfo;
        }

        public PropInfo GetPropInfo()
        {
            return draftInfo.baseInfo;
        }
    }
}
