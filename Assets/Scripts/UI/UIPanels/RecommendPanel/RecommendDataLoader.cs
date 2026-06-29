using System;
using System.Collections.Generic;
using GameData.Base;
using GameData.MapData;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class MapRecommendParam
{
    public string cookie;
    public string sectionId;
    public int ugcType;
}

public class RecommendDataLoader
{
    private bool isEnd = false;
    private string cookie = "";
    private string sectionId = "";

    public void SetType(string id)
    {
        sectionId = id;
    }

    public void ResetCookie()
    {
        cookie = "";
        isEnd = false;
    }

    public void GetSectionList(UnityAction<RecommendDataList> resultAction = null, bool isCache = false)
    {
        if (isEnd)
        {
            resultAction?.Invoke(new RecommendDataList {list = new List<RecommendData>()});
            return;
        }

        UnityAction<string> onReceive = arg0 =>
        {
            var data = JsonConvert.DeserializeObject<RecommendDataList>(arg0);
            if (data != null)
            {
                cookie = data.cookie;
                isEnd = data.isEnd == 1;
                resultAction?.Invoke(data);
            }
        };

        NetCacheEvent evt = isCache ? new NetCacheEvent(onReceive) : null;
        var httpData = Es.DataTables.GetNetHttpBusiness(HttpNetCommand.GameSectionInfoRecommend);
        JObject jObject = JsonConvert.DeserializeObject<JObject>(httpData.ParamStr);
        jObject["cookie"] = cookie;
        jObject["sectionId"] = sectionId;
        NetworkProxy.SendHttpRequest(httpData.HttpUrl,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jObject),
            onReceive,
            onFail: arg0 => { resultAction?.Invoke(new RecommendDataList {list = new List<RecommendData>()}); },
            evt);
    }

}


public enum RecommendEnum
{
    Official,
    Selection,
    Clothes = 2,
    Prop = 3,
    Material = 4
}

public class RecommendConfig
{
    public string name;
    public RecommendEnum type;
}

public class RecommendData
{
    public UgcBaseInfo ugcInfo;
    public BaseCreator creatorInfo;
    public BaseInteractInfo interactInfo;
    public int type;
}

public class RecommendDataList
{
    public string cookie { get; set; }
    /// <summary>
    /// 是否结束，0 否 1 是
    /// </summary>
    public long isEnd { get; set; }
    public List<RecommendData> list;
}

public class SectionConfig
{
    public string SectionName;
    public string SectionId;
    public int type;
}
     
public class SectionConfigList
{
    public List<SectionConfig> list;
}