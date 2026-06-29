using System;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using GameUI;
using UnityEngine.EventSystems;

namespace UI.UIPanels.FittingRoom
{
    public class OcCompetitionRankAdapter: OSA<BaseParamsWithPrefab,OcCompetitionRankListItemHolder>,IPointerClickHandler
	{
        public Action<OcCptListMsgItem> OnItemSelected;

        public UnityEngine.Events.UnityEvent OnItemsUpdated;
		// Helper that stores data and notifies the adapter when items count changes
		// Can be iterated and can also have its elements accessed by the [] operator
		public SimpleDataHelper<OcCptListMsgItem> Data { get; private set; }

		#region OSA implementation
		protected override void Start()
		{
			Data = new SimpleDataHelper<OcCptListMsgItem>(this);
			base.Start();
		}
		
		/// <summary>
		/// 销毁必须清空池对象
		/// </summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();
		}


		private void TextureDestoryer(object urlKey, object texture)
		{
			var asUnityObject = texture as UnityEngine.Object;
			if (asUnityObject != null)
				Destroy(asUnityObject);
		}
		
		
		public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
		{
			OnItemsUpdated?.Invoke();
			base.Refresh(false, keepVelocity);
		}
		
		
		/// <inheritdoc/>
		protected override OcCompetitionRankListItemHolder CreateViewsHolder(int itemIndex)
		{
			var instance = new OcCompetitionRankListItemHolder();
			instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
			return instance;
		}
		
		
	
		
		#endregion

		
		private Action<PointerEventData> mClickedHandler = null;
		//竖直方向移动到列表尾
		private Action<PointerEventData> mMoveEndHandler = null;
		/// <summary>
		/// 用来区分拖动的点击和滑动的点击
		/// 滑动时不会响应点击事件
		/// </summary>
		private bool m_canClick = true;

        public void AddClickListener(Action<PointerEventData> clickAction)
        {
            mClickedHandler = clickAction;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsDragging)
            {
                mClickedHandler?.Invoke(eventData);
            }
        }

        protected override void UpdateViewsHolder(OcCompetitionRankListItemHolder newOrRecycled)
        {
            OcCptListMsgItem model = Data[newOrRecycled.ItemIndex];
            newOrRecycled.UpdateViews(model, OnItemSelected);
        }
    }
	
	
	public class OcCompetitionRankListItemHolder : BaseItemViewsHolder
	{
		public OcCompetitionRankItem OcCompetitionRankItem;
		public override void CollectViews()
		{
			base.CollectViews();
			OcCompetitionRankItem = root.GetComponent<OcCompetitionRankItem>();
		}
	
		public void UpdateViews(OcCptListMsgItem model, Action<OcCptListMsgItem> ac)
		{
			OcCompetitionRankItem.SetData(model, ac, ItemIndex);
		}
	}
}