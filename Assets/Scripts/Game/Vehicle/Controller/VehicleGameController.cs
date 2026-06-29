using Game.Audio;
using GameData.BaseInfo;
using Message;
using UnityEngine;

/// <summary>
/// 用于联机状态下的房间内其他玩家载具行为管理，和数据同步管理
/// 包括鸣笛、移动、跳跃等行为
/// </summary>
public class VehicleGameController : IGameMono{

    private VehicleGameInfo vehicleGameInfo;
    private VehicleAudioInfo vehicleAudioInfo;
    private bool isSelfVehicle = false;
    private bool isPgcVehicle = false;
    private float audioCoolTime = 1f; //鸣笛冷却时间
    private float audioTime = 0f; //鸣笛时间
    private bool isHonking = false;

    public string DriverUid => vehicleGameInfo.driver;
    public string VehicleId => vehicleGameInfo.vehicleId;
    public bool IsPgcVehicle => isPgcVehicle;
    public VehicleInfo VehicleInfo => vehicleGameInfo.vehicleInfo;

    public VehicleGameController(
        VehicleGameInfo vehicleGameInfo, 
        VehicleAudioInfo vehicleAudioInfo, 
        bool isSelfVehicle,
        bool isPgcVehicle
    ){
        this.vehicleGameInfo = vehicleGameInfo;
        this.vehicleAudioInfo = vehicleAudioInfo;
        this.isSelfVehicle = isSelfVehicle;
        this.isPgcVehicle = isPgcVehicle;
        Init();
    }

    public void Init()
    {
        StateEventManager.Inst.RegisterStateEvent<float, float>(vehicleGameInfo.driver, StateEvent.MoveJoystick, TriggerVehicleMoving);
        StateEventManager.Inst.RegisterStateEvent(vehicleGameInfo.driver, StateEvent.JumpBtn, TriggerVehicleJump);
    }

    public void Release()
    {
        StateEventManager.Inst.UnRegisterStateEvent<float, float>(vehicleGameInfo.driver, StateEvent.MoveJoystick, TriggerVehicleMoving);
        StateEventManager.Inst.UnRegisterStateEvent(vehicleGameInfo.driver, StateEvent.JumpBtn, TriggerVehicleJump);
    }

    public void AddPassenger(string uid){
        vehicleGameInfo.AddPassenger(uid);
    }

    public void AddPassengerOnPos(string uid, int pos){
        vehicleGameInfo.AddPassengerOnPos(uid, pos);
    }

    public void RemovePassenger(string uid){
        vehicleGameInfo.RemovePassenger(uid);
    }

    public bool isPlayerInVehicle(string uid){
        return vehicleGameInfo.passengers.Contains(uid);
    }

    public string[] GetPassengers(){
        return vehicleGameInfo.passengers.ToArray();
    }

    public void FixedUpdate()
    {
        
    }

    public void Update()
    {
        //UpdateVehicleHonking();
        if(isHonking){
            audioTime += Time.deltaTime;
            if(audioTime >= audioCoolTime){
                isHonking = false;
                audioTime = 0f;
                MessageHelper.Broadcast(MessageName.OnVehicleHonkingStop, DriverUid);
            }
        }
    }

    //长鸣笛需求改短鸣笛了
    // private void UpdateVehicleHonking()
    // {
    //     if(vehicleAudioInfo == null){
    //         LoggerUtils.LogError("UpdateVehicleHonking vehicleAudioInfo is null");
    //         return;
    //     }

    //     if(vehicleAudioInfo.IsHonking){

    //         if(vehicleAudioInfo.AudioTimer >= audioKeepTime){
    //             MessageHelper.Broadcast(MessageName.OnVehicleHonkingStop, DriverUid);
    //             vehicleAudioInfo.ResetHonking();
    //             return;
    //         }

    //         vehicleAudioInfo.AddTime(Time.deltaTime);
    //     }
    // }

    public void TriggerVehicleMoving(float axisForward, float axisRight){

        //if(isSelfVehicle){
        //    return; //自己的就本地触发
        //}

        if(axisForward == 0 && axisRight == 0){
            if(vehicleAudioInfo.IsPlayingMoving){
                vehicleAudioInfo.ResetMoving();
                MessageHelper.Broadcast(MessageName.OnVehicleMovingSoundStop, DriverUid);
                //AkSoundManager.Inst.StopSound(vehicleAudioInfo.VehicleAudio, vehicleGameInfo.vehicleObject);
                //To-do 播放其他载具的停止动画
            }
            return;
        }else{
            if(!vehicleAudioInfo.IsPlayingMoving){
                vehicleAudioInfo.TriggerMoving();
                MessageHelper.Broadcast(MessageName.OnVehicleMovingSoundStart, DriverUid);
                //AkSoundManager.Inst.PlayInteractable3DSound(vehicleAudioInfo.VehicleAudio, vehicleGameInfo.vehicleObject);
            }
        }
        
    }

    public void TriggerVehicleJump(){
        if(isSelfVehicle){
            return; //自己不用触发载具跳跃音频
        }
    }

    public void TriggerVehicleHonking()
    {
        //if(isSelfVehicle){
        //    return; //自己不用触发联机鸣笛效果,本地效果即可
        //}
        
        // if(vehicleAudioInfo == null){
        //     LoggerUtils.LogError("TriggerVehicleHonking vehicleAudioInfo is null");
        //     return;
        // }

        // if(!vehicleAudioInfo.IsHonking){
        //     vehicleAudioInfo.ResetHonking();
        //     //AkSoundManager.Inst.StopSound(vehicleAudioInfo.VehicleAudio, vehicleGameInfo.vehicleObject);
        //     //AkSoundManager.Inst.PlayInteractable3DSound(vehicleAudioInfo.VehicleAudio, vehicleGameInfo.vehicleObject);
        //     MessageHelper.Broadcast(MessageName.OnVehicleHonkingStop, DriverUid);
        //     MessageHelper.Broadcast(MessageName.OnVehicleHonkingStart, DriverUid);
        // }

        // vehicleAudioInfo.TriggerHonking();

        if(isHonking){
            return;
        }   

        isHonking = true;
        audioTime = 0f;

        MessageHelper.Broadcast(MessageName.OnVehicleHonkingStart, DriverUid);

    }

}