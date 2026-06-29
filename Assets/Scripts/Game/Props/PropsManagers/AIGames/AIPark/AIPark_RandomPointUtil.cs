using System.Collections;
using System.Collections.Generic;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Props.PropsManagers
{
    public class AIPark_RandomPointUtil : GlobalInstance<AIPark_RandomPointUtil>
    {
        /// <summary>
        /// 在指定范围内采样可行走点
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="forward">朝向</param>
        /// <param name="distance">半径</param>
        /// <param name="maxAttempts">最大尝试次数</param>
        /// <returns>可行走点，如果没有找到则返回中心点</returns>
        public Vector3 RandomPoint(Vector3 center, Vector3 forward, float distance, int maxAttempts = 30)
        {
            forward = Vector3.Normalize(forward);
            // 计算扇形区域的角度范围（180度）
            float angleRange = 180f;
            float halfAngle = angleRange * 0.5f;

            for (int i = 0; i < maxAttempts; i++)
            {
                // 随机角度（在扇形范围内）
                float randomAngle = Random.Range(-halfAngle, halfAngle);
                
                // 随机距离（在指定半径内）
                float randomDistance = Random.Range(1f, distance);
                
                // 计算随机方向
                Vector3 randomDirection = Quaternion.Euler(0, randomAngle, 0) * forward;
                
                // 计算目标点
                Vector3 targetPoint = center + randomDirection * randomDistance;

                // 在NavMesh上找最近的点
                if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, distance, NavMesh.AllAreas))
                {
                    // 检查该点是否可达
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(center, hit.position, NavMesh.AllAreas, path))
                    {
                        // 检查路径是否有效
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            return hit.position;
                        }
                    }
                }
            }

            // 如果找不到合适的点，返回中心点
            return center;
        }

        /// <summary>
        /// 在指定范围内采样多个可行走点
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="forward">朝向</param>
        /// <param name="distance">半径</param>
        /// <param name="count">需要的点数</param>
        /// <param name="minDistance">点之间的最小距离</param>
        /// <returns>可行走点列表</returns>
        public List<Vector3> RandomPoints(Vector3 center, Vector3 forward, float distance, int count, float minDistance = 1f)
        {
            List<Vector3> points = new List<Vector3>();
            int maxAttempts = count * 3; // 增加尝试次数以确保能找到足够的点
            int attempts = 0;

            while (points.Count < count && attempts < maxAttempts)
            {
                Vector3 point = RandomPoint(center, forward, distance);
                
                // 检查与已有点的距离
                bool isValid = true;
                foreach (var existingPoint in points)
                {
                    if (Vector3.Distance(point, existingPoint) < minDistance)
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                {
                    points.Add(point);
                }

                attempts++;
            }

            return points;
        }

        /// <summary>
        /// 检查点是否在可行走区域
        /// </summary>
        private bool IsPointWalkable(Vector3 point, float checkRadius = 0.1f)
        {
            return NavMesh.SamplePosition(point, out NavMeshHit hit, checkRadius, NavMesh.AllAreas);
        }

        /// <summary>
        /// 检查两点之间是否可达
        /// </summary>
        private bool IsPathValid(Vector3 start, Vector3 end)
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
            {
                return path.status == NavMeshPathStatus.PathComplete;
            }
            return false;
        }
    }
}