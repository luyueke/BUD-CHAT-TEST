using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CommunityGame
{
    public class CommunityGamesPanel : BasePanel<CommunityGamesPanel>
    {
        public CButton CreatBtn;

        public CButton CloseBtn;

        public CButton searchBtn;

        public CommunityGamesSearchView searchView;

        public CommunityGameSpotlightPanel GamePanel;

        public CommunityGameForYouPanel CommunityPanel;

        public Toggle GameTog;

        public Toggle CommunityTog;
        public Toggle ShowCollectTog;

        public List<Toggle> GameTogList;

        public Transform GameTogListParent;

        public override void OnCreate()
        {
            base.OnCreate();
            CreatBtn.onClick.AddListener(OnCreatButtonClick);
            CloseBtn.onClick.AddListener(CloseSelf);
            searchBtn.onClick.AddListener(OnSearchButtonClick);

            GameTog.onValueChanged.AddListener(OnGameTogClick);

            CommunityTog.onValueChanged.AddListener(OnCommunityTogClick);
            ShowCollectTog.onValueChanged.AddListener(OnShowCollectTogClick);

            for (int i = 0; i < GameTogList.Count; i++)
            {
                var idx = i;
                GameTogList[i].onValueChanged.AddListener((succ) => { OnGameTogListClick(idx,succ); });
            }

            searchView.Init();
            searchView.Hide();
        }
        
        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            ReportOncePerDay_CommunityGamesPanel();

            GameTog.isOn = true;
        }

        private void ReportOncePerDay_CommunityGamesPanel()
        {
            string key = AccountDataManager.Inst.Uid + "_" + "CommunityGamesPanel";
            int lastReportDay = PlayerPrefs.GetInt(key, 0);
            int currentDay = System.DateTime.Now.DayOfYear;
            int currentYear = System.DateTime.Now.Year;
            int todayIdentifier = currentYear * 1000 + currentDay;
            if (lastReportDay == todayIdentifier)
            {
                LoggerUtils.Log("ReportOncePerDay_CommunityGamesPanel: 今天已经上报过，跳过");
                return;
            }
            PlayerPrefs.SetInt(key, todayIdentifier);
            PlayerPrefs.Save();
            Dictionary<string, object> trackData = new Dictionary<string, object>();
            AnalyticsManager.Inst.Track(AnalyticsEventName.UGCGameHall, trackData);
            LoggerUtils.Log("ReportOncePerDay_CommunityGamesPanel: 上报成功，日期标识: " + todayIdentifier);

        }

        //private void InitUI()
        //{
        //    if (_transBG == null)
        //    {
        //        return;
        //    }
        //    
        //    string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        //    var itemObj = Loader
        //        .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
        //        .Instantiate(_transBG);
        //    var item = itemObj.GetComponent<ActivityCenterBgItem>();
        //    item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        //    {
        //        "store_icon4","store_icon5","store_icon6"
        //    });
        //    item.gameObject.SetActive(true);
        //}
        //private void InitTopBar()
        //{
        //    _navigationBarTabs.AddBackBtnClickListener(OnBtnBackClick);
        //    foreach (var config in _topBarConfigs)
        //    {
        //        _navigationBarTabs.CreateItem(config.type.ToString(), config.name).SetIsSelect(false);
        //    }
        //    
        //    _navigationBarTabs.AddItemSelectCallBack(OnTopBarItemClick);
        //    _navigationBarTabs.SetSelect(0);
        //}
        private void OnCreatButtonClick()
        {
            UIManager.Inst.OpenPanel(PanelId.GameStudioPanel);
        }
        private void OnSearchButtonClick()
        {
            searchView.Show();
        }

        private void OnGameTogClick(bool bo) {
            if (bo)
            {
                GameTogListParent.gameObject.SetActive(true);
                GamePanel.gameObject.SetActive(true);
                for (int i = 0; i < GameTogList.Count; i++)
                {
                    if (GameTogList[i].isOn)
                    {
                        return;
                    }
                }
                if (GameTogList[0].gameObject.activeSelf)
                {
                    GameTogList[0].isOn = true;
                }
            }
            else
            {
                GameTogListParent.gameObject.SetActive(false);
                GamePanel.gameObject.SetActive(false);
            }
        }
        private void OnCommunityTogClick(bool bo)
        {
            if (bo)
            {
                CommunityPanel.gameObject.SetActive(true);
            }
            else if (!ShowCollectTog.isOn)
            {
                // ShowCollectTog 还亮着的时候不要关闭 CommunityPanel
                CommunityPanel.gameObject.SetActive(false);
            }
        }

        private void OnShowCollectTogClick(bool bo)
        {
            if (CommunityPanel != null)
            {
                // ShowCollectTog 可能和 CommunityTog 同组导致 CommunityPanel 被关掉，这里兜底保活
                if (bo) CommunityPanel.gameObject.SetActive(true);
                CommunityPanel.ShowCollectPanel(bo);
            }
        }
        private void OnGameTogListClick(int idx,bool bo)
        {
            if (bo)
            {
                GamePanel.SelectSection(idx);
            }
        }

        public void RefreshTog(List<OfficalRecommendItem> items) 
        {
            for (int i = 0; i < GameTogList.Count; i++)
            {
                if (items != null && i < items.Count)
                {
                    GameTogList[i].gameObject.SetActive(true);
                    var txt = GameTogList[i].transform.GetChild(0).GetComponent<Text>();
                    var txt2 = GameTogList[i].transform.GetChild(1).GetComponent<Text>();
                    txt.text = items[i].sectionName;
                    txt2.text = txt.text;
                }
                else
                {
                    GameTogList[i].gameObject.SetActive(false);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(GameTogListParent.parent.transform as RectTransform);

            if (GameTogList[0].gameObject.activeSelf)
            {
                GameTogList[0].isOn = true;
            }
        }

        public void SetTog(int idx) {
            foreach (var item in GameTogList)
            {
                item.SetIsOnWithoutNotify(false);
                item.GetComponent<ToggleShow>().SetOn(false);
            }
            GameTogList[idx].SetIsOnWithoutNotify(true);
            GameTogList[idx].GetComponent<ToggleShow>().SetOn(true);
        }
    }
}