using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.KinematicCharacter;
using Game.Vehicle.PGCVehicle.KVC;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Vehicle.PGCVehicle
{

    public enum PGCVehicleUIUsageType{
        Hall,
        FittingRoom,
        Profile,
    }
    /// <summary>
    /// PGC载具基础控制器框架
    /// 负责管理乘客座位分配，并提供基础的输入接口供子类实现物理逻辑
    /// </summary>
    public class PGCVehicleManager : MonoBehaviour
    {

        private Dictionary<string, PGCVehicleController> vehicleControllers = new Dictionary<string, PGCVehicleController>();

        private Dictionary<PGCVehicleUIUsageType, GameObject> currentUIPGCVehicles = new Dictionary<PGCVehicleUIUsageType, GameObject>();
        private Dictionary<PGCVehicleUIUsageType, PlayerAnimationCtrl[]> uiPlayerAnimationCtrls = new Dictionary<PGCVehicleUIUsageType, PlayerAnimationCtrl[]>();
        private Vector3 uiPlayerOriginalPosition;
        
        private const string DetectTriggerName = "rayUseVehicleObj";

        private static PGCVehicleManager _inst;
        public static PGCVehicleManager Inst
        {
            get
            {
                if (_inst == null)
                {
                    LoggerUtils.Log("PGCVehicleManager Create");
                    _inst = new GameObject("PGCVehicleManager").AddComponent<PGCVehicleManager>();
                    DontDestroyOnLoad(_inst.gameObject);
                }
                return _inst;
            }
        }

        protected virtual void Start()
        {
            GameObject vehicleController = new GameObject("PGCVehicleController");
            vehicleController.GetInstanceID();
        }

        /// <summary>
        /// 当前是否有游戏场景中的活跃载具（非 UI 预览载具）。
        /// FittingRoom 等 UI 场景在调用 Release() 前应检查此属性，避免销毁游戏载具。
        /// </summary>
        public bool HasActiveGameVehicles => vehicleControllers.Count > 0;

        public void Release()
        {
            foreach(Transform vehicle in transform)
            {
                GameObject.Destroy(vehicle.gameObject);
            }
            foreach(var vehicleController in vehicleControllers)
            {
                vehicleController.Value.Release();
            }
            vehicleControllers.Clear();
        }

        public void CreatePGCVehicle(int pgcId, PlayerStateController caller, Action<PGCVehicleController> callback, bool isReconstruction = false)
        {
            var pgcVehicleConfig = DataTables.GetPgcVehicleConfig(pgcId);
            if (pgcVehicleConfig == null)
            {
                LoggerUtils.LogError("PGCVehicleConfig not found for id: " + pgcId);
                callback?.Invoke(null);
                return;
            }
            
            var vehicleSource = Loader.Load<GameObject>(pgcVehicleConfig.loadPath, caller.gameObject);
            GameObject vehicleObject = GameObject.Instantiate(vehicleSource,transform);
            vehicleObject.layer = LayerMask.NameToLayer("Model");
            Vector3 offset = caller.transform.forward * 2;
            vehicleObject.transform.SetPositionAndRotation(caller.transform.position + offset, caller.transform.rotation);
            if (vehicleObject == null)
            {
                LoggerUtils.LogError("PGCVehicle prefab load fail, path: " + pgcVehicleConfig.loadPath);
                callback?.Invoke(null);
                return;
            }

            // 优先使用Prefab自带的Controller（避免重复AddComponent导致丢引用/逻辑重复）
            PGCVehicleController vehicleController = vehicleObject.GetComponent<PGCVehicleController>();
            if (vehicleController == null)
            {
                vehicleController = vehicleObject.AddComponent<PGCVehicleController>();
            }
            KinematicCharacterMotor motor = vehicleObject.GetComponentInChildren<KinematicCharacterMotor>();
            if(motor == null){
                motor = vehicleObject.AddComponent<KinematicCharacterMotor>();
            }
            PGCKinematicVehicleController vKvc = vehicleObject.GetComponentInChildren<PGCKinematicVehicleController>();
            if (vKvc == null)
            {
                vKvc = vehicleObject.AddComponent<PGCKinematicVehicleController>();
            }
            Animator animator = vehicleObject.GetComponentInChildren<Animator>();
            PGCVehicleAnimCtrl animCtrl = animator.GetComponent<PGCVehicleAnimCtrl>();
            if (animCtrl == null)
            {
                animCtrl = animator.gameObject.AddComponent<PGCVehicleAnimCtrl>();
            }
            animCtrl.Init(caller.PlayerAnimCtrl);
            animCtrl.LoadVehicleAni(pgcVehicleConfig);

            vKvc.InitWithConfig(pgcVehicleConfig, caller.IsSelf, isReconstruction);

            vehicleController.Init(pgcVehicleConfig, caller.PlayerID);
            //if (vehicleControllers == null || vehicleControllers.Count == 0)
            //{
            //    callback?.Invoke(vehicleController);
            //    return;
            //}
            vehicleControllers[caller.PlayerID] = vehicleController;
            vKvc.CameraTarget = caller.PlayerKCCtrl.CameraTarget;

            callback?.Invoke(vehicleController);
            if (vehicleObject.transform.Find("root") != null)
            {
                vehicleObject.transform.Find("root").transform.localScale = Vector3.one * 1.7f;
            }

            //给载具添加一个触发器用于双人载具的上车检测
            if(!AccountDataManager.Inst.IsMySelf(caller.PlayerID) && pgcVehicleConfig.type == VehicleType.Double.ToString())
            {
                var vehicleDetectable = caller.transform.Find("rayUseVehicleObj");
                if(vehicleDetectable == null)
                {
                   vehicleDetectable = new GameObject(DetectTriggerName).transform;
                }
                vehicleDetectable.SetParent(caller.transform);
                vehicleDetectable.localPosition = new Vector3(0, 0, 0);
                vehicleDetectable.localRotation = Quaternion.identity;
                vehicleDetectable.localScale = Vector3.one;
                vehicleDetectable.gameObject.layer = LayerMask.NameToLayer("Model");
                var trigger = vehicleDetectable.gameObject.AddComponent<BoxCollider>();
                trigger.size = new Vector3(0.2f, 0.2f, 0.2f);
                trigger.isTrigger = true;
            }
        }

        //给UI用的载具创建
        public void CreateUIPGCVehicle(
            int pgcId, 
            Transform parent, 
            PlayerAnimationCtrl[] playerAnimationCtrls, 
            Action<GameObject> callback,
            PGCVehicleUIUsageType usageType = PGCVehicleUIUsageType.FittingRoom
            ){
            if(currentUIPGCVehicles.TryGetValue(usageType, out var currentUIPGCVehicle)){
                GameObject.Destroy(currentUIPGCVehicle);
                currentUIPGCVehicles.Remove(usageType);
            }

            if(playerAnimationCtrls.Length == 0){
                return;
            }
            
            var pgcVehicleConfig = DataTables.GetPgcVehicleConfig(pgcId);
            if (pgcVehicleConfig == null)
            {
                LoggerUtils.LogError("PGCVehicleConfig not found for id: " + pgcId);
                return;
            }

            uiPlayerAnimationCtrls[usageType] = playerAnimationCtrls;


            
            var vehicleSource = Loader.Load<GameObject>(pgcVehicleConfig.loadPath, playerAnimationCtrls[0].gameObject);
            var newUIPGCVehicle = GameObject.Instantiate(vehicleSource);
            currentUIPGCVehicles[usageType] = newUIPGCVehicle;
            newUIPGCVehicle.transform.SetParent(parent);
            newUIPGCVehicle.transform.localPosition = pgcVehicleConfig.fittingRoomPosConfig;
            newUIPGCVehicle.transform.localEulerAngles = new Vector3(0, 0, 0);
            newUIPGCVehicle.transform.localScale = Vector3.one;

            Animator animator = newUIPGCVehicle.GetComponentInChildren<Animator>();
            if(animator == null){
                LoggerUtils.LogError("Animator not found in UIPGCVehicle prefab, path: " + pgcVehicleConfig.loadPath);
                callback?.Invoke(newUIPGCVehicle);
                return;
            }
            PGCVehicleAnimCtrl animCtrl = animator.GetComponent<PGCVehicleAnimCtrl>();
            if (animCtrl == null)
            {
                animCtrl = animator.gameObject.AddComponent<PGCVehicleAnimCtrl>();
            }

            animCtrl.Init(playerAnimationCtrls[0]);
            animCtrl.LoadVehicleAni(pgcVehicleConfig);
            animCtrl.SetPlayerAnimOverride();

            for(int i = 0; i < playerAnimationCtrls.Length; i++){
                animCtrl.SetOtherPlayerAnimOverride(playerAnimationCtrls[i]);
            }

            animCtrl.SetAniState(PGCVehicleAniState.Idle);
            // UI 预览：把所有乘客角色 idle 重置到第 0 帧，与载具 idle 相位对齐，避免出现一前一后的错开
            for (int i = 0; i < playerAnimationCtrls.Length; i++)
                playerAnimationCtrls[i]?.Play("idle", 0, 0f);

            uiPlayerOriginalPosition = playerAnimationCtrls[0].transform.localPosition;

            if(pgcVehicleConfig.seatOffset != null && pgcVehicleConfig.seatOffset.Count > 0){
                for(int i = 0; i < playerAnimationCtrls.Length; i++){
                    if(i >= pgcVehicleConfig.seatOffset.Count){
                        break;
                    }
                    Vector3 offset = pgcVehicleConfig.seatOffset[i];
                    offset.y -= 0.5f; //玩家角色在FittingRoom有0.5f y的偏移
                    newUIPGCVehicle.transform.InverseTransformPoint(offset);
                    playerAnimationCtrls[i].transform.localPosition = offset + pgcVehicleConfig.fittingRoomPosConfig;
                }
            }

            callback?.Invoke(newUIPGCVehicle);
        }

        public PGCVehicleController GetPGCVehicleController(string uid){
            if(vehicleControllers.TryGetValue(uid, out var vehicleController)){
                return vehicleController;
            }
            return null;
        }

        public bool HasHallUIVehicleFor(PlayerAnimationCtrl ctrl)
        {
            if (uiPlayerAnimationCtrls.TryGetValue(PGCVehicleUIUsageType.Hall, out var ctrls) && ctrls != null)
                return Array.IndexOf(ctrls, ctrl) >= 0;
            return false;
        }

        public void SetPlayerAnimOverride(string driverUid){
            if(vehicleControllers.TryGetValue(driverUid, out var vehicleController))
            {
                vehicleController.GetComponentInChildren<PGCVehicleAnimCtrl>().SetPlayerAnimOverride();
            }
        }

        public void RemoveUIPGCVehicle(PGCVehicleUIUsageType usageType = PGCVehicleUIUsageType.FittingRoom, Action callback = null){
            if(currentUIPGCVehicles.TryGetValue(usageType, out var currentUIPGCVehicle)){
                GameObject.Destroy(currentUIPGCVehicle);
                currentUIPGCVehicles.Remove(usageType);
                callback?.Invoke();
            }
            if(uiPlayerAnimationCtrls.TryGetValue(usageType, out var playerAnimationCtrls)){
                //0是默认玩家
                if(playerAnimationCtrls != null 
                && playerAnimationCtrls.Length > 0
                && playerAnimationCtrls[0] != null){
                    playerAnimationCtrls[0].transform.localPosition = uiPlayerOriginalPosition;
                    uiPlayerAnimationCtrls.Remove(usageType);
                }
            }
        }

        public void RemoveAllUIPGCVehicles(Action callback = null){
            foreach(var currentUIPGCVehicle in currentUIPGCVehicles){
                GameObject.Destroy(currentUIPGCVehicle.Value);
            }
            currentUIPGCVehicles.Clear();
            uiPlayerAnimationCtrls.Clear();
            callback?.Invoke();
        }

        public void RemovePGCVehicle(string uid)
        {
            if(vehicleControllers.TryGetValue(uid, out var vehicleController))
            {
                //移除乘客
                vehicleController.RemoveAllPassengers();
                vehicleController.Release();
                GameObject.Destroy(vehicleController.gameObject);
            }
            vehicleControllers.Remove(uid);
        }

        public void EnterDrive(string uid)
        {
            if(vehicleControllers.TryGetValue(uid, out var vehicleController))
            {
                vehicleController.EnterDrive(uid);
            }
        }

        public void CancelDrive(string uid, bool keepInVehicle = true)
        {
            if(vehicleControllers.TryGetValue(uid, out var vehicleController))
            {
                vehicleController.CancelDrive(uid, keepInVehicle);
            }
        }

        public bool EnterPassenger(string uid, string driverUid)
        {
            if(vehicleControllers.TryGetValue(driverUid, out var vehicleController))
            {
                if(!vehicleController.IsHasSeat()){
                    //LoggerUtils.LogError("EnterPassenger - No seat found - " + driverUid);
                    return false;
                }
                vehicleController.EnterPassenger(uid);
                var otherPlayerAnimCtrl = AvatarController.Inst.GetPlayerStateCtrl(uid).PlayerAnimCtrl;
                if(otherPlayerAnimCtrl != null){
                    vehicleController.GetComponentInChildren<PGCVehicleAnimCtrl>().SetOtherPlayerAnimOverride(otherPlayerAnimCtrl);
                }
                return true;
            }
            return false;
        }

        public void CancelPassenger(string uid, string driverUid)
        {
            if(vehicleControllers.TryGetValue(driverUid, out var vehicleController))
            {
                vehicleController.CancelPassenger(uid);
            }
        }

        /// <summary>AI 伙伴上 driverUid 的 PGC 载具乘客位（伙伴在 AIBuddyAvatarController，按 stateCtrl 直接处理）。</summary>
        public bool EnterPassengerForBuddy(string driverUid, PlayerStateController buddyStateCtrl)
        {
            if(buddyStateCtrl == null) return false;
            if(vehicleControllers.TryGetValue(driverUid, out var vehicleController))
            {
                if(!vehicleController.IsHasSeat()){
                    return false;
                }
                vehicleController.EnterPassengerForBuddy(buddyStateCtrl);
                if(buddyStateCtrl.PlayerAnimCtrl != null){
                    vehicleController.GetComponentInChildren<PGCVehicleAnimCtrl>().SetOtherPlayerAnimOverride(buddyStateCtrl.PlayerAnimCtrl);
                }
                return true;
            }
            return false;
        }

        public void CancelPassengerForBuddy(string driverUid, PlayerStateController buddyStateCtrl)
        {
            if(vehicleControllers.TryGetValue(driverUid, out var vehicleController))
            {
                vehicleController.CancelPassengerForBuddy(buddyStateCtrl);
            }
        }
        
        public void TriggerVehicleSkill(string uid, int skillId, bool isPress, string extraJson = null)
        {
            if(vehicleControllers.TryGetValue(uid, out var vehicleController))
            {
                vehicleController.VehicleKinematic.OnSkill(skillId, isPress, extraJson);
            }
        }

        public void ChangeBannerText(string uid,string bannerText)
        {
            if(vehicleControllers.TryGetValue(uid, out var vehicleController))
            {
                vehicleController.VehicleKinematic.ChangeAirBanner(bannerText);
            }
        }
        public void OnUIInit(string uid)
        {

            if(!AccountDataManager.Inst.IsMySelf(uid))
            {
                return;
            }
            
            if(vehicleControllers.TryGetValue(uid, out var vehicleController))
            {
                vehicleController.VehicleKinematic.OnUIInit();
            }
        }

    }
}
