using System;
using System.Collections.Generic;
using Game.CommunityGame;
using Game.PropStore;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
using SectionItem = Game.CommunityGame.SectionItem;

namespace Game.AINPCStudio
{
    public class AINpcStorePanelCommunityView : MonoBehaviour
    {
        private CButton _searchSectionBtn;
        private Text tipText;
        private Transform _sectionContent;
        private GameObject _sectionItemPrefab;
        private SearchView _sectionSearchRoot;
        private AINpcStoreSectionInfoPanel _sectionInfoPanel;
        private AINpcStoreSectionInfoPanel _searchInfoPanel;
        private List<SectionItem> _sectionItems = new List<SectionItem>();
        private string _curSectionId = "null";
        private NpcStoreEnterType _curEnterType = NpcStoreEnterType.Store;

        public void InitEnterMode(NpcStoreEnterType enterType)
        {
            this._curEnterType = enterType;
            Init();
            RefreshData();
        }

        private void Init()
        {
            _searchSectionBtn = GameObjectEx.FindChildByName(this.transform, "SearchSectionButton").GetComponent<CButton>();
            _sectionSearchRoot = GameObjectEx.FindChildByName(this.transform, "SearchRoot").GetComponent<SearchView>();
            tipText = GameObjectEx.FindComponentByName<Text>(this.transform, "SearchText");
            _sectionContent = GameObjectEx.FindChildByName(this.transform, "SectionContent");
            _sectionItemPrefab = GameObjectEx.FindChildByName(this.transform, "SectionItemPrefab").gameObject;
            _sectionInfoPanel = GameObjectEx.FindChildByName(this.transform, "SectionInfoPanel").GetComponent<AINpcStoreSectionInfoPanel>();
            _searchInfoPanel = GameObjectEx.FindChildByName(this.transform, "SearchInfoPanel").GetComponent<AINpcStoreSectionInfoPanel>();
            _searchSectionBtn.onClick.AddListener(OnSearchListBtnClick);
        }

        public void RefreshData()
        {
            //初始化Section栏目
            InitSectionPanel();
        }

        private void InitSectionPanel()
        {
            JObject req = new JObject()
            {
                ["ugcType"] = (int)UgcType.AINpc
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
                var sectionItemItem = GameObject.Instantiate(_sectionItemPrefab, _sectionContent)
                    .GetComponent<SectionItem>();
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
            _sectionInfoPanel.OnSelectSection(_curSectionId, (int)CurrencyType.None, OnGetDatas);
        }

        private void OnGetDatas()
        {
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
            _searchInfoPanel.OnSelectSection(str, (int)CurrencyType.None, OnGetDatas, OnHasSearchDatas);

            ShowTip("搜索中");
        }

        private void OnHasSearchDatas(bool hasDatas)
        {
            ShowTip(hasDatas ? "" : "没有找到相关内容");
        }

        internal void OnClearAction()
        {
            _searchInfoPanel.ResetAdpater();
            ShowTip("");
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