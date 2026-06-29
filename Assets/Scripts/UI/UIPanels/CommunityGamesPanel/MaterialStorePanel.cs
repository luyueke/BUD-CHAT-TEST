using System;
using System.Collections.Generic;
using GameData;
using Network;
using Network.Http;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;

namespace Game.CommunityGame
{
    public class MaterialStorePanel : BasePanel<MaterialStorePanel>
    {
        public CommunityGameForYouGridAdapter adapter;
        private Transform _sectionContent;
        private GameObject _sectionItemPrefab;
        private BaseSectionInfoPanel _sectionInfoPanel;
        private GameObject _emptyView;
        private CButton backBtn;
        private List<SectionItem> _sectionItems = new List<SectionItem>();
        private string _curSectionId = "null";
        private Action updateList;
        private bool isUpdateMatList = false;
        private  SearchInputView input_search;
        // private CurrencyPicker _currencyPicker;
        // private CurrencyType CurrencyType = CurrencyType.None;
        public override void OnCreate()
        {
            base.OnCreate();
            BindUI();
            InitSectionPanel();

            backBtn.onClick.AddListener(OnBackClick);
            adapter.InteractiveCallback = () =>
            {
                isUpdateMatList = true;
            };

            // _currencyPicker.SetCallback(currencyType =>
            // {
            //     if (_sectionInfoPanel == null)
            //     {
            //         return;
            //     }
            //     _sectionInfoPanel.OnSelectSection(_curSectionId, (int)currencyType,OnGetDatas, OnHasDatas) ;
            // });
        }

        private void OnBackClick()
        {
            if (isUpdateMatList)
            {
                updateList?.Invoke();
            }
            CloseSelf();
        }

        public void AddRefreshMaterialList(Action act)
        {
            updateList = act;
        }

        private void BindUI()
        {
            _sectionContent = GameObjectEx.FindChildByName(this.transform, "SectionContent");
            backBtn = GameObjectEx.FindComponentByName<CButton>(this.transform, "BackButton");
            _sectionItemPrefab = GameObjectEx.FindChildByName(this.transform, "SectionItemPrefab").gameObject;
            _sectionInfoPanel =  GameObjectEx.FindChildByName(this.transform, "SectionInfoPanel").GetComponent<BaseSectionInfoPanel>();
            _emptyView = GameObjectEx.FindChildByName(this.transform, "emptyViewBig").gameObject;
            // _currencyPicker = GameObjectEx.FindChildByName(this.transform, "CurrencySelectRoot").GetComponent<CurrencyPicker>();

            var searchGo = GameObjectEx.FindChildByName(transform, "input_search");
            if (searchGo != null)
            {
                input_search = searchGo.GetComponent<SearchInputView>();
                input_search.SetOnConfirm(SearchByKeyword);
                input_search.SetOnClear(OnSearchClear);
            }
        }

        private void SearchByKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                OnSearchClear();
                return;
            }

            JObject jb = new JObject
            {
                ["searchWord"] = keyword,
                ["searchScope"] = 0
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SearchMaterial, HttpMethod.GET, JsonConvert.SerializeObject(jb), (content) =>
            {
                var rsp = JsonConvert.DeserializeObject<SearchRsp>(content);
                var list = rsp?.list ?? new List<RecommendItemData>();
                _sectionInfoPanel.gameObject.SetActive(true);
                _sectionInfoPanel.OnGetFirstPageDatas(list);
                OnHasDatas(list.Count > 0);
            }, (error) =>
            {
                LoggerUtils.LogError("MaterialStorePanel SearchByKeyword fail: " + error);
            });
        }

        private void OnSearchClear()
        {
            string savedId = _curSectionId;
            _curSectionId = "";
            ShowCurRightPanel(savedId);
        }

        private class SearchRsp
        {
            public List<RecommendItemData> list;
        }

        private void InitSectionPanel()
        {
            JObject req = new JObject()
            {
                ["ugcType"] = (int)UgcType.Material,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.sectionList, HttpMethod.GET, JsonConvert.SerializeObject(req), OnGetSectionSuccess, OnGetSectionFail);
        }

        private void OnGetSectionSuccess(string content)
        {
            LoggerUtils.Log("OnGetSectionSuccess content = ", content);
            SectionListRsp sectionListRsp = JsonConvert.DeserializeObject<SectionListRsp>(content);
            if (sectionListRsp == null || sectionListRsp.list == null || sectionListRsp.list.Count == 0)
            {
                OnHasDatas(false);
                return;
            }

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

            _sectionInfoPanel.gameObject.SetActive(false);
            _curSectionId = sectionId;
            _sectionInfoPanel.OnSelectSection(_curSectionId, (int)CurrencyType.None,OnGetDatas, OnHasDatas) ;
            // _currencyPicker?.SetDefault();
        }

        private void OnGetDatas()
        {
            if (gameObject == null) {
                return;
            }
            _sectionInfoPanel.gameObject.SetActive(true);
        }

        private void OnHasDatas(bool hasData)
        {
            if (gameObject == null) {
                return;
            }
            _emptyView.gameObject.SetActive(!hasData);
        }
    }
}
