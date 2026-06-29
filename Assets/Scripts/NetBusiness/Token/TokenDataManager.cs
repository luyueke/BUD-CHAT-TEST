using System;
using GameData.Gashapon;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GameData.Manager
{

    public class TokenData
    {
        /// <summary>
        /// badge数量
        /// </summary>
        public long Badge { get; set; }

        /// <summary>
        /// 金币数量
        /// </summary>
        public long Coin { get; set; }

        /// <summary>
        /// 钻石数量
        /// </summary>
        public long Gem { get; set; }

        /// <summary>
        /// 庆典币数量
        /// </summary>
        public long CelebrationCoin { get; set; }

        /// <summary>
        /// 社区乐器兑换卷数量
        /// </summary>
        public long CommunityInstrumentTicket { get; set; }
        /// <summary>
        /// 社区皮肤兑换卷数量
        /// </summary>
        public long CommunitySkinTicket { get; set; }
        /// <summary>
        /// 社区动作兑换卷数量
        /// </summary>
        public long CommunityAnimationTicket { get; set; }

        /// <summary>
        /// 社区载具兑换卷数量
        /// </summary>
        public long CommunityVehicleTicket { get; set; }
        /// <summary>
        /// 社区剧本兑换券
        /// </summary>
        public long CommunityTheaterTicket { get; set; }
        
        public long GetToken(CurrencyType tType)
        {
            switch (tType)
            {
                case CurrencyType.Badge:
                    return Badge;
                case CurrencyType.Coin:
                    return Coin;
                case CurrencyType.Gem:
                    return Gem;
                case CurrencyType.CelebrationCoin:
                    return CelebrationCoin;
                case CurrencyType.CommunityAnimationTicket:
                    return CommunityAnimationTicket;
                case CurrencyType.CommunityInstrumentTicket:
                    return CommunityInstrumentTicket;
                case CurrencyType.CommunitySkinTicket:
                    return CommunitySkinTicket;
                case CurrencyType.CommunityVehicleTicket:
                    return CommunityVehicleTicket;
                case CurrencyType.CommunityTheaterTicket:
                    return CommunityTheaterTicket;
                default:
                    return 0;
            }
        }
    }

    public class TokenDataManager : GlobalInstance<TokenDataManager>
    {
        public long Badge { get; private set; }
        public long Coin { get; private set; }
        public long Gem { get; private set; }
        /// <summary>
        /// 庆典币数量
        /// </summary>
        public long CelebrationCoin { get; private set; }

        /// <summary>
        /// 社区乐器兑换卷数量
        /// </summary>
        public long CommunityInstrumentTicket { get; private set; }
        /// <summary>
        /// 社区皮肤兑换卷数量
        /// </summary>
        public long CommunitySkinTicket { get; private set; }
        /// <summary>
        /// 社区动作兑换卷数量
        /// </summary>
        public long CommunityAnimationTicket { get; private set; }
        /// <summary>
        /// 社区载具兑换卷数量
        /// </summary>
        public long CommunityVehicleTicket { get; private set; }

        private TokenData _data = new TokenData();

        public TokenData Data { get => _data; }

        public void GetTokenData(Action<TokenData> success = null, Action<string> fail = null)
        {
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TokenData,
                HttpMethod.GET,"",
                onReceive: arg0 =>
                {
                    var data = JsonConvert.DeserializeObject<TokenData>(arg0);
                    if (data != null)
                    {
                        _data = data;
                        Badge = data.Badge;
                        Coin = data.Coin;
                        Gem = data.Gem;
                        CelebrationCoin = data.CelebrationCoin;
                        CommunityInstrumentTicket = data.CommunityInstrumentTicket;
                        CommunitySkinTicket = data.CommunitySkinTicket;
                        CommunityAnimationTicket = data.CommunityAnimationTicket;
                        CommunityVehicleTicket = data.CommunityVehicleTicket;

                        success?.Invoke(data);
                        MessageHelper.Broadcast(MessageName.TokenUpdate, data);
                        Debug.Log("刷新token成功！刷新的数值是：" + data.CelebrationCoin);
                        MessageHelper.Broadcast(MessageName.OnPlayerInfoAccountChange , CurrencyType.CelebrationCoin);
                    }
                    else
                    {
                        fail?.Invoke("TokenData解析失败");
                    }
                }, onFail: arg0 =>
                {
                    LoggerUtils.LogError(HttpUrlDefine.TokenData + "请求出错" + arg0);
                    fail?.Invoke(arg0);
                });
                
                }
    }
}
