using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using Game.CommunityGame;
using Newtonsoft.Json;
using Network.Http;
using Network;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Es;
using System;
using System.Security.Cryptography;
using GameData.BaseInfo;
using System.Linq;
using Game;
using Game.Event;
using Message;

namespace AIGame.Base
{
    /// <summary>
    /// s9 逃离医院玩法入口
    /// </summary>
    public class AIHospitalMainEntryPanel : BasePanel<AIHospitalMainEntryPanel>
    {
        // Start is called before the first frame update
        [SerializeField] private CButton _backButton;
        [SerializeField] private CButton _searchButton;
        [SerializeField] private CButton _playButton;
        [SerializeField] private CButton _TopListButton;

        [SerializeField] private CButton _playGiftBtn;
        [SerializeField] private CButton _finishGiftBtn;
        [SerializeField] private CButton _storeBtn;
        [SerializeField] private CButton _battlePassBtn;
        [SerializeField] private CButton _activityBtn;

        /// <summary>
        /// 两个礼包倒计时
        /// </summary>
        [SerializeField] private Text _playTimer;
        [SerializeField] private Text _finishTimer;

        [SerializeField] private GameObject _rightPanel_official;
        [SerializeField] private GameObject _rightPanel_recommend;
        [SerializeField] private GameObject _rightPanel_popular;
        [SerializeField] private GameObject _rightPanel_studio;


        [SerializeField] private Transform _sectionContent;
        [SerializeField] private GameObject _sectionItemPrefab;
        [SerializeField] private GameObject _emptyGo;
        [SerializeField] private BaseSectionInfoPanel _sectionInfoPanel;
        private List<SectionItem> _sectionItems = new List<SectionItem>();
        private string _curSectionId = "null";

        [SerializeField] private Transform _reccommendBg;
        [SerializeField] private CommunityGamesSearchView _searchPanel;
        [SerializeField] private GameObject _imgFinish;

        [SerializeField] private Toggle _tog_official;
        [SerializeField] private Toggle _tog_recommend;
        [SerializeField] private Toggle _tog_studio;
        [SerializeField] private Toggle _tog_popular;

        [SerializeField] private AIHospitalPopularListCtrl _popularPanel;

        private AIGamePassStatus _aiGamePassStatus;

        private bool _recommendInit = false;

        //[SerializeField]private CButton _testShare;

        #region DataLogic
        private AIHospitalCommunityTab _curShowType = AIHospitalCommunityTab.MainEntry;
        

        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            InitUI();
            EventCenterDataManager.Inst.ReportTask(PostEventId.ViewEscapeSimulator);
        }

        private void InitUI()
        {
            // 绑定按钮事件
            AddListener();
            _searchPanel.Init(true);
            _searchPanel.Hide();
            InitToggleListener();
            InitRecommendBg();
            InitOfficialModelFinish(PlayerPrefs.GetInt(AccountDataManager.Inst.Uid+"_"+"PlayS9Pgc",0)==1);
            GetUserImageStatus();
            
            InitRedDot();
        }

        private void AddListener()
        {
            _backButton.onClick.AddListener(OnBackButtonClick);
            _searchButton.onClick.AddListener(OnSearchButtonClick);
            _playButton.onClick.AddListener(OnPlayButtonClick);
            _TopListButton.onClick.AddListener(OnClickTopList);
            //_testShare.onClick.AddListener(OnClickTestShareBtn);
            _playGiftBtn.onClick.AddListener(OnClickPlayGiftBtn);
            _finishGiftBtn.onClick.AddListener(OnClickFinishGiftBtn);
            _storeBtn.onClick.AddListener(OnClickStoreBtn);
            _battlePassBtn.onClick.AddListener(OnClickBattlePassBtn);
            _activityBtn.onClick.AddListener(OnClickActivityBtn);

            //MessageHelper.AddListener(MessageName.OnS9UpdateGiftState,InitTopRightBtnList);
            MessageHelper.AddListener(MessageName.OnS9StoreTips, OnStoreRedDotUpdate);
        }

        private void RemoveListener()
        {
            _backButton.onClick.RemoveListener(OnBackButtonClick);
            _searchButton.onClick.RemoveListener(OnSearchButtonClick);
            _playButton.onClick.RemoveListener(OnPlayButtonClick);
            _TopListButton.onClick.RemoveListener(OnClickTopList);
            //_testShare.onClick.RemoveListener(OnClickTestShareBtn); 
            _playGiftBtn.onClick.RemoveListener(OnClickPlayGiftBtn);
            _finishGiftBtn.onClick.RemoveListener(OnClickFinishGiftBtn);
            _storeBtn.onClick.RemoveListener(OnClickStoreBtn);
            _battlePassBtn.onClick.RemoveListener(OnClickBattlePassBtn);
            _activityBtn.onClick.RemoveListener(OnClickActivityBtn);
            
            //MessageHelper.RemoveListener(MessageName.OnS9UpdateGiftState,InitTopRightBtnList);
            MessageHelper.RemoveListener(MessageName.OnS9StoreTips, OnStoreRedDotUpdate);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            

        }


        private void OnClickTopList()
        {
            UIManager.Inst.OpenPanel(PanelId.MapTopListPanel);
        }
        

        private void GetUserImageStatus()
        {
            // 构建请求参数
            JObject req = new JObject()
            {
                ["targetUid"] = AccountDataManager.Inst.Uid,  // 假设需要用户ID
            };

            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.getUserImage,         // 接口地址
                HttpMethod.GET,                     // 请求方法
                JsonConvert.SerializeObject(req),   // 请求参数
                OnGetUserImageSuccess,              // 成功回调
                OnGetUserImageFail                  // 失败回调
            );
        }

        private void OnGetUserImageSuccess(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                OnGetUserImageFail("Empty Response");
                return;
            }

            try
            {
                // 解析返回数据
                var response = JsonConvert.DeserializeObject<GetImageRes>(content);
                if (response != null&& response.aiGameStatus!= null)
                {
                    // 根据返回数据设置状态
                    _aiGamePassStatus = response.aiGameStatus.FirstOrDefault(x => x.gameId == (int)PGCGameType.AIHospital); // 查找gameId为2的数据
                    //InitOfficialModelFinish(_aiGamePassStatus.hasPlayedPgc);
                }
                //else
                    //InitOfficialModelFinish(false);
                //InitTopRightBtnList();
            }
            catch (System.Exception e)
            {
                LoggerUtils.LogError($"Parse user image response failed: {e.Message}");
                //InitOfficialModelFinish(false); // 解析失败时设置默认状态
            }
        }

        private void OnGetUserImageFail(string error)
        {
            LoggerUtils.LogError($"Get user image failed: {error}");
            //InitOfficialModelFinish(false); // 请求失败时设置默认状态
        }

        // 左侧Toggle事件
        private void InitToggleListener()
        {
            _tog_official.onValueChanged.AddListener((isOn) =>
            {
                // 避免重复触发
                if (!_tog_official.interactable) return;
                
                if (isOn)
                {
                    SetDisplayTab(AIHospitalCommunityTab.MainEntry);
                    var toggle = _tog_official.GetComponent<AIHospitalMainEntryToggle>();
                    toggle.SetSelect(true);
                }
                else
                {
                    var toggle = _tog_official.GetComponent<AIHospitalMainEntryToggle>();
                    toggle.SetSelect(false);
                }
            });

            _tog_recommend.onValueChanged.AddListener((isOn) =>
            {
                // 避免重复触发
                if (!_tog_recommend.interactable) return;
                
                if (isOn)
                {
                    SetDisplayTab(AIHospitalCommunityTab.Daily);
                    InitItem();
                    var toggle = _tog_recommend.GetComponent<AIHospitalMainEntryToggle>();
                    toggle.SetSelect(true);
                }
                else
                {
                    var toggle = _tog_recommend.GetComponent<AIHospitalMainEntryToggle>();
                    toggle.SetSelect(false);
                }
            });

            _tog_studio.onValueChanged.AddListener((isOn) =>
            {
                // 避免重复触发
                if (!_tog_studio.interactable) return;
                
                if (isOn)
                {
                    SetDisplayTab(AIHospitalCommunityTab.Studio);
                }
            });

            _tog_popular.onValueChanged.AddListener((isOn) =>
            {
                // 避免重复触发
                if (!_tog_popular.interactable) return;

                if (isOn)
                {
                    SetDisplayTab(AIHospitalCommunityTab.HotSection);
                    _popularPanel.OnSelectSection();
                    var toggle = _tog_popular.GetComponent<AIHospitalMainEntryToggle>();
                    toggle.SetSelect(true);
                }
                else
                {
                    var toggle = _tog_popular.GetComponent<AIHospitalMainEntryToggle>();
                    toggle.SetSelect(false);
                }
            });
        }

        // 添加一个方法来安全地切换Toggle状态
        private void SetToggleState(Toggle toggle, bool value)
        {
            //toggle.interactable = false;
            //LoggerUtils.LogError($"{toggle.name} isOn= {value}");
            toggle.isOn = value;
            //toggle.interactable = true;
        }

        public void InitOfficialModelFinish(bool value)
        {
            _imgFinish.SetActive(value);
            _tog_official.isOn = false;
            _tog_recommend.isOn = false;
            _tog_studio.isOn = false;
            _tog_popular.isOn = false;

            SetToggleState(_tog_official, !value);
            SetToggleState(_tog_recommend, value);
            SetToggleState(_tog_studio, false);
            SetToggleState(_tog_popular, false);
        }


        private void InitRecommendBg()
        {
            if (_reccommendBg == null)
            {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(_reccommendBg);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#433E3C", atlasPath, new List<string>()
            {
                "S9BgElement1", "S9BgElement2", "S9BgElement3"
            });
            item.gameObject.SetActive(true);
            item.transform.SetAsFirstSibling();
        }


        #region

        public void InitItem()
        {
            if (_recommendInit)
            {
                return;
            }
            _recommendInit = true;

            JObject req = new JObject
            {
                ["gameType"] = (int)GameType.AIGame,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.gameSpotlight, HttpMethod.GET, JsonConvert.SerializeObject(req), OnGetDataSuccess,
                OnGetDataFail);
        }

        private void OnGetDataSuccess(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                OnGetDataFail("Empty Rsp");
                return;
            }

            OfficalRecommendRsp rsp = JsonConvert.DeserializeObject<OfficalRecommendRsp>(content);
            if (rsp == null || rsp.sections == null || rsp.sections.Count == 0)
            {
                return;
            }

            // 将 OfficalRecommendRsp 转换为 SectionListRsp
            SectionListRsp sectionListRsp = new SectionListRsp
            {
                list = new List<SectionItemData>()
            };

            // 将 sections 中的数据转换到 list 中
            foreach (var section in rsp.sections)
            {
                sectionListRsp.list.Add(new SectionItemData
                {
                    sectionId = section.sectionId,
                    sectionName = section.sectionName
                });
            }

            // 如果转换后的列表为空，直接返回
            if (sectionListRsp.list.Count == 0)
                return;

            for (int i = 0; i < sectionListRsp.list.Count; i++)
            {
                var sectionItemItem = GameObject.Instantiate(_sectionItemPrefab, _sectionContent)
                    .GetComponent<SectionItem>();
                var curData = sectionListRsp.list[i];
                sectionItemItem.InitItem(curData, i, OnSectionItemClick);
                _sectionItems.Add(sectionItemItem);
                sectionItemItem.gameObject.SetActive(true);
            }

            _sectionItems[0].Tog.isOn = true;
        }

        private void OnGetDataFail(string error)
        {
            _recommendInit = false;
            LoggerUtils.LogError("CommunityGameSpotlightPanel OnGetDataFail " + error);
        }

        private void OnSectionItemClick(string sectionId)
        {
            ShowCurRightPanel(sectionId);
        }

        private void ShowCurRightPanel(string sectionId)
        {
            if (_curSectionId == sectionId)
                return;

            _sectionInfoPanel.gameObject.SetActive(false);
            //_emptyGo.SetActive(true);
            _curSectionId = sectionId;
            _sectionInfoPanel.OnSelectSection(_curSectionId, 0, OnGetDatas);
        }

        private void OnGetDatas()
        {
            //_emptyGo.SetActive(false);
            _sectionInfoPanel.gameObject.SetActive(true);
        }

        #endregion 初始化循环列表相关






        // 返回按钮功能
        private void OnBackButtonClick()
        {
            CloseSelf();
            // 返回上一级界面
            // 可以是关闭当前面板或加载上一个场景
            //gameObject.SetActive(false);
            // 或者
            // SceneManager.LoadScene("PreviousScene");
        }

        // 搜索按钮功能
        private void OnSearchButtonClick()
        {
            SetDisplayTab(AIHospitalCommunityTab.SearchView);
            _searchPanel.Show();
            _searchPanel.RegisterAction(() =>
            {
                if(_aiGamePassStatus != null)
                {
                    SetDisplayTab(_aiGamePassStatus.hasFinPgc ? AIHospitalCommunityTab.Daily : AIHospitalCommunityTab.MainEntry);
                    InitOfficialModelFinish(_aiGamePassStatus.hasFinPgc);
                }
             
            });
            // 显示搜索面板
            //_rightPanel_official.gameObject.SetActive(false);
            //_rightPanel_recommend.gameObject.SetActive(false);
        }

        private void OnClickPlayGiftBtn()
        {
            // 打开礼包界面
            UIManager.Inst.OpenPanel(PanelId.AIHospitalFirstPlayGiftPanel);
        }


        private void OnClickFinishGiftBtn() 
        {
            UIManager.Inst.OpenPanel(PanelId.AIHospitalFirstPassGiftPanel);
        }

        private void OnClickStoreBtn()
        {
            UIManager.Inst.OpenPanel(PanelId.AIHospitalStorePanel);
        }

        private void OnClickBattlePassBtn()
        {
            var panel = UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel, SeasonPassType.S9AbandonedHospitalSeasonPass);
        }

        private void OnClickActivityBtn()
        {
            UIManager.Inst.OpenPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel, ActivityId.AbandonedHospitalEscape.ToString());
        }       

                    
        


        
        // 游玩按钮功能
        private void OnPlayButtonClick()
        {
            //CloseSelf();
            AIHospitalUtils.Inst.EnterOfficalHospitalGame();
            //AIParkUtils.Inst.EnterOfficalParkGame();
        }
        

        #region 控制各个Type的Panel显隐藏

        private void SetDisplayTab(AIHospitalCommunityTab tab)
        {
            this._curShowType = tab;
            _rightPanel_official.SetActive(tab == AIHospitalCommunityTab.MainEntry);
            _rightPanel_recommend.SetActive(tab == AIHospitalCommunityTab.Daily);
            _rightPanel_popular.SetActive(tab == AIHospitalCommunityTab.HotSection);
            _rightPanel_studio.SetActive(tab == AIHospitalCommunityTab.Studio);
            _searchPanel.gameObject.SetActive(tab == AIHospitalCommunityTab.SearchView);
        }
        #endregion

        #region 右上按钮状态控制
        BudTimer _playGiftTimer;
        BudTimer _finishGiftTimer;
        private void InitTopRightBtnList()
        {
            StopTimer();
            bool hasPlayGift = _aiGamePassStatus.playedUGCRewardLeftTime > 0;
            bool hasFinishGift = _aiGamePassStatus.finUGCRewardLeftTime > 0;
            _playGiftBtn.gameObject.SetActive(hasPlayGift);
            _finishGiftBtn.gameObject.SetActive(hasFinishGift);
            if (_aiGamePassStatus.playedUGCRewardLeftTime>0)
            {
                _playGiftTimer = TimerManager.Inst.RunDisposable("_playGiftTimer",1,1,_aiGamePassStatus.playedUGCRewardLeftTime,()=>
                {
                    UpdateTextTime(--_aiGamePassStatus.playedUGCRewardLeftTime,_playTimer, _playGiftBtn.gameObject);
                });
            }
            if (_aiGamePassStatus.finUGCRewardLeftTime>0)
            {
                _finishGiftTimer = TimerManager.Inst.RunDisposable("_finishGiftTimer",1, 1, _aiGamePassStatus.finUGCRewardLeftTime, () =>
                {
                    UpdateTextTime(--_aiGamePassStatus.finUGCRewardLeftTime, _finishTimer,_finishGiftBtn.gameObject);
                });
            }
        }

        private void UpdateTextTime(int remainTime,Text text,GameObject obj)
        {
            //将身下的时间转换为 小时 分钟 
            int hour = remainTime / 3600;
            int minute = (remainTime % 3600) / 60;
            int second = remainTime % 60;
            text.text = string.Format("{0:D2}:{1:D2}:{2:D2}", hour, minute, second);
            if (remainTime<0)
            {
                obj.SetActive(false);
                StopTimer();
            }
        }

        private void StopTimer()
        {
            TimerManager.Inst.Stop(_playGiftTimer);
            TimerManager.Inst.Stop(_finishGiftTimer);
        }

        #endregion


        #region 商店红点
        private void InitRedDot()
        {
            bool bShowStoreRedDot = AIHospitalStoreManager.Inst.OnCheckRedDotState();
            if (bShowStoreRedDot)
            {
                var obj = GameObjectEx.FindChildByName(_storeBtn.gameObject, "RedDot");
                obj.gameObject.SetActive(true);
            }
        }

        private void OnStoreRedDotUpdate()
        {
            var obj = GameObjectEx.FindChildByName(_storeBtn.gameObject, "RedDot");
            obj.gameObject.SetActive(true);
        }

        private void HideStoreRedDot()
        {
            var obj = GameObjectEx.FindChildByName(_storeBtn.gameObject, "RedDot");
            obj.gameObject.SetActive(false);
        }

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // 移除按钮事件监听
            RemoveListener();
            StopTimer();
            // 清理列表数据
            _sectionItems.Clear();
        }

        #region 主界面测试分享按钮

        public void OnClickTestShareBtn()
        {
            if (SunShineNativeShare.instance==null)
            {
                LoggerUtils.LogError("share sdk obj null");   
            }
            string shareContent = "测试s9分享功能";
            LoggerUtils.Log($"分享内容: {shareContent}");
            SunShineNativeShare.instance.ShareText("abc", shareContent);
            TipPanel.ShowToast(shareContent);
        }
        #endregion
    }

    public enum AIHospitalCommunityTab
    {
        MainEntry,
        Daily,
        HotSection,
        SearchView,
        Studio,
    }

}
