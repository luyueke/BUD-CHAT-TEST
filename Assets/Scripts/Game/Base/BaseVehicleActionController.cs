using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;

namespace Game.Base
{
    public class BaseVehicleActionController : BaseEnterModelController
    {
        protected VehicleInfo _vehicleInfo;

        public virtual void Start(VehicleInfo vehicleInfo)
        {
            if (!isInitialized)
            {
                Init();
            }

            this._vehicleInfo = vehicleInfo;
        }
    }
}