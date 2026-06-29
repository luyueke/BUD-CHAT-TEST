using System.Collections;
using System.Collections.Generic;
using GameData;
using GameUI;
using UnityEngine;

public class CommonContestGroupView : ActiveContestsGroupView
{
    public bool noOc;

    protected override bool IsContestToShow(ContestInfo contestInfo)
    {
        base.IsContestToShow(contestInfo);
        if (contestInfo.contestType == (int)BUDContestType.OC)
        {
            if (noOc)
            {
                return false;
            }
            if (!OcCompetitionSystem.Inst.Exist())
            {
                return false;
            }
        }
        return contestInfo.status == (int)ContestStatus.InProgress || contestInfo.status == (int)ContestStatus.Completed
            || contestInfo.status == (int)ContestStatus.Submission;
    }

    protected override ActiveContestItem CreateNewItem(ContestInfo contestInfo)
    {
        base.CreateNewItem(contestInfo);
        return Instantiate(itemSrc, contentParent);
    }
}
