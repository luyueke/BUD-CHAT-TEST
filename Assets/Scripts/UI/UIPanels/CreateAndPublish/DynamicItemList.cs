using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRefreshable<T>
{
    public void Refresh(T data);
}

public class DynamicItemList<ItemT, DataT> where ItemT : MonoBehaviour, IRefreshable<DataT>
{
    public List<ItemT> AllItems { get; private set; }
    public List<DataT> AllDatas { get; private set; }
    private Func<ItemT> itemCreator;

    public DynamicItemList(Func<ItemT> creator)
    {
        AllItems = new List<ItemT>();
        AllDatas = new List<DataT>();
        itemCreator = creator;
    }

    public void Refresh(List<DataT> newDatas)
    {
        AllDatas.Clear();
        AllDatas.AddRange(newDatas);
        Refresh();
    }

    public void Refresh()
    {
        for(int i = 0; i < AllDatas.Count; ++i)
        {
            ItemT item;
            if (i >= AllItems.Count)
            {
                item = itemCreator.Invoke();
                AllItems.Add(item);
            }
            else
            {
                item = AllItems[i];
            }
            item.gameObject.SetActive(true);
            item.Refresh(AllDatas[i]);
        }
        for(int j = AllDatas.Count; j < AllItems.Count; ++j)
        {
            AllItems[j].gameObject.SetActive(false);
        }
    }
}
