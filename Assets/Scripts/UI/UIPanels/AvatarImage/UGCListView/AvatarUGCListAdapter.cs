using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using Game.Avatar;
using System;
using GameData.PgcData;
using static BUD.AvatarRole.AvatarUGCListAdapter;

namespace BUD.AvatarRole
{
    public class AvatarUGCListAdapter : GridAdapter<GridParams, UGCClothesGridItemViewsHolder>
    {
        public UnityEngine.Events.UnityEvent OnItemsUpdated;
        public SimpleDataHelper<AvatarRoleItemData> Data { get; set; }
        private IPool texturePool;
        private int currentSelect = -1;
        public AvatarSubType partType { private get; set; }

        protected override void Start()
        {
            // Prevent initialization. It'll be done from the outside
            //base.Start();
            Data = new SimpleDataHelper<AvatarRoleItemData>(this);
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

        protected override void OnCellViewsHolderCreated(UGCClothesGridItemViewsHolder cellVH,
            CellGroupViewsHolder<UGCClothesGridItemViewsHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
            cellVH.iconRemoteImageBehaviour.InitializeWithPool(texturePool);
        }

        // This is called anytime a previously invisible item become visible, or after it's created, 
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateCellViewsHolder(UGCClothesGridItemViewsHolder newOrRecycled)
        {
            var model = Data[newOrRecycled.ItemIndex];
            newOrRecycled.UpdateViews(model, partType, ClickAct);
            newOrRecycled.iconRemoteImageBehaviour.Load(model.iconPath, true, null);
        }

        public AvatarRoleItemData OnSelect(string uid)
        {
            if (Data == null || Data.Count == 0) return null;

            var ovh = GetCellViewsHolderIfVisible(currentSelect);
            if (ovh != null)
            {
                ovh.UpdateSelect(false);
            }

            for (int i = 0; i < Data.Count; i++)
            {
                if (Data[i].itemId == uid)
                {
                    currentSelect = i;
                    var nvh = GetCellViewsHolderIfVisible(currentSelect);
                    nvh.UpdateSelect(true);
                    return Data[i];
                }
            }

            return null;
        }

        public void UpdateCollect(string uid, bool isCollect)
        {
            for (int i = 0; i < Data.Count; i++)
            {
                if (Data[i].itemId == uid)
                {
                    var nvh = GetCellViewsHolderIfVisible(i);
                    nvh.UpdateCollect(isCollect);
                    break;
                }
            }
        }

        private Action<RoleActionType, AvatarRoleItemProtocol> ClickAct;
        public void RegisterAction(Action<RoleActionType, AvatarRoleItemProtocol> ClickAction = null)
        {
            ClickAct = ClickAction;
        }

        public class UGCClothesGridItemViewsHolder : CellViewsHolder
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

            public void UpdateViews(AvatarRoleItemData data, AvatarSubType partType, Action<RoleActionType, AvatarRoleItemProtocol> ClickAction = null)
            {
                communityGameItem.SetData(data, ClickAction);
                RedDotManager.Inst.CheckRedDot((int)partType, data.itemId, communityGameItem.transform);
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