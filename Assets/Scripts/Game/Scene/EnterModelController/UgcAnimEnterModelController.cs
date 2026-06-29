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
using BUD.AnimPose;
using Game.Config;
using Game.Props.PropsManagers;
using Game.Utils;
using Message;
using Newtonsoft.Json;
using RTG;
using UIAgent;
using Object = UnityEngine.Object;

namespace Game.Scene.EnterModelController
{
    [EnterModel(EnterGameModel.UgcAnimEmpty, EnterGameModel.UgcAnimContinueEdit)]
    public class UgcAnimEnterModelController : BaseEnterModelController
    {
        protected AnimInfo animInfo;


        public override void Start(UgcBaseInfo baseInfo)
        {
            base.Start(baseInfo);
            animInfo = baseInfo as AnimInfo;
            if (animInfo == null)
            {
                GameController.ExitGame("创建草稿失败");
                return;
            }

            if (string.IsNullOrEmpty(animInfo.id))
            {
                animInfo.cover = "https://cdn.budapp.cn/UgcAnimStudio/TemplateCover/UGCAnim_" + animInfo.animType + ".png";
                UGCAnimAssetManager.Inst.CreateUgcAnimInServer(animInfo, (success) =>
                {
                    if (success)
                    {
                        BuildEditMapEnv(animInfo);
                    }
                    else
                    {
                        UIAgentManager.Inst.ClosePanel(WindowId.GameHallWindow, PanelId.UgcLoadingPanel);
                        GameController.ExitGame("创建草稿失败");
                    }
                });
            }
            else
            {
                BuildEditMapEnv(animInfo);
            }
        }

        /// <summary>
        /// 构建素材编辑所需要的地图环境
        /// </summary>
        bool BuildEditMapEnv(AnimInfo info)
        {
            string mapMetaUrl = info.metaDataUrl;
            var localDraftInfo = UGCAnimAssetManager.Inst.GetDraftInfo(info);
            if (localDraftInfo != null) {
                var tmpMapMetaUrl = localDraftInfo.GetMetadataUrl();
                if (!string.IsNullOrEmpty(tmpMapMetaUrl)) {
                    mapMetaUrl = tmpMapMetaUrl;
                }
            }
            BuildEditMapEnvRemote(mapMetaUrl);
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
            
            string jsonString = System.Text.Encoding.UTF8.GetString(metaDataBytes);
            
            MessageHelper.Broadcast(MessageName.StartBuildMap, GameController.enterGameModel);
            try
            {
                MessageHelper.Broadcast<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, GameController.enterGameModel, animInfo);
                
                AnimFrameData animFrameData = JsonConvert.DeserializeObject<AnimFrameData>(jsonString);
                UIAgentManager.Inst.OpenPanel(PanelId.AnimationStudioEditPanel, animInfo, animFrameData);
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("UgcAnim 解析失败:" + e.Message + ", e:" + e.StackTrace);
                GameController.ExitGame();
                return false;
            }
            return true;
        }
    }
}


