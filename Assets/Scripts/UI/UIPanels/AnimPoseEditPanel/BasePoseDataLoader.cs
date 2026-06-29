using System;
using System.Collections.Generic;
using UnityEngine;

namespace BUD.AnimPose
{
    public abstract class BasePoseDataLoader : MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";
        public void ResetCookie()
        {
            _cookie = "";
            _isEnd = false;
        }
        public abstract void GetDatas(Action<List<QuickPoseData>> resultAction);
    }
}