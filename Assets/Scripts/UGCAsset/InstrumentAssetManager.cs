// @Author: YangJie
// @Description:
// @Date:  2023/08/17
// @Modify:

using System;
using System.Linq;
using Es;
using Game.Config;
using Game.COSXML;
using GameData.BaseInfo;
using GameData.UGCData;
using Network.Http;
using UGCAsset.Draft;
using UnityEngine;

namespace UGCAsset {
    public class InstrumentAssetManager : UGCSkinActionAssetManager<InstrumentAssetManager>
    {
        public override string GetTemplateCover(string templateId = null) {
            ClothesTemplate template = null;
            template = DataTables.GetClothesTemplate(templateId);
            return CosXmlUploadManager.GetBusinessRootUrl() + "/" + template?.RemoteCover;
        }
    }
}
