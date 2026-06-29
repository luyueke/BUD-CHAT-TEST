using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Event
{
    public class EventCenterPanel : BasePanel<EventCenterPanel>
    {
        private Transform _transBG;
        private Text FirstTitle;
        private Text SubTitle;
        private GameObject _goLoading;
        private NavigationBarTabs _navigationBarTabs;
        private Dictionary<EventCenterTopBarConfig, TabItem> _tabItems = new Dictionary<EventCenterTopBarConfig, TabItem>();
        private Transform _bottomBarParent;
        private List<EventCenterTopBarConfig> _topBarConfigs => EventCenterDataManager.Inst.TopBarConfigs;
        private Dictionary<TASK_ID, EventCenterBottomPanel> _eventPanel = new Dictionary<TASK_ID, EventCenterBottomPanel>();

        public override void OnCreate()
        {
            base.OnCreate();
            _transBG = GameObjectEx.FindChildByName(this.transform, "Trans_BG");
            _navigationBarTabs = GameObjectEx.FindChildByName(this.transform, "NavigationBarTabs").GetComponent<NavigationBarTabs>();
            _bottomBarParent = GameObjectEx.FindChildByName(this.transform, "BottomPanels");
            FirstTitle =  GameObjectEx.FindChildByName(this.transform, "FirstTitle").GetComponent<Text>();
            SubTitle =  GameObjectEx.FindChildByName(this.transform, "SubTitle").GetComponent<Text>();
            InitUI();
            InitTopBar();
            EventCenterDataManager.Inst.AddTaskDataCallBack("EventPanelRedDot", RefreshRedot);
        }
        
        private void InitUI()
        {
            if (_transBG == null)
            {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(_transBG);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            if (item != null)
            {
                item.InitCustomBgItem("#9656FF", atlasPath, new List<string>()
                {
                    "ic_bud",
                });
                item.gameObject.SetActive(true);
            }
        }

        public override void OnHidden()
        {
            base.OnHidden();
            EventCenterDataManager.Inst.RemoveTaskDataCallBack("EventPanelRedDot");
        }

        private void RefreshRedot(TaskListRsp taskListRsp)
        {
            foreach (var tab in _tabItems)
            {
                var tabTaskId = tab.Key.type.ToString();
                var taskInfoData = taskListRsp.list.Find(x => x.taskId == tabTaskId);
                if(taskInfoData == null)
                    continue;
                
                var unClaimData = taskInfoData.eventList.Find(x => x.eventStatus == (int)TaskClaimState.Enable);
                var redot = GameObjectEx.FindChildByName(tab.Value.transform, "Redot").gameObject;
                redot?.SetActive(unClaimData != null);
            }
        }
        
        
        private void InitTopBar()
        {
            _navigationBarTabs.AddBackBtnClickListener(CloseSelf);
            foreach (var config in _topBarConfigs)
            {
                var tabItem = _navigationBarTabs.CreateItem(config.name, config.name);
                tabItem.SetIsSelect(false);
                _tabItems.Add(config, tabItem);
            }

            _navigationBarTabs.AddItemSelectCallBack(OnTopBarItemClick);
            _navigationBarTabs.SetSelect(0);
        }
        
        private void OnTopBarItemClick(TabItem item, int index)
        {
            var data = _topBarConfigs[index];
            foreach (var panel in _eventPanel.Values)
            {
                panel.HidePanel();
            }

            //2.显示对应的Panel
            var curSubPanel = GetEventPanel(data);
            curSubPanel.ShowPanel();
            FirstTitle.SetLocalText(data.firstTitle);
            SubTitle.SetLocalText(data.subTitle);
        }
        
        private EventCenterBottomPanel GetEventPanel(EventCenterTopBarConfig topBarConfig)
        {
            var type = topBarConfig.type;
            var panelPath = topBarConfig.panelPath;
            if (!_eventPanel.ContainsKey(type))
            {
                var panel = Loader.Load<GameObject>(panelPath).Instantiate(_bottomBarParent);
                _eventPanel[type] = panel.GetComponent<EventCenterBottomPanel>();
                _eventPanel[type].InitData(type);
            }

            return _eventPanel[type];
        }
        
        public override void OnWindowBeFocused()
        {
            EventCenterDataManager.Inst.GetTaskInfo();
        }
    }
}
