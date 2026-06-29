using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using GameData.GameSync;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels
{
    public class GameChatOSAAdapter : OSA<BaseParamsWithPrefab, GameChatOSAItemHolder>
    {
        public GameChatOSAAdapter()
        {
        }

        FilterableDataHelper<ChatUIData> _Data;
        public FilterableDataHelper<ChatUIData> Data { 
            get{
                if (_Data == null)
                    _Data = new FilterableDataHelper<ChatUIData>(this);
                return _Data;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            Init();
        }

        protected override void Start()
        {
            base.Start();

            Data.NotifyListChangedExternally();
        }

        protected override void OnItemHeightChangedPreTwinPass(GameChatOSAItemHolder viewsHolder)
        {
            base.OnItemHeightChangedPreTwinPass(viewsHolder);

            // 还原默认值
            Data[viewsHolder.ItemIndex].HasPendingVisualSizeChange = false; 
        }

        protected override GameChatOSAItemHolder CreateViewsHolder(int itemIndex)
        {
            var instance = new GameChatOSAItemHolder();
            instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
            return instance;
        }

        protected override void UpdateViewsHolder(GameChatOSAItemHolder newOrRecycled)
        {
            var data = Data[newOrRecycled.ItemIndex];
            newOrRecycled.UpdateByItemIndex(data);

            if (data.HasPendingVisualSizeChange)
			{
				ScheduleComputeVisibilityTwinPass(false);
			}
        }

        protected override void RebuildLayoutDueToScrollViewSizeChange()
        {
            SetAllModelsHavePendingSizeChange();

            base.RebuildLayoutDueToScrollViewSizeChange();
        }

        public override void ChangeItemsCount(ItemCountChangeMode changeMode, int itemsCount, int indexIfInsertingOrRemoving = -1, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {
            if (changeMode == ItemCountChangeMode.RESET)
				SetAllModelsHavePendingSizeChange();    

            base.ChangeItemsCount(changeMode, itemsCount, indexIfInsertingOrRemoving, contentPanelEndEdgeStationary, keepVelocity);
        }

        public void MoveToEnd()
        {
            if(!IsDragging && Data.Count > 0)
            {
                ScrollTo(Data.Count - 1, 1, 1);
                // 最后一条的大小还没有计算大小，再调一次
                ScrollTo(Data.Count - 1, 1, 1); 
            }
        }

        void SetAllModelsHavePendingSizeChange()
		{
			foreach (var model in Data)
				model.HasPendingVisualSizeChange = true;
		}
    }

    public class GameChatOSAItemHolder : BaseItemViewsHolder
    {
        BUD_Text contentText;
        ContentSizeFitter contentSizeFitter;

        public override void CollectViews()
        {
            base.CollectViews();
            contentText = root.GetComponentInChildren<BUD_Text>(true);
            contentSizeFitter = root.GetComponent<ContentSizeFitter>();

            contentSizeFitter.enabled = false;
        }

        public void UpdateByItemIndex(ChatUIData data)
        {
            contentText.text = data.Text;
        }

        public override void MarkForRebuild()
		{
			base.MarkForRebuild();
			if (contentSizeFitter)
				contentSizeFitter.enabled = true;
		}

		public override void UnmarkForRebuild()
		{
			if (contentSizeFitter)
				contentSizeFitter.enabled = false;
			base.UnmarkForRebuild();
		}
    }
}