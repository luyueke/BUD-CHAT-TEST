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
using UnityEngine;

namespace BUD.MailBox
{
    
    public class NotificationAdapter : GridAdapter<GridParams, NotificationItemHolder>
    {
          public UnityEngine.Events.UnityEvent OnItemsUpdatedAct;
            public Action<NotificationInfo> onPropClickAct;
            // Helper that stores data and notifies the adapter when items count changes
            // Can be iterated and can also have its elements accessed by the [] operator
            public SimpleDataHelper<NotificationInfo> Data { get; private set; }
            private IPool texturePool;
            public MailboxSubType _mailboxSubType;
            #region OSA implementation

            protected override void Start()
            {
                texturePool = new FIFOCachingPool(12, TextureDestoryer);
                Data = new SimpleDataHelper<NotificationInfo>(this);
                base.Start();
            }
            private void TextureDestoryer(object urlKey, object texture)
            {
                var asUnityObject = texture as UnityEngine.Object;
                if (asUnityObject != null)
                    Destroy(asUnityObject);
            }
            public void ClearPool()
            {
                texturePool.Clear();
            }
            /// <summary>
            /// 销毁必须清空池对象
            /// </summary>
            protected override void OnDestroy()
            {
                base.OnDestroy();
                texturePool.Clear();
            }

            public void OnPropClick(NotificationInfo info)
            {
                onPropClickAct?.Invoke(info);
            
            }
            public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
            {
                OnItemsUpdatedAct?.Invoke();
                base.Refresh(false, keepVelocity);
            }
            
            protected override void OnCellViewsHolderCreated(NotificationItemHolder cellVH, CellGroupViewsHolder<NotificationItemHolder> cellGroup)
            {
                base.OnCellViewsHolderCreated(cellVH, cellGroup);
                cellVH.propBev.InitializeWithPool(texturePool);
            }
            
            protected override void UpdateCellViewsHolder(NotificationItemHolder newOrRecycled)
            {
                NotificationInfo info = Data[newOrRecycled.ItemIndex];
                
                newOrRecycled.UpdateViews(_mailboxSubType, OnPropClick,info,_mailboxSubType);
                if (info.info!=null)
                {
                    newOrRecycled.propBev.gameObject.SetActive(false);
                    if ( !string.IsNullOrEmpty(info.info.bizUrl))
                    {
                        newOrRecycled.propBev.Load(info.info.bizUrl, true,
                            (from, success) => { newOrRecycled.propBev.gameObject.SetActive(true); });
                    }
                }
            }
            #endregion
    }
    public class  NotificationItemHolder : CellViewsHolder
    {
        public MailBoxNotificationItem mailItem;
        public RemoteImageBehaviour propBev;
        public override void CollectViews()
        {
            base.CollectViews();
            mailItem = root.GetComponentInParent<MailBoxNotificationItem>();
            propBev = GameObjectEx.FindChildByName(views, "PropIcon").GetComponent<RemoteImageBehaviour>();
        }
            
        public void UpdateViews(MailboxSubType mailboxSubType, Action<NotificationInfo> onPropClick, NotificationInfo model,MailboxSubType subType)
        {
            if (mailItem != null)
            {
                mailItem.Init(mailboxSubType, subType == MailboxSubType.Check?null:onPropClick, model);
            }
        }
    }
    
}