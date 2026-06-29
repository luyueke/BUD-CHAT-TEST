// @Author: YangJie
// @Description: 材质本地草稿信息
// @Date:  2023/08/29
// @Modify:

using System;
using System.Collections.Generic;
using System.IO;
using Basic.Utils;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using Network;
using Network.Http;
using UnityEngine;

namespace UGCAsset.Draft
{
    public class MaterialDraftInfo : BaseDraftInfo<MaterialDraftInfo, MaterialInfo>
    {
        protected override string SetUrl => HttpUrlDefine.SetMaterial;
        protected override string CoverRemoteFolder => $"UgcMaterialCover/{uid}";
        protected override string MetaDataRemoteFolder => $"UgcMaterialMetadata/{uid}";

        private string PartKey => "Part";
        private string PartRemoteFolder => $"UgcMaterial/{uid}";


        #region 不需要上传文件，但是需要保存的信息
        public string[] imgs;
        #endregion

        public MaterialDraftInfo()
        {

        }

        public MaterialDraftInfo(MaterialInfo materialInfo): base(materialInfo)
        {
            imgs = materialInfo.imgs;
            // 初始化草稿基础信息
            SetUploadRemoteInfo(PartKey, materialInfo.materialUrl, PartRemoteFolder);

            if (materialInfo.paymentInfo == null) {
                materialInfo.paymentInfo = new PaymentInfo() {
                    currencyType = CurrencyType.PinkCoin,
                    price = 0,
                };
            }
        }


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
            if (draftUploadInfo.draftName == PartKey && baseInfo != null) {
                baseInfo.materialUrl = draftUploadInfo.remoteUrl;
            }
        }

        public void SetParts(byte[] bytes)
        {
            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            var filePath = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".png");
            File.WriteAllBytes(filePath, bytes);
            SetUploadLocalInfo(PartKey, filePath, PartRemoteFolder);
        }

        private string GetPartsLocalUrl()
        {
            return draftUploadInfos.Find(info => info.draftName == PartKey)?.draftPath;
        }

        public string GetPartsRemoteUrl()
        {
            return draftUploadInfos.Find(info => info.draftName == PartKey)?.remoteUrl;
        }

        public string GetPartsUrl()
        {
            var partsUrl = GetPartsLocalUrl();
            if (string.IsNullOrEmpty(partsUrl))
            {
                partsUrl = GetPartsRemoteUrl();
            }
            return partsUrl;
        }

        public override MaterialInfo ToUgcInfo()
        {
            var info = base.ToUgcInfo();
            info.imgs = imgs;
            var partRemoteUrl = GetPartsRemoteUrl();
            if (!string.IsNullOrEmpty(partRemoteUrl))
            {
                info.materialUrl = partRemoteUrl;
            }
            return info;
        }


        public void SetClothesSaveTexture2D(Texture2D bigTexture)
        {
            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            var bytes =bigTexture.EncodeToPNG();
            var filePath = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".png");
            File.WriteAllBytes(filePath, bytes);
            SetUploadLocalInfo(PartKey, filePath, PartRemoteFolder);
        }


    }
}
