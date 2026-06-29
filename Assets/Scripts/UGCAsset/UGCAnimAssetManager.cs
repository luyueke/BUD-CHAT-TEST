using System;
using System.Linq;
using Game.COSXML;
using Es;
using Game.Config;
using GameData.BaseInfo;
using GameData.UGCData;
using Network.Http;
using UGCAsset.Draft;
using UnityEngine;

namespace UGCAsset
{
    public class UGCAnimAssetManager : UGCAssetManager<UgcAnimDraftInfo, AnimInfo, UGCAnimAssetManager>
    {
        protected override string GetAssetInfoUrl => HttpUrlDefine.getAnimInfo;
        
        public void CreateUgcAnimInServer(AnimInfo info, Action<bool> callBack = null) {
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
