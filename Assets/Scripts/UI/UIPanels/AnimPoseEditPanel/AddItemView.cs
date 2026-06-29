using System;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.PgcData;
using UnityEngine;

namespace BUD.AnimPose
{
    public class AddItemView : PoseBaseView
    {
        [SerializeField] private Transform contentNode;
        [SerializeField] private AddPropViewItem propItem;
        private List<AddPropViewItem> propItems = new List<AddPropViewItem>();
        private const int ItemCount = 5;
        public Action<AddPropViewItem> OnSelectProp;
        public override void OnShow(EnterPanelMode panelMode, UgcPoseSubType poseType)
        {
            base.OnShow(panelMode, poseType);
            this.gameObject.SetActive(panelMode == EnterPanelMode.AnimEnter);
            if (panelMode != EnterPanelMode.AnimEnter)
            {
                return;
            }
         
            for (int i = 0; i < ItemCount; i++)
            {
                var item = GameObject.Instantiate(propItem, contentNode);
                var propData = GetOrAddPropData(i);
                item.UpdateData(i > 0, propData, OnSelect);
                propItems.Add(item);
            }
        }

        public void SetPropIk(PropAnimIK propIk)
        {
            propItems[propIk.curIndex].propIk = propIk;
        }

        public AnimPropData GetOrAddPropData(int i)
        {
            //TODO:目前propList存在重复数据，临时策略
            var propList = AnimDataManager.Inst.animInfo.propList;
            if (propList == null)
            {
                propList = new List<AnimPropData>();
            }

            var propDic = AnimDataManager.Inst.propDic;
            if (propDic == null)
            {
                propDic= new Dictionary<int, AnimPropData>();
            }
            
            foreach (var prop in propList)
            {
                propDic[prop.index] = prop;
            }
           
            AnimPropData propData = null;
            if (propDic.ContainsKey(i))
            {
                propData = propDic[i];
            }
            else
            {
                propData = new AnimPropData
                {
                    index = i
                };
                propDic.Add(i,propData);
            }
            AnimDataManager.Inst.propDic = propDic;
            return propData;
        }

        public AddPropViewItem GetSelectItem(int index)
        {
            return propItems[index];
        }

        private void OnSelect(AddPropViewItem item)
        {
            for (var i = 0; i < propItems.Count; i++)
            {
                propItems[i].SetSelectIconVisible(false);
            }
            item.SetSelectIconVisible(true);
            OnSelectProp?.Invoke(item);
        }
    }

}