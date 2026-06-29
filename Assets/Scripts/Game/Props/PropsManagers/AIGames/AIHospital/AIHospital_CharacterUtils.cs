using System.Collections;
using System.Collections.Generic;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    public class AIHospital_CharacterUtils : GlobalInstance<AIHospital_CharacterUtils>
    {
        //官方游戏出生点
        public Dictionary<HospitalNpcRoleType, HospitalNpcTransData> OfficalGameSpawnPoint =
            new Dictionary<HospitalNpcRoleType, HospitalNpcTransData>()
            {
                [HospitalNpcRoleType.Doctor] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-12.4783f,1.5f,-25.42f),
                    rot =  new Vector3(0,-90,0),
                },
                [HospitalNpcRoleType.Dean] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-17.48f,1.5f,-15.9f),
                    rot =  new Vector3(0,90,0),
                },
                [HospitalNpcRoleType.Nurse] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-3.71f,1.5f,-25.42f),
                    rot =  new Vector3(0,-90,0),
                },
                [HospitalNpcRoleType.Dustman] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-10.375f,1.5f,13.78f),
                    rot =  new Vector3(0,90,0),
                },
                [HospitalNpcRoleType.Pharmacist] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-13.341f,1.5f,1.5817f),
                    rot =  new Vector3(0,90,0),
                },
                [HospitalNpcRoleType.Patient] = new HospitalNpcTransData()
                {
                    pos = new Vector3(2.486f,1.5f,-18.96f),
                    rot =  new Vector3(0,180,0),
                },
                [HospitalNpcRoleType.Security] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-2.884f,1.41f,-2.323f),
                    rot =  new Vector3(0,-45,0),
                },
            };
        
        //官方游戏出生点
        public List<HospitalNpcTransData> UGCGameSpawnPoint =
            new List<HospitalNpcTransData>()
            {
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-12.4783f,1.5f,-25.42f),
                    rot =  new Vector3(0,-90,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-17.48f,1.5f,-15.9f),
                    rot =  new Vector3(0,90,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-3.71f,1.5f,-25.42f),
                    rot =  new Vector3(0,-90,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-10.375f,1.5f,13.78f),
                    rot =  new Vector3(0,90,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-13.341f,1.5f,1.5817f),
                    rot =  new Vector3(0,90,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(2.486f,1.5f,-18.96f),
                    rot =  new Vector3(0,180,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-2.884f,1.41f,-2.323f),
                    rot =  new Vector3(0,-45,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-9.67f,1.41f,-20.8f),
                    rot =  new Vector3(0,180,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-8.5f,1.41f,-20.8f),
                    rot =  new Vector3(0,180,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-7f,1.41f,-20.8f),
                    rot =  new Vector3(0,180,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-7.2f,1.41f,-23.8f),
                    rot =  new Vector3(0,180,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-5.79f,1.41f,-6.91f),
                    rot = new Vector3(0,180,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-6.3f,1.41f,-9f),
                    rot = new Vector3(0,180,0),
                },
                new HospitalNpcTransData()
                {
                    pos = new Vector3(-7.03f,1.41f,-12.5f),
                    rot = new Vector3(0,180,0),
                },
            };
        
        public Dictionary<LocationType, HospitalNpcTransData> SpawnPointByLocationTypeDict =
            new Dictionary<LocationType, HospitalNpcTransData>()
            {
                [LocationType.ConsultingRoom] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-10, 1.5f, -18),
                    rot =  new Vector3(0,90,0),
                },
                [LocationType.DirectorsOffice] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-10, 1.5f, -18),
                    rot =  new Vector3(0,90,0),
                },
                [LocationType.WaitingRoom] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-10, 1.5f, -18),
                    rot =  new Vector3(0,90,0),
                },
                [LocationType.Toilet] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-11.53f, 1.5f, -12),
                    rot =  new Vector3(0,180,0),
                },
                [LocationType.Ward] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-10, 1.5f, -18),
                    rot =  new Vector3(0,90,0),
                },
                [LocationType.Corridor] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-10, 1.5f, -18),
                    rot =  new Vector3(0,90,0),
                },
                [LocationType.Pharmacy] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-10, 1.5f, -18),
                    rot =  new Vector3(0,90,0),
                },
                [LocationType.OutDoor] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-10, 1.5f, -18),
                    rot =  new Vector3(0,90,0),
                },
                [LocationType.None] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-10, 1.5f, -18),
                    rot =  new Vector3(0,90,0),
                },
            };

        private Dictionary<string, HospitalNpcTransData> ActionLocationDict = new Dictionary<string, HospitalNpcTransData>()
            {
                #region ConsultingRoom 诊室
                //在 诊室 进行 弄设备 时的位置
                [LocationType.ConsultingRoom + ActionType.OperatingEquipment.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-15.247f,1.41f,-28.9f),
                    rot =  new Vector3(0,-90,0),
                },
                //在 诊室 进行 给病人打针 时的位置
                [LocationType.ConsultingRoom + ActionType.InjectionsToPatients.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-17.75f,1.41f,-24.46f),
                    rot =  new Vector3(0,180,0),
                },
                //在 诊室 进行 坐下 时的位置
                [LocationType.ConsultingRoom + ActionType.Sitting.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-14.86f,1.41f,-24.69f),
                    rot =  new Vector3(0,180,0),
                },
                //在 诊室 进行 躺下 时的位置
                [LocationType.ConsultingRoom + ActionType.LyingDown.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-11.39f,1.41f,-19.4f),
                    rot =  new Vector3(0,180,0),
                },
                //在 诊室 进行 交谈 时的位置
                [LocationType.ConsultingRoom + ActionType.Talk.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-11.39f,1.41f,-19.4f),
                    rot =  new Vector3(0,180,0),
                },
                #endregion

                #region DirectorsOffice 院长室
                //在 院长室 整理文件
                [LocationType.DirectorsOffice + ActionType.OrganizingDocuments.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-13.0f,1.41f,-20.73f),
                    rot =  new Vector3(0,180,0),
                },
                //在 院长室 办公
                [LocationType.DirectorsOffice + ActionType.Working.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-15.736f,1.41f,-20.42f),
                    rot =  new Vector3(0,-90,0),
                },
                //在 院长室 交谈
                [LocationType.DirectorsOffice + ActionType.Talk.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-11.39f,1.41f,-19.4f),
                    rot =  new Vector3(0,180,0),
                },
                #endregion

                #region WaitingRoom 候诊室
                // 在 候诊室 坐下
                [LocationType.WaitingRoom + ActionType.Sitting.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-10.05f,1.41f,-8.60f),
                    rot =  new Vector3(0,90,0),
                },
                [LocationType.WaitingRoom + ActionType.Talk.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-11.53f, 1.5f, 12),
                    rot =  new Vector3(0,180,0),
                },
                #endregion
                
                #region Toilet 厕所
                //在 厕所 进行 上厕所 时的位置
                [LocationType.Toilet + ActionType.UsingRestroom.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-16.332f,1.41f,15.90f),
                    rot =  new Vector3(0,180,0),
                },
                //在 厕所 进行 清洁 时的位置
                [LocationType.Toilet + ActionType.Cleaning.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-18.08f,1.41f,18.25f),
                    rot =  new Vector3(0,90,0),
                },
                #endregion
                
                #region Ward 病房
                //在 病房 整理文件
                [LocationType.Ward + ActionType.OrganizingDocuments.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-9.2f,1.5f,-13.8f),
                    rot =  new Vector3(0,180,0),
                },
                //在 病房 进行 吃药 时的位置
                [LocationType.Ward + ActionType.TakingMedication.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(2.41f,1.41f,-23.236f),
                    rot =  new Vector3(0,150,0),
                },
                //在 病房 进行 查房 时的位置
                [LocationType.Ward + ActionType.ConductingRounds.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(0.241f,1.41f,-23.32f),
                    rot =  new Vector3(0,90,0),
                },
                //在 病房 进行 躺下 时的位置
                [LocationType.Ward + ActionType.LyingDown.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(2.705f,1.41f,-19.30f),
                    rot =  new Vector3(0,0,0),
                },
                #endregion

                #region Corridor 走廊
                //在 走廊 进行 清洁
                [LocationType.Corridor + ActionType.Cleaning.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-4.78f,1.41f,-16.42f),
                    rot =  new Vector3(0,-90,0),
                },
                //在 走廊 进行 交谈
                [LocationType.Corridor + ActionType.Talk.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-11.53f, 1.5f, 12),
                    rot =  new Vector3(0,180,0),
                },
                #endregion
                
                #region Pharmacy 药房
                //在 药房 进行 办公
                [LocationType.Pharmacy + ActionType.Working.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-15.04f,1.41f,0.284f),
                    rot =  new Vector3(0,-90,0),
                },
                //在 药房 进行 配药
                [LocationType.Pharmacy + ActionType.DispensingMedication.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-15.04f,1.41f,0.284f),
                    rot =  new Vector3(0,-90,0),
                },
                //在 药房 进行 交谈
                [LocationType.Pharmacy + ActionType.Talk.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-10.5f, 1.5f, 0.1f),
                    rot =  new Vector3(0,-90,0),
                },
                #endregion
                
                #region OutDoor 大门
                //在 大门 进行 交谈
                [LocationType.OutDoor + ActionType.Talk.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-2.884f,1.41f,-2.323f),
                    rot =  new Vector3(0,-45,0),
                },
                //在 大门 进行 站立
                [LocationType.OutDoor + ActionType.Stand.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-2.884f,1.41f,-2.323f),
                    rot =  new Vector3(0,-45,0),
                },
                //在 大门 进行 睡觉
                [LocationType.OutDoor + ActionType.Sleep.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-2.884f,1.41f,-2.323f),
                    rot =  new Vector3(0,-45,0),
                },
                #endregion

                [LocationType.None.ToString()] = new HospitalNpcTransData()
                {
                    pos = new Vector3(-4.324f,1.41f,-7.17f),
                    rot =  new Vector3(0,180,0),
                },
            };
        
        public HospitalNpcTransData GetSpawnPointByRoleType(HospitalNpcRoleType roleType)
        {
            if (OfficalGameSpawnPoint.TryGetValue(roleType, out HospitalNpcTransData transData))
            {
                return transData;
            }
            else
            {
                LoggerUtils.LogError("没有找到对应人物的出生位置");
            }

            return OfficalGameSpawnPoint[HospitalNpcRoleType.Doctor];
        }
        
        public HospitalNpcTransData GetSpawnPointByLocationType(LocationType locationType)
        {
            if (SpawnPointByLocationTypeDict.TryGetValue(locationType, out HospitalNpcTransData transData))
            {
                return transData;
            }
            else
            {
                LoggerUtils.LogError("没有找到对应操作目标的位置");
            }

            return SpawnPointByLocationTypeDict[LocationType.None];
        }
        
        public HospitalNpcTransData GetBehaviourLocation(LocationType locationType, ActionType actionType)
        {
            var keyName = locationType + actionType.ToString();
            if (ActionLocationDict.TryGetValue(keyName, out HospitalNpcTransData transData))
            {
                return transData;
            }
            else
            {
                LoggerUtils.LogError("没有找到对应操作目标的位置 " + locationType + actionType);
            }
            return ActionLocationDict[LocationType.None.ToString()];
        }
    }
}