using System;
using System.Collections;
using System.Collections.Generic;
using Game.Store;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class BundleExpandView : MonoBehaviour
{
    [SerializeField] private BundleExpandItem itemPrefab;
    [SerializeField] private Transform cacheNode;
    [SerializeField] private Transform content;
    [SerializeField] private Text nameText;
    [SerializeField] private Image viewBg;

    private List<BundleExpandItem> itemList = new List<BundleExpandItem>();
    private LinkedList<BundleExpandItem> cacheItems = new LinkedList<BundleExpandItem>();
    private void Awake()
    {
        
    }

    public void SetData(List<AssetsData> pgcList,int level = 0,string nameStr = "")
    {
        ClearItems();
        if (pgcList == null || pgcList.Count <= 0) return;
        foreach (var assetsData in pgcList)
        {
            var item = GetItem();
            item.Init(assetsData);
            if (level > 0)
            {
                item.SetLevel(level);
            }
            itemList.Add(item);
        }
        
        if (!string.IsNullOrEmpty(nameStr))
        {
            SetName(nameStr);
        }
    }

    public void Init(GashaponRewardData rewardData)
    {
        string bundleName = PgcUtils.GetBundleName(rewardData.BundleId);
        SetData(rewardData.PgcDatas,(int)rewardData.Level,bundleName);
    }
    
    private BundleExpandItem GetItem()
    {
        if (cacheItems != null && cacheItems.Count != 0)
        {
            BundleExpandItem cache = cacheItems.Last.Value;
            cacheItems.RemoveLast();
            cache.gameObject.SetActive(true);
            cache.transform.SetParent(content);
            return cache;
        }
        BundleExpandItem newIns = Instantiate(itemPrefab, content);
        return newIns;
    }
    private void RecycleItem(BundleExpandItem item)
    {
        if (cacheItems == null)
        {
            cacheItems = new LinkedList<BundleExpandItem>();
        }

        cacheItems.AddLast(item);
        item.gameObject.SetActive(false);
        item.SetSelectStatus(false);
        item.transform.SetParent(cacheNode);
    }
    private void ClearItems()
    {
        if (itemList != null)
        {
            for (int i = 0; i < itemList.Count; i++)
            {
                RecycleItem(itemList[i]);
            }
            itemList.Clear();
        }
    }
    
    public void SetBgColor(string colorStr)
    {
        viewBg.color = DataUtil.DeSerializeColorCheckHash(colorStr);
    }

    public void SetName(string bundleName)
    {
        nameText.SetLocalText(bundleName);
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
