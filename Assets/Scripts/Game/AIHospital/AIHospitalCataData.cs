using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UI.Catalog {
    
    public enum CatalogComponentType
    {
        None = 0,
        Profession = 1,     // 角色信息
        InterativeAction = 2,      // 动作解锁
        Plot = 3,      // 剧情
    }

    /// <summary>
    /// 目录角色数据
    /// </summary>
    [Serializable]
    public class CatalogCharacterData
    {
        // 基本信息
        public string characterId;          // 角色ID
        public string characterName;        // 角色名称
        public string description;          // 角色描述
        public bool isUnlocked;             // 是否已解锁
        public int age;                     // 年龄
        public string gender;               // 性别

    }


    public class Gallery
    {
        public string galleryId;    // 图鉴ID
        public string galleryCover; // 图片封面
        public string name;         // 名称
        public int isLock;          // 是否锁住 (1表示锁住)
        public int isClaim;         // 是否领取 (1表示已领取)
        public RoleGallery roleGallery;
    }

    public class RoleGallery
    {
        public string roleAvatar;   // 角色头像
        public string roleName;     // 角色名称
        public int roleAge;         // 角色年龄
        public int roleGender;      // 角色性别
        public string roleDesc;     // 角色描述
        public string rolePlot;     // 角色剧情
        public List<RoleEmote> roleEmote; // 角色表情列表
        public string roleProfession;
    }

    public class RoleEmote
    {
        public string emoteId;      // 表情ID
        public int islock;          // 是否锁住 (1表示锁住)
    }

    public class ApiResponse
    {
        public int result { get; set; }
        public string rmsg { get; set; }
        public string requestId { get; set; }
        public ApiData data { get; set; }
    }

    public class ApiData
    {
        public List<Gallery> galleryList { get; set; }
    }


    public class AIHospitalCataData : GlobalInstance<AIHospitalCataData>
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
                ["gameId"] = 2,      //(int)PGCGameType.AIHospital,
                ["mapId"] = mapID
            };
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.hospitalCata,
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
                    if (mapID == "")
                    {
                        actions?.Invoke(_galleryList);
                    }
                    else
                    {
                        ugcActions?.Invoke(_galleryList);
                    }
                    
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

