// @Author: YangJie
// @Description:
// @Date:  2023/07/18
// @Modify:

using System.IO;
using Basic.Extensions;
using Game.Base;
using Game.Scene.ModeController;
using GameData;
using GameData.Base;
using GameData.Managers;
using GameData.BaseInfo;
using Google.Protobuf;
using Pb.Map;
using SceneController.Attribute;
using UGCAsset;
using UGCAsset.Draft;
using UnityEngine;

namespace Game.Scene.EnterModelController {
    [EnterModel(EnterGameModel.ContinueEditScene, EnterGameModel.CreateEmptyScene)]
    public class EditEnterModelController : UGCMapEnterModelController {
        protected BaseModeController modeController;


        protected override void Init() {
            base.Init();
            modeController = GameController.GetModeController<EditModeController>(GameMode.Edit);
        }

        public override void Start(UgcBaseInfo baseInfo) {
            base.Start(baseInfo);
            if (string.IsNullOrEmpty(mapInfo.id)) {
                // 创建地图
                MapAssetManager.Inst.CreateInServer(mapInfo, (metaDataBytes) => {
                    if (metaDataBytes == null) {
                        LoggerUtils.Log("创建地图失败");
                        GameController.ExitGame("");
                        return;
                    }
                    OnLoadMetaData(metaDataBytes);
                });
            } else {
                var localDraftInfo = MapAssetManager.Inst.GetDraftInfo(mapInfo);
                var metaDataUrl = mapInfo.metaDataUrl;
                if (localDraftInfo != null) {
                    var tmpMetadataUrl = localDraftInfo.GetMetadataUrl();
                    if (!string.IsNullOrEmpty(tmpMetadataUrl)) {
                        metaDataUrl = tmpMetadataUrl;
                    }
                }
                if (string.IsNullOrEmpty(metaDataUrl)) {
                    if (!string.IsNullOrEmpty(mapInfo.templateId)) {
                        LoggerUtils.LogError("metaDataUrl 为 null:" + mapInfo.id + " 从模板加载地图元数据");
                        OnLoadMetaData(MapAssetManager.Inst.GetTemplateMetaData(mapInfo.templateId));
                    } else {
                        LoggerUtils.LogError("metaDataUrl为空，无法加载地图元数据:" + mapInfo.id);
                        GameController.ExitGame("metaDataUrl为空，无法加载地图元数据");
                    }
                    return;
                }
                if (!metaDataUrl.StartsWith("https://") && !metaDataUrl.StartsWith("http://")) {
                    if (File.Exists(metaDataUrl)) {
                        OnLoadMetaData(File.ReadAllBytes(metaDataUrl));
                    } else {
                        LoggerUtils.LogError("Url数据为空:" + mapInfo.id);
                        GameController.ExitGame("Url数据为空:" + mapInfo.id);
                    }
                } else {
                    LoadRemoteMetaData(metaDataUrl);
                }
            }
        }

        protected override bool BuildMap() {
            var isSuccess = base.BuildMap();
            if (isSuccess) {
                GameController.ChangeMode(GameMode.Edit);
            }

            return isSuccess;
        }
    }
}
