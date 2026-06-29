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
	public class UgcToneAdapter : GridAdapter<GridParams, UgcTonePublishedListViewsHolder>
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public SimpleDataHelper<ToneItemData> Data { get; set; }
		private bool IsInDeleteMode;
		private Action<ToneInfo> _onItemSelect;
		private string _curSelectedToneId = "";

		protected override void OnDisable()
		{
			base.OnDisable();
			MusicalInstrumentManager.Inst.StopPreviewUgcTone(gameObject);
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
		protected override void UpdateCellViewsHolder(UgcTonePublishedListViewsHolder newOrRecycled)
		{
			var curData = Data[newOrRecycled.ItemIndex]; //RecommendItemData
			var ugcItemCount = Data.Count - 1;
			newOrRecycled.UpdateViews(curData, ugcItemCount,OnItemSelect, OnDeleteAction, OnPreviewTone);
			newOrRecycled.SetSelectState(curData?.musicToneInfo?.id == GetCurToneId());

			if (IsInDeleteMode)
			{
				newOrRecycled.InitDeleteMode();
			}
		}

		//进入删除模式
		public void SetDeleteState(bool inDeleteMode)
		{
			if(Data == null || Data.Count == 0)
				return;

			IsInDeleteMode = inDeleteMode;
			Refresh();
		}

		public void SetOnToneItemSelectAct(Action<ToneInfo> act)
		{
			this._onItemSelect = act;
		}

		private void OnItemSelect(ToneInfo info)
		{
			this._onItemSelect?.Invoke(info);
			Refresh();
		}

		private void OnDeleteAction(ToneInfo deleteToneInfo)
		{
			if (deleteToneInfo.id == GetCurToneId())
			{
                TipPanel.ShowToast("设置的自定义音色已被删除，自动切换为默认音色");
				var defaultToneInfo = MusicalInstrumentUtils.GetDefaultToneInfo();
				this._onItemSelect?.Invoke(defaultToneInfo);
                MusicalInstrumentManager.Inst.StopPreviewUgcTone(gameObject);
			}
		}

		public string GetCurToneId()
		{
			string curToneId = "";
			var studioPanel = UIManager.Inst.FindPanel<ToneStudioPanel>(PanelId.ToneStudioPanel);
			if (studioPanel != null)
			{
				curToneId = studioPanel.GetCurToneId();
			}
			var publishTonePanel = UIManager.Inst.FindPanel<PublishTonePanel>(PanelId.PublishTonePanel);
			if (publishTonePanel != null)
			{
				curToneId = publishTonePanel.GetCurToneId();
			}
			return curToneId;
		}

		private void OnPreviewTone(ToneInfo toneInfo)
		{
			MusicalInstrumentManager.Inst.PreviewUgcTone(toneInfo, gameObject);
		}
	}

	public class UgcTonePublishedListViewsHolder : CellViewsHolder
	{
		public UgcToneInfoPublishedItem ToneInfoPublishedItem;

		public override void CollectViews()
		{
			base.CollectViews();
			root.TryGetComponent(out ToneInfoPublishedItem);
		}

		public virtual void UpdateViews(ToneItemData data, int ugcItemCount, Action<ToneInfo> act, Action<ToneInfo> deleteAct, Action<ToneInfo> previewToneAct)
		{
			if(ToneInfoPublishedItem == null)
				return;

			if (data.musicToneInfo == null)
			{
				ToneInfoPublishedItem.InitCreateMode(ugcItemCount);
			}
			else
			{
				ToneInfoPublishedItem.InitSelectMode(data, act, deleteAct, previewToneAct);
			}
		}

		public void InitDeleteMode()
		{
			if(ToneInfoPublishedItem == null)
				return;

			ToneInfoPublishedItem.InitDeleteMode();
		}

		public void SetSelectState(bool isSelect)
		{
			ToneInfoPublishedItem.SetSelectState(isSelect);
		}
	}
}
