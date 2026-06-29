using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 机型分级
/// </summary>

/*
 * 超高配:
 * 安卓:
 *      [Cpu核数>=10] 并且 [主频>=2.2ghz] 并且 [内存]>=3.6g 并且 [着色器级别]>=50 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true 并且 [几何着色器]=true 并且 [曲面细分着色器]=true
 *      或
 *      [Cpu核数>=8] 并且 [主频>=3ghz] 并且 [内存]>=3.6g 并且 [着色器级别]>=50 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true 并且 [几何着色器]=true 并且 [曲面细分着色器]=true
 * IOS:
 *      [Cpu核数>=6] 并且 [内存]>=5g 并且 [着色器级别]>=50 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true 并且 [几何着色器]=true 并且 [曲面细分着色器]=true
	1.可使用计算着色器
	2.可使用曲面细分着色器
	3.可使用延迟渲染
	4.可使用后期效果（Bloom，ToneMaping，校色，白平衡，HDR），分辨率为1/4
	5.可选择使用自定义阴影/Unity级联阴影
	6.可使用PBR渲染
	7.可使用空气扭曲
	8.默认开启颜色缓冲+深度缓冲
	9.可使用雾效
	10.可使用SkinGPU优化
	11.颜色空间默认使用Linner（效果差异性）
	12.可以使用抗锯齿（与延迟渲染冲突）
	13.模型细节选择根据遮挡剔除与分块信息加载
	14.相机可是范围为正常范围
	15.特效分级为高
	16.物理水体
	17.开启镜面反射
	18.开启纹理各向异性
	19.纹理保持原始尺寸
	20.可使用次表面散射材质
	21.可使用物理草地
	22.可使用物理树木
	23.不可使用体积渲染
	24.不可使用光追
 */

/*
 * 高配:
 * 安卓:
 *      [Cpu核数>=8] 并且 [Cpu核数<10] 并且 [主频<3ghz] 并且 [内存]>=3.6g 并且 [着色器级别]>=40 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true
 *      或
 *      [Cpu核数>=10] 并且 [主频<2.2ghz] 并且 [内存]>=3.6g 并且 [着色器级别]>=40 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true
 * IOS:
 *      [Cpu核数>=6] 并且 [内存]>=3g 并且 [着色器级别]>=40 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true
    1.可使用计算着色器
	2.不可使用曲面细分着色器
	3.可使用延迟渲染
	4.可使用后期效果（Bloom，ToneMaping，校色，白平衡，HDR），分辨率为1/4
	5.可选择使用自定义阴影/Unity级联阴影
	6.可使用PBR渲染
	7.可使用空气扭曲
	8.默认开启颜色缓冲+深度缓冲
	9.可使用雾效
	10.可使用SkinGPU优化
	11.颜色空间默认使用Linner（效果差异性）
	12.可以使用抗锯齿（与延迟渲染冲突）
	13.模型细节选择根据遮挡剔除与分块信息加载
	14.相机可是范围为正常范围
	15.特效分级为高
	16.物理水体
	17.可选开启镜面反射
	18.开启纹理各向异性
	19.纹理保持原始尺寸
	20.可使用次表面散射材质
	21.可使用物理草地
	22.可使用物理树木
	23.不可使用体积渲染
	24.不可使用光追
 */

/*
 * 中配:
 * 安卓:
 *      [Cpu核数>=4] 并且 [Cpu核数<6] 并且 [主频>=1.8ghz] 并且 [分辨率]<=1440 并且 [内存]>=1.5g 并且 [着色器级别]>=30 
 *      或
 *      [Cpu核数>=6] 并且 [Cpu核数<8] 并且 [主频>=1.8ghz] 并且 [分辨率]<=1600 并且 [内存]>=3.6g 并且 [着色器级别]>=30
 * IOS:
 *      [Cpu核数>2] 并且 [Cpu核数<6] 并且 [内存]<3g 并且 [着色器级别]>=30
	1.不可使用计算着色器
	2.不可使用曲面细分着色器
	3.不可使用延迟渲染
	4.选择性使用后期效果（Bloom，ToneMaping，校色，白平衡，HDR），分辨率为1/8
	5.可选择使用自定义阴影/Unity级联阴影
	6.不可使用PBR渲染，使用布林冯模型代替
	7.不可使用空气扭曲
	8.默认开启深度缓冲，不开启颜色缓冲
	9.可使用雾效
	10.不可使用SkinGPU优化
	11.颜色空间默认使用Linner（效果差异性）
	12.可以使用抗锯齿（与延迟渲染冲突）
	13.模型细节选择根据遮挡剔除与分块信息加载
	14.相机可是范围为正常范围
	15.特效分级为中
	16.平面水体
	17.无镜面反射
	18.不开启纹理各向异性
	19.纹理保持原始尺寸
	20.可使用次表面散射材质
	21.不可使用物理草地
	22.不可使用物理树木
	23.不可使用体积渲染
	24.不可使用光追
 */

/*
 * 低配:
 * 安卓:
 *      [Cpu核数<4]
 * IOS:
 *      [Cpu核数<=2]
	1.不可使用计算着色器
	2.不可使用曲面细分着色器
	3.不可使用延迟渲染
	4.不可使用后期效果（Bloom，ToneMaping，校色，白平衡，HDR）
	5.不可使用阴影
	6.不可使用PBR渲染，不可使用光照，使用MatCap代替
	7.不可使用空气扭曲
	8.不开启深度缓冲
	9.不可使用雾效
	10.不可使用SkinGPU优化
	11.颜色空间默认使用Linner（效果差异性）
	12.不可以使用抗锯齿
	13.模型细节选择根据遮挡剔除与分块信息加载
	14.相机可是范围为正常范围的0.5
	15.特效分级为低
	16.平面水体
	17.无镜面反射
	18.不开启纹理各向异性
	19.纹理为原始尺寸的一半
	20.不可使用次表面散射材质
	21.不可使用物理草地
	22.不可使用物理树木
	23.不可使用体积渲染
	24.不可使用光追
 */

public class EquipmentSizing
{

    /// <summary>
    /// 版本号
    /// </summary>
    public static float VersionNumber = 1.2f;

    #region//分级条件 

    /// <summary>
    /// 获得当前设备的设备分级
    /// </summary>
    /// <returns></returns>
    public static ModelClassificationType GetCurClassification()
    {
        Dictionary<int, JudgeConditionData> conditionDataDic = null;
        //编辑器环境下的安卓，Ios平台默认使用中配

#if UNITY_EDITOR
        return ModelClassificationType.Middle;
#else
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
            {
                conditionDataDic = JudgeConditionDataDic;
            }
                break;
            case RuntimePlatform.IPhonePlayer:
                {
                    conditionDataDic = AppleJudgeConditionDataDic;
                }
                break;
            default:
                {
                    conditionDataDic = JudgeConditionDataDic;
                }
                break;
        }
#endif

        try
        {
            var equipData = new EquipmentData();
            LoggerUtils.Log("equipData",equipData.ToString());
            Dictionary<string, JudgeData> res = GetJudgeData(conditionDataDic,equipData);
            Dictionary<string, JudgeData>.Enumerator enumerator = res.GetEnumerator();
            ModelClassificationType lastType= ModelClassificationType.Middle;
            while (enumerator.MoveNext())
            {
                JudgeData judgeData = enumerator.Current.Value;
                if (judgeData.datas.Count > 0)
                {
                    switch (judgeData.judgeConditionData.level)
                    {
                        case 0:
                            {
                                lastType = ModelClassificationType.Low;
                            }
                            break;
                        case 1:
                            {
                                lastType = ModelClassificationType.Middle;
                            }
                            break;
                        case 2:
                            {
                                lastType = ModelClassificationType.High;
                            }
                            break;
                        case 3:
                            {
                                lastType = ModelClassificationType.SupeHigh;
                            }
                            break;
                    }
                }
            }
            return lastType;
        }
        catch (System.Exception ex)
        {
            LoggerUtils.LogError(ex.Message);
        }
        return ModelClassificationType.Middle;
    }

    /// <summary>
    /// 低中高配分级
    /// </summary>
    /// <param name="conditionDataDic"></param>
    /// <param name="equipmentDatas"></param>
    /// <returns></returns>
    public static Dictionary<string, JudgeData> GetJudgeData(Dictionary<int, JudgeConditionData> conditionDataDic, EquipmentData equipmentData)
    {
        Dictionary<string, JudgeData> resultDic = new Dictionary<string, JudgeData>();
        Dictionary<int, JudgeConditionData>.Enumerator enumerator = conditionDataDic.GetEnumerator();

        Dictionary<int, List<GetJudgeDataResData>> tempDic = new Dictionary<int, List<GetJudgeDataResData>>();
        List<JudgeData> tempList = new List<JudgeData>();
        while (enumerator.MoveNext())
        {
            int level = enumerator.Current.Key;
            List<GetJudgeDataResData> resList = new List<GetJudgeDataResData>(); ;
            tempDic.Add(level, resList);
            JudgeConditionData judgeConditionData = enumerator.Current.Value;
            JudgeData judgeData = new JudgeData();
            judgeData.judgeConditionData = judgeConditionData;
            resultDic.Add(judgeConditionData.key, judgeData);
            tempList.Add(judgeData);

            bool allTrue = true;
            bool majorParameterFail = false;
            for (int j = 0; j < judgeConditionData.JudgeConditions.Count; ++j)
            {
                JudgeCondition judgeCondition = judgeConditionData.JudgeConditions[j];
                bool bl = judgeCondition.IsCondition(equipmentData);
                if (!bl)
                {
                    allTrue = false;
                    if (typeof(JudgeConditionRelation) == judgeCondition.GetType())
                    {
                        majorParameterFail = ((JudgeConditionRelation)judgeCondition).majorParameterFail;
                    }
                    else
                    {
                        if (judgeCondition.isMajorParameter)
                        {
                            majorParameterFail = true;
                        }
                    }
                }
            }
            GetJudgeDataResData getJudgeDataResData = new GetJudgeDataResData();
            getJudgeDataResData.equipmentData = equipmentData;
            getJudgeDataResData.allTrue = allTrue;
            getJudgeDataResData.majorParameterFail = majorParameterFail;
            getJudgeDataResData.judgeData = judgeData;
            resList.Add(getJudgeDataResData);
        }
        Dictionary<int, List<GetJudgeDataResData>>.Enumerator tempDicEnumerator = tempDic.GetEnumerator();
        while (tempDicEnumerator.MoveNext())
        {
            List<GetJudgeDataResData> list = tempDicEnumerator.Current.Value;
            for (int i=0;i< list.Count; ++i)
            {
                GetJudgeDataResData data = list[i];
                if (data.allTrue)
                {
                    data.judgeData.datas.Add(data.equipmentData);
                }
            }
        }
        return resultDic;
    }

    class GetJudgeDataResData
    {
        public EquipmentData equipmentData;

        public bool allTrue;

        public bool majorParameterFail;

        public JudgeData judgeData;
    }

    /// <summary>
    /// 分级结果
    /// </summary>
    public class JudgeData
    {
        public JudgeConditionData judgeConditionData;

        public List<EquipmentData> datas = new List<EquipmentData>();

        public override string ToString()
        {
            datas.Sort((A, B) => {
                return B.liveness.CompareTo(A.liveness);
            });
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < datas.Count; ++i)
            {
                EquipmentData equipmentData = datas[i];
                sb.Append(string.Format("[Name]={0} [Liveness]={1} [Model]={2} [Memory]={3} [CpuName]={4} [CpuCount]={5} [CpuFrequency]={6} [GraphicsName]={7} [GraphicsMemory]={8}\n", equipmentData.brandName, equipmentData.liveness, equipmentData.modelName,
                    equipmentData.memory, equipmentData.cpuName, equipmentData.cpuCount, equipmentData.cpuFrequency, equipmentData.graphicsName, equipmentData.graphicsMemory));

            }
            return sb.ToString();
        }

    }

    /// <summary>
    /// 组合And条件
    /// </summary>
    /// <param name="objs"></param>
    /// <returns></returns>
    static JudgeConditionRelation CombinationConditions(params JudgeCondition[] objs)
    {
        JudgeConditionRelation lastData = null;
        if (objs.Length == 1)
        {
            lastData = new JudgeConditionRelation(objs[0], null, ConditionRelation.And);
            return lastData;
        }
        for (int i = 0; i < objs.Length; ++i)
        {
            if (i == 0)
            {
                continue;
            }
            if (i == 1)
            {
                lastData = new JudgeConditionRelation(objs[0], objs[1], ConditionRelation.And);
            }
            else
            {
                JudgeConditionRelation newData = new JudgeConditionRelation(lastData, objs[i], ConditionRelation.And);
                lastData = newData;
            }
        }
        if (lastData == null)
        {
            lastData = new JudgeConditionRelation(null, null, ConditionRelation.And);
        }
        return lastData;
    }

    #region//PC机型配置

    static Dictionary<int, JudgeConditionData> judgeConditionDataPCDic;

    public static Dictionary<int, JudgeConditionData> JudgeConditionDataPCDic
    {
        get
        {
            if (judgeConditionDataPCDic == null)
            {
                judgeConditionDataPCDic = new Dictionary<int, JudgeConditionData>();
            }
            judgeConditionDataPCDic.Clear();
            //
            #region//低配

            //低配
            //[Cpu核数<4]
            JudgeConditionData judgeConditionData_L = new JudgeConditionData(0, "低配");
            judgeConditionDataPCDic.Add(judgeConditionData_L.level, judgeConditionData_L);
            List<JudgeCondition> judgeConditions_L = new List<JudgeCondition>();
            judgeConditionData_L.JudgeConditions = judgeConditions_L;
            //
            JudgeConditionCpuCount conditionL_1_A = new JudgeConditionCpuCount(true, 0, JudgeConditionType.GEqual, 4, JudgeConditionType.Less);//Cpu核数<4
                                                                                                                                               //
            judgeConditions_L.Add(conditionL_1_A);

            #endregion

            #region//中配

            //中配
            //[Cpu核数>=4] 并且 [Cpu核数<6] 并且 [主频>=1.8ghz] 并且 [分辨率]<=1440 并且 [内存]>=1.5g 并且 [着色器级别]>=30 
            //[Cpu核数>=6] 并且 [Cpu核数<8] 并且 [主频>=1.8ghz] 并且 [分辨率]<=1600 并且 [内存]>=3.6g 并且 [着色器级别]>=30
            JudgeConditionData judgeConditionData_M = new JudgeConditionData(1, "中配");
            judgeConditionDataPCDic.Add(judgeConditionData_M.level, judgeConditionData_M);
            List<JudgeCondition> judgeConditions_M = new List<JudgeCondition>();
            judgeConditionData_M.JudgeConditions = judgeConditions_M;
            //
            JudgeConditionCpuCount conditionM_1_B = new JudgeConditionCpuCount(true, 4, JudgeConditionType.GEqual, 6, JudgeConditionType.Less);//4<=Cpu核数<6
            JudgeConditionCpuFrequency conditionM_1_C = new JudgeConditionCpuFrequency(false, 1.8f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//主频>=1.8
            JudgeConditionMemory conditionM_1_D = new JudgeConditionMemory(false, 1.5f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=1.5
            JudgeConditionScreen conditionM_1_E = new JudgeConditionScreen(false, 0, JudgeConditionType.GEqual, 1440, JudgeConditionType.LEqual);//屏幕<=1440
            JudgeConditionShaderLevel conditionM_1_F = new JudgeConditionShaderLevel(false, 30, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//着色器等级>30
                                                                                                                                                                  //
            JudgeConditionRelation relationM_1 = CombinationConditions(conditionM_1_B, conditionM_1_C, conditionM_1_D, conditionM_1_E, conditionM_1_F);
            //
            JudgeConditionCpuCount conditionM_2_B = new JudgeConditionCpuCount(true, 6, JudgeConditionType.GEqual, 8, JudgeConditionType.Less);//6<=Cpu核数<8
            JudgeConditionCpuFrequency conditionM_2_C = new JudgeConditionCpuFrequency(false, 1.8f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//主频>=1.8
            JudgeConditionMemory conditionM_2_D = new JudgeConditionMemory(false, 1.5f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=1.5
            JudgeConditionScreen conditionM_2_E = new JudgeConditionScreen(false, 0, JudgeConditionType.GEqual, 1600, JudgeConditionType.LEqual);//屏幕<=1660
            JudgeConditionShaderLevel conditionM_2_F = new JudgeConditionShaderLevel(false, 30, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//着色器等级>30
                                                                                                                                                                  //
            JudgeConditionRelation relationM_2 = CombinationConditions(conditionM_2_B, conditionM_2_C, conditionM_2_D, conditionM_2_E, conditionM_2_F);
            //
            JudgeConditionRelation relationM = new JudgeConditionRelation(relationM_1, relationM_2, ConditionRelation.Or);
            //
            judgeConditions_M.Add(relationM);

            #endregion

            #region//高配

            //高配
            //[Cpu核数>=8] 并且 [Cpu核数<10] 并且 [主频<3ghz] 并且 [内存]>=3.6g 并且 [着色器级别]>=40 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true
            //[Cpu核数>=10] 并且 [主频<2.2ghz] 并且 [内存]>=3.6g 并且 [着色器级别]>=40 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true
            JudgeConditionData judgeConditionData_H = new JudgeConditionData(2, "高配");
            judgeConditionDataPCDic.Add(judgeConditionData_H.level, judgeConditionData_H);
            List<JudgeCondition> judgeConditions_H = new List<JudgeCondition>();
            judgeConditionData_H.JudgeConditions = judgeConditions_H;
            //
            JudgeConditionCpuCount conditionH_1_B = new JudgeConditionCpuCount(true, 8, JudgeConditionType.GEqual, 10, JudgeConditionType.Less);//8<=Cpu核数<10
            JudgeConditionCpuFrequency conditionH_1_C = new JudgeConditionCpuFrequency(false, 0, JudgeConditionType.GEqual, 3f, JudgeConditionType.Less);//主频<3
            JudgeConditionMemory conditionH_1_D = new JudgeConditionMemory(false, 3.6f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=3.6
            JudgeConditionShaderLevel conditionH_1_E = new JudgeConditionShaderLevel(false, 40, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.LEqual);//着色器等级>=40
            JudgeConditionIsMRT conditionH_1_F = new JudgeConditionIsMRT(false, true, JudgeConditionType.Equal);//支持MRT
            JudgeConditionIsComputerShader conditionH_1_G = new JudgeConditionIsComputerShader(false, true, JudgeConditionType.Equal);//支持计算着色器
            JudgeConditionSupportsMultisampledTextures conditionH_1_H = new JudgeConditionSupportsMultisampledTextures(false, true, JudgeConditionType.Equal);//支持多重采样
                                                                                                                                                              //
            JudgeConditionRelation relationH_1 = CombinationConditions(conditionH_1_B, conditionH_1_C, conditionH_1_D, conditionH_1_E, conditionH_1_F, conditionH_1_G, conditionH_1_H);
            //
            JudgeConditionCpuCount conditionH_2_B = new JudgeConditionCpuCount(true, 10, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//Cpu核数>=10
            JudgeConditionCpuFrequency conditionH_2_C = new JudgeConditionCpuFrequency(false, 0, JudgeConditionType.GEqual, 2.2f, JudgeConditionType.Less);//主频<2.2
            JudgeConditionMemory conditionH_2_D = new JudgeConditionMemory(false, 3.6f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=3.6
            JudgeConditionShaderLevel conditionH_2_E = new JudgeConditionShaderLevel(false, 40, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.LEqual);//着色器等级>=40
            JudgeConditionIsMRT conditionH_2_F = new JudgeConditionIsMRT(false, true, JudgeConditionType.Equal);//支持MRT
            JudgeConditionIsComputerShader conditionH_2_G = new JudgeConditionIsComputerShader(false, true, JudgeConditionType.Equal);//支持计算着色器
            JudgeConditionSupportsMultisampledTextures conditionH_2_H = new JudgeConditionSupportsMultisampledTextures(false, true, JudgeConditionType.Equal);//支持多重采样
                                                                                                                                                              //
            JudgeConditionRelation relationH_2 = CombinationConditions(conditionH_2_B, conditionH_2_C, conditionH_2_D, conditionH_2_E, conditionH_2_F, conditionH_2_G, conditionH_2_H);
            //
            JudgeConditionRelation relationH = new JudgeConditionRelation(relationH_1, relationH_2, ConditionRelation.Or);
            //
            judgeConditions_H.Add(relationH);

            #endregion

            #region//超高配

            //超高配
            //[Cpu核数>=10] 并且 [主频>=2.2ghz] 并且 [内存]>=3.6g 并且 [着色器级别]>=50 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true 并且 [几何着色器]=true 并且 [曲面细分着色器]=true
            //[Cpu核数>=8] 并且 [主频>=3ghz] 并且 [内存]>=3.6g 并且 [着色器级别]>=50 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true 并且 [几何着色器]=true 并且 [曲面细分着色器]=true
            JudgeConditionData judgeConditionData_HH = new JudgeConditionData(3, "超高配");
            judgeConditionDataPCDic.Add(judgeConditionData_HH.level, judgeConditionData_HH);
            List<JudgeCondition> judgeConditions_HH = new List<JudgeCondition>();
            judgeConditionData_HH.JudgeConditions = judgeConditions_HH;
            //
            JudgeConditionCpuCount conditionHH_1_B = new JudgeConditionCpuCount(true, 10, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//Cpu核数>=10
            JudgeConditionCpuFrequency conditionHH_1_C = new JudgeConditionCpuFrequency(false, 2.2f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//主频>=2.2
            JudgeConditionMemory conditionHH_1_D = new JudgeConditionMemory(false, 3.6f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=3.6
            JudgeConditionShaderLevel conditionHH_1_E = new JudgeConditionShaderLevel(false, 50, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//着色器级别>=50
            JudgeConditionIsMRT conditionHH_1_F = new JudgeConditionIsMRT(false, true, JudgeConditionType.Equal);//支持MRT
            JudgeConditionIsComputerShader conditionHH_1_G = new JudgeConditionIsComputerShader(false, true, JudgeConditionType.Equal);//支持计算着色器
            JudgeConditionSupportsMultisampledTextures conditionHH_1_H = new JudgeConditionSupportsMultisampledTextures(false, true, JudgeConditionType.Equal);//支持多重采样
            JudgeConditionSupportsGeometryShaders conditionHH_1_I = new JudgeConditionSupportsGeometryShaders(false, true, JudgeConditionType.Equal);//支持几何着色器
            JudgeConditionSupportsTessellationShaders conditionHH_1_J = new JudgeConditionSupportsTessellationShaders(false, true, JudgeConditionType.Equal);//支持曲面细分着色器
                                                                                                                                                             //
            JudgeConditionRelation relationHH_1 = CombinationConditions(conditionHH_1_B, conditionHH_1_C, conditionHH_1_D, conditionHH_1_E, conditionHH_1_F, conditionHH_1_G, conditionHH_1_H, conditionHH_1_I, conditionHH_1_J);
            //
            JudgeConditionCpuCount conditionHH_2_B = new JudgeConditionCpuCount(true, 8, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//Cpu核数>=8
            JudgeConditionCpuFrequency conditionHH_2_C = new JudgeConditionCpuFrequency(false, 3f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//主频>=3
            JudgeConditionMemory conditionHH_2_D = new JudgeConditionMemory(false, 3.6f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=3.6
            JudgeConditionShaderLevel conditionHH_2_E = new JudgeConditionShaderLevel(false, 50, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//着色器级别>=50
            JudgeConditionIsMRT conditionHH_2_F = new JudgeConditionIsMRT(false, true, JudgeConditionType.Equal);//支持MRT
            JudgeConditionIsComputerShader conditionHH_2_G = new JudgeConditionIsComputerShader(false, true, JudgeConditionType.Equal);//支持计算着色器
            JudgeConditionSupportsMultisampledTextures conditionHH_2_H = new JudgeConditionSupportsMultisampledTextures(false, true, JudgeConditionType.Equal);//支持多重采样
            JudgeConditionSupportsGeometryShaders conditionHH_2_I = new JudgeConditionSupportsGeometryShaders(false, true, JudgeConditionType.Equal);//支持几何着色器
            JudgeConditionSupportsTessellationShaders conditionHH_2_J = new JudgeConditionSupportsTessellationShaders(false, true, JudgeConditionType.Equal);//支持曲面细分着色器
                                                                                                                                                             //
            JudgeConditionRelation relationHH_2 = CombinationConditions(conditionHH_2_B, conditionHH_2_C, conditionHH_2_D, conditionHH_2_E, conditionHH_2_F, conditionHH_2_G, conditionHH_2_H, conditionHH_2_I, conditionHH_2_J);
            //
            JudgeConditionRelation relationHH = new JudgeConditionRelation(relationHH_1, relationHH_2, ConditionRelation.Or);
            //
            judgeConditions_HH.Add(relationHH);

            #endregion
            //
            return judgeConditionDataPCDic;
        }
    }

    #endregion

    #region//非苹果手机机型分级配置

    static Dictionary<int, JudgeConditionData> judgeConditionDataDic;

    /// <summary>
    /// 非苹果分级 key=等级 等级需要逐级增加
    /// </summary>
    public static Dictionary<int, JudgeConditionData> JudgeConditionDataDic
    {
        get
        {
            if (judgeConditionDataDic == null)
            {
                judgeConditionDataDic = new Dictionary<int, JudgeConditionData>();
            }
            judgeConditionDataDic.Clear();

            #region//低配

            //低配-默认低配开始
            JudgeConditionData judgeConditionData_L = new JudgeConditionData(0, "低配");
            judgeConditionDataDic.Add(judgeConditionData_L.level, judgeConditionData_L);
            List<JudgeCondition> judgeConditions_L = new List<JudgeCondition>();
            judgeConditionData_L.JudgeConditions = judgeConditions_L;
            //
            JudgeConditionBrand conditionL_1_A = new JudgeConditionBrand(true, "Apple/iPhone/iPad", JudgeConditionType.UnEqual);//非苹果
            JudgeConditionCpuCount conditionL_1_B = new JudgeConditionCpuCount(true, 0, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);
                                                                                                                                               //
            JudgeConditionRelation relationL = CombinationConditions(conditionL_1_A, conditionL_1_B);
            //
            judgeConditions_L.Add(relationL);

            #endregion

            #region//中配

            //中配
            //[Cpu核数>=6]  并且 [主频>=2.2ghz] 并且 [分辨率]>=1440 并且 [内存]>=3.5g 并且 [着色器级别]>=30 
            //[Cpu核数>=6]  并且 [主频>=2.2ghz] 并且 [分辨率]>=1440 并且 [内存]>=3.5g 并且 [着色器级别]>=30
            JudgeConditionData judgeConditionData_M = new JudgeConditionData(1, "中配");
            judgeConditionDataDic.Add(judgeConditionData_M.level, judgeConditionData_M);
            List<JudgeCondition> judgeConditions_M = new List<JudgeCondition>();
            judgeConditionData_M.JudgeConditions = judgeConditions_M;
            //
            JudgeConditionBrand conditionM_1_A = new JudgeConditionBrand(true, "Apple/iPhone/iPad", JudgeConditionType.UnEqual);//非苹果
            JudgeConditionCpuCount conditionM_1_B = new JudgeConditionCpuCount(true, 6, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//4<=Cpu核数<6
            JudgeConditionCpuFrequency conditionM_1_C = new JudgeConditionCpuFrequency(false, 2.2f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//主频>=1.8
            JudgeConditionMemory conditionM_1_D = new JudgeConditionMemory(false, 3.5f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=1.5
            JudgeConditionScreen conditionM_1_E = new JudgeConditionScreen(false, 1440, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.LEqual);//屏幕<=1440
            JudgeConditionShaderLevel conditionM_1_F = new JudgeConditionShaderLevel(false, 30, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//着色器等级>30
                                                                                                                                                                  //
            JudgeConditionRelation relationM_1 = CombinationConditions(conditionM_1_A, conditionM_1_B, conditionM_1_C, conditionM_1_D, conditionM_1_E, conditionM_1_F);
            //
            JudgeConditionBrand conditionM_2_A = new JudgeConditionBrand(true, "Apple/iPhone/iPad", JudgeConditionType.UnEqual);//非苹果
            JudgeConditionCpuCount conditionM_2_B = new JudgeConditionCpuCount(true, 6, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//6<=Cpu核数<8
            JudgeConditionCpuFrequency conditionM_2_C = new JudgeConditionCpuFrequency(false, 2.2f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//主频>=1.8
            JudgeConditionMemory conditionM_2_D = new JudgeConditionMemory(false, 3.5f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=1.5
            JudgeConditionScreen conditionM_2_E = new JudgeConditionScreen(false, 1440, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.LEqual);//屏幕<=1660
            JudgeConditionShaderLevel conditionM_2_F = new JudgeConditionShaderLevel(false, 30, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//着色器等级>30
                                                                                                                                                                  //
            JudgeConditionRelation relationM_2 = CombinationConditions(conditionM_2_A, conditionM_2_B, conditionM_2_C, conditionM_2_D, conditionM_2_E, conditionM_2_F);
            //
            JudgeConditionRelation relationM = new JudgeConditionRelation(relationM_1, relationM_2, ConditionRelation.Or);
            //
            judgeConditions_M.Add(relationM);

            #endregion

            #region//高配

            //高配
            //[Cpu核数>=8] 并且 [Cpu核数<10] 并且 [主频>3ghz] 并且 [内存]>=5g 
            //[Cpu核数>=10] 并且 [主频>2.5ghz] 并且 [内存]>=5g 
            JudgeConditionData judgeConditionData_H = new JudgeConditionData(2, "高配");
            judgeConditionDataDic.Add(judgeConditionData_H.level, judgeConditionData_H);
            List<JudgeCondition> judgeConditions_H = new List<JudgeCondition>();
            judgeConditionData_H.JudgeConditions = judgeConditions_H;
            //
            JudgeConditionBrand conditionH_1_A = new JudgeConditionBrand(true, "Apple/iPhone/iPad", JudgeConditionType.UnEqual);//非苹果
            JudgeConditionCpuCount conditionH_1_B = new JudgeConditionCpuCount(true, 8, JudgeConditionType.GEqual, 10, JudgeConditionType.Less);//8<=Cpu核数<10
            JudgeConditionCpuFrequency conditionH_1_C = new JudgeConditionCpuFrequency(false, 3f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//主频<3
            JudgeConditionMemory conditionH_1_D = new JudgeConditionMemory(false, 5f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=3.6
            // JudgeConditionShaderLevel conditionH_1_E = new JudgeConditionShaderLevel(false, 40, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.LEqual);//着色器等级>=40
            // JudgeConditionIsMRT conditionH_1_F = new JudgeConditionIsMRT(false, true, JudgeConditionType.Equal);//支持MRT
            // JudgeConditionIsComputerShader conditionH_1_G = new JudgeConditionIsComputerShader(false, true, JudgeConditionType.Equal);//支持计算着色器
            // JudgeConditionSupportsMultisampledTextures conditionH_1_H = new JudgeConditionSupportsMultisampledTextures(false, true, JudgeConditionType.Equal);//支持多重采样
                                                                                                                                                              //
            JudgeConditionRelation relationH_1 = CombinationConditions(conditionH_1_A, conditionH_1_B, conditionH_1_C, conditionH_1_D);
            //
            JudgeConditionBrand conditionH_2_A = new JudgeConditionBrand(true, "Apple/iPhone/iPad", JudgeConditionType.UnEqual);//非苹果
            JudgeConditionCpuCount conditionH_2_B = new JudgeConditionCpuCount(true, 10, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//Cpu核数>=10
            JudgeConditionCpuFrequency conditionH_2_C = new JudgeConditionCpuFrequency(false, 2.5f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//主频<2.2
            JudgeConditionMemory conditionH_2_D = new JudgeConditionMemory(false, 5f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=3.6
            // JudgeConditionShaderLevel conditionH_2_E = new JudgeConditionShaderLevel(false, 40, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.LEqual);//着色器等级>=40
            // JudgeConditionIsMRT conditionH_2_F = new JudgeConditionIsMRT(false, true, JudgeConditionType.Equal);//支持MRT
            // JudgeConditionIsComputerShader conditionH_2_G = new JudgeConditionIsComputerShader(false, true, JudgeConditionType.Equal);//支持计算着色器
            // JudgeConditionSupportsMultisampledTextures conditionH_2_H = new JudgeConditionSupportsMultisampledTextures(false, true, JudgeConditionType.Equal);//支持多重采样
                                                                                                                                                              //
            JudgeConditionRelation relationH_2 = CombinationConditions(conditionH_2_A, conditionH_2_B, conditionH_2_C, conditionH_2_D);
            //
            JudgeConditionRelation relationH = new JudgeConditionRelation(relationH_1, relationH_2, ConditionRelation.Or);
            //
            judgeConditions_H.Add(relationH);

            #endregion

            #region//超高配

            //超高配
            //[Cpu核数>=10] 并且 [主频>=4ghz] 并且 [内存]>=6g 并且 [着色器级别]>=50 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true 并且 [几何着色器]=true 并且 [曲面细分着色器]=true
            //[Cpu核数>=12] 并且 [主频>=4ghz] 并且 [内存]>=6g 并且 [着色器级别]>=50 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true 并且 [几何着色器]=true 并且 [曲面细分着色器]=true
            JudgeConditionData judgeConditionData_HH = new JudgeConditionData(3, "超高配");
            judgeConditionDataDic.Add(judgeConditionData_HH.level, judgeConditionData_HH);
            List<JudgeCondition> judgeConditions_HH = new List<JudgeCondition>();
            judgeConditionData_HH.JudgeConditions = judgeConditions_HH;
            //
            JudgeConditionBrand conditionHH_1_A = new JudgeConditionBrand(true, "Apple/iPhone/iPad", JudgeConditionType.UnEqual);//非苹果
            JudgeConditionCpuCount conditionHH_1_B = new JudgeConditionCpuCount(true, 10, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//Cpu核数>=10
            JudgeConditionCpuFrequency conditionHH_1_C = new JudgeConditionCpuFrequency(false, 4f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//主频>=2.2
            JudgeConditionMemory conditionHH_1_D = new JudgeConditionMemory(false, 6f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=3.6
            // JudgeConditionShaderLevel conditionHH_1_E = new JudgeConditionShaderLevel(false, 50, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//着色器级别>=50
            // JudgeConditionIsMRT conditionHH_1_F = new JudgeConditionIsMRT(false, true, JudgeConditionType.Equal);//支持MRT
            // JudgeConditionIsComputerShader conditionHH_1_G = new JudgeConditionIsComputerShader(false, true, JudgeConditionType.Equal);//支持计算着色器
            // JudgeConditionSupportsMultisampledTextures conditionHH_1_H = new JudgeConditionSupportsMultisampledTextures(false, true, JudgeConditionType.Equal);//支持多重采样
            // JudgeConditionSupportsGeometryShaders conditionHH_1_I = new JudgeConditionSupportsGeometryShaders(false, true, JudgeConditionType.Equal);//支持几何着色器
            // JudgeConditionSupportsTessellationShaders conditionHH_1_J = new JudgeConditionSupportsTessellationShaders(false, true, JudgeConditionType.Equal);//支持曲面细分着色器
                                                                                                                                                             //
            JudgeConditionRelation relationHH_1 = CombinationConditions(conditionHH_1_A, conditionHH_1_B, conditionHH_1_C, conditionHH_1_D);
            //
            JudgeConditionBrand conditionHH_2_A = new JudgeConditionBrand(true, "Apple/iPhone/iPad", JudgeConditionType.UnEqual);//非苹果
            JudgeConditionCpuCount conditionHH_2_B = new JudgeConditionCpuCount(true, 12, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//Cpu核数>=8
            JudgeConditionCpuFrequency conditionHH_2_C = new JudgeConditionCpuFrequency(false, 4f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//主频>=3
            JudgeConditionMemory conditionHH_2_D = new JudgeConditionMemory(false, 6f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.Less);//内存>=3.6
            // JudgeConditionShaderLevel conditionHH_2_E = new JudgeConditionShaderLevel(false, 50, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//着色器级别>=50
            // JudgeConditionIsMRT conditionHH_2_F = new JudgeConditionIsMRT(false, true, JudgeConditionType.Equal);//支持MRT
            // JudgeConditionIsComputerShader conditionHH_2_G = new JudgeConditionIsComputerShader(false, true, JudgeConditionType.Equal);//支持计算着色器
            // JudgeConditionSupportsMultisampledTextures conditionHH_2_H = new JudgeConditionSupportsMultisampledTextures(false, true, JudgeConditionType.Equal);//支持多重采样
            // JudgeConditionSupportsGeometryShaders conditionHH_2_I = new JudgeConditionSupportsGeometryShaders(false, true, JudgeConditionType.Equal);//支持几何着色器
            // JudgeConditionSupportsTessellationShaders conditionHH_2_J = new JudgeConditionSupportsTessellationShaders(false, true, JudgeConditionType.Equal);//支持曲面细分着色器
            //                                                                                                                                                  //
            JudgeConditionRelation relationHH_2 = CombinationConditions(conditionHH_2_A, conditionHH_2_B, conditionHH_2_C, conditionHH_2_D);
            //
            JudgeConditionRelation relationHH = new JudgeConditionRelation(relationHH_1, relationHH_2, ConditionRelation.Or);
            //
            judgeConditions_HH.Add(relationHH);

            #endregion

            return judgeConditionDataDic;
        }
    }

    #endregion

    #region//苹果手机机型分级配置


    static Dictionary<int, JudgeConditionData> appleJudgeConditionDataDic;

    /// <summary>
    /// 苹果分级 key=等级  等级需要逐级增加
    /// </summary>
    public static Dictionary<int, JudgeConditionData> AppleJudgeConditionDataDic
    {
        get
        {
            if (appleJudgeConditionDataDic == null)
            {
                appleJudgeConditionDataDic = new Dictionary<int, JudgeConditionData>();
            }
            appleJudgeConditionDataDic.Clear();

            #region//低配

            //低配-默认低配开始
            JudgeConditionData judgeConditionData_L = new JudgeConditionData(0, "低配");
            appleJudgeConditionDataDic.Add(judgeConditionData_L.level, judgeConditionData_L);
            List<JudgeCondition> judgeConditions_L = new List<JudgeCondition>();
            judgeConditionData_L.JudgeConditions = judgeConditions_L;
            //
            JudgeConditionBrand conditionL_1_A = new JudgeConditionBrand(true, "Apple/iPhone/iPad", JudgeConditionType.Equal);//苹果
            JudgeConditionCpuCount conditionL_1_B = new JudgeConditionCpuCount(true, 0, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.LEqual);
                                                                                                                                                 //
            JudgeConditionRelation relationL = CombinationConditions(conditionL_1_A, conditionL_1_B);
            //
            judgeConditions_L.Add(relationL);

            #endregion

            #region//中配

            //中配
            //[Cpu核数>2] 并且 [Cpu核数<6] 并且 [内存]>=2.5g 并且 [着色器级别]>=30
            JudgeConditionData judgeConditionData_M = new JudgeConditionData(1, "中配");
            appleJudgeConditionDataDic.Add(judgeConditionData_M.level, judgeConditionData_M);
            List<JudgeCondition> judgeConditions_M = new List<JudgeCondition>();
            judgeConditionData_M.JudgeConditions = judgeConditions_M;
            //
            JudgeConditionBrand conditionM_1_A = new JudgeConditionBrand(true, "Apple/iPhone/iPad", JudgeConditionType.Equal);//苹果
            JudgeConditionCpuCount conditionM_1_B = new JudgeConditionCpuCount(true, 2, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//2<Cpu核数<6
            JudgeConditionMemory conditionM_1_C = new JudgeConditionMemory(false, 2.5f, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.LEqual);//内存<=3
            JudgeConditionShaderLevel conditionM_1_D = new JudgeConditionShaderLevel(false, 30, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//着色器等级>=30
                                                                                                                                                                  //
            JudgeConditionRelation relationM = CombinationConditions(conditionM_1_A, conditionM_1_B, conditionM_1_C, conditionM_1_D);
            //
            judgeConditions_M.Add(relationM);

            #endregion

            #region//高配

            //高配
            //[Cpu核数>=6] 并且 [内存]>=3g 并且 [着色器级别]>=40 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true
            JudgeConditionData judgeConditionData_H = new JudgeConditionData(2, "高配");
            appleJudgeConditionDataDic.Add(judgeConditionData_H.level, judgeConditionData_H);
            List<JudgeCondition> judgeConditions_H = new List<JudgeCondition>();
            judgeConditionData_H.JudgeConditions = judgeConditions_H;
            //
            JudgeConditionBrand conditionH_1_A = new JudgeConditionBrand(true, "Apple/iPhone/iPad", JudgeConditionType.Equal);//苹果
            JudgeConditionCpuCount conditionH_1_B = new JudgeConditionCpuCount(true, 6, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.LEqual);//Cpu核数>=6
            JudgeConditionMemory conditionH_1_C = new JudgeConditionMemory(false, 3, JudgeConditionType.Greater, 5, JudgeConditionType.Less);//3<内存<5
            JudgeConditionShaderLevel conditionH_1_D = new JudgeConditionShaderLevel(false, 40, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//着色器等级>=40
            JudgeConditionIsMRT conditionH_1_E = new JudgeConditionIsMRT(false, true, JudgeConditionType.Equal);//支持MRT
            JudgeConditionIsComputerShader conditionH_1_F = new JudgeConditionIsComputerShader(false, true, JudgeConditionType.Equal);//支持计算着色器
            JudgeConditionSupportsMultisampledTextures conditionH_1_G = new JudgeConditionSupportsMultisampledTextures(false, true, JudgeConditionType.Equal);//支持多重采样
                                                                                                                                                              //
            JudgeConditionRelation relationH = CombinationConditions(conditionH_1_A, conditionH_1_B, conditionH_1_C, conditionH_1_D, conditionH_1_E, conditionH_1_F, conditionH_1_G);
            //
            judgeConditions_H.Add(relationH);

            #endregion

            #region//超高配

            //超高配
            //[Cpu核数>=6] 并且 [内存]>=5g 并且 [着色器级别]>=50 并且 [MRT]=true 并且 [计算着色器]=true 并且 [多重采样]=true 并且 [几何着色器]=true 并且 [曲面细分着色器]=true
            JudgeConditionData judgeConditionData_HH = new JudgeConditionData(3, "超高配");
            appleJudgeConditionDataDic.Add(judgeConditionData_HH.level, judgeConditionData_HH);
            List<JudgeCondition> judgeConditions_HH = new List<JudgeCondition>();
            judgeConditionData_HH.JudgeConditions = judgeConditions_HH;
            //
            JudgeConditionBrand conditionHH_1_A = new JudgeConditionBrand(true, "Apple/iPhone/iPad", JudgeConditionType.Equal);//苹果
            JudgeConditionCpuCount conditionHH_1_B = new JudgeConditionCpuCount(true, 6, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.LEqual);//Cpu核数>=6
            JudgeConditionMemory conditionHH_1_C = new JudgeConditionMemory(false, 5, JudgeConditionType.GEqual, float.MaxValue, JudgeConditionType.LEqual);//内存>=5
            JudgeConditionShaderLevel conditionHH_1_D = new JudgeConditionShaderLevel(false, 50, JudgeConditionType.GEqual, int.MaxValue, JudgeConditionType.Less);//着色器等级>=50
            JudgeConditionIsMRT conditionHH_1_E = new JudgeConditionIsMRT(false, true, JudgeConditionType.Equal);//支持MRT
            JudgeConditionIsComputerShader conditionHH_1_F = new JudgeConditionIsComputerShader(false, true, JudgeConditionType.Equal);//支持计算着色器
            JudgeConditionSupportsMultisampledTextures conditionHH_1_G = new JudgeConditionSupportsMultisampledTextures(false, true, JudgeConditionType.Equal);//支持多重采样
            //JudgeConditionSupportsGeometryShaders conditionHH_1_H = new JudgeConditionSupportsGeometryShaders(false, true, JudgeConditionType.Equal);//支持几何着色器
            JudgeConditionSupportsTessellationShaders conditionHH_1_I = new JudgeConditionSupportsTessellationShaders(false, true, JudgeConditionType.Equal);//支持曲面细分着色器
                                                                                                                                                             //
            JudgeConditionRelation relationHH = CombinationConditions(conditionHH_1_A, conditionHH_1_B, conditionHH_1_C, conditionHH_1_D, conditionHH_1_E, conditionHH_1_F, conditionHH_1_G, conditionHH_1_I);
            //
            judgeConditions_HH.Add(relationHH);

            #endregion

            return appleJudgeConditionDataDic;
        }
    }

    #endregion

    #region//机型分级条件定义

    /// <summary>
    /// 分级条件
    /// </summary>
    public class JudgeConditionData
    {
        public JudgeConditionData(int level, string key)
        {
            this.level = level;
            this.key = key;
        }

        public int level;

        public string key;

        public List<JudgeCondition> JudgeConditions = new List<JudgeCondition>();
    }

    /// <summary>
    /// 分级条件
    /// </summary>
    public class JudgeCondition
    {
        /// <summary>
        /// 是否为主要参数
        /// </summary>
        public bool isMajorParameter = false;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool IsCondition(EquipmentData data)
        {
            return false;
        }
    }

    enum JudgeConditionType
    {
        Equal,
        UnEqual,
        Less,
        LEqual,
        Greater,
        GEqual,
    }

    /// <summary>
    /// 内存条件
    /// </summary>
    class JudgeConditionMemory : JudgeCondition
    {
        public JudgeConditionMemory(bool isMajorParameter, float minMemory, JudgeConditionType minJudgeConditionType, float maxMemory, JudgeConditionType maxJudgeConditionType)
        {
            this.isMajorParameter = isMajorParameter;
            this.minMemory = minMemory;
            this.maxMemory = maxMemory;
            this.minJudgeConditionType = minJudgeConditionType;
            this.maxJudgeConditionType = maxJudgeConditionType;
        }

        public float minMemory;

        public JudgeConditionType minJudgeConditionType;

        public float maxMemory;

        public JudgeConditionType maxJudgeConditionType;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool IsCondition(EquipmentData data)
        {
            bool min = false;
            switch (minJudgeConditionType)
            {
                case JudgeConditionType.Equal:
                    {
                        min = data.memory == minMemory;
                    }
                    break;
                case JudgeConditionType.UnEqual:
                    {
                        min = data.memory != minMemory;
                    }
                    break;
                case JudgeConditionType.Less:
                    {
                        min = data.memory < minMemory;
                    }
                    break;
                case JudgeConditionType.LEqual:
                    {
                        min = data.memory <= minMemory;
                    }
                    break;
                case JudgeConditionType.Greater:
                    {
                        min = data.memory > minMemory;
                    }
                    break;
                case JudgeConditionType.GEqual:
                    {
                        min = data.memory >= minMemory;
                    }
                    break;
            }
            bool max = false;
            switch (maxJudgeConditionType)
            {
                case JudgeConditionType.Equal:
                    {
                        max = data.memory == maxMemory;
                    }
                    break;
                case JudgeConditionType.UnEqual:
                    {
                        max = data.memory != maxMemory;
                    }
                    break;
                case JudgeConditionType.Less:
                    {
                        max = data.memory < maxMemory;
                    }
                    break;
                case JudgeConditionType.LEqual:
                    {
                        max = data.memory <= maxMemory;
                    }
                    break;
                case JudgeConditionType.Greater:
                    {
                        max = data.memory > maxMemory;
                    }
                    break;
                case JudgeConditionType.GEqual:
                    {
                        max = data.memory >= maxMemory;
                    }
                    break;
            }
            return min & max;
        }
    }

    /// <summary>
    /// 分辨率
    /// </summary>
    class JudgeConditionScreen : JudgeCondition
    {
        public JudgeConditionScreen(bool isMajorParameter, int minScreen, JudgeConditionType minJudgeConditionType, int maxScreen, JudgeConditionType maxJudgeConditionType)
        {
            this.isMajorParameter = isMajorParameter;
            this.minScreen = minScreen;
            this.maxScreen = maxScreen;
            this.minJudgeConditionType = minJudgeConditionType;
            this.maxJudgeConditionType = maxJudgeConditionType;
        }

        public int minScreen;

        public JudgeConditionType minJudgeConditionType;

        public int maxScreen;

        public JudgeConditionType maxJudgeConditionType;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool IsCondition(EquipmentData data)
        {
            bool min = false;
            switch (minJudgeConditionType)
            {
                case JudgeConditionType.Equal:
                    {
                        min = data.screen == minScreen;
                    }
                    break;
                case JudgeConditionType.UnEqual:
                    {
                        min = data.screen != minScreen;
                    }
                    break;
                case JudgeConditionType.Less:
                    {
                        min = data.screen < minScreen;
                    }
                    break;
                case JudgeConditionType.LEqual:
                    {
                        min = data.screen <= minScreen;
                    }
                    break;
                case JudgeConditionType.Greater:
                    {
                        min = data.screen > minScreen;
                    }
                    break;
                case JudgeConditionType.GEqual:
                    {
                        min = data.screen >= minScreen;
                    }
                    break;
            }
            bool max = false;
            switch (maxJudgeConditionType)
            {
                case JudgeConditionType.Equal:
                    {
                        max = data.screen == maxScreen;
                    }
                    break;
                case JudgeConditionType.UnEqual:
                    {
                        max = data.screen != maxScreen;
                    }
                    break;
                case JudgeConditionType.Less:
                    {
                        max = data.screen < maxScreen;
                    }
                    break;
                case JudgeConditionType.LEqual:
                    {
                        max = data.screen <= maxScreen;
                    }
                    break;
                case JudgeConditionType.Greater:
                    {
                        max = data.screen > maxScreen;
                    }
                    break;
                case JudgeConditionType.GEqual:
                    {
                        max = data.screen >= maxScreen;
                    }
                    break;
            }
            return min & max;
        }
    }

    /// <summary>
    /// 着色器级别
    /// </summary>
    class JudgeConditionShaderLevel : JudgeCondition
    {
        public JudgeConditionShaderLevel(bool isMajorParameter, int minShaderLevel, JudgeConditionType minJudgeConditionType, int maxShaderLevel, JudgeConditionType maxJudgeConditionType)
        {
            this.isMajorParameter = isMajorParameter;
            this.minShaderLevel = minShaderLevel;
            this.maxShaderLevel = maxShaderLevel;
            this.minJudgeConditionType = minJudgeConditionType;
            this.maxJudgeConditionType = maxJudgeConditionType;
        }

        public int minShaderLevel;

        public JudgeConditionType minJudgeConditionType;

        public int maxShaderLevel;

        public JudgeConditionType maxJudgeConditionType;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool IsCondition(EquipmentData data)
        {
            bool min = false;
            switch (minJudgeConditionType)
            {
                case JudgeConditionType.Equal:
                    {
                        min = data.shaderLevel == minShaderLevel;
                    }
                    break;
                case JudgeConditionType.UnEqual:
                    {
                        min = data.shaderLevel != minShaderLevel;
                    }
                    break;
                case JudgeConditionType.Less:
                    {
                        min = data.shaderLevel < minShaderLevel;
                    }
                    break;
                case JudgeConditionType.LEqual:
                    {
                        min = data.shaderLevel <= minShaderLevel;
                    }
                    break;
                case JudgeConditionType.Greater:
                    {
                        min = data.shaderLevel > minShaderLevel;
                    }
                    break;
                case JudgeConditionType.GEqual:
                    {
                        min = data.shaderLevel >= minShaderLevel;
                    }
                    break;
            }
            bool max = false;
            switch (maxJudgeConditionType)
            {
                case JudgeConditionType.Equal:
                    {
                        max = data.shaderLevel == maxShaderLevel;
                    }
                    break;
                case JudgeConditionType.UnEqual:
                    {
                        max = data.shaderLevel != maxShaderLevel;
                    }
                    break;
                case JudgeConditionType.Less:
                    {
                        max = data.shaderLevel < maxShaderLevel;
                    }
                    break;
                case JudgeConditionType.LEqual:
                    {
                        max = data.shaderLevel <= maxShaderLevel;
                    }
                    break;
                case JudgeConditionType.Greater:
                    {
                        max = data.shaderLevel > maxShaderLevel;
                    }
                    break;
                case JudgeConditionType.GEqual:
                    {
                        max = data.shaderLevel >= maxShaderLevel;
                    }
                    break;
            }
            return min & max;
        }
    }

    /// <summary>
    /// 是否支持MRT
    /// </summary>
    class JudgeConditionIsMRT : JudgeCondition
    {
        public JudgeConditionIsMRT(bool isMajorParameter, bool isMRT, JudgeConditionType judgeConditionType)
        {
            this.isMajorParameter = isMajorParameter;
            this.isMRT = isMRT;
            this.judgeConditionType = judgeConditionType;
        }

        public bool isMRT;

        public JudgeConditionType judgeConditionType;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool IsCondition(EquipmentData data)
        {
            switch (judgeConditionType)
            {
                case JudgeConditionType.Equal:
                case JudgeConditionType.GEqual:
                case JudgeConditionType.LEqual:
                    {
                        return data.isMRT == isMRT;
                    }
                default:
                    {
                        return data.isMRT != isMRT;
                    }
            }
        }
    }

    /// <summary>
    /// 是否支持多重采样
    /// </summary>
    class JudgeConditionSupportsMultisampledTextures : JudgeCondition
    {
        public JudgeConditionSupportsMultisampledTextures(bool isMajorParameter, bool supportsMultisampledTextures, JudgeConditionType judgeConditionType)
        {
            this.isMajorParameter = isMajorParameter;
            this.supportsMultisampledTextures = supportsMultisampledTextures;
            this.judgeConditionType = judgeConditionType;
        }

        public bool supportsMultisampledTextures;

        public JudgeConditionType judgeConditionType;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool IsCondition(EquipmentData data)
        {
            switch (judgeConditionType)
            {
                case JudgeConditionType.Equal:
                case JudgeConditionType.GEqual:
                case JudgeConditionType.LEqual:
                    {
                        return data.supportsMultisampledTextures == supportsMultisampledTextures;
                    }
                default:
                    {
                        return data.supportsMultisampledTextures != supportsMultisampledTextures;
                    }
            }
        }
    }

    /// <summary>
    /// 是否支持曲面细分
    /// </summary>
    class JudgeConditionSupportsTessellationShaders : JudgeCondition
    {
        public JudgeConditionSupportsTessellationShaders(bool isMajorParameter, bool supportsTessellationShaders, JudgeConditionType judgeConditionType)
        {
            this.isMajorParameter = isMajorParameter;
            this.supportsTessellationShaders = supportsTessellationShaders;
            this.judgeConditionType = judgeConditionType;
        }

        public bool supportsTessellationShaders;

        public JudgeConditionType judgeConditionType;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool IsCondition(EquipmentData data)
        {
            switch (judgeConditionType)
            {
                case JudgeConditionType.Equal:
                case JudgeConditionType.GEqual:
                case JudgeConditionType.LEqual:
                    {
                        return data.supportsTessellationShaders == supportsTessellationShaders;
                    }
                default:
                    {
                        return data.supportsTessellationShaders != supportsTessellationShaders;
                    }
            }
        }
    }

    /// <summary>
    /// 是否支持几何着色器
    /// </summary>
    class JudgeConditionSupportsGeometryShaders : JudgeCondition
    {
        public JudgeConditionSupportsGeometryShaders(bool isMajorParameter, bool supportsGeometryShaders, JudgeConditionType judgeConditionType)
        {
            this.isMajorParameter = isMajorParameter;
            this.supportsGeometryShaders = supportsGeometryShaders;
            this.judgeConditionType = judgeConditionType;
        }

        public bool supportsGeometryShaders;

        public JudgeConditionType judgeConditionType;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool IsCondition(EquipmentData data)
        {
            switch (judgeConditionType)
            {
                case JudgeConditionType.Equal:
                case JudgeConditionType.GEqual:
                case JudgeConditionType.LEqual:
                    {
                        return data.supportsGeometryShaders == supportsGeometryShaders;
                    }
                default:
                    {
                        return data.supportsGeometryShaders != supportsGeometryShaders;
                    }
            }
        }
    }

    /// <summary>
    /// 是否支持ComputerShader
    /// </summary>
    class JudgeConditionIsComputerShader : JudgeCondition
    {
        public JudgeConditionIsComputerShader(bool isMajorParameter, bool isComputerShader, JudgeConditionType judgeConditionType)
        {
            this.isMajorParameter = isMajorParameter;
            this.isComputerShader = isComputerShader;
            this.judgeConditionType = judgeConditionType;
        }

        public bool isComputerShader;

        public JudgeConditionType judgeConditionType;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool IsCondition(EquipmentData data)
        {
            switch (judgeConditionType)
            {
                case JudgeConditionType.Equal:
                case JudgeConditionType.GEqual:
                case JudgeConditionType.LEqual:
                    {
                        return data.isComputerShader == isComputerShader;
                    }
                default:
                    {
                        return data.isComputerShader != isComputerShader;
                    }
            }
        }
    }

    /// <summary>
    /// 核数条件
    /// </summary>
    class JudgeConditionCpuCount : JudgeCondition
    {
        public JudgeConditionCpuCount(bool isMajorParameter, int minCpuCount, JudgeConditionType minJudgeConditionType, int maxCpuCount, JudgeConditionType maxJudgeConditionType)
        {
            this.isMajorParameter = isMajorParameter;
            this.minCpuCount = minCpuCount;
            this.maxCpuCount = maxCpuCount;
            this.minJudgeConditionType = minJudgeConditionType;
            this.maxJudgeConditionType = maxJudgeConditionType;
        }

        public int minCpuCount;

        public JudgeConditionType minJudgeConditionType;

        public int maxCpuCount;

        public JudgeConditionType maxJudgeConditionType;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool IsCondition(EquipmentData data)
        {
            bool min = false;
            switch (minJudgeConditionType)
            {
                case JudgeConditionType.Equal:
                    {
                        min = data.cpuCount == minCpuCount;
                    }
                    break;
                case JudgeConditionType.UnEqual:
                    {
                        min = data.cpuCount != minCpuCount;
                    }
                    break;
                case JudgeConditionType.Less:
                    {
                        min = data.cpuCount < minCpuCount;
                    }
                    break;
                case JudgeConditionType.LEqual:
                    {
                        min = data.cpuCount <= minCpuCount;
                    }
                    break;
                case JudgeConditionType.Greater:
                    {
                        min = data.cpuCount > minCpuCount;
                    }
                    break;
                case JudgeConditionType.GEqual:
                    {
                        min = data.cpuCount >= minCpuCount;
                    }
                    break;
            }
            bool max = false;
            switch (maxJudgeConditionType)
            {
                case JudgeConditionType.Equal:
                    {
                        max = data.cpuCount == maxCpuCount;
                    }
                    break;
                case JudgeConditionType.UnEqual:
                    {
                        max = data.cpuCount != maxCpuCount;
                    }
                    break;
                case JudgeConditionType.Less:
                    {
                        max = data.cpuCount < maxCpuCount;
                    }
                    break;
                case JudgeConditionType.LEqual:
                    {
                        max = data.cpuCount <= maxCpuCount;
                    }
                    break;
                case JudgeConditionType.Greater:
                    {
                        max = data.cpuCount > maxCpuCount;
                    }
                    break;
                case JudgeConditionType.GEqual:
                    {
                        max = data.cpuCount >= maxCpuCount;
                    }
                    break;
            }
            return min & max;
        }
    }

    /// <summary>
    /// 主频条件
    /// </summary>
    class JudgeConditionCpuFrequency : JudgeCondition
    {
        public JudgeConditionCpuFrequency(bool isMajorParameter, float minFrequency, JudgeConditionType minJudgeConditionType, float maxFrequency, JudgeConditionType maxJudgeConditionType)
        {
            this.isMajorParameter = isMajorParameter;
            this.minFrequency = minFrequency;
            this.maxFrequency = maxFrequency;
            this.minJudgeConditionType = minJudgeConditionType;
            this.maxJudgeConditionType = maxJudgeConditionType;
        }

        public float minFrequency;

        public JudgeConditionType minJudgeConditionType;

        public float maxFrequency;

        public JudgeConditionType maxJudgeConditionType;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool IsCondition(EquipmentData data)
        {
            bool min = false;
            switch (minJudgeConditionType)
            {
                case JudgeConditionType.Equal:
                    {
                        min = data.cpuFrequency == minFrequency;
                    }
                    break;
                case JudgeConditionType.UnEqual:
                    {
                        min = data.cpuFrequency != minFrequency;
                    }
                    break;
                case JudgeConditionType.Less:
                    {
                        min = data.cpuFrequency < minFrequency;
                    }
                    break;
                case JudgeConditionType.LEqual:
                    {
                        min = data.cpuFrequency <= minFrequency;
                    }
                    break;
                case JudgeConditionType.Greater:
                    {
                        min = data.cpuFrequency > minFrequency;
                    }
                    break;
                case JudgeConditionType.GEqual:
                    {
                        min = data.cpuFrequency >= minFrequency;
                    }
                    break;
            }
            bool max = false;
            switch (maxJudgeConditionType)
            {
                case JudgeConditionType.Equal:
                    {
                        max = data.cpuFrequency == maxFrequency;
                    }
                    break;
                case JudgeConditionType.UnEqual:
                    {
                        max = data.cpuFrequency != maxFrequency;
                    }
                    break;
                case JudgeConditionType.Less:
                    {
                        max = data.cpuFrequency < maxFrequency;
                    }
                    break;
                case JudgeConditionType.LEqual:
                    {
                        max = data.cpuFrequency <= maxFrequency;
                    }
                    break;
                case JudgeConditionType.Greater:
                    {
                        max = data.cpuFrequency > maxFrequency;
                    }
                    break;
                case JudgeConditionType.GEqual:
                    {
                        max = data.cpuFrequency >= maxFrequency;
                    }
                    break;
            }
            return min & max;
        }
    }

    /// <summary>
    /// 型号条件
    /// </summary>
    class JudgeConditionBrand : JudgeCondition
    {
        public JudgeConditionBrand(bool isMajorParameter, string brandName, JudgeConditionType brandNameJudgeConditionType)
        {
            this.isMajorParameter = isMajorParameter;
            this.brandName = brandName;
            this.brandNameJudgeConditionType = brandNameJudgeConditionType;
            if (!string.IsNullOrEmpty(this.brandName))
            {
                setNames = this.brandName.Split('/');
            }
        }

        public string brandName;

        string[] setNames;

        public JudgeConditionType brandNameJudgeConditionType;

        /// <summary>
        /// 条件是否达成
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool IsCondition(EquipmentData data)
        {
            switch (brandNameJudgeConditionType)
            {
                case JudgeConditionType.Equal:
                case JudgeConditionType.GEqual:
                case JudgeConditionType.LEqual:
                    {
                        if (setNames==null || string.IsNullOrEmpty(data.modelName))
                        {
                            return false;
                        }
                        else
                        {
                            bool find = false;
                            for (int i=0;i< setNames.Length;++i)
                            {
                                string str = setNames[i];
                                if (!string.IsNullOrEmpty(str))
                                {
                                    if (data.modelName.Contains(str))
                                    {
                                        find = true;
                                        break;
                                    }
                                }
                            }
                            return find;
                        }
                    }
                    break;
                default:
                    {
                        if (setNames!=null && !string.IsNullOrEmpty(data.modelName))
                        {
                            bool find = false;
                            for (int i = 0; i < setNames.Length; ++i)
                            {
                                string str = setNames[i];
                                if (!string.IsNullOrEmpty(str))
                                {
                                    if (data.modelName.Contains(str))
                                    {
                                        find = true;
                                        break;
                                    }
                                }
                            }
                            if (find)
                            {
                                return false;
                            }
                        }
                    }
                    break;
            }
            return true;
        }
    }

    enum ConditionRelation
    {
        /// <summary>
        /// 与
        /// </summary>
        And,
        /// <summary>
        /// 或
        /// </summary>
        Or,
    }

    /// <summary>
    /// 判断条件与条件之间的关系
    /// </summary>
    class JudgeConditionRelation : JudgeCondition
    {
        public JudgeConditionRelation(JudgeCondition conditionA, JudgeCondition conditionB, ConditionRelation relation)
        {
            this.conditionA = conditionA;
            this.conditionB = conditionB;
            this.relation = relation;
            majorParameterFail = false;
        }

        public JudgeCondition conditionA;

        public JudgeCondition conditionB;

        public ConditionRelation relation;

        /// <summary>
        /// 主要参数是否全部达成
        /// </summary>
        public bool majorParameterFail = false;

        public override bool IsCondition(EquipmentData data)
        {
            if (conditionA == null && conditionB == null)
            {
                return false;
            }
            else if (conditionA == null)
            {
                conditionA = conditionB;
            }
            else if (conditionB == null)
            {
                conditionB = conditionA;
            }
            bool aResult = conditionA.IsCondition(data);
            bool aMainResult = false;
            if (conditionA.GetType() == typeof(JudgeConditionRelation))
            {
                aMainResult = ((JudgeConditionRelation)conditionA).majorParameterFail;
            }
            else
            {
                if (conditionA.isMajorParameter && !aResult)
                {
                    aMainResult = true;
                }
            }
            bool bResult = conditionB.IsCondition(data);
            bool bMainResult = false;
            if (conditionB.GetType() == typeof(JudgeConditionRelation))
            {
                bMainResult = ((JudgeConditionRelation)conditionB).majorParameterFail;
            }
            else
            {
                if (conditionA.isMajorParameter && !bResult)
                {
                    bMainResult = true;
                }
            }
            switch (relation)
            {
                case ConditionRelation.And:
                    {
                        if (aMainResult || bMainResult)
                        {
                            majorParameterFail = true;
                        }
                        return aResult && bResult;
                    }
                case ConditionRelation.Or:
                    {
                        if (!aResult && !bResult)
                        {
                            if (aMainResult && bMainResult)
                            {
                                majorParameterFail = true;
                            }
                        }
                        return aResult || bResult;
                    }
            }
            return false;
        }

    }

    #endregion

    #endregion

}

