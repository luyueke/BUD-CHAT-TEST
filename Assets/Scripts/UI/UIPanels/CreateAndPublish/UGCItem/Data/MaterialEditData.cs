using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;

namespace UI {
    public class MaterialEditData : UGCBaseEditData
    {
        public MaterialDraftInfo draftInfo;

        public override UgcBaseInfo GetInfo()
        {
            return draftInfo.baseInfo;
        }

        public MaterialInfo GetMaterialInfo()
        {
            return draftInfo.baseInfo;
        }
    }
}
