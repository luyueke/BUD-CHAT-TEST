using System;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine.EventSystems;

namespace Game.GameHall.View
{
    public class ChatFriendListAdapter: OSA<BaseParamsWithPrefab,ChatFriendListItemHolder>,IPointerClickHandler
	{
		public UnityEngine.Events.UnityEvent OnItemsUpdated;
		// Helper that stores data and notifies the adapter when items count changes
		// Can be iterated and can also have its elements accessed by the [] operator
		public SimpleDataHelper<ConversationListItem> Data { get; private set; }

		private Action<ConversationListItem> clickAction;

		#region OSA implementation
		protected override void Start()
		{
			Data = new SimpleDataHelper<ConversationListItem>(this);
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
		protected override ChatFriendListItemHolder CreateViewsHolder(int itemIndex)
		{
			var instance = new ChatFriendListItemHolder();
			instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
			return instance;
		}
		
		
		protected override void UpdateViewsHolder(ChatFriendListItemHolder newOrRecycled)
		{
			ConversationListItem model = Data[newOrRecycled.ItemIndex];
			newOrRecycled.UpdateViews(model, clickAction);
			newOrRecycled.iconRemoteImageBehaviour.Load(model.portraitUrl, true, null);
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

		public void SetClickAction(Action<ConversationListItem> action)
		{
			clickAction = action;
		}
		
		
	}
	
	
	public class ChatFriendListItemHolder : BaseItemViewsHolder
	{
		public RemoteImageBehaviour iconRemoteImageBehaviour;
		public ChatFriendListItem FriendListItem;
		public override void CollectViews()
		{
			base.CollectViews();
			root.GetComponentAtPath("photoNode/ProfilePhotoItem/photobg/photoImg", out iconRemoteImageBehaviour);
			FriendListItem = root.GetComponent<ChatFriendListItem>();
		}

		public void UpdateViews(ConversationListItem model,Action<ConversationListItem> clickAction)
		{
			FriendListItem.SetData(model, clickAction);
		}
	}
}