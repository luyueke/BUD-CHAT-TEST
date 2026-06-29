using System;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UnityEngine;
using UnityEngine.Events;
using GameData;
using Newtonsoft.Json.Linq;
using UnityEngine.EventSystems;

public class AvatarOcFixData: AvatarOcData
{
    public bool isSelect = false;

    public string OcId
    {
        get
        {
            return ocInfo?.ocId ?? "";
        }
    }

    public static AvatarOcFixData Init(AvatarOcData ocData)
    {
        var fixeData = new AvatarOcFixData();
        fixeData.ocInfo = ocData.ocInfo;
        fixeData.interact = ocData.interact;
        return fixeData;
    }
}
public class ContestCreateOcRequester : MonoBehaviour
{
    public const string gAddOcKey = "CustomCreateOcKey";
    
    private string currentCookie = "";
    private bool isEnd = false;
    private bool isRequest = false;

    private bool CanLoadMore()
    {
        if (isRequest)
        {
            return false;
        }
        return !isEnd;
    }

    public void ResetCookie()
    {
        currentCookie = "";
        isEnd = false;
        isRequest = false;
    }
    
    public void GetData(bool forceRefresh = false, bool isPet = false, Action<List<AvatarOcFixData>> resultAction = null)
    {
        if (forceRefresh)
        {
            ResetCookie();
        }
        
        if (isRequest)
        {
            resultAction?.Invoke(new List<AvatarOcFixData>());
            return;
        }

        if (isEnd)
        {
            resultAction?.Invoke(new List<AvatarOcFixData>());
            return;
        }
        
        // 调用后端接口
        var jb = new JObject
        {
            ["cookie"] = currentCookie,
            ["skinType"] = isPet ? 1 : 0,
        };
        bool isNeedInset = string.IsNullOrEmpty(currentCookie);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ocList,
            HttpMethod.GET, 
            JsonConvert.SerializeObject(jb),
            onReceive: arg0 =>
            {
                isRequest = false;
                AvatarOcListRes serverData = JsonConvert.DeserializeObject<AvatarOcListRes>(arg0);
                currentCookie = serverData.cookie;
                isEnd = serverData.isEnd == 1;
                
                // 过滤掉白班数据
                var filterList = new List<AvatarOcFixData>();
                
                var fixedList = serverData?.list ?? new List<AvatarOcData>();
                foreach (AvatarOcData element in fixedList)
                {
                    var ocId = element?.ocInfo?.ocId;
                    if (!string.IsNullOrEmpty(ocId))
                    {
                        var fixedData = AvatarOcFixData.Init(element);
                        filterList.Add(fixedData);
                    }
                }

                if (isNeedInset)
                {
                    filterList.Insert(0, AddOcData());
                }
             
                resultAction?.Invoke(filterList);
            }, onFail: arg0 =>
            {
                isRequest = false;
                
                var fixedList = new List<AvatarOcFixData>();
                if (isNeedInset)
                {
                    fixedList.Insert(0, AddOcData());
                }
             
                resultAction?.Invoke(fixedList);
            });
    }

    private AvatarOcFixData AddOcData()
    {
        var data = new AvatarOcFixData();
        data.ocInfo = new AvatarOcInfo();
        data.ocInfo.ocId = gAddOcKey;
        return data;
    }

}
