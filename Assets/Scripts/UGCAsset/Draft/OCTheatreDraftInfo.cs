using GameData.BaseInfo;
using Network.Http;

namespace UGCAsset.Draft {
    public class OCTheatreDraftInfo : BaseDraftInfo<OCTheatreDraftInfo, OCTheatreInfo>
    {
        public OCTheatreDraftInfo() { }
        public OCTheatreDraftInfo(OCTheatreInfo info) : base(info) { }

        protected override string SetUrl => HttpUrlDefine.TheatreSet;

        protected override string CoverRemoteFolder => $"UgcOCTheatreCover/{uid}";
        protected override string MetaDataRemoteFolder => $"UgcOCTheatreMetadata/{uid}";

        public void SyncCoverUrl(string cdnUrl) =>
            SetUploadRemoteInfo(CoverKey, cdnUrl, CoverRemoteFolder);
    }
}