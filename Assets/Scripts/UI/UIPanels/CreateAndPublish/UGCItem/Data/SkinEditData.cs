using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;

namespace UI {
    public class SkinEditData : BasePropEditData
    {
        public SkinDraftInfo draftInfo;
        public override UgcBaseInfo GetInfo()
        {
            return draftInfo.baseInfo;
        }

        public SkinInfo GetSkinInfo()
        {
            return draftInfo.baseInfo;
        }
    }
}
