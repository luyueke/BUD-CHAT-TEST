using System;
using Game.Store;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class CreatorSeriesItem : MonoBehaviour
{
    public GashaponType ViewType;
    public int SeriesId;
    [SerializeField] internal Image Item;
    [SerializeField] internal Text Name;
    [SerializeField] public Button Button;
    [SerializeField] protected long EndTime;
    [SerializeField] protected Text TimeTipText;
    [SerializeField] protected GameObject TimeTipNode;
    private BUDSeriesData _seriesData;

    private void Start()
    {
        if (EndTime > 0)
        {
            SetEndTime(EndTime);
        }
    }

    public BUDSeriesData GetData() {
        return _seriesData;
    }

    public void SetData(BUDSeriesData seriesData)
    {
        _seriesData = seriesData;
        Name.SetLocalText(seriesData.ServerData.Name);
    }

    public void SetEndTime(long endTime)
    {
        if (TimeTipNode!= null && TimeTipText != null && endTime > 0)
        {
            TimeTipNode.gameObject.SetActive(true);
            long remaining = endTime - DataUtil.GetUtcTimeStamp();
            remaining = Mathf.Max(1, (int)remaining);
            long day = remaining / 24 / 3600;
            long hour = (remaining / 3600 - 24 * day);
            if (day == 0 && hour == 0)
            {
                hour = 1;
            }
            
            if (day == 0)
            {
                TimeTipText.SetLocalText("距离售卖结束: {0}小时",hour);
            }
            else
            {
                if (hour == 0)
                {
                    TimeTipText.SetLocalText("距离售卖结束: {0}天",day);
                }
                else
                {
                    TimeTipText.SetLocalText("距离售卖结束: {0}天{1}小时",day,hour);
                }
            }
        }

    }
}
