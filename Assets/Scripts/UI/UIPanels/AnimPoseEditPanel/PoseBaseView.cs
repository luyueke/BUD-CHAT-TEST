using System;
using GameData.BaseInfo;
using GameData.PgcData;
using UnityEngine;

namespace BUD.AnimPose
{
    public class PoseBaseView : MonoBehaviour
    {
        protected UgcPoseSubType curPoseType;
        public virtual void OnCreate()
        {
        }

        public virtual void OnShow(EnterPanelMode panelMode,UgcPoseSubType poseType)
        {
        }
    }
}