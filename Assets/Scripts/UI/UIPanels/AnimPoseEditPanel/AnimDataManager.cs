using System;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;

namespace BUD.AnimPose
{
    public class AnimDataManager : GlobalInstance<AnimDataManager>
    {
        public EnterPanelMode enterMode;
        public AnimInfo animInfo;
        public Dictionary<int, AnimPropData> propDic;
        public PoseInfo animPose;
        public int CurOPFrame;
        public bool isLoop = false;
        public PoseImageType curImageMode = PoseImageType.Current;

        public void ClearData()
        {
            animInfo = null;
            animPose = null;
            propDic = null;
            CurOPFrame = 0;
            isLoop = false;
            curImageMode = PoseImageType.Current;
        }
    }
}