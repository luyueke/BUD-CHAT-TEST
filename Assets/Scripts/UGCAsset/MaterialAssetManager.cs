// @Author: YangJie
// @Description:
// @Date:  2023/08/30
// @Modify:

using System.Linq;
using Game.COSXML;
using Es;
using GameData.BaseInfo;
using Network.Http;
using UGCAsset.Draft;
using UnityEngine;

namespace UGCAsset
{
    public class MaterialAssetManager: UGCAssetManager<MaterialDraftInfo, MaterialInfo, MaterialAssetManager>
    {
        protected override string GetAssetInfoUrl => HttpUrlDefine.GetMaterialInfo;

        public override byte[] GetTemplateMetaData(string templateId = null) {

            UGCMaterialTemplate template = null;
            if (string.IsNullOrEmpty(templateId)) {
                template = DataTables.GetUGCMaterialTemplateList().FirstOrDefault();
            } else {
                template = DataTables.GetUGCMaterialTemplate(templateId);
            }
            var metaDataUrl = template?.MetaDataUrl;
            if (string.IsNullOrEmpty(metaDataUrl)) return null;
            var metaDataRequest = xasset.Asset.Load(metaDataUrl, typeof(TextAsset));
            if (metaDataRequest == null) return null;
            var metaData = metaDataRequest.asset as TextAsset;
            if (metaData == null) return null;
            return metaData.bytes;
        }

        public override string GetTemplateCover(string templateId = null) {
            UGCMaterialTemplate template = null;
            if (string.IsNullOrEmpty(templateId)) {
                template = DataTables.GetUGCMaterialTemplateList().FirstOrDefault();
            } else {
                template = DataTables.GetUGCMaterialTemplate(templateId);
            }
            return CosXmlUploadManager.GetBusinessRootUrl() + "/" + template?.RemoteCover;
        }
    }
}
