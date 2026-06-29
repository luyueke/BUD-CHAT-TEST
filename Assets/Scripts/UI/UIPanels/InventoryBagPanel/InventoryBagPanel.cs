using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Base;
using Game.Props.PropsManagers;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.AssetToolBox
{
    public class InventoryBagPanel : BasePanel<InventoryBagPanel>
    {
        public GameObject NoneTips;
        public CButton _btnClose;
        public Transform _transBG;
        public PullToRefreshBehaviour RefreshCtr;
        public ToolBoxOSAAdapter Adapter;
        public ToolBoxBaseDataLoader DataLoader;
        private List<ToolBoxItemData> allDatas = new List<ToolBoxItemData>();
        private SearchInputView input_search;
        private string _searchKeyword = "";
        private string _searchCookie = "";
        private bool _searchIsEnd = false;
        //private bool canPull = true;

        public override void OnCreate()
        {
            base.OnCreate();
            _btnClose.onClick.AddListener(() =>
            {
                CloseSelf();
            });
            Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/AvatarBg.prefab").Instantiate(_transBG);
            input_search = GetComponentInChildren<SearchInputView>(true);
            BindUI();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            //if(args != null && args.Length >0) //数据通过参数传过来，不需要请求
            //{
            //   var onlyShowDatas = args[0] as LazyList<PropResInfo>;
               
            //    allDatas.Clear();
            //    List<ToolBoxItemData> datas = new List<ToolBoxItemData>();
            //    if (onlyShowDatas != null && onlyShowDatas.Count >0)
            //    {
            //        for(int i=0;i<onlyShowDatas.Count;i++)
            //        {
            //            ToolBoxItemData d = new ToolBoxItemData();
            //            d.interactInfo = onlyShowDatas[i].interactInfo;
            //            d.ugcInfo = onlyShowDatas[i].propInfo;
            //            datas.Add(d);
            //        }         
            //    }
            //    canPull = false;
            //    ShowDatas(datas);
            //}else
            //{
            //    canPull = true;
                GetFirstPageDatas();
            //}
        }

        private void BindUI()
        {
            //需要动态拉取数据必须要做的初始化操作
            RefreshCtr.OnRefreshWithSign.AddListener(OnPullReleased);

            DataLoader.ResetCookie();
            Adapter.OnItemsUpdated.AddListener(RefreshCtr.HideGizmo);
            Adapter.Data = new LazyDataHelper<ToolBoxItemData>(Adapter, CreateNewModel);
            Adapter.Init();
            Adapter.SetOnSelectAct(OnOwnedItemClick);

            if (input_search != null)
            {
                input_search.SetOnConfirm(SearchByKeyword);
                input_search.SetOnClear(GetFirstPageDatas);
            }
        }

        private void GetFirstPageDatas()
        {
            _searchKeyword = "";
            allDatas.Clear();
            NoneTips.SetActive(false);
            DataLoader.ResetCookie();
            DataLoader.GetDatas((datas) =>
            {
                ShowDatas(datas);
            });
        }

        private void SearchByKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                GetFirstPageDatas();
                return;
            }

            _searchKeyword = keyword;
            _searchCookie = "";
            _searchIsEnd = false;
            allDatas.Clear();
            NoneTips.SetActive(false);
            SendSearchRequest(keyword, "");
        }

        private void SendSearchRequest(string keyword, string cookie)
        {
            if (_searchIsEnd) return;

            bool isFirstPage = string.IsNullOrEmpty(cookie);
            JObject jb = new JObject
            {
                ["searchWord"] = keyword,
                ["searchScope"] = 1,
                ["cookie"] = cookie
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
                    ShowDatas(rsp?.list);
                else
                    OnReceivedNewModelsForInsert(rsp?.list);
            }, (error) =>
            {
                LoggerUtils.LogError("InventoryBagPanel SearchByKeyword fail: " + error);
            });
        }

        private void ShowDatas(List<ToolBoxItemData> datas)
        {
            if (datas == null || datas.Count == 0)
            {
                NoneTips.SetActive(true);
                return;
            }

            allDatas.AddRange(datas);
            Adapter.Data.ResetItems(allDatas.Count, false);
            Adapter.OnItemsUpdated?.Invoke();
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

            Adapter.Data.List.AddRange(newDatas);
            Adapter.Refresh(false);
        }

        private void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                if (!string.IsNullOrEmpty(_searchKeyword))
                    SendSearchRequest(_searchKeyword, _searchCookie);
                else
                    DataLoader.GetDatas(OnReceivedNewModelsForInsert);
            }
        }
        
        private void OnOwnedItemClick(ToolBoxItemData itemData)
        {
            CreateUgcAsset(itemData);
        }
        
        private void CreateUgcAsset(ToolBoxItemData itemData)
        {
            if (itemData != null && itemData.ugcInfo != null)
            {
                GlobalNodeManager.Inst.Get<PropManager>().Create(itemData.ugcInfo, behaviour =>
                {
                    if (behaviour != null)
                    {
                        UI.Manager.InputHandlerManager.Inst.SelectEntity(behaviour.entity);
                    }
                    CloseSelf();
                });
            }
            else
            {
                LoggerUtils.LogError("itemData 类型不对或为null:");
            }
        }
    }
}
