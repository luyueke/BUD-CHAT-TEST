using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.AIGames.AIHospital
{
    /// <summary>
    /// 医院场景季节面板
    /// </summary>
    public class AIHospitalSeasonPanel : BasePanel<AIHospitalSeasonPanel>
    {
        [SerializeField] private CButton _gamePlayBtn;
        [SerializeField] private CButton _activityBtn;
        [SerializeField] private CButton _seasonPassBtn;
        [SerializeField] private Text _seasonPassTips;
        [SerializeField] private CButton _storeBtn;
        [SerializeField] private CButton _closeBtn;

        // 各按钮上的红点对象
        [SerializeField] private GameObject _gamePlayRedDot;
        [SerializeField] private GameObject _activityRedDot;
        [SerializeField] private GameObject _seasonPassRedDot;
        [SerializeField] private GameObject _storeRedDot;

        // 是否已初始化红点
        private bool _isRedDotInitialized = false;
        
        // 红点管理器
        public HospitalSeasonReddotManager _reddotManager;

        public override void OnCreate()
        {
            base.OnCreate();
            AddListener();

            RefreshRedDots(HospitalSeasonRedDotType.GamePlay);
            RefreshRedDots(HospitalSeasonRedDotType.Activity);
            RefreshRedDots(HospitalSeasonRedDotType.SeasonPass);
            RefreshRedDots(HospitalSeasonRedDotType.Store);

            RefreshSeasonPassProgress();
        }

        public override void OnHidden()
        {
            base.OnHidden();
            MessageHelper.RemoveListener<HospitalSeasonRedDotType>(MessageName.OnHospitalSeasonRedDotStateChanged, RefreshRedDots);
        }

        private void AddListener()
        {
            _gamePlayBtn.onClick.AddListener(OnBtnGameClick);
            _activityBtn.onClick.AddListener(OnBtnActivityClick);
            _seasonPassBtn.onClick.AddListener(OnBtnSeasonPassBtnClick);
            _storeBtn.onClick.AddListener(OnBtnStoreClick);
            _closeBtn.onClick.AddListener(CloseSelf);
            
            // 添加红点状态变化的消息监听
            MessageHelper.AddListener<HospitalSeasonRedDotType>(MessageName.OnHospitalSeasonRedDotStateChanged, RefreshRedDots);
        }

        private void RefreshRedDots(HospitalSeasonRedDotType type)
        {
            switch (type)
            {
                case HospitalSeasonRedDotType.GamePlay:
                    var isClickGamePlayBtn = PlayerPrefs.GetInt("HasClickGamePlayBtn"  + AccountDataManager.Inst.Uid, 0);
                    _gamePlayRedDot.SetActive(isClickGamePlayBtn == 0);
                    break;
                
                case HospitalSeasonRedDotType.Activity:
                    // 获取活动红点状态
                    bool hasActivityRedDot = _reddotManager.CheckActivityRedDot(ActivityId.AbandonedHospitalEscape);
                    _activityRedDot.SetActive(hasActivityRedDot);
                    break;
                
                case HospitalSeasonRedDotType.SeasonPass:
                    _reddotManager.GetSeasonPassReddot((hasReddot) =>
                    {
                        _seasonPassRedDot.SetActive(hasReddot);
                    });
                    break;
                
                case HospitalSeasonRedDotType.Store:
                    bool bShowStoreRedDot = AIHospitalStoreManager.Inst.OnCheckRedDotState();
                    var isClickStoreBtn = PlayerPrefs.GetInt("HasClickStoreBtn" + AccountDataManager.Inst.Uid, 0);
                    _storeRedDot.SetActive(bShowStoreRedDot);
                    break;
            }
        }
        
        private void OnBtnGameClick()
        {
            PlayerPrefs.SetInt("HasClickGamePlayBtn" + AccountDataManager.Inst.Uid, 1);
            UIManager.Inst.OpenPanel(PanelId.AIHospitalMainEntryPanel);
            _gamePlayRedDot.SetActive(false);
        }

        private void OnBtnActivityClick()
        {
            UIManager.Inst.OpenPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel,ActivityId.AbandonedHospitalEscape.ToString());
            _activityRedDot.SetActive(false);
        }

        private void OnBtnSeasonPassBtnClick()
        {
            var panel = UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel, SeasonPassType.S9AbandonedHospitalSeasonPass);
            _seasonPassRedDot.SetActive(false);
        }

        private void OnBtnStoreClick()
        {
            PlayerPrefs.SetInt("HasClickStoreBtn" + AccountDataManager.Inst.Uid, 1);
            UIManager.Inst.OpenPanel(PanelId.AIHospitalStorePanel);
            //_storeRedDot.SetActive(false);
        }

        private void RefreshSeasonPassProgress()
        {
            SeasonPassDataManager.Inst.GetSeasonPassList(SeasonPassType.S9AbandonedHospitalSeasonPass, (b, rsp) =>
            {
                if(this == null)
                    return;
                
                var targetLevel = 40;
                if (b && rsp != null)
                {
                    var curDay = SeasonPassDataManager.Inst.GetCurDay(rsp.rewardList);
                    var delt = targetLevel - curDay;
                    if (delt > 0)
                    {
                        this._seasonPassTips.text = $"再升级 {delt} 级可领取";
                    }
                }
            });
        }
    }
}
