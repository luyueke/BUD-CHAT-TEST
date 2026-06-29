using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;
using GameData.Base;
using BUD.AnimPose;

public class VehicleDataManager : GlobalInstance<VehicleDataManager>
{
    
    public VehicleData SingleVehicleData = new VehicleData();//单人的载具数据

    public VehicleData DoubleVehicleData = new VehicleData();//第二人的载具数据


    /// <summary>
    /// 是否选中编辑驾驶位置
    /// </summary>
    public bool IsSelectDrivePos
    {
        get; set;
    }

    /// <summary>
    /// 是否驾驶载具(游戏内使用)
    /// </summary>
    public bool IsDriveVehicle
    {
        get; set;
    }

}

public class VehicleData
{
    //驾驶姿势
    public KeyFrameData keyFrameData;


}

