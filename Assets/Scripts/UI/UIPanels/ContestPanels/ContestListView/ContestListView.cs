using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using UI.BaseWidgets;
using UnityEngine;

public class ContestListView : MonoBehaviour
{
    public static ContestListView ContestListViewObject(ContestInfo info, GameObject refObj)
    {
        if (info == null)
        {
            return null;
        }

        var path = "Assets/Loadable/UI/UIPanel/Contest/ContestDetail/ContestPropListView.prefab";
        switch (info.CurrentContestType)
        {
            case BUDContestType.Skin:
            case BUDContestType.MusicScore:
            case BUDContestType.Instrument:
            case BUDContestType.Bundle:
            case BUDContestType.PetSkin:
            case BUDContestType.PetBundle:
            case BUDContestType.Vehicle:
                path = "Assets/Loadable/UI/UIPanel/Contest/ContestDetail/ContestPropListView.prefab";
                break;
            case BUDContestType.Camera:
                path = "Assets/Loadable/UI/UIPanel/Contest/ContestDetail/ContestPhotoListView.prefab";
                break;
            case BUDContestType.OC:
            case BUDContestType.PetOC:
                path = "Assets/Loadable/UI/UIPanel/Contest/ContestDetail/ContestAvatarListView.prefab";
                break;
        }

        var obj = XAssetLoaderMgr.Inst.LoadResource<GameObject>(path, refObj);
        ContestListView view = obj.GetComponent<ContestListView>();
        return view;
    }

    [SerializeField] private ContestListViewEntry gameEntry;
    [SerializeField] private CText emptyText;
    [SerializeField] private ContestSearchInputView _searchInputView;
    private Action<ContestEntryInfo> selectAction;
    private string _ContestId;

    private string emptyTextStr = "还没有作品，去参加活动领取奖励吧！";

    private string searchStart = "加载中...";
    private string searchEmpty = "没有找到相关内容";

    public string CurContestId
    {
        get
        {
            return _ContestId;
        }
    }

    private void Awake()
    {
        if (_searchInputView != null)
        {
            _searchInputView.ClearAction = LearchSearch;
            _searchInputView.SearchAction = EnterSearch;
        }
    }

    public void InitUI(ContestPageType pageType, ContestInfo contestInfo, Action<ContestEntryInfo> selectAction = null, bool hideRankForce = false)
    {

        this.selectAction = selectAction;
        this._ContestId = contestInfo.contestId;
        gameEntry.SetActions(pageType, contestInfo, selectAction, IsEmptyAction, hideRankForce);

        RequestDataList();
    }

    public void SetSearchPlaceholder(string text)
    {
        string holderText = LocalizationManager.Inst.GetLocalizedText(text);
        _searchInputView?.SetPlaceholder(holderText);
    }

    public void RemoveSingleItem(string mapId)
    {
        if (gameEntry != null)
        {
            gameEntry.RemoveSingleItem(mapId);
        }
    }

    public void UpdateSingleItem(ContestEntryInfo draftListItem)
    {
        if (gameEntry != null && draftListItem != null)
        {
            gameEntry.UpdateSingleItem(draftListItem);
        }
    }

    public void ShowSearch()
    {
        _searchInputView.gameObject.SetActive(true);
    }

    public void HideSearch()
    {
        _searchInputView.gameObject.SetActive(false);
    }

    private void EnterSearch(string searchKey)
    {
        if (emptyText)
        {
            emptyText.SetLocalText(searchStart);
        }
        ShowEmptyTips(true);

        gameEntry.EnterSearch(searchKey);
        RequestDataList();
    }

    private void LearchSearch()
    {
        if (emptyText)
        {
            emptyText.SetLocalText(emptyTextStr);
        }
        ShowEmptyTips(false);
        gameEntry.LeaveSearch();
        RequestDataList();
    }

    /// <summary>
    /// 重新请求数据
    /// </summary>
    public void RequestDataList()
    {
        gameEntry.GetFirstPageDatas(GetPageDatas);
    }

    private void GetPageDatas(List<ContestEntryInfo> infos)
    {
        ShowEmptyTips(infos == null || infos.Count == 0);
    }

    private void IsEmptyAction()
    {
        ShowEmptyTips(true);
    }

    private void ShowEmptyTips(bool show)
    {
        if (gameObject == null) {
            return;
        }
        if (gameEntry.isInSearch)
        {
            emptyText.SetLocalText(gameEntry.isInSearch ? searchEmpty : emptyTextStr);
        }
        else
        {
            emptyText.SetLocalText(emptyTextStr);
        }

        emptyText?.gameObject.SetActive(show);
    }

}
