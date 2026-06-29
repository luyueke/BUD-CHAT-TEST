using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
public class CabinBtnToggleParent : MonoBehaviour
{
    public List<CabinBtnToggle> cabinBtnToggles;
    public Action<int> onSelect;
    public void Init(int idx)
    {
        int i = 0;
        foreach (var item in cabinBtnToggles)
        {
            item.Init(i,this);
            i++;
        }
        OnClick(cabinBtnToggles[idx],idx);
    }
    public void OnClick(CabinBtnToggle cabinBtnToggle,int index)
    {
        foreach (var item in cabinBtnToggles)
        {
            item.UnSelect();
        }
        cabinBtnToggle.Select();
        onSelect?.Invoke(index);
    }
}
