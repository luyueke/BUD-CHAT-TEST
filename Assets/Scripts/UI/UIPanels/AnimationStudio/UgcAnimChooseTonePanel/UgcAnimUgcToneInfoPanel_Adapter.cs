using System;
using System.Collections.Generic;
using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.BaseInfo;
using UGCAsset;

namespace Game.MusicalInstrument
{
	public class UgcAnimUgcToneInfoPanel_Adapter : GridAdapter<GridParams, UgcAnimOwnedToneInfoPanel_ViewsHolder>
	{
		public bool isOwnedList;
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public SimpleDataHelper<AnimMusicListRspData> Data { get; set; }
		private Action<AnimMusicInfo> _onItemSelect;
		private Action _refreshAct;
		private string _curSelectedToneId = "";

		protected override void OnDisable()
		{
			base.OnDisable();
			UgcAnimToneManager.Inst.StopPreviewTone();
		}

		public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
		{
			_CellsCount = Data.Count;
			OnItemsUpdated?.Invoke();
			base.Refresh(false, keepVelocity);
		}

		// This is called anytime a previously invisible item become visible, or after it's created,
		// or when anything that requires a refresh happens
		// Here you bind the data from the model to the item's views
		// *For the method's full description check the base implementation
		protected override void UpdateCellViewsHolder(UgcAnimOwnedToneInfoPanel_ViewsHolder newOrRecycled)
		{
			var curData = Data[newOrRecycled.ItemIndex]; //RecommendItemData
			newOrRecycled.UpdateViews(isOwnedList, curData,OnItemSelect, OnPreviewTone, _refreshAct);
			if (isOwnedList)
			{
				newOrRecycled.SetSelectState(!string.IsNullOrEmpty(curData?.ugcInfo?.id) && curData?.ugcInfo?.id == GetCurToneId());
			}
			else
			{
				newOrRecycled.SetSelectState(!string.IsNullOrEmpty(curData?.animMusicInfo?.id) && curData?.animMusicInfo?.id == GetCurToneId());
			}
		}

		public void SetOnToneItemSelectAct(Action<AnimMusicInfo> act, Action refreshAct)
		{
			this._onItemSelect = act;
			this._refreshAct = refreshAct;
		}

		private void OnItemSelect(AnimMusicInfo info)
		{
			this._onItemSelect?.Invoke(info);
			Refresh();
		}

		public string GetCurToneId()
		{
			string curToneId = null;
			var studioPanel = UIManager.Inst.FindPanel<UgcAnimChooseTonePanel>(PanelId.UgcAnimChooseTonePanel);
			if (studioPanel != null)
			{
				curToneId = studioPanel.GetCurToneId();
			}
			else{
				var vehicleAudioPanel = UIManager.Inst.FindPanel<VehicleAudioPanel>(PanelId.VehicleAudioPanel);
				if (vehicleAudioPanel != null)
				{
					curToneId = vehicleAudioPanel.GetCurToneId();
				}
			}
			return curToneId;
		}

		private void OnPreviewTone(AnimMusicInfo animMusicInfo)
		{
			UgcAnimToneManager.Inst.PreviewTone(animMusicInfo);
		}
	}

	public class UgcAnimOwnedToneInfoPanel_ViewsHolder : CellViewsHolder
	{
		public UgcAnimUgcToneItem UgcToneItem;

		public override void CollectViews()
		{
			base.CollectViews();
			root.TryGetComponent(out UgcToneItem);
		}

		public virtual void UpdateViews(bool isOwnedList, AnimMusicListRspData data, Action<AnimMusicInfo> act, Action<AnimMusicInfo> previewToneAct, Action refreshAct)
		{
			if(UgcToneItem == null)
				return;

			if (isOwnedList)
			{
				if (data == null || data.ugcInfo == null)
				{
					UgcToneItem.InitGoStoreMode(refreshAct);
				}
				else
				{
					UgcToneItem.InitSelectMode(isOwnedList, data.ugcInfo, act, previewToneAct, refreshAct);
				}
			}
			else
			{
				if (data == null || data.animMusicInfo == null)
				{
					UgcToneItem.InitCreateMode();
				}
				else
				{
					UgcToneItem.InitSelectMode(isOwnedList, data.animMusicInfo, act, previewToneAct, refreshAct);
				}
			}
		}
		
		public void SetSelectState(bool isSelect)
		{
			UgcToneItem.SetSelectState(isSelect);
		}
	}
}

