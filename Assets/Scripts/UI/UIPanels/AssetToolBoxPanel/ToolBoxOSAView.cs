using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Base;
using Game.Props.PropsManagers;
using GameData.MapData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.AssetToolBox
{
    public class ToolBoxOSAView : MonoBehaviour
    {
        public CButton Btn_Bottom;
        public PullToRefreshBehaviour RefreshCtr;
        public ToolBoxOSAAdapter Adapter;
        public ToolBoxBaseDataLoader DataLoader;
        public bool IsInit = false;
        private List<ToolBoxItemData> allDatas = new List<ToolBoxItemData>();
        private List<ToolBoxItemData> _originalDatas = new List<ToolBoxItemData>();
        private Action _bottomBtnAct;
        private string _searchKeyword = "";
        private string _searchCookie = "";
        private bool _searchIsEnd = false;
        private int _searchScope = 0;

        public void Start()
        {
            //需要动态拉取数据必须要做的初始化操作
            RefreshCtr.OnRefreshWithSign.AddListener(OnPullReleased);
            
            ResetData();
            Adapter.Init();

            if(Btn_Bottom)
                Btn_Bottom.onClick.AddListener(OnBottomBtnClick);

            GetFirstPageDatas();
            IsInit = true;
        }

        public void OnEnable()
        {
            GetFirstPageDatas();
        }

        private void ResetData()
        {
            allDatas.Clear();
            _originalDatas.Clear();
            DataLoader.ResetCookie();
            Adapter.OnItemsUpdated.RemoveAllListeners();
            Adapter.OnItemsUpdated.AddListener(RefreshCtr.HideGizmo);
            Adapter.Data = new LazyDataHelper<ToolBoxItemData>(Adapter, CreateNewModel);
        }

        public void SetBottomBtnAct(Action act)
        {
            this._bottomBtnAct = act;
        }

        public void SetOSAItemClickAct(Action<ToolBoxItemData> act)
        {
            Adapter.SetOnSelectAct(act);
        }

        public void GetFirstPageDatas()
        {
            _searchKeyword = "";
            _searchCookie = "";
            _searchIsEnd = false;
            ResetData();
            DataLoader.GetDatas((datas) =>
            {
                allDatas.AddRange(datas);
                _originalDatas.AddRange(datas);
                Adapter.Data.ResetItems(datas.Count, false);
                Adapter.OnItemsUpdated?.Invoke();
            });
        }

        public void StartSearch(string keyword, int searchScope)
        {
            _searchKeyword = keyword;
            _searchScope = searchScope;
            _searchCookie = "";
            _searchIsEnd = false;
            allDatas.Clear();
            _originalDatas.Clear();
            SendSearchRequest(true);
        }

        private void SendSearchRequest(bool isFirstPage)
        {
            if (_searchIsEnd) return;

            JObject jb = new JObject
            {
                ["searchWord"] = _searchKeyword,
                ["searchScope"] = _searchScope,
                ["cookie"] = _searchCookie
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SearchProp, HttpMethod.GET, reqParam, (content) =>
            {
                var rsp = JsonConvert.DeserializeObject<ToolBoxDataRsp>(content);
                if (rsp != null)
                {
                    _searchCookie = rsp.cookie ?? "";
                    _searchIsEnd = rsp.IsEnd == 1;
                }
                if (isFirstPage)
                    ShowSearchResult(rsp?.list);
                else
                    OnReceivedNewModelsForInsert(rsp?.list);
            }, (error) =>
            {
                LoggerUtils.LogError("ToolBoxOSAView SendSearchRequest fail: " + error);
            });
        }

        /// <summary>
        /// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private ToolBoxItemData CreateNewModel(int index)
        {
            if (index >= 0 && index < allDatas.Count)
            {
                return allDatas[index];
            }

            return new ToolBoxItemData();
        }

        private void OnReceivedNewModelsForInsert(List<ToolBoxItemData> newDatas)
        {
            if (newDatas == null || newDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }

            allDatas.AddRange(newDatas);
            _originalDatas.AddRange(newDatas);
            Adapter.Data.List.AddRange(newDatas);
            Adapter.Refresh(false);
        }

        public void ShowSearchResult(List<ToolBoxItemData> datas)
        {
            allDatas.Clear();
            _originalDatas.Clear();
            if (datas != null)
            {
                allDatas.AddRange(datas);
                _originalDatas.AddRange(datas);
            }
            Adapter.Data = new LazyDataHelper<ToolBoxItemData>(Adapter, CreateNewModel);
            Adapter.Data.ResetItems(allDatas.Count, false);
            Adapter.OnItemsUpdated?.Invoke();
        }

        public void FilterByKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                allDatas = new List<ToolBoxItemData>(_originalDatas);
            }
            else
            {
                string lower = keyword.ToLower();
                allDatas = _originalDatas.FindAll(item =>
                {
                    if (item?.ugcInfo == null) return false;
                    var info = item.ugcInfo;
                    return (!string.IsNullOrEmpty(info.designCode) && info.designCode.ToLower().Contains(lower))
                        || (!string.IsNullOrEmpty(info.creator) && info.creator.ToLower().Contains(lower))
                        || (!string.IsNullOrEmpty(info.name) && info.name.ToLower().Contains(lower));
                });
            }
            Adapter.Data = new LazyDataHelper<ToolBoxItemData>(Adapter, CreateNewModel);
            Adapter.Data.ResetItems(allDatas.Count, false);
            Adapter.OnItemsUpdated?.Invoke();
        }

        private void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                if (!string.IsNullOrEmpty(_searchKeyword))
                    SendSearchRequest(false);
                else
                    DataLoader.GetDatas(OnReceivedNewModelsForInsert);
            }
        }

        private void OnBottomBtnClick()
        {
            this._bottomBtnAct?.Invoke();
        }
    }
}
