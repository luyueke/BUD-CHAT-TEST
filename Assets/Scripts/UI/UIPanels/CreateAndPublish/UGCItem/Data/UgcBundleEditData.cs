using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;

namespace UI {
    public class UgcBundleEditData : UGCBaseEditData
    {
        public UgcBundleDraftInfo draftInfo;
        public CurrencyType CurrencyType;
        public int OriginPrice;
        public override UgcBaseInfo GetInfo()
        {
            return draftInfo.baseInfo;
        }

        public SkinInfo GetUgcBundleInfo()
        {
            return draftInfo.baseInfo;
        }
    }
}
