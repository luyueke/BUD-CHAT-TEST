// @Author: YangJie
// @Description:
// @Date:  2023/08/17
// @Modify:

using System;
using System.Collections.Generic;
using System.IO;
using Basic.Extensions;
using Basic.Utils;
using GameData;
using GameData.BaseInfo;
using ICSharpCode.SharpZipLib.Zip;
using Network;
using Network.Http;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UGCAsset.Draft
{
    public sealed class SkinDraftInfo : BaseDraftInfo<SkinDraftInfo, SkinInfo> {
        protected override string SetUrl => HttpUrlDefine.SetSkin;

        protected override string CoverRemoteFolder => $"UgcClothesCover/{uid}";
        protected override string MetaDataRemoteFolder => $"UgcClothesMetadata/{uid}";
        private string ClothesKey => "clothes";
        private string ClothesRemoteFolder => $"UgcClothes/{uid}";

        private string MapMetaKey => "MapMetaKey"; //  3D 素材衣服 保存的地图元数据Key

        private string VerifyKey => "VerifyKey"; //  3D 素材衣服 添加审核图

        // public string[] photoUrls;


        public SkinDraftInfo()
        {

        }

        public SkinDraftInfo(SkinInfo clothesInfo): base(clothesInfo)
        {
            // 初始化草稿基础信息
            SetUploadRemoteInfo(ClothesKey, clothesInfo.clothesUrl, ClothesRemoteFolder);
        }

        // public override SkinInfo ToUgcInfo()
        // {
        //     var ugcInfo = base.ToUgcInfo();
        //     ugcInfo.imgs = photoUrls;
        //     return ugcInfo;
        // }

        public void SetMetaData(string metaData)
        {
            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            var localMetaDataUrl = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".json");
            File.WriteAllText(localMetaDataUrl, metaData);
            SetUploadLocalInfo(MetaDataKey, localMetaDataUrl, MetaDataRemoteFolder);
        }


        protected override void RefreshUploadInfo(DraftUploadInfo draftUploadInfo) {
            base.RefreshUploadInfo(draftUploadInfo);
            if (draftUploadInfo.draftName == ClothesKey && baseInfo != null) {
                baseInfo.clothesUrl = draftUploadInfo.remoteUrl;
            } else if (draftUploadInfo.draftName == MapMetaKey && baseInfo != null) {
                baseInfo.mapUrl = draftUploadInfo.remoteUrl;
            } else if (draftUploadInfo.draftName == VerifyKey && baseInfo != null) {
                baseInfo.verifyUrl = draftUploadInfo.remoteUrl;
            }
        }


        public void SetMapMetaData(byte[] metaData) {
            var localMetaDataUrl = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".bytes");
            File.WriteAllBytes(localMetaDataUrl, metaData);
            SetUploadLocalInfo(MapMetaKey, localMetaDataUrl, MetaDataRemoteFolder);
        }

        public string GetMapUrl() {
            var uploadInfo = draftUploadInfos.Find(info => info.draftName == MapMetaKey);
            return uploadInfo?.draftPath ?? uploadInfo?.remoteUrl;
        }


        public void SetVerify(byte[] verifyData) {
            if (verifyData == null) {
                LoggerUtils.LogError("SetVerify Error: verifyData == null" );
                return;
            }

            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            var localVerifyPath = Path.Combine(GetDraftCacheFolder(), GetDraftFileName()  + ".png");
            if (File.Exists(localVerifyPath)) {
                return;
            }
            File.WriteAllBytes(localVerifyPath, verifyData);
            SetUploadLocalInfo(VerifyKey, localVerifyPath, CoverRemoteFolder);
        }

        public void SetClothesSaveTexture2D(Texture2D bigTexture)
        {
            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            var bytes =bigTexture.EncodeToPNG();
            var filePath = Path.Combine(GetDraftCacheFolder(), GetDraftFileName()  + ".png");
            File.WriteAllBytes(filePath, bytes);
            SetUploadLocalInfo(ClothesKey, filePath, ClothesRemoteFolder);
        }
    }
}
