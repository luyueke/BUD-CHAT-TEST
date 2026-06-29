using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIAgent
{
    /// <summary>
    /// 业务反向调用Game程序集，目前支持
    /// - 创建素材
    /// </summary>
    public class GameAgentManager : GlobalInstance<GameAgentManager>
    {
        public delegate GameObject CreatePropEvent(string id, string url,Action<GameObject> callBack);
        public delegate GameObject CreatePropOfflineEvent(string id, string url, Action<GameObject> callBack);
        public delegate bool IsInHallSceneEvent();


        public event CreatePropEvent CreatePropEventHandler;
        public event CreatePropOfflineEvent CreatePropOfflineEventHandler;
        public event IsInHallSceneEvent IsInHallSceneHandler;

        public void ClearHandler()
        {
            CreatePropEventHandler = null;
            CreatePropOfflineEventHandler = null;
            IsInHallSceneHandler = null;
        }

        public GameObject CreateProp(string id, string url, Action<GameObject> callBack)
        {
            if (CreatePropEventHandler == null)
            {
                return null;
            }

            return CreatePropEventHandler.Invoke(id,url, callBack);
        }


        public GameObject CreatePropWithOffline(string id, string url, Action<GameObject> callBack)
        {
            if (CreatePropOfflineEventHandler == null)
            {
                return null;
            }

            return CreatePropOfflineEventHandler.Invoke(id,url, callBack);
        }

        public bool IsInHallScene()
        {
            if (IsInHallSceneHandler == null)
                return false;

            return IsInHallSceneHandler.Invoke();
        }


        public override void Release()
        {
            base.Release();
            CreatePropEventHandler = null;
        }


    }
}
