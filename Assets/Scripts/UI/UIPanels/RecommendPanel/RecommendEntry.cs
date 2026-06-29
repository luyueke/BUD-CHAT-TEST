using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using UnityEngine;

public class RecommendEntry : MonoBehaviour
{
	public RecommendAdapter adapter;
	[SerializeField] private PullToRefreshBehaviour refreshController;
	private List<RecommendData> allModels = new List<RecommendData>();
	private RecommendDataLoader dataLoader;

	public void SetLoader(RecommendDataLoader loader)
	{
		dataLoader = loader;
	}
	public void ResetAdpater()
	{
		// Resetting to 0 count clears everything, including visible items, so nothing will be recycled
		if (adapter.Data != null)
		{
			adapter.ResetItems(0);
			adapter.ClearPool();
		}
	}

	public void InitCommunityGameDatas(List<RecommendData> datas)
	{
		if (datas == null || datas.Count == 0)
		{
			adapter.OnItemsUpdated?.Invoke();
			adapter.ResetItems(0);
			adapter.ClearPool();
			return;
		}
		allModels = datas;
		adapter.Data.ResetItems(datas.Count, false);
	}

	protected void Awake()
	{
		//需要动态拉取数据必须要做的初始化操作
		refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
		adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
		adapter.Data = new LazyDataHelper<RecommendData>(adapter, CreateNewModel);
		adapter.Init();
		ResetAdpater();
	}

	/// <summary>
	/// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	private RecommendData CreateNewModel(int index)
	{
		return allModels[index];
	}

	public void OnPullReleased()
	{
		if (dataLoader != null)
		{
			dataLoader.GetSectionList(OnReceivedNewModelsForInsert);
		}
	}

	void OnReceivedNewModelsForInsert(RecommendDataList newModels)
	{
		if (newModels == null || newModels.list.Count == 0)
		{
			adapter.OnItemsUpdated?.Invoke();
			return;
		}
		adapter.Data.List.AddRange(newModels.list);
		adapter.Refresh(false);
	}
}
