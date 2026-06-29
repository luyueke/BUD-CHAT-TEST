// @Author: YangJie
// @Description: 素材本地草稿信息
// @Date:  2023/08/29
// @Modify:

using System.IO;
using GameData.BaseInfo;
using Network;
using Network.Http;
using System;
using Basic.Utils;
using GameData.Base;
using GameData.Gashapon;
using UIAgent;
using Game.Config;

namespace UGCAsset.Draft {
    public class PropDraftInfo : BaseDraftInfo<PropDraftInfo, PropInfo> {
        protected override string SetUrl => HttpUrlDefine.setProp;
        protected sealed override string CoverRemoteFolder => $"UgcItemCover/{uid}";
        protected sealed override string MetaDataRemoteFolder => $"UgcItemMetadata/{uid}";

        private string MapMetaKey => "MapMetaKey"; // 保存的地图元数据Key

        private string VerifyKey => "VerifyKey";

        public PropDraftInfo() {
        }

        public PropDraftInfo(PropInfo propInfo) : base(propInfo) {
            // 地图的信息
            SetUploadRemoteInfo(MapMetaKey, propInfo.mapUrl, MetaDataRemoteFolder);

            if (propInfo.paymentInfo == null) {
                propInfo.paymentInfo = new PaymentInfo() {
                    currencyType = CurrencyType.PinkCoin,
                    price = 0,
                };
            }
        }

        protected override void RefreshUploadInfo(DraftUploadInfo draftUploadInfo) {
            base.RefreshUploadInfo(draftUploadInfo);
            if (draftUploadInfo.draftName == MapMetaKey && baseInfo != null) {
                baseInfo.mapUrl = draftUploadInfo.remoteUrl;
            } else if (draftUploadInfo.draftName == VerifyKey && baseInfo != null) {
                baseInfo.verifyUrl = draftUploadInfo.remoteUrl;
            }
        }


        public void SetMapMetaData(byte[] metaData) {
            draftVersion++;
            var localMetaDataUrl = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".bytes");
            File.WriteAllBytes(localMetaDataUrl, metaData);
            SetUploadLocalInfo(MapMetaKey, localMetaDataUrl, MetaDataRemoteFolder);
        }


        /// <summary>
        /// 该接口合并了本地和远程的元数据，可能返回本地，也可能返回远端
        /// </summary>
        /// <returns></returns>
        public string GetMapUrl() {
            var uploadInfo = draftUploadInfos.Find(info => info.draftName == MapMetaKey);
            if (uploadInfo != null) {
                if (!string.IsNullOrEmpty(uploadInfo.draftPath) && File.Exists(uploadInfo.draftPath)) {
                    return uploadInfo.draftPath;
                }
                return uploadInfo.remoteUrl;
            }
            return null;
        }


        public void SetVerify(byte[] verifyData) {
            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            var localVerifyPath = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".png");
            if (File.Exists(localVerifyPath)) {
                return;
            }
            File.WriteAllBytes(localVerifyPath, verifyData);
            SetUploadLocalInfo(VerifyKey, localVerifyPath, CoverRemoteFolder);
        }

        public string GetVerifyRemoteUrl() {
            return draftUploadInfos.Find(info => info.draftName == VerifyKey)?.remoteUrl;
        }


        public override void EditDraftToServer(Action<PropDraftInfo, bool> callBack = null) {
            if (draftId == GameConsts.ScenePropDraftId) {
                LoggerUtils.LogError("本地草稿 需要先创建");
                callBack?.Invoke(this, false);
            } else {
                base.EditDraftToServer(callBack);
            }
        }


        public override void PublishDraftToServer(Action<PropInfo, bool> callBack) {

            if (draftId == GameConsts.ScenePropDraftId) {
                // 本地草稿、调用立即发布接口
                var ugcInfo = ToUgcInfo();
                ugcInfo.id = null;
                var req = new UGCSetRequest(ugcInfo, UGCOperationType.ImmediatePublish);
                NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
                    callBack(draftListItem?.Get<PropInfo>(), true);
                }, rsp => {
                    LoggerUtils.LogError($"发布失败 [{ugcInfo.id}]:" + rsp.rmsg);
                    callBack(null, false);
                });
                return;
            }
            base.PublishDraftToServer(callBack);
        }
    }
}
