using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Store;
using GameData.PgcData;
using System;

namespace UI.UIPanels.IncubationCabin
{
    // 孵化舱表情弹窗专用 OSA GridAdapter，Item 使用 InteractNodeEmoteItem
    public class IncubationCabinEmoteAdapter : GridAdapter<GridParams, IncubationCabinEmoteItemHolder>
    {
        // 外部（面板）赋值的 LazyDataHelper，按索引懒加载 GoodsData
        public LazyDataHelper<GoodsData> Data { get; set; }

        // 外部注入的选中回调，Item 点击时触发
        public Action<GoodsData> OnItemSelected;

        // 是否多选模式，透传给每个 Item 控制 multiSelectGo 的显示
        public bool IsMultiSelect { get; set; }

        // OSA 回调：每次 Item 进入可视区时绑定数据
        protected override void UpdateCellViewsHolder(IncubationCabinEmoteItemHolder newOrRecycled)
        {
            var data = Data.GetOrCreate(newOrRecycled.ItemIndex);
            if (data == null)
                return;

            bool isPgc = data.GoodsType == GoodsType.SinglePgc|| data.GoodsType== GoodsType.ErrGoodsType;
            newOrRecycled.UpdateViews(data, isPgc, data.Selected, IsMultiSelect, OnItemSelected);
        }
    }

    // 对应的 Cell ViewsHolder，持有 InteractNodeEmoteItem 引用
    public class IncubationCabinEmoteItemHolder : CellViewsHolder
    {
        public InteractNodeEmoteItem EmoteItem;

        // OSA 回调：从 root 收集组件引用
        // InteractNodeEmoteItem 挂在 cellPrefab root 上，views 是其下的"Views"子节点
        public override void CollectViews()
        {
            base.CollectViews();
            EmoteItem = root.GetComponent<InteractNodeEmoteItem>();
        }

        // 绑定数据并同步选中高亮状态
        public void UpdateViews(GoodsData data, bool isPgc, bool isSelected, bool isMultiSelect, Action<GoodsData> onSelect)
        {
            if (EmoteItem == null || data == null)
                return;

            EmoteItem.Init(data, isPgc, isMultiSelect, onSelect);
            EmoteItem.selectImgGo.SetActive(isSelected);
        }
    }
}
