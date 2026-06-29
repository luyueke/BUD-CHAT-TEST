// @Author: YangJie
// @Description:
// @Date:  2023/09/12
// @Modify:

using System;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UI.BaseOSA
{
    
    public class BaseDataList<T> where T: BaseData
    {
        public string cookie { get; set; }
        /// <summary>
        /// 是否结束，0 否 1 是
        /// </summary>
        public long isEnd { get; set; }
        public List<T> list;
    }

    public class BaseDataRawList
    {
        public string cookie { get; set; }
        /// <summary>
        /// 是否结束，0 否 1 是
        /// </summary>
        public long isEnd { get; set; }
        public object list;
    }

    public class BaseDataLoader<T> : MonoBehaviour where T: BaseData
    {
        
        private bool isEnd = false;
        private string cookie = "";
        
        protected  virtual string DataUrl => "";
        
        public virtual void ResetCookie()
        {
            cookie = "";
            isEnd = false;
        }
        
        public virtual void GetDataList(Action<List<T>> callback)
        {
            if (isEnd)
            {
                callback?.Invoke(new List<T>());
                return;
            }
            NetworkManager.Inst.SendHttpRequest(DataUrl,
                HttpMethod.GET,
                JsonConvert.SerializeObject(GetParams()),
                onReceive: arg0 =>
                {
                    BaseDataRawList rawData = null;
                    try
                    {
                        rawData = JsonConvert.DeserializeObject<BaseDataRawList>(arg0);
                    }
                    catch (Exception e)
                    {
                        LoggerUtils.LogError(DataUrl + "解析失败:" + arg0 + "," + e.Message);
                    }
              
                    // var dataList = new TD() { list = new List<T>()};
                    if (rawData != null)
                    {
                        cookie = rawData.cookie;
                        isEnd = rawData.isEnd == 1;
                        callback?.Invoke(rawData.list == null ? new List<T>() : OnDataResponse(rawData.list.ToString()));
                    }
                    else
                    {
                        callback?.Invoke(new List<T>());
                     
                    }

                }, onFail: arg0 =>
                {
                    callback?.Invoke(new List<T>());
                    LoggerUtils.LogError(DataUrl + "请求出错" + arg0);
                });
        }

        public virtual List<T> OnDataResponse(string responseData)
        {
            
            return JsonConvert.DeserializeObject<List<T>>(responseData);
        }

        public virtual JObject GetParams()
        {
            return new JObject
            {
                ["cookie"] = cookie,
            };
        }
        
        
    }
}