using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;

namespace UI {
    public class InstrumentEditData : BasePropEditData
    {
        public SkinActionBaseDraftInfo skinActionDraftInfo;
        public override UgcBaseInfo GetInfo()
        {
            return skinActionDraftInfo._skinInfo;
        }

        public SkinInfo GetSkinInfo()
        {
            return skinActionDraftInfo._skinInfo;
        }
        
        public SkinActionInfo GetSkinActionInfo()
        {
            return skinActionDraftInfo._skinActionInfo;
        }
        
        public InstrumentInfo GetInstrumentInfo()
        {
            return skinActionDraftInfo._skinActionInfo.instrumentInfo;
        }

        public ToneInfo GetToneInfo()
        {
            return skinActionDraftInfo._skinActionInfo.instrumentInfo.toneInfo;
        }

        public void SetToneInfo(ToneInfo toneInfo)
        {
            skinActionDraftInfo._skinActionInfo.instrumentInfo.toneInfo = toneInfo;
        }

        public void SetAnimId(string moveId)
        {
            skinActionDraftInfo._skinActionInfo.instrumentInfo.moveId = moveId;
        }

        public void SetInstrumentDetailInfo(InstrumentDetailInfo info)
        {
            skinActionDraftInfo._skinActionInfo.instrumentInfo.animDetailInfo = info;
        }

        public InstrumentDetailInfo GetInstrumentDetailInfo()
        {
            return skinActionDraftInfo._skinActionInfo.instrumentInfo?.animDetailInfo;
        }
    }
}
