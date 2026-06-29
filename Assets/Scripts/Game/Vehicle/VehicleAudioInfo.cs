
using GameData.BaseInfo;
using UnityEngine;

public class VehicleAudioInfo{

    private float audioTimer = 0f; //音频计数器
    private string vehicleAudio = ""; //音频路径
    private string vehicleRunningAudio = ""; //运行时音频路径
    private bool isHonking = false; //是否正在播放
    private bool isPlayingMoving = false; //是否正在移动

    public float AudioTimer => audioTimer;
    public string VehicleAudio => vehicleAudio;
    public string VehicleRunningAudio => vehicleRunningAudio;
    public bool IsHonking => isHonking;
    public bool IsPlayingMoving => isPlayingMoving;
    public void Init(string vehicleAudio, string vehicleRunningAudio){
        this.vehicleAudio = vehicleAudio;
        this.vehicleRunningAudio = vehicleRunningAudio;
        audioTimer = 0f;
        isHonking = false;
        isPlayingMoving = false;
    }

    public void AddTime(float time){
        audioTimer += time;
    }

    public void TriggerHonking(){
        isHonking = true;
        audioTimer = 0f;
    }

    public void ResetHonking(){
        audioTimer = 0f;
        isHonking = false;
    }
    
    public void TriggerMoving(){
        isPlayingMoving = true;
    }

    public void ResetMoving(){
        isPlayingMoving = false;
    }

}