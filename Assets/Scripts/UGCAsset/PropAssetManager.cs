/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-31 16:06:16
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-05 14:10:40
 * @ Description: 素材编辑器的资源管理
 */

using System;
using System.Linq;
using Game.COSXML;
using Es;
using GameData.Managers;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset.Draft;
using UnityEngine;

namespace UGCAsset
{
    public class PropAssetManager : UGCAssetManager<PropDraftInfo, PropInfo, PropAssetManager>
    {

        protected override string GetAssetInfoUrl => HttpUrlDefine.propInfo;


        public override byte[] GetTemplateMetaData(string templateId = null) {
            PropTemplate template = null;
            if (string.IsNullOrEmpty(templateId)) {
                template = DataTables.GetPropTemplateList().FirstOrDefault();
            } else {
                template = DataTables.GetPropTemplate(templateId);
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
            PropTemplate template = null;
            if (string.IsNullOrEmpty(templateId)) {
                template = DataTables.GetPropTemplateList().FirstOrDefault();
            } else {
                template = DataTables.GetPropTemplate(templateId);
            }
            return CosXmlUploadManager.GetBusinessRootUrl() + "/" + template?.RemoteCover;
        }
    }
}
