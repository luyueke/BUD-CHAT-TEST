using System;
using NetBusiness.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NetBusiness.TopPicks
{
    public enum TopPicksLeftType
    {
        None,
        Avatar,
        Emote,
    }
    
    public class TopPicksSeriesList
    {
        public SeriesData[] seriesList { get; set; }
    }

    public class SeriesData
    {
        public long id { get; set; }

        public string name { get; set; }
    }
    
    public class TopPicksOutfitMainList
    {
        public TopPicksOutfitList[] list { get; set; }
    }

    public class TopPicksOutfitList
    {
        public TopPicksOutfitData[] outfitList { get; set; }

        /// <summary>
        /// 类型，1 皮肤 2 emote
        /// </summary>
        public long resourceType { get; set; }
    }

    public class TopPicksOutfitData
    {
        /// <summary>
        /// 套装id
        /// </summary>
        public long outfitId { get; set; }

        /// <summary>
        /// 套装图片
        /// </summary>
        // public string outfitUrl { get; set; }

        public Store.ProductData[] products { get; set; }
    }
    
    public class TopPicksDataManager : GlobalInstance<TopPicksDataManager>
    {
        public TopPicksSeriesList seriesList { get; private set; }
        public TopPicksOutfitMainList outfitMainList { get; private set; }
        
        public void GetSeriesList(Action<TopPicksSeriesList> success = null, Action<string> fail = null)
        {
            return;
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.seriesList,
                HttpMethod.GET,"",
                onReceive: arg0 =>
                {
                    var data = JsonConvert.DeserializeObject<TopPicksSeriesList>(arg0);
                    if (data != null)
                    {
                        seriesList = data;
                        success?.Invoke(data);
                    }else
                    {
                        fail?.Invoke("BannerData解析失败");
                    }
                }, onFail: arg0 =>
                {
                    LoggerUtils.LogError(HttpUrlDefine.TokenData + "请求出错" + arg0);
                    fail?.Invoke(arg0);
                });
        }
        
        public void GetOutfitList(int seriesId,Action<TopPicksOutfitMainList> success = null, Action<string> fail = null)
        {
            return;
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.outfitList,
                HttpMethod.GET,JsonConvert.SerializeObject(new JObject
                {
                    ["seriesId"] = seriesId,
                }),
                onReceive: arg0 =>
                {
                    var data = JsonConvert.DeserializeObject<TopPicksOutfitMainList>(arg0);
                    if (data != null)
                    {
                        outfitMainList = data;
                        success?.Invoke(data);
                    }else
                    {
                        fail?.Invoke("BannerData解析失败");
                    }
                }, onFail: arg0 =>
                {
                    LoggerUtils.LogError(HttpUrlDefine.TokenData + "请求出错" + arg0);
                    fail?.Invoke(arg0);
                });
        }
    }
}