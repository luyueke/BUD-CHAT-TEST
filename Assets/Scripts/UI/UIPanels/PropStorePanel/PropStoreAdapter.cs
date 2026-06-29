using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.Util.IO;
using frame8.Logic.Misc.Other.Extensions;
using Game.CommunityGame;
using Game.PropStore;


namespace Game.PropStore
{
    public class PropStoreAdapter : BaseSectionInfoAdapter
    {
        public Action<RecommendItemData> OnSelectAct;
        private string curSelectId;
        private List<string> OwnPropList = new List<string>();
        protected override void OnCellViewsHolderCreated(BaseSectionInfoViewsHolder cellVH, CellGroupViewsHolder<BaseSectionInfoViewsHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
            cellVH.IconRemoteImageBehaviour.InitializeWithPool(texturePool);
            var storeItem = (PropStoreItem)cellVH.SectionInfoItem;
            if (storeItem == null)
            {
                LoggerUtils.LogError("OnCellViewsHolderCreated PropStoreItem Is Null");
                return;
            }
            storeItem.SetOnSelectedAct(OnPropStoreItemSelected);
        }

        protected override void UpdateCellViewsHolder(BaseSectionInfoViewsHolder newOrRecycled)
        {
            base.UpdateCellViewsHolder(newOrRecycled);
            var curData = Data[newOrRecycled.ItemIndex]; //RecommendItemData
            newOrRecycled.UpdateViews(curData);
            newOrRecycled.IconRemoteImageBehaviour.Load(curData?.ugcInfo?.cover);

            var storeItem = (PropStoreItem)newOrRecycled.SectionInfoItem;
            if (storeItem == null)
            {
                LoggerUtils.LogError("UpdateCellViewsHolder PropStoreItem Is Null");
                return;
            }

            storeItem.SetSelectState(curData.ugcInfo.id == curSelectId);
            if(OwnPropList.Contains(curData.ugcInfo.id))
                storeItem.SetOwned();
        }

        public void OnPropStoreItemSelected(RecommendItemData data)
        {
            curSelectId = data.ugcInfo.id;
            OnSelectAct?.Invoke(data);
            Refresh();
        }

        //购买成功后的UI刷新
        public void OnBuySuccess(string ugcId)
        {
            OwnPropList.Add(ugcId);
            Refresh();
        }

        public void SetOnClickAction(Action<RecommendItemData> act)
        {
            this.OnSelectAct = act;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
        }
    }
}