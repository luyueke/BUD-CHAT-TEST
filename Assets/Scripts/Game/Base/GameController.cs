// @Author: YangJie
// @Description: 3D场景启动器
// @Date:  2023/07/12
// @Modify:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Audio;
using Game.Scene.EnterModelController;
using Game.Scene.ModeController;
using Game.Store;
using Basic;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.MapData;
using GameData.UGCData;
using GameSync.Manager;
using Message;
using Newtonsoft.Json;
using SceneController.Attribute;
using UIAgent;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Base
{
    public class GameController
    {

        private static readonly Dictionary<EnterGameModel, BaseEnterModelController> enterModelControllers = new Dictionary<EnterGameModel, BaseEnterModelController>();
        private static readonly Dictionary<GameMode, BaseModeController> modeControllers = new Dictionary<GameMode, BaseModeController>();
        private static BaseEnterModelController currentEnterModelController;
        private static BaseModeController currentModeController;
        private static bool isInitialized = false;
        private static bool isExiting = false;//是否正在退房，防止重复退出
        internal static EnterGameModel enterGameModel;

        public const string MapScene = "MapScene";
        public const string HallScene = "GameHall";


        public static bool IsInHallScene()
        {
            return SceneManager.GetActiveScene().name == HallScene;
        }

        public static void StartGame<T>(EnterGameModel model, T tmpInfo, bool isBroadcast = true, string targetScene = MapScene) where T : UgcBaseInfo, new()
        {
            StartGame(model,null,null,tmpInfo,isBroadcast,targetScene);
        }

        //单独封装给地图游玩模式使用，其他模式别使用
        public static void StartGuestGame(MapInfo mapInfo,BaseCreator creator,BaseInteractInfo interactInfo)
        {
            StartGame(EnterGameModel.GuestScene,creator,interactInfo,mapInfo);
        }

        private static void StartGame<T>(EnterGameModel model, BaseCreator creator = null,BaseInteractInfo interactInfo = null,T tmpInfo = null,bool isBroadcast = true, string targetScene = MapScene) where T : UgcBaseInfo, new()
        {
            if (!isInitialized)
            {
                Init();
            }
            if (tmpInfo == null)
            {
                tmpInfo = new T();
            }
            enterGameModel = model;

            GameDataManager.Inst.mapGlobalData.ClearData();
            GameDataManager.Inst.mapGlobalData.originUgcBaseInfo = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(tmpInfo));
            GameDataManager.Inst.mapGlobalData.curUgcBaseInfo = tmpInfo;
            GameDataManager.Inst.mapGlobalData.Creator = creator;
            GameDataManager.Inst.mapGlobalData.InteractInfo = interactInfo;

            if(isBroadcast) MessageHelper.Broadcast(MessageName.StartEnterGame, enterGameModel, tmpInfo);
            if (string.IsNullOrEmpty(targetScene))
            {
                if (enterModelControllers.TryGetValue(enterGameModel, out currentEnterModelController))
                {
                    currentEnterModelController.Start(tmpInfo);
                    MessageHelper.Broadcast(MessageName.OverEnterGame, enterGameModel);
                }
                else
                {
                    LoggerUtils.LogError("对应的创建模式未找到:" + enterGameModel);
                    ExitGame("对应的创建模式未找到:" + enterGameModel);
                }
            }
            else
            {
                xasset.Scene.LoadAsync($"Assets/Arts/Scenes/{targetScene}.unity").completed += operation =>
                {
                    if (enterModelControllers.TryGetValue(enterGameModel, out currentEnterModelController))
                    {
                        currentEnterModelController.Start(tmpInfo);
                        MessageHelper.Broadcast(MessageName.OverEnterGame, enterGameModel);
                    }
                    else
                    {
                        LoggerUtils.LogError("对应的创建模式未找到:" + enterGameModel);
                        ExitGame("对应的创建模式未找到:" + enterGameModel);
                    }
                };
            }

            GlobalCameraManager.Inst.AddCameraAudioListener();

        }
        
        public static void StartAIGame<T>(T tmpInfo,EnterGameModel model, bool isBroadcast = true, string targetScene = MapScene,Action<bool> ac = null) where T : UgcBaseInfo, new()
        {
            if (!isInitialized)
            {
                Init();
            }
            enterGameModel = model;
            GameDataManager.Inst.mapGlobalData.ClearData();
            GameDataManager.Inst.mapGlobalData.originUgcBaseInfo = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(tmpInfo));
            GameDataManager.Inst.mapGlobalData.curUgcBaseInfo = tmpInfo;

            if(isBroadcast) 
                MessageHelper.Broadcast(MessageName.StartEnterGame, enterGameModel, new UgcBaseInfo());

            xasset.Scene.LoadAsync($"Assets/Arts/Scenes/{targetScene}.unity").completed += operation =>
            {
                if (enterModelControllers.TryGetValue(enterGameModel, out currentEnterModelController))
                {
                    var AIEnterCtr = currentEnterModelController as AIGameEnterModelController;
                    if (AIEnterCtr != null)
                    {
                        AIEnterCtr.StartAIGame();
                    }
                    MessageHelper.Broadcast(MessageName.OverEnterGame, enterGameModel);
                    ac?.Invoke(true);
                }
                else
                {
                    LoggerUtils.LogError("对应的创建模式未找到:" + enterGameModel);
                    ExitGame("对应的创建模式未找到:" + enterGameModel);
                    ac?.Invoke(false);
                }
            };
        }
        
        public static void StartSkinActionGame(EnterGameModel model, SkinInfo skinInfo, SkinActionInfo skinActionInfo = null)
        {
            StartSkinActionGame(model,null,null,skinInfo,skinActionInfo, true, MapScene);
        }
        
        private static void StartSkinActionGame(EnterGameModel model, BaseCreator creator = null,BaseInteractInfo interactInfo = null,SkinInfo skinInfo = null, SkinActionInfo skinActionInfo = null,bool isBroadcast = true, string targetScene = MapScene)
        {
            if (!isInitialized)
            {
                Init();
            }
            if (skinInfo == null)
            {
                LoggerUtils.LogError("StartSkinActionGame Data Error");
                return;
            }
            enterGameModel = model;

            GameDataManager.Inst.mapGlobalData.ClearData();
            GameDataManager.Inst.mapGlobalData.originUgcBaseInfo = JsonConvert.DeserializeObject<SkinInfo>(JsonConvert.SerializeObject(skinInfo));
            GameDataManager.Inst.mapGlobalData.curUgcBaseInfo = skinInfo;
            GameDataManager.Inst.mapGlobalData.Creator = creator;
            GameDataManager.Inst.mapGlobalData.InteractInfo = interactInfo;
            GameDataManager.Inst.mapGlobalData.skinActionInfo = skinActionInfo;

            if(isBroadcast) MessageHelper.Broadcast(MessageName.StartEnterGame, enterGameModel, skinInfo);
            if (string.IsNullOrEmpty(targetScene))
            {
                if (enterModelControllers.TryGetValue(enterGameModel, out currentEnterModelController))
                {
                    if (currentEnterModelController is BaseSkinActionController)
                    {
                        var baseSkinActionEnterModelController = (BaseSkinActionController)currentEnterModelController;
                        baseSkinActionEnterModelController.Start(skinInfo, skinActionInfo);
                        MessageHelper.Broadcast(MessageName.OverEnterGame, enterGameModel);
                    }
                    else
                    {
                        LoggerUtils.LogError("对应的创建模式未找到:" + enterGameModel);
                        ExitGame("对应的创建模式未找到:" + enterGameModel);
                    }
                }
                else
                {
                    LoggerUtils.LogError("对应的创建模式未找到:" + enterGameModel);
                    ExitGame("对应的创建模式未找到:" + enterGameModel);
                }
            }
            else
            {
                xasset.Scene.LoadAsync($"Assets/Arts/Scenes/{targetScene}.unity").completed += operation =>
                {
                    if (enterModelControllers.TryGetValue(enterGameModel, out currentEnterModelController))
                    {
                        if (currentEnterModelController is BaseSkinActionController)
                        {
                            var baseSkinActionEnterModelController = (BaseSkinActionController)currentEnterModelController;
                            baseSkinActionEnterModelController.Start(skinInfo, skinActionInfo);
                            MessageHelper.Broadcast(MessageName.OverEnterGame, enterGameModel);
                        }
                        else
                        {
                            LoggerUtils.LogError("对应的创建模式未找到:" + enterGameModel);
                            ExitGame("对应的创建模式未找到:" + enterGameModel);
                        }
                    }
                    else
                    {
                        LoggerUtils.LogError("对应的创建模式未找到:" + enterGameModel);
                        ExitGame("对应的创建模式未找到:" + enterGameModel);
                    }
                };
            }
        }
        
        public static void StartVehicleActionGame(EnterGameModel model, VehicleInfo vehicleInfo, BaseCreator creator = null, BaseInteractInfo interactInfo = null, bool isBroadcast = true, string targetScene = MapScene)
        {
            if (!isInitialized)
            {
                Init();
            }
            if (vehicleInfo == null)
            {
                LoggerUtils.LogError("StartVehicleActionGame Data Error");
                return;
            }
            enterGameModel = model;
            GameDataManager.Inst.mapGlobalData.ClearData();
            GameDataManager.Inst.mapGlobalData.originUgcBaseInfo = JsonConvert.DeserializeObject<VehicleInfo>(JsonConvert.SerializeObject(vehicleInfo));
            GameDataManager.Inst.mapGlobalData.curUgcBaseInfo = vehicleInfo;
            GameDataManager.Inst.mapGlobalData.Creator = creator;
            GameDataManager.Inst.mapGlobalData.InteractInfo = interactInfo;
            if (isBroadcast) MessageHelper.Broadcast(MessageName.StartEnterGame, enterGameModel, vehicleInfo);
            if (string.IsNullOrEmpty(targetScene))
            {
                OverVehicleSceneInfo(vehicleInfo);
            }
            else
            {
                xasset.Scene.LoadAsync($"Assets/Arts/Scenes/{targetScene}.unity").completed += operation =>
                {
                    OverVehicleSceneInfo(vehicleInfo);
                };
            }
        }


        private static void OverVehicleSceneInfo(VehicleInfo vehicleInfo)
        {
            if (enterModelControllers.TryGetValue(enterGameModel, out currentEnterModelController))
            {
                if (currentEnterModelController is BaseVehicleActionController)
                {
                    var baseVehicleActionEnterModelController = (BaseVehicleActionController)currentEnterModelController;
                    baseVehicleActionEnterModelController.Start(vehicleInfo);
                    MessageHelper.Broadcast(MessageName.OverEnterGame, enterGameModel);
                }
                else
                {
                    LoggerUtils.LogError("对应的创建模式未找到:" + enterGameModel);
                    ExitGame("对应的创建模式未找到:" + enterGameModel);
                }
            }
            else
            {
                LoggerUtils.LogError("对应的创建模式未找到:" + enterGameModel);
                ExitGame("对应的创建模式未找到:" + enterGameModel);
            }
        }


        private static void Init()
        {
            enterModelControllers.Clear();
            modeControllers.Clear();
            isInitialized = true;
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes().Where(t => t.IsSubclassOf(typeof(BaseEnterModelController)) || t.IsSubclassOf(typeof(BaseModeController))))
                .ToArray();
            foreach (var tmpPipelineType in types)
            {
                var enterModelAttributes = tmpPipelineType.GetCustomAttributes(typeof(EnterModelAttribute), true);
                if (enterModelAttributes.FirstOrDefault(tmp => tmp.GetType() == typeof(EnterModelAttribute)) is EnterModelAttribute enterModelAttribute)
                {
                    foreach (var gameModel in enterModelAttribute.EnterGameModels)
                    {
                        if (!enterModelControllers.ContainsKey(gameModel))
                        {
                            enterModelControllers.Add(gameModel, (BaseEnterModelController)Activator.CreateInstance(tmpPipelineType));
                        }
                    }
                }

                var modelAttributes = tmpPipelineType.GetCustomAttributes(typeof(GameModelAttribute), true);
                if (modelAttributes.FirstOrDefault(tmp => tmp.GetType() == typeof(GameModelAttribute)) is GameModelAttribute modelAttribute)
                {
                    if (!modeControllers.ContainsKey(modelAttribute.GameMode))
                    {
                        modeControllers.Add(modelAttribute.GameMode, (BaseModeController)Activator.CreateInstance(tmpPipelineType));
                    }
                }
            }
        }

        public static void ExitGame() {
            ExitGame(null, true, "");
        }

        public static void ExitGame(Action callBack, bool isBroadcast = true, string err = "")
        {
            if (isExiting)
            {
                LoggerUtils.Log("####重复退出房间");
                return;
            }
            isExiting = true;

            try
            {
                MessageHelper.Broadcast(MessageName.StartExitGame);
                UIAgentManager.Inst.OpenPanel(PanelId.BlackPanel);
                if (ClientManager.HasInstance)
                {
                    ClientManager.Inst.LeaveRoom();
                }

                isInitialized = false;
                currentEnterModelController?.Release();
                currentModeController?.LeaveMode();
                currentEnterModelController = null;
                currentModeController = null;
                if (SceneManager.GetActiveScene().name == HallScene)
                {
                    UIAgentManager.Inst.ClosePanel(WindowId.CommonWindow, PanelId.BlackPanel);
                    callBack?.Invoke();
                    isExiting = false;
                    if (isBroadcast) MessageHelper.Broadcast(MessageName.OverExitGame, enterGameModel, "");
                }
                else
                {
                    xasset.Scene.LoadAsync($"Assets/Arts/Scenes/{HallScene}.unity").completed += operation =>
                    {
                        UIAgentManager.Inst.ClosePanel(WindowId.CommonWindow, PanelId.BlackPanel);
                        callBack?.Invoke();
                        isExiting = false;
                        if (isBroadcast) MessageHelper.Broadcast(MessageName.OverExitGame, enterGameModel, "");
                    };
                }
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("退出房间报错："+e.ToString());
                callBack?.Invoke();
                isExiting = false;
            }

            GlobalCameraManager.Inst.RemoveCameraAudioListener();


        }

        public static void ExitGame(string err)
        {
            ExitGame(null, true, err);
        }
        //重走一波进图流程
        public static void ReEnterGame()
        {
            ExitGame(null, false);
            if (GameDataManager.Inst.mapGlobalData.originUgcBaseInfo == null)
            {
                LoggerUtils.LogError("OriginMapInfo is null, ReEnterGame Failed!!!");
                return;
            }
            StartGame(enterGameModel, GameDataManager.Inst.mapGlobalData.originUgcBaseInfo, false);
        }


        public static T GetModeController<T>(GameMode gameMode) where T : BaseModeController
        {
            if (modeControllers.TryGetValue(gameMode, out var controller))
            {
                return controller as T;
            }
            return null;
        }

        public static T GetEnterModelController<T>(EnterGameModel model) where T : BaseEnterModelController
        {
            if (enterModelControllers.TryGetValue(model, out var controller))
            {
                return controller as T;
            }
            return null;
        }

        public static EnterGameModel GetEnterGameModel()
        {
            return enterGameModel;
        }

        public static bool IsGame()
        {
            if (currentEnterModelController == null)
            {
                return false;
            }
            if (currentEnterModelController is EditEnterModelController ||
                currentEnterModelController is GuestEnterModelController ||
                currentEnterModelController is PublishTestEnterModelController ||
                currentEnterModelController is UgcPropEnterModelController
                )
            {
                return true;
            }
            return false;
        }

        public static void ChangeMode(GameMode gameMode, Action callBack = null)
        {
            if (modeControllers.TryGetValue(gameMode, out var controller))
            {
                if (controller == currentModeController)
                {
                    return;
                }
                currentModeController?.LeaveMode();
                currentModeController = controller;
                currentModeController.EnterMode();
                callBack?.Invoke();
            }
        }

        public static GameMode? GetCurrentGameMode()
        {
            foreach (var item in modeControllers)
            {
                if (item.Value == currentModeController)
                {
                    return item.Key;
                }
            }

            return null;
        }

        internal static BaseModeController GetCurrentModeController()
        {
            return currentModeController;
        }
        
        public static T GetEnterModelController<T>() where T : BaseEnterModelController
        {
            return currentEnterModelController as T;
        }
    }
}
