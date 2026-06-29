/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-02 13:08:17
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-22 17:35:42
 * @ Description: 通用属性面板
 */

using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using System;
using System.Collections.Generic;
using UI.EditOperation;

namespace UI.UIPanels.GameEdit
{
    [Serializable]
    public class UniversalPropertyViewConfig
    {
        public string name;
        public OperationType operationType;
        public GameObject view;
    }

    public class UniversalPropertyPanel : BasePanel<UniversalPropertyPanel>
    {
        [SerializeField]private CButton closeBtn;
        [SerializeField]private TabView topTabView;
        [SerializeField]private Transform viewContent;
        [Header("界面配置")]
        [SerializeField]private List<UniversalPropertyViewConfig> views;

        int currentSelectIndex = -1;
        Dictionary<int, GameObject> viewCache = new Dictionary<int, GameObject>();

        public override void OnCreate()
        {
            closeBtn.onClick.AddListener(OnCloseClick);

            for (int i = 0; i < views.Count; i++)
            {
                var viewConfig = views[i];
                string nameKey = viewConfig.name;
                if (nameKey == "移动")
                {
                    nameKey = "移动性";
                }
                var tabItem = topTabView.CreateItem($"topTab_{viewConfig.operationType.ToString()}", nameKey);

                EditOperationManager.Inst.BindRuleUI(viewConfig.operationType, tabItem.gameObject);
            }

            topTabView.AddItemSelectCallBack(OnTabSelect);
        }

        public override void OnShow(params object[] args)
        {
            UIManager.Inst.HideAllOtherPanelInWindow(this);
            topTabView.SelectWithoutCallback(0);
            SetSelect(0);
            for (int i = 0; i < views.Count; i++)
            {
                EditOperationManager.Inst.TriggerRule(views[i].operationType);
            }
        }

        public override void OnHidden()
        {
            UIManager.Inst.ShowAllOtherPanelInWindow(this);
            for (int i = 0; i < views.Count; i++)
            {
                EditOperationManager.Inst.ReleaseRuleUI(views[i].operationType);
            }
        }

        protected override void OnDestroy()
        {
        }

        public override void OnWindowBeFocused()
        {
        }

        public override void OnWindowPop()
        {
        }

        void SetSelect(int index)
        {
            if (currentSelectIndex == index) return;
            if (currentSelectIndex>=0)
                viewCache[currentSelectIndex].SetActive(false);

            if (!viewCache.ContainsKey(index))
            {
                var viewGo = Instantiate(views[index].view, viewContent);
                viewCache.Add(index, viewGo);
            }
            viewCache[index].SetActive(true);
            currentSelectIndex = index;
        }

        void OnCloseClick()
        {
            UIManager.Inst.ClosePanel(this);
        }

        void OnTabSelect(TabItem item, int index)
        {
            SetSelect(index);
        }
    }
}