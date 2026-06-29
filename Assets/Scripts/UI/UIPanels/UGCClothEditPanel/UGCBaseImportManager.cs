using System.Collections.Generic;
using System.Linq;
using GameData.UGCData;
using UnityEngine;
using System;
using UnityEngine.UI;

namespace Game.UGCEditor
{
    public abstract class UGCBaseImportManger
    {
        public ElementBaseBehaviour CurrentSelectBehaviour { get; set; }
        public abstract void SetElementHandleCanUse(bool isUse);
    }

    public class UGCImportManager<T>:UGCBaseImportManger where T:UGCImportManager<T>,new()
    {
        
        protected Dictionary<int, List<ElementBaseBehaviour>> dataDic =
            new Dictionary<int, List<ElementBaseBehaviour>>();

        protected virtual float textScalingRatio => 3;
        protected virtual float minSize => 414f;
        protected virtual int maxNum => 99;
        protected Action UndoRedoAct;
        protected virtual string overTip => "";
        private static T _instance;

        public static T Inst
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("UGCImportTextManager is not Init");
                }

                return _instance;
            }
        }

        public static void Create()
        {
            _instance = new T();
        }

        public void AddElement(int partIndex, ElementBaseBehaviour behav)
        {
            if (!dataDic.ContainsKey(partIndex))
            {
                dataDic.Add(partIndex, new List<ElementBaseBehaviour>());
            }
            dataDic[partIndex].Add(behav);
        }
        public List<ElementBaseBehaviour> GetElementList()
        {
            var list = new List<ElementBaseBehaviour>();
            if (dataDic.Count > 0)
            {
                foreach (var item in dataDic)
                {
                    if (item.Value!=null)
                    {
                        for (int i = 0; i < item.Value.Count; i++)
                        {
                            list.Add(item.Value[i]);
                        }
                    }
                }
            }
            return list;
        }
        public void RemoveElement(int partIndex, ElementBaseBehaviour behav)
        {
            if (dataDic.ContainsKey(partIndex))
            {
                dataDic[partIndex].Remove(behav);
                
            }
        }
        
        public void SetRaycastTarget(bool isActive)
        {
            foreach (var keyValue in dataDic)
            {
                keyValue.Value.ForEach(behaviour =>
                {
                    var childNode = behaviour.transform.Find("Button");
                    childNode.GetComponent<Image>().raycastTarget = isActive;
                });
            }
        }

        public ElementBaseBehaviour GetDefaultElement(int partIndex)
        {
            if (dataDic.ContainsKey(partIndex) && dataDic[partIndex].Count > 0)
            {
                return dataDic[partIndex][0];
            }

            return null;
        }

        public ElementBaseBehaviour GetLastElement(int partIndex)
        {
            if (dataDic.ContainsKey(partIndex) && dataDic[partIndex].Count > 0)
            {
                return dataDic[partIndex].Last();
            }

            return null;
        }

        public override void SetElementHandleCanUse(bool isUse)
        {
            foreach (var keyValue in dataDic)
            {
                keyValue.Value.ForEach(behaviour => { behaviour.IsCanSelect = isUse; });
            }
        }

        public void ShowElementByIndex(int partIndex)
        {
            if (dataDic.ContainsKey(partIndex))
            {
                dataDic[partIndex].ForEach(behaivour =>
                {
                    if (behaivour)
                    {
                        behaivour.SetActive(true);
                    }
                });
            }
        }
        
        public int GetCompCountByPartIndex(int partIndex)
        {
            if(dataDic.ContainsKey(partIndex))
                return dataDic[partIndex].Count;
            return 0;
        }

        public void OnChangePart(int partIndex)
        {
            foreach (var keyValue in dataDic)
            {
                if (keyValue.Key == partIndex)
                {
                    keyValue.Value.ForEach(behaviour => { behaviour.SetActive(true); });
                }
                else
                {
                    keyValue.Value.ForEach(behaviour => { behaviour.SetActive(false); });
                }
            }
        }

        public void ShowHideAllPhoto(bool isShow)
        {
            foreach (var keyValue in dataDic)
            {
                keyValue.Value.ForEach(behaviour => { behaviour.SetActive(isShow); });
            }
        }

        public bool IsReachMaximumValue()
        {
            int count = 0;
            foreach (var keyValue in dataDic)
            {
                if (keyValue.Value != null)
                {
                    count += keyValue.Value.Count;
                }
            }

            if (count >= maxNum)
            {
                TipPanel.ShowToast(overTip);
                return true;
            }

            return false;
        }
        
        public T CreatEmptyElement<T>(int partIndex, Transform protoParent,
            Transform mirrorParent, GameObject photoPrefab) where T:ElementBaseBehaviour
        {
            if (IsReachMaximumValue())
            {
                return null;
            }
            var photoGo = GameObject.Instantiate(photoPrefab, protoParent);
            var behav = photoGo.AddComponent<T>();
            behav.OnCreate(mirrorParent);
            AddElement(partIndex, behav);
            return behav;
        }
        
        public T CreatElement<T,D>(int partIndex, D pData, Transform protoParent,
            Transform mirrorParent, GameObject photoPrefab)where T:ElementBaseBehaviour where D:UGCImportData
        {
            var behav = CreatEmptyElement<T>(partIndex,protoParent,mirrorParent,photoPrefab);
            if (behav != null && pData != null)
            { 
                behav.SetData(pData);
            }
            return behav;
        }
        
        public virtual void Dispose()
        {
            _instance = null;
        }
    }
}