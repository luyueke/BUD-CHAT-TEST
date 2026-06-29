using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.BaseWidgets
{
    /// <summary>
    /// Author:JayWill
    /// Description:组件SectionTab
    /// </summary>
    [Serializable]
    public class TabView : MonoBehaviour
    {
        public GameObject ItemTmpl; //Item模板，供动态创建Item时使用

        [SerializeField]private Transform ItemsContent;
        private Action<TabItem, int> onSelectCallBack;

        void Awake()
        {
            if (ItemTmpl == null)
            {
                ItemTmpl = GameObjectEx.FindChildByName(transform, "ItemTmpl")?.gameObject;
            }

            if (ItemTmpl != null)
            {
                ItemTmpl.SetActive(false);
            }
            
            if (ItemsContent == null)
            {
                ItemsContent = GameObjectEx.FindChildByName(transform, "ItemsContent");
            }

            InitTabItemListener();
        }

        private void InitTabItemListener()
        {
            for (int i = 0; i < ItemsContent.childCount; i++)
            {
                var item = ItemsContent.GetChild(i);
                TabItem tabItem = item.GetComponentInChildren<TabItem>(true);
                if (tabItem.IsInited) continue; // 这里可能被重复初始化
                int index = i;
                tabItem.Init();
                tabItem.AddValueChangeCallListener((isOn) =>
                {
                    if (isOn == true)
                    {
                        OnTabItemSelect(tabItem, index);
                    }
                });

            }
        }

        /// <summary>
        /// 通过名字获取到Item
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public TabItem GetItemByName(string itemName)
        {
            TabItem result = null;
            for (int i = 0; i < ItemsContent.childCount; i++)
            {
                var item = ItemsContent.GetChild(i);
                if (item.name == itemName)
                {
                    result = item.GetComponentInChildren<TabItem>(true);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// 通过Index获取Item
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TabItem GetItemByIndex(int index)
        {
            Transform itemNode = ItemsContent.GetChild(index);
            if (itemNode)
            {
                return itemNode.GetComponentInChildren<TabItem>(true);
            }

            return null;
        }

        /// <summary>
        /// 动态创建Item
        /// </summary>
        /// <param name="itemName">开发命名</param>
        /// <param name="showName">显示的名字</param>
        /// <returns></returns>
        public TabItem CreateItem(string itemName, string showName = "")
        {
            var index = ItemsContent.childCount;
            var newItem = GameObject.Instantiate(ItemTmpl, ItemsContent);
            newItem.SetActive(true);
            TabItem tabItem = newItem.GetComponent<TabItem>();
            if (tabItem == null)
            {
                tabItem = newItem.AddComponent<TabItem>();
            }
            tabItem.Init();
            tabItem.SetShowName(showName);
            tabItem.SetEditorName(itemName);
            tabItem.AddValueChangeCallListener((isOn) =>
            {
                if (isOn == true)
                {
                    OnTabItemSelect(tabItem, index);
                }
            });
            return tabItem;
        }

   
        /// <summary>
        /// //通过下标选中
        /// </summary>
        /// <param name="index"></param>
        public void SetSelect(int index)
        {
            TabItem tabItem = GetItemByIndex(index);
            if (tabItem != null)
            {
                tabItem.SetIsSelect(true);
            }
        }

        /// <summary>
        /// 通过名字选中
        /// </summary>
        /// <param name="itemName"></param>
        public void SetSelect(string itemName)
        {
            TabItem tabItem = GetItemByName(itemName);
            if (tabItem != null)
            {
                tabItem.SetIsSelect(true);
            }
        }

        public void SelectWithoutCallback(int index)
        {
            TabItem tabItem = GetItemByIndex(index);
            if (tabItem != null)
            {
                tabItem.SetIsSelectWithoutCallback(true);
            }
        }
        
        public void SelectWithoutCallback(string itemName)
        {
            TabItem tabItem = GetItemByName(itemName);
            if (tabItem != null)
            {
                tabItem.SetIsSelectWithoutCallback(true);
            }
        }


        private void OnTabItemSelect(TabItem tabItem, int index)
        {
            LoggerUtils.Log("OnTabItemSelect:" + tabItem.name + "index:" + index);
            onSelectCallBack?.Invoke(tabItem, index);
        }

        /// <summary>
        /// 监听被选中的，TabItem，回调包含TabItem本身和index
        /// </summary>
        /// <param name="callback"></param>
        public void AddItemSelectCallBack(Action<TabItem, int> callback)
        {
            onSelectCallBack += callback;
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveItemSelectCallBack(Action<TabItem, int> callback)
        {
            onSelectCallBack -= callback;
        }

        public void RemoveAllSelectCallBack()
        {
            onSelectCallBack = null;
        }

        private void OnDestroy()
        {
            RemoveAllSelectCallBack();
        }
    }
}
