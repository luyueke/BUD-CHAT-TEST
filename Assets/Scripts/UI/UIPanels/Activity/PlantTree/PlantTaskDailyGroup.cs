using Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantTaskDailyGroup : MonoBehaviour
{
    public List<PlantTaskDailyGroupItem> Items;

    private void Awake()
    {
        MessageHelper.AddListener(MessageName.PlantTreeUpdate, RefreshView);

    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.PlantTreeUpdate, RefreshView);
    }

    private void OnEnable()
    {
        RefreshView();
    }

    public void RefreshView()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].SetData();
        }
    }


}