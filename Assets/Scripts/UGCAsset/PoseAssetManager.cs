using System;
using GameData.BaseInfo;
using GameData.UGCData;
using Network.Http;
using UGCAsset.Draft;

namespace UGCAsset
{
    public class PoseAssetManager: UGCAssetManager<PoseDraftInfo, PoseInfo, PoseAssetManager>
    {
        protected override string GetAssetInfoUrl => HttpUrlDefine.GetPoseInfo;
        
        public void CreatePoseInServer(PoseInfo info, Action<bool> callBack = null) {
            var draftInfo = GetOrCreateDraftInfo(info);
            draftInfo?.CreateDraftToServer((tmpInfo, isSuccess) => {
                if (isSuccess) {
                    tmpInfo.CopyTo(info);
                }
                callBack?.Invoke(isSuccess);
            });
        }
    }
}