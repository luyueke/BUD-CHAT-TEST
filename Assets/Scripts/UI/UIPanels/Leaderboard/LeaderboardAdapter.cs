using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using UnityEngine;

public class LeaderboardAdapter : GridAdapter<GridParams, LeaderboardInfoViewsHolder>
{
	public UnityEngine.Events.UnityEvent OnItemsUpdated;
	public SimpleDataHelper<LeaderboardItemData> Data { get; set; }
	public IPool texturePool;
	private RankType _curRankType = RankType.YandereTheBest;

	protected override void Start()
	{
		texturePool = new FIFOCachingPool(12, TextureDestoryer);
		base.Start();
	}

	/// <summary>
	/// 销毁必须清空池对象
	/// </summary>
	protected override void OnDestroy()
	{
		base.OnDestroy();
		ClearPool();
	}

	public void ClearPool()
	{
		texturePool.Clear();
	}

	private void TextureDestoryer(object urlKey, object texture)
	{
		var asUnityObject = texture as UnityEngine.Object;
		if (asUnityObject != null)
			Destroy(asUnityObject);
	}

	public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
	{
		_CellsCount = Data.Count;
		OnItemsUpdated?.Invoke();
		base.Refresh(false, keepVelocity);
	}


	protected override void OnCellViewsHolderCreated(LeaderboardInfoViewsHolder cellVH, CellGroupViewsHolder<LeaderboardInfoViewsHolder> cellGroup)
	{
		base.OnCellViewsHolderCreated(cellVH, cellGroup);
	}

	// This is called anytime a previously invisible item become visible, or after it's created, 
	// or when anything that requires a refresh happens
	// Here you bind the data from the model to the item's views
	// *For the method's full description check the base implementation
	protected override void UpdateCellViewsHolder(LeaderboardInfoViewsHolder newOrRecycled)
	{
		var curData = Data[newOrRecycled.ItemIndex]; //RecommendItemData
		newOrRecycled.UpdateViews(_curRankType, curData);
	}

	public void SetRankType(RankType rankType)
	{
		_curRankType = rankType;
	}
}

public class LeaderboardInfoViewsHolder : CellViewsHolder
{
    public LeaderboardItem leaderboardItem;
	
    public override void CollectViews()
    {
        base.CollectViews();
        root.TryGetComponent(out leaderboardItem);
    }

    public virtual void UpdateViews(RankType rankType, LeaderboardItemData data)
    {
        if(leaderboardItem == null)
            return;
			
        leaderboardItem.InitData(rankType, data);
    }
}
