using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Event
{
    public class NewbieEventBottomPanel : EventCenterBottomPanel
    {
        public GameObject ItemPrefab;
        private NewbieAllFinishedItem _allFinishedItem;

        public override void BindUI()
        {
            base.BindUI();
            _allFinishedItem = GameObjectEx.FindChildByName(this.transform, "NewbieAllFinishedItem").GetComponent<NewbieAllFinishedItem>();
        }

        public override void InitItems(TaskInfoData infoData)
        {
            base.InitItems(infoData);
            var eventList = infoData.eventList;
            for (int i = 0; i < eventList.Count - 1; i++)
            {
                var curChildCount = ItemContent.childCount;
                var itemObj = GameObject.Instantiate(ItemPrefab, ItemContent);
                itemObj.transform.SetSiblingIndex(curChildCount - 1);
                var itemComp = itemObj.GetComponent<NewbieEventItem>();
                _baseEventItems.Add(itemComp);
            }
            _baseEventItems.Add(_allFinishedItem);
        }

        public override void RefreshItems(TaskInfoData syncTaskData)
        {
            base.RefreshItems(syncTaskData);
            var eventList = this._taskInfoData.eventList;
            bool CurLockState = true;
            for (int i = 0; i < eventList.Count; i++)
            {
                if (i == 0)
                {
                    CurLockState = false;
                }
                else
                {
                    var lastData = eventList[i - 1];
                    CurLockState = lastData.eventStatus != (int)TaskClaimState.Finished;
                }
                
                if (_baseEventItems[i] is NewbieEventItem)
                {
                    var item1 = (NewbieEventItem)_baseEventItems[i];
                    item1.InitData(this._taskInfoData.taskId, eventList[i], CurLockState);
                }
                else if (_baseEventItems[i] is NewbieAllFinishedItem)
                {
                    var item2 = (NewbieAllFinishedItem)_baseEventItems[i];
                    item2.InitData(this._taskInfoData.taskId, eventList[i], CurLockState);
                }
            }
        }
    }
}
