using System.Collections.Generic;
using System.Linq;
using Es;
using UI.Preview3D.Base;
using UnityEngine;

namespace UI.Preview3D.Helper
{
    public class PreviewParamsHelper
    {
        // #region Privates
        //
        // private static Vector3 GetVector3BySource(Dictionary<int, Vector3> cfg, Preview3DSource source, Vector3 defaultValue = default)
        // {
        //     if (cfg.TryGetValue((int)Preview3DSource.Any, out var anyPos))
        //     {
        //         return anyPos;
        //     }
        //
        //     if (cfg.TryGetValue((int)source, out var specificPos))
        //     {
        //         return specificPos;
        //     }
        //
        //     return defaultValue;
        // }
        //
        // #endregion
        //
        // public const string DefaultConfigId = "1";
        // public static PreviewParamsConfig DefaultConfig => GetPreviewParams(DefaultConfigId);
        //
        // /// <summary>
        // /// 读取预览配置，没有则默认用第一条
        // /// </summary>
        // /// <param name="id"></param>
        // /// <returns></returns>
        // public static PreviewParamsConfig GetPreviewParams(string id)
        // {
        //     var cfg = DataTables.GetPreviewParamsConfig(id);
        //     cfg ??= DefaultConfig;
        //     return cfg;
        // }
        //
        // public static Vector3 GetModelPreviewLocalPos(PreviewParamsConfig cfg, Preview3DSource source)
        // {
        //     cfg ??= DefaultConfig;
        //     return GetVector3BySource(cfg.ModelLocalPos, source, DefaultConfig.ModelLocalPos.First().Value);
        // }
        //
        // public static Vector3 GetModelPreviewLocalEulerAngles(PreviewParamsConfig cfg, Preview3DSource source)
        // {
        //     cfg ??= DefaultConfig;
        //     return GetVector3BySource(cfg.ModelLocalEulerAngles, source, DefaultConfig.ModelLocalEulerAngles.First().Value);
        // }
        //
        // public static Vector3 GetModelPreviewLocalScale(PreviewParamsConfig cfg, Preview3DSource source)
        // {
        //     cfg ??= DefaultConfig;
        //     return GetVector3BySource(cfg.ModelLocalScale, source, DefaultConfig.ModelLocalScale.First().Value);
        // }
        //
        // public static Vector3 GetCameraPreviewLocalPos(PreviewParamsConfig cfg, Preview3DSource source)
        // {
        //     cfg ??= DefaultConfig;
        //     return GetVector3BySource(cfg.CameraLocalPos, source, DefaultConfig.CameraLocalPos.First().Value);
        // }
        //
        // public static Vector3 GetCameraPreviewLocalEulerAngles(PreviewParamsConfig cfg, Preview3DSource source)
        // {
        //     cfg ??= DefaultConfig;
        //     return GetVector3BySource(cfg.CameraLocalEulerAngles, source, DefaultConfig.CameraLocalEulerAngles.First().Value);
        // }
    }
}