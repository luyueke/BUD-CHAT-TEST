using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using frame8.Logic.Misc.Other.Extensions;
using Game.Store;
using GameData.Base;
using GameData.BaseInfo;
using GameUI;
using Network.Message;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class PlantSeedPanelAdpter : GridAdapter<GridParams, PlantSeedPanelItemHolder>
    {
        public LazyDataHelper<object> Data { get; set; }

        public Action<object> OnItemSelected;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void UpdateCellViewsHolder(PlantSeedPanelItemHolder viewsHolder)
        {
            var model = Data.GetOrCreate(viewsHolder.ItemIndex);
            if (model == null)
            {
                return;
            }
            viewsHolder.UpdateViews(model, OnItemSelected, viewsHolder.ItemIndex);
        }
    }

    public class PlantSeedPanelItemHolder : CellViewsHolder
    {
        public PlantSeedPanelAdpterItem item;

        public override void CollectViews()
        {
            base.CollectViews();
            item = root.GetComponent<PlantSeedPanelAdpterItem>();
        }

        public void UpdateViews(object data, Action<object> action, int idx)
        {
            if (data == null)
            {
                return;
            }

            item.SetData(data, action, idx);
        }
    }
}
