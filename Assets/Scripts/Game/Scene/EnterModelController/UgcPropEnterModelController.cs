/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-29 17:29:03
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-05 11:01:24
 * @ Description: 素材编辑器进入流程
 */

using System;
using Game.Base;
using GameData;
using GameData.Base;
using GameData.MapData;
using GameData.BaseInfo;
using SceneController.Attribute;
using Google.Protobuf;
using UGCAsset;
using UGCAsset.Draft;
using UnityEngine;
using System.IO;
using Game.Config;
using Game.Props.PropsManagers;
using Game.Utils;
using Message;
using Newtonsoft.Json;
using RTG;
using Object = UnityEngine.Object;

namespace Game.Scene.EnterModelController
{
    [EnterModel(EnterGameModel.UgcPropEmpty, EnterGameModel.UgcPropContinueEdit)]
    public class UgcPropEnterModelController : BaseEnterModelController
    {
        protected PropInfo propInfo;


        public override void Start(UgcBaseInfo baseInfo)
        {
            base.Start(baseInfo);
            propInfo = baseInfo as PropInfo;


            if (string.IsNullOrEmpty(propInfo.id) )
            {
                PropAssetManager.Inst.CreateInServer(propInfo, (metaData) =>
                {
                    if (metaData == null) {
                        GameController.ExitGame("");
                        return;
                    }
                    OnLoadMetaData(metaData);
                });
            }
            else
            {
                var buildSuccess = BuildEditMapEnv(propInfo);
                if (!buildSuccess)
                {
                    GameController.ExitGame("构建素材编辑所需要的地图环境失败");
                }
            }

        }

        /// <summary>
        /// 构建素材编辑所需要的地图环境
        /// </summary>
        bool BuildEditMapEnv(PropInfo info)
        {
            string mapMetaUrl = info.mapUrl;
            if (!string.IsNullOrEmpty(mapMetaUrl))
            {
                var localDraftInfo = PropAssetManager.Inst.GetDraftInfo(info);
                if (localDraftInfo != null) {
                    var tmpMapMetaUrl = localDraftInfo.GetMapUrl();
                    if (!string.IsNullOrEmpty(tmpMapMetaUrl)) {
                        mapMetaUrl = tmpMapMetaUrl;
                    }
                }
                BuildEditMapEnvRemote(mapMetaUrl);
                return true;
            }
            else
            {
                var metaData = PropAssetManager.Inst.GetTemplateMetaData(info.templateId);
                if (metaData == null)
                {
                    LoggerUtils.LogError("模板数据为空:" + info.templateId);
                    GameController.ExitGame("模板数据为空:" + info.templateId);
                    return false;
                }
                else
                {
                    OnLoadMetaData(metaData);
                }
            }
            return true;
        }

        void BuildEditMapEnvRemote(string mapMetaUrl)
        {
            if (!mapMetaUrl.StartsWith("https://") && !mapMetaUrl.StartsWith("http://"))
            {
                if (File.Exists(mapMetaUrl))
                {
                    OnLoadMetaData(File.ReadAllBytes(mapMetaUrl));
                }
                else
                {
                    LoggerUtils.LogError("Url数据为空:" + mapMetaUrl);
                    GameController.ExitGame("Url数据为空:" + mapMetaUrl);
                }
            } else
            {
                LoadRemoteMetaData(mapMetaUrl);
            }
        }

        protected override bool OnLoadMetaData(byte[] metaDataBytes)
        {
            if (!base.OnLoadMetaData(metaDataBytes))
            {
                return false;
            }

            if (metaDataBytes == null || metaDataBytes.Length == 0)
            {
                GameController.ExitGame();
                return false;
            }
            MessageHelper.Broadcast(MessageName.StartBuildMap, GameController.enterGameModel);
            try
            {
                var mapData = MapPbDataTool.ParseMapPb(metaDataBytes);
                SceneBuilder.Inst.BuildMapByData(mapData);
                MessageHelper.Broadcast<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, GameController.enterGameModel, propInfo);
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("地图解析失败:" + e.Message + ", e:" + e.StackTrace);
                GameController.ExitGame();
                return false;
            }
            GameController.ChangeMode(GameMode.Edit);
            return true;
        }

        public PropInfo GetUGCItemInfo()
        {
            return propInfo;
        }
    }
}
