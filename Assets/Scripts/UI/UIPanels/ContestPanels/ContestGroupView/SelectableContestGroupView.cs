using GameData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectableContestGroupView : CommonContestGroupView
{
    public Action<ContestInfo> OnSelect { get; set; }
    public ContestInfo CurContest { get; private set; }

    protected override ActiveContestItem CreateNewItem(ContestInfo contestInfo)
    {
        ActiveContestItem item = base.CreateNewItem(contestInfo);
        if(item is SelectableContestItem sItem)
        {
            sItem.OnSelect = OnSelectItem;
        }
        return item;
    }

    protected void OnSelectItem(SelectableContestItem item)
    {
        for (int i = 0; i < allItems.Count; ++i)
        {
            if(allItems[i] is SelectableContestItem sItem)
            {
                sItem.SetSelect(sItem == item);
            }
        }
        CurContest = item.ContestInfo;
        OnSelect?.Invoke(item.ContestInfo);
    }

    public void SetSelectItem(string contestId)
    {
        SelectableContestItem item = allItems.Find(i => i.ContestInfo.contestId == contestId) as SelectableContestItem;
        if (item)
        {
            OnSelectItem(item);
        }
    }

    public void SetSelectItem(int idx)
    {
        if (allItems.Count > idx)
        {
            SelectableContestItem item = allItems[idx] as SelectableContestItem;
            if (item)
            {
                OnSelectItem(item);
            }
        }

    }

    public void SetDraftListUI()
    {
        var layout = contentParent.GetComponent<VerticalLayoutGroup>();
        layout.padding.top = 50;
    }
}
