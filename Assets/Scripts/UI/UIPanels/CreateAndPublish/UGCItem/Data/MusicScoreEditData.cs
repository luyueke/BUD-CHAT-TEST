using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;

namespace UI {
    public class MusicScoreEditData : BasePropEditData
    {
        public MusicScoreDraftInfo draftInfo;
        public override UgcBaseInfo GetInfo()
        {
            return draftInfo.baseInfo;
        }

        public MusicScoreInfo GetMusicScoreInfo()
        {
            return draftInfo.baseInfo;
        }
    }
}
