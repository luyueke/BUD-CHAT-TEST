using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;


namespace UI.UIPanels.CreaterRewardPanel
{
    public class CreatorRewardPanel : BasePanel<CreatorRewardPanel>
    {
        [SerializeField] internal Transform transBg;
        [SerializeField] internal NavigationBarTabs _navigationBarTabs;
        [SerializeField] internal CreatorRewardView _creatorRewardView;
        [SerializeField] internal PointsIncomeView _pointsIncomeView;
        [SerializeField] internal TaskBoardView _taskBoardView;

        private List<CreatorTopConfig> _topBarConfigs = new()
        {
            new()
            {
                tapId = 0,
                name = "创作者币收益",
            },
            new()
            {
                tapId = 1,
                name = "积分收益",
            },
            new()
            {
                tapId = 2,
                name = "任务板",
            },
        };

        private Dictionary<CreatorTopConfig, TabItem> _tabItems = new Dictionary<CreatorTopConfig, TabItem>();

        private int curSelect = 0;


        public override void OnShow(params object[] args)
        {
            InitUI();
            HideAll();
            InitTopBar();
            _creatorRewardView?.OnInitCreated(isBack => { CloseSelf(); },
                isClick =>
                {
                    // _navigationBarTabs.gameObject.SetActive(!isClick);
                });
        }

        private void InitUI()
        {
            if (transBg == null)
            {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(transBg);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#895ef6", atlasPath, new List<string>()
            {
                "AvatarBg_icon1", "AvatarBg_icon5", "AvatarBg_icon3", "AvatarBg_icon4", "AvatarBg_icon2"
            });
            item.gameObject.SetActive(true);

            item.GetComponent<ColorBgPanel>().SetImagesColor(DataUtil.DeSerializeColorCheckHash("#D0C1FF"));
        }

        public void Refresh()
        {
        }

        private void InitTopBar()
        {
            for (int i = 0; i < _topBarConfigs.Count; i++)
            {
                var tabItem = _navigationBarTabs.CreateItem(_topBarConfigs[i].name, _topBarConfigs[i].name);
                tabItem.SetIsSelect(false);
                if (i == 1)
                {
                    tabItem.SetIsLock(!ContestDataManager.Inst.GetEnablePublishByGem());
                }
                else if (i == 2)
                {
                    // int creatorNum =
                    //     ReddotManagerUtils.Inst.GetRedDotCount(ReddotManagerUtils.ReddotType.CreatorCenter);
                    // var reddot = GameObjectEx.FindChildByName(tabItem.transform, "Redot").gameObject;
                    // reddot.gameObject.SetActive(creatorNum > 0);
                }

                _tabItems.Add(_topBarConfigs[i], tabItem);
            }


            _navigationBarTabs.AddItemSelectCallBack(OnTopBarItemClick);
            _navigationBarTabs.SetSelect(0);
        }

        private void OnTopBarItemClick(TabItem item, int index)
        {
            if (index == 1)
            {
                if (!ContestDataManager.Inst.GetEnablePublishByGem())
                {
                    TipPanel.ShowToast("粉丝数到达200可解锁");
                    _navigationBarTabs.SetSelect(curSelect);
                    return;
                }
            }

            HideAll();
            curSelect = index;
            if (index == 0)
            {
                _creatorRewardView?.gameObject.SetActive(true);
                _creatorRewardView?.OnInitCreated(isBack => { CloseSelf(); },
                    isClick =>
                    {
                        // _navigationBarTabs.gameObject.SetActive(!isClick);
                    });
            }
            else if (index == 1)
            {
                _pointsIncomeView?.gameObject.SetActive(true);
                _pointsIncomeView?.OnInitCreated(_ => { CloseSelf(); });
            }
            else if (index == 2)
            {
                _taskBoardView?.gameObject.SetActive(true);
                _taskBoardView?.OnInitCreated(_ => { CloseSelf(); }, reddot =>
                {
                    var reddotObj = GameObjectEx.FindChildByName(_tabItems[_topBarConfigs[2]].transform, "Redot").gameObject;
                    reddotObj.gameObject.SetActive(reddot);
                });
            }
        }

        private void HideAll()
        {
            _creatorRewardView?.gameObject.SetActive(false);
            _pointsIncomeView?.gameObject.SetActive(false);
            _taskBoardView?.gameObject.SetActive(false);
        }

        public override void OnHidden()
        {
            base.OnHidden();
            _creatorRewardView?.OnHidden();
        }
    }
}
