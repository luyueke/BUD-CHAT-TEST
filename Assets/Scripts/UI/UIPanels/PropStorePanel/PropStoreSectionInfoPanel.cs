using System.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using Game.CommunityGame;
using UnityEngine;

namespace Game.PropStore
{
    public class PropStoreSectionInfoPanel : BaseSectionInfoPanel
    {
        public void SetOnSelectedAct(Action<RecommendItemData> act)
        {
            var propAdapter = (PropStoreAdapter)Adapter;
            propAdapter.OnSelectAct = act;
        }

        public override void OnGetFirstPageDatas(List<RecommendItemData> recommendItemDatas)
        {
            base.OnGetFirstPageDatas(recommendItemDatas);
            if (recommendItemDatas != null && recommendItemDatas.Count > 0)
            {
                var propAdapter = (PropStoreAdapter)Adapter;
                propAdapter.OnPropStoreItemSelected(recommendItemDatas[0]);
                Adapter.Refresh();
            }
        }
    }
}
