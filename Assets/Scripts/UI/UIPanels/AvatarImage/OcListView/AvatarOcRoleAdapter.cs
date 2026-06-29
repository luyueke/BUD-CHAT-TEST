using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;

namespace BUD.AvatarRole
{
    public class AvatarOcRoleAdapter : GridAdapter<GridParams, MyGridItemViewsHolder>
    {
        public UnityEngine.Events.UnityEvent OnItemsUpdated;
        public SimpleDataHelper<AvatarOcData> Data { get; set; }
        private IPool texturePool;
        private int currentSelect = -1;

        protected override void Start()
        {
            // Prevent initialization. It'll be done from the outside
            //base.Start();
            Data = new SimpleDataHelper<AvatarOcData>(this);
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
            texturePool?.Clear();
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

        protected override void OnCellViewsHolderCreated(MyGridItemViewsHolder cellVH,
            CellGroupViewsHolder<MyGridItemViewsHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
            cellVH.iconRemoteImageBehaviour.InitializeWithPool(texturePool);
        }

        // This is called anytime a previously invisible item become visible, or after it's created, 
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateCellViewsHolder(MyGridItemViewsHolder newOrRecycled)
        {
            var model = Data[newOrRecycled.ItemIndex];
            newOrRecycled.UpdateViews(model, ClickAct, LongClickAct);
            newOrRecycled.iconRemoteImageBehaviour.Load(model.ocInfo.ocCover, true, null);
        }

        public void OnSelect(AvatarOcData ocData)
        {
            if (Data == null || Data.Count == 0) return;

            var ovh = GetCellViewsHolderIfVisible(currentSelect);
            if (ovh != null)
            {
                ovh.UpdateSelect(false);
            }

            for (int i = 0; i < Data.Count; i++)
            {
                if (Data[i].ocInfo.ocId == ocData.ocInfo.ocId)
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

        private Action<AvatarOcData> ClickAct;
        private Action<AvatarOcData> LongClickAct;
        public void RegisterAction(Action<AvatarOcData> ClickAction = null, Action<AvatarOcData> LongClickAction = null)
        {
            ClickAct = ClickAction;
            LongClickAct = LongClickAction;
        }
    }

    public class MyGridItemViewsHolder : CellViewsHolder
    {
        private string RemoteImageViewName = "Image";
        public RemoteImageBehaviour iconRemoteImageBehaviour;
        public AvatarOcItemView communityGameItem;

        public override void CollectViews()
        {
            base.CollectViews();
            views.GetComponentAtPath(RemoteImageViewName, out iconRemoteImageBehaviour);
            communityGameItem = views.GetComponentInParent<AvatarOcItemView>(true);
            communityGameItem.Init();
        }

        public void UpdateViews(AvatarOcData model, Action<AvatarOcData> ClickAction = null, Action<AvatarOcData> LongClickAction = null)
        {
            communityGameItem.SetData(model, ClickAction, LongClickAction);
        }

        public void UpdateSelect(bool isSelect)
        {
            if (communityGameItem != null)
            {
                communityGameItem.UpdateSelected(isSelect);
            }
        }
    }
}