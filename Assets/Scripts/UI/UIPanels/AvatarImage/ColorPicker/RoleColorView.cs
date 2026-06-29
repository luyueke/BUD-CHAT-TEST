using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 
/// </summary>
public class RoleColorView : MonoBehaviour , IColorSetable
{
    public Transform colorParent;
    public GameObject colorItem;
    public RoleColorItem curItem;  
    public Action<string> OnSelect;
    private List<RoleColorItem> items = new List<RoleColorItem>();
    private bool hasManualSet = false;
    private bool isInit = false;

    public void Init(List<string> datas, Action<string> select)
    {
        if (isInit)
        {
            return;
        }

        isInit = true;
        OnSelect = select;
        for (int i = 0; i < datas.Count; i++)
        {
            var go = Instantiate(colorItem, colorParent);
            var goScript = go.GetComponent<RoleColorItem>();
            goScript.Init(datas[i], OnSelectClick);
            items.Add(goScript);
        }
    }

    public bool GetHasManualSet()
    {
        return hasManualSet;
    }

    public void OnSelectClick(RoleColorItem item)
    {
        if (curItem == item)
        {
            return;
        }
        if (curItem != null)
        {
            curItem.SetSelectState(false);
        }
        curItem = item;
        curItem.SetSelectState(true);
        OnSelect?.Invoke(curItem.rcData);
        hasManualSet = true;
    }

    public void SetSelect(string hexColor)
    {
        bool isExist=false;
        items.ForEach(x =>
        {
            x.SetSelectState(false);
            if (x.rcData.Equals(hexColor) || x.rcData.Equals(hexColor.ToLower()))
            {
                curItem = x;
                curItem.SetSelectState(true);
                OnSelect?.Invoke(curItem.rcData);
                isExist=true;
            }
        });
        if(isExist==false)
        {
            curItem=null;
        }
    }

}