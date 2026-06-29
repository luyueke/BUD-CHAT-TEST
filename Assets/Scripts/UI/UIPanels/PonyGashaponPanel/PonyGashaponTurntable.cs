using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

/// <summary>
/// 转盘控制器
/// 5个奖励按正五边形分布，大奖在正上方第一个点
/// </summary>
public class PonyGashaponTurntable : MonoBehaviour
{
    [Header("转盘设置")]
    [SerializeField] private Transform turntableTransform; // 转盘Transform，用于旋转
    [SerializeField] private float rotationRadius = 200f; // 转盘半径（用于计算奖励位置）
    [SerializeField] private float rotationDuration = 3f; // 旋转持续时间
    [SerializeField] private int minRotationRounds = 3; // 最小旋转圈数
    [SerializeField] private int maxRotationRounds = 5; // 最大旋转圈数

    [Header("奖励位置")]
    [SerializeField] private Transform[] rewardPositions = new Transform[5]; // 5个奖励位置

    // 正五边形的5个角度（每个间隔72度）
    private const float PENTAGON_ANGLE_STEP = 72f; // 360 / 5 = 72度
    private const float PRIZE_ANGLE = 0f; // 大奖在0度位置（Vector3.zero）

    private bool isRotating = false; // 是否正在旋转
    private Tweener currentTween; // 当前的旋转动画
    private float savedTargetAngle; // 保存的目标角度，避免重新计算导致闪烁
    private bool savedHitPrize; // 保存是否命中大奖

    /// <summary>
    /// 转动结束回调
    /// </summary>
    public Action<bool> OnRotationComplete; // 参数：是否命中大奖

    private void Awake()
    {
        // 如果没有指定转盘Transform，使用自身
        if (turntableTransform == null)
        {
            turntableTransform = transform;
        }

        // 初始化奖励位置（如果未在Inspector中设置）
        InitializeRewardPositions();
    }

    /// <summary>
    /// 初始化奖励位置（按正五边形分布）
    /// </summary>
    private void InitializeRewardPositions()
    {
        // 如果已经在Inspector中设置了位置，就不需要自动创建
        if (rewardPositions[0] != null)
        {
            // return;
        }

        // 创建5个奖励位置（按正五边形分布）
        // 第一个位置（i=0）在0度（大奖位置）
        int[] angles = { 90, 18, -54, -126, -198 };
        for (int i = 0; i < 5; i++)
        {
            // 从0度开始，逆时针分布
            // 0: 0度(大奖), 1: 72度, 2: 144度, 3: 216度, 4: 288度
            float angle = angles[i];
            float radian = angle * Mathf.Deg2Rad;

            Vector3 position = new Vector3(
                Mathf.Cos(radian) * rotationRadius,
                Mathf.Sin(radian) * rotationRadius,
                0f
            );

            // 创建位置标记（可选，用于调试）
            // GameObject posMarker = new GameObject($"RewardPosition_{i}");
            // posMarker.transform.SetParent(transform);
            // posMarker.transform.localScale = Vector3.one;
            // posMarker.transform.localPosition = position;
            // rewardPositions[i] = posMarker.transform;
            rewardPositions[i].localPosition = position;
        }
    }

    /// <summary>
    /// 开始旋转转盘
    /// </summary>
    /// <param name="hitPrize">是否命中大奖</param>
    /// <param name="onComplete">旋转完成回调</param>
    public void StartRotation(bool hitPrize, Action<bool> onComplete = null)
    {
        if (isRotating)
        {
            Debug.LogWarning("转盘正在旋转中，无法再次旋转");
            return;
        }

        isRotating = true;

        // 设置回调
        if (onComplete != null)
        {
            OnRotationComplete = onComplete;
        }

        // 停止之前的动画
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        // 计算目标角度并保存
        savedTargetAngle = CalculateTargetAngle(hitPrize);
        savedHitPrize = hitPrize;

        // 获取当前角度（规范化到0-360范围）
        float currentAngle = NormalizeAngle(turntableTransform.localEulerAngles.z);

        // 计算需要旋转的总角度（加上多圈旋转）
        int rotationRounds = UnityEngine.Random.Range(minRotationRounds, maxRotationRounds + 1);

        // 计算从当前角度到目标角度需要旋转的角度（顺时针方向）
        float angleDiff = savedTargetAngle - currentAngle;
        // 如果角度差为负，需要加上360度使其为正（确保顺时针旋转）
        if (angleDiff < 0)
        {
            angleDiff += 360f;
        }
        // 计算相对旋转角度：多圈旋转 + 到目标的角度差
        float relativeRotation = (rotationRounds * 360f) + angleDiff;

        // relativeRotation--;
        // 使用DOLocalRotate进行相对旋转（使用LocalAxisAdd模式确保始终顺时针）
        currentTween = turntableTransform.DOLocalRotate(
            new Vector3(0, 0, relativeRotation),
            rotationDuration,
            RotateMode.LocalAxisAdd // 使用LocalAxisAdd进行相对旋转，不会自动选择最短路径
        )
        .SetEase(Ease.Linear)
        // .SetEase(Ease.OutCubic) // 使用缓出曲线，让旋转逐渐减速
        .OnComplete(() =>
        {
            OnRotationFinished();
        });
    }

    /// <summary>
    /// 计算目标角度
    /// </summary>
    /// <param name="hitPrize">是否命中大奖</param>
    /// <returns>目标角度</returns>
    private float CalculateTargetAngle(bool hitPrize)
    {
        if (hitPrize)
        {
            // 命中大奖：停在0度位置（Vector3.zero）
            return PRIZE_ANGLE;
        }
        else
        {
            // 未中奖：随机停在其他4个位置之一
            // 5个位置：0度(大奖), 72度, 144度, 216度, 288度
            // 其他4个位置：72度, 144度, 216度, 288度
            float[] nonPrizeAngles = { 72f, 144f, 216f, 288f };
            int randomIndex = UnityEngine.Random.Range(0, nonPrizeAngles.Length);
            return NormalizeAngle(nonPrizeAngles[randomIndex]);
        }
    }

    /// <summary>
    /// 规范化角度到0-360范围
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        while (angle < 0) angle += 360f;
        while (angle >= 360f) angle -= 360f;
        return angle;
    }

    void OnRotationFinished(){
        Invoke("waitRotationFinished",0.1f);
    }

    /// <summary>
    /// 旋转完成
    /// </summary>
    private void waitRotationFinished()
    {
        isRotating = false;

        // 获取当前角度（规范化到0-360范围）
        float currentAngle = NormalizeAngle(turntableTransform.localEulerAngles.z);

        // 计算需要调整的角度差（确保停在目标角度）
        float angleDiff = savedTargetAngle - currentAngle;
        // 规范化角度差到-180到180度范围
        while (angleDiff > 180f) angleDiff -= 360f;
        while (angleDiff < -180f) angleDiff += 360f;

        // 如果角度差很小（小于0.1度），说明已经基本到位，直接设置精确值
        // 否则需要微调（可能是浮点数精度问题）
        if (Mathf.Abs(angleDiff) > 0.1f)
        {
            // 微调到精确位置（确保顺时针，避免逆时针调整）
            if (angleDiff < 0)
            {
                angleDiff += 360f; // 确保顺时针微调
            }
            turntableTransform.Rotate(0, 0, angleDiff, Space.Self);
        }

        // 最终确保角度精确（规范化到目标角度）
        turntableTransform.localEulerAngles = new Vector3(0, 0, savedTargetAngle);

        // 触发回调
        OnRotationComplete?.Invoke(savedHitPrize);
    }

    /// <summary>
    /// 停止旋转
    /// </summary>
    public void StopRotation()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
        isRotating = false;
    }

    /// <summary>
    /// 重置转盘到初始位置
    /// </summary>
    public void ResetRotation()
    {
        StopRotation();
        turntableTransform.localEulerAngles = Vector3.zero;
    }

    /// <summary>
    /// 检查是否正在旋转
    /// </summary>
    public bool IsRotating()
    {
        return isRotating;
    }

    private void OnDestroy()
    {
        StopRotation();
    }


    [Button("测试未命中大奖")]
    public void TestNotHitPrize()
    {
        StartRotation(false, (hitPrize) =>
        {
        });
    }

    [Button("测试停止旋转")]
    public void TestStopRotation()
    {
        StopRotation();
    }

    [Button("测试重置旋转")]
    public void TestResetRotation()
    {
        ResetRotation();
    }
    [Button("测试命中大奖")]
    public void TestHitPrize()
    {
        StartRotation(true);
    }
}
