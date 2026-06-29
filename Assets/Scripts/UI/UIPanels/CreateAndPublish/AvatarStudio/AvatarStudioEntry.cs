using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
public class AvatarStudioEntry : MonoBehaviour
{
    public bool InSelectBundleView;
    public AvatarStudioAdapter adapter;
    private StudioSubType _studioSubType;
    private AvatarSubType _skinSubType = AvatarSubType.Clothes;
    private CurrencyType _currencyType;
    private bool _isEnd;
    private string _cookie;
    private bool _isReqing;//正在请求
    protected CharacterStyle currentStyle;
    protected void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
        refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
        adapter.OnItemsUpdatedAct.AddListener(refreshController.HideGizmo);
    }

    public void ResetAdpater()
    {
        if (adapter != null && adapter.IsInitialized)
        {
            adapter.ResetItems(0);
            adapter.ClearPool();
        }
    }

    public void Init(CharacterStyle style)
    {
        currentStyle = style;
    }
    public void SetActions(Action<DraftListItem> onSelectItemAct,Action uploadCreateAct,  Action isEmptyAction, StudioSubType studioSubType,AvatarSubType skinSubType,CurrencyType currencyType)
    {
        adapter.OnSelectItemAct = onSelectItemAct;
        adapter.UploadCreateAct = uploadCreateAct;
        adapter.EmptyDataAct = isEmptyAction;
        this._studioSubType = studioSubType;
        this._skinSubType = skinSubType;
        this._currencyType = currencyType;
    }

    public void GetFirstPageDatas(Action<List<DraftListItem>> complete)
    {
        ResetCookies();
        GetStudioDatas(datas =>
        {
            //如果是草稿箱-需要增加创建按钮
            if(this._studioSubType == StudioSubType.Drafts)
                datas.Insert(0,null);
            
            complete?.Invoke(datas);
            ResetAdpater();
            if (adapter != null&&adapter.Data!=null) 
            {
                adapter.OnItemsUpdatedAct?.Invoke();
                adapter.Data.ResetItems(datas);
            }
            
        });
    }


    public void OnPullReleased(float sign)
    {
        if (sign < 0)
        {
            GetStudioDatas(OnReceivedNewModelsForInsert);
        }
    }

    void OnReceivedNewModelsForInsert(List<DraftListItem> newModels)
    {
        if (newModels == null || newModels.Count == 0)
        {
            adapter.OnItemsUpdatedAct?.Invoke();
            return;
        }

        adapter.Data.InsertItems(adapter.GetItemsCount(), newModels);
        adapter.OnItemsUpdatedAct?.Invoke();
    }
    public void RemoveSingleItem(string mapId)
    {
        adapter.RemoveSingleItem(mapId);
    }

    public void UpdateSingleItem(DraftListItem draftListItem)
    {
        adapter.UpdateSingleItem(draftListItem);
    }
    private void ResetCookies()
    {
        this._cookie = "";
        this._isEnd = false;
    }
    
    private void GetStudioDatas(UnityAction<List<DraftListItem>> resultAction = null)
    {
        if(_isEnd)
            return;
        
        if(!this)
            return;

        _isReqing = true;
        var req = new AvatarStudioListReq
        {
            cookie = this._cookie,
            uid = AccountDataManager.Inst.Uid,
            subType = (int)_skinSubType,
            skinType = (int)currentStyle,
            currencyType = (int)_currencyType
        };

        var reqHead = _studioSubType == StudioSubType.Drafts ? HttpUrlDefine.clothCreateList : HttpUrlDefine.clothPublishList;
        NetworkManager.Inst.SendHttpRequest(reqHead, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
            {
                MapListResponse mapListResponse = JsonConvert.DeserializeObject<MapListResponse>(content);
                this._isEnd = mapListResponse.isEnd == 1;
                this._cookie = mapListResponse.cookie;
                if (mapListResponse.list == null)
                {
                    mapListResponse.list = new List<DraftListItem>();
                }

                if (InSelectBundleView)
                {
                    List<DraftListItem> NotBundleList = new List<DraftListItem>();
                    mapListResponse.list.ForEach(x =>
                    {
                        if (x.skinInfo != null && x.skinInfo.isPrivateOrder != 1)
                        {
                            NotBundleList.Add(x);
                        }
                    });
                    resultAction?.Invoke(NotBundleList);
                    _isReqing = false;
                }
                else
                {
                    resultAction?.Invoke(mapListResponse.list);
                    _isReqing = false;
                }
            },
            (error) =>
            {
                resultAction?.Invoke(new List<DraftListItem>());
                _isReqing = false;
            });
    }
    
}
