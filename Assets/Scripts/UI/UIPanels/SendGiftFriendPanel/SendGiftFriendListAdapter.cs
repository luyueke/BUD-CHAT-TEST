using System;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using Game.Store;
using UnityEngine.EventSystems;

namespace Game.GameHall.View
{
    public class SendGiftFriendListAdater: OSA<BaseParamsWithPrefab,SendGiftFriendItemHolder>,IPointerClickHandler
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		// Helper that stores data and notifies the adapter when items count changes
		// Can be iterated and can also have its elements accessed by the [] operator
		public SimpleDataHelper<MyFriendsInfo> Data { get; private set; }
		private GoodsData goodsData;
		private int giftType = 0;
		private IPool texturePool;

		#region OSA implementation
		protected override void Start()
		{
			texturePool = new FIFOCachingPool(12, TextureDestoryer);
			Data = new SimpleDataHelper<MyFriendsInfo>(this);
			base.Start();
		}

		public void SetGoodsData(GoodsData goodsData, int giftType)
		{
			this.goodsData = goodsData;
			this.giftType = giftType;
		}
		
		/// <summary>
		/// 销毁必须清空池对象
		/// </summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (texturePool!=null)
			{
				texturePool.Clear();
			}
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
		protected override SendGiftFriendItemHolder CreateViewsHolder(int itemIndex)
		{
			var instance = new SendGiftFriendItemHolder();
			instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
			return instance;
		}
		
		
		protected override void UpdateViewsHolder(SendGiftFriendItemHolder newOrRecycled)
		{
			MyFriendsInfo model = Data[newOrRecycled.ItemIndex];
			newOrRecycled.UpdateViews(model, goodsData, giftType);
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
	}
	
	
	public class SendGiftFriendItemHolder : BaseItemViewsHolder
	{
		public SendGiftFriendListItem FriendRequestListItem;
		public override void CollectViews()
		{
			base.CollectViews();
			FriendRequestListItem = root.GetComponent<SendGiftFriendListItem>();
			FriendRequestListItem.Init();
		}

		public void UpdateViews(MyFriendsInfo model, GoodsData goodsData, int giftType)
		{
			FriendRequestListItem.SetData(model, goodsData, giftType);
		}
	}
}