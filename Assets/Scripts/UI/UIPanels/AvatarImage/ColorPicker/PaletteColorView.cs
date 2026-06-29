using System.Collections.Generic;
using UnityEngine;
using System;


/// <summary>
/// 全部颜色的展示面板
/// @stanley
/// </summary>
public class PaletteColorView : MonoBehaviour, IColorSetable
{
    public Transform colorParent;
    public GameObject colorItem;
    public Action<string> OnSelect;
    [HideInInspector]
    public RoleColorItem paletteCurItem;
    private List<RoleColorItem> paletteitems = new List<RoleColorItem>();
    private bool hasManualSet = false;
    private bool isInit = false;

    public void InitPaletteView(List<string> colors, Action<string> select)
    {
        if (isInit)
        {
            return;
        }

        isInit = true;
        OnSelect = select;
        for (int i = 0; i < colors.Count; i++)
        {
            var go = Instantiate(colorItem, colorParent);
            var goScript = go.GetComponent<RoleColorItem>();
            goScript.Init(colors[i], this.OnSelectClick);
            paletteitems.Add(goScript);
        }
    }

    public bool GetHasManualSet()
    {
        return hasManualSet;
    }

    public void OnSelectClick(RoleColorItem item)
    {
        if (paletteCurItem == item)
        {
            return;
        }
        if (paletteCurItem != null)
        {
            paletteCurItem.SetSelectState(false);
        }
        paletteCurItem = item;
        paletteCurItem.SetSelectState(true);
        OnSelect?.Invoke(paletteCurItem.rcData);
        hasManualSet = true;
    }

    public void SetSelect(string hexColor)
    {
        bool isExist = false;
        paletteitems.ForEach(x =>
        {
            x.SetSelectState(false);
            if (x.rcData.Equals(hexColor) || x.rcData.Equals(hexColor.ToLower()))
            {
                paletteCurItem = x;
                paletteCurItem.SetSelectState(true);
                OnSelect?.Invoke(paletteCurItem.rcData);
                isExist = true;
            }
        });
        if (!isExist)
        {
            paletteCurItem = null;
        }
    }
}
