// @Author: YangJie
// @Description:
// @Date:  2023/07/14
// @Modify:

using System;
using System.Collections.Generic;
using System.IO;
using Basic.Extensions;
using Basic.Utils;
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
    public class UgcAnimDraftInfo : BaseDraftInfo<UgcAnimDraftInfo, AnimInfo>
    {
        protected override string SetUrl => HttpUrlDefine.setAnim;

        protected sealed override string CoverRemoteFolder => $"UgcAnimCover/{uid}";
        protected sealed override string MetaDataRemoteFolder => $"UgcAnimMetadata/{uid}";

        #region 不需要上传但是需要保存的信息
        public OfflineRenderInfo renderInfo;
        #endregion


        /// <summary>
        /// 使用了 Newtonsoft.Json 序列化，所以必须要有无参构造函数
        /// </summary>
        public UgcAnimDraftInfo()
        {

        }

        public UgcAnimDraftInfo(AnimInfo animInfo): base(animInfo)
        {
        }

        public void PublishDraftToServer(string overwriteId, Action<AnimInfo, bool> callBack) {
            var ugcInfo = ToUgcInfo();
            var req = new UGCSetRequest(ugcInfo, UGCOperationType.OverwritePublish);
            req.overwriteId = overwriteId;
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
                callBack(draftListItem?.Get<AnimInfo>(), true);
            }, fail => {
                LoggerUtils.LogError($"发布失败 [{ugcInfo.id}]:" + fail);
                callBack(null, false);
                UIAgentManager.Inst.ShowToast("发布失败！");
            });
        }
        
        public void SetMetaData(string metaData)
        {
            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            var localMetaDataUrl = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".json");
            File.WriteAllText(localMetaDataUrl, metaData);
            SetUploadLocalInfo(MetaDataKey, localMetaDataUrl, MetaDataRemoteFolder);
        }
        
        public void SetAnimTime(float time)
        {
            this.baseInfo.animationTime = time;
        }
        
        public void SetSkinType(int skinType)
        {
            this.baseInfo.skinType = skinType;
        }

    }
}