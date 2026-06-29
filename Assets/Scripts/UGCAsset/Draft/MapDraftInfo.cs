// @Author: YangJie
// @Description:
// @Date:  2023/07/14
// @Modify:

using System;
using System.Collections.Generic;
using System.IO;
using Basic.Extensions;
using GameData.Manager;
using GameData.BaseInfo;
using GameData.OfflineRender;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UIAgent;
using UnityEngine;

namespace UGCAsset.Draft
{
    public class MapDraftInfo : BaseDraftInfo<MapDraftInfo, MapInfo>
    {
        protected override string SetUrl => HttpUrlDefine.setMap;

        protected sealed override string CoverRemoteFolder => $"UgcMapCover/{uid}";
        protected sealed override string MetaDataRemoteFolder => $"UgcMapMetadata/{uid}";

        #region 不需要上传但是需要保存的信息
        public OfflineRenderInfo renderInfo;
        #endregion


        /// <summary>
        /// 使用了 Newtonsoft.Json 序列化，所以必须要有无参构造函数
        /// </summary>
        public MapDraftInfo()
        {

        }

        public MapDraftInfo(MapInfo mapInfo): base(mapInfo)
        {
        }

        public void PublishDraftToServer(string overwriteId, Action<MapInfo, bool> callBack) {
            var ugcInfo = ToUgcInfo();
            var req = new UGCSetRequest(ugcInfo, UGCOperationType.OverwritePublish);
            req.overwriteId = overwriteId;
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
                callBack(draftListItem?.Get<MapInfo>(), true);
            }, fail => {
                LoggerUtils.LogError($"发布失败 [{ugcInfo.id}]:" + fail.rmsg);
                callBack(null, false);
                UIAgentManager.Inst.ShowToast($"发布失败！{fail.rmsg}");
            });
        }

    }
}
