using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GameData;

public class ActiveContestsGroupView : MonoBehaviour
{
    public Text emptyTxt;
    public Transform contentParent;
    public ActiveContestItem itemSrc;
    public bool autoToContest = true;
    protected List<ActiveContestItem> allItems = new List<ActiveContestItem>();

    public void InitViewInfo( List<BUDContestType> types = null)
    {
        allItems.Clear();
        contentParent.ClearChildren();
        var contests = ContestDataManager.Inst.CurrentLobbyInfo?.contestList;
        if (contests == null || contests.Count == 0)
        {
            emptyTxt.gameObject.SetActive(true);
            return;
        }

        if (types != null && types.Count > 0)
        {
            contests = contests.FindAll(x => types.Contains(x.CurrentContestType) && x.CurrentContestType != BUDContestType.Unknown);
        }
        
        foreach (var kv in contests)
        {
            if (IsContestToShow(kv))
            {
                var itemScript = CreateNewItem(kv);
                if (itemScript) 
                {
                    itemScript.SetItemInfo(kv, null);
                    allItems.Add(itemScript);
                }
            }
        }
        emptyTxt.gameObject.SetActive(contentParent.childCount == 0);
    }

    protected virtual bool IsContestToShow(ContestInfo contestInfo)
    {
        return true;
    }

    protected virtual ActiveContestItem CreateNewItem(ContestInfo contestInfo)
    {
        return null;
    }
}
