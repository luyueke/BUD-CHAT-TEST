using System;
using System.Collections.Generic;
using Game.CommunityGame;
using GameData;
using Network;
using Network.Http;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine.UI;
using SectionItem = Game.CommunityGame.SectionItem;
using SectionListRsp = Game.CommunityGame.SectionListRsp;

namespace Game.PropStore
{
    public class PropStorePanel : BasePanel<PropStorePanel>
    {
        private CButton _btnClose;
        private CButton _searchBtn;
        private CButton _searchSectionBtn;
        private Transform _transBG;
        private Text tipText;
        private Transform _sectionContent;
        private GameObject _sectionItemPrefab;
        private SearchView _sectionSearchRoot;
        private PropStoreSectionInfoPanel _sectionInfoPanel;
        private PropStoreSectionInfoPanel _searchInfoPanel;
        private PropStoreDetailView _propStoreDetailView;
        private List<SectionItem> _sectionItems = new List<SectionItem>();
        private string _curSectionId = "null";
        private AmbientLightSetting _srcLightSetting;
        private bool _srcHallLightVisible;
        private bool _srcGameSceneLightVisible;
        private bool _srcPreviewSceneLightVisible;
        // private CurrencyPicker _currencyPicker;
        // private CurrencyType CurrencyType = CurrencyType.None;

        public override void OnCreate()
        {
            base.OnCreate();
            BindUI();
            //初始化Section栏目
            InitSectionPanel();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            InputReceiver.Inst.enabled = false;
            _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
            _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
            _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
            _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();
        }

        public override void OnHidden()
        {
            base.OnHidden();
            InputReceiver.Inst.enabled = true;
            AmbientLightManager.Inst.CloseUILight(_srcLightSetting);
            AmbientLightManager.Inst.RevertHallLight(_srcHallLightVisible);
            AmbientLightManager.Inst.RevertGameSceneLight(_srcGameSceneLightVisible);
            AmbientLightManager.Inst.RevertPreviewLight(_srcPreviewSceneLightVisible);
        }

        private void BindUI()
        {
            _btnClose = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
            _searchBtn = GameObjectEx.FindChildByName(this.transform, "SearchButton").GetComponent<CButton>();
            _searchSectionBtn = GameObjectEx.FindChildByName(this.transform, "SearchSectionButton").GetComponent<CButton>();
            _sectionSearchRoot = GameObjectEx.FindChildByName(this.transform, "SearchRoot").GetComponent<SearchView>();
            _transBG = GameObjectEx.FindChildByName(this.transform, "BG");
            tipText= GameObjectEx.FindComponentByName<Text>(this.transform, "SearchText");
            _sectionContent = GameObjectEx.FindChildByName(this.transform, "SectionContent");
            _sectionItemPrefab = GameObjectEx.FindChildByName(this.transform, "SectionItemPrefab").gameObject;
            _sectionInfoPanel =  GameObjectEx.FindChildByName(this.transform, "SectionInfoPanel").GetComponent<PropStoreSectionInfoPanel>();
            _searchInfoPanel =  GameObjectEx.FindChildByName(this.transform, "SearchInfoPanel").GetComponent<PropStoreSectionInfoPanel>();
            _propStoreDetailView = GameObjectEx.FindChildByName(this.transform, "DetailView").GetComponent<PropStoreDetailView>();
            // _currencyPicker = GameObjectEx.FindChildByName(this.transform, "CurrencySelectRoot").GetComponent<CurrencyPicker>();
            InitUI();
            _btnClose.onClick.AddListener(() =>
            {
                CloseSelf();
            });
            _searchBtn.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel,SearchPanel.SearchType.Prop);
            });
            _searchSectionBtn.onClick.AddListener(OnSearchListBtnClick);
            _sectionInfoPanel.SetOnSelectedAct(OnSelectPropStoreItem);
            _searchInfoPanel.SetOnSelectedAct(OnSelectPropStoreItem);
            // _currencyPicker.SetCallback(currencyType =>
            // {
            //     if (_sectionInfoPanel == null)
            //     {
            //         return;
            //     }
            //     _sectionInfoPanel.OnSelectSection(_curSectionId, (int)currencyType,OnGetDatas);
            // });
        }
        
        public void SetBuyUpdate(Action buyUpdate)
        {
            if (_propStoreDetailView != null && _propStoreDetailView.PurchaseButton != null)
            {
                _propStoreDetailView.PurchaseButton.SetBuyUpdate(buyUpdate);
            }
        }
        
        public void SetBuySuccessUpdate(Action buySuccess)
        {
            if (_propStoreDetailView != null && _propStoreDetailView.PurchaseButton != null)
            {
                _propStoreDetailView.PurchaseButton.SetBuyAction(buySuccess);
            }
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
            item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
            {
                "store_icon4","store_icon5","store_icon6"
            });
            item.gameObject.SetActive(true);
        }

        private void InitSectionPanel()
        {
            JObject req = new JObject()
            {
                ["ugcType"] = (int)UgcType.Prop
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.sectionList, HttpMethod.GET, JsonConvert.SerializeObject(req), OnGetSectionSuccess, OnGetSectionFail);
        }

        private void OnGetSectionSuccess(string content)
        {
            LoggerUtils.Log("OnGetSectionSuccess content = ", content);
            SectionListRsp sectionListRsp = JsonConvert.DeserializeObject<SectionListRsp>(content);
            if (sectionListRsp == null || sectionListRsp.list == null || sectionListRsp.list.Count == 0)
                return;

            for (int i = 0; i < sectionListRsp.list.Count; i++)
            {
                var sectionItemItem = GameObject.Instantiate(_sectionItemPrefab, _sectionContent).GetComponent<SectionItem>();
                var curData = sectionListRsp.list[i];
                sectionItemItem.InitItem(curData, i, OnSectionItemClick);
                _sectionItems.Add(sectionItemItem);
                sectionItemItem.gameObject.SetActive(true);
            }
            _sectionItems[0].Tog.isOn = true;
        }

        private void OnGetSectionFail(string error)
        {
            LoggerUtils.LogError("OnGetSection error = " + error);
        }

        private void OnSectionItemClick(string sectionId)
        {
            ShowCurRightPanel(sectionId);
        }

        private void ShowCurRightPanel(string sectionId)
        {
            if (_curSectionId == sectionId)
                return;

            _curSectionId = sectionId;
            _sectionInfoPanel.OnSelectSection(_curSectionId, (int)CurrencyType.None,OnGetDatas);
            // _currencyPicker?.SetDefault();
        }

        private void OnGetDatas()
        {
        }

        private void OnSelectPropStoreItem(RecommendItemData data)
        {
            _propStoreDetailView.RefreshUIByData(data);
        }

        private void OnSearchListBtnClick()
        {
            _sectionContent.gameObject.SetActive(false);
            _searchInfoPanel.gameObject.SetActive(true);
            _sectionInfoPanel.gameObject.SetActive(false);
            _searchInfoPanel.ResetAdpater();
            _sectionSearchRoot.SetSearchAction(OnSearchAction, OnClearAction, OnCancelAction);
            _sectionSearchRoot.Show();
        }
        internal void OnSearchAction(string str)
        {
            if (string.IsNullOrEmpty(str))
                return;
            _searchInfoPanel.OnSelectSection(str, (int)CurrencyType.None,OnGetDatas,OnHasSearchDatas);

            ShowTip("搜索中");
            // UI.assetsList.Data.ResetItems(0);
            //
            // ugcTuple.SearchKey = str;
            // var nextAction = dataHandler.SearchGoodsData(classSelected.Id, str, OnSearchItemDataChange);
            // UI.assetsList.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
            // UI.assetsList.PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(() => nextAction?.Invoke());
        }

        private void OnHasSearchDatas(bool hasDatas)
        {
            ShowTip(hasDatas?"":"没有找到相关内容");
        }
        internal void OnClearAction()
        {
            _searchInfoPanel.ResetAdpater();
            // ugcTuple.SearchKey = null;
            ShowTip("");
            // UI.assetsList.Data.ResetItems(0);
        }

        internal void OnCancelAction()
        {
            _sectionContent.gameObject.SetActive(true);
            _searchInfoPanel.gameObject.SetActive(false);
            _sectionInfoPanel.gameObject.SetActive(true);
        }
        public void ShowTip(string str)
        {
            tipText.SetLocalText(str);
        }

    }
}

