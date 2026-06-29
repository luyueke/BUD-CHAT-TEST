using System;
using System.Collections.Generic;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Network.Http;
using UnityEngine;

namespace Game.AssetToolBox
{
    public class PublishAssetBaseDataLoader : MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";

        public void ResetCookie()
        {
            _cookie = "";
            _isEnd = false;
        }

        public virtual void GetDatas(Action<List<PropResInfo>> onResult)
        {
        }

        public virtual void GetPublishList(Action<bool, List<PropResInfo>> resultAction)
        {

        }
    }
    
    //public class ToolBoxDataRsp : HttpPageBaseData
    //{
    //    public List<ToolBoxItemData> list;
    //}

    //public class ToolBoxItemData
    //{
    //    public PropInfo ugcInfo;
    //    public BaseInteractInfo interactInfo;
    //}
}
