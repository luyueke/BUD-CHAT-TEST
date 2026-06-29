using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using UnityEngine;

namespace Game.Base
{
    public class BaseSkinActionController : BaseEnterModelController
    {
        protected SkinInfo _skinInfo;
        protected SkinActionInfo _skinActionInfo;
        
        public virtual void Start(SkinInfo skinInfo, SkinActionInfo skinActionInfo)
        {
            if (!isInitialized)
            {
                Init();
            }

            this._skinInfo = skinInfo;
            this._skinActionInfo = skinActionInfo;
        }
    }
}
