using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

/// <summary>
/// 游戏状态
/// </summary>
public class EquipmentSizingGame
{

    static EquipmentSizingSaveData curEquipmentSizing;

    /// <summary>
    /// 当前设备的分级
    /// </summary>
    public static EquipmentSizingSaveData CurEquipmentSizing
    {
        get
        {
            if (curEquipmentSizing == null)
            {
                Initialize();
                if (curEquipmentSizing==null)
                {
                    curEquipmentSizing = new EquipmentSizingSaveData();
                    curEquipmentSizing.modelClassificationType = ModelClassificationType.Middle;
                }
            }
            return curEquipmentSizing;
        }
    }

    /// <summary>
    /// 设置设备分级，设置后会改变当前的设备分级
    /// </summary>
    /// <param name="modelClassificationType"></param>
    public static void SetEquipmentSizing(ModelClassificationType modelClassificationType)
    {
        CurEquipmentSizing.modelClassificationType = modelClassificationType;
        Save();
    }

    /// <summary>
    /// 设置设备分级，会在下次App启动时生效
    /// </summary>
    /// <param name="modelClassificationType"></param>
    public static void SetEquipmentSizingNextStart(ModelClassificationType modelClassificationType)
    {
        EquipmentSizingSaveData newEquipmentSizing = new EquipmentSizingSaveData();
        newEquipmentSizing.modelClassificationType = ModelClassificationType.Middle;
        EquipmentSizingSaveData.Save(newEquipmentSizing);
    }

    static EquipmentData equipmentData;

    /// <summary>
    /// 当前设备信息
    /// </summary>
    public static EquipmentData EquipmentData
    {
        get
        {
            if (equipmentData==null)
            {
                equipmentData = new EquipmentData();
            }
            return equipmentData;
        }
    }

    /// <summary>
    /// 设备信息Log
    /// </summary>
    public static Dictionary<string, string> equipmentSizingSaveDataInfo = new Dictionary<string, string>();

    /// <summary>
    /// 描述信息刷新
    /// </summary>
    static void FreshEquipmentSizingSaveDataInfo()
    {
        try
        {
            equipmentSizingSaveDataInfo.Clear();
            if (curEquipmentSizing != null)
            {
                equipmentSizingSaveDataInfo.Add("Sizing", curEquipmentSizing.modelClassificationType.ToString());
            }
            else
            {
                equipmentSizingSaveDataInfo.Add("Sizing", "Null");
            }
            Debug.Log("设备信息:" + EquipmentData.ToString());
            //EquipmentSizingSaveDataInfo.Add("Memory/CpuCount/CpuFrequency", EquipmentData.Memory.ToString()+"/"+ EquipmentData.CpuCount.ToString()+"/"+ EquipmentData.CpuFrequency.ToString());
            //EquipmentSizingSaveDataInfo.Add("Screen/ShaderLevel/IsMRT", EquipmentData.Screen.ToString()+"/"+ EquipmentData.ShaderLevel.ToString()+"/"+ EquipmentData.IsMRT.ToString());
            //EquipmentSizingSaveDataInfo.Add("ModelName/BrandName/GraphicsName/CpuName", EquipmentData.ModelName+"/"+ EquipmentData.BrandName+"/" + EquipmentData.GraphicsName+"/"+ EquipmentData.CpuName);
            equipmentSizingSaveDataInfo.Add("M/CC/CF", EquipmentData.memory.ToString() + "/" + EquipmentData.cpuCount.ToString() + "/" + EquipmentData.cpuFrequency.ToString());
            equipmentSizingSaveDataInfo.Add("S/S/MRT", EquipmentData.screen.ToString() + "/" + EquipmentData.shaderLevel.ToString() + "/" + EquipmentData.isMRT.ToString());
            equipmentSizingSaveDataInfo.Add("M/B/G/C", EquipmentData.graphicsName + "/" + EquipmentData.cpuName+"/"+ EquipmentData.modelName + "/" + EquipmentData.brandName);
        }
        catch (System.Exception ex)
        {
            LoggerUtils.LogError(ex.Message);
        }
    }

    static EquipmentSpecialList curEquipmentSpecialList;


    private static bool serverSupportGPU = true;

    public static void SetServerSupportGPU(bool closeGPU)
    {
        serverSupportGPU = !closeGPU;
    }


    public static bool IsSupportOpenGLES30()
    {
        return serverSupportGPU && EquipmentData.shaderLevel >= 35;
    }

    /// <summary>
    /// 特殊机型列表
    /// </summary>
    public static EquipmentSpecialList CurEquipmentSpecialList
    {
        get
        {
            return curEquipmentSpecialList;
        }
    }

    static EquipmentSpecialData curEquipmentSpecialData;

    /// <summary>
    /// 当前特殊机型数据
    /// </summary>
    public static EquipmentSpecialData CurEquipmentSpecialData
    {
        get
        {
            return curEquipmentSpecialData;
        }
    }

    /// <summary>
    /// 玩家可选机型配置功能是否有上架
    /// </summary>
    public static bool userCanSetEquipmentSizing = false;

    public static void WriteTxt(string savePath, string text)
    {
        try
        {
            string dir = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            File.WriteAllText(savePath, text);
        }
        catch (System.Exception ex)
        {
            LoggerUtils.LogError(ex.Message);
        }
    }

    /// <summary>
    /// 特殊机型Url
    /// </summary>
    /// <returns></returns>
    static string GetRuntimeEquipmentSpecialUrl()
    {
        string url = null;
        try
        {
            string environment = "pr";
#if UNITY_EDITOR
#else
            // string environment = GameManager.Inst.baseGameJsonData.baseInfo.environment;
#endif
            if (environment == "master")
            {
                url = EquipmentSizingPath.equipmentSpecialUrl_Master;
            }
            else if (environment == "pr")
            {
                url = EquipmentSizingPath.equipmentSpecialUrl_Alpha;
            }
            else if (environment != null)
            {
                url = EquipmentSizingPath.equipmentSpecialUrl_Prod;
            }
            else
            {
                url = EquipmentSizingPath.equipmentSpecialUrl_Master;
            }
        }
        catch (System.Exception ex)
        {
            LoggerUtils.LogError(ex.Message);
            url = EquipmentSizingPath.equipmentSpecialUrl_Prod;
        }
        return url;
    }

    static bool initialize = false;

    /// <summary>
    /// Unity启动，资源下载之前需要运行此函数
    /// 读取本地存储的分级数据，如果没有会重新计算并存储到本地
    /// </summary>
    /// <param name="equipmentSpecialUrl">特殊机型下载URL</param>
    public static void Initialize()
    {
        try
        {
            if (initialize) return;
            initialize = true;

            // curEquipmentSizing = EquipmentSizingSaveData.Read();
 
            if (curEquipmentSizing == null || curEquipmentSizing.VersionNumber< EquipmentSizing.VersionNumber)
            {
                LoggerUtils.Log("本地机型分析数据为NULL或者版本号过低，进行设备分级创建 ！");
                curEquipmentSizing = new EquipmentSizingSaveData();
                curEquipmentSizing.modelClassificationType = EquipmentSizing.GetCurClassification();
                LoggerUtils.Log(string.Format("设备分级结果={0}", curEquipmentSizing.modelClassificationType.ToString()));
                EquipmentSizingSaveData.Save(curEquipmentSizing);
            }

            // FreshEquipmentSizingSaveDataInfo();
        }
        catch (System.Exception ex)
        {
            LoggerUtils.LogError(ex.Message);
        }
    }

    /// <summary>
    /// 特殊机型协程启动
    /// </summary>
    public static void InitializeEquipmentSpecial()
    {
#if !UNITY_EDITOR

        try
        {
            bool isFirstUnityStart = false;
            if (!isFirstUnityStart) return;

            if (!userCanSetEquipmentSizing)
            {
                string equipmentSpecialUrl = GetRuntimeEquipmentSpecialUrl();
                //开启协程 下载特殊机型配置
                LoggerUtils.Log("开始特殊机型列表下载！");
                StartEquipmentSpecial(equipmentSpecialUrl);
            }

        }
        catch (System.Exception ex)
        {
            LoggerUtils.LogError(string.Format("特殊机型初始化异常:{0}", ex.Message));
        }

#endif

    }

    /// <summary>
    /// 存储机型分级到本地
    /// </summary>
    public static void Save()
    {
        if (CurEquipmentSizing==null) return;
        EquipmentSizingSaveData.Save(CurEquipmentSizing);
    }

    static void StartEquipmentSpecial(string equipmentSpecialUrl)
    {
        if (equipmentSpecialUrl.StartsWith(string.Intern("http:")) || equipmentSpecialUrl.StartsWith(string.Intern("https:")) || equipmentSpecialUrl.StartsWith(string.Intern("file://"))
            || equipmentSpecialUrl.StartsWith(string.Intern("jar:")) || equipmentSpecialUrl.StartsWith(string.Intern("ftp:")))
        {

        }
        else
        {
            equipmentSpecialUrl = "file://" + equipmentSpecialUrl;
        }
        LoggerUtils.Log(string.Format("特殊机型 url={0}", equipmentSpecialUrl));
        new AssetLoader().DownloadAsset(equipmentSpecialUrl, (content) =>
        {
            EquipmentSpecialList data = EquipmentSpecial.DeserializeToList(content);
            EquipmentSpecialLoadCallBack(data);
        }, (code) =>
        {
            LoggerUtils.LogError($"特殊机型 Load CallBack Exception !{code}");
        });
    }

    /// <summary>
    /// 特殊机型下载完成
    /// </summary>
    /// <param name="specialList"></param>
    static void EquipmentSpecialLoadCallBack(EquipmentSpecialList specialList)
    {
        try
        {
            curEquipmentSpecialList = specialList;
            if (curEquipmentSpecialList != null)
            {
                LoggerUtils.Log("特殊机型信息下载成功!" + curEquipmentSpecialList.datas.Count);
                //如果有玩家手动设置配置的功能 则此特殊机型强制判定逻辑需要去掉
                EquipmentSpecialData specialData = EquipmentSpecial.FindSpecialData(curEquipmentSpecialList);
                if (specialData != null)
                {
                    LoggerUtils.Log(string.Format("特殊机型:品牌名={0} 型号名={1} Cpu名={2} 显卡名={3} 目标机型={4} Cpu核数={5} 主频={6} 内存={7} 着色其功能级别={8} 是否支持MRT={9}" +
                        " 是否支持计算着色器={10} 是否支持几何着色器={11} 是否支持曲面细分={12} 是否支持多重采样={13} 是否使用多线程渲染={14} 显卡驱动版本名={15}",
                        specialData.brandName, specialData.modelName, specialData.cpuName, specialData.graphicsName, specialData.modelType.ToString()
                        , specialData.cpuCount, specialData.cpuFrequency, specialData.memory, specialData.shaderLevel, specialData.isMRT
                        , specialData.isComputerShader, specialData.supportsGeometryShaders, specialData.supportsTessellationShaders
                        , specialData.supportsMultisampledTextures, specialData.isThreadRender, specialData.graphicsDeviceVersion
                        ));
                    EquipmentSizingSaveData newEquipmentSizingSaveData = new EquipmentSizingSaveData();
                    newEquipmentSizingSaveData.modelClassificationType = specialData.modelType;
                    //特殊机型配置存储  在下次Unity启动的时候加载
                    EquipmentSizingSaveData.Save(newEquipmentSizingSaveData);
                    curEquipmentSpecialData = specialData;
                    LoggerUtils.Log("特殊机型存储!");
                }
                else
                {
                    LoggerUtils.Log("未包含本设备特殊机型");
                }
            }
            else
            {
                LoggerUtils.Log("特殊机型信息为NULL！");
            }
        }
        catch (System.Exception ex)
        {
            LoggerUtils.LogError("未包含本设备特殊机型 异常:"+ ex.Message);
        }
    }
}


