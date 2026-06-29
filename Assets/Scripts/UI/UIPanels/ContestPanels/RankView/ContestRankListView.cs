using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using UnityEngine;

public class ContestRankListView : MonoBehaviour
{
    public ContestRankEntry gameEntry;
    public GameObject emptyText;
    private Action<ContestEntryInfo> selectAction;
    public ContestRankViewItem selfItem;
    private string _ContestId;

    public string CurContestId
    {
        get
        {
            return _ContestId;
        }
    }
    
    public void InitUI(String contestId, Action<ContestEntryInfo> selectAction)
    {
        this.selectAction = selectAction;
        this._ContestId = contestId;
        gameEntry.SetActions(contestId, selectAction, IsEmptyAction);
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
            selectAction?.Invoke(draftListItem);
        }
    }
    

    /// <summary>
    /// 重新请求数据
    /// </summary>
    public void RequestDataList(Action<List<ContestEntryInfo>> rankItems)
    {
        gameEntry.GetFirstPageDatas((info, self) =>
        {
            if (this == null)
            {
                return;
            }
            rankItems?.Invoke(info);
            GetPageDatas(info, self);
        });
    }

    private void GetPageDatas(List<ContestEntryInfo> infos, ContestEntryInfo selfInfo)
    {
        ShowEmptyTips(infos == null || infos.Count == 0);
        if (selfInfo != null)
        {
            selfItem.gameObject.SetActive(true);
            selfItem.SetData(selfInfo, true);
        }
        else
        {
            selfItem.gameObject.SetActive(false);
        }
    }
    
    private void IsEmptyAction()
    {
        ShowEmptyTips(true);
    }

    private void ShowEmptyTips(bool show)
    {
        emptyText?.gameObject.SetActive(show);
    } 
    
}
