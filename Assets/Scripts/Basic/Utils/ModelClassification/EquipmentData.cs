using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Text;

[Serializable]
/// <summary>
/// 设备参数
/// </summary>
public class EquipmentData
{
    public EquipmentData()
    {
        modelName = SystemInfo.deviceModel;
        memory = SystemInfo.systemMemorySize / 1000f;
        cpuName = SystemInfo.processorType;
        cpuCount = SystemInfo.processorCount;
        cpuFrequency = SystemInfo.processorFrequency / 1000f;
        screen = Math.Max(UnityEngine.Screen.width, UnityEngine.Screen.height);
        graphicsName = SystemInfo.graphicsDeviceName;
        graphicsMemory = SystemInfo.graphicsMemorySize / 1000f;
        shaderLevel = SystemInfo.graphicsShaderLevel;
        isMRT = SystemInfo.supportedRenderTargetCount > 1;
        isComputerShader = SystemInfo.supportsComputeShaders;
        supportsGeometryShaders = SystemInfo.supportsGeometryShaders;
        supportsTessellationShaders = SystemInfo.supportsTessellationShaders;
        supportsMultisampledTextures = SystemInfo.supportsMultisampledTextures >= 1;
        isThreadRender = SystemInfo.graphicsMultiThreaded;
        graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion;
        brandName = SystemInfo.deviceName;
    }

    /// <summary>
    /// 设备所属国家
    /// </summary>
    public string countryName=null;

    /// <summary>
    /// 活跃度
    /// </summary>
    public int liveness=0;

    /// <summary>
    /// 品牌名
    /// </summary>
    public string brandName = null;

    /// <summary>
    /// 型号名
    /// </summary>
    public string modelName = null;

    /// <summary>
    /// 内存
    /// </summary>
    public float memory=0;

    /// <summary>
    /// Cpu名
    /// </summary>
    public string cpuName = null;

    /// <summary>
    /// Cpu核数
    /// </summary>
    public int cpuCount=0;

    /// <summary>
    /// Cpu主频
    /// </summary>
    public float cpuFrequency=0;

    /// <summary>
    /// 屏幕分辨率
    /// </summary>
    public int screen=0;

    /// <summary>
    /// 显卡名
    /// </summary>
    public string graphicsName = null;

    /// <summary>
    /// 显存
    /// </summary>
    public float graphicsMemory=0;

    /// <summary>
    /// 着色其功能级别
    /// </summary>
    public int shaderLevel = 0;

    /// <summary>
    /// 是否支持MRT
    /// </summary>
    public bool isMRT = false;

    /// <summary>
    /// 是否支持计算着色器
    /// </summary>
    public bool isComputerShader = false;

    /// <summary>
    /// 是否支持几何着色器
    /// </summary>
    public bool supportsGeometryShaders = false;

    /// <summary>
    /// 是否支持曲面细分
    /// </summary>
    public bool supportsTessellationShaders = false;

    /// <summary>
    /// 是否支持多重采样
    /// </summary>
    public bool supportsMultisampledTextures=false;

    /// <summary>
    /// 是否使用多线程渲染
    /// </summary>
    public bool isThreadRender = false;

    /// <summary>
    /// 显卡驱动版本名
    /// </summary>
    public string graphicsDeviceVersion = null;

    GraphicsTypeEnum graphicsType = GraphicsTypeEnum.None;

    /// <summary>
    /// 显卡类型
    /// </summary>
    public GraphicsTypeEnum GraphicsType
    {
        get
        {
            if (graphicsType == GraphicsTypeEnum.None && !string.IsNullOrEmpty(graphicsDeviceVersion))
            {
                if (graphicsDeviceVersion.StartsWith("OpenGL"))
                {
                    graphicsType = GraphicsTypeEnum.OpenGL;
                }
                else if (graphicsDeviceVersion.StartsWith("Direct3D"))
                {
                    graphicsType = GraphicsTypeEnum.Direct3D;
                }
                else if (graphicsDeviceVersion.StartsWith("Metal"))
                {
                    graphicsType = GraphicsTypeEnum.Metal;
                }
                else if (graphicsDeviceVersion.StartsWith("Vulkan"))
                {
                    graphicsType = GraphicsTypeEnum.Vulkan;
                }
                else
                {
                    graphicsType = GraphicsTypeEnum.Other;
                }
            }
            return graphicsType;
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(string.Format("CountryName={0} [{1}]\n", countryName, "设备所属国家"));
        sb.Append(string.Format("Liveness={0} [{1}]\n", liveness, "活跃度"));
        sb.Append(string.Format("BrandName={0} [{1}]\n", brandName, "品牌名"));
        sb.Append(string.Format("ModelName={0} [{1}]\n", modelName, "型号名"));
        sb.Append(string.Format("Memory={0} [{1}]\n", memory, "内存"));
        sb.Append(string.Format("CpuName={0} [{1}]\n", cpuName, "Cpu名"));
        sb.Append(string.Format("CpuCount={0} [{1}]\n", cpuCount, "Cpu核数"));
        sb.Append(string.Format("CpuFrequency={0} [{1}]\n", cpuFrequency, "Cpu主频"));
        sb.Append(string.Format("Screen={0} [{1}]\n", screen, "屏幕分辨率"));
        sb.Append(string.Format("GraphicsName={0} [{1}]\n", graphicsName, "显卡名"));
        sb.Append(string.Format("GraphicsMemory={0} [{1}]\n", graphicsMemory, "显存"));
        sb.Append(string.Format("ShaderLevel={0} [{1}]\n", shaderLevel, "着色其功能级别"));
        sb.Append(string.Format("IsMRT={0} [{1}] [2]\n", isMRT, "是否支持MRT", SystemInfo.supportedRenderTargetCount));
        sb.Append(string.Format("IsComputerShader={0} [{1}]\n", isComputerShader, "是否支持计算着色器"));
        sb.Append(string.Format("SupportsGeometryShaders={0} [{1}]\n", supportsGeometryShaders, "是否支持几何着色器"));
        sb.Append(string.Format("SupportsTessellationShaders={0} [{1}]\n", supportsTessellationShaders, "是否支持曲面细分"));
        sb.Append(string.Format("SupportsMultisampledTextures={0} [{1}] [{2}]\n", supportsMultisampledTextures, "是否支持多重采样", SystemInfo.supportsMultisampledTextures));
        sb.Append(string.Format("IsThreadRender={0} [{1}]\n", isThreadRender, "是否使用多线程渲染"));
        sb.Append(string.Format("GraphicsDeviceVersion={0} [{1}]\n", graphicsDeviceVersion, "显卡驱动版本名"));
        sb.Append(string.Format("GraphicsType={0} [{1}]\n", GraphicsType.ToString(), "显卡类型"));

        return sb.ToString();
    }
}

[Serializable]
/// <summary>
/// 显卡类型
/// </summary>
public enum GraphicsTypeEnum
{
    None,
    OpenGL,
    Direct3D,
    Metal,
    Vulkan,
    Other,
}

[Serializable]
/// <summary>
/// 机型分级类型
/// </summary>
public enum ModelClassificationType
{
    /// <summary>
    /// 默认情况通过设备分级
    /// </summary>
    Default = 0,
    /// <summary>
    /// 低配
    /// </summary>
    Low = 1,
    /// <summary>
    /// 中配
    /// </summary>
    Middle,
    /// <summary>
    /// 高配
    /// </summary>
    High,
    /// <summary>
    /// 超高
    /// </summary>
    SupeHigh,
}

[Serializable]
/// <summary>
/// 本地结果数据
/// </summary>
public class EquipmentSizingSaveData
{
    /// <summary>
    /// 设备分级结果
    /// </summary>
    public ModelClassificationType modelClassificationType;

    /// <summary>
    /// 版本号
    /// </summary>
    public float VersionNumber = 1.0f;

    static string fileDir => Application.persistentDataPath + "/EquipmentSizing/equipmentSizingData.txt";

    /// <summary>
    /// 存储本地配置
    /// </summary>
    /// <param name="savePath"></param>
    /// <param name="data"></param>
    public static void Save(EquipmentSizingSaveData data)
    {
        try
        {
            string dir = Path.GetDirectoryName(fileDir);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            data.VersionNumber = EquipmentSizing.VersionNumber;
            File.WriteAllText(fileDir, data.ToString());
            LoggerUtils.Log(string.Format("EquipmentSizing Save : {0}", fileDir));
        }
        catch (System.Exception ex)
        {
            LoggerUtils.LogError(string.Format("EquipmentSizing Save Exception: path={0} err={1}", fileDir, ex.Message));
            LoggerUtils.LogError(ex.Message);
        }
    }

    /// <summary>
    /// 读取本地配置
    /// </summary>
    /// <param name="savePath"></param>
    /// <returns></returns>
    public static EquipmentSizingSaveData Read()
    {
        try
        {
            if (File.Exists(fileDir))
            {
                string str = File.ReadAllText(fileDir);
                EquipmentSizingSaveData data = JsonConvert.DeserializeObject<EquipmentSizingSaveData>(str);
                LoggerUtils.Log(string.Format("EquipmentSizing Read : {0}", fileDir));
                return data;
            }
            else
            {
                return null;
            }
        }
        catch (System.Exception ex)
        {
            LoggerUtils.LogError(string.Format("EquipmentSizing Read Exception: path={0} err={1}", fileDir, ex.Message));
            LoggerUtils.LogError(ex.Message);
            return null;
        }
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}


