using System;
using System.Collections;
using System.Collections.Generic;
using UIAgent;
using UnityEngine;
using UnityEngine.Events;

public class AvatarRaycast : MonoBehaviour
{
    private float raycastDistance = 5;
    public UnityEvent<Collider[]> OnRaycast = new UnityEvent<Collider[]>();

    private int layMask;

    private int obstacleLayerMask;  // 障碍物层级（墙体等）
    private Camera optCamere;

    // 缓存射线检测结果
    private List<Collider> validTargets = new List<Collider>();

    public void Awake()
    {
        // 检测目标层级
        layMask = 1 << LayerMask.NameToLayer("Model") |
                 1 << LayerMask.NameToLayer("GameSurface") |
                 1 << LayerMask.NameToLayer("Prop");

        // 障碍物层级
        obstacleLayerMask = 1 << LayerMask.NameToLayer("Terrain") |
                           1 << LayerMask.NameToLayer("Obstacle");
    }

    private void Update()
    {
        validTargets.Clear();
        var targets = Physics.OverlapSphere(transform.position, raycastDistance, layMask);

        foreach (var target in targets)
        {
            // 对每个检测到的目标进行射线检测，确认是否被墙体遮挡
            if (!IsTargetBlocked(target))
            {
                validTargets.Add(target);
            }
        }

        // if (validTargets.Count > 0)
        {
            OnRaycast?.Invoke(validTargets.ToArray());
        }
    }

    private bool IsTargetBlocked(Collider target)
    {
        Vector3 directionToTarget = (target.bounds.center - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.bounds.center);

        // 进行射线检测
        if (Physics.Raycast(transform.position, directionToTarget, out RaycastHit hit,
            distanceToTarget, obstacleLayerMask))
        {
            // 如果射线首先击中了障碍物，说明目标被遮挡
            return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raycastDistance);

        // 绘制有效目标
        foreach (var target in validTargets)
        {
            if (target != null)
            {
                // 绘制到目标的连线
                Gizmos.color = Color.green;
                Vector3 targetCenter = target.bounds.center;
                Gizmos.DrawLine(transform.position, targetCenter);

                // 绘制目标边界
                Gizmos.DrawWireCube(targetCenter, target.bounds.size);
            }
        }

        // 绘制障碍物检测
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit,
            raycastDistance, obstacleLayerMask))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hit.point, 0.1f);
            Gizmos.DrawLine(transform.position, hit.point);
        }
    }
}



