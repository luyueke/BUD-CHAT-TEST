using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Base;
using Game.Props.PropsManagers;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.MapData;
using Message;
using SceneController.Attribute;
using UGCAsset;
using UnityEngine;
using UIAgent;

namespace Game.Scene.EnterModelController
{
    [EnterModel(EnterGameModel.UgcVehicleEmpty, EnterGameModel.UgcVehicleEmptyContinueEdit, EnterGameModel.UgcVehicleTryPlay)]
    public class UgcVehicleEnterModelController : BaseVehicleActionController
    {
        public override void Start(VehicleInfo skinInfo)
        {
            base.Start(skinInfo);
            StartVehicleInstrument();
        }

        protected override bool OnLoadMetaData(byte[] metaDataBytes)
        {
            return OnLoadPropSkinMetaData(metaDataBytes);
        }

        protected bool OnLoadPropSkinMetaData(byte[] metaDataBytes)
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
            GameController.ChangeMode(GameMode.Edit);
            MessageHelper.Broadcast(MessageName.StartBuildMap, GameController.enterGameModel);
            try
            {
                var mapData = MapPbDataTool.ParseMapPb(metaDataBytes);
                SceneBuilder.Inst.BuildMapByData(mapData);
                var vehicleInfo = GameDataManager.Inst.mapGlobalData?.GetCurInfo<VehicleInfo>();
                //for (int i = 0; i < vehicleInfo.vehicleType; i++)
                //{
                //    GlobalNodeManager.Inst.Get<PreviewModelManager>().CheckPersonModel();
                //}
                MessageHelper.Broadcast<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, GameController.enterGameModel, _vehicleInfo);
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("地图解析失败:" + e.Message + ", e:" + e.StackTrace);
                GameController.ExitGame();
                return false;
            }

            return true;
        }

        private void StartVehicleInstrument()
        {
            //1.全新的载具
            if (string.IsNullOrEmpty(_vehicleInfo.id))
            {
                VehicleAssetManager.Inst.CreateInServer(_vehicleInfo, (metaData) =>
                {
                    OnLoadMetaData(metaData);
                });
            }
            //2.继续编辑载具
            else
            {
                var buildSuccess = BuildEditMapEnv();
                if (!buildSuccess)
                {
                    GameController.ExitGame("构建衣服编辑所需要的地图环境失败");
                }
            }
        }

        /// <summary>
        /// 构建素材编辑所需要的地图环境
        /// </summary>
        bool BuildEditMapEnv()
        {
            string mapMetaUrl = _vehicleInfo.mapUrl;
            if (!string.IsNullOrEmpty(mapMetaUrl))
            {
                var localDraftInfo = VehicleAssetManager.Inst.GetDraftInfo(_vehicleInfo);
                if (localDraftInfo != null)
                {
                    var tmpMapMetaUrl = localDraftInfo.GetMapUrl();
                    if (!string.IsNullOrEmpty(tmpMapMetaUrl))
                    {
                        mapMetaUrl = tmpMapMetaUrl;
                    }
                }

                BuildEditMapEnvRemote(mapMetaUrl);
                return true;
            }
            else
            {
                var metaData = InstrumentAssetManager.Inst.GetTemplateMetaData(_vehicleInfo.templateId);
                if (metaData == null)
                {
                    LoggerUtils.LogError("模板数据为空:" + _vehicleInfo.templateId);
                    GameController.ExitGame("模板数据为空:" + _vehicleInfo.templateId);
                    return false;
                }
                else
                {
                    OnLoadMetaData(metaData);
                }
            }

            //var metaData = InstrumentAssetManager.Inst.GetTemplateMetaData(_vehicleInfo.templateId);
            //if (metaData == null)
            //{
            //    LoggerUtils.LogError("模板数据为空:" + _vehicleInfo.templateId);
            //    GameController.ExitGame("模板数据为空:" + _vehicleInfo.templateId);
            //    return false;
            //}
            //else
            //{
            //    OnLoadMetaData(metaData);
            //}
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
            }
            else
            {
                LoadRemoteMetaData(mapMetaUrl);
            }
        }
    }

}
