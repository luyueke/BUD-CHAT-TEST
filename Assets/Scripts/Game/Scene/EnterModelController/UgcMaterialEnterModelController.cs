// @Author: YangJie
// @Description:
// @Date:  2023/08/30
// @Modify:

using System.Collections.Generic;
using System.IO;
using Basic.Extensions;
using Basic.Utils;
using Game.Base;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Newtonsoft.Json;
using SceneController.Attribute;
using UGCAsset;
using UIAgent;
using UnityEngine;

namespace Game.Scene.EnterModelController
{
    [EnterModel(EnterGameModel.UgcMaterialContinueEdit, EnterGameModel.UgcMaterialEmpty)]
    public class UgcMaterialEnterModelController: BaseEnterModelController
    {
        protected MaterialInfo MaterialInfo;


        public override void Start(UgcBaseInfo baseInfo)
        {
            base.Start(baseInfo);
            MaterialInfo = baseInfo as MaterialInfo;
            if (string.IsNullOrEmpty(MaterialInfo.id))
            {
                // 创建地图
                MaterialAssetManager.Inst.CreateInServer(MaterialInfo, (metaDataBytes) =>
                {
                    OnLoadMetaData(metaDataBytes);
                });
            }
            else
            {
                //读本地草稿
                if (string.IsNullOrEmpty(MaterialInfo.metaDataUrl))
                {
                    if (!string.IsNullOrEmpty(MaterialInfo.templateId))
                    {
                        LoggerUtils.LogError("metaDataUrl 为 null:" + MaterialInfo.id + " 从模板加载元数据");
                        var templateJson = MaterialAssetManager.Inst.GetTemplateMetaData(MaterialInfo.templateId);
                        OnLoadMetaData(templateJson);
                    }
                    else
                    {
                        LoggerUtils.LogError("metaDataUrl为空，无法加载元数据:" + MaterialInfo.id);
                        GameController.ExitGame("metaDataUrl为空，无法加载元数据:" + MaterialInfo.id);
                    }

                    return;
                }

                var localDraftInfo = MaterialAssetManager.Inst.GetDraftInfo(MaterialInfo.id);
                if (localDraftInfo != null)
                {
                    var jsonData = File.ReadAllBytes(localDraftInfo.GetMetadataLocalUrl());
                    OnLoadMetaData(jsonData);
                }
                else if (!MaterialInfo.metaDataUrl.StartsWith("https://") && !MaterialInfo.metaDataUrl.StartsWith("http://"))
                {
                    if (MaterialInfo.metaDataUrl.StartsWith(Application.persistentDataPath))
                    {
                        if (File.Exists(MaterialInfo.metaDataUrl))
                        {
                            var jsonData = File.ReadAllBytes(baseInfo.metaDataUrl);
                            OnLoadMetaData(jsonData);
                        }
                        else
                        {
                            LoggerUtils.LogError("Url数据为空:" + MaterialInfo.id);
                            GameController.ExitGame("Url数据为空:" + MaterialInfo.id);
                        }
                    }
                    else
                    {
                        var metaDataRequest = xasset.Asset.Load(MaterialInfo.metaDataUrl, typeof(TextAsset));
                        if (metaDataRequest is {asset: TextAsset textAsset})
                        {
                            OnLoadMetaData(textAsset.bytes);
                        }
                        else
                        {
                            LoggerUtils.LogError("Url数据为空:" + MaterialInfo.id);
                            GameController.ExitGame("Url数据为空:" + MaterialInfo.id);
                        }
                    }

                }
                else
                {
                    // 读远端数据
                    LoadRemoteMetaData(MaterialInfo.metaDataUrl);
                }
            }
        }

        protected override bool OnLoadMetaData(byte[] metaDataBytes)
        {
            if (metaDataBytes == null)
            {
                LoggerUtils.LogError("元数据为空:" + MaterialInfo.id);
                GameController.ExitGame("元数据为空:" + MaterialInfo.id);
                return false;
            }
            UGCMaterialData materialData = JsonConvert.DeserializeObject<UGCMaterialData>(metaDataBytes.ToStr());
            Debug.Log("解析的材质数据:" + materialData);

            PreloadPhoto(materialData.parts, (failCount) =>
            {
                UIAgentManager.Inst.ClosePanel(WindowId.CommonWindow,PanelId.UgcLoadingPanel);
                if (failCount == 0)
                {
                    UIAgentManager.Inst.OpenPanel(PanelId.UGCMaterialEditorPanel,materialData, MaterialInfo);
                }
                else
                {
                    GameController.ExitGame("材质模版图片下载失败");
                }
            });
            return true;
        }
    }
}
