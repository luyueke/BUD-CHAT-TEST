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
using UIAgent;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UGCAsset.Draft
{
    public sealed class MusicScoreDraftInfo : BaseDraftInfo<MusicScoreDraftInfo, MusicScoreInfo> {
        protected override string SetUrl => HttpUrlDefine.SetMusicScore;

        protected override string CoverRemoteFolder => $"UgcMusicScoreCover/{uid}";
        protected override string MetaDataRemoteFolder => $"UgcMusicScoreMetadata/{uid}";

        // public string[] photoUrls;


        public MusicScoreDraftInfo()
        {

        }

        public MusicScoreDraftInfo(MusicScoreInfo musicScoreInfo): base(musicScoreInfo)
        {
            // 初始化草稿基础信息
           
        }   
        public void PublishDraftToServer(string overwriteId, Action<MusicScoreInfo, bool> callBack) {
            var ugcInfo = ToUgcInfo();
            var req = new UGCSetRequest(ugcInfo, UGCOperationType.OverwritePublish);
            req.overwriteId = overwriteId;
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
                callBack(draftListItem?.Get<MusicScoreInfo>(), true);
            }, fail => {
                LoggerUtils.LogError($"发布失败 [{ugcInfo.id}]:" + fail);
                callBack(null, false);
                UIAgentManager.Inst.ShowToast("发布失败！");
            });
        }

        public void SetCover(string url)
        {
            SetUploadRemoteInfo(CoverKey, url, CoverRemoteFolder);
            
        }
       
    }
}
