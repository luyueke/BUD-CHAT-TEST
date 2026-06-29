using System;

//todo:fsc 暂不考虑复杂的数据结构，json 仅存id
[Serializable]
public class GameDurationData
{
    public int duration = 0; //倒计时时长 单位Seconds. 0:无限时
}