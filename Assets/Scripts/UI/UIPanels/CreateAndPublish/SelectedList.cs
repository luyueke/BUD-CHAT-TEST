using GameData.Base;
using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedList : MonoBehaviour
{
    [SerializeField] Transform contentRoot;
    [SerializeField] SelectedItem itemPrefab;

    private List<SelectedItem> items = new();

    public void SetList(List<DraftListItem> skins)
    {
        items.ForEach(i => i.gameObject.SetActive(false));
        for (int i = 0, C = skins.Count; i < C; i++)
        {
            var skin = skins[i];
            if (items.Count <= i)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                items.Add(item);
            }
            items[i].gameObject.SetActive(true);
            items[i].SetData(skin.skinInfo);
        }
    }
}
