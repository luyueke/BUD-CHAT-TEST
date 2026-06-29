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
    public class GEParkSelectAdpter : GridAdapter<GridParams, GEParkSelectItemHolder>
    {
        public LazyDataHelper<UgcBaseInfo> Data { get; set; }

        public Action<UgcBaseInfo> OnItemSelected;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void UpdateCellViewsHolder(GEParkSelectItemHolder viewsHolder)
        {
            var model = Data.GetOrCreate(viewsHolder.ItemIndex);
            if (model == null)
            {
                return;
            }
            viewsHolder.UpdateViews(model, OnItemSelected, viewsHolder.ItemIndex);
        }
    }

    public class GEParkSelectItemHolder : CellViewsHolder
    {
        public GEParkSelectItem item;

        public override void CollectViews()
        {
            base.CollectViews();
            item = root.GetComponent<GEParkSelectItem>();
        }

        public void UpdateViews(UgcBaseInfo data, Action<UgcBaseInfo> action,int idx)
        {
            if (data == null)
            {
                return;
            }

            item.SetData(data, action, idx);
        }
    }
}
