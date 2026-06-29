using System.Linq;
using Game.COSXML;
using Es;
using Game.Config;
using GameData.BaseInfo;
using Network.Http;
using UGCAsset.Draft;
using UnityEngine;

namespace UGCAsset
{
    public class PetSkinAssetManager : UGCAssetManager<SkinDraftInfo, SkinInfo, PetSkinAssetManager>
    {
        protected override string GetAssetInfoUrl => HttpUrlDefine.GetClothesInfo;

        public override byte[] GetTemplateMetaData(string templateId = null)
        {
            ClothesTemplate template = null;
            if (string.IsNullOrEmpty(templateId))
            {
                template = DataTables.GetPetClothesTemplateList().FirstOrDefault();
            }
            else
            {
                template = DataTables.GetPetClothesTemplate(templateId);
            }

            var metaDataUrl = template?.MetaDataUrl;
            if (string.IsNullOrEmpty(metaDataUrl)) return null;
            var metaDataRequest = xasset.Asset.Load(GameConsts.PetClothesAssetDir + metaDataUrl, typeof(TextAsset));
            if (metaDataRequest == null) return null;
            var metaData = metaDataRequest.asset as TextAsset;
            if (metaData == null) return null;
            return metaData.bytes;
        }

        public override string GetTemplateCover(string templateId = null)
        {
            ClothesTemplate template = null;
            if (string.IsNullOrEmpty(templateId))
            {
                template = DataTables.GetPetClothesTemplateList().FirstOrDefault();
            }
            else
            {
                template = DataTables.GetPetClothesTemplate(templateId);
            }

            return CosXmlUploadManager.GetBusinessRootUrl() + "/" + template?.RemoteCover;
        }


    }
}
