using Game.Store;
using GameData.BaseInfo;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreviewBundleItemsList : MonoBehaviour
{
    [SerializeField] Transform content;
    [SerializeField] ToggleGroup toggleGroup;
    [SerializeField] PreviewBundleItem bundleItem;

    public Action<SkinInfo> isOnAction;
    public List<PreviewBundleItem> items = new();

    public void SetTarget(List<SkinInfo> datas)
    {
        content.ClearChildren();
        items.Clear();
        foreach (var asset in datas)
        {
            var item = Instantiate(bundleItem, content);

            item.SetData(asset, toggleGroup, (data) =>
            {
                isOnAction?.Invoke(data);
            });
            items.Add(item);
        }

        if (items.Count > 0) items[0].DefaultOn();
    }
}
