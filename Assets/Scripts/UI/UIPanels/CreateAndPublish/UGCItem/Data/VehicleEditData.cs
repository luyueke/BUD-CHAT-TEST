using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using UGCAsset.Draft;
using UI;
using UnityEngine;

public class VehicleEditData : BasePropEditData
{

    public VehicleDraftInfo skinActionDraftInfo;

    public override UgcBaseInfo GetInfo()
    {
        return skinActionDraftInfo.vehicleInfo;
    }

    public VehicleInfo GetVehicleInfo()
    {
        return skinActionDraftInfo.vehicleInfo;
    }

    public void SetInstrumentDetailInfo(VehicleDetailInfo info)
    {
        skinActionDraftInfo.vehicleInfo.detailInfo = info;
    }

    public VehicleDetailInfo GetInstrumentDetailInfo()
    {
        return skinActionDraftInfo.vehicleInfo.detailInfo;
    }
}
