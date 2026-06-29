using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using UnityEngine;

public class NewGameStudioEntry : MonoBehaviour
{
	public NewGameStudioAdapter adapter;
	public NewGameStudioDataLoader dataLoader;

	private List<DraftListItem> allModels = new List<DraftListItem>();
	private PullToRefreshBehaviour refreshController;

	private void Awake()
	{
		//需要动态拉取数据必须要做的初始化操作
		refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
		refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
		adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
		adapter.Init();
		ResetAdpater();
	}

	public void ResetAdpater()
	{
		if (refreshController != null)
			refreshController.isDrag = false;
		// Resetting to 0 count clears everything, including visible items, so nothing will be recycled
		adapter.ResetItems(0);
		adapter.ClearPool();
	}

	public void OnGetDatas(List<DraftListItem> datas)
	{
		if (datas == null || datas.Count == 0)
		{
			adapter.OnItemsUpdated?.Invoke();
			return;
		}

		allModels = datas;
		adapter.Data.ResetItems(datas);
		OnPullReleased();
	}

	public void OnPullReleased()
	{
		if (dataLoader != null)
		{
			dataLoader.SetCallBack(OnReceivedNewModelsForInsert);
			dataLoader.GetDatas();
		}
	}

	void OnReceivedNewModelsForInsert(List<DraftListItem> newModels)
	{
		if (newModels == null || newModels.Count == 0)
		{
			adapter.OnItemsUpdated?.Invoke();
			return;
		}

		adapter.Data.List.AddRange(newModels);
		adapter.Refresh(false);
	}
}
