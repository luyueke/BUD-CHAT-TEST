using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UI.Catalog {
    
  


    public class AIParkCataData : GlobalInstance<AIParkCataData>
    {   

        public Action<List<Gallery>> actions;//图鉴回调
        public Action<List<Gallery>> ugcActions; //ugc图鉴回调
        string mapID = "";
        private List<Gallery> _galleryList; //图鉴数据保存

        //Get函数
        public List<Gallery>  GetGalleryData()
        {
            return _galleryList;
        }

        public void TryGetCataData()
        {
            GetCataData(mapID);
        }

        //网络请求
        public void GetCataData(string _mapID)
        {
            mapID = _mapID;
            JObject galleryClaim = new JObject
            {
                ["gameId"] = 2,      //(int)PGCGameType.AIPark,
                ["mapId"] = mapID
            };
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ParkCata,
                HttpMethod.GET,
                JsonConvert.SerializeObject(galleryClaim),
                onReceive: msg =>
                {
                    if (string.IsNullOrEmpty(msg))
                    {
                        LoggerUtils.LogError("响应为空");
                        return;
                    }
                    


                    JObject jsonObject = null;

                    var response = JsonConvert.DeserializeObject<ApiData>(msg);


                    _galleryList = response.galleryList;
                    
                    //UpdateRedPoint();

                    // 检查galleryList
                    if (_galleryList == null)
                    {   
                        LoggerUtils.LogError("galleryList解析为null");
                        return;
                    }

                    if (_galleryList.Count == 0)
                    {
                        LoggerUtils.LogError("galleryList为空列表");
                        return;
                    }

                    // 更新组件 - 确保有数据后再处理

                    LoggerUtils.Log("成功更新组件");

                    if (string.IsNullOrEmpty(mapID))
                    {
                        actions?.Invoke(_galleryList);
                    }
                    else
                    {
                        ugcActions?.Invoke(_galleryList);
                    }


                }, onFail: arg0 =>
                {
                    // 处理失败响应
                    HttpResponseRawData httpResponseRawData = JsonConvert.DeserializeObject<HttpResponseRawData>(arg0);
                    if (httpResponseRawData == null)
                    {
                        return;
                    }

                    // 显示错误提示
                    string rmsg = httpResponseRawData.rmsg;
                });
        }

        //结束销毁，线程安全
        public override void Release()
        {
            base.Release();
            _galleryList?.Clear();
            actions = null;
        }
    }

}

