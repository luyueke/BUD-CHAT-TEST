using System;
using System.Collections.Generic;
using Basic.Extensions;
using Game.Event;
using UnityEngine;

public class QQWWGiftView : MonoBehaviour
{
    public List<QQWWGiftViewItem> rewardItems;


    private void Start()
    {
        InitItems();
    }

    private void InitItems()
    {
        for(int i=0;i<rewardItems.Count;i++)
        {
            rewardItems[i].SetData(i);
        }
        for (int i = 0; i < rewardItems.Count; i++)
        {
            rewardItems[i].UpdateSort();
        }
    }


}