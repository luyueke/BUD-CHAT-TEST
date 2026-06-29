using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Basic.Extensions;
using Basic.Utils;
using Game.Base;
using Game.Config;
using Game.Props.PropsManagers;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.UGCData;
using Message;
using Newtonsoft.Json;
using SceneController.Attribute;
using UGCAsset;
using UIAgent;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scene.EnterModelController {
    [EnterModel(EnterGameModel.UgcMusicalInstrumentEmpty, EnterGameModel.UgcMusicalInstrumentContinueEdit)]

    public class UgcMusicalInstrumentEnterModelController : BaseSkinActionController
    {
        public override void Start(SkinInfo skinInfo, SkinActionInfo skinActionInfo)
        {
            base.Start(skinInfo, skinActionInfo);
            StartMusicalInstrument();
        }

        protected override bool OnLoadMetaData(byte[] metaDataBytes) {
            return OnLoadPropSkinMetaData(metaDataBytes);
        }

        #region UGC乐器

        protected bool OnLoadPropSkinMetaData(byte[] metaDataBytes) {
            if (!base.OnLoadMetaData(metaDataBytes)) {
                return false;
            }

            if (metaDataBytes == null || metaDataBytes.Length == 0) {
                GameController.ExitGame();
                return false;
            }

            MessageHelper.Broadcast(MessageName.StartBuildMap, GameController.enterGameModel);
            try {
                var mapData = MapPbDataTool.ParseMapPb(metaDataBytes);
                SceneBuilder.Inst.BuildMapByData(mapData);
                GlobalNodeManager.Inst.Get<PreviewModelManager>().CheckModel();
                MessageHelper.Broadcast<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, GameController.enterGameModel, _skinInfo);
            } catch (Exception e) {
                LoggerUtils.LogError("地图解析失败:" + e.Message + ", e:" + e.StackTrace);
                GameController.ExitGame();
                return false;
            }

            GameController.ChangeMode(GameMode.Edit);
            return true;
        }


        private void StartMusicalInstrument() {

            //1.全新的UGC乐器
            if (string.IsNullOrEmpty(_skinInfo.id)) {
                InstrumentAssetManager.Inst.CreateInServer(_skinInfo, _skinActionInfo, (metaData) => {
                    OnLoadMetaData(metaData);
                });
            }
            //2.继续编辑UGC乐器
            else {
                var buildSuccess = BuildEditMapEnv();
                if (!buildSuccess) {
                    GameController.ExitGame("构建衣服编辑所需要的地图环境失败");
                }
            }
        }


        /// <summary>
        /// 构建素材编辑所需要的地图环境
        /// </summary>
        bool BuildEditMapEnv() {
            string mapMetaUrl = _skinInfo.mapUrl;
            if (!string.IsNullOrEmpty(mapMetaUrl)) {
                var localDraftInfo = InstrumentAssetManager.Inst.GetDraftInfo(_skinInfo, _skinActionInfo);
                if (localDraftInfo != null) {
                    var tmpMapMetaUrl = localDraftInfo.GetMapUrl();
                    if (!string.IsNullOrEmpty(tmpMapMetaUrl)) {
                        mapMetaUrl = tmpMapMetaUrl;
                    }
                }

                BuildEditMapEnvRemote(mapMetaUrl);
                return true;
            }
            else {
                var metaData = InstrumentAssetManager.Inst.GetTemplateMetaData(_skinInfo.templateId);
                if (metaData == null) {
                    LoggerUtils.LogError("模板数据为空:" + _skinInfo.templateId);
                    GameController.ExitGame("模板数据为空:" + _skinInfo.templateId);
                    return false;
                }
                else {
                    OnLoadMetaData(metaData);
                }
            }

            return true;
        }

        void BuildEditMapEnvRemote(string mapMetaUrl) {
            if (!mapMetaUrl.StartsWith("https://") && !mapMetaUrl.StartsWith("http://")) {
                if (File.Exists(mapMetaUrl)) {
                    OnLoadMetaData(File.ReadAllBytes(mapMetaUrl));
                } else {
                    LoggerUtils.LogError("Url数据为空:" + mapMetaUrl);
                    GameController.ExitGame("Url数据为空:" + mapMetaUrl);
                }
            } else {
                LoadRemoteMetaData(mapMetaUrl);
            }
        }

        #endregion
    }
}
