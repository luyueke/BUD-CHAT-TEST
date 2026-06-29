// @Author: YangJie
// @Description:
// @Date:  2023/08/17
// @Modify:

using System;
using System.Linq;
using Es;
using Game.Config;
using GameData.BaseInfo;
using GameData.UGCData;
using Network.Http;
using UGCAsset.Draft;
using UnityEngine;

namespace UGCAsset {
    public class MusicScoreAssetManager : UGCAssetManager<MusicScoreDraftInfo, MusicScoreInfo, MusicScoreAssetManager> {
        protected override string GetAssetInfoUrl => HttpUrlDefine.GetMusicScoreInfo;
        public void CreateMusicScoreInServer(MusicScoreInfo info, Action<bool> callBack = null) {
            // if (string.IsNullOrEmpty(info.cover)) {
            //     info.cover = GetTemplateCover(info.templateId);
            // }
            var draftInfo = GetOrCreateDraftInfo(info);
            draftInfo?.CreateDraftToServer((tmpInfo, isSuccess) => {
                if (isSuccess) {
                    tmpInfo.CopyTo(info);
                }
                callBack?.Invoke(isSuccess);
            });
        }
        // public override byte[] GetTemplateMetaData(string templateId = null) {
        //     ClothesTemplate template = null;
        //     if (string.IsNullOrEmpty(templateId)) {
        //         template = DataTables.GetClothesTemplateList().FirstOrDefault();
        //     } else {
        //         template = DataTables.GetClothesTemplate(templateId);
        //     }
        //     var metaDataUrl = template?.MetaDataUrl;
        //     if (string.IsNullOrEmpty(metaDataUrl)) return null;
        //     var metaDataRequest = xasset.Asset.Load(GameConsts.ClothesAssetDir + metaDataUrl, typeof(TextAsset));
        //     if (metaDataRequest == null) return null;
        //     var metaData = metaDataRequest.asset as TextAsset;
        //     if (metaData == null) return null;
        //     return metaData.bytes;
        // }



    }
}
