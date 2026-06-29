using Game.Store;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BundleItemsList : MonoBehaviour
{
    [SerializeField] Text bundleName;
    [SerializeField] Transform content;
    [SerializeField] BundleItem bundleItem;

    public void SetTarget(GoodsData goodsData)
    {
        if (goodsData.GoodsType != GoodsType.BundleUgc) return;

        content.ClearChildren();
        bundleName.text = goodsData.Name;
        foreach (var asset in goodsData.Assets)
        {
            var item = Instantiate(bundleItem, content);

            item.SetData(asset);
        }
    }
}
