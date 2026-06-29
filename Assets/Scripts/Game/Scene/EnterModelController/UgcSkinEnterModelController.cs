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
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SceneController.Attribute;
using UGCAsset;
using UIAgent;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scene.EnterModelController {
    [EnterModel(EnterGameModel.UgcSkinEmpty, EnterGameModel.UgcSkinContinueEdit)]
    public class UgcSkinEnterModelController : BaseEnterModelController {
        public SkinInfo skinInfo;


        public override void Start(UgcBaseInfo baseInfo) {
            skinInfo = baseInfo as SkinInfo;
            base.Start(baseInfo);
            if (skinInfo.isProp) {
                StartPropSkin();
            } else {
                StartNormalSkin();
            }
        }

        protected override bool OnLoadMetaData(byte[] metaDataBytes) {
            return skinInfo.isProp ? OnLoadPropSkinMetaData(metaDataBytes) : OnLoadNormalSkinMetaData(metaDataBytes);
        }

        #region 模板 衣服

        protected bool OnLoadNormalSkinMetaData(byte[] metaDataBytes) {
            if (metaDataBytes == null) {
                LoggerUtils.LogError($"衣服元数据为空: {skinInfo.id} --{skinInfo.templateId}");
                GameController.ExitGame($"衣服元数据为空:{skinInfo.id} --{skinInfo.templateId}");
                return false;
            }

            UGCClothesData clothesData = JsonConvert.DeserializeObject<UGCClothesData>(metaDataBytes.ToStr());
            clothesData.id = skinInfo.templateId;
            Debug.Log("解析的衣服数据:" + JsonConvert.SerializeObject(clothesData));

            var bData =  UgcPartDataManager.Inst.GetUgcPartDataOnFirst(skinInfo.templateId);
            Loader.LoadAsyncOrSync<GameObject>((skinInfo.skinType == (int)SkinType.Avatar?GameConsts.ClothesAssetDir:GameConsts.PetClothesAssetDir) + bData.prefabName + ".prefab",
                (isSuc, warpper) => {
                    if (isSuc && warpper != null) {
                        PreloadPhoto(clothesData.parts, (failCount) => {
                            UIAgentManager.Inst.ClosePanel(WindowId.CommonWindow, PanelId.UgcLoadingPanel);
                            Debug.Log("下载失败的衣服数据:" + failCount);
                            if (failCount == 0) {
                                UIAgentManager.Inst.OpenPanel(PanelId.UGCResourceEditPanel, clothesData, skinInfo);
                                MessageHelper.Broadcast(MessageName.DidOpenUgcCloseEidtPage);
                                if ((SkinType)skinInfo.skinType == SkinType.Pet)
                                {
                                    ReportTask(34);
                                }
                            } else {
                                GameController.ExitGame($"衣服模版图片下载失败:{skinInfo.id} --{skinInfo.templateId}");
                            }
                        });
                    } else {
                        UIAgentManager.Inst.ClosePanel(WindowId.CommonWindow, PanelId.UgcLoadingPanel);
                        GameController.ExitGame($"衣服模版下载失败:{skinInfo.id} --{skinInfo.templateId}");
                    }
                });
            return true;
        }

        private void PreloadPhoto(List<UGCPartData> partDatas, Action<int> callback) {
            int downCount = 0;
            int failCount = 0;
            Action<int> complete = callback;
            List<string> photoUrls = new List<string>();
            if (partDatas != null) {
                for (var i = 0; i < partDatas.Count; i++) {
                    var partData = partDatas[i];
                    if (partData.photos != null) {
                        var urls = partData.photos.Select(x => x.photoUrl).Where(x => !string.IsNullOrEmpty(x));
                        photoUrls.AddRange(urls);
                    }
                }
            }

            if (photoUrls.Count == 0) {
                complete?.Invoke(failCount);
                return;
            }

            for (var i = 0; i < photoUrls.Count; i++) {
                var warpper = Loader.LoadRemoteImageAsync(photoUrls[i]);
                warpper.completed = success => {
                    downCount++;
                    if (!success) {
                        failCount++;
                    }

                    if (downCount == photoUrls.Count) {
                        complete?.Invoke(failCount);
                    }
                };
            }
        }

        private void StartNormalSkin()
        {
            bool isPet = skinInfo.skinType == (int)SkinType.Pet;
            if (string.IsNullOrEmpty(skinInfo.id)) {
                // 创建地图
                if (isPet)
                {
                    PetSkinAssetManager.Inst.CreateInServer(skinInfo, (metaDataBytes) => {
                        OnLoadMetaData(metaDataBytes);
                    });
                }
                else
                {
                    SkinAssetManager.Inst.CreateInServer(skinInfo, (metaDataBytes) => {
                        OnLoadMetaData(metaDataBytes);
                    });
                }
            } else {
                string metaDataUrl = skinInfo.metaDataUrl;
                if (isPet)
                {
                    var localDraftInfo = PetSkinAssetManager.Inst.GetDraftInfo(skinInfo.id);
                    if (localDraftInfo != null) {
                        metaDataUrl = localDraftInfo.GetMetadataUrl();
                    }
                }
                else
                {
                    var localDraftInfo = SkinAssetManager.Inst.GetDraftInfo(skinInfo.id);
                    if (localDraftInfo != null) {
                        metaDataUrl = localDraftInfo.GetMetadataUrl();
                    }
                }


                if (string.IsNullOrEmpty(metaDataUrl)) {
                    if (!string.IsNullOrEmpty(skinInfo.templateId)) {
                        LoggerUtils.LogError("metaDataUrl 为 null:" + skinInfo.id + " 从模板加载地图元数据");
                        if (isPet)
                        {
                            var templateJson = PetSkinAssetManager.Inst.GetTemplateMetaData(skinInfo.templateId);
                            OnLoadMetaData(templateJson);
                        }
                        else
                        {
                            var templateJson = SkinAssetManager.Inst.GetTemplateMetaData(skinInfo.templateId);
                            OnLoadMetaData(templateJson);
                        }

                    } else {
                        LoggerUtils.LogError("metaDataUrl为空，无法加载衣服元数据:" + skinInfo.id);
                        GameController.ExitGame("metaDataUrl为空，无法加载衣服元数据:" + skinInfo.id);
                    }

                    return;
                }

                if (!metaDataUrl.StartsWith("https://") &&
                    !metaDataUrl.StartsWith("http://")) {
                    if (File.Exists(skinInfo.metaDataUrl)) {
                        var jsonData = File.ReadAllBytes(skinInfo.metaDataUrl);
                        OnLoadMetaData(jsonData);
                    } else {
                        LoggerUtils.LogError("Url数据为空:" + skinInfo.id);
                        GameController.ExitGame("Url数据为空:" + skinInfo.id);
                    }
                } else {
                    // 读远端数据
                    LoadRemoteMetaData(skinInfo.metaDataUrl);
                }
            }
        }

        #endregion


        #region 3D 素材衣服

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
                MessageHelper.Broadcast<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, GameController.enterGameModel, skinInfo);
                if ((SkinType)skinInfo.skinType == SkinType.Pet)
                {
                    ReportTask(34);
                }
            } catch (Exception e) {
                LoggerUtils.LogError("地图解析失败:" + e.Message + ", e:" + e.StackTrace);
                GameController.ExitGame();
                return false;
            }

            GameController.ChangeMode(GameMode.Edit);
            return true;
        }

        private void ReportTask(int eventId)
        {
            JObject jObject = new JObject()
            {
                ["eventId"] = eventId
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PostEvent,
                HttpMethod.POST,
                JsonConvert.SerializeObject(jObject),
                (content) =>
                {

                    if (this == null)
                    {
                        return;
                    }
                },
                (error) =>
                {

                });
        }



        private void StartPropSkin() {
            switch ((SkinType)skinInfo.skinType)
            {
                case SkinType.Pet:
                    if (string.IsNullOrEmpty(skinInfo.id)) {
                        PetSkinAssetManager.Inst.CreateInServer(skinInfo, (metaData) => {
                            OnLoadMetaData(metaData);
                        });
                    }
                    else {
                        var buildSuccess = BuildEditMapEnv();
                        if (!buildSuccess) {
                            GameController.ExitGame("构建衣服编辑所需要的地图环境失败");
                        }
                    }
                    break;

                case SkinType.Avatar:
                default:
                    if (string.IsNullOrEmpty(skinInfo.id)) {
                        SkinAssetManager.Inst.CreateInServer(skinInfo, (metaData) => {
                            OnLoadMetaData(metaData);
                        });
                    }
                    else {
                        var buildSuccess = BuildEditMapEnv();
                        if (!buildSuccess) {
                            GameController.ExitGame("构建衣服编辑所需要的地图环境失败");
                        }
                    }
                    break;
            }
        }


        /// <summary>
        /// 构建素材编辑所需要的地图环境
        /// </summary>
        bool BuildEditMapEnv()
        {
            SkinType curSkinType = (SkinType)skinInfo.skinType;
            string mapMetaUrl = skinInfo.mapUrl;
            if (!string.IsNullOrEmpty(mapMetaUrl)) {
                var localDraftInfo = SkinAssetManager.Inst.GetDraftInfo(skinInfo);
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
                byte[] metaData = null;
                switch (curSkinType)
                {
                    case SkinType.Pet:
                        metaData = PetSkinAssetManager.Inst.GetTemplateMetaData(skinInfo.templateId);
                        break;

                    default:
                    case SkinType.Avatar:
                        metaData = SkinAssetManager.Inst.GetTemplateMetaData(skinInfo.templateId);
                        break;
                }

                if (metaData == null) {
                    LoggerUtils.LogError("模板数据为空:" + skinInfo.templateId);
                    GameController.ExitGame("模板数据为空:" + skinInfo.templateId);
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
