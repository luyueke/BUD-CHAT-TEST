using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Es;
using Game.COSXML;
using GameData.BaseInfo;
using UGCAsset;
using UGCAsset.Draft;
using UnityEngine;

public class VehicleAssetManager : UGCVehicleAssetManager<VehicleAssetManager>
{
    public override string GetTemplateCover(string templateId = null)
    {
        VehicleTemplate template = null;
        if (string.IsNullOrEmpty(templateId))
        {
            template = DataTables.GetVehicleTemplateList().FirstOrDefault();
        }
        else
        {
            template = DataTables.GetVehicleTemplate(templateId);
        }
        return CosXmlUploadManager.GetBusinessRootUrl() + "/" + template?.RemoteCover;
    }
}
