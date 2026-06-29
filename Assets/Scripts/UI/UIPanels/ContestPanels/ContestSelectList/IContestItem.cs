using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using GameData;

public interface IContestItem : IRefreshable<ContestEntryInfo>
{
    public void RefreshContest(ContestInfo contestInfo);
    public void SetImagePool(IPool pool);
    public void UseRanking(bool isUse);
}
