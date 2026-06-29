using System;
using System.Collections.Generic;
using System.Linq;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Avatar;
using Game.Store;
using Network.Http;
using UI.Preview3D.Base;
using UnityEngine.UI;
using xasset;

public class LeaderboardWidget : MonoBehaviour
{
    public LeaderboardAdapter Adapter;
    public PullToRefreshBehaviour refreshController;
    public LeaderboardDataLoader DataLoader;
    public LeaderboardItem SelfLeaderboardItem;
    public RawImage RImg_Preview;
    
    private PlayerPodiumController podiumController;

    private RankType _curRankType = RankType.YandereTheBest;
    private bool _isSelfInit;
    private string _leaderId;
    
    private void Start()
    {
        InitPodium();
        
        //需要动态拉取数据必须要做的初始化操作
        refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
        Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        Adapter.Data = new SimpleDataHelper<LeaderboardItemData>(Adapter);
        Adapter.Init();
    }
    
    private void InitPodium()
    {
        var asset = Asset.Load(Preview3DConstant.PATH_PODIUM_PREVIEW_MODEL, typeof(GameObject));
        GameObject prefab = asset.asset as GameObject;
        GameObject podiumInst = UnityEngine.Object.Instantiate(prefab, this.transform);
        podiumController = podiumInst.GetComponent<PlayerPodiumController>();
        RImg_Preview.texture = podiumController.viewCamera.targetTexture;
    }

    public void GetData(RankType rankType)
    {
        _curRankType = rankType;
        _isSelfInit = false;
        HideGizmo();

        Adapter.SetRankType(rankType);
        DataLoader.SetLeaderboardType((int)rankType);
        DataLoader.GetLeaderboardData(OnGetFirstPageDatas, OnGetSelfData);
    }
    
    public void ResetAdpater()
    {
        if(!Adapter.IsInitialized)
            return;

        Adapter.ResetItems(0);
        Adapter.ClearPool();
    }
    
    private void HideGizmo()
    {
        //需要动态拉取数据必须要做的初始化操作
        refreshController.HideGizmo();
    }
    
    public virtual void OnGetFirstPageDatas(List<LeaderboardItemData> leaderboardItemDatas)
    {
        ResetAdpater();
        if (leaderboardItemDatas == null || leaderboardItemDatas.Count == 0)
        {
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }
        Adapter.Data.ResetItems(leaderboardItemDatas);
        Adapter.OnItemsUpdated?.Invoke();

        var playerUidList = leaderboardItemDatas.Select(x => x.userInfo.uid).ToList();
        podiumController.ShowPlayers(playerUidList);
    }
        
    private void OnPullReleased()
    {
        DataLoader.GetLeaderboardData(OnReceivedNewModelsForInsert);
    }
        
    private void OnReceivedNewModelsForInsert(List<LeaderboardItemData> leaderboardItemDatas)
    {
        if (leaderboardItemDatas == null || leaderboardItemDatas.Count == 0)
        {
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }
        Adapter.Data.List.AddRange(leaderboardItemDatas);
        Adapter.Refresh(false);
    }

    private void OnGetSelfData(LeaderboardItemData data)
    {
        if(data == null)
            return;
        
        if(_isSelfInit)
            return;
        
        SelfLeaderboardItem.InitData(_curRankType, data);
        _isSelfInit = true;
    }
}

public class LeaderboardDataRsp : HttpPageBaseData
{
    public List<LeaderboardItemData> rankList;
    public LeaderboardItemData userRank;
} 

public class LeaderboardItemData
{
    public int rank;
    public int score;
    public AccountUserInfo userInfo;
}

public enum RankType
{
    YandereTheBest = 0,
    YandereTheFast = 1,
}