using System;
using System.Collections.Generic;
using Es;
using UnityEngine;

public class EmoteSelectView : MonoBehaviour
{
    [SerializeField] private Transform itemParent;
    [SerializeField] private EmoteSelectItem itemPrefab;
    
    public string DefalutName { get; set; }
    protected List<EmoteSelectItem> Items { get; set; } = new List<EmoteSelectItem>();

    protected Action<EmoteSelectItem> onClick;
    

    public virtual void Refresh(List<string> emos ) 
    {
        for (int i = 0; i < emos.Count; ++i)
        {
            var emoteId = emos[i];
          
            EmoteSelectItem item;
            if (Items.Count <= i)
            {
                item = CreateItem();
            }
            else
            {
                item = Items[i];
            }

            if (string.IsNullOrEmpty(emoteId))
            {
                item.SetData(emoteId,DefalutName);
            }
            else
            {
                var emoteConfig = DataTables.GetPgcNameData(emoteId);
                item.SetData(emoteId,emoteConfig?.Name);
            }
           
            OnItemRefresh(item, true);
        }
        for (int j = emos.Count; j < Items.Count; ++j)
        {
            OnItemRefresh(Items[j], false);
        }
    }

    public void SetOnClick(Action<EmoteSelectItem> act)
    {
        onClick = act;
    }

    
    public void SetSingleSelect(string emoteId)
    {
        for(int i = 0; i < Items.Count; ++i)
        {
            Items[i].Selected = Items[i].EmoteId == emoteId;
        }
    }

    public void SetSingleSelectLoading(string emoteId)
    {
        for (int i = 0; i < Items.Count; ++i)
        {
            Items[i].SelectedLoading = Items[i].EmoteId == emoteId;
        }
    }
    
    protected virtual void OnClick(EmoteSelectItem item) 
    {
        SetSingleSelect(item.EmoteId);
        // SetSingleSelectLoading(item.EmoteId);
        onClick?.Invoke(item);
    }
    
    protected EmoteSelectItem CreateItem()
    {
        EmoteSelectItem item = Instantiate(itemPrefab, itemParent);
        item.AddOnClick(OnClick);
        Items.Add(item);
        return item;
    }

    protected virtual void OnItemRefresh(EmoteSelectItem item, bool isOn)
    {
        item.Show(isOn);
    }
}
