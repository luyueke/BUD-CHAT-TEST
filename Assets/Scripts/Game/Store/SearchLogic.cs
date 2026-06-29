using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Store;
using GameData;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class SearchLogicMgr : GameInstance<SearchLogicMgr>
{

    public Dictionary<int, SearchSettingSectionListRsp> searchSettingSectionList = new();

    public SearchWordListRsp searchWordListRsp; //这里正常有几十条，客户端取10条，刷新的时候取下一页
    public static List<int> avatarSubTypeList = new() {   //新增内容需要在GetCacheFilterSearchParam添加默认勾选
            (int)AvatarSubType.Bundle, //套装
            (int)AvatarSubType.Eyes,
            (int)AvatarSubType.Hair, //头发
            (int)AvatarSubType.Clothes, //衣服
            107, //动作
            (int)AvatarSubType.Backpack,
            (int)AvatarSubType.Hats, //帽子
            (int)AvatarSubType.Shoe, //鞋子
            (int)AvatarSubType.Mouth,
            (int)AvatarSubType.Glasses,
            (int)AvatarSubType.Hand, //手部饰品
            (int)AvatarSubType.FacePaint,
            (int)AvatarSubType.MusicalInstrument,
            111,//载具
            114,//演员
            115,//剧场
    };
    public void PreGetData()
    {
        GetSearchWordList(null);
    }
    /// <summary>
    /// 获取搜索设置的section列表
    /// </summary>
    /// <param name="subType"></param>
    /// <param name="suc"></param>
    /// <param name="fail"></param>
    public void GetSearchSettingSectionList(int subType, int ugcType, Action<bool, SearchSettingSectionListRsp> func)
    {
        JObject req = new JObject();
        if (subType == 0)
        {
            req["ugcType"] = ugcType;
        }
        else
        {
            req["subType"] = subType;
            req["ugcType"] = ugcType;
        }
        ;
        Debug.Log("获取搜索设置的section列表: " + JsonConvert.SerializeObject(req));
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.getSelectedSectionList, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
        {
            Debug.Log("获取搜索设置的section列表成功: " + content);
            SearchSettingSectionListRsp searchSettingSectionListRsp = JsonConvert.DeserializeObject<SearchSettingSectionListRsp>(content);
            searchSettingSectionList[subType] = searchSettingSectionListRsp;
            func?.Invoke(true, searchSettingSectionListRsp);
        },
        (msg) =>
        {
            func?.Invoke(false, null);
        });
    }

    /// <summary>
    /// 设置搜索设置的section列表
    /// </summary>
    /// <param name="subType"></param>
    /// <param name="selectedIds"></param>
    /// <param name="func"></param>
    public void SetSearchSettingSectionList(int subType, int ugcType, List<string> selectedIds, Action<bool> func)
    {
        JObject req = new JObject();
        if (subType == 0)
        {
            req["ugcType"] = ugcType;
            req["ids"] = JArray.FromObject(selectedIds);
        }
        else
        {
            req["subType"] = subType;
            req["ugcType"] = ugcType;
            req["ids"] = JArray.FromObject(selectedIds);
        }
        ;
        Debug.Log("设置搜索设置的section列表: " + JsonConvert.SerializeObject(req));
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setSelectedSectionList, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
        {
            Debug.Log("设置搜索设置的section列表成功: " + content);
            searchSettingSectionList[subType].selectedIds = selectedIds;
            func?.Invoke(true);
        }, (msg) => { func?.Invoke(false); });
    }

    /// <summary>
    /// 获取商城推荐搜索词列表
    /// </summary>
    /// <param name="func"></param>
    public void GetSearchWordList(Action<bool> func)
    {
        JObject req = new JObject()
        {
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.searchWordList, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
        {
            Debug.Log("获取商城推荐搜索词列表: " + content);
            searchWordListRsp = JsonConvert.DeserializeObject<SearchWordListRsp>(content);
            func?.Invoke(true);
        }, (msg) => { func?.Invoke(false); });
    }
    #region 过滤参数
    /// <summary>
    /// 检查过滤参数是否变化
    /// </summary>
    /// <param name="classType"></param>
    /// <returns></returns>
    public bool CheckFilterParamChange(int classType)
    {
        if (!requestFilterParamDict.ContainsKey(classType))
        {
            return true;
        }
        if (requestFilterParamDict[classType] != GetCacheFilterSearchParam(classType))
        {
            return true;
        }
        return false;
    }

    Dictionary<int, JObject> requestFilterParamDict = new Dictionary<int, JObject>();
    /// <summary>
    /// 保存请求时的参数
    /// </summary>
    /// <param name="classType"></param>
    public void SaveRequestFilterParam(int classType)
    {
        requestFilterParamDict[classType] = GetCacheFilterSearchParam(classType);
    }

    /// <summary>
    /// 添加过滤参数
    /// </summary>
    /// <param name="req"></param>
    public JObject AddFilterSearchParam(JObject req, int classType)
    {
        JObject cacheReq = GetCacheFilterSearchParam(classType);
        if (cacheReq["sortType"] != null)
        {
            req["sortType"] = cacheReq["sortType"];
        }
        if (cacheReq["ownType"] != null)
        {
            req["ownType"] = cacheReq["ownType"];
        }
        if (classType == (int)OtherClass.UgcTheme)
        {
            if (cacheReq["subTypes"] != null)
            {
                req["subTypes"] = cacheReq["subTypes"];
            }
        }
        else
        {
            int ugcType = AvatarUgcSceneHandler.GetUgcType(classType);
            if (ugcType == (int)UgcType.Theatre)
            {
                req["subTypes"] = "115";
            }
            else if (ugcType == (int)UgcType.ActorCard)
            {
                req["subTypes"] = "114";
            }
        }
        // req["sortType"] = 0;//仅试衣间，排序类型，0：默认，1：商品价格，2：发布时间
        // req["ownType"] = 0;//仅试衣间, 拥有类型，0：全部，1：未拥有;
        // req["subTypes"] = "";//仅试衣间主题推荐，需要用英文逗号隔开,
        return req;
    }


    /// <summary>
    /// 获取过滤参数
    /// </summary>
    /// <returns></returns>
    public JObject GetCacheFilterSearchParam(int classType)
    {
        string cacheKey = "FilterSearchParam_" + classType;
        string cacheValue = PlayerPrefs.GetString(cacheKey);
        if (string.IsNullOrEmpty(cacheValue))
        {
            var req = new JObject();
            req["sortType"] = 0;
            req["ownType"] = 0;
            req["subTypes"] = string.Join(",", avatarSubTypeList.Select(item => item.ToString()).ToArray());  //默认全选,用逗号隔开
            PlayerPrefs.SetString(cacheKey, JsonConvert.SerializeObject(req));
            PlayerPrefs.Save();
            return req;
        }
        else
        {
            var req = JsonConvert.DeserializeObject<JObject>(cacheValue);
            string cacheKeySubTypes = "FilterSearchParamSubTypes_111_" + classType;
            if (!PlayerPrefs.HasKey(cacheKeySubTypes))
            {
                req["subTypes"] = req["subTypes"] + ",111";
                PlayerPrefs.SetString(cacheKeySubTypes, "1");
                PlayerPrefs.SetString(cacheKey, JsonConvert.SerializeObject(req));
                PlayerPrefs.Save();
            }
            string cacheKeySubTypes114 = "FilterSearchParamSubTypes_114_" + classType;
            if (!PlayerPrefs.HasKey(cacheKeySubTypes114))
            {
                req["subTypes"] = req["subTypes"] + ",114,115";
                PlayerPrefs.SetString(cacheKeySubTypes114, "1");
                PlayerPrefs.SetString(cacheKey, JsonConvert.SerializeObject(req));
                PlayerPrefs.Save();
            }
            return req;
        }
    }

    /// <summary>
    /// 保存过滤参数
    /// </summary>
    /// <param name="req"></param>
    public void SaveCacheFilterSearchParam(string key, object value, int type, int classType)
    {
        string cacheKey = "FilterSearchParam_" + classType;
        JObject req = GetCacheFilterSearchParam(classType);
        if (type == 1)
        {
            req[key] = int.Parse(value.ToString());
        }
        else if (type == 2)
        {
            if (string.IsNullOrEmpty(req[key].ToString()))
            {
                req[key] = value.ToString();
            }
            else
            {
                var arr = value.ToString().Split(',');
                string subTypes = "";
                for (int i = 0; i < arr.Length; i++)
                {
                    subTypes += arr[i] + ",";
                }
                subTypes += req[key].ToString();
                req[key] = subTypes;
            }

        }
        string cacheValue = JsonConvert.SerializeObject(req);
        PlayerPrefs.SetString(cacheKey, cacheValue);
        PlayerPrefs.Save();
    }


    public void RemoveCacheFilterSearchParam(string key, object value, int classType)
    {
        string cacheKey = "FilterSearchParam_" + classType;
        JObject req = GetCacheFilterSearchParam(classType);
        if (!string.IsNullOrEmpty(req[key].ToString()))
        {
            var arr = req[key].ToString().Split(',');
            string subTypes = "";
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == value.ToString())
                {
                    continue;
                }
                subTypes += arr[i] + ",";
            }
            if (!string.IsNullOrEmpty(subTypes))
            {
                subTypes = subTypes.TrimEnd(',');
            }
            req[key] = subTypes;
        }
        string cacheValue = JsonConvert.SerializeObject(req);
        PlayerPrefs.SetString(cacheKey, cacheValue);
        PlayerPrefs.Save();
    }
    #endregion

    /// <summary>
    /// 热词搜索上传接口
    /// </summary>
    /// <param name="id"></param>
    /// <param name="func"></param>
    public void UploadSearchWords(int id, Action<bool> func)
    {
        JObject req = new JObject()
        {
            ["id"] = id,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.uploadSearchWords, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
        {
            func?.Invoke(true);
        }, (msg) => { func?.Invoke(false); });
    }

    /// <summary>
    /// 获取搜索历史词列表
    /// </summary>
    /// <returns></returns>
    public List<string> GetSearchHistoryWordList()
    {
        List<string> list = new List<string>();
        string cacheKey = "SearchHistoryWordList";
        string cacheValue = PlayerPrefs.GetString(cacheKey);
        if (string.IsNullOrEmpty(cacheValue))
        {
            return list;
        }
        list = JsonConvert.DeserializeObject<List<string>>(cacheValue);
        if (list == null || list.Count == 0)
        {
            return list;
        }
        return list;
    }

    /// <summary>
    /// 添加搜索历史词 最多保留10条
    /// </summary>
    /// <param name="word"></param>
    public void AddSearchHistoryWord(string word)
    {
        List<string> list = GetSearchHistoryWordList();
        if (list.Contains(word))
        {
            list.Remove(word);
        }
        list.Insert(0, word);
        if (list.Count > 10)
        {
            list.RemoveAt(10);
        }
        string cacheKey = "SearchHistoryWordList";
        string cacheValue = JsonConvert.SerializeObject(list);
        PlayerPrefs.SetString(cacheKey, cacheValue);
        PlayerPrefs.Save();
    }

    public void DeleteSearchHistoryWord()
    {
        string cacheKey = "SearchHistoryWordList";
        PlayerPrefs.DeleteKey(cacheKey);
        PlayerPrefs.Save();
    }
}

#region 搜索设置section列表
public class SearchSettingSectionListRsp
{
    public List<SearchSettingSectionItem> List;
    public List<string> selectedIds;
}


public class SearchSettingSectionItem
{
    public string sectionId;
    public string sectionName;
}

#endregion

#region 商城推荐搜索词列表
public class SearchWordListRsp
{
    public List<SearchWordItem> list;
    int pageIndex = 1;
    public void MoveNextPage()
    {
        pageIndex++;
        if (pageIndex > Mathf.Ceil((float)list.Count / 10))
        {
            pageIndex = 1;
        }
    }
    public List<SearchWordItem> GetSearchWordList()
    {
        List<SearchWordItem> tList = new();
        if (list == null || list.Count == 0)
        {
            return tList;
        }
        int minCnt = Math.Min(list.Count, 10);
        int addCnt = 0;
        //取10条，按页数循环取数据
        for (int i = (pageIndex - 1) * 10; i < list.Count; i++)
        {
            if (addCnt < minCnt)
            {
                tList.Add(list[i]);
                addCnt++;
            }
            else
            {
                break;
            }
        }
        if (addCnt < minCnt)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (addCnt < minCnt)
                {
                    tList.Add(list[i]);
                    addCnt++;
                }
                else
                {
                    break;
                }
            }
        }
        //取满10条
        return tList;
    }
}



public class SearchWordItem
{
    public int id;
    public string word;
}
#endregion