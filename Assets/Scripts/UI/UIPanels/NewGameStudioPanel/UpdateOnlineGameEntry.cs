using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.BaseInfo;
using GameData.MapData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace BUD.GameStudio
{
	public class UpdateOnlineGameEntry : MonoBehaviour
	{
		public Action<bool> updateItems; 
		public UpdateDraftGameAdapter adapter;
		private List<MapInfo> allModels = new List<MapInfo>();
		private string cookie = "";
		protected bool isEnd = false;
		private GameType _gameType;
        private int _gameId;
        private void Awake()
		{
			adapter.OnItemsUpdated.AddListener(()=> {
				updateItems?.Invoke(GetItemCount() == 0 && isEnd);
			});
		}
		
		protected void Start()
		{
			//需要动态拉取数据必须要做的初始化操作
			PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
			refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
			
			adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
			adapter.Data = new LazyDataHelper<MapInfo>(adapter, CreateNewModel);
			adapter.Init();
		}
		public void SetGameID(int gameID) {
			if (gameID > 0)
			{
                _gameId = gameID;
			}
		}
		public void GetFirstPageFriendDatas(GameType gameType=GameType.Normal)
		{
			isEnd = false;
			cookie = "";
			allModels.Clear();
			_gameType = gameType;
			UpdateDraftsGameList(datas =>
			{
				allModels.AddRange(datas);
				adapter.Data.ResetItems(datas.Count, false);
				adapter.OnItemsUpdated?.Invoke();
			});
		}

		public void SetAction(Action<MapInfo> dataAction)
		{
			if (adapter != null)
			{
				adapter.dataAction = dataAction;
			}
		}

		public int GetItemCount()
		{
			if (adapter.Data != null)
				return adapter.Data.Count;
			return 0;
		}
		

		/// <summary>
		/// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private MapInfo CreateNewModel(int index)
		{
			if (index >= 0 && index < allModels.Count)
			{
				return allModels[index];
			}

			return new MapInfo();
		}

		protected string GetUrl()
        {
			return HttpUrlDefine.publishList;;
        }

		public void OnPullReleased(float sign)
		{
			if (sign < 0)
			{
				UpdateDraftsGameList(OnReceivedNewModelsForInsert);
			}
			else if (sign > 0)
			{
				GetFirstPageFriendDatas();
			}
		}

		private void OnReceivedNewModelsForInsert(List<MapInfo> newModels)
		{
			if (newModels == null || newModels.Count == 0)
			{
				adapter.OnItemsUpdated?.Invoke();
				return;
			}

			adapter.Data.List.AddRange(newModels);
			adapter.Refresh(false);
		}


		public void UpdateDraftsGameList(Action<List<MapInfo>> onResult)
		{
			var httpReq = new MapListReq
			{
				cookie = cookie,
				uid = AccountDataManager.Inst.Uid,
				gameType = (int)_gameType,
                gameId = (int)_gameId,
            };
			if (_gameId > 0)
			{
				httpReq.gameId = _gameId;
            }
			
			if (isEnd)
			{
				onResult?.Invoke(new List<MapInfo>());
				return;
			}
			
			NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.publishList, HttpMethod.GET, JsonConvert.SerializeObject(httpReq), (content) =>
				{
					MapListResponse mapListResponse = JsonConvert.DeserializeObject<MapListResponse>(content);
					this.isEnd = mapListResponse.isEnd == 1;
					this.cookie = mapListResponse.cookie;
        
					if (mapListResponse.list == null)
					{
						mapListResponse.list = new List<DraftListItem>();
					}

					List<MapInfo> mapInfos = new List<MapInfo>();
					mapListResponse.list.ForEach(x =>
					{
						if (x.mapInfo != null)
							mapInfos.Add(x.mapInfo);
					});
        
					onResult?.Invoke(mapInfos);
				},
				(error) =>
				{
					onResult?.Invoke(new List<MapInfo>());
				});
		}
	}
}