using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using System;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 每格数据：index 0 = 当前穿搭（isCurrentOutfit=true），其余为服务端 OC 项
    /// </summary>
    public class ChoiceAvatarItemData
    {
        public bool isCurrentOutfit;
        public OcInfo ocInfo;
        public bool selected;
    }

    public class ChoiceAvatarAdapter : GridAdapter<GridParams, ChoiceAvatarItemHolder>
    {
        public PullToRefreshBehaviour PullToRefreshBehaviour;
        public LazyDataHelper<ChoiceAvatarItemData> Data { get; set; }

        public Action<int> OnItemSelected;

        protected override void UpdateCellViewsHolder(ChoiceAvatarItemHolder viewsHolder)
        {
            var model = Data.GetOrCreate(viewsHolder.ItemIndex);
            if (model == null) return;
            viewsHolder.UpdateViews(model, OnItemSelected);
        }
    }

    public class ChoiceAvatarItemHolder : CellViewsHolder
    {
        public ChatChoiceAvatarItem item;

        public override void CollectViews()
        {
            base.CollectViews();
            item = root.GetComponent<ChatChoiceAvatarItem>();
        }

        public void UpdateViews(ChoiceAvatarItemData data, Action<int> onSelect)
        {
            if (item == null) return;
            int idx = ItemIndex;
            item.SetData(data.ocInfo?.ocCover, () => onSelect?.Invoke(idx));
            item.SetSelected(data.selected);
        }
    }
}
