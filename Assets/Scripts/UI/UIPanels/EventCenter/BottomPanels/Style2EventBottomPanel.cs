using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Event
{
    public class Style2EventBottomPanel : EventCenterBottomPanel
    {
        public GameObject ItemPrefab;

        public override void InitItems(TaskInfoData infoData)
        {
            base.InitItems(infoData);
            var eventList = infoData.eventList;
            for (int i = 0; i < eventList.Count; i++)
            {
                var itemObj = GameObject.Instantiate(ItemPrefab, ItemContent);
                var itemComp = itemObj.GetComponent<Style2EventItem>();
                _baseEventItems.Add(itemComp);
            }
        }

        public override void RefreshItems(TaskInfoData syncTaskData)
        {
            base.RefreshItems(syncTaskData);
            var eventList = this._taskInfoData.eventList;
            for (int i = 0; i < eventList.Count; i++)
            {
                var itemComp = (Style2EventItem)_baseEventItems[i];
                itemComp.InitData(this._taskInfoData.taskId, eventList[i]);
            }
        }
    }
}