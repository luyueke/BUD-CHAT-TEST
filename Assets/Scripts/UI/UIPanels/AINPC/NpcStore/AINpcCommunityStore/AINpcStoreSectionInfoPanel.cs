using System.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using Game.CommunityGame;
using Game.PropStore;
using UnityEngine;

namespace Game.AINPCStudio
{
    public class AINpcStoreSectionInfoPanel : BaseSectionInfoPanel
    {
        public override void OnGetFirstPageDatas(List<RecommendItemData> recommendItemDatas)
        {
            base.OnGetFirstPageDatas(recommendItemDatas);
            if (recommendItemDatas != null && recommendItemDatas.Count > 0)
            {
                var propAdapter = (AINpcPropStoreAdapter)Adapter;
                propAdapter.OnPropStoreItemSelected(recommendItemDatas[0]);
                Adapter.Refresh();
            }
        }
    }
}
