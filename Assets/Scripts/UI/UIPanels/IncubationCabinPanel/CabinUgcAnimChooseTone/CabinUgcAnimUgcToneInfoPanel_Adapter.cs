using System;
using System.Collections.Generic;
using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.BaseInfo;
using UI.UIPanels.IncubationCabin;
using UGCAsset;

namespace Game.MusicalInstrument
{
	public class CabinUgcAnimUgcToneInfoPanel_Adapter : GridAdapter<GridParams, CabinUgcAnimOwnedToneInfoPanel_ViewsHolder>
	{
		public bool isOwnedList;
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		public SimpleDataHelper<CabinToneInfo> Data { get; set; }
		private Action<CabinToneInfo> _onItemSelect;
		private string _curSelectedToneId = "";
        private CabinCharacterUgcInfo _characterUgcInfo;


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

		protected override void UpdateCellViewsHolder(CabinUgcAnimOwnedToneInfoPanel_ViewsHolder newOrRecycled)
		{
			var curData = Data[newOrRecycled.ItemIndex];
			newOrRecycled.UpdateViews(_characterUgcInfo, isOwnedList, curData, OnItemSelect, OnPreviewTone);
			newOrRecycled.SetSelectState(!string.IsNullOrEmpty(curData?.id) && curData?.id == GetCurToneId());
		}

        public void SetOnToneItemSelectAct(CabinCharacterUgcInfo characterUgcInfo)
        {
            this._characterUgcInfo = characterUgcInfo;
        }

        public void SetOnToneItemSelectAct(Action<CabinToneInfo> act)
		{
            this._onItemSelect = act;
		}

		private void OnItemSelect(CabinToneInfo info)
		{
			_curSelectedToneId = info?.id ?? "";
			this._onItemSelect?.Invoke(info);
			Refresh();
		}

		public string GetCurToneId()
		{
			if (!string.IsNullOrEmpty(_curSelectedToneId))
				return _curSelectedToneId;

			string curToneId = null;
			var studioPanel = UIManager.Inst.FindPanel<UgcAnimChooseTonePanel>(PanelId.UgcAnimChooseTonePanel);
			if (studioPanel != null)
			{
				curToneId = studioPanel.GetCurToneId();
			}
			return curToneId;
		}

		public void SelectFirstRealItem(List<CabinToneInfo> data)
		{
			if (data == null)
				return;

			CabinToneInfo firstReal = null;
			foreach (var item in data)
			{
				if (!string.IsNullOrEmpty(item?.id))
				{
					firstReal = item;
					break;
				}
			}

			if (firstReal == null)
				return;

			_curSelectedToneId = firstReal.id;
			_onItemSelect?.Invoke(firstReal);
			Refresh();
		}

		private void OnPreviewTone(CabinToneInfo toneInfo)
		{
			UgcAnimToneManager.Inst.PreviewTone(toneInfo);
		}
	}

	public class CabinUgcAnimOwnedToneInfoPanel_ViewsHolder : CellViewsHolder
	{
        private CabinCharacterUgcInfo _characterUgcInfo;
        public IncubationToneShopItem UgcToneItem;

		public override void CollectViews()
		{
			base.CollectViews();
			root.TryGetComponent(out UgcToneItem);
		}

		public virtual void UpdateViews(CabinCharacterUgcInfo characterUgcInfo, bool isOwnedList, CabinToneInfo data, Action<CabinToneInfo> act, Action<CabinToneInfo> previewToneAct)
		{
            this._characterUgcInfo = characterUgcInfo;

			if (UgcToneItem == null)
				return;

			if (isOwnedList)
			{
				if (data == null || string.IsNullOrEmpty(data.id))
				{
					UgcToneItem.InitGoStoreMode();
				}
				else
				{
					UgcToneItem.InitSelectMode(data, act, previewToneAct);
				}
			}
			else
			{
				if (data == null || string.IsNullOrEmpty(data.id))
				{
					UgcToneItem.InitCreateMode(characterUgcInfo);
				}
				else
				{
					UgcToneItem.InitSelectMode(data, act, previewToneAct);
				}
			}
		}

		public void SetSelectState(bool isSelect)
		{
			UgcToneItem.SetSelectState(isSelect);
		}
	}
}
