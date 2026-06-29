using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宠物自定义体型控制器
/// 用于实现三种不同的宠物体型数据，并对指定骨骼节点进行缩放
/// </summary>
public class PetCustomBodyTypeController : MonoBehaviour
{
    [System.Serializable]
    public enum PetBodyType
    {
        None,
        Type1,  // 宠物体型1
        Type2,  // 宠物体型2
        Type3   // 宠物体型3
    }

    [System.Serializable]
    public class PetBodyTypeData
    {
        public Vector3 entityScale = Vector3.one;
        public Vector3 spine1Scale;
        public Vector3 headScale;
    }

    [Header("宠物体型设置")]
    [SerializeField] private PetBodyType currentPetBodyType = PetBodyType.None;
    [SerializeField] private Dictionary<PetBodyType, PetBodyTypeData> petBodyTypeDataList = new Dictionary<PetBodyType, PetBodyTypeData>();

    [Header("宠物骨骼节点")]
    [SerializeField] private Transform spine1;
    [SerializeField] private Transform head;

    // 原始缩放值缓存
    private Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();

    // 当前应用的宠物体型缩放值
    private Dictionary<Transform, Vector3> currentPetBodyTypeScales = new Dictionary<Transform, Vector3>();

    // 动画缩放值缓存（用于处理动画冲突）
    private Dictionary<Transform, Vector3> animationScales = new Dictionary<Transform, Vector3>();

    private PetBodyTypeData currentTypeData;

    private void Awake()
    {
        InitializePetBoneReferences();
        CacheOriginalScales();
        InitializePetBodyTypeData();
    }

    private void Start()
    {
        // ApplyPetBodyType(currentPetBodyType);
    }

    /// <summary>
    /// 初始化宠物骨骼引用
    /// </summary>
    private void InitializePetBoneReferences()
    {
        if (spine1 == null)
            spine1 = FindPetBoneTransform("Pet001 Spine1");
        if (head == null)
            head = FindPetBoneTransform("pet001 Head");
    }

    /// <summary>
    /// 查找宠物骨骼变换
    /// </summary>
    /// <param name="boneName">骨骼名称</param>
    /// <returns>骨骼变换组件</returns>
    private Transform FindPetBoneTransform(string boneName)
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name == boneName)
                return t;
        }
        Debug.LogWarning($"未找到宠物骨骼节点: {boneName}");
        return null;
    }

    /// <summary>
    /// 缓存原始缩放值
    /// </summary>
    private void CacheOriginalScales()
    {
        Transform[] targetBones = { spine1, head };
        foreach (Transform bone in targetBones)
        {
            if (bone != null)
            {
                originalScales[bone] = bone.localScale;
            }
        }
    }

    /// <summary>
    /// 初始化宠物体型数据
    /// </summary>
    private void InitializePetBodyTypeData()
    {
        if (petBodyTypeDataList.Count == 0)
        {
            // 创建默认宠物体型数据
            PetBodyTypeData data1 = new PetBodyTypeData
            {
                spine1Scale = new Vector3(0.83f, 0.83f, 0.83f),
                headScale = new Vector3(0.9f, 0.9f, 0.9f),
            };
            petBodyTypeDataList.Add(PetBodyType.Type1, data1);

            PetBodyTypeData data2 = new PetBodyTypeData
            {
                spine1Scale = new Vector3(1, 1, 1),
                headScale = new Vector3(1.389f, 1.389f, 1.389f),
            };
            petBodyTypeDataList.Add(PetBodyType.Type2, data2);

            PetBodyTypeData data3 = new PetBodyTypeData
            {
                entityScale = new Vector3(0.5f, 0.5f, 0.5f),
                spine1Scale = new Vector3(1f, 1f, 1f),
                headScale = new Vector3(1.389f, 1.389f, 1.389f),
            };
            petBodyTypeDataList.Add(PetBodyType.Type3, data3);
        }
    }

    /// <summary>
    /// 应用指定宠物体型
    /// </summary>
    /// <param name="petBodyType">宠物体型类型</param>
    public void ApplyPetBodyType(PetBodyType petBodyType)
    {
        currentPetBodyType = petBodyType;
        PetBodyTypeData data = GetPetBodyTypeData(petBodyType);
        if (data != null)
        {
            transform.localScale = data.entityScale;
        }
        // currentTypeData = data;
    }

    private void FixedUpdate()
    {
        ApplyPetBodyTypeScales();
    }

    /// <summary>
    /// 获取宠物体型数据
    /// </summary>
    /// <param name="petBodyType">宠物体型类型</param>
    /// <returns>宠物体型数据</returns>
    private PetBodyTypeData GetPetBodyTypeData(PetBodyType petBodyType)
    {
        if (petBodyTypeDataList.ContainsKey(petBodyType))
        {
            return petBodyTypeDataList[petBodyType];
        }
        return null;
    }

    /// <summary>
    /// 应用宠物体型缩放
    /// </summary>
    private void ApplyPetBodyTypeScales()
    {
        if (currentPetBodyType == PetBodyType.None)
            return;
        PetBodyTypeData data = petBodyTypeDataList[currentPetBodyType];
        if (data == null)
            return;
#if UNITY_EDITOR
        transform.localScale = data.entityScale;
#endif
        // 更新宠物体型缩放缓存
        if (spine1 != null)
        {
            currentPetBodyTypeScales[spine1] = data.spine1Scale;
            UpdatePetBoneScale(spine1);
        }
        if (head != null)
        {
            currentPetBodyTypeScales[head] = data.headScale;
            UpdatePetBoneScale(head);
        }
    }

    /// <summary>
    /// 更新宠物骨骼缩放（处理动画冲突）
    /// </summary>
    /// <param name="bone">目标骨骼</param>
    private void UpdatePetBoneScale(Transform bone)
    {
        if (bone == null) return;

        Vector3 originalScale = originalScales.ContainsKey(bone) ? originalScales[bone] : Vector3.one;
        Vector3 bodyTypeScale = currentPetBodyTypeScales.ContainsKey(bone) ? currentPetBodyTypeScales[bone] : Vector3.one;
        Vector3 animationScale = animationScales.ContainsKey(bone) ? animationScales[bone] : Vector3.one;

        // 最终缩放 = 原始缩放 * 宠物体型缩放 * 动画缩放
        bone.localScale = Vector3.Scale(Vector3.Scale(originalScale, bodyTypeScale), animationScale);
        // Debug.Log($"UpdatePetBoneScale bone.scale={bone.localScale},originalScale={originalScale},bodyTypeScale={bodyTypeScale}，animationScale={animationScale}");
    }

    /// <summary>
    /// 设置宠物动画缩放值（由动画系统调用）
    /// </summary>
    /// <param name="boneName">骨骼名称</param>
    /// <param name="scale">缩放值</param>
    public void SetPetAnimationScale(string boneName, Vector3 scale)
    {
        Transform targetBone = GetPetBoneByName(boneName);
        if (targetBone != null)
        {
            animationScales[targetBone] = scale;
            UpdatePetBoneScale(targetBone);
        }
    }

    /// <summary>
    /// 根据名称获取宠物骨骼
    /// </summary>
    /// <param name="boneName">骨骼名称</param>
    /// <returns>骨骼变换</returns>
    private Transform GetPetBoneByName(string boneName)
    {
        switch (boneName)
        {
            case "Pet001 Spine1":
                return spine1;
            case "pet001 Head":
                return head;
            default:
                return null;
        }
    }

    /// <summary>
    /// 重置所有宠物骨骼到原始状态
    /// </summary>
    public void ResetPetToOriginal()
    {
        Transform[] targetBones = { spine1, head };
        foreach (Transform bone in targetBones)
        {
            if (bone != null && originalScales.ContainsKey(bone))
            {
                bone.localScale = originalScales[bone];
            }
        }

        currentPetBodyTypeScales.Clear();
        animationScales.Clear();
    }

    /// <summary>
    /// 获取当前宠物体型
    /// </summary>
    /// <returns>当前宠物体型</returns>
    public PetBodyType GetCurrentPetBodyType()
    {
        return currentPetBodyType;
    }

    /// <summary>
    /// 设置宠物体型数据
    /// </summary>
    /// <param name="petBodyType">宠物体型类型</param>
    /// <param name="data">宠物体型数据</param>
    public void SetPetBodyTypeData(PetBodyType petBodyType, PetBodyTypeData data)
    {
        petBodyTypeDataList[petBodyType] = data;
    }

    /// <summary>
    /// 获取宠物体型数据
    /// </summary>
    /// <param name="petBodyType">宠物体型类型</param>
    /// <returns>宠物体型数据</returns>
    public PetBodyTypeData GetPetBodyTypeDataPublic(PetBodyType petBodyType)
    {
        return GetPetBodyTypeData(petBodyType);
    }

}
