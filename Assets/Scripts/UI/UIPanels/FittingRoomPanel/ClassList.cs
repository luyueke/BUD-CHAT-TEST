using System;
using System.Collections;
using System.Collections.Generic;
using EventTracking;
using Game.Store;
using Game.Vehicle.PGCVehicle;
using GameData.PgcData;
using Newbie;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{

    public class ClassList : MonoBehaviour
    {
        [SerializeField] ToggleGroup classContent;
        [SerializeField] ClassItem classItem;

        private List<ClassData> classList;
        private Queue<ClassItem> classItemPool = new();
        private Dictionary<ClassData, ClassItem> classDict = new();
        private ClassItem curSelectedItem;
        private HashSet<int> _permanentHiddenIds = new();

        private Action<ClassData> onValueChanged;
        internal Action<ClassData> onValueChangedBase;

        public void SetPermanentHiddenIds(HashSet<int> ids)
        {
            _permanentHiddenIds = ids ?? new();
            foreach (var kv in classDict)
                if (_permanentHiddenIds.Contains(kv.Key.Id))
                    kv.Value.gameObject.SetActive(false);
        }

        public bool isInit = false;
        public void SetClass(ClassData selected, List<ClassData> list)
        {
            classList = list;
            UnuseAllItem();
            for (int i = 0, C = list.Count; i < C; i++)
            {
                var data = list[i];
                ClassItem item;
                if (classItemPool.Count > 0)
                {
                    item = classItemPool.Dequeue();
                }
                else
                {
                    item = Instantiate(classItem, classContent.transform);
                }
                item.SetToggleGroup(classContent);
                item.gameObject.SetActive(!_permanentHiddenIds.Contains(data.Id));
                item.transform.SetSiblingIndex(2 + i);
                item.SetClass(data);
                if(data.Id == 10008)
                {
                    var bootMono = item.gameObject.GetComponent<BootMaskMono>();
                    if (bootMono == null)
                    {
                        item.gameObject.AddComponent<BootMaskMono>().id.Add(105);
                    }
                    else
                    {
                        bootMono.id.Add(105);
                    }
                }
                if (data.Id == 10004)
                {
                    var bootMono = item.gameObject.GetComponent<BootMaskMono>();
                    if (bootMono == null)
                    {
                        item.gameObject.AddComponent<BootMaskMono>().id.Add(107);
                    }
                    else
                    {
                        bootMono.id.Add(107);
                    }
                }
                item.SetSelectedCallBack(OnSelecedClass);
                classDict.Add(data, item);
            }
            if(selected.Id == (int)OtherClass.UgcTheme)
            {
                isInit = true;
            }
            OnSelecedClass(selected);

            // 回调链可能重新激活被屏蔽的项，强制重新隐藏
            if (_permanentHiddenIds.Count > 0)
            {
                foreach (var kv in classDict)
                    if (_permanentHiddenIds.Contains(kv.Key.Id))
                        kv.Value.gameObject.SetActive(false);
            }
        }

        public void HideClass(int id)
        {
            foreach (var kv in classDict)
            {
                if(kv.Key.Id == id)
                {
                    kv.Value.gameObject.SetActive(false);
                }
            }
        }

        public void ShowClass(int id)
        {
            foreach (var kv in classDict)
            {
                if(kv.Key.Id == id)
                {
                    kv.Value.gameObject.SetActive(true);
                }
            }
        }

        public void OnSelecedClass(ClassData data)
        {
            if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc && !isInit)
            {
                LoadEvent.ReportPopupStatus(data.SpriteName+"Clicked", "ClickUGCTag");
            }
            else
            {
                isInit = false;
            }
            SetSelectedItem(classDict[data]);
            onValueChangedBase?.Invoke(data);
            onValueChanged?.Invoke(data);
        }

        private void UnuseAllItem()
        {
            foreach (var kv in classDict)
            {
                kv.Value.gameObject.SetActive(false);
                kv.Value.SetToggleGroup(null);
                classItemPool.Enqueue(kv.Value);
               if( kv.Value.transform.TryGetComponent<BootMaskMono>(out var comp))
                {
                    GameObject.DestroyImmediate(comp);
                }

            }

            classDict.Clear();
        }

        private void SetSelectedItem(ClassItem item)
        {
            curSelectedItem?.SetIsOn(false);
            curSelectedItem = item;
            curSelectedItem.SetIsOn(true);
        }

        public void SetCallback(Action<ClassData> action)
        {
            onValueChanged = action;
        }

        public void SetRedDot(HashSet<int> hashSet)
        {
            foreach (var kv in classDict)
            {
                var resourceType = UniqueType.ResourceType(kv.Key.Id);
                if (resourceType == ResourceType.Avatar || resourceType == ResourceType.UgcAvatar)
                {
                    var avatarSubType = UniqueType.AvatarSubType(kv.Key.Id);
                    kv.Value.SetRedDot(hashSet.Contains(UniqueType.GetAvatar(avatarSubType)) || hashSet.Contains(UniqueType.GetUgcAvatar(avatarSubType)));
                    if(avatarSubType == AvatarSubType.Shape && FittingRoomPanel.curTab == MainTabs.Tab.Bag)
                    {
                        kv.Value.SetRedDot(!PlayerPrefs.HasKey(ShapeDataMgr.Inst.shapeKey));
                    }
                } else if (resourceType == ResourceType.PGCPetAvatar || resourceType == ResourceType.UGCPetAvatar)
                {
                    var avatarSubType = UniqueType.AvatarSubType(kv.Key.Id);
                    kv.Value.SetRedDot(hashSet.Contains(UniqueType.GetPGCPetAvatar(avatarSubType)) || hashSet.Contains(UniqueType.GetUGCPetAvatar(avatarSubType)));
                }
                else if(resourceType == ResourceType.UgcVehicle)
                {
                    if(kv.Key.Id == UniqueType.Get(ResourceType.UgcVehicle, (int)VehicleSubType.AllVehicle))
                    {
                        var singleRedDot = hashSet.Contains(UniqueType.Get(ResourceType.UgcVehicle, (int)VehicleSubType.SingleVehicle));
                        var doubleRedDot = hashSet.Contains(UniqueType.Get(ResourceType.UgcVehicle, (int)VehicleSubType.DoubleVehicle));
                        kv.Value.SetRedDot(singleRedDot || doubleRedDot);
                    }
                    else{
                        kv.Value.SetRedDot(hashSet.Contains(kv.Key.Id));
                    }
                }
                else
                {
                    kv.Value.SetRedDot(hashSet.Contains(kv.Key.Id));
                }
            }
        }

        public void DefualtOn(int classId)
        {
            var classData = classList.Find(c => c.Id == classId);
            if (classData != null) OnSelecedClass(classData);
        }
    }

    public class ClassData
    {
        public int Id;
        public string SpriteName;

        public ClassData(int id, string spriteName)
        {
            Id = id;
            SpriteName = spriteName;
        }
    }
}
