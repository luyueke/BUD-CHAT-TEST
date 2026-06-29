using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Props.PropsManagers
{
    public class AIPark_CharacterUtils : GlobalInstance<AIPark_CharacterUtils>
    {
        public bool CanContinueOsDialog = false;

        public Dictionary<GuidePosType, ParkNpcTransData> GuidePosData = new Dictionary<GuidePosType, ParkNpcTransData>()
        {
            [GuidePosType.TiliaSpawn_Park] = new ParkNpcTransData()
            {
                pos = new Vector3(30.9f,0,2.23f),
                rot = new Vector3(0, -64f, 0),
            },
            [GuidePosType.TiliaTalk_Park] = new ParkNpcTransData()
            {
                pos = new Vector3(30.9f,0,2.23f),
                rot = new Vector3(0, -64f, 0),
            },
            [GuidePosType.PlayerSpawn_Park] = new ParkNpcTransData()
            {
                pos = new Vector3(26.876f,0,3.74f),
                rot = new Vector3(0, 113f, 0),
            },



            [GuidePosType.TiliaSpawn_First_Park] = new ParkNpcTransData()
            {
                pos = new Vector3(7.57f,0,10.43f),
                rot = new Vector3(0, 0, 0),
            },
            [GuidePosType.TiliaTalk_First_Park] = new ParkNpcTransData()
            {
                pos = new Vector3(5.189f,0,15.586f),
                rot = new Vector3(0, 0, 0),
            },
            [GuidePosType.PlayerSpawn_First_Park] = new ParkNpcTransData()
            {
                pos = new Vector3(4.689f,0,16.91f),
                rot = new Vector3(0, 180, 0),
            },

            [GuidePosType.TiliaSpawn_Fountain] = new ParkNpcTransData()
            {
                pos = new Vector3(-4, 0, -1.6f),
                rot = new Vector3(0, 0, 0),
            },
            [GuidePosType.TiliaTalk_Fountain] = new ParkNpcTransData()
            {
                pos = new Vector3(-3.7f, 0, 3.9f),
                rot = new Vector3(0, 0, 0),
            },
            [GuidePosType.PlayerSpawn_Fountain] = new ParkNpcTransData()
            {
                pos = new Vector3(-4.3f, 0, 4.4f),
                rot = new Vector3(0, 171, 0),
            },

            [GuidePosType.TiliaSpawn_Stage] = new ParkNpcTransData()
            {
                pos = new Vector3(3.5f,0,-7.25f),
                rot = new Vector3(0, -55, 0),
            },
            [GuidePosType.TiliaTalk_Stage] = new ParkNpcTransData()
            {
                pos = new Vector3(4, 0, -6.2f),
                rot = new Vector3(0, -90, 0),
            },
            [GuidePosType.PlayerSpawn_Stage] = new ParkNpcTransData()
            {
                pos = new Vector3(1, 0, -5f),
                rot = new Vector3(0, 115, 0),

            },

            [GuidePosType.Guide_FirstInGame_Npc_1] = new ParkNpcTransData()
            {
                pos = new Vector3(17.2f, 0, 1.26f),
                rot = new Vector3(0, -90, 0),
            },
            [GuidePosType.Guide_FirstInGame_Self] = new ParkNpcTransData()
            {
                pos = new Vector3(17.4f, 0, 10.26f),
                rot = new Vector3(0, 90, 0),
            },
        };

        public Dictionary<LocationType, ParkNpcTransData> OfficalLocationGameSpawnPoint =
            new Dictionary<LocationType, ParkNpcTransData>()
            {
                [LocationType.TrojanHorse] = new ParkNpcTransData()
                {
                    pos = new Vector3(-9.17f, 0, -8.53f),
                    rot = new Vector3(0, 75, 0),
                },
                [LocationType.SlideSlides] = new ParkNpcTransData()
                {
                    pos = new Vector3(25, 0, -5.2f),
                    rot = new Vector3(0, -85, 0),
                },
                [LocationType.SeeSaw] = new ParkNpcTransData()
                {
                    pos = new Vector3(25, 0, 10),
                    rot = new Vector3(0, 252, 0),
                },
                [LocationType.Swinging] = new ParkNpcTransData()
                {
                    pos = new Vector3(24, 0, 3.6f),
                    rot = new Vector3(0, 110, 0),
                },
                [LocationType.Fountain] = new ParkNpcTransData()
                {
                    pos = new Vector3(15.6f, 0, 0.5f),
                    rot = new Vector3(0, -70, 0),
                },
                [LocationType.Stage] = new ParkNpcTransData()
                {
                    pos = new Vector3(-9.5f, 0, -6.5f),
                    rot = new Vector3(0, 139, 0),
                },
                [LocationType.Park] = new ParkNpcTransData()
                {
                    pos = new Vector3(20.15f, 0, 3.74f),
                    rot = new Vector3(0, 0, 0),
                }

            };
        //官方游戏出生点
        public Dictionary<ParkNpcRoleType, ParkNpcTransData> OfficalGameSpawnPoint =
            new Dictionary<ParkNpcRoleType, ParkNpcTransData>()
            {
                [ParkNpcRoleType.Tilia] = new ParkNpcTransData()
                {
                    pos = new Vector3(-3.82f, 0, 4.72f),
                    rot = new Vector3(0, -65, 0),
                },
                [ParkNpcRoleType.Elise] = new ParkNpcTransData()
                {
                    pos = new Vector3(-3.94f, 0, 7.72f),
                    rot = new Vector3(0, -127, 0),
                },
                [ParkNpcRoleType.Casper] = new ParkNpcTransData()
                {
                    pos = new Vector3(-4.9f, 0, 3.87f),
                    rot = new Vector3(0, -90, 0),
                },

                [ParkNpcRoleType.Pio] = new ParkNpcTransData()
                {
                    pos = new Vector3(-4.15f, 0, 2.13f),
                    rot = new Vector3(0, -90, 0),
                },
                [ParkNpcRoleType.Teddy] = new ParkNpcTransData()
                {
                    pos = new Vector3(-4.71f, 0, 8.77f),
                    rot = new Vector3(0, -127, 0),
                },
                [ParkNpcRoleType.Vivien] = new ParkNpcTransData()
                {
                    pos = new Vector3(-1.84f, 0, 9.6f),
                    rot = new Vector3(0, -107, 0),
                },
                [ParkNpcRoleType.Rowland] = new ParkNpcTransData()
                {
                    pos = new Vector3(-4f, 0, 10.7f),
                    rot = new Vector3(0, -107, 0),
                },

            };

        //官方游戏出生点
        public List<ParkNpcTransData> UGCGameSpawnPoint =
            new List<ParkNpcTransData>()
            {
                new ParkNpcTransData()
                {
                    pos = new Vector3(-3.82f,0,4.72f),
                    rot =  new Vector3(0,-65,0),
                },
                new ParkNpcTransData()
                {
                     pos = new Vector3(-3.94f,0,7.72f),
                    rot =  new Vector3(0,-127,0),
                },
                new ParkNpcTransData()
                {
              pos = new Vector3(-4.9f,0,3.87f),
                    rot =  new Vector3(0,-90,0),
                },
                new ParkNpcTransData()
                {
                    pos = new Vector3(-4.15f,0,2.13f),
                    rot = new Vector3(0, -90, 0),
                },
                new ParkNpcTransData()
                {
                    pos = new Vector3(-4.71f,0,8.77f),
                    rot = new Vector3(0, -127, 0),
                },
                new ParkNpcTransData()
                {
                    pos = new Vector3(-1.84f,0,9.6f),
                    rot = new Vector3(0, -107, 0),
                },
                new ParkNpcTransData()
                {
                    pos = new Vector3(-4f,0,10.7f),
                    rot = new Vector3(0, -107, 0),
                },

            };

        public Dictionary<LocationType, ParkNpcTransData> SpawnPointByLocationTypeDict =
            new Dictionary<LocationType, ParkNpcTransData>()
            {
                [LocationType.TrojanHorse] = new ParkNpcTransData()
                {
                    pos = new Vector3(-12.78f, 0, 8.6f),
                    rot = new Vector3(0, -180, 0),
                },
                [LocationType.SlideSlides] = new ParkNpcTransData()
                {
                    pos = new Vector3(19.78f, 0, -6.3f),
                    rot = new Vector3(0, -357, 0),
                },
                [LocationType.SeeSaw] = new ParkNpcTransData()
                {
                    pos = new Vector3(22.5f, 0, 5.15f),
                    rot = new Vector3(0, -80, 0),
                },
                [LocationType.Swinging] = new ParkNpcTransData()
                {
                    pos = new Vector3(25.7f, 0, 0.35f),
                    rot = new Vector3(0, 55.6f, 0),
                },
                [LocationType.Fountain] = new ParkNpcTransData()
                {
                    pos = new Vector3(10.1f, 0, 2.35f),
                    rot = new Vector3(0, -75, 0),
                },
                [LocationType.Stage] = new ParkNpcTransData()
                {
                    pos = new Vector3(8.4f, 0, -4.6f),
                    rot = new Vector3(0, -161, 0),
                },
                [LocationType.Park] = new ParkNpcTransData()
                {
                    pos = new Vector3(-21, 0, 2.9f),
                    rot = new Vector3(0, 90, 0),
                }
            };

        private Dictionary<string, ParkNpcTransData> ActionLocationDict = new Dictionary<string, ParkNpcTransData>()
        {
            #region ConsultingRoom 诊室
            //在 诊室 进行 弄设备 时的位置
            [LocationType.ConsultingRoom + ActionType.OperatingEquipment.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-15.247f, 1.41f, -28.9f),
                rot = new Vector3(0, -90, 0),
            },
            //在 诊室 进行 给病人打针 时的位置
            [LocationType.ConsultingRoom + ActionType.InjectionsToPatients.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-17.75f, 1.41f, -24.46f),
                rot = new Vector3(0, 180, 0),
            },
            //在 诊室 进行 坐下 时的位置
            [LocationType.ConsultingRoom + ActionType.Sitting.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-14.86f, 1.41f, -24.69f),
                rot = new Vector3(0, 180, 0),
            },
            //在 诊室 进行 躺下 时的位置
            [LocationType.ConsultingRoom + ActionType.LyingDown.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-11.39f, 1.41f, -19.4f),
                rot = new Vector3(0, 180, 0),
            },
            //在 诊室 进行 交谈 时的位置
            [LocationType.ConsultingRoom + ActionType.Talk.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-11.39f, 1.41f, -19.4f),
                rot = new Vector3(0, 180, 0),
            },
            #endregion

            #region DirectorsOffice 院长室
            //在 院长室 整理文件
            [LocationType.DirectorsOffice + ActionType.OrganizingDocuments.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-13.0f, 1.41f, -20.73f),
                rot = new Vector3(0, 180, 0),
            },
            //在 院长室 办公
            [LocationType.DirectorsOffice + ActionType.Working.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-15.736f, 1.41f, -20.42f),
                rot = new Vector3(0, -90, 0),
            },
            //在 院长室 交谈
            [LocationType.DirectorsOffice + ActionType.Talk.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-11.39f, 1.41f, -19.4f),
                rot = new Vector3(0, 180, 0),
            },
            #endregion

            #region WaitingRoom 候诊室
            // 在 候诊室 坐下
            [LocationType.WaitingRoom + ActionType.Sitting.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-10.05f, 1.41f, -8.60f),
                rot = new Vector3(0, 90, 0),
            },
            [LocationType.WaitingRoom + ActionType.Talk.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-11.53f, 1.5f, 12),
                rot = new Vector3(0, 180, 0),
            },
            #endregion

            #region Toilet 厕所
            //在 厕所 进行 上厕所 时的位置
            [LocationType.Toilet + ActionType.UsingRestroom.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-16.332f, 1.41f, 15.90f),
                rot = new Vector3(0, 180, 0),
            },
            //在 厕所 进行 清洁 时的位置
            [LocationType.Toilet + ActionType.Cleaning.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-18.08f, 1.41f, 18.25f),
                rot = new Vector3(0, 90, 0),
            },
            #endregion

            #region Ward 病房
            //在 病房 整理文件
            [LocationType.Ward + ActionType.OrganizingDocuments.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-9.2f, 1.5f, -13.8f),
                rot = new Vector3(0, 180, 0),
            },
            //在 病房 进行 吃药 时的位置
            [LocationType.Ward + ActionType.TakingMedication.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(2.41f, 1.41f, -23.236f),
                rot = new Vector3(0, 150, 0),
            },
            //在 病房 进行 查房 时的位置
            [LocationType.Ward + ActionType.ConductingRounds.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(0.241f, 1.41f, -23.32f),
                rot = new Vector3(0, 90, 0),
            },
            //在 病房 进行 躺下 时的位置
            [LocationType.Ward + ActionType.LyingDown.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(2.705f, 1.41f, -19.30f),
                rot = new Vector3(0, 0, 0),
            },
            #endregion

            #region Corridor 走廊
            //在 走廊 进行 清洁
            [LocationType.Corridor + ActionType.Cleaning.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-4.78f, 1.41f, -16.42f),
                rot = new Vector3(0, -90, 0),
            },
            //在 走廊 进行 交谈
            [LocationType.Corridor + ActionType.Talk.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-11.53f, 1.5f, 12),
                rot = new Vector3(0, 180, 0),
            },
            #endregion

            #region Pharmacy 药房
            //在 药房 进行 办公
            [LocationType.Pharmacy + ActionType.Working.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-15.04f, 1.41f, 0.284f),
                rot = new Vector3(0, -90, 0),
            },
            //在 药房 进行 配药
            [LocationType.Pharmacy + ActionType.DispensingMedication.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-15.04f, 1.41f, 0.284f),
                rot = new Vector3(0, -90, 0),
            },
            //在 药房 进行 交谈
            [LocationType.Pharmacy + ActionType.Talk.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-10.5f, 1.5f, 0.1f),
                rot = new Vector3(0, -90, 0),
            },
            #endregion

            #region OutDoor 大门
            //在 大门 进行 交谈
            [LocationType.OutDoor + ActionType.Talk.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-2.884f, 1.41f, -2.323f),
                rot = new Vector3(0, -45, 0),
            },
            //在 大门 进行 站立
            [LocationType.OutDoor + ActionType.Stand.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-2.884f, 1.41f, -2.323f),
                rot = new Vector3(0, -45, 0),
            },
            //在 大门 进行 睡觉
            [LocationType.OutDoor + ActionType.Sleep.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-2.884f, 1.41f, -2.323f),
                rot = new Vector3(0, -45, 0),
            },
            #endregion

            [LocationType.None.ToString()] = new ParkNpcTransData()
            {
                pos = new Vector3(-4.324f, 1.41f, -7.17f),
                rot = new Vector3(0, 180, 0),
            },
        };

        public ParkNpcTransData GetSpawnPointByRoleType(ParkNpcRoleType roleType)
        {
            if (OfficalGameSpawnPoint.TryGetValue(roleType, out ParkNpcTransData transData))
            {
                return transData;
            }
            else
            {
                LoggerUtils.LogError("没有找到对应人物的出生位置");
            }

            return OfficalGameSpawnPoint[ParkNpcRoleType.Elise];
        }

        public ParkNpcTransData GetSelfSpawnPointByLocationType(LocationType locationType)
        {
            if (OfficalLocationGameSpawnPoint.TryGetValue(locationType, out ParkNpcTransData transData))
            {
                return transData;
            }
            else
            {
                LoggerUtils.LogError("没有找到对应操作目标的位置");
            }
            return OfficalLocationGameSpawnPoint[LocationType.None];
        }

        public ParkNpcTransData GetGuidePosByGuidePosType(GuidePosType guidePosType)
        {
            if (GuidePosData.TryGetValue(guidePosType, out ParkNpcTransData transData))
            {
                return transData;
            }
            else
            {
                LoggerUtils.LogError("没有找到对应引导位置" + guidePosType);
            }
            return null;
        }

        public ParkNpcTransData GetGuidePosByGuidePosType(bool isSelf, LocationType locationType)
        {
            var guidePosType = GuidePosType.PlayerSpawn_Park;
            if (locationType == LocationType.Park)
            {
                guidePosType = isSelf ? GuidePosType.PlayerSpawn_Park : GuidePosType.TiliaSpawn_Park;
            }
            else if (locationType == LocationType.TrojanHorse)
            {
                guidePosType = isSelf ? GuidePosType.PlayerSpawn_Fountain : GuidePosType.TiliaSpawn_Fountain;
            }
            else
            {
                guidePosType = isSelf ? GuidePosType.PlayerSpawn_Stage : GuidePosType.TiliaSpawn_Stage;
            }
            if (GuidePosData.TryGetValue(guidePosType, out ParkNpcTransData transData))
            {
                return transData;
            }
            else
            {
                LoggerUtils.LogError("没有找到对应引导位置" + guidePosType);
            }
            return null;
        }
        public List<ParkNpcTransData> GetAllSpawnPointByLocationType(LocationType locationType, int count, float minDistance = 1f)
        {
            var selfSpawnPoint = GetSelfSpawnPointByLocationType(locationType);
            var forward = Quaternion.Euler(selfSpawnPoint.rot) * Vector3.forward;
            var points = AIPark_RandomPointUtil.Inst.RandomPoints(selfSpawnPoint.pos, forward, 5, count, minDistance);
            List<ParkNpcTransData> spawnPoints = new List<ParkNpcTransData>();
            for (int i = 0; i < count; i++)
            {
                var spawnPoint = points[i];
                var direction = (selfSpawnPoint.pos - spawnPoint).normalized;
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                var euler = new Vector3(0, angle, 0);
                spawnPoints.Add(new ParkNpcTransData()
                {
                    pos = spawnPoint,
                    rot = euler,
                });
            }
            return spawnPoints;
        }

        public List<ParkNpcTransData> GetAllSpawnPointByLocationType2(LocationType locationType, int count, float radius, float minDistance = 1f)
        {
            // 获取指定位置类型的中心点
            var centerSpawnPoint = GetSelfSpawnPointByLocationType(locationType);
            Vector3 centerPos = centerSpawnPoint.pos;
            centerPos.y = 0; // 确保Y轴为0

            List<ParkNpcTransData> spawnPoints = new List<ParkNpcTransData>();

            // 参数验证
            if (count <= 0 || radius <= 0)
            {
                LoggerUtils.LogError($"GetAllSpawnPointByLocationType2: 参数无效 count={count}, radius={radius}");
                return spawnPoints;
            }

            // 计算圆上的角度间隔，确保点均匀分布
            float angleStep = 360f / count;
            int maxAttemptsPerPoint = 10; // 每个点的最大尝试次数

            for (int i = 0; i < count; i++)
            {
                float targetAngle = i * angleStep;
                ParkNpcTransData validPoint = GetValidPointOnCircle(centerPos, radius, targetAngle, maxAttemptsPerPoint, minDistance, spawnPoints);

                if (validPoint != null)
                {
                    spawnPoints.Add(validPoint);
                }
                else
                {
                    LoggerUtils.Log($"GetAllSpawnPointByLocationType2: 无法在角度 {targetAngle}° 找到有效点");
                }
            }

            LoggerUtils.Log($"GetAllSpawnPointByLocationType2: 在位置 {centerPos} 半径 {radius} 的圆上找到 {spawnPoints.Count}/{count} 个有效点");
            return spawnPoints;
        }

        /// <summary>
        /// 在指定角度的圆上获取有效行走点
        /// </summary>
        /// <param name="center">圆心位置</param>
        /// <param name="radius">半径</param>
        /// <param name="targetAngle">目标角度（度）</param>
        /// <param name="maxAttempts">最大尝试次数</param>
        /// <param name="minDistance">与已有点的最小距离</param>
        /// <param name="existingPoints">已存在的点列表</param>
        /// <returns>有效的行走点数据，如果没找到返回null</returns>
        private ParkNpcTransData GetValidPointOnCircle(Vector3 center, float radius, float targetAngle, int maxAttempts, float minDistance, List<ParkNpcTransData> existingPoints)
        {
            NavMeshHit hit;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 在目标角度附近添加一些随机偏移，增加找到有效点的概率
                float randomOffset = Random.Range(-15f, 15f); // ±15度的随机偏移
                float currentAngle = targetAngle + randomOffset;

                // 计算圆上的点
                float angleRad = currentAngle * Mathf.Deg2Rad;
                Vector3 circlePoint = new Vector3(
                    center.x + radius * Mathf.Cos(angleRad),
                    center.y,
                    center.z + radius * Mathf.Sin(angleRad)
                );

                // 尝试在NavMesh上找到有效位置
                if (NavMesh.SamplePosition(circlePoint, out hit, radius * 0.5f, NavMesh.AllAreas))
                {
                    // 检查找到的点是否在合理的圆形范围内
                    float distanceFromCenter = Vector3.Distance(center, hit.position);
                    float tolerance = radius * 0.3f; // 允许30%的半径误差

                    if (Mathf.Abs(distanceFromCenter - radius) <= tolerance)
                    {
                        // 检查是否与已有点距离太近
                        bool tooClose = false;
                        foreach (var existingPoint in existingPoints)
                        {
                            if (Vector3.Distance(hit.position, existingPoint.pos) < minDistance)
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (!tooClose)
                        {
                            // 计算朝向圆心的旋转角度
                            Vector3 direction = (center - hit.position).normalized;
                            float rotationAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

                            return new ParkNpcTransData()
                            {
                                pos = hit.position,
                                rot = new Vector3(0, rotationAngle, 0)
                            };
                        }
                    }
                }
            }

            return null;
        }

        public ParkNpcTransData GetNearTransBetweenTwoPoints(Vector3 pos1, Vector3 pos2, float minRadius, float maxRadius)
        {
            // 参数验证
            if (minRadius < 0 || maxRadius <= 0 || minRadius >= maxRadius)
            {
                LoggerUtils.LogError($"GetNearTransBetweenTwoPoints: 参数无效 minRadius={minRadius}, maxRadius={maxRadius}");
                return null;
            }

            // 将Y坐标设为0，只考虑XZ平面
            pos1.y = 0;
            pos2.y = 0;

            // 计算两点之间的距离
            float distanceBetweenPoints = Vector3.Distance(pos1, pos2);

            // 如果两点距离太近，直接返回pos1附近的点
            if (distanceBetweenPoints < 0.1f)
            {
                return GetNearTransData(pos1, minRadius, maxRadius);
            }

            // 计算两点之间的中点
            Vector3 midPoint = (pos1 + pos2) * 0.5f;

            // 计算从pos1到pos2的方向向量
            Vector3 direction = (pos2 - pos1).normalized;

            // 计算垂直于两点连线的方向（用于确定搜索区域）
            Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x);

            NavMeshHit hit;
            int maxAttempts = 50;
            ParkNpcTransData bestPoint = null;
            float bestTotalDistance = float.MaxValue;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 在两点之间的区域内生成随机点
                Vector3 samplePos = GetRandomPointBetweenTwoPoints(pos1, pos2, minRadius, maxRadius, attempt);

                // 尝试在NavMesh上找到有效位置
                if (NavMesh.SamplePosition(samplePos, out hit, maxRadius, NavMesh.AllAreas))
                {
                    // 检查找到的点是否在指定的环形区域内（相对于pos1）
                    float distanceFromPos1 = Vector3.Distance(pos1, hit.position);
                    if (distanceFromPos1 >= minRadius && distanceFromPos1 <= maxRadius)
                    {
                        // 计算总距离：从pos1到该点 + 从该点到pos2
                        float totalDistance = distanceFromPos1 + Vector3.Distance(hit.position, pos2);

                        // 如果这个点的总距离更小，则更新最佳点
                        if (totalDistance < bestTotalDistance)
                        {
                            bestTotalDistance = totalDistance;

                            // 计算朝向pos2的旋转角度
                            Vector3 directionToPos2 = (pos2 - hit.position).normalized;
                            float rotationAngle = Mathf.Atan2(directionToPos2.x, directionToPos2.z) * Mathf.Rad2Deg;

                            bestPoint = new ParkNpcTransData()
                            {
                                pos = hit.position,
                                rot = new Vector3(0, rotationAngle, 0)
                            };
                        }
                    }
                }
            }

            // 如果没找到，尝试在稍微扩大的范围内搜索
            if (bestPoint == null)
            {
                float expandedMaxRadius = maxRadius * 1.2f;
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    Vector3 samplePos = GetRandomPointBetweenTwoPoints(pos1, pos2, minRadius, expandedMaxRadius, attempt);

                    if (NavMesh.SamplePosition(samplePos, out hit, expandedMaxRadius, NavMesh.AllAreas))
                    {
                        float distanceFromPos1 = Vector3.Distance(pos1, hit.position);
                        if (distanceFromPos1 >= minRadius && distanceFromPos1 <= expandedMaxRadius)
                        {
                            float totalDistance = distanceFromPos1 + Vector3.Distance(hit.position, pos2);

                            if (totalDistance < bestTotalDistance)
                            {
                                bestTotalDistance = totalDistance;

                                Vector3 directionToPos2 = (pos2 - hit.position).normalized;
                                float rotationAngle = Mathf.Atan2(directionToPos2.x, directionToPos2.z) * Mathf.Rad2Deg;

                                bestPoint = new ParkNpcTransData()
                                {
                                    pos = hit.position,
                                    rot = new Vector3(0, rotationAngle, 0)
                                };
                            }
                        }
                    }
                }
            }

            if (bestPoint != null)
            {
                LoggerUtils.Log($"GetNearTransBetweenTwoPoints: 找到最佳点，总距离: {bestTotalDistance:F2}, 位置: {bestPoint.pos}");
            }
            else
            {
                LoggerUtils.Log($"GetNearTransBetweenTwoPoints: 在两点之间环形区域 [{minRadius}, {maxRadius}] 内未找到有效的行走点");
            }

            return bestPoint;
        }

        /// <summary>
        /// 在两点之间的区域内生成随机点
        /// </summary>
        /// <param name="pos1">第一个点</param>
        /// <param name="pos2">第二个点</param>
        /// <param name="minRadius">最小半径（相对于pos1）</param>
        /// <param name="maxRadius">最大半径（相对于pos1）</param>
        /// <param name="attempt">尝试次数（用于增加随机性）</param>
        /// <returns>两点之间区域内的随机点</returns>
        private Vector3 GetRandomPointBetweenTwoPoints(Vector3 pos1, Vector3 pos2, float minRadius, float maxRadius, int attempt)
        {
            // 计算两点之间的中点
            Vector3 midPoint = (pos1 + pos2) * 0.5f;

            // 计算两点之间的距离
            float distanceBetweenPoints = Vector3.Distance(pos1, pos2);

            // 计算从pos1到pos2的方向向量
            Vector3 direction = (pos2 - pos1).normalized;

            // 计算垂直于两点连线的方向
            Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x);

            // 在两点之间的区域内生成随机点
            // 使用不同的随机策略来增加找到好点的概率
            Vector3 randomPoint;

            if (attempt % 3 == 0)
            {
                // 策略1：在两点连线上随机选择位置，然后向垂直方向偏移
                float t = Random.Range(0.2f, 0.8f); // 避免太靠近端点
                Vector3 linePoint = Vector3.Lerp(pos1, pos2, t);
                float perpendicularOffset = Random.Range(-distanceBetweenPoints * 0.3f, distanceBetweenPoints * 0.3f);
                randomPoint = linePoint + perpendicular * perpendicularOffset;
            }
            else if (attempt % 3 == 1)
            {
                // 策略2：以pos1为中心，在环形区域内随机生成
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float randomRadius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));
                randomPoint = new Vector3(
                    pos1.x + randomRadius * Mathf.Cos(randomAngle),
                    pos1.y,
                    pos1.z + randomRadius * Mathf.Sin(randomAngle)
                );
            }
            else
            {
                // 策略3：在中点附近随机生成
                float randomRadius = Random.Range(0f, distanceBetweenPoints * 0.4f);
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                randomPoint = new Vector3(
                    midPoint.x + randomRadius * Mathf.Cos(randomAngle),
                    midPoint.y,
                    midPoint.z + randomRadius * Mathf.Sin(randomAngle)
                );
            }

            return randomPoint;
        }

        public ParkNpcTransData GetNearTransDataAtSameSide(Vector3 curPos, Vector3 centerPos, float minRadius, float maxRadius)
        {
            // 参数验证
            if (minRadius < 0 || maxRadius <= 0 || minRadius >= maxRadius)
            {
                LoggerUtils.LogError($"GetNearTransDataAtSameSide: 参数无效 minRadius={minRadius}, maxRadius={maxRadius}");
                return null;
            }

            // 将Y坐标设为0，只考虑XZ平面
            curPos.y = 0;
            centerPos.y = 0;

            // 计算curPos相对于centerPos的方向向量
            Vector3 directionToCurPos = (curPos - centerPos).normalized;

            // 计算curPos在centerPos的哪一侧（通过X坐标判断）
            bool curPosIsOnRightSide = directionToCurPos.x > 0;

            // 定义半环形的角度范围（90度范围，确保在同一边）
            float angleRange = 90f; // 半环形角度范围
            float baseAngle = curPosIsOnRightSide ? 0f : 180f; // 基础角度
            float startAngle = baseAngle - angleRange * 0.5f;
            float endAngle = baseAngle + angleRange * 0.5f;

            NavMeshHit hit;
            int maxAttempts = 50; // 最大尝试次数

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 在半环形区域内随机生成一个点
                Vector3 samplePos = GetRandomPointInSemiRing(centerPos, minRadius, maxRadius, startAngle, endAngle);

                // 尝试在NavMesh上找到有效位置
                if (NavMesh.SamplePosition(samplePos, out hit, maxRadius, NavMesh.AllAreas))
                {
                    // 检查找到的点是否在指定的半环形区域内
                    float distance = Vector3.Distance(centerPos, hit.position);
                    if (distance >= minRadius && distance <= maxRadius)
                    {
                        // 验证点是否真的在同一边
                        Vector3 directionToHitPos = (hit.position - centerPos).normalized;
                        bool hitPosIsOnRightSide = directionToHitPos.x > 0;

                        if (hitPosIsOnRightSide == curPosIsOnRightSide)
                        {
                            return new ParkNpcTransData()
                            {
                                pos = hit.position,
                                rot = Vector3.zero
                            };
                        }
                    }
                }
            }

            // 如果没找到，尝试在稍微扩大的范围内搜索
            float expandedMaxRadius = maxRadius * 1.2f;
            for (int attempt = 0; attempt < 15; attempt++)
            {
                Vector3 samplePos = GetRandomPointInSemiRing(centerPos, minRadius, expandedMaxRadius, startAngle, endAngle);

                if (NavMesh.SamplePosition(samplePos, out hit, expandedMaxRadius, NavMesh.AllAreas))
                {
                    float distance = Vector3.Distance(centerPos, hit.position);
                    if (distance >= minRadius && distance <= expandedMaxRadius)
                    {
                        // 验证点是否真的在同一边
                        Vector3 directionToHitPos = (hit.position - centerPos).normalized;
                        bool hitPosIsOnRightSide = directionToHitPos.x > 0;

                        if (hitPosIsOnRightSide == curPosIsOnRightSide)
                        {
                            LoggerUtils.Log($"GetNearTransDataAtSameSide: 在扩大范围内找到点，距离: {distance:F2}");
                            return new ParkNpcTransData()
                            {
                                pos = hit.position,
                                rot = Vector3.zero
                            };
                        }
                    }
                }
            }

            LoggerUtils.Log($"GetNearTransDataAtSameSide: 在位置 {centerPos} 半环形区域 [{minRadius}, {maxRadius}] 内未找到有效的行走点");
            return null;
        }

        /// <summary>
        /// 在半环形区域内生成随机点
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="minRadius">最小半径</param>
        /// <param name="maxRadius">最大半径</param>
        /// <param name="startAngle">起始角度（度）</param>
        /// <param name="endAngle">结束角度（度）</param>
        /// <returns>半环形区域内的随机点</returns>
        private Vector3 GetRandomPointInSemiRing(Vector3 center, float minRadius, float maxRadius, float startAngle, float endAngle)
        {
            // 生成随机角度 (在指定范围内)
            float randomAngle = Random.Range(startAngle, endAngle) * Mathf.Deg2Rad;

            // 生成随机半径 (在minRadius和maxRadius之间)
            // 使用平方根分布确保均匀分布
            float randomRadius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));

            // 计算随机点的坐标
            float x = center.x + randomRadius * Mathf.Cos(randomAngle);
            float z = center.z + randomRadius * Mathf.Sin(randomAngle);

            return new Vector3(x, center.y, z);
        }

        /// <summary>
        /// 获取指定位置环形区域内的有效行走点
        /// </summary>
        /// <param name="pos">中心位置</param>
        /// <param name="minRadius">最小半径</param>
        /// <param name="maxRadius">最大半径</param>
        /// <returns>有效的行走点数据，如果没找到返回null</returns>
        public ParkNpcTransData GetNearTransData(Vector3 pos, float minRadius, float maxRadius)
        {
            // 参数验证
            if (minRadius < 0 || maxRadius <= 0 || minRadius >= maxRadius)
            {
                LoggerUtils.LogError($"GetNearTransData: 参数无效 minRadius={minRadius}, maxRadius={maxRadius}");
                return null;
            }
            pos.y = 0;

            NavMeshHit hit;
            int maxAttempts = 30; // 最大尝试次数

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 在环形区域内随机生成一个点
                Vector3 samplePos = GetRandomPointInRing(pos, minRadius, maxRadius);

                // 尝试在NavMesh上找到有效位置
                if (NavMesh.SamplePosition(samplePos, out hit, maxRadius, NavMesh.AllAreas))
                {
                    // 检查找到的点是否在指定的环形区域内
                    float distance = Vector3.Distance(pos, hit.position);
                    if (distance >= minRadius && distance <= maxRadius)
                    {
                        return new ParkNpcTransData()
                        {
                            pos = hit.position,
                            rot = Vector3.zero
                        };
                    }
                }
            }

            // 如果没找到，尝试在稍微扩大的范围内搜索
            float expandedMaxRadius = maxRadius * 1.2f;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector3 samplePos = GetRandomPointInRing(pos, minRadius, expandedMaxRadius);

                if (NavMesh.SamplePosition(samplePos, out hit, expandedMaxRadius, NavMesh.AllAreas))
                {
                    float distance = Vector3.Distance(pos, hit.position);
                    if (distance >= minRadius && distance <= expandedMaxRadius)
                    {
                        LoggerUtils.Log($"GetNearTransData: 在扩大范围内找到点，距离: {distance:F2}");
                        return new ParkNpcTransData()
                        {
                            pos = hit.position,
                            rot = Vector3.zero
                        };
                    }
                }
            }

            LoggerUtils.Log($"GetNearTransData: 在位置 {pos} 环形区域 [{minRadius}, {maxRadius}] 内未找到有效的行走点");
            return null;
        }

        /// <summary>
        /// 在环形区域内生成随机点
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="minRadius">最小半径</param>
        /// <param name="maxRadius">最大半径</param>
        /// <returns>环形区域内的随机点</returns>
        private Vector3 GetRandomPointInRing(Vector3 center, float minRadius, float maxRadius)
        {
            // 生成随机角度 (0-360度)
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // 生成随机半径 (在minRadius和maxRadius之间)
            // 使用平方根分布确保均匀分布
            float randomRadius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));

            // 计算随机点的坐标
            float x = center.x + randomRadius * Mathf.Cos(randomAngle);
            float z = center.z + randomRadius * Mathf.Sin(randomAngle);

            return new Vector3(x, center.y, z);
        }

        /// <summary>
        /// 获取指定位置环形区域内的多个有效行走点
        /// </summary>
        /// <param name="pos">中心位置</param>
        /// <param name="minRadius">最小半径</param>
        /// <param name="maxRadius">最大半径</param>
        /// <param name="count">需要获取的点数量</param>
        /// <param name="minDistance">点之间的最小距离</param>
        /// <returns>有效的行走点列表</returns>
        public List<ParkNpcTransData> GetMultipleNearTransData(Vector3 pos, float minRadius, float maxRadius, int count, float minDistance = 1.0f)
        {
            List<ParkNpcTransData> results = new List<ParkNpcTransData>();

            if (count <= 0 || minRadius < 0 || maxRadius <= 0 || minRadius >= maxRadius)
            {
                return results;
            }

            int maxAttempts = count * 15; // 最大尝试次数
            int attempts = 0;

            while (results.Count < count && attempts < maxAttempts)
            {
                ParkNpcTransData point = GetNearTransData(pos, minRadius, maxRadius);

                if (point != null)
                {
                    // 检查是否与已有点距离太近
                    bool tooClose = false;
                    foreach (var existingPoint in results)
                    {
                        if (Vector3.Distance(point.pos, existingPoint.pos) < minDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        results.Add(point);
                    }
                }

                attempts++;
            }

            LoggerUtils.Log($"GetMultipleNearTransData: 找到 {results.Count} 个有效点，尝试次数: {attempts}");
            return results;
        }

        public ParkNpcTransData GetBehaviourLocation(LocationType locationType, ActionType actionType)
        {
            var keyName = locationType + actionType.ToString();
            if (ActionLocationDict.TryGetValue(keyName, out ParkNpcTransData transData))
            {
                return transData;
            }
            else
            {
                LoggerUtils.LogError("没有找到对应操作目标的位置 " + locationType + actionType);
            }
            return ActionLocationDict[LocationType.None.ToString()];
        }

        public ParkNpcTransData GetLocationTargetNear(LocationType locationType)
        {
            ParkNpcTransData parkNpcTransData = new();
            if (locationType == LocationType.TrojanHorse)
            {
                var bev = GlobalNodeManager.Inst.Get<AIPark_TrojanhorseMgr>().GetBehaviour();
                var pos = bev.transform.position; pos.y = 0;
                parkNpcTransData = GetNearTransData(pos, 8f, 10f);
                parkNpcTransData.rot = Vector3.zero;
                return parkNpcTransData;
            }
            else if (locationType == LocationType.SlideSlides)
            {
                var bev = GlobalNodeManager.Inst.Get<AIPark_SlideMgr>().GetBehaviour();
                var pos = bev.transform.position; pos.y = 0;
                parkNpcTransData = GetNearTransData(pos, 4f, 6f);
                parkNpcTransData.rot = Vector3.zero;
                return parkNpcTransData;
            }
            else if (locationType == LocationType.SeeSaw)
            {
                var bev = GlobalNodeManager.Inst.Get<AIPark_SeesawMgr>().GetBehaviour();
                var pos = bev.transform.position; pos.y = 0;

                parkNpcTransData = GetNearTransData(pos, 0.5f, 2f);
                parkNpcTransData.rot = Vector3.zero;

                return parkNpcTransData;

            }
            else if (locationType == LocationType.Swinging)
            {
                var bev = GlobalNodeManager.Inst.Get<AIPark_SwingMgr>().GetBehaviour();
                var pos = bev.transform.position; pos.y = 0;

                parkNpcTransData = GetNearTransData(pos, 0.3f, 2f);
                parkNpcTransData.rot = Vector3.zero;
                return parkNpcTransData;

            }
            else if (locationType == LocationType.Fountain)
            {
                parkNpcTransData = GetNearTransData(new(5, 0, 3.36f), 7, 8);
                parkNpcTransData.rot = Vector3.zero;
                return parkNpcTransData;
            }
            else if (locationType == LocationType.Stage)
            {
                parkNpcTransData.pos = new Vector3(6, 0, -8.93f);
                parkNpcTransData.rot = Vector3.zero;
                return parkNpcTransData;
            }
            else if (locationType == LocationType.Park)
            {
                LocationType[] locationTypes = new LocationType[] { LocationType.SlideSlides, LocationType.SeeSaw, LocationType.Swinging };
                var tempLocationType = locationTypes[Random.Range(0, locationTypes.Length)];
                return GetLocationTargetNear(tempLocationType);
            }
            else
            {
                // var transData = GetBehaviourLocation(locationType, actionType);
                // return GetNearTransData(transData.pos, 0.5f, 1.2f);
            }
            return null;
        }

        public ParkNpcTransData GetMySpawnPointBy(LocationType locationType)
        {
            return null;
        }
    }
}