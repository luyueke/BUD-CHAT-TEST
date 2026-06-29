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
    public sealed class UgcBundleDraftInfo : BaseDraftInfo<UgcBundleDraftInfo, SkinInfo> {
        protected override string SetUrl => HttpUrlDefine.SetSkin;

        protected override string CoverRemoteFolder => $"UgcBundleCover/{uid}";

        public UgcBundleDraftInfo()
        {

        }

        public UgcBundleDraftInfo(SkinInfo ugcBundleInfo) : base(ugcBundleInfo)
        {
            // 初始化草稿基础信息
           
        }   
        public void PublishDraftToServer(string overwriteId, Action<SkinInfo, bool> callBack) {
            var ugcInfo = ToUgcInfo();
            var req = new UGCSetRequest(ugcInfo, UGCOperationType.Publish);
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
                callBack(draftListItem?.Get<SkinInfo>(), true);
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
