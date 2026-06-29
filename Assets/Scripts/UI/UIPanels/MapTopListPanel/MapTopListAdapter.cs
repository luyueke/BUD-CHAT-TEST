using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using UI.TopList;
using UnityEngine;


public class MapTopListAdapter : GridAdapter<GridParams, LeaderboardTopListInfoViewsHolder>
{
	public UnityEngine.Events.UnityEvent OnItemsUpdated;
	
	public SimpleDataHelper<RankItem> Data { get; set; }
	public IPool texturePool;
	private MapTopRankType _curRankType = MapTopRankType.fire;
	

	protected override void Start()
	{
		texturePool = new FIFOCachingPool(5);
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


	public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
	{
		_CellsCount = Data.Count;
		OnItemsUpdated?.Invoke();
		base.Refresh(false, keepVelocity);
	}


	protected override void OnCellViewsHolderCreated(LeaderboardTopListInfoViewsHolder cellVH, CellGroupViewsHolder<LeaderboardTopListInfoViewsHolder> cellGroup)
	{
		base.OnCellViewsHolderCreated(cellVH, cellGroup);
	}

	// This is called anytime a previously invisible item become visible, or after it's created, 
	// or when anything that requires a refresh happens
	// Here you bind the data from the model to the item's views
	// *For the method's full description check the base implementation
	protected override void UpdateCellViewsHolder(LeaderboardTopListInfoViewsHolder newOrRecycled)
	{
		var curData = Data[newOrRecycled.ItemIndex]; //RecommendItemData

		//更新方法，要什么就传
		newOrRecycled.UpdateViews(curData);
	}

	public void SetRankType(MapTopRankType rankType)
	{
		_curRankType = rankType;
	}
}

public class LeaderboardTopListInfoViewsHolder : CellViewsHolder
{
	public TopListItem leaderboardItem;
    private bool isInit;

    public override void CollectViews()
	{
		base.CollectViews();
		root.TryGetComponent(out leaderboardItem);
	}

	public virtual void UpdateViews(RankItem data)
	{
		if (leaderboardItem == null)
			return;
		leaderboardItem.InitData(data);
		if (!MapTopListPanel._isInit) {
			MapTopListPanel.Instan.ChangeComponent(data);
			MapTopListPanel._isInit = true;
		}
	}
}



