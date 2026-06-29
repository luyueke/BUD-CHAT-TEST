// @Author: YangJie
// @Description:
// @Date:  2023/07/17
// @Modify:

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Basic.Extensions;
using Game.COSXML;
using Es;
using GameData.Managers;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset.Draft;
using UnityEngine;

namespace UGCAsset
{
    public class MapAssetManager : UGCAssetManager<MapDraftInfo, MapInfo, MapAssetManager>
    {
        protected override string GetAssetInfoUrl => HttpUrlDefine.mapInfo;

        public override byte[] GetTemplateMetaData(string templateId = null) {
            MapTemplate template = null;
            if (string.IsNullOrEmpty(templateId)) {
                template = DataTables.GetMapTemplateList().FirstOrDefault();
            } else {
                template = DataTables.GetMapTemplate(templateId);
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
            MapTemplate template = null;
            if (string.IsNullOrEmpty(templateId)) {
                template = DataTables.GetMapTemplateList().FirstOrDefault();
            } else {
                template = DataTables.GetMapTemplate(templateId);
            }
            return CosXmlUploadManager.GetBusinessRootUrl() + "/" + template?.RemoteCover;
        }
    }
}
