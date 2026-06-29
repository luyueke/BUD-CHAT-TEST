using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using Game.Avatar;
using System;
using GameData.PgcData;
using static BUD.AvatarRole.AvatarCollectListAdapter;

namespace BUD.AvatarRole
{
    public class AvatarCollectListAdapter : GridAdapter<GridParams, MyGridCollectItemViewsHolder>
    {
        public UnityEngine.Events.UnityEvent OnItemsUpdated;
        public SimpleDataHelper<AvatarRoleItemProtocol> Data { get; set; }
        private IPool texturePool;
        private int currentSelect = -1;
        public AvatarSubType partType { private get; set; }

        protected override void Start()
        {
            // Prevent initialization. It'll be done from the outside
            //base.Start();
            Data = new SimpleDataHelper<AvatarRoleItemProtocol>(this);
            texturePool = new FIFOCachingPool(36, TextureDestoryer);
        }

        /// <summary>
        /// 销毁必须清空池对象
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearPool();
        }

        public void ClearPool()
        {
            texturePool.Clear();
        }

        private void TextureDestoryer(object urlKey, object texture)
        {
            var asUnityObject = texture as UnityEngine.Object;
            if (asUnityObject != null)
                Destroy(asUnityObject);
        }

        public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
        {
            _CellsCount = Data.Count;
            OnItemsUpdated?.Invoke();
            base.Refresh(false, keepVelocity);
        }

        protected override void OnCellViewsHolderCreated(MyGridCollectItemViewsHolder cellVH,
            CellGroupViewsHolder<MyGridCollectItemViewsHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
            cellVH.iconRemoteImageBehaviour.InitializeWithPool(texturePool);
        }

        // This is called anytime a previously invisible item become visible, or after it's created, 
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateCellViewsHolder(MyGridCollectItemViewsHolder newOrRecycled)
        {
            var model = Data[newOrRecycled.ItemIndex];
            newOrRecycled.UpdateViews(model, partType, ClickAct);
            if (!model.isPGCItem)
            {
                newOrRecycled.iconRemoteImageBehaviour.Load(model.iconPath, true, null);
            }
        }

        public void OnSelect(AvatarRoleItemProtocol itemData)
        {
            if (Data == null || Data.Count == 0) return;

            var ovh = GetCellViewsHolderIfVisible(currentSelect);
            if (ovh != null)
            {
                ovh.UpdateSelect(false);
            }

            for (int i = 0; i < Data.Count; i++)
            {
                if (Data[i].itemId == itemData.itemId && Data[i].templateId == itemData.templateId)
                {
                    currentSelect = i;
                    var nvh = GetCellViewsHolderIfVisible(currentSelect);
                    nvh.UpdateSelect(true);
                    break;
                }
            }
        }

        public void HideSelected()
        {
            if (Data == null || Data.Count == 0) return;

            var ovh = GetCellViewsHolderIfVisible(currentSelect);
            if (ovh != null)
            {
                ovh.UpdateSelect(false);
            }
        }

        private Action<RoleActionType, AvatarRoleItemProtocol> ClickAct;
        public void RegisterAction(Action<RoleActionType, AvatarRoleItemProtocol> ClickAction = null)
        {
            ClickAct = ClickAction;
        }

        public class MyGridCollectItemViewsHolder : CellViewsHolder
        {
            private string RemoteImageViewName = "RawImage";
            public RemoteImageBehaviour iconRemoteImageBehaviour;
            public AvatarRoleItemView communityGameItem;

            public override void CollectViews()
            {
                base.CollectViews();
                views.GetComponentAtPath(RemoteImageViewName, out iconRemoteImageBehaviour);
                communityGameItem = views.GetComponentInParent<AvatarRoleItemView>();
            }

            public void UpdateViews(AvatarRoleItemProtocol data, AvatarSubType partType, Action<RoleActionType, AvatarRoleItemProtocol> ClickAction = null)
            {
                if (communityGameItem == null)
                {
                    communityGameItem = views.GetComponentInParent<AvatarRoleItemView>(true);
                }
                communityGameItem.SetData(data, ClickAction);
            }

            public void UpdateSelect(bool isSelect)
            {
                if (communityGameItem != null)
                {
                    communityGameItem.UpdateSelected(isSelect);
                }
            }

            public void UpdateCollect(bool isCollect)
            {
                if (communityGameItem != null)
                {
                    communityGameItem.UpdateCollect(isCollect);
                }
            }
        }
    }
}