using System;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Network.Http;
using UnityEngine;

namespace Game.AssetToolBox
{
    public class ToolBoxBaseDataLoader : MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";

        public void ResetCookie()
        {
            _cookie = "";
            _isEnd = false;
        }

        public virtual void GetDatas(Action<List<ToolBoxItemData>> onResult)
        {
        }
    }
    
    public class ToolBoxDataRsp : HttpPageBaseData
    {
        public List<ToolBoxItemData> list;
    }

    public class ToolBoxItemData
    {
        public PropInfo ugcInfo;
        public BaseInteractInfo interactInfo;
    }
}
