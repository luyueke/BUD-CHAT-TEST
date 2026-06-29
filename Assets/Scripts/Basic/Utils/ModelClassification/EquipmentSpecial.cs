using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Networking;

/// <summary>
/// 特殊设备清单
/// </summary>
public class EquipmentSpecial
{

#if UNITY_EDITOR

    #region//工具菜单

    #region//Master

    [MenuItem("Tools/设备分级/Master/特殊机型编辑")]
    static void MasterEditorWind()
    {

    }

    [MenuItem("Tools/设备分级/Master/Copy To Alpha")]
    static void MasterToAlpha()
    {
        LoadDataEditor_Master((data) => {
            if (data!=null)
            {
                UpLoadDataEditor_Alpha(data, () => {
                });
            }
        });
    }

    [MenuItem("Tools/设备分级/Master/Copy To Prod")]
    static void MasterToProd()
    {
        LoadDataEditor_Master((data) => {
            if (data != null)
            {
                UpLoadDataEditor_Prod(data, () => {
                });
            }
        });
    }

    #endregion

    #region//Alpha

    [MenuItem("Tools/设备分级/Alpha/特殊机型编辑")]
    static void AlphaEditorWind()
    {

    }

    [MenuItem("Tools/设备分级/Alpha/Copy To Master")]
    static void AlphaToMaster()
    {
        LoadDataEditor_Alpha((data) => {
            if (data != null)
            {
                UpLoadDataEditor_Master(data, () => {
                });
            }
        });
    }

    [MenuItem("Tools/设备分级/Alpha/Copy To Prod")]
    static void AlphaToProd()
    {
        LoadDataEditor_Alpha((data) => {
            if (data != null)
            {
                UpLoadDataEditor_Prod(data, () => {
                });
            }
        });
    }

    #endregion

    #region//Prod

    [MenuItem("Tools/设备分级/Prod/特殊机型编辑")]
    static void ProdEditorWind()
    {

    }

    [MenuItem("Tools/设备分级/Prod/Copy To Master")]
    static void ProdToMaster()
    {
        LoadDataEditor_Prod((data) => {
            if (data != null)
            {
                UpLoadDataEditor_Master(data, () => {
                });
            }
        });
    }

    [MenuItem("Tools/设备分级/Prod/Copy To Alpha")]
    static void ProdToAlpha()
    {
        LoadDataEditor_Prod((data) => {
            if (data != null)
            {
                UpLoadDataEditor_Alpha(data, () => {
                });
            }
        });
    }

    #endregion

    #endregion

    #region//上传

    /// <summary>
    /// 上传特殊机型列表 Master
    /// </summary>
    /// <param name="data"></param>
    /// <param name="callBack"></param>
    public static void UpLoadDataEditor_Master(EquipmentSpecialList data, System.Action callBack)
    {
        string upLoadDir = EquipmentSizingPath.specialUrlDir_Master;
        UpLoadDataEditor(data, upLoadDir, callBack);
    }

    /// <summary>
    /// 上传特殊机型列表 Alpha
    /// </summary>
    /// <param name="data"></param>
    /// <param name="callBack"></param>
    public static void UpLoadDataEditor_Alpha(EquipmentSpecialList data, System.Action callBack)
    {
        string upLoadDir = EquipmentSizingPath.specialUrlDir_Alpha;
        UpLoadDataEditor(data, upLoadDir, callBack);
    }

    /// <summary>
    /// 上传特殊机型列表 Prod
    /// </summary>
    /// <param name="data"></param>
    /// <param name="callBack"></param>
    public static void UpLoadDataEditor_Prod(EquipmentSpecialList data, System.Action callBack)
    {
        string upLoadDir = EquipmentSizingPath.specialUrlDir_Prod;
        UpLoadDataEditor(data, upLoadDir, callBack);
    }

    /// <summary>
    /// 上传修改过的数据
    /// </summary>
    /// <param name="data"></param>
    static void UpLoadDataEditor(EquipmentSpecialList data, string upLoadDir, System.Action callBack)
    {
        try
        {
            string jsonStr = data.ToString();
            string savePath = Application.persistentDataPath + "/"+ EquipmentSizingPath.specialFileName;
            LoggerUtils.Log(string.Format("特殊机型临时存储目录:{0}", savePath));
            EquipmentSizingGame.WriteTxt(savePath, jsonStr);
            //TODO:上传逻辑待修改
            // AWSUtill.UpLoadRes(EquipmentSizingPath.specialFileName, savePath, upLoadDir, (msg) =>
            // {
            //     Debug.Log(string.Format("特殊机型上传成功: msg={0}", msg));
            //     callBack();
            // }, (err) =>
            // {
            //     Debug.LogError(string.Format("特殊机型上传失败: err = {0}", err));
            //     callBack();
            // }, true);
        }
        catch (System.Exception ex)
        {
            LoggerUtils.LogError(ex.Message);
            callBack();
        }
    }

    #endregion

    #region//下载

    /// <summary>
    /// 下载现有数据 Master
    /// </summary>
    /// <returns></returns>
    public static void LoadDataEditor_Master(System.Action<EquipmentSpecialList> callBack)
    {
        if (Application.isPlaying) return;
        string url = EquipmentSizingPath.equipmentSpecialUrl_Master;
        LoadDataEditor(url, callBack);
    }

    /// <summary>
    /// 下载现有数据 Alpha
    /// </summary>
    /// <returns></returns>
    public static void LoadDataEditor_Alpha(System.Action<EquipmentSpecialList> callBack)
    {
        if (Application.isPlaying) return;
        string url = EquipmentSizingPath.equipmentSpecialUrl_Alpha;
        LoadDataEditor(url, callBack);
    }

    /// <summary>
    /// 下载现有数据 Prod
    /// </summary>
    /// <returns></returns>
    public static void LoadDataEditor_Prod(System.Action<EquipmentSpecialList> callBack)
    {
        if (Application.isPlaying) return;
        string url = EquipmentSizingPath.equipmentSpecialUrl_Prod;
        LoadDataEditor(url, callBack);
    }

    static void LoadDataEditor(string url, System.Action<EquipmentSpecialList> callBack)
    {
        UnityWebRequest www = new UnityWebRequest(url);
        www.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation requestAsync = www.SendWebRequest();
        requestAsync.completed += (asyncOperation) => {
            if (string.IsNullOrEmpty(www.error))
            {
                EquipmentSpecialList data = DeserializeToList(www.downloadHandler.text);
                callBack(data);
            }
            else
            {
                LoggerUtils.LogError(www.error);
                callBack(null);
            }
        };
    }

    #endregion

    #region//添加

    /// <summary>
    /// 特殊机型添加 Master
    /// </summary>
    /// <param name="datas"></param>
    public static void AddDataEditor_Master(List<EquipmentSpecialData> datas,System.Action callBack)
    {
        LoadDataEditor_Master((list) => {
            if (list==null)
            {
                list = new EquipmentSpecialList();
            }
            AddDataEditor(list, datas);
            UpLoadDataEditor_Master(list, () => {
                callBack();
            });
        });
    }

    /// <summary>
    /// 特殊机型添加 Alpha
    /// </summary>
    /// <param name="datas"></param>
    public static void AddDataEditor_Alpha(List<EquipmentSpecialData> datas, System.Action callBack)
    {
        LoadDataEditor_Alpha((list) => {
            if (list == null)
            {
                list = new EquipmentSpecialList();
            }
            AddDataEditor(list, datas);
            UpLoadDataEditor_Alpha(list, () => {
                callBack();
            });
        });
    }

    /// <summary>
    /// 特殊机型添加 Prod
    /// </summary>
    /// <param name="datas"></param>
    public static void AddDataEditor_Prod(List<EquipmentSpecialData> datas, System.Action callBack)
    {
        LoadDataEditor_Prod((list) => {
            if (list == null)
            {
                list = new EquipmentSpecialList();
            }
            AddDataEditor(list, datas);
            UpLoadDataEditor_Prod(list, () => {
                callBack();
            });
        });
    }

    /// <summary>
    /// 添加特殊机型数据
    /// </summary>
    /// <param name="datas"></param>
    static void AddDataEditor(EquipmentSpecialList list, List<EquipmentSpecialData> datas)
    {
        for (int j = 0; j < datas.Count; ++j)
        {
            EquipmentSpecialData data = datas[j];
            bool isSame = false;
            for (int i = 0; i < list.datas.Count; ++i)
            {
                EquipmentSpecialData equipmentSpecialData = list.datas[i];

                bool bl = SpecialDataIsSame(equipmentSpecialData,data);
                Debug.Log(bl+ data.modelType.ToString());
                if (bl)
                {
                    equipmentSpecialData.modelType = data.modelType;
                    isSame = true;
                    break;
                }
            }
            if (!isSame)
            {
                list.datas.Add(data);
            }
        }
    }

    #endregion

    #region//删除

    /// <summary>
    /// 删除特殊机型列表数据 Master
    /// </summary>
    /// <param name="datas"></param>
    public static void DeleteDataEditor_Master( List<EquipmentSpecialData> datas, System.Action callBack)
    {
        LoadDataEditor_Master((list) => {
            if (list == null)
            {
                list = new EquipmentSpecialList();
            }
            DeleteDataEditor(list, datas);
            UpLoadDataEditor_Master(list, () => {
                callBack();
            });
        });
    }

    /// <summary>
    /// 删除特殊机型列表数据 Alpha
    /// </summary>
    /// <param name="datas"></param>
    public static void DeleteDataEditor_Alpha(List<EquipmentSpecialData> datas, System.Action callBack)
    {
        LoadDataEditor_Alpha((list) => {
            if (list == null)
            {
                list = new EquipmentSpecialList();
            }
            DeleteDataEditor(list, datas);
            UpLoadDataEditor_Alpha(list, () => {
                callBack();
            });
        });
    }

    /// <summary>
    /// 删除特殊机型列表数据 Prod
    /// </summary>
    /// <param name="datas"></param>
    public static void DeleteDataEditor_Prod(List<EquipmentSpecialData> datas, System.Action callBack)
    {
        LoadDataEditor_Prod((list) => {
            if (list == null)
            {
                list = new EquipmentSpecialList();
            }
            DeleteDataEditor(list, datas);
            UpLoadDataEditor_Prod(list, () => {
                callBack();
            });
        });
    }

    /// <summary>
    /// 添加特殊机型数据
    /// </summary>
    /// <param name="datas"></param>
    static void DeleteDataEditor(EquipmentSpecialList list, List<EquipmentSpecialData> datas)
    {
        for (int j = 0; j < datas.Count; ++j)
        {
            EquipmentSpecialData data = datas[j];
            for (int i = 0; i < list.datas.Count; ++i)
            {
                EquipmentSpecialData equipmentSpecialData = list.datas[i];
                bool bl = SpecialDataIsSame(equipmentSpecialData, data);

                if (bl)
                {
                    list.datas.Remove(equipmentSpecialData);
                    break;
                }
            }
        }
    }

    #endregion

#endif

    static bool SpecialDataIsSame(EquipmentSpecialData dataA, EquipmentSpecialData dataB)
    {
        bool stringIsSame = IsSameString(dataA.brandName, dataA.modelName, dataA.cpuName, dataA.graphicsName, dataA.graphicsDeviceVersion,
                    dataB.brandName, dataB.modelName, dataB.cpuName, dataB.graphicsName, dataB.graphicsDeviceVersion);

        bool numBerIsSame = IsSameNumber(dataA.cpuCount, dataA.cpuFrequency, dataA.memory, dataA.shaderLevel
        , dataB.cpuCount, dataB.cpuFrequency, dataB.memory, dataB.shaderLevel);

        bool boolIsSame = IsSameBool(dataA.isMRT, dataA.isComputerShader, dataA.supportsGeometryShaders, dataA.supportsTessellationShaders, dataA.supportsMultisampledTextures, dataA.isThreadRender
        , dataB.isMRT, dataB.isComputerShader, dataB.supportsGeometryShaders, dataB.supportsTessellationShaders, dataB.supportsMultisampledTextures, dataB.isThreadRender);

        if (stringIsSame && numBerIsSame && boolIsSame)
        {
            return true;
        }
        return false;
    }

    static bool IsSameString(string brandNameA,string modelNameA,string cpuNameA,string graphicsNameA, string graphicsDeviceVersionA,
        string brandNameB, string modelNameB, string cpuNameB, string graphicsNameB, string graphicsDeviceVersionB)
    {
        //
        bool brandNameSame = true;
        if (!string.IsNullOrEmpty(brandNameA))
        {
            if (string.IsNullOrEmpty(brandNameB) || brandNameB.CompareTo(brandNameA) != 0)
            {
                brandNameSame = false;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(brandNameB))
            {
                brandNameSame = false;
            }
        }
        //
        bool modelNameSame = true;
        if (!string.IsNullOrEmpty(modelNameA))
        {
            if (string.IsNullOrEmpty(modelNameB) || modelNameB.CompareTo(modelNameA) != 0)
            {
                modelNameSame = false;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(modelNameB))
            {
                modelNameSame = false;
            }
        }
        //
        bool cpuNameSame = true;
        if (!string.IsNullOrEmpty(cpuNameA))
        {
            if (string.IsNullOrEmpty(cpuNameB) || cpuNameB.CompareTo(cpuNameA) != 0)
            {
                cpuNameSame = false;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(cpuNameB))
            {
                cpuNameSame = false;
            }
        }
        //
        bool graphicsNameSame = true;
        if (!string.IsNullOrEmpty(graphicsNameA))
        {
            if (string.IsNullOrEmpty(graphicsNameB) || graphicsNameB.CompareTo(graphicsNameA) != 0)
            {
                graphicsNameSame = false;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(graphicsNameB))
            {
                graphicsNameSame = false;
            }
        }
        //
        bool graphicsDeviceVersionSame = true;
        if (!string.IsNullOrEmpty(graphicsDeviceVersionA))
        {
            if (string.IsNullOrEmpty(graphicsDeviceVersionB) || graphicsDeviceVersionB.CompareTo(graphicsDeviceVersionA) != 0)
            {
                graphicsDeviceVersionSame = false;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(graphicsDeviceVersionB))
            {
                graphicsDeviceVersionSame = false;
            }
        }

        if (brandNameSame && modelNameSame && cpuNameSame && graphicsNameSame && graphicsDeviceVersionSame)
        {
            return true;
        }
        return false;
    }

    static bool IsSameNumber(int cpuCountA, float cpuFrequencyA, float memoryA, int shaderLevelA
        , int cpuCountB, float cpuFrequencyB, float memoryB, int shaderLevelB)
    {
        if (cpuCountA== cpuCountB && cpuFrequencyA== cpuFrequencyB && memoryA== memoryB && shaderLevelA== shaderLevelB)
        {
            return true;
        }
        return false;
    }

    static bool IsSameBool(bool isMRTA, bool isComputerShaderA, bool supportsGeometryShadersA, bool supportsTessellationShadersA, bool supportsMultisampledTexturesA, bool isThreadRenderA
        , bool isMRTB, bool isComputerShaderB, bool supportsGeometryShadersB, bool supportsTessellationShadersB, bool supportsMultisampledTexturesB, bool isThreadRenderB)
    {
        if (isMRTA== isMRTB && isComputerShaderA== isComputerShaderB && supportsGeometryShadersA== supportsGeometryShadersB && supportsTessellationShadersA== supportsTessellationShadersB
            && supportsMultisampledTexturesA== supportsMultisampledTexturesB && isThreadRenderA== isThreadRenderB)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 查找当前设备是否为特殊机型
    /// </summary>
    /// <returns></returns>
    public static EquipmentSpecialData FindSpecialData(EquipmentSpecialList specialList)
    {
        EquipmentSpecialData findData = null;
        EquipmentData equipmentData = new EquipmentData();
        for (int i = 0; i < specialList.datas.Count; ++i)
        {
            EquipmentSpecialData data = specialList.datas[i];

            bool stringIsSame = IsSameString(equipmentData.brandName, equipmentData.modelName, equipmentData.cpuName, equipmentData.graphicsName, equipmentData.graphicsDeviceVersion,
            data.brandName, data.modelName, data.cpuName, data.graphicsName, data.graphicsDeviceVersion);

            bool numBerIsSame = IsSameNumber(equipmentData.cpuCount, equipmentData.cpuFrequency, equipmentData.memory, equipmentData.shaderLevel
            , data.cpuCount, data.cpuFrequency, data.memory, data.shaderLevel);

            bool boolIsSame = IsSameBool(equipmentData.isMRT, equipmentData.isComputerShader, equipmentData.supportsGeometryShaders, equipmentData.supportsTessellationShaders, equipmentData.supportsMultisampledTextures, equipmentData.isThreadRender
            , data.isMRT, data.isComputerShader, data.supportsGeometryShaders, data.supportsTessellationShaders, data.supportsMultisampledTextures, data.isThreadRender);


            if (stringIsSame && numBerIsSame && boolIsSame)
            {
                findData = data;
                break;
            }
        }
        return findData;
    }

    /// <summary>
    /// Json解析特殊机型数据
    /// </summary>
    /// <param name="jsonStr"></param>
    /// <returns></returns>
    public static EquipmentSpecialList DeserializeToList(string jsonStr)
    {
        try
        {
            EquipmentSpecialList list = JsonConvert.DeserializeObject<EquipmentSpecialList>(jsonStr);
            return list;
        }
        catch (System.Exception ex)
        {
            LoggerUtils.LogError(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 创建测试案例数据
    /// </summary>
    /// <param name="savePath"></param>
    public static void CreateDemoEquipmentSpecial(string savePath)
    {
        EquipmentSpecialList specialList = new EquipmentSpecialList();
        EquipmentSpecialData specialData = new EquipmentSpecialData();
        specialList.datas.Add(specialData);
        string dir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(savePath, specialList.ToString());
    }
}


[Serializable]
public class EquipmentSpecialList
{

    public List<EquipmentSpecialData> datas = new List<EquipmentSpecialData>();

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }

}

[Serializable]
public class EquipmentSpecialData
{
    public EquipmentSpecialData()
    {

    }

    public EquipmentSpecialData(ModelClassificationType modelType,string brandName, string modelName, string cpuName, string graphicsName,  string graphicsDeviceVersion,
        int cpuCount, float cpuFrequency, float memory, int shaderLevel,
        bool isMRT, bool isComputerShader, bool supportsGeometryShaders, bool supportsTessellationShaders, bool supportsMultisampledTextures, bool isThreadRender)
    {
        this.modelType = modelType;
        //
        this.brandName = brandName;
        this.modelName = modelName;
        this.cpuName = cpuName;
        this.graphicsName = graphicsName;
        this.graphicsDeviceVersion = graphicsDeviceVersion;
        //
        this.cpuCount = cpuCount;
        this.cpuFrequency = cpuFrequency;
        this.memory = memory;
        this.shaderLevel = shaderLevel;
        //
        this.isMRT = isMRT;
        this.isComputerShader = isComputerShader;
        this.supportsGeometryShaders = supportsGeometryShaders;
        this.supportsTessellationShaders = supportsTessellationShaders;
        this.supportsMultisampledTextures = supportsMultisampledTextures;
        this.isThreadRender = isThreadRender;
    }

    /// <summary>
    /// 品牌名
    /// </summary>
    public string brandName="";

    /// <summary>
    /// 型号名
    /// </summary>
    public string modelName = "";

    /// <summary>
    /// Cpu名
    /// </summary>
    public string cpuName = "";

    /// <summary>
    /// 显卡名
    /// </summary>
    public string graphicsName = "";

    /// <summary>
    /// 显卡驱动版本名
    /// </summary>
    public string graphicsDeviceVersion = null;

    //

    /// <summary>
    /// Cpu核数
    /// </summary>
    public int cpuCount = 0;

    /// <summary>
    /// 主频
    /// </summary>
    public float cpuFrequency = 0;

    /// <summary>
    /// 内存
    /// </summary>
    public float memory = 0;

    /// <summary>
    /// 着色其功能级别
    /// </summary>
    public int shaderLevel = 0;

    //

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
    public bool supportsMultisampledTextures = false;

    /// <summary>
    /// 是否使用多线程渲染
    /// </summary>
    public bool isThreadRender = false;

    /// <summary>
    /// 目标机型
    /// </summary>
    public ModelClassificationType modelType;
}

