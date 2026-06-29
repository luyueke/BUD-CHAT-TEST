// @Author: YangJie
// @Description:
// @Date:  2023/08/17
// @Modify:

using System.Linq;
using Game.COSXML;
using Es;
using Game.Config;
using GameData.BaseInfo;
using Network.Http;
using UGCAsset.Draft;
using UnityEngine;

namespace UGCAsset {
    public class SkinAssetManager : UGCAssetManager<SkinDraftInfo, SkinInfo, SkinAssetManager> {
        protected override string GetAssetInfoUrl => HttpUrlDefine.GetClothesInfo;

        public override byte[] GetTemplateMetaData(string templateId = null) {
            ClothesTemplate template = null;
            if (string.IsNullOrEmpty(templateId)) {
                template = DataTables.GetClothesTemplateList().FirstOrDefault();
            } else {
                template = DataTables.GetClothesTemplate(templateId);
            }
            var metaDataUrl = template?.MetaDataUrl;
            if (string.IsNullOrEmpty(metaDataUrl)) return null;
            var metaDataRequest = xasset.Asset.Load(GameConsts.ClothesAssetDir + metaDataUrl, typeof(TextAsset));
            if (metaDataRequest == null) return null;
            var metaData = metaDataRequest.asset as TextAsset;
            if (metaData == null) return null;
            return metaData.bytes;
        }

        public override string GetTemplateCover(string templateId = null) {
            ClothesTemplate template = null;
            if (string.IsNullOrEmpty(templateId)) {
                template = DataTables.GetClothesTemplateList().FirstOrDefault();
            } else {
                template = DataTables.GetClothesTemplate(templateId);
            }
            return CosXmlUploadManager.GetBusinessRootUrl() + "/" + template?.RemoteCover;
        }


    }
}
