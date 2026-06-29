using System;
using Basic.Extensions;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.Base;
using UGCAsset;
using UGCAsset.Draft;

public class AvatarStudioAdapter : GridAdapter<GridParams, AvatarStudioItemHolder>
{
      public UnityEngine.Events.UnityEvent OnItemsUpdatedAct;
        public Action<DraftListItem> OnSelectItemAct;
        public Action EmptyDataAct;
        public Action UploadCreateAct;

        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        public SimpleDataHelper<DraftListItem> Data { get; private set; }
        private IPool texturePool;

        #region OSA implementation

        protected override void Start()
        {
            texturePool = new FIFOCachingPool(12, TextureDestoryer);
            Data = new SimpleDataHelper<DraftListItem>(this);
            base.Start();
        }

        /// <summary>
        /// 销毁必须清空池对象
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(texturePool != null)
                texturePool.Clear();
        }
        public void RemoveSingleItem(string infoId)
        {
            if (Data.Count == 0)
            {
                return;
            }

            int index = 0;
            for (var i = 0; i < Data.Count; i++)
            {
                if(Data[i] == null)
                    continue;
                
                UgcBaseInfo ugcBaseInfo = GameStudioUtils.GetBaseInfo(Data[i]);
                if (ugcBaseInfo != null && ugcBaseInfo.id == infoId)
                {
                    index = i;
                    break;
                }
            }

            Data.RemoveOne(index);
            Refresh();
            if (Data.Count == 0)
            {
                EmptyDataAct.Invoke();
            }
        }

        public void UpdateSingleItem(DraftListItem draftsListItem)
        {
            if (Data.Count == 0)
                return;
            
            int index = 0;
            for (var i = 0; i < Data.Count; i++)
            {
                if(Data[i] == null)
                    continue;
                
                UgcBaseInfo ugcBaseInfo = GameStudioUtils.GetBaseInfo(Data[i]);
                UgcBaseInfo draftBaseInfo = GameStudioUtils.GetBaseInfo(draftsListItem);
                if (ugcBaseInfo != null && ugcBaseInfo.id == draftBaseInfo.id)
                {
                    index = i;
                    break;
                }
            }

            Data.UpdateItem(index, draftsListItem);
            Refresh();
        }

        public void ClearPool()
        {
            texturePool.Clear();
        }
        public void OnSelectItem(DraftListItem info)
        {
            OnSelectItemAct?.Invoke(info);
        }

        private void TextureDestoryer(object urlKey, object texture)
        {
            var asUnityObject = texture as UnityEngine.Object;
            if (asUnityObject != null)
                Destroy(asUnityObject);
        }

        public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
        {
            OnItemsUpdatedAct?.Invoke();
            base.Refresh(false, keepVelocity);
        }
        
        protected override void OnCellViewsHolderCreated(AvatarStudioItemHolder cellVH, CellGroupViewsHolder<AvatarStudioItemHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
            cellVH.Rm_Cover.InitializeWithPool(texturePool);
        }
        
        protected override void UpdateCellViewsHolder(AvatarStudioItemHolder newOrRecycled)
        {
            DraftListItem data = Data[newOrRecycled.ItemIndex];
            
            newOrRecycled.UpdateViews(OnSelectItem,UploadCreateAct,  data);
            if (data != null && data.skinInfo != null)
            {
                newOrRecycled.Rm_Cover.gameObject.SetActive(false);
                if (data.skinInfo.cover != null && !string.IsNullOrEmpty(data.skinInfo.cover))
                {
                    newOrRecycled.Rm_Cover.Load(data.skinInfo.cover, true,
                        (from, success) => { newOrRecycled.Rm_Cover.gameObject.SetActive(true); });
                }
            }
        }
        #endregion
}
public class AvatarStudioItemHolder : CellViewsHolder
{
    public RemoteImageBehaviour Rm_Cover;
    public AvatarStudioBaseItem draftsItem;

    public override void CollectViews()
    {
        base.CollectViews();
        Rm_Cover = GameObjectEx.FindChildByName(views,"Rm_Cover").GetComponent<RemoteImageBehaviour>();
        draftsItem = root.GetComponentInParent<AvatarStudioBaseItem>();
    }
        
    public void UpdateViews(Action<DraftListItem> onSelect,Action onCreateSelect, DraftListItem model)
    {
        if (draftsItem != null)
        {
            draftsItem.Init(onSelect,onCreateSelect, model);
        }
    }
}