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
using UI.Base;
using UI.TopList;
using UI.BaseWidgets;
using Network;
using Newtonsoft.Json;
using Com.TheFallenGames.OSA.Core;
using static Com.TheFallenGames.OSA.Core.BaseParams;
using DG.Tweening;


public class MapTopListPanel : BasePanel<MapDetailPanel>
{
    public MapTopListAdapter Adapter;
    public PullToRefreshBehaviour refreshController;
    public TopListDataLoder DataLoader;
    public PlayerContributionView contributionView;
    static public MapTopListPanel Instan;
    public MapTopListShowMapView showMapView;


    public CButton dRankButton;
    public CButton zRankButton;
    public CButton mRankButton;


    public CButton backButton;

    public CButton RuleRankButton;
    public CButton closeRuleRankButton;

    public GameObject ruleView;

    public static TopListData topListData;

    public Text timeText;

    public Transform center;

    public static bool _isInit = false;

    private void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
        Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        Adapter.Data = new SimpleDataHelper<RankItem>(Adapter);
        Adapter.Init();
        InitRecommendBg();
        Instan = this;

    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        dRankButton.onClick.AddListener(() => {
            TopListDataLoder._scope = 1;
            dRankButton.interactable = false;
            zRankButton.interactable = true;
            mRankButton.interactable = true;

            // 使用 anchoredPosition
            dRankButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-84, dRankButton.GetComponent<RectTransform>().anchoredPosition.y);
            zRankButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-54, zRankButton.GetComponent<RectTransform>().anchoredPosition.y);
            mRankButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-54, mRankButton.GetComponent<RectTransform>().anchoredPosition.y);
            _isInit = false;
            contributionView.gameObject.SetActive(false);
            GetData(MapTopRankType.fire);
        });

        zRankButton.onClick.AddListener(() => {
            TopListDataLoder._scope = 2;
            dRankButton.interactable = true;
            zRankButton.interactable = false;
            mRankButton.interactable = true;

            dRankButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-54, dRankButton.GetComponent<RectTransform>().anchoredPosition.y);
            zRankButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-84, zRankButton.GetComponent<RectTransform>().anchoredPosition.y);
            mRankButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-54, mRankButton.GetComponent<RectTransform>().anchoredPosition.y);
            _isInit = false;
            contributionView.gameObject.SetActive(false);
            GetData(MapTopRankType.fire);
        });

        mRankButton.onClick.AddListener(() => {
            TopListDataLoder._scope = 3;
            dRankButton.interactable = true;
            zRankButton.interactable = true;
            mRankButton.interactable = false;

            dRankButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-54, dRankButton.GetComponent<RectTransform>().anchoredPosition.y);
            zRankButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-54, zRankButton.GetComponent<RectTransform>().anchoredPosition.y);
            mRankButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-84, mRankButton.GetComponent<RectTransform>().anchoredPosition.y);
            _isInit = false;
            contributionView.gameObject.SetActive(true);
            GetData(MapTopRankType.fire);
        });
        RuleRankButton.onClick.AddListener(() =>
        {
            ruleView.SetActive(true);
        });
        closeRuleRankButton.onClick.AddListener(() =>
        {
            ruleView.SetActive(false);
        });
        backButton.onClick.AddListener(() =>
        {
            CloseSelf();
        });

        dRankButton.onClick.Invoke();
    }

    private void InitRecommendBg()
    {
        if (center == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(center);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#433E3C", atlasPath, new List<string>()
            {
                "S9BgElement1", "S9BgElement2", "S9BgElement3"
            });
        item.gameObject.SetActive(true);
        item.transform.SetAsFirstSibling();
    }


    public void InitComponent(RankItem item)
    {
        if (item == null) return;
        contributionView.InitUI(item.heatContribution);
        showMapView.InitUI(item);
        showMapView.gameObject.SetActive(true);
    }

    



    public void ChangeComponent(RankItem item)
    {
        if (item == null) return;
        contributionView.UpdateUI(item.heatContribution);
        showMapView.UpdateUI(item);
    }


    public void GetData(MapTopRankType rankType)
    {
        Adapter.SetRankType(rankType);
        DataLoader.SetLeaderboardType((int)rankType);
        // 获取数据并通过回调更新UI
        DataLoader.GetLeaderboardData(OnGetFirstPageDatas);
    }

    public void ResetAdpater()
    {
        if (!Adapter.IsInitialized)
            return;

        Adapter.ResetItems(0);
        Adapter.ClearPool();
    }

    private void HideGizmo()
    {
        //需要动态拉取数据必须要做的初始化操作
        refreshController.HideGizmo();
    }

    public virtual void OnGetFirstPageDatas(List<RankItem> leaderboardItemDatas)
    {
        ResetAdpater();
        if (topListData != null)
        {
            timeText.text = topListData.displayText;
        }
        else
        {
            LoggerUtils.LogError("未找到topListData");
        }
        
        if (leaderboardItemDatas == null || leaderboardItemDatas.Count == 0)
        {
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }

        // 确保数据正确初始化
        foreach (var item in leaderboardItemDatas)
        {
            if (item.heatContribution != null)
            {
                foreach (var contribution in item.heatContribution)
                {
                    if (contribution.userInfo == null)
                    {
                        LoggerUtils.LogError($"排行榜数据错误: 贡献数据中缺少用户信息");
                        continue;
                    }
                }
            }
        }

        Adapter.Data.ResetItems(leaderboardItemDatas);
        Adapter.OnItemsUpdated?.Invoke();
    }

    private void OnPullReleased()
    {   
        
        DataLoader.GetLeaderboardData(OnReceivedNewModelsForInsert);
        
    }

    private void OnReceivedNewModelsForInsert(List<RankItem> leaderboardItemDatas)
    {
        if (leaderboardItemDatas == null || leaderboardItemDatas.Count == 0)
        {
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }
        Adapter.Data.List.AddRange(leaderboardItemDatas);
        Adapter.Refresh(false);
    }

}
