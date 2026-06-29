using System.Collections;
using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;

public class BaseLimitedPackageItem : MonoBehaviour
{
    public List<LimitPackageItem> PackageItems;
    private List<BaseLimitPackageData> _curDataList;
    
    public void InitData(List<BaseLimitPackageData> dataList)
    {
        this._curDataList = dataList;
        BindData();
        // CheckPackLive();
    }

    private void BindData()
    {
        if (_curDataList.IsNullOrEmpty())
        {
            LoggerUtils.LogError("_curDataList是空的");
            return;
        }
        for (int i = 0; i < PackageItems.Count; i++)
        {
            var curItem = PackageItems[i];
            
            if (_curDataList.Count > i)
            {
                var data = _curDataList[i];
                curItem.InitData(data);
            }
        }
    }
    
    //检测是否已上线
    public void CheckPackLive()
    {
        bool isShow = false;
        foreach (var itemNode in PackageItems)
        {
            var data = itemNode?.GetBindData();
            if (data != null && BusinessLiveManager.Inst.IsLimitProductLive(data.productId))
            {
                itemNode.gameObject.SetActive(true);
                isShow = true;
            }
            else
            {
                itemNode.gameObject.SetActive(false);
            }
        }
        
        gameObject.SetActive(isShow);
    }

    public bool CheckSelectPackLive(string productId)
    {
        return BusinessLiveManager.Inst.IsLimitProductLive(productId);
    }

    public bool GetPackLive()
    {
        bool isShow = false;
        foreach (var itemNode in PackageItems)
        {
            var data = itemNode?.GetBindData();
            if (data != null && BusinessLiveManager.Inst.IsLimitProductLive(data.productId))
            {
                isShow = true;
            }
            else
            {
            }
        }
        
        return isShow;
    }
}
