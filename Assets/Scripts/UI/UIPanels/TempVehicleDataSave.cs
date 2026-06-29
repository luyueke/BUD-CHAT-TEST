using System.IO;
using GameData.BaseInfo;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class TempVehicleDataSave
{
    #if UNITY_EDITOR
    //存入Assets/SaveGame/中
    private static string JsonFilePath => Path.Combine(Application.dataPath, "SaveGame/temp_vehicle.json");
    private static string MetaDataFilePath => Path.Combine(Application.dataPath, "SaveGame/temp_vehicle_meta.bytes");

    // 用于 runtime 加载的 file:// URL
    private static string MetaDataUrl => "file://" + MetaDataFilePath;

    public static void SaveTempVehicleInfo(VehicleInfo info)
    {
        if (info == null)
        {
            Debug.LogError("VehicleInfo is null, cannot save.");
            return;
        }

        try
        {
            // 1. 保存 metaData 到单独的 .bytes 文件
            if (info.metaData != null && info.metaData.Length > 0)
            {
                File.WriteAllBytes(MetaDataFilePath, info.metaData);
                Debug.Log($"[TempVehicleDataSave] Saved meta data to: {MetaDataFilePath}");
                
                // 2. 更新 info 中的 metaDataUrl 指向本地文件
                info.metaDataUrl = MetaDataUrl;
            }
            else
            {
                Debug.LogWarning("[TempVehicleDataSave] info.metaData is null or empty, skipping .bytes file save.");
            }

            // 3. 保存 VehicleInfo 到 .json 文件
            // 不需要自定义 ContractResolver 了，因为 metaData 字段本身被 [JsonIgnore] 忽略，
            // 而我们现在希望它被忽略（不需要存入 json），只需要 URL。
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };

            string json = JsonConvert.SerializeObject(info, settings);
            File.WriteAllText(JsonFilePath, json);
            
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            Debug.Log($"[TempVehicleDataSave] Saved temporary vehicle info to: {JsonFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TempVehicleDataSave] Failed to save vehicle info: {e.Message}");
        }
    }

    public static VehicleInfo GetTempVehicleInfo()
    {
        if (!File.Exists(JsonFilePath))
        {
            Debug.LogWarning($"[TempVehicleDataSave] File not found at: {JsonFilePath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(JsonFilePath);
            
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            VehicleInfo info = JsonConvert.DeserializeObject<VehicleInfo>(json, settings);
            
            // 如果本地存在 metaData 文件，且 metaDataUrl 匹配，我们不需要在这里读取 byte[] 
            // 因为业务逻辑会通过 Asset.LoadRemoteAssetAsync(info.metaDataUrl) 去加载。
            // 但为了方便调试或某些非异步逻辑，如果需要立即获取，可以手动读取：
            // if (File.Exists(MetaDataFilePath)) { ... }
            
            // 确保 URL 是正确的本地路径（防止移动工程目录后路径失效，重新生成一遍）
            // 但如果保存时已经是绝对路径，这里可能需要注意。
            // 简单起见，这里假设读取时总是指向当前的 Application.dataPath
            info.metaDataUrl = MetaDataUrl; 

            return info;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TempVehicleDataSave] Failed to load vehicle info: {e.Message}");
            return null;
        }
    }
    #endif
}
