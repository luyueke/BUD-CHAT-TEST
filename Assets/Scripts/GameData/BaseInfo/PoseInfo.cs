using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using UnityEngine;

namespace GameData.BaseInfo
{
    public class PoseInfo : UgcBaseInfo
    {
        public int skinType;// 0 默认用户skin 1 宠物皮肤
        public int poseType; //EmoteSubType
        public string poseData; //姿势信息
        public int isBan;
        public PaymentInfo paymentInfo;
        public int isVip = 0;
    }
}
