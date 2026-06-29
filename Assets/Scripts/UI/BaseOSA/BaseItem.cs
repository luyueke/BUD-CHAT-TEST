// @Author: YangJie
// @Description:
// @Date:  2023/09/12
// @Modify:

using System;
using Com.TheFallenGames.OSA.Util.IO;
using UnityEngine;

namespace UI.BaseOSA
{
    public class BaseItem<T> : MonoBehaviour where T: BaseData
    {
        protected T itemData;
        public RemoteImageBehaviour remoteImageBehaviour;
        protected Action<T> selectedCallBack;
        
        
        public void SetSelectedCallBack(Action<T> callBack)
        {
            selectedCallBack += callBack;
        }

        public virtual void Init(T data)
        {
            itemData = data;
        }
    }
}