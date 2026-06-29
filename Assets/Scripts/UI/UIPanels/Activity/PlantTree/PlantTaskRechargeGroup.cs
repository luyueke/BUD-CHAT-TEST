using Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantTaskRechargeGroup : MonoBehaviour
{
    public List<PlantTaskRechargeGroupItem> Items;

    private void Awake()
    {
        MessageHelper.AddListener(MessageName.PlantTreeUpdate, RefreshView);
        MessageHelper.AddListener(MessageName.RechargePanelClose, RechargePanelClose);
    }

    private void RechargePanelClose()
    {
        Debug.Log(" PlantTaskRechargeGroup RechargePanelClose");
        PlantTreeSystem.Inst.ActivityReq();
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.PlantTreeUpdate, RefreshView);
        MessageHelper.RemoveListener(MessageName.RechargePanelClose, RechargePanelClose);
    }

    private void OnEnable()
    {
        RefreshView();
    }

    public void RefreshView() {
        Debug.Log("PlantTaskRechargeGroup RefreshView");
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].SetData();
        }
    }


}