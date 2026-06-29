using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 自定义体型控制器
/// 用于实现六种不同的体型数据，并对指定骨骼节点进行缩放
/// </summary>
public class CustomBodyTypeController : MonoBehaviour
{
    [System.Serializable]
    public enum BodyType
    {
        None = 1001,
        Type1 = 1002,  // 体型1
        Type2 = 1003,  // 体型2
        Type3 = 1004,  // 体型3
        Type4 = 1005,  // 体型4
        Type5 = 1006,  // 体型5
        Type6 = 1007   // 体型6
    }

    [System.Serializable]
    public class BodyTypeData
    {
        public Vector3 entityScale = Vector3.one;
        public Vector3 leftThighScale;
        public Vector3 rightThighScale;
        public Vector3 headScale;
        public Vector3 leftClavicleScale;
        public Vector3 rightClavicleScale;
    }

    [Header("体型设置")]
    [SerializeField] private BodyType currentBodyType = BodyType.None;
    [SerializeField] private Dictionary<BodyType, BodyTypeData> bodyTypeDataList = new Dictionary<BodyType, BodyTypeData>();

    [Header("骨骼节点")]
    [SerializeField] private Transform leftThigh;
    [SerializeField] private Transform rightThigh;
    [SerializeField] private Transform head;
    [SerializeField] private Transform leftClavicle;
    [SerializeField] private Transform rightClavicle;

    // 原始缩放值缓存
    private Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
    private Vector3 originalEntityScale = Vector3.one;

    // 当前应用的体型缩放值
    private Dictionary<Transform, Vector3> currentBodyTypeScales = new Dictionary<Transform, Vector3>();

    // 动画缩放值缓存（用于处理动画冲突）
    private Dictionary<Transform, Vector3> animationScales = new Dictionary<Transform, Vector3>();

    private BodyTypeData currentTypeData;

    private void Awake()
    {
        InitializeBoneReferences();
        CacheOriginalScales();
        InitializeBodyTypeData();
    }

    private void Start()
    {
       // ApplyBodyType(currentBodyType);
    }

    /// <summary>
    /// 初始化骨骼引用
    /// </summary>
    private void InitializeBoneReferences()
    {
        if (leftThigh == null)
            leftThigh = FindBoneTransform("Bip001 L Thigh");
        if (rightThigh == null)
            rightThigh = FindBoneTransform("Bip001 R Thigh");
        if (head == null)
            head = FindBoneTransform("Bip001 Head");
        if (leftClavicle == null)
            leftClavicle = FindBoneTransform("Bip001 L Clavicle");
        if (rightClavicle == null)
            rightClavicle = FindBoneTransform("Bip001 R Clavicle");
    }

    /// <summary>
    /// 查找骨骼变换
    /// </summary>
    /// <param name="boneName">骨骼名称</param>
    /// <returns>骨骼变换组件</returns>
    private Transform FindBoneTransform(string boneName)
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name == boneName)
                return t;
        }
        Debug.LogWarning($"未找到骨骼节点: {boneName}");
        return null;
    }

    /// <summary>
    /// 缓存原始缩放值
    /// </summary>
    private void CacheOriginalScales()
    {
        Transform[] targetBones = { leftThigh, rightThigh, head, leftClavicle, rightClavicle };
        foreach (Transform bone in targetBones)
        {
            if (bone != null)
            {
                originalScales[bone] = bone.localScale;
            }
        }
        originalEntityScale = transform.localScale;
    }

    /// <summary>
    /// 初始化体型数据
    /// </summary>
    private void InitializeBodyTypeData()
    {
        if (bodyTypeDataList.Count == 0)
        {
            // 创建默认体型数据
            BodyTypeData data1 = new BodyTypeData
            {
                leftThighScale = new Vector3(0.6f, 1, 1),
                rightThighScale = new Vector3(0.6f, 1, 1),
                headScale = new Vector3(1.23f, 1.23f, 1.23f),
                leftClavicleScale = new Vector3(0.8f, 0.8f, 0.8f),
                rightClavicleScale = new Vector3(0.8f, 0.8f, 0.8f),
            };
            bodyTypeDataList.Add(BodyType.Type1, data1);

            BodyTypeData data2 = new BodyTypeData
            {
                leftThighScale = new Vector3(0.6f, 1, 1),
                rightThighScale = new Vector3(0.6f, 1, 1),
                headScale = new Vector3(1.5f, 1.5f, 1.5f),
                leftClavicleScale = new Vector3(0.8f, 0.8f, 0.8f),
                rightClavicleScale = new Vector3(0.8f, 0.8f, 0.8f),
            };
            bodyTypeDataList.Add(BodyType.Type2, data2);

            BodyTypeData data3 = new BodyTypeData
            {
                leftThighScale = new Vector3(1.3f, 1.3f, 1.3f),
                rightThighScale = new Vector3(1.3f, 1.3f, 1.3f),
                headScale = new Vector3(0.68f, 0.68f, 0.68f),
                leftClavicleScale = new Vector3(1.03f, 1, 0.9f),
                rightClavicleScale = new Vector3(1.03f, 1, 0.9f),
            };
            bodyTypeDataList.Add(BodyType.Type3, data3);

            BodyTypeData data4 = new BodyTypeData
            {
                leftThighScale = new Vector3(2, 1.2f, 1.25f),
                rightThighScale = new Vector3(2, 1.2f, 1.25f),
                headScale = new Vector3(1, 1, 1),
                leftClavicleScale = new Vector3(1.11f, 1.11f, 1.11f),
                rightClavicleScale = new Vector3(1.11f, 1.11f, 1.11f),
            };
            bodyTypeDataList.Add(BodyType.Type4, data4);

            BodyTypeData data5 = new BodyTypeData
            {
                leftThighScale = new Vector3(1, 1, 1),
                rightThighScale = new Vector3(1, 1, 1),
                headScale = new Vector3(0.5f, 0.5f, 0.5f),
                leftClavicleScale = new Vector3(1, 1, 1),
                rightClavicleScale = new Vector3(1, 1, 1),
            };
            bodyTypeDataList.Add(BodyType.Type5, data5);

            BodyTypeData data6 = new BodyTypeData
            {
                entityScale = new Vector3(0.66f, 0.66f, 0.66f),
                leftThighScale = new Vector3(0.6f, 1, 1),
                rightThighScale = new Vector3(0.6f, 1, 1),
                headScale = new Vector3(1.23f, 1.23f, 1.23f),
                leftClavicleScale = new Vector3(0.8f, 0.8f, 0.8f),
                rightClavicleScale = new Vector3(0.8f, 0.8f, 0.8f),
            };
            bodyTypeDataList.Add(BodyType.Type6, data6);
        }
    }

    /// <summary>
    /// 应用指定体型
    /// </summary>
    /// <param name="bodyType">体型类型</param>
    public void ApplyBodyType(BodyType bodyType)
    {
        currentBodyType = bodyType;
        BodyTypeData data = GetBodyTypeData(bodyType);
        if(data != null)
        {
            transform.localScale = data.entityScale;
        }
        else
        {
            ResetToOriginal();
        }
        //currentTypeData = data;

    }

    private void LateUpdate()
    {
        ApplyBodyTypeScales();
    }

    /// <summary>
    /// 获取体型数据
    /// </summary>
    /// <param name="bodyType">体型类型</param>
    /// <returns>体型数据</returns>
    private BodyTypeData GetBodyTypeData(BodyType bodyType)
    {
        if(bodyTypeDataList.ContainsKey(bodyType))
        {
            return bodyTypeDataList[bodyType];
        }
        return null;
    }

    /// <summary>
    /// 应用体型缩放
    /// </summary>
    /// <param name="data">体型数据</param>
    private void ApplyBodyTypeScales()
    {
        if (currentBodyType == BodyType.None){
            return;
        }

        if (!bodyTypeDataList.TryGetValue(currentBodyType,out BodyTypeData data))
        {
            return;
        }
// #if UNITY_EDITOR
//         transform.localScale = data.entityScale;
// #endif
        // 更新体型缩放缓存
        if (leftThigh != null)
        {
            currentBodyTypeScales[leftThigh] = data.leftThighScale;
            UpdateBoneScale(leftThigh);
        }
        if (rightThigh != null)
        {
            currentBodyTypeScales[rightThigh] = data.rightThighScale;
            UpdateBoneScale(rightThigh);
        }
        if (head != null)
        {
            currentBodyTypeScales[head] = data.headScale;
            UpdateBoneScale(head);
        }
        if (leftClavicle != null)
        {
            currentBodyTypeScales[leftClavicle] = data.leftClavicleScale;
            UpdateBoneScale(leftClavicle);
        }
        if (rightClavicle != null)
        {
            currentBodyTypeScales[rightClavicle] = data.rightClavicleScale;
            UpdateBoneScale(rightClavicle);
        }
    }

    /// <summary>
    /// 更新骨骼缩放（处理动画冲突）
    /// </summary>
    /// <param name="bone">目标骨骼</param>
    private void UpdateBoneScale(Transform bone)
    {
        if (bone == null) return;

        Vector3 originalScale = originalScales.ContainsKey(bone) ? originalScales[bone] : Vector3.one;
        Vector3 bodyTypeScale = currentBodyTypeScales.ContainsKey(bone) ? currentBodyTypeScales[bone] : Vector3.one;
        Vector3 animationScale = animationScales.ContainsKey(bone) ? animationScales[bone] : Vector3.one;

        // 最终缩放 = 原始缩放 * 体型缩放 * 动画缩放
        bone.localScale = Vector3.Scale(Vector3.Scale(originalScale, bodyTypeScale), animationScale);
        // Debug.Log($"UpdateBoneScale bone.scale={bone.localScale},originalScale={originalScale},bodyTypeScale={bodyTypeScale}，animationScale={animationScale}");
    }

    /// <summary>
    /// 设置动画缩放值（由动画系统调用）
    /// </summary>
    /// <param name="boneName">骨骼名称</param>
    /// <param name="scale">缩放值</param>
    public void SetAnimationScale(string boneName, Vector3 scale)
    {
        Transform targetBone = GetBoneByName(boneName);
        if (targetBone != null)
        {
            animationScales[targetBone] = scale;
            UpdateBoneScale(targetBone);
        }
    }

    /// <summary>
    /// 根据名称获取骨骼
    /// </summary>
    /// <param name="boneName">骨骼名称</param>
    /// <returns>骨骼变换</returns>
    private Transform GetBoneByName(string boneName)
    {
        switch (boneName)
        {
            case "Bip001 L Thigh":
                return leftThigh;
            case "Bip001 R Thigh":
                return rightThigh;
            case "Bip001 Head":
                return head;
            case "Bip001 L Clavicle":
                return leftClavicle;
            case "Bip001 R Clavicle":
                return rightClavicle;
            default:
                return null;
        }
    }

    /// <summary>
    /// 重置所有骨骼到原始状态
    /// </summary>
    public void ResetToOriginal()
    {
        Transform[] targetBones = { leftThigh, rightThigh, head, leftClavicle, rightClavicle };
        foreach (Transform bone in targetBones)
        {
            if (bone != null && originalScales.ContainsKey(bone))
            {
                bone.localScale = originalScales[bone];
            }
        }
        transform.localScale = originalEntityScale;
        currentBodyTypeScales.Clear();
        animationScales.Clear();
    }

    /// <summary>
    /// 获取当前体型
    /// </summary>
    /// <returns>当前体型</returns>
    public BodyType GetCurrentBodyType()
    {
        return currentBodyType;
    }





}

