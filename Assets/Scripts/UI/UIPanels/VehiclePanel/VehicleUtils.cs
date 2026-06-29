using System.Collections;
using System.Collections.Generic;
using Es;
using GameData.BaseInfo;
using GameData.PgcData;
using UnityEngine;

public static class VehicleUtils
{
    public const string VehicleTemplateId = "80000001";
    public const VehicleType VehicleDefaultType = VehicleType.Single;

    public static VehicleInfo GetDefaultVehicleSkinInfo()
    {
        var skinInfo = new VehicleInfo();
        skinInfo.templateId = VehicleTemplateId;
        skinInfo.vehicleType = (int)VehicleSubType.SingleVehicle;

        return skinInfo;
    }

    public static VehicleDetailInfo GetDefaultVehicleDetailInfo()
    {
        var curDetailInfo = new VehicleDetailInfo
        {
            pDef = Vector3.zero,
            rDef = Vector3.zero,
            sDef = Vector3.one
        };

        return curDetailInfo;
    }

    public static VehicleDetailInfo ConvertVehicleDetailInfo(VehicleDetailInfo info)
    {
        // 因为角色缩放了 1.7倍, 因此值需要缩小 1.7倍率
        var fixScale = 1 ;//  / 1.7f

        if (info == null)
            return GetDefaultVehicleDetailInfo();

        else
        {
            info.pDef = info.pDef * fixScale;
            info.sDef = info.sDef * fixScale;
            return info;
        }
    }


    //public static VehicleDetailInfo GetNowVehicleDetailInfo(GameObject personNode, )
    //{
    //    return null;
    //}
}

