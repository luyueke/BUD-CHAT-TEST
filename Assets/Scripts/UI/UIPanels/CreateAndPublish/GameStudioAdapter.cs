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

namespace BUD.GameStudio
{
    public class GameStudioAdapter : GridAdapter<GridParams, GameStudioItemHolder>
    {
        public StudioSubType StudioSubType;
        public UnityEngine.Events.UnityEvent OnItemsUpdatedAct;
        public Action<DraftListItem> OnSelectItemAct;
        public Action EmptyDataAct;
        public Action<DraftListItem, DraftsItem> UploadAct;

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
            texturePool.Clear();
        }

        public void ClearPool()
        {
            texturePool.Clear();
        }


        /// <summary>
        /// 更新单个上传状态
        /// </summary>
        public void UpdateSingleUploadStatus(string draftId, UploadStatus uploadStatus)
        {
            Refresh();
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

        protected override void OnCellViewsHolderCreated(GameStudioItemHolder cellVH, CellGroupViewsHolder<GameStudioItemHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
            cellVH.Rm_Cover.InitializeWithPool(texturePool);
        }

        protected override void UpdateCellViewsHolder(GameStudioItemHolder newOrRecycled)
        {
            DraftListItem data = Data[newOrRecycled.ItemIndex];

            newOrRecycled.UpdateViews(OnSelectItem, UploadAct, data, this.StudioSubType);
            if (data != null && data.mapInfo != null) {

                // newOrRecycled.Rm_Cover.gameObject.SetActive(false);

                if (string.IsNullOrEmpty(data.mapInfo.cover)) {
                    data.mapInfo.cover = MapAssetManager.Inst.GetTemplateCover();
                }

                if (data.mapInfo.cover != null && !string.IsNullOrEmpty(data.mapInfo.cover))
                {
                    newOrRecycled.Rm_Cover.gameObject.SetActive(false);;
                    newOrRecycled.Rm_Cover.Load(data.mapInfo.cover, true,
                        (from, success) => {
                            newOrRecycled.Rm_Cover.gameObject.SetActive(true);;
                        });
                }
            }
        }
        #endregion
    }


    public class GameStudioItemHolder : CellViewsHolder
    {
        public RemoteImageBehaviour Rm_Cover;
        public DraftsItem draftsItem;

        public override void CollectViews()
        {
            base.CollectViews();
            Rm_Cover = GameObjectEx.FindChildByName(views,"Rm_Cover").GetComponent<RemoteImageBehaviour>();
            draftsItem = root.GetComponentInParent<DraftsItem>();
        }

        public void UpdateViews(Action<DraftListItem> onSelect, Action<DraftListItem, DraftsItem> uploadAction, DraftListItem model, StudioSubType studioSubType)
        {
            if (draftsItem != null)
            {
                draftsItem.Init(onSelect, uploadAction, model, studioSubType);
            }
        }
    }
}
