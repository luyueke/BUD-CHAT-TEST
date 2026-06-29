using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;


/// <summary>
/// 载具在游戏内的的包裹类
/// </summary>
public class VehicleGameInfo{
    public string vehicleId;
    public bool isPgc; // 是否是PGC
    public GameObject vehicleObject;
    public VehicleInfo vehicleInfo;
    public string driver;
    public List<string> passengers; //乘客列表,第一版双人载具只支持二位乘客,如果有需要可以扩展
    public VehicleAudioInfo vehicleAudioInfo;

    public VehicleGameInfo(
        string vehicleId, 
        GameObject vehicleObject,  
        bool isPgc, 
        VehicleInfo vehicleInfo, 
        string driver, 
        int passengers
    ){
        this.vehicleId = vehicleId;
        this.vehicleObject = vehicleObject;
        this.vehicleInfo = vehicleInfo;
        this.isPgc = isPgc;
        this.driver = driver;
        this.passengers = new List<string>(passengers);
    }

    public void Release(){
        this.vehicleId = "";
        this.vehicleObject = null;
        this.vehicleInfo = null;
        this.driver = "";
        this.passengers.Clear();
    }

    /// <summary>
    /// 添加乘客
    /// </summary>
    /// <param name="uid"></param>
    public void AddPassenger(string uid){
        if(passengers.Contains(uid)){
            return;
        }
        passengers.Add(uid);
    }

    /// <summary>
    /// 添加乘客到指定位置
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="pos"></param>
    public void AddPassengerOnPos(string uid, int pos){
        if(pos < 0 || pos >= passengers.Count){
            return;
        }
        if(passengers.Contains(uid)){
            return;
        }
        passengers.Insert(pos, uid);
    }

    public void RemovePassenger(string uid){
        if(passengers.Contains(uid)){
            passengers.Remove(uid);
        }
    }
}