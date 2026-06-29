using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;
using GameData.BaseInfo;
using Game.Props.PropsComponents;

namespace Game.Props.PropsManagers
{
    public class AIHospital_PropsManager : GlobalInstance<AIHospital_PropsManager>
    {
        private List<NodeBaseBehaviour> _sceneNodeBevs = new List<NodeBaseBehaviour>();
        public void InitData()
        {
            _sceneNodeBevs.Clear();
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_DoorManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_FileCabinetManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_DeanCabinetManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_MedicineCabinetManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_RoomDoorManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_ToiletManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_CorridorDoorManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_ReceptionDoorManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_ChairManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_EquipmentManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_BedManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_LampManager>().GetBevs());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIHospital_MedicineManager>().GetBevs());
        }

        public void SetInteractEnable(bool isEnable)
        {
            bool isLink = BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(AccountDataManager.Inst.Uid);
            foreach (var item in _sceneNodeBevs)
            {
                if (item is AIPropBaseBehaviour propBaseBehaviour && isLink)
                {
                    propBaseBehaviour.IsCanClick = isEnable && propBaseBehaviour.AllowClickInEmoteLink;
                }
                else
                    item.IsCanClick = isEnable;
            }
        }

        public void OpenConsultingRoomDoor()
        {
            var roomDoorMgr = GlobalNodeManager.Inst.Get<AIHospital_RoomDoorManager>();
            var ConsultingDoor = roomDoorMgr.GetConsultingRoomDoorBehaviour();
            ConsultingDoor?.HandOpenDoor();

            roomDoorMgr.GetBevs().ForEach(door => door.SetEnableColliderHitOpenDoor(true));
            GlobalNodeManager.Inst.Get<AIHospital_CorridorDoorManager>().GetBevs().ForEach(door => door.SetEnableColliderHitOpenDoor(true));
            GlobalNodeManager.Inst.Get<AIHospital_ReceptionDoorManager>().GetBevs().ForEach(door => door.SetEnableColliderHitOpenDoor(true));
        }

        public void InitPhoto(List<HospitalPhotoData> datas)
        {
            var photoMgr = GlobalNodeManager.Inst.Get<AIHospital_PhotoManager>();
            var photos = photoMgr.GetBevs();

            // 1. 按location分组
            Dictionary<int, List<string>> locationGroups = new Dictionary<int, List<string>>();
            foreach (var data in datas)
            {
                if (!locationGroups.ContainsKey(data.location))
                {
                    locationGroups[data.location] = data.urls;
                }
                //locationGroups[data.location].Add(data.urls);
            }

            foreach (var item in photos)
            {
                item.gameObject.SetActive(false);
            }
            // 2. 遍历每个location组，找到对应的ShotPhotoBehaviour并初始化
            foreach (var locationGroup in locationGroups)
            {
                int location = locationGroup.Key;
                var photoDataList = locationGroup.Value;
                
                // 找到该location下的所有照片行为组件
                var locationPhotos = photos.Where(p => p.location == location).ToList();
                
                // 确保照片数量匹配
                if (locationPhotos.Count != photoDataList.Count)
                {
                    Debug.LogError($"Photo count mismatch for location {location}: expected {photoDataList.Count}, found {locationPhotos.Count}");
                    continue;
                }

                // 按顺序初始化每个照片
                for (int i = 0; i < photoDataList.Count; i++)
                {
                    var photoBev = locationPhotos[i];
                    var url = photoDataList[i];

                    if (photoBev != null&&!string.IsNullOrEmpty(url))
                    {
                        photoBev.gameObject.SetActive(true);
                        photoBev.Load(url);
                    }
                    else
                        photoBev.gameObject.SetActive(false);
                }
            }
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_BaseDoorBehaviour))]
    public class AIHospital_DoorManager : BaseNodeManager
    {
        public List<AIHospital_BaseDoorBehaviour> GetBevs()
        {
            List<AIHospital_BaseDoorBehaviour> list = new List<AIHospital_BaseDoorBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_BaseDoorBehaviour);
            });
            return list;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_FileCabinetBehaviour))]
    public class AIHospital_FileCabinetManager : BaseNodeManager
    {
        public List<AIHospital_FileCabinetBehaviour> GetBevs()
        {
            List<AIHospital_FileCabinetBehaviour> list = new List<AIHospital_FileCabinetBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_FileCabinetBehaviour);
            });
            return list;
        }
        
        public AIHospital_FileCabinetBehaviour GetCabinetBev()
        {
            return entities[0] as AIHospital_FileCabinetBehaviour;
        }
    }
        
    [NodeBehaviourAttribute(typeof(AIHospital_DeanCabinetBehaviour))]
    public class AIHospital_DeanCabinetManager : BaseNodeManager
    {
        public List<AIHospital_DeanCabinetBehaviour> GetBevs()
        {
            List<AIHospital_DeanCabinetBehaviour> list = new List<AIHospital_DeanCabinetBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_DeanCabinetBehaviour);
            });
            return list;
        }
        
        public AIHospital_DeanCabinetBehaviour GetCabinetBev()
        {
            return entities[0] as AIHospital_DeanCabinetBehaviour;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_MedicineCabinetBehaviour))]
    public class AIHospital_MedicineCabinetManager : BaseNodeManager
    {
        public List<AIHospital_MedicineCabinetBehaviour> GetBevs()
        {
            List<AIHospital_MedicineCabinetBehaviour> list = new List<AIHospital_MedicineCabinetBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_MedicineCabinetBehaviour);
            });
            return list;
        }
        
        public AIHospital_MedicineCabinetBehaviour GetCabinetBev()
        {
            return entities[0] as AIHospital_MedicineCabinetBehaviour;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_RoomDoorBehaviour))]
    public class AIHospital_RoomDoorManager : BaseNodeManager
    {
        public List<AIHospital_RoomDoorBehaviour> GetBevs()
        {
            List<AIHospital_RoomDoorBehaviour> list = new List<AIHospital_RoomDoorBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_RoomDoorBehaviour);
            });
            return list;
        }

        public AIHospital_RoomDoorBehaviour GetConsultingRoomDoorBehaviour()
        {
            var doors = GetBevs();
            var targetDoor = doors.Find(x => x.DoorLocation == LocationType.ConsultingRoom);
            return targetDoor;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_ToiletBehaviour))]
    public class AIHospital_ToiletManager : BaseNodeManager
    {
        public List<AIHospital_ToiletBehaviour> GetBevs()
        {
            List<AIHospital_ToiletBehaviour> list = new List<AIHospital_ToiletBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_ToiletBehaviour);
            });
            return list;
        }

        public AIHospital_ToiletBehaviour GetEmptyToilet()
        {
            foreach (var bev in entities)
            {
                var toiletBev = bev as AIHospital_ToiletBehaviour;
                if (toiletBev.IsCanClick)
                    return toiletBev;
            }
            return null;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_CorridorDoorBehaviour))]
    public class AIHospital_CorridorDoorManager : BaseNodeManager
    {
        public List<AIHospital_CorridorDoorBehaviour> GetBevs()
        {
            List<AIHospital_CorridorDoorBehaviour> list = new List<AIHospital_CorridorDoorBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_CorridorDoorBehaviour);
            });
            return list;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_ReceptionDoorBehaviour))]
    public class AIHospital_ReceptionDoorManager : BaseNodeManager
    {
        public List<AIHospital_ReceptionDoorBehaviour> GetBevs()
        {
            List<AIHospital_ReceptionDoorBehaviour> list = new List<AIHospital_ReceptionDoorBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_ReceptionDoorBehaviour);
            });
            return list;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_ChairBehaviour))]
    public class AIHospital_ChairManager : BaseNodeManager
    {
        Dictionary<int, AIHospital_ChairBehaviour> _bevDic = new();
        public List<AIHospital_ChairBehaviour> GetBevs()
        {
            List<AIHospital_ChairBehaviour> list = new List<AIHospital_ChairBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                if (x is AIHospital_ChairBehaviour chair)
                {
                    list.Add(chair);
                    _bevDic.Add(chair.Uid, chair);
                }
            });
            return list;
        }
        public AIHospital_ChairBehaviour GetChairByType(AIHospital_ChairBehaviour.Hospital_ChairType type)
        {
            foreach (var bev in entities)
            {
                var chairBev = bev as AIHospital_ChairBehaviour;
                if (chairBev._chairType == type)
                    return chairBev;
            }
            return null;
        }

        public AIHospital_ChairBehaviour GetEmptyCorridorChair()
        {
            foreach (var bev in entities)
            {
                var chairBev = bev as AIHospital_ChairBehaviour;
                if ((chairBev._chairType == AIHospital_ChairBehaviour.Hospital_ChairType.WaitingRoom))
                {
                    if(chairBev.IsCanClick)
                        return chairBev;
                }
            }
            return null;
        }

        public AIHospital_ChairBehaviour GetBevsByUID(int uid)
        {
            if (_bevDic.TryGetValue(uid,out AIHospital_ChairBehaviour chair))
            {
                return chair;
            }
            return null;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_EquipmentBehaviour))]
    public class AIHospital_EquipmentManager : BaseNodeManager
    {
        public List<AIHospital_EquipmentBehaviour> GetBevs()
        {
            List<AIHospital_EquipmentBehaviour> list = new List<AIHospital_EquipmentBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_EquipmentBehaviour);
            });
            return list;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_BedBehaviour))]
    public class AIHospital_BedManager : BaseNodeManager
    {
        public List<AIHospital_BedBehaviour> GetBevs()
        {
            List<AIHospital_BedBehaviour> list = new List<AIHospital_BedBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_BedBehaviour);
            });
            return list;
        }
        public AIHospital_BedBehaviour GetEmptyBed()
        {
            foreach (var bev in entities)
            {
                var bedBev = bev as AIHospital_BedBehaviour;
                if (bedBev.IsCanClick)
                    return bedBev;
            }
            return null;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_LampBehaviour))]
    public class AIHospital_LampManager : BaseNodeManager
    {
        public List<AIHospital_LampBehaviour> GetBevs()
        {
            List<AIHospital_LampBehaviour> list = new List<AIHospital_LampBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_LampBehaviour);
            });
            return list;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_MedicineBehaviour))]
    public class AIHospital_MedicineManager : BaseNodeManager
    {
        public List<AIHospital_MedicineBehaviour> GetBevs()
        {
            List<AIHospital_MedicineBehaviour> list = new List<AIHospital_MedicineBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_MedicineBehaviour);
            });
            return list;
        }
        
        public AIHospital_MedicineBehaviour GetEmptyMedicine()
        {
            foreach (var bev in entities)
            {
                var medBev = bev as AIHospital_MedicineBehaviour;
                if (medBev.IsCanClick)
                    return medBev;
            }
            return null;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_CharacterBehaviour))]
    public class AIHospital_CharacterNodeManager : BaseNodeManager
    {
    }

    [NodeBehaviourAttribute(typeof(AIHospital_MainDoorBehaviour))]
    public class AIHospital_MainDoorManager : BaseNodeManager
    {
        public AIHospital_MainDoorBehaviour GetMainDoor()
        {
            List<AIHospital_MainDoorBehaviour> list = new List<AIHospital_MainDoorBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_MainDoorBehaviour);
            });
            return list[0] as AIHospital_MainDoorBehaviour;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIHospital_BrokeLightBehaviour))]
    public class AIHospital_BrokeLightManager : BaseNodeManager
    {
    }

    [NodeBehaviourAttribute(typeof(AIHospital_TargetPointBehaviour))]
    public class AIHospital_TargetPointManager : BaseNodeManager
    {
    }

    [NodeBehaviourAttribute(typeof(AIHospital_PhotoBehaviour))]
    public class AIHospital_PhotoManager : BaseNodeManager
    {
        public List<AIHospital_PhotoBehaviour> GetBevs()
        {
            List<AIHospital_PhotoBehaviour> list = new List<AIHospital_PhotoBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIHospital_PhotoBehaviour);
            });
            return list;
        }
    }
}