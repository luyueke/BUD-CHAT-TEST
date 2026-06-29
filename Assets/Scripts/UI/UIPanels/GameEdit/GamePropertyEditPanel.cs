using System.Collections;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using Game.ECS;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Basic.Extensions;
using UI.EditOperation;
using Game.Base;
using GameData.Manager;

namespace UI.UIPanels.GameEdit
{
    public class GamePropertyEditPanel : BasePanel<GamePropertyEditPanel>,IPropertyPanel
    {
        [SerializeField]private CButton closeBtn;
        [SerializeField]private CButton optionBtn; // 选项
        [SerializeField]private CButton settingBtn; // 设置
        [SerializeField]private PropEditTabItem tabTmpl; // 选项卡的按钮模版
        [SerializeField]private Transform tabContentTF; // 选项卡的父级布局
        [SerializeField]private Transform viewContentTF;
        [SerializeField] private CanvasGroup canvasGroup;

        List<PropEditTabItem> tabItems = new List<PropEditTabItem>();
        List<BasePropertyEditSubView> views = new List<BasePropertyEditSubView>();
        int curSelectIndex = 0;

        public override void OnCreate()
        {
            closeBtn.onClick.AddListener(OnCloseClick);
            optionBtn.onClick.AddListener(OnOptionClick);
            settingBtn.onClick.AddListener(OnSettingClick);
            settingBtn.gameObject.SetActive(false);
            optionBtn.gameObject.SetActive(false);
        }

        public override void OnShow(params object[] args)
        {
            // 编辑器不一样 UI有点不一样
            switch (GameController.GetEnterGameModel())
            {
                case GameData.EnterGameModel.UgcSkinEmpty:
                case GameData.EnterGameModel.UgcSkinContinueEdit:
                case GameData.EnterGameModel.UgcPropEmpty:
                case GameData.EnterGameModel.UgcPropContinueEdit:
                case GameData.EnterGameModel.UgcMusicalInstrumentEmpty:
                case GameData.EnterGameModel.UgcMusicalInstrumentContinueEdit:
                case GameData.EnterGameModel.UgcVehicleEmpty:
                case GameData.EnterGameModel.UgcVehicleEmptyContinueEdit:
                    var optionBtnRTF = optionBtn.transform as  RectTransform;
                    optionBtnRTF.anchoredPosition = optionBtnRTF.anchoredPosition - new Vector2(150, 0);
                    break;
                default:
                    EditOperationManager.Inst.BindRuleUI(OperationType.Properties, settingBtn.gameObject);
                    break;
            }

            // 延迟一帧执行
            this.SetFrameCallBack(1, () => {
                FoldContent(GameDataManager.Inst.editorSessionData.GamePropertyEditPanelFoldState);
                canvasGroup.alpha = 1;
            });


        }

        public override void OnHidden()
        {
            EditOperationManager.Inst.ReleaseRuleUI(OperationType.Properties);
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

        public void SetSelectEntity(SceneEntity entity)
        {
            foreach (var subView in views)
            {
                subView.SetSelectEntity(entity);
            }
        }

        /// <summary>
        /// 需要选中的Tab
        /// </summary>
        public void SelectTabItem(int index)
        {
            tabItems[index].SetIsSelectWithoutCallback(true);
            OnTabChange(index);
        }

        public void SelectTabItem<K>() where K : BasePropertyEditSubView
        {
            var viewName = typeof(K).Name;
            int selectIndex = -1;
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i].name == viewName)
                {
                    selectIndex = i;
                    break;
                }
            }

            if (selectIndex >= 0)
            {
                SelectTabItem(selectIndex);
            }
        }

        public K AddTabView<K>(string viewName, string tabName) where K : BasePropertyEditSubView
        {
            if (string.IsNullOrEmpty(tabName))
            {
                tabName = "设置";
            }

            viewContentTF.gameObject.SetActive(true);
            var index = tabItems.Count;
            // 初始化View
            var wrapper = Loader.Load<GameObject>($"Assets/Loadable/UI/UIPanel/GameEdit/View/{viewName}.prefab");
            var go = wrapper.Instantiate(viewContentTF);
            var view = go.GetComponent<K>();
            view.Init(this, index);
            view.name = viewName;
            views.Add(view);

            // 刷新一下布局
            var viewLayout = view.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewLayout);
            view.gameObject.SetActive(index == curSelectIndex);

            // 初始化Tab
            var tabItem = GameObject.Instantiate<PropEditTabItem>(tabTmpl, tabContentTF);
            tabItem.SetName(tabName);
            tabItem.AddValueChangeCallListener(isOn => {if(isOn) OnTabChange(index);});
            tabItem.AddFlodChangeCallListener(isExpand => OnTabFlodChange(isExpand, index));
            tabItem.SetIsSelectWithoutCallback(index == curSelectIndex);
            tabItem.SetIsExpandWithoutCallback(view.IsExpand());
            tabItems.Add(tabItem);
            return view;
        }

        public T GetViewAdapter<T>() where T : BasePropertyAdapter
        {
            return GetComponentInChildren<T>(true);
        }

        public T GetSubView<T>() where T : BasePropertyEditSubView
        {
            return GetComponentInChildren<T>(true);
        }

        public T GetSubView<T>(int vIndex) where T : BasePropertyEditSubView
        {
            return (T)views[vIndex];
        }

        /// <summary>
        /// 最小化主界面
        /// </summary>
        void FoldContent(bool bIsFold) {
            GameDataManager.Inst.editorSessionData.GamePropertyEditPanelFoldState = bIsFold;
            if (tabItems.Count > 0)
            {
                viewContentTF.gameObject.SetActive(!bIsFold);
                optionBtn.gameObject.SetActive(bIsFold);
                EditOperationManager.Inst.TriggerRule(OperationType.Properties, !bIsFold);
            } else {
                // 没有添加其他SubView
                viewContentTF.gameObject.SetActive(false);
                optionBtn.gameObject.SetActive(false);
                EditOperationManager.Inst.TriggerRule(OperationType.Properties);
            }

        }

        void OnTabFlodChange(bool isExpand, int index)
        {
            // Tab的展开状态会联动
            // 改变所有的视图展开状态
            GameDataManager.Inst.editorSessionData.GamePropertyEditViewExpandState = isExpand;
            for (int i = 0; i < views.Count; i++)
            {
                views[i].SetIsExpand(isExpand);
                tabItems[i].SetIsExpandWithoutCallback(isExpand);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewContentTF.GetComponent<RectTransform>());
        }

        void OnTabChange(int index)
        {
            if (curSelectIndex == index) return;

            if (curSelectIndex >= 0)
            {
                views[curSelectIndex].gameObject.SetActive(false);
                tabItems[curSelectIndex].SetIsSelectWithoutCallback(false);
            }

            views[index].gameObject.SetActive(true);
            tabItems[index].SetIsSelectWithoutCallback(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewContentTF.GetComponent<RectTransform>());
            curSelectIndex = index;
        }

        void OnCloseClick()
        {
            FoldContent(true);
        }

        void OnOptionClick()
        {
            FoldContent(false);
        }

        void OnSettingClick()
        {
            UIManager.Inst.OpenPanel(PanelId.UniversalPropertyPanel);
        }

        internal void OnViewInitComplete()
        {
            var state = GameDataManager.Inst.editorSessionData.GamePropertyEditViewExpandState;
            if (tabItems.Count != 0)
                tabItems[0].SetIsExpand(state);
        }
    }
}
