// @Author: YangJie
// @Description:
// @Date:  2023/07/17
// @Modify:

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.Config;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Utils;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.OfflineRender;
using GameData.PgcData;
using GameData.UGCData;
using Google.Protobuf;
using Pb.Map;
using RTG;
using SceneController.Attribute;
using UGCAsset;
using UGCAsset.Draft;
using UnityEngine;
using static AssetLoader;

namespace Game.Scene.ModeController {
    [GameModel(GameMode.Edit)]
    public class EditModeController : BaseModeController {
        private BudTimer autoSaveTimer;

        public override void EnterMode() {
            base.EnterMode();
            EnterEditMode();

            foreach (var modeManager in GetAllModeManager()) {
                modeManager.OnEdit();
            }

            autoSaveTimer = TimerManager.Inst.Run("AutoSaveMap", 300, 300, AutoSaveTmpData);
			GameTimeUtils.Inst.StartCollect("EditModeController");//开始统计编辑时长
        }

        public override void LeaveMode() {
            base.LeaveMode();
            if (autoSaveTimer != null) {
                TimerManager.Inst.Stop(autoSaveTimer);
                autoSaveTimer = null;
            }

            GameTimeUtils.Inst.StopCollect("EditModeController");//停止统计编辑时长
        }

        private void EnterEditMode() {
            GameCameraUtils.Inst.GetEditVirtualCamera().enabled = true;
            GameCameraUtils.Inst.GetPlayVirtualCamera().enabled = false;
        }

        private void SaveTmpMapInfo(bool isUpload = false, Action<bool> callBack = null) {
            var mapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            var mapData = SceneBuilder.Inst.SaveMapToData();

            var mapDraftInfo = MapAssetManager.Inst.GetOrCreateDraftInfo(mapInfo);
            mapDraftInfo.baseInfo.npcIds = GamePropNodeManager.Inst.GetBehaviours<AIBuddyInMapBehaviour>()
            .Where(tmp => !string.IsNullOrEmpty(tmp.aIBuddyInMapComponent.AiBuddyID))
            .Select(tmp => tmp.aIBuddyInMapComponent.AiBuddyID)
            .ToList();
            mapDraftInfo.baseInfo.detailInfo = GamePropNodeManager.Inst.GetUGCItemDetailInfo();
            mapDraftInfo.baseInfo.propIds = mapData.UgcItemData.ItemDataList.Select(tmp => tmp.UItemId).ToList();
            mapDraftInfo.baseInfo.materialIds = mapData.UgcmatData.MatDataList.Select(tmp => tmp.UmatId).ToList();
            mapDraftInfo.SetMetaData(mapData.ToByteArray());
            if (mapDraftInfo.baseInfo.coverAutoSaved != CoverSaveStatus.ManualSaved) {
                bool isRtEnabled = RTGApp.Get.enabled;
                RTGApp.Get.enabled = false;
                mapDraftInfo.SetCover(ScreenShotUtils.ScreenShot(GameCameraUtils.Inst.GetMainCamera(),
                    GameConsts.UGCMapShotSize));
                RTGApp.Get.enabled = isRtEnabled;
                mapDraftInfo.baseInfo.coverAutoSaved = CoverSaveStatus.AutoSaved;
            }
           	int curEditTime = GameTimeUtils.Inst.RestartCollect("EditModeController");//重启编辑时长
            LoggerUtils.Log("###原编辑总时长："+mapDraftInfo.editTime + "  当次编辑时长："+ curEditTime);
            mapDraftInfo.editTime += curEditTime;

            if (isUpload) {
                mapDraftInfo.UploadAndSave((info, isSuccess) => {
                    callBack?.Invoke(isSuccess);
                });
            } else {
                MapAssetManager.Inst.SaveDraftInfo(mapDraftInfo);
                callBack?.Invoke(true);
            }
        }

        private void SaveTmpSkinInfo(bool isUpload = false, Action<bool> callBack = null) {
            var skinInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
            // 保存素材草稿
            var pUGCItemData = GamePropNodeManager.Inst.SaveUgcItemData();
            var pMapData = SceneBuilder.Inst.SaveMapToData();

            var skinDraftInfo = SkinAssetManager.Inst.GetOrCreateDraftInfo(skinInfo);
            skinDraftInfo.baseInfo.ugcStyle = GetItemUgcStyle(pUGCItemData);
            if (skinDraftInfo.baseInfo.skinDetailInfo == null) {
                skinDraftInfo.baseInfo.skinDetailInfo = SkinDetailInfo.FromDetailInfo(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            } else {
                skinDraftInfo.baseInfo.skinDetailInfo.Assign(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            }

            skinDraftInfo.baseInfo.skinDetailInfo.size = pUGCItemData.Size.ToVector3();


            // 素材元信息
            skinDraftInfo.SetMetaData(pUGCItemData.ToByteArray());
            // 地图元信息
            skinDraftInfo.SetMapMetaData(pMapData.ToByteArray());

            if (skinDraftInfo.baseInfo.coverAutoSaved != CoverSaveStatus.ManualSaved)
            {
                // 截图需要隐藏地板
                var terrain = GlobalNodeManager.Inst.Get<TerrainManager>().GetTerrain();
                terrain.gameObject.SetActive(false);
                bool isRtEnabled = RTGApp.Get.enabled;
                RTGApp.Get.enabled = false;
                skinDraftInfo.SetCover(ScreenShotUtils.ScreenShot(GameCameraUtils.Inst.GetMainCamera(), GameConsts.UGCItemShotSize,  new []{LayerMask.NameToLayer("Terrain")}, true));
                RTGApp.Get.enabled = isRtEnabled;
                terrain.gameObject.SetActive(true);
                skinDraftInfo.baseInfo.coverAutoSaved = CoverSaveStatus.AutoSaved;
            }

            int curEditTime = GameTimeUtils.Inst.RestartCollect("EditModeController");//重启编辑时长
            LoggerUtils.Log("###原编辑总时长："+skinDraftInfo.editTime + "  当次编辑时长："+ curEditTime);
            skinDraftInfo.editTime += curEditTime;

            if (isUpload)
            {
                skinDraftInfo.UploadAndSave((info, isSuccess) => {
                    callBack?.Invoke(isSuccess);
                });
            }
            else
            {
                SkinAssetManager.Inst.SaveDraftInfo(skinDraftInfo);
                callBack?.Invoke(true);
            }
        }

        private void SaveTmpVehicleInfo(bool isUpload = false, Action<bool> callBack = null)
        {
            var vehicleInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<VehicleInfo>();
            // 保存素材草稿
            var pUGCItemData = GamePropNodeManager.Inst.SaveUgcItemData();
            var pMapData = SceneBuilder.Inst.SaveMapToData();
            SetVehicleDetailOffset(vehicleInfo);
            var skinDraftInfo = VehicleAssetManager.Inst.GetOrCreateDraftInfo(vehicleInfo);
            //skinDraftInfo.baseInfo.ugcStyle = GetItemUgcStyle(pUGCItemData);
            if (skinDraftInfo.vehicleInfo.detailInfo == null)
            {
                skinDraftInfo.vehicleInfo.detailInfo = VehicleDetailInfo.FromDetailInfo(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            }
            else
            {
                skinDraftInfo.vehicleInfo.detailInfo.Assign(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            }

            skinDraftInfo.vehicleInfo.detailInfo.size = pUGCItemData.Size.ToVector3();


            // 素材元信息
            skinDraftInfo.SetMetaData(pUGCItemData.ToByteArray());
            // 地图元信息
            skinDraftInfo.SetMapMetaData(pMapData.ToByteArray());

            if (skinDraftInfo.vehicleInfo.coverAutoSaved != CoverSaveStatus.ManualSaved)
            {
                // 截图需要隐藏地板
                var terrain = GlobalNodeManager.Inst.Get<TerrainManager>().GetTerrain();
                terrain.gameObject.SetActive(false);
                bool isRtEnabled = RTGApp.Get.enabled;
                RTGApp.Get.enabled = false;
                skinDraftInfo.SetCover(ScreenShotUtils.ScreenShot(GameCameraUtils.Inst.GetMainCamera(), GameConsts.UGCItemShotSize, new[] { LayerMask.NameToLayer("Terrain") }, true));
                RTGApp.Get.enabled = isRtEnabled;
                terrain.gameObject.SetActive(true);
                skinDraftInfo.vehicleInfo.coverAutoSaved = CoverSaveStatus.AutoSaved;
            }

            int curEditTime = GameTimeUtils.Inst.RestartCollect("EditModeController");//重启编辑时长
            LoggerUtils.Log("###原编辑总时长：" + skinDraftInfo.editTime + "  当次编辑时长：" + curEditTime);
            skinDraftInfo.editTime += curEditTime;

            if (isUpload)
            {
                skinDraftInfo.UploadAndSave((info, isSuccess) => {
                    callBack?.Invoke(isSuccess);
                });
            }
            else
            {
                VehicleAssetManager.Inst.SaveDraftInfo(skinDraftInfo);
                callBack?.Invoke(true);
            }
        }

        private void SetVehicleDetailOffset(VehicleInfo vehicleInfo)
        {
            //生成坐标系不同，这里值取反
            var nodeList = GamePropNodeManager.Inst.GetBehaviours<PreviewModelBehaviour>();
            if(nodeList == null || nodeList.Count <= 0)
            {
                return;
            }
            var endPos = nodeList[0].gameObject.transform.position - GamePropNodeManager.Inst.CombineNodePosLogic();
            vehicleInfo.detailInfo.pDef = new Vector3(endPos.x, -endPos.y, endPos.z);
            vehicleInfo.detailInfo.rDef = new Vector3(0, 180, 0);
        }
        private int GetItemUgcStyle(PUGCItemData itemData)
        {
            var ugcStyle = UgcShaderStyle.Normal;
            if (itemData != null && itemData.UgcmatData != null && itemData.UgcmatData.MatDataList != null)
            {
                foreach (var matData in itemData.UgcmatData.MatDataList)
                {
                    if (matData.UmatStyle == (int) UgcShaderStyle.Anime)
                    {
                        ugcStyle = UgcShaderStyle.Anime;
                        break;
                    }
                }
            }
            if (ugcStyle == UgcShaderStyle.Normal)
            {
                ugcStyle = GameUgcMatManager.Inst.GetPGCAnimMatStyle();
            }
            return (int) ugcStyle;
        }

        private void SaveTmpPropInfo(bool isUpload = false, Action<bool> callBack = null) {
            var propInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<PropInfo>();
            // 保存素材草稿
            var pUGCItemData = GamePropNodeManager.Inst.SaveUgcItemData();
            var pMapData = SceneBuilder.Inst.SaveMapToData();
            var propDraftInfo = PropAssetManager.Inst.GetOrCreateDraftInfo(propInfo);
            propDraftInfo.baseInfo.ugcStyle = GetItemUgcStyle(pUGCItemData);
            propDraftInfo.baseInfo.detailInfo = GamePropNodeManager.Inst.GetUGCItemDetailInfo();
            propDraftInfo.baseInfo.detailInfo.size = pUGCItemData.Size.ToVector3();
            // 素材元信息
            propDraftInfo.SetMetaData(pUGCItemData.ToByteArray());
            // 地图元信息
            propDraftInfo.SetMapMetaData(pMapData.ToByteArray());

            if (propDraftInfo.baseInfo.coverAutoSaved != CoverSaveStatus.ManualSaved)
            {
                // 截图需要隐藏地板
                var terrain = GlobalNodeManager.Inst.Get<TerrainManager>().GetTerrain();
                terrain.gameObject.SetActive(false);
                bool isRtEnabled = RTGApp.Get.enabled;
                RTGApp.Get.enabled = false;
                propDraftInfo.SetCover(ScreenShotUtils.ScreenShot(GameCameraUtils.Inst.GetMainCamera(), GameConsts.UGCItemShotSize,  new []{LayerMask.NameToLayer("Terrain")}, true));
                RTGApp.Get.enabled = isRtEnabled;
                terrain.gameObject.SetActive(true);
                propDraftInfo.baseInfo.coverAutoSaved = CoverSaveStatus.AutoSaved;
            }
            int curEditTime = GameTimeUtils.Inst.RestartCollect("EditModeController");//重启编辑时长
            LoggerUtils.Log("###原编辑总时长："+propDraftInfo.editTime + "  当次编辑时长："+ curEditTime);
            propDraftInfo.editTime += curEditTime;

            if (isUpload)
            {
                propDraftInfo.UploadAndSave((info, isSuccess) => {
                    callBack?.Invoke(isSuccess);
                });
            }
            else
            {
                PropAssetManager.Inst.SaveDraftInfo(propDraftInfo);
                callBack?.Invoke(true);
            }
        }
        
        private void SaveTmpMusicalInstrumentInfo(bool isUpload = false, Action<bool> callBack = null) {
            var skinInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
            var skinActionInfo = GameDataManager.Inst.mapGlobalData.skinActionInfo;
            
            // 保存素材草稿
            var pUGCItemData = GamePropNodeManager.Inst.SaveUgcItemData();
            var pMapData = SceneBuilder.Inst.SaveMapToData();
            
            SkinActionBaseDraftInfo skinActionDraftInfo = InstrumentAssetManager.Inst.GetOrCreateDraftInfo(skinInfo, skinActionInfo);
            skinActionDraftInfo._skinInfo.ugcStyle = GetItemUgcStyle(pUGCItemData);
            if (skinActionDraftInfo._skinInfo.skinDetailInfo == null) {
                skinActionDraftInfo._skinInfo.skinDetailInfo = SkinDetailInfo.FromDetailInfo(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            } else {
                skinActionDraftInfo._skinInfo.skinDetailInfo.Assign(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            }

            skinActionDraftInfo._skinInfo.skinDetailInfo.size = pUGCItemData.Size.ToVector3();


            // 素材元信息
            skinActionDraftInfo.SetMetaData(pUGCItemData.ToByteArray());
            // 地图元信息
            skinActionDraftInfo.SetMapMetaData(pMapData.ToByteArray());

            if (skinActionDraftInfo._skinInfo.coverAutoSaved != CoverSaveStatus.ManualSaved)
            {
                // 截图需要隐藏地板
                var terrain = GlobalNodeManager.Inst.Get<TerrainManager>().GetTerrain();
                terrain.gameObject.SetActive(false);
                bool isRtEnabled = RTGApp.Get.enabled;
                RTGApp.Get.enabled = false;
                skinActionDraftInfo.SetCover(ScreenShotUtils.ScreenShot(GameCameraUtils.Inst.GetMainCamera(), GameConsts.UGCItemShotSize,  new []{LayerMask.NameToLayer("Terrain")}, true));
                RTGApp.Get.enabled = isRtEnabled;
                terrain.gameObject.SetActive(true);
                skinActionDraftInfo._skinInfo.coverAutoSaved = CoverSaveStatus.AutoSaved;
            }

            int curEditTime = GameTimeUtils.Inst.RestartCollect("EditModeController");//重启编辑时长
            LoggerUtils.Log("###原编辑总时长："+ skinActionDraftInfo.editTime + "  当次编辑时长："+ curEditTime);
            skinActionDraftInfo.editTime += curEditTime;

            if (isUpload)
            {
                skinActionDraftInfo.UploadAndSave((info, isSuccess) => {
                    callBack?.Invoke(isSuccess);
                });
            }
            else
            {
                InstrumentAssetManager.Inst.SaveDraftInfo(skinActionDraftInfo);
                callBack?.Invoke(true);
            }
        }

        public void SaveData(Action<bool> callBack = null) {
            var curBaseInfo = GameDataManager.Inst.mapGlobalData.curUgcBaseInfo;
            if (curBaseInfo is MapInfo) {
                SaveTmpMapInfo(true, callBack);
            } 
            else if (curBaseInfo is SkinInfo) {
                var skinInfo = (SkinInfo)curBaseInfo;
                switch ((AvatarSubType)skinInfo.subType)
                {
                    case AvatarSubType.MusicalInstrument:
                        SaveTmpMusicalInstrumentInfo(true, callBack);
                        break;
                     
                    default:
                        SaveTmpSkinInfo(true, callBack);
                        break;
                }
            } 
            else if (curBaseInfo is PropInfo) {
                SaveTmpPropInfo(true, callBack);
            }
            else if(curBaseInfo is VehicleInfo)
            {
                SaveTmpVehicleInfo(true, callBack);
            }
        }


        public void AutoSaveTmpData() {
             var curBaseInfo = GameDataManager.Inst.mapGlobalData.curUgcBaseInfo;
             if (curBaseInfo is MapInfo) {
                 SaveTmpMapInfo();
             } 
             else if (curBaseInfo is SkinInfo)
             {
                 var skinInfo = (SkinInfo)curBaseInfo;
                 switch ((AvatarSubType)skinInfo.subType)
                 {
                     case AvatarSubType.MusicalInstrument:
                         SaveTmpMusicalInstrumentInfo();
                         break;
                     
                     default:
                         SaveTmpSkinInfo();
                         break;
                 }
             } 
             else if (curBaseInfo is PropInfo) {
                 SaveTmpPropInfo();
             }
            else if (curBaseInfo is VehicleInfo)
            {
                SaveTmpVehicleInfo();
            }
        }




    }
}
