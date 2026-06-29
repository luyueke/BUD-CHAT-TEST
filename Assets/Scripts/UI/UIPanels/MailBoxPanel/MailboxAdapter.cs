using System;
using Basic.Utils;
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

namespace BUD.MailBox
{
    
    public class MailboxAdapter : GridAdapter<GridParams, MailboxItemHolder>
    {
          public UnityEngine.Events.UnityEvent OnItemsUpdatedAct;
            public Action<MailInfo> OnSelectItemAct;
            // Helper that stores data and notifies the adapter when items count changes
            // Can be iterated and can also have its elements accessed by the [] operator
            public SimpleDataHelper<MailInfo> Data { get; private set; }

            #region OSA implementation

            protected override void Start()
            {
                Data = new SimpleDataHelper<MailInfo>(this);
                base.Start();
            }

            /// <summary>
            /// 销毁必须清空池对象
            /// </summary>
            protected override void OnDestroy()
            {
                base.OnDestroy();
            }
           

            public void UpdateSingleItem(MailInfo mailInfo)
            {
                if (Data.Count == 0)
                    return;
                
                int index = 0;
                for (var i = 0; i < Data.Count; i++)
                {
                    if(Data[i] == null)
                        continue;
                    if (Data[i] != null && Data[i].mailId == mailInfo.mailId)
                    {
                        index = i;
                        break;
                    }
                }

                Data.UpdateItem(index, mailInfo);
                Refresh();
            }

            public void OnSelectItem(MailInfo info)
            {
                OnSelectItemAct?.Invoke(info);
                //处理item的唯一选中态
                if (Data.Count == 0)
                    return;
                for (var i = 0; i < Data.Count; i++)
                {
                    if(Data[i] == null)
                        continue;
                    if (Data[i] != null)
                    {
                        if (Data[i].isSelect)
                        {
                            if (Data[i].mailId == info.mailId)
                            {
                                return;
                            }
                            Data[i].isSelect = false;
                        }
                        if (Data[i].mailId == info.mailId)
                        {
                            Data[i].isSelect = true;
                        }
                    }
                }
                Refresh();
            }
            public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
            {
                OnItemsUpdatedAct?.Invoke();
                base.Refresh(false, keepVelocity);
            }
            
            protected override void OnCellViewsHolderCreated(MailboxItemHolder cellVH, CellGroupViewsHolder<MailboxItemHolder> cellGroup)
            {
                base.OnCellViewsHolderCreated(cellVH, cellGroup);
                
            }
            
            protected override void UpdateCellViewsHolder(MailboxItemHolder newOrRecycled)
            {
                MailInfo data = Data[newOrRecycled.ItemIndex];
                
                newOrRecycled.UpdateViews(OnSelectItem,  data);
            }
            #endregion
    }
    public class  MailboxItemHolder : CellViewsHolder
    {
        public MailboxSystemItem mailItem;

        public override void CollectViews()
        {
            base.CollectViews();
            mailItem = root.GetComponentInParent<MailboxSystemItem>();
        }
            
        public void UpdateViews(Action<MailInfo> onSelect, MailInfo model)
        {
            if (mailItem != null)
            {
                mailItem.Init(onSelect, model);
            }
        }
    }
    
}