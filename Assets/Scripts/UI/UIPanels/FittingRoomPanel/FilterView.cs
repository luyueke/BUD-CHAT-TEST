using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EventTracking;
using Game.CommunityGame;
using Game.Store;
using GameData.PgcData;
using UI.BaseWidgets;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class FilterView : MonoBehaviour
    {
        public Transform layout1Trans;
        public Transform layout2Trans;
        public Transform layout3TextTrans;
        public Transform layout3Trans;

        FilterItem[] filterItemList1;
        FilterItem[] filterItemList2;
        FilterItem[] filterItemList3;

        List<string> dataList1 = new() { "默认", "商品价格", "发布时间" };
        List<string> dataList2 = new() { "全部", "未拥有" };
        List<string> dataList3 = new(){
            "套装", //4
            "眼睛", //6
            "头发", //8
            "衣服", //98
            "动作类", //107
            "背包", //14
            "帽子", //9
            "鞋子", //13
            "嘴巴", //11 
            "眼镜", //7
            "手部饰品", //10
            "脸绘", //12
            "乐器类", //24
            "载具",//31
            "演员",
            "剧场",
        };


        private CButton cancelButton;
        private Action cancelAction;

        int classType;
        public void Awake()
        {
            cancelButton = GameObjectEx.FindChildByName(this.transform, "BackToPanelButton").GetComponent<CButton>();
            cancelButton.onClick.AddListener(OnCancelClick);

            filterItemList1 = layout1Trans.GetComponentsInChildren<FilterItem>(true);
            for (int i = 0; i < filterItemList1.Length; i++)
            {
                int index = i;
                filterItemList1[i].selectedAction = (isSelected) => OnFilter1Click(index);
                filterItemList1[i].SetText(dataList1[index]);
            }
            filterItemList2 = layout2Trans.GetComponentsInChildren<FilterItem>(true);
            for (int i = 0; i < filterItemList2.Length; i++)
            {
                int index = i;
                filterItemList2[i].selectedAction = (isSelected) => OnFilter2Click(index);
                filterItemList2[i].SetText(dataList2[index]);
            }
            filterItemList3 = layout3Trans.GetComponentsInChildren<FilterItem>(true);
            for (int i = 0; i < filterItemList3.Length; i++)
            {
                int index = i;
                filterItemList3[i].selectedAction = (isSelected) => OnFilter3Click(index, isSelected);
                filterItemList3[i].SetText(dataList3[index]);
            }
        }

        void OnFilter1Click(int index)
        {
            for (int i = 0; i < filterItemList1.Length; i++)
            {
                filterItemList1[i].SetSelected(i == index);
            }
            SearchLogicMgr.Inst.SaveCacheFilterSearchParam("sortType", index, 1, classType);
        }

        void OnFilter2Click(int index)
        {
            for (int i = 0; i < filterItemList2.Length; i++)
            {
                filterItemList2[i].SetSelected(i == index);
            }
            SearchLogicMgr.Inst.SaveCacheFilterSearchParam("ownType", index, 1, classType);
        }

        void OnFilter3Click(int index, bool isSelected)
        {
            if (isSelected)
            {
                //判断是否可去勾选 要求最少有3个勾选
                int selectedCount = 0;
                for (int i = 0; i < filterItemList3.Length; i++)
                {
                    if (filterItemList3[i].IsSelected)
                    {
                        selectedCount++;
                    }
                }
                if (selectedCount <= 3)
                {
                    UIAgentManager.Inst.ShowToast("至少需要选择三个分类");
                    return;
                }
            }
            filterItemList3[index].SetSelected(!isSelected);
            if (isSelected)
            {
                SearchLogicMgr.Inst.RemoveCacheFilterSearchParam("subTypes", SearchLogicMgr.avatarSubTypeList[index].ToString(), classType);
            }
            else
            {
                SearchLogicMgr.Inst.SaveCacheFilterSearchParam("subTypes", SearchLogicMgr.avatarSubTypeList[index].ToString(), 2, classType);
            }
        }
        public void OnCancelClick()
        {
            gameObject.SetActive(false);
            cancelAction?.Invoke();
        }
        public void OnEnable()
        {
            RefreshView();
        }

        public void SetFilterAction(Action action)
        {
            cancelAction = action;
        }

        void OnDisable()
        {
            try
            {
                transform.parent.GetComponent<RectMask2D>().enabled = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public bool IsFilterMode(){
            return gameObject.activeInHierarchy;
        }

        void RefreshView()
        {
            try
            {
                transform.parent.GetComponent<RectMask2D>().enabled = false;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }

            var cacheReq = SearchLogicMgr.Inst.GetCacheFilterSearchParam(classType);
            int sortType = int.Parse(cacheReq["sortType"].ToString());
            int ownType = int.Parse(cacheReq["ownType"].ToString());
            string subTypes = cacheReq["subTypes"].ToString();
            string[] subTypesList = subTypes.Split(',');
            for (int i = 0; i < filterItemList1.Length; i++)
            {
                filterItemList1[i].SetSelected(sortType == i);
            }
            for (int i = 0; i < filterItemList2.Length; i++)
            {
                filterItemList2[i].SetSelected(ownType == i);
            }
            for (int i = 0; i < filterItemList3.Length; i++)
            {
                filterItemList3[i].SetSelected(subTypesList.Contains(SearchLogicMgr.avatarSubTypeList[i].ToString()));
            }
        }

        public void SetClassType(int classType)
        {
            this.classType = classType;
            if (gameObject.activeInHierarchy)
            {
                Show(classType == (int)OtherClass.UgcTheme);
                RefreshView();
            }
        }

        public void Show(bool hadSubTypes)
        {
            gameObject.SetActive(true);
            if (hadSubTypes)
            {
                layout3TextTrans.gameObject.SetActive(true);
                layout3Trans.gameObject.SetActive(true);
            }
            else
            {
                layout3TextTrans.gameObject.SetActive(false);
                layout3Trans.gameObject.SetActive(false);
            }
        }
    }
}