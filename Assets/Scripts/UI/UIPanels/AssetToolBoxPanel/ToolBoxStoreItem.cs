using System;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;


namespace Game.AssetToolBox
{
    public class ToolBoxStoreItem : ToolBoxBaseItem
    {
        public SellPriceTag PriceTag;

        public override void RefreshData(ToolBoxItemData data, Action<ToolBoxItemData> onClickAct)
        {
            base.RefreshData(data, onClickAct);
            
            if (data.interactInfo != null && data.interactInfo.consumed == 1)
            {
                PriceTag.SetOwnState();
            }
            else
            {
                PriceTag.SetPaymentInfo(data?.ugcInfo?.paymentInfo);
            }
        }
    }
}
