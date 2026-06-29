using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;

namespace UI {
    public class ToneEditData : BasePropEditData
    {
        public ToneInfo draftInfo;

        public ToneInfo GetToneInfo()
        {
            return draftInfo;
        }

        public override UgcBaseInfo GetInfo()
        {
            return draftInfo;
        }
    }
}
