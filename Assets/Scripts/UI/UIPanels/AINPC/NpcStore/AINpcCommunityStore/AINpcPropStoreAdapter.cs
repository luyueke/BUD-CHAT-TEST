using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Game.CommunityGame;
using GameData.Base;
using Newtonsoft.Json;

namespace Game.PropStore
{
    public class AINpcPropStoreAdapter : BaseSectionInfoAdapter
    {
        public Action<RecommendItemData> OnSelectAct;
        private List<string> OwnPropList = new List<string>();
        private Func<string> GetCurSelectedIdFunc;
        
        protected override void OnCellViewsHolderCreated(BaseSectionInfoViewsHolder cellVH, CellGroupViewsHolder<BaseSectionInfoViewsHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
            cellVH.IconRemoteImageBehaviour.InitializeWithPool(texturePool);
            var storeItem = (AINpcPropStoreItem)cellVH.SectionInfoItem;
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

            if (OwnPropList.Contains(curData.ugcId))
            {
                curData.interactInfo.consumed = 1;
            }
            
            newOrRecycled.UpdateViews(curData);

            var storeItem = (AINpcPropStoreItem)newOrRecycled.SectionInfoItem;
            if (storeItem == null)
            {
                LoggerUtils.LogError("UpdateCellViewsHolder PropStoreItem Is Null");
                return;
            }

            storeItem.SetSelectState(curData.ugcId == GetCurSelectedIdFunc());
            if(OwnPropList.Contains(curData.ugcId))
                storeItem.SetOwned();
        }

        public void OnPropStoreItemSelected(RecommendItemData data)
        {
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

        public void SetGetSelectedIdFunc(Func<string> func)
        {
            this.GetCurSelectedIdFunc = func;
        }
    }
}