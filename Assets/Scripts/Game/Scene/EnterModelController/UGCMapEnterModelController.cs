// @Author: YangJie
// @Description:
// @Date:  2023/09/19
// @Modify:

using System;
using Game.Base;
using Game.OfflineRender;
using GameData;
using GameData.Base;
using GameData.MapData;
using GameData.BaseInfo;
using Message;
using Pb.Map;
using UGCAsset;
using Object = UnityEngine.Object;

namespace Game.Scene.EnterModelController
{
    public class UGCMapEnterModelController : BaseEnterModelController
    {
        protected MapInfo mapInfo;
        protected byte[] remoteMetaDataBytes;
        protected PMapData pMapData = null;

        public override void Start(UgcBaseInfo baseInfo)
        {
            mapInfo = baseInfo as MapInfo;
            base.Start(baseInfo);
        }

        protected override bool OnLoadMetaData(byte[] metaDataBytes)
        {
            base.OnLoadMetaData(metaDataBytes);
            remoteMetaDataBytes = metaDataBytes;
            pMapData = MapPbDataTool.ParseMapPb(remoteMetaDataBytes);
            LoadRemoteOfflineRenderData();
            return true;
        }

        protected virtual void LoadRemoteOfflineRenderData()
        {
            GameOfflineRenderManager.Inst.InitAndLoadMapOfflineRenderData(() =>
            {
                BuildMap();
            });
        }

        protected override void LoadRemoteMetaData(string metaDataUrl) {
            if (string.IsNullOrEmpty(metaDataUrl)) {
                LoggerUtils.LogError("Url数据为空 进入默认地图");
                MessageHelper.Broadcast(MessageName.StartLoadRemoteMetadata);
                var metaDataBytes = MapAssetManager.Inst.GetTemplateMetaData(mapInfo.metaDataUrl);
                MessageHelper.Broadcast(MessageName.OverLoadRemoteMetadata);
                OnLoadMetaData(metaDataBytes);
                return;
            }
            base.LoadRemoteMetaData(metaDataUrl);
        }


        protected virtual bool BuildMap()
        {
            if (remoteMetaDataBytes == null || remoteMetaDataBytes.Length == 0)
            {
                LoggerUtils.LogError("元数据为空");
                GameController.ExitGame("元数据为空");
                return false;
            }
            try
            {
                if (pMapData == null)
                {
                    pMapData = MapPbDataTool.ParseMapPb(remoteMetaDataBytes);
                }
                MessageHelper.Broadcast<EnterGameModel, object>(MessageName.StartBuildMap, GameController.enterGameModel, pMapData);
                SceneBuilder.Inst.BuildMapByData(pMapData);
                MessageHelper.Broadcast<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, GameController.enterGameModel, mapInfo);
                return true;
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("地图解析失败:" + e.Message + ", e:" + e.StackTrace);
                GameController.ExitGame("地图解析失败:" + e.Message);
                return false;
            }

        }



    }
}
