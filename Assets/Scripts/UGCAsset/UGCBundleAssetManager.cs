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
    public class UGCBundleAssetManager : UGCAssetManager<UgcBundleDraftInfo, SkinInfo, UGCBundleAssetManager> {
        protected override string GetAssetInfoUrl => HttpUrlDefine.GetClothesInfo;
        public void CreateBundleInServer(SkinInfo info, Action<bool> callBack = null) {

            var draftInfo = GetOrCreateDraftInfo(info);
            draftInfo?.CreateDraftToServer((tmpInfo, isSuccess) => {
                if (isSuccess) {
                    tmpInfo.CopyTo(info);
                }
                callBack?.Invoke(isSuccess);
            });
        }
    }
}
