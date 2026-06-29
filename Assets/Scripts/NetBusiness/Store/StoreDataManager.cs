using System;
using System.Collections.Generic;
using Es;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NetBusiness.Store
{
    public enum StoreLeftType
    {
        Hot,
        Avatar,
        Emote,
    }

    public enum StoreAvatarType
    {
        None,
        Hair,
        Clothes,
        Hats,
        Earring,
        Glasses,
        Visor,
        Shoe,
        Scarf,
        Backpack,
        Hand,
        Glove,
        Effect,
        Belt,
        Eyes,
        FacePaint,
    }

    public class BannerData
    {
        public string[] idList { get; set; }
    }

    public class ProductDataList
    {
        public List<ProductData> list { get; set; }
    }

    /// <summary>
    /// 跳转数据，如果商城里面的资源为扭蛋资源，返回所属的扭蛋系列Id
    /// </summary>
    public class ExtraJumpData
    {
        /// <summary>
        /// 扭蛋id
        /// </summary>
        public string gashaponId { get; set; }
    }

    public class ProductData
    {
        /// <summary>
        /// 跳转数据，如果商城里面的资源为扭蛋资源，返回所属的扭蛋系列Id
        /// </summary>
        public ExtraJumpData extraJumpData { get; set; }

        /// <summary>
        /// 等级，S = 1;     A = 2;     B = 3;
        /// </summary>
        public long level { get; set; }

        public string name { get; set; }

        public long pgcId { get; set; }

        /// <summary>
        /// 价格
        /// </summary>
        public long price { get; set; }

        /// <summary>
        /// 购买类型，coin = 1;  badge = 2;  gem = 3;
        /// </summary>
        public long purchaseType { get; set; }

        /// <summary>
        /// 是否拥有，0 未拥有 1 已拥有
        /// </summary>
        public long isOwned { get; set; }

        public GameResData gameResData { get; set; }
    }

    public class StoreDataManager : GlobalInstance<StoreDataManager>
    {
        public BannerData bannerData { get; private set; }
        public ProductDataList productData { get; private set; }

        public void GetBannerData(Action<BannerData> success = null, Action<string> fail = null)
        {
            return;
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.storeBanner,
                HttpMethod.GET, "",
                onReceive: arg0 =>
                {
                    var data = JsonConvert.DeserializeObject<BannerData>(arg0);
                    if (data != null)
                    {
                        bannerData = data;
                        success?.Invoke(data);
                    }
                    else
                    {
                        fail?.Invoke("BannerData解析失败");
                    }
                }, onFail: arg0 =>
                {
                    LoggerUtils.LogError(HttpUrlDefine.TokenData + "请求出错" + arg0);
                    fail?.Invoke(arg0);
                });
        }

        public void GetProductData(int resourceType, int subType, Action<ProductDataList> success = null,
            Action<string> fail = null)
        {
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.storeProduct,
                HttpMethod.GET, JsonConvert.SerializeObject(new JObject
                {
                    ["resourceType"] = resourceType,
                    ["subType"] = subType,
                }),
                arg0 =>
                {
                    var data = JsonConvert.DeserializeObject<ProductDataList>(arg0);
                    if (data != null)
                    {
                        productData = data;
                        if (data != null && data.list != null)
                        {
                            foreach (ProductData product in productData.list)
                            {
                                product.gameResData = DataTables.GetGameResData(product.pgcId.ToString());
                            }
                        }
                        success?.Invoke(data);
                    }
                    else
                    {
                        fail?.Invoke("BannerData解析失败");
                    }
                }, arg0 =>
                {
                    LoggerUtils.LogError(HttpUrlDefine.TokenData + "请求出错" + arg0);
                    fail?.Invoke(arg0);
                });
        }
    }
}